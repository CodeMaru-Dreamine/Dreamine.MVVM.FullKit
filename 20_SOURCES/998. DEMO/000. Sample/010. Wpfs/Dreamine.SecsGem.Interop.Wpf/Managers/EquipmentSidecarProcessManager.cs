using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using Dreamine.SecsGem.Interop.Wpf.Models;

namespace Dreamine.SecsGem.Interop.Wpf.Managers;

public sealed class EquipmentSidecarProcessManager : IAsyncDisposable
{
    private readonly IEquipmentSidecarProcessFactory _processFactory;
    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
    private readonly object _stateGate = new();
    private IEquipmentSidecarProcess? _process;
    private Task? _monitorTask;
    private Task? _disposeTask;
    private TimeSpan _naturalExitGracePeriod;
    private bool _forceStopRequested;
    private bool _disposeRequested;
    private EquipmentSidecarState _state = EquipmentSidecarState.Stopped;
    private int? _processId;
    private int? _lastExitCode;

    public EquipmentSidecarProcessManager() : this(new SystemEquipmentSidecarProcessFactory()) { }

    internal EquipmentSidecarProcessManager(IEquipmentSidecarProcessFactory processFactory) =>
        _processFactory = processFactory ?? throw new ArgumentNullException(nameof(processFactory));

    public event EventHandler<EquipmentSidecarOutputEventArgs>? OutputReceived;
    public event EventHandler<EquipmentSidecarStateChangedEventArgs>? StateChanged;
    public event EventHandler<EquipmentSidecarExitedEventArgs>? Exited;

    public bool IsRunning
    {
        get
        {
            lock (_stateGate)
                return _state is EquipmentSidecarState.Starting or EquipmentSidecarState.Running or
                    EquipmentSidecarState.Stopping;
        }
    }

    public EquipmentSidecarState State
    {
        get { lock (_stateGate) return _state; }
    }

    public int? ProcessId
    {
        get { lock (_stateGate) return _processId; }
    }

    public int? LastExitCode
    {
        get { lock (_stateGate) return _lastExitCode; }
    }

    public static ProcessStartInfo CreateStartInfo(EquipmentSidecarStartOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        var executablePath = Path.GetFullPath(options.ExecutablePath);
        var evidencePath = Path.GetFullPath(options.EvidencePath);
        var isAssembly = Path.GetExtension(executablePath).Equals(".dll", StringComparison.OrdinalIgnoreCase);
        var startInfo = new ProcessStartInfo
        {
            FileName = isAssembly ? "dotnet" : executablePath,
            WorkingDirectory = Path.GetDirectoryName(executablePath)!,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = new UTF8Encoding(false),
            StandardErrorEncoding = new UTF8Encoding(false)
        };

        if (isAssembly) startInfo.ArgumentList.Add(executablePath);
        AddOption(startInfo, "--bind", options.BindAddress);
        AddOption(startInfo, "--port", options.Port.ToString(CultureInfo.InvariantCulture));
        AddOption(startInfo, "--session-id", options.SessionId.ToString(CultureInfo.InvariantCulture));
        AddOption(startInfo, "--connection-limit", options.ConnectionLimit.ToString(CultureInfo.InvariantCulture));
        if (options.RequestHostTime) startInfo.ArgumentList.Add("--request-host-time");
        AddOption(startInfo, "--evidence", evidencePath);
        if (options.TelemetryStdout) startInfo.ArgumentList.Add("--telemetry-stdout");
        return startInfo;
    }

    public async Task StartAsync(
        EquipmentSidecarStartOptions options,
        CancellationToken cancellationToken = default)
    {
        var startInfo = CreateStartInfo(options);
        await _lifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ThrowIfDisposeRequested();
            if (_process is not null)
                throw new InvalidOperationException("An equipment sidecar process is already owned by this manager.");

            SetState(EquipmentSidecarState.Starting);
            IEquipmentSidecarProcess? process = null;
            try
            {
                process = _processFactory.Create(startInfo);
                process.OutputReceived += OnProcessOutputReceived;
                _process = process;
                _naturalExitGracePeriod = options.NaturalExitGracePeriod;
                _forceStopRequested = false;
                lock (_stateGate)
                {
                    _processId = null;
                    _lastExitCode = null;
                }

                process.Start();
                var processId = process.Id;
                lock (_stateGate) _processId = processId;
                SetState(EquipmentSidecarState.Running);
                _monitorTask = MonitorExitAsync(process, processId);
            }
            catch (Exception exception)
            {
                if (process is not null)
                {
                    process.OutputReceived -= OnProcessOutputReceived;
                    process.Dispose();
                }
                _process = null;
                _monitorTask = null;
                lock (_stateGate) _processId = null;
                SetState(EquipmentSidecarState.Faulted, exception.GetType().Name);
                throw;
            }
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    public async Task<bool> ForceStopAsync(CancellationToken cancellationToken = default)
    {
        IEquipmentSidecarProcess process;
        Task monitorTask;
        await _lifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ThrowIfDisposeRequested();
            if (_process is null || _monitorTask is null) return false;
            process = _process;
            monitorTask = _monitorTask;
            _forceStopRequested = true;
            SetState(EquipmentSidecarState.Stopping, "Force stop requested for the owned process.");
        }
        finally
        {
            _lifecycleGate.Release();
        }

        var killed = TryKillOwnedProcess(process);
        await monitorTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        return killed;
    }

    public ValueTask DisposeAsync()
    {
        lock (_stateGate)
        {
            if (_disposeTask is not null) return new ValueTask(_disposeTask);
            _disposeRequested = true;
            _disposeTask = DisposeCoreAsync();
            return new ValueTask(_disposeTask);
        }
    }

    private async Task DisposeCoreAsync()
    {
        IEquipmentSidecarProcess? process;
        Task? monitorTask;
        TimeSpan gracePeriod;

        await _lifecycleGate.WaitAsync().ConfigureAwait(false);
        try
        {
            process = _process;
            monitorTask = _monitorTask;
            gracePeriod = _naturalExitGracePeriod;
            if (process is not null)
                SetState(EquipmentSidecarState.Stopping, "Waiting for the owned process to exit naturally.");
        }
        finally
        {
            _lifecycleGate.Release();
        }

        if (process is not null && monitorTask is not null)
        {
            if (!monitorTask.IsCompleted && gracePeriod > TimeSpan.Zero)
                _ = await Task.WhenAny(monitorTask, Task.Delay(gracePeriod)).ConfigureAwait(false);

            if (!monitorTask.IsCompleted)
            {
                await _lifecycleGate.WaitAsync().ConfigureAwait(false);
                try
                {
                    if (ReferenceEquals(_process, process)) _forceStopRequested = true;
                }
                finally
                {
                    _lifecycleGate.Release();
                }
                _ = TryKillOwnedProcess(process);
            }

            await monitorTask.ConfigureAwait(false);
        }

        SetState(EquipmentSidecarState.Disposed);
    }

    private async Task MonitorExitAsync(IEquipmentSidecarProcess process, int processId)
    {
        var exitCode = -1;
        Exception? observationFailure = null;
        try
        {
            await process.WaitForExitAndDrainAsync().ConfigureAwait(false);
            exitCode = process.ExitCode;
        }
        catch (Exception exception)
        {
            observationFailure = exception;
        }

        var forceStopped = false;
        var owned = false;
        await _lifecycleGate.WaitAsync().ConfigureAwait(false);
        try
        {
            owned = ReferenceEquals(_process, process);
            if (owned)
            {
                forceStopped = _forceStopRequested;
                process.OutputReceived -= OnProcessOutputReceived;
                _process = null;
                lock (_stateGate)
                {
                    _processId = null;
                    _lastExitCode = exitCode;
                }
                SetState(observationFailure is null
                    ? EquipmentSidecarState.Exited
                    : EquipmentSidecarState.Faulted,
                    observationFailure?.GetType().Name ?? string.Empty);
            }
        }
        finally
        {
            _lifecycleGate.Release();
            process.Dispose();
        }

        if (owned)
            RaiseSafely(Exited, new EquipmentSidecarExitedEventArgs(processId, exitCode, forceStopped));
    }

    private void OnProcessOutputReceived(
        EquipmentSidecarOutputStream stream,
        string line) =>
        RaiseSafely(OutputReceived, new EquipmentSidecarOutputEventArgs(DateTimeOffset.Now, stream, line));

    private bool TryKillOwnedProcess(IEquipmentSidecarProcess process)
    {
        if (HasExited(process)) return false;
        try
        {
            process.Kill(entireProcessTree: true);
            return true;
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
        catch (InvalidOperationException) when (HasExited(process))
        {
            return false;
        }
    }

    private static bool HasExited(IEquipmentSidecarProcess process)
    {
        try { return process.HasExited; }
        catch (ObjectDisposedException) { return true; }
        catch (InvalidOperationException) { return false; }
    }

    private void ThrowIfDisposeRequested()
    {
        lock (_stateGate)
            ObjectDisposedException.ThrowIf(_disposeRequested, this);
    }

    private void SetState(EquipmentSidecarState state, string detail = "")
    {
        EquipmentSidecarState previous;
        lock (_stateGate)
        {
            previous = _state;
            if (previous == state) return;
            _state = state;
        }
        RaiseSafely(StateChanged, new EquipmentSidecarStateChangedEventArgs(previous, state, detail));
    }

    private void RaiseSafely<TEventArgs>(
        EventHandler<TEventArgs>? handlers,
        TEventArgs eventArgs)
        where TEventArgs : EventArgs
    {
        if (handlers is null) return;
        foreach (EventHandler<TEventArgs> handler in handlers.GetInvocationList())
        {
            try { handler(this, eventArgs); }
            catch
            {
                // A UI or telemetry callback must not own the child-process lifetime.
            }
        }
    }

    private static void AddOption(ProcessStartInfo startInfo, string name, string value)
    {
        startInfo.ArgumentList.Add(name);
        startInfo.ArgumentList.Add(value);
    }
}

internal interface IEquipmentSidecarProcessFactory
{
    IEquipmentSidecarProcess Create(ProcessStartInfo startInfo);
}

internal interface IEquipmentSidecarProcess : IDisposable
{
    event Action<EquipmentSidecarOutputStream, string>? OutputReceived;
    int Id { get; }
    bool HasExited { get; }
    int ExitCode { get; }
    void Start();
    void Kill(bool entireProcessTree);
    Task WaitForExitAndDrainAsync();
}

internal sealed class SystemEquipmentSidecarProcessFactory : IEquipmentSidecarProcessFactory
{
    public IEquipmentSidecarProcess Create(ProcessStartInfo startInfo) => new SystemEquipmentSidecarProcess(startInfo);
}

internal sealed class SystemEquipmentSidecarProcess : IEquipmentSidecarProcess
{
    private readonly Process _process;
    private bool _started;

    public SystemEquipmentSidecarProcess(ProcessStartInfo startInfo)
    {
        ArgumentNullException.ThrowIfNull(startInfo);
        _process = new Process { StartInfo = startInfo };
        _process.OutputDataReceived += OnOutputDataReceived;
        _process.ErrorDataReceived += OnErrorDataReceived;
    }

    public event Action<EquipmentSidecarOutputStream, string>? OutputReceived;
    public int Id => _process.Id;
    public bool HasExited => _process.HasExited;
    public int ExitCode => _process.ExitCode;

    public void Start()
    {
        if (_started) throw new InvalidOperationException("The equipment sidecar process was already started.");
        if (!_process.Start()) throw new InvalidOperationException("The equipment sidecar process did not start.");
        _started = true;
        try
        {
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
        }
        catch
        {
            try
            {
                if (!_process.HasExited) _process.Kill(entireProcessTree: true);
                _process.WaitForExit();
            }
            catch
            {
                // Preserve the stream-drain setup failure.
            }
            throw;
        }
    }

    public void Kill(bool entireProcessTree) => _process.Kill(entireProcessTree);

    public async Task WaitForExitAndDrainAsync()
    {
        await _process.WaitForExitAsync().ConfigureAwait(false);
        _process.WaitForExit();
    }

    public void Dispose()
    {
        _process.OutputDataReceived -= OnOutputDataReceived;
        _process.ErrorDataReceived -= OnErrorDataReceived;
        _process.Dispose();
    }

    private void OnOutputDataReceived(object sender, DataReceivedEventArgs eventArgs)
    {
        if (eventArgs.Data is { } line)
            OutputReceived?.Invoke(EquipmentSidecarOutputStream.StandardOutput, line);
    }

    private void OnErrorDataReceived(object sender, DataReceivedEventArgs eventArgs)
    {
        if (eventArgs.Data is { } line)
            OutputReceived?.Invoke(EquipmentSidecarOutputStream.StandardError, line);
    }
}
