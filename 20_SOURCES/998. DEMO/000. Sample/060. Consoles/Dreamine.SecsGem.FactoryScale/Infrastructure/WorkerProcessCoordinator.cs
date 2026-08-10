using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Dreamine.SecsGem.FactoryScale.Infrastructure;

internal sealed record WorkerLaunchRequest(
    string RunId,
    string WorkerId,
    string ProgramPath,
    string WorkingDirectory,
    int StartIndex,
    int Count,
    string ControlDirectory,
    string ResultPath,
    IReadOnlyDictionary<string, string>? AdditionalOptions = null,
    TimeSpan? ReadyTimeout = null,
    TimeSpan? ShutdownTimeout = null);

internal sealed record WorkerReadyRecord(
    string RunId,
    string WorkerId,
    int ProcessId,
    int StartIndex,
    int Count,
    DateTimeOffset ReadyAt,
    string? EndpointManifestPath = null);

internal sealed record WorkerHeartbeatRecord(
    string RunId,
    string WorkerId,
    int ProcessId,
    DateTimeOffset Timestamp,
    string State,
    int Connected,
    int Selected,
    long Requests,
    long Responses,
    int TrackedOperations,
    int QueueDepth);

internal sealed record WorkerStopRequest(
    string RunId,
    string WorkerId,
    DateTimeOffset RequestedAt,
    int ParentProcessId);

internal sealed record WorkerProcessResult(
    string RunId,
    string WorkerId,
    int ProcessId,
    int ExitCode,
    bool KilledAfterGracefulTimeout,
    string StandardOutputTail,
    string StandardErrorTail,
    string ResultPath,
    JsonElement? Result);

internal sealed class WorkerProcessCoordinator : IAsyncDisposable
{
    private static readonly TimeSpan DefaultReadyTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan DefaultShutdownTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan ForcedExitTimeout = TimeSpan.FromSeconds(5);
    private readonly ConcurrentDictionary<string, WorkerHandle> _workers =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly TaskCompletionSource _disposeCompletion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _disposed;

    internal IReadOnlyCollection<string> WorkerIds => _workers.Keys.ToArray();
    internal int RunningWorkerCount => _workers.Values.Count(handle => handle.IsRunning);

    internal static string ResolveCurrentProgramPath()
    {
        var assemblyPath = Assembly.GetEntryAssembly()?.Location;
        if (!string.IsNullOrWhiteSpace(assemblyPath) && File.Exists(assemblyPath))
            return assemblyPath;
        return Environment.ProcessPath is { Length: > 0 } processPath && File.Exists(processPath)
            ? processPath
            : throw new InvalidOperationException("The current Factory-Scale program path could not be resolved.");
    }

    internal async Task<IReadOnlyDictionary<string, WorkerReadyRecord>> StartAllAsync(
        IEnumerable<WorkerLaunchRequest> requests,
        int startConcurrency,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(requests);
        if (startConcurrency <= 0) throw new ArgumentOutOfRangeException(nameof(startConcurrency));
        ThrowIfDisposed();

        var materialized = requests.ToArray();
        var duplicate = materialized.GroupBy(request => request.WorkerId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
            throw new ArgumentException($"Duplicate worker id '{duplicate.Key}'.", nameof(requests));

        var ready = new ConcurrentDictionary<string, WorkerReadyRecord>(StringComparer.OrdinalIgnoreCase);
        try
        {
            await Parallel.ForEachAsync(materialized, new ParallelOptions
            {
                MaxDegreeOfParallelism = startConcurrency,
                CancellationToken = cancellationToken
            }, async (request, token) =>
            {
                var record = await StartAsync(request, token).ConfigureAwait(false);
                ready[request.WorkerId] = record;
            }).ConfigureAwait(false);
            return ready;
        }
        catch
        {
            await StopAllCoreAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    internal async Task<WorkerReadyRecord> StartAsync(
        WorkerLaunchRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ThrowIfDisposed();
        Validate(request);

        var paths = WorkerFileProtocol.Paths(request.ControlDirectory, request.RunId, request.WorkerId);
        Directory.CreateDirectory(paths.Directory);
        WorkerFileProtocol.DeleteForNewRun(paths.ReadyPath);
        WorkerFileProtocol.DeleteForNewRun(paths.HeartbeatPath);
        WorkerFileProtocol.DeleteForNewRun(paths.StopPath);
        WorkerFileProtocol.DeleteForNewRun(request.ResultPath);

        var process = new Process
        {
            StartInfo = CreateStartInfo(request),
            EnableRaisingEvents = true
        };
        var handle = new WorkerHandle(request, paths, process);
        if (!_workers.TryAdd(request.WorkerId, handle))
        {
            process.Dispose();
            throw new InvalidOperationException($"Worker '{request.WorkerId}' is already registered.");
        }

        try
        {
            if (!process.Start())
                throw new InvalidOperationException($"Worker '{request.WorkerId}' did not start.");
            handle.ProcessId = process.Id;
            handle.StartOutputDrain();
            var ready = await WaitForReadyAsync(
                handle,
                request.ReadyTimeout ?? DefaultReadyTimeout,
                cancellationToken).ConfigureAwait(false);
            handle.Ready = ready;
            return ready;
        }
        catch
        {
            try { await StopHandleAsync(handle, CancellationToken.None).ConfigureAwait(false); }
            catch { }
            _workers.TryRemove(request.WorkerId, out _);
            process.Dispose();
            throw;
        }
    }

    internal async Task<WorkerHeartbeatRecord?> TryReadHeartbeatAsync(
        string workerId,
        CancellationToken cancellationToken)
    {
        if (!_workers.TryGetValue(workerId, out var handle)) return null;
        try
        {
            var heartbeat = await WorkerFileProtocol.ReadJsonAsync<WorkerHeartbeatRecord>(
                handle.Paths.HeartbeatPath,
                cancellationToken).ConfigureAwait(false);
            return heartbeat.RunId.Equals(handle.Request.RunId, StringComparison.Ordinal) &&
                   heartbeat.WorkerId.Equals(handle.Request.WorkerId, StringComparison.OrdinalIgnoreCase) &&
                   heartbeat.ProcessId == handle.ProcessId
                ? heartbeat
                : null;
        }
        catch (Exception exception) when (exception is IOException or JsonException or FileNotFoundException)
        {
            return null;
        }
    }

    internal async Task<WorkerProcessResult?> StopAsync(
        string workerId,
        CancellationToken cancellationToken)
    {
        if (!_workers.TryGetValue(workerId, out var handle)) return null;
        var result = await StopHandleAsync(handle, cancellationToken).ConfigureAwait(false);
        _workers.TryRemove(workerId, out _);
        return result;
    }

    internal Task<IReadOnlyList<WorkerProcessResult>> StopAllAsync(CancellationToken cancellationToken) =>
        StopAllCoreAsync(cancellationToken);

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            await _disposeCompletion.Task.ConfigureAwait(false);
            return;
        }

        try
        {
            await StopAllCoreAsync(CancellationToken.None).ConfigureAwait(false);
            _disposeCompletion.TrySetResult();
        }
        catch (Exception exception)
        {
            _disposeCompletion.TrySetException(exception);
            throw;
        }
    }

    private async Task<IReadOnlyList<WorkerProcessResult>> StopAllCoreAsync(
        CancellationToken cancellationToken)
    {
        var handles = _workers.Values.ToArray();
        if (handles.Length == 0) return [];

        await Task.WhenAll(handles.Select(handle => RequestStopBestEffortAsync(handle, cancellationToken)))
            .ConfigureAwait(false);
        var results = await Task.WhenAll(handles.Select(handle => StopHandleAsync(handle, cancellationToken)))
            .ConfigureAwait(false);
        foreach (var handle in handles)
        {
            _workers.TryRemove(handle.Request.WorkerId, out _);
            handle.Process.Dispose();
        }
        return results;
    }

    private static async Task<WorkerReadyRecord> WaitForReadyAsync(
        WorkerHandle handle,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        ValidateTimeout(timeout, nameof(timeout));
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(timeout);
        while (true)
        {
            deadline.Token.ThrowIfCancellationRequested();
            if (handle.Process.HasExited)
            {
                await handle.ObserveOutputAsync().ConfigureAwait(false);
                throw new InvalidOperationException(
                    $"Worker '{handle.Request.WorkerId}' exited with code {handle.Process.ExitCode} before ready. " +
                    $"stderr: {handle.StandardError.Snapshot()}");
            }
            try
            {
                var ready = await WorkerFileProtocol.ReadJsonAsync<WorkerReadyRecord>(
                    handle.Paths.ReadyPath,
                    deadline.Token).ConfigureAwait(false);
                if (!ready.RunId.Equals(handle.Request.RunId, StringComparison.Ordinal) ||
                    !ready.WorkerId.Equals(handle.Request.WorkerId, StringComparison.OrdinalIgnoreCase) ||
                    ready.ProcessId != handle.ProcessId || ready.StartIndex != handle.Request.StartIndex ||
                    ready.Count != handle.Request.Count)
                    throw new InvalidDataException(
                        $"Worker '{handle.Request.WorkerId}' published a mismatched ready record.");
                ValidateReadyManifestPath(handle, ready);
                return ready;
            }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
            catch (JsonException) { }
            await Task.Delay(50, deadline.Token).ConfigureAwait(false);
        }
    }

    private static void ValidateReadyManifestPath(WorkerHandle handle, WorkerReadyRecord ready)
    {
        if (string.IsNullOrWhiteSpace(ready.EndpointManifestPath))
            throw new InvalidDataException(
                $"Worker '{handle.Request.WorkerId}' did not publish an endpoint manifest path.");
        var directory = Path.GetFullPath(handle.Paths.Directory);
        var manifest = Path.GetFullPath(ready.EndpointManifestPath);
        var prefix = directory.EndsWith(Path.DirectorySeparatorChar)
            ? directory
            : directory + Path.DirectorySeparatorChar;
        if (!manifest.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException(
                $"Worker '{handle.Request.WorkerId}' endpoint manifest escaped its control directory.");
        if (!File.Exists(manifest))
            throw new InvalidDataException(
                $"Worker '{handle.Request.WorkerId}' endpoint manifest was not found.");
    }

    private static async Task<WorkerProcessResult> StopHandleAsync(
        WorkerHandle handle,
        CancellationToken cancellationToken)
    {
        await handle.StopGate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        try
        {
            if (handle.FinalResult is not null) return handle.FinalResult;
            await RequestStopBestEffortAsync(handle, cancellationToken).ConfigureAwait(false);
            var killed = false;
            if (handle.IsRunning)
            {
                var graceful = await WaitForExitAsync(
                    handle.Process,
                    handle.Request.ShutdownTimeout ?? DefaultShutdownTimeout,
                    cancellationToken).ConfigureAwait(false);
                if (!graceful)
                {
                    killed = TryKillOwnProcessTree(handle.Process);
                    _ = await WaitForExitAsync(
                        handle.Process,
                        ForcedExitTimeout,
                        CancellationToken.None).ConfigureAwait(false);
                }
            }

            if (handle.IsRunning) handle.OutputCancellation.Cancel();
            await handle.ObserveOutputAsync().ConfigureAwait(false);
            var exitCode = TryGetExitCode(handle.Process);
            var resultPayload = await TryReadResultAsync(handle.Request.ResultPath).ConfigureAwait(false);
            WorkerFileProtocol.DeleteIfExists(handle.Paths.StopPath);
            handle.FinalResult = new WorkerProcessResult(
                handle.Request.RunId,
                handle.Request.WorkerId,
                handle.ProcessId,
                exitCode,
                killed,
                handle.StandardOutput.Snapshot(),
                handle.StandardError.Snapshot(),
                handle.Request.ResultPath,
                resultPayload);
            return handle.FinalResult;
        }
        finally
        {
            handle.OutputCancellation.Dispose();
            handle.StopGate.Release();
        }
    }

    private static async Task RequestStopBestEffortAsync(
        WorkerHandle handle,
        CancellationToken cancellationToken)
    {
        if (!handle.IsRunning) return;
        try
        {
            await WorkerFileProtocol.WriteJsonAtomicAsync(
                handle.Paths.StopPath,
                new WorkerStopRequest(
                    handle.Request.RunId,
                    handle.Request.WorkerId,
                    DateTimeOffset.UtcNow,
                    Environment.ProcessId),
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or OperationCanceledException)
        {
            // Failure to publish a graceful stop request is handled by the owned-process kill fallback.
        }
    }

    private static async Task<bool> WaitForExitAsync(
        Process process,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        if (HasExited(process)) return true;
        ValidateTimeout(timeout, nameof(timeout));
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(timeout);
        try
        {
            await process.WaitForExitAsync(deadline.Token).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException)
        {
            return HasExited(process);
        }
    }

    private static ProcessStartInfo CreateStartInfo(WorkerLaunchRequest request)
    {
        var programPath = Path.GetFullPath(request.ProgramPath);
        var isManagedDll = programPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase);
        var info = new ProcessStartInfo
        {
            FileName = isManagedDll ? ResolveDotnetHost() : programPath,
            WorkingDirectory = Path.GetFullPath(request.WorkingDirectory),
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = new UTF8Encoding(false),
            StandardErrorEncoding = new UTF8Encoding(false)
        };
        if (isManagedDll) info.ArgumentList.Add(programPath);
        foreach (var value in new[]
                 {
                     "worker",
                     "--run-id", request.RunId,
                     "--worker-id", request.WorkerId,
                     "--start-index", request.StartIndex.ToString(System.Globalization.CultureInfo.InvariantCulture),
                     "--count", request.Count.ToString(System.Globalization.CultureInfo.InvariantCulture),
                     "--control-directory", Path.GetFullPath(request.ControlDirectory),
                     "--output", Path.GetFullPath(request.ResultPath)
                 })
            info.ArgumentList.Add(value);
        if (request.AdditionalOptions is not null)
        {
            foreach (var option in request.AdditionalOptions.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
            {
                info.ArgumentList.Add(NormalizeOption(option.Key));
                info.ArgumentList.Add(option.Value);
            }
        }
        info.Environment["DREAMINE_FACTORY_PARENT_PID"] = Environment.ProcessId.ToString(
            System.Globalization.CultureInfo.InvariantCulture);
        info.Environment["DREAMINE_FACTORY_RUN_ID"] = request.RunId;
        return info;
    }

    private static async Task<JsonElement?> TryReadResultAsync(string resultPath)
    {
        try
        {
            await using var stream = new FileStream(
                resultPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read | FileShare.Delete,
                16 * 1024,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            using var document = await JsonDocument.ParseAsync(stream).ConfigureAwait(false);
            return document.RootElement.Clone();
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static bool TryKillOwnProcessTree(Process process)
    {
        try
        {
            if (process.HasExited) return false;
            process.Kill(entireProcessTree: true);
            return true;
        }
        catch (Exception exception) when (exception is InvalidOperationException or NotSupportedException or System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }

    private static bool HasExited(Process process)
    {
        try { return process.HasExited; }
        catch (InvalidOperationException) { return true; }
    }

    private static int TryGetExitCode(Process process)
    {
        try { return process.HasExited ? process.ExitCode : -1; }
        catch (InvalidOperationException) { return -1; }
    }

    private static string ResolveDotnetHost() =>
        Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") is { Length: > 0 } path
            ? path
            : "dotnet";

    private static void Validate(WorkerLaunchRequest request)
    {
        WorkerFileProtocol.ValidateIdentifier(request.RunId, nameof(request.RunId));
        WorkerFileProtocol.ValidateIdentifier(request.WorkerId, nameof(request.WorkerId));
        if (request.StartIndex <= 0) throw new ArgumentOutOfRangeException(nameof(request.StartIndex));
        if (request.Count is <= 0 or > 100) throw new ArgumentOutOfRangeException(nameof(request.Count));
        if (!File.Exists(request.ProgramPath))
            throw new FileNotFoundException("Worker program was not found.", request.ProgramPath);
        if (!Directory.Exists(request.WorkingDirectory))
            throw new DirectoryNotFoundException($"Worker directory was not found: {request.WorkingDirectory}");
        _ = WorkerFileProtocol.Paths(request.ControlDirectory, request.RunId, request.WorkerId);
        _ = Path.GetFullPath(request.ResultPath);
        if (request.ReadyTimeout is { } ready) ValidateTimeout(ready, nameof(request.ReadyTimeout));
        if (request.ShutdownTimeout is { } shutdown) ValidateTimeout(shutdown, nameof(request.ShutdownTimeout));

        if (request.AdditionalOptions is null) return;
        var reserved = new HashSet<string>(new[]
        {
            "--run-id", "--worker-id", "--start-index", "--count", "--control-directory", "--output"
        }, StringComparer.OrdinalIgnoreCase);
        foreach (var option in request.AdditionalOptions)
        {
            var name = NormalizeOption(option.Key);
            if (reserved.Contains(name))
                throw new ArgumentException($"Additional option '{name}' is reserved.", nameof(request));
            if (string.IsNullOrWhiteSpace(option.Value))
                throw new ArgumentException($"Additional option '{name}' has an empty value.", nameof(request));
        }
    }

    private static string NormalizeOption(string name)
    {
        var normalized = name.StartsWith("--", StringComparison.Ordinal) ? name : "--" + name;
        if (normalized.Length <= 2 || normalized.Any(character =>
                !char.IsAsciiLetterOrDigit(character) && character != '-'))
            throw new ArgumentException($"Invalid worker option name '{name}'.", nameof(name));
        return normalized;
    }

    private static void ValidateTimeout(TimeSpan value, string name)
    {
        if (value <= TimeSpan.Zero || value > TimeSpan.FromMinutes(10))
            throw new ArgumentOutOfRangeException(name, value, "Timeout must be greater than zero and no more than ten minutes.");
    }

    private void ThrowIfDisposed() =>
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);

    private sealed class WorkerHandle(
        WorkerLaunchRequest request,
        WorkerControlPaths paths,
        Process process)
    {
        internal WorkerLaunchRequest Request { get; } = request;
        internal WorkerControlPaths Paths { get; } = paths;
        internal Process Process { get; } = process;
        internal OutputTail StandardOutput { get; } = new();
        internal OutputTail StandardError { get; } = new();
        internal CancellationTokenSource OutputCancellation { get; } = new();
        internal SemaphoreSlim StopGate { get; } = new(1, 1);
        internal Task StandardOutputTask { get; private set; } = Task.CompletedTask;
        internal Task StandardErrorTask { get; private set; } = Task.CompletedTask;
        internal WorkerReadyRecord? Ready { get; set; }
        internal WorkerProcessResult? FinalResult { get; set; }
        internal int ProcessId { get; set; } = -1;
        internal bool IsRunning => !HasExited(Process);

        internal void StartOutputDrain()
        {
            StandardOutputTask = DrainAsync(Process.StandardOutput, StandardOutput, OutputCancellation.Token);
            StandardErrorTask = DrainAsync(Process.StandardError, StandardError, OutputCancellation.Token);
        }

        internal async Task ObserveOutputAsync()
        {
            try { await Task.WhenAll(StandardOutputTask, StandardErrorTask).ConfigureAwait(false); }
            catch (OperationCanceledException) when (OutputCancellation.IsCancellationRequested) { }
            catch (IOException) { }
        }

        private static async Task DrainAsync(
            StreamReader reader,
            OutputTail tail,
            CancellationToken cancellationToken)
        {
            try
            {
                while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
                    tail.Append(line);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
            catch (IOException) { }
        }
    }

    private sealed class OutputTail
    {
        private const int MaximumLines = 256;
        private const int MaximumCharacters = 64 * 1024;
        private readonly Queue<string> _lines = new();
        private readonly object _gate = new();
        private int _characters;

        internal void Append(string line)
        {
            var bounded = line.Length <= 4_096 ? line : line[..4_096] + "…";
            lock (_gate)
            {
                _lines.Enqueue(bounded);
                _characters += bounded.Length + Environment.NewLine.Length;
                while (_lines.Count > MaximumLines || _characters > MaximumCharacters)
                {
                    var removed = _lines.Dequeue();
                    _characters -= removed.Length + Environment.NewLine.Length;
                }
            }
        }

        internal string Snapshot()
        {
            lock (_gate) return string.Join(Environment.NewLine, _lines);
        }
    }
}

internal sealed record WorkerControlPaths(
    string Directory,
    string ReadyPath,
    string HeartbeatPath,
    string StopPath);

internal static class WorkerFileProtocol
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    internal static WorkerControlPaths Paths(string directory, string runId, string workerId)
    {
        ValidateIdentifier(runId, nameof(runId));
        ValidateIdentifier(workerId, nameof(workerId));
        var fullDirectory = Path.GetFullPath(directory);
        var prefix = $"{runId}.{workerId}";
        return new WorkerControlPaths(
            fullDirectory,
            ChildPath(fullDirectory, prefix + ".ready.json"),
            ChildPath(fullDirectory, prefix + ".heartbeat.json"),
            ChildPath(fullDirectory, prefix + ".stop.json"));
    }

    internal static Task PublishReadyAsync(
        WorkerControlPaths paths,
        WorkerReadyRecord ready,
        CancellationToken cancellationToken) =>
        WriteJsonAtomicAsync(paths.ReadyPath, ready, cancellationToken);

    internal static Task PublishHeartbeatAsync(
        WorkerControlPaths paths,
        WorkerHeartbeatRecord heartbeat,
        CancellationToken cancellationToken) =>
        WriteJsonAtomicAsync(paths.HeartbeatPath, heartbeat, cancellationToken);

    internal static async Task WaitForStopRequestAsync(
        WorkerControlPaths paths,
        string runId,
        string workerId,
        TimeSpan pollInterval,
        CancellationToken cancellationToken)
    {
        if (pollInterval <= TimeSpan.Zero || pollInterval > TimeSpan.FromSeconds(5))
            throw new ArgumentOutOfRangeException(nameof(pollInterval));
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var request = await ReadJsonAsync<WorkerStopRequest>(paths.StopPath, cancellationToken)
                    .ConfigureAwait(false);
                if (request.RunId.Equals(runId, StringComparison.Ordinal) &&
                    request.WorkerId.Equals(workerId, StringComparison.OrdinalIgnoreCase))
                    return;
            }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
            catch (JsonException) { }
            await Task.Delay(pollInterval, cancellationToken).ConfigureAwait(false);
        }
    }

    internal static async Task<T> ReadJsonAsync<T>(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read | FileShare.Delete,
            16 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken).ConfigureAwait(false)
               ?? throw new JsonException($"Control file '{Path.GetFileName(path)}' was empty.");
    }

    internal static async Task WriteJsonAtomicAsync<T>(
        string path,
        T value,
        CancellationToken cancellationToken)
    {
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath)
                        ?? throw new InvalidOperationException("Control file has no parent directory.");
        Directory.CreateDirectory(directory);
        var temporaryPath = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            await using (var stream = new FileStream(
                             temporaryPath,
                             FileMode.CreateNew,
                             FileAccess.Write,
                             FileShare.None,
                             16 * 1024,
                             FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await JsonSerializer.SerializeAsync(stream, value, JsonOptions, cancellationToken)
                    .ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        finally
        {
            DeleteIfExists(temporaryPath);
        }
    }

    internal static void DeleteIfExists(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    internal static void DeleteForNewRun(string path)
    {
        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath)) return;
        File.Delete(fullPath);
        if (File.Exists(fullPath))
            throw new IOException($"Stale worker file could not be removed: {Path.GetFileName(fullPath)}");
    }

    internal static void ValidateIdentifier(string value, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, name);
        if (value.Length > 64 || value.Any(character =>
                !char.IsAsciiLetterOrDigit(character) && character is not '-' and not '_' and not '.'))
            throw new ArgumentException(
                "Identifier must contain at most 64 ASCII letters, digits, '.', '-' or '_'.",
                name);
    }

    private static string ChildPath(string directory, string name)
    {
        var candidate = Path.GetFullPath(Path.Combine(directory, name));
        var prefix = directory.EndsWith(Path.DirectorySeparatorChar)
            ? directory
            : directory + Path.DirectorySeparatorChar;
        if (!candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Worker control path escaped its run directory.");
        return candidate;
    }
}

internal sealed record HostProcessLaunchRequest(
    string RunId,
    string ProgramPath,
    string WorkingDirectory,
    string ManifestPath,
    string ResultPath,
    int EquipmentCount,
    IReadOnlyDictionary<string, string>? AdditionalOptions = null,
    TimeSpan? CompletionTimeout = null);

internal sealed record HostProcessResult(
    string RunId,
    int ProcessId,
    int ExitCode,
    bool TimedOut,
    bool KilledAfterTimeout,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    string StandardOutputTail,
    string StandardErrorTail,
    string ResultPath,
    JsonElement? Result);

/// <summary>
/// Runs one bounded Host-only child. The command owns its natural shutdown;
/// timeout/cancellation paths drain both redirected streams before killing only
/// the process tree started by this coordinator.
/// </summary>
internal sealed class HostProcessCoordinator
{
    private static readonly TimeSpan DefaultCompletionTimeout = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ForcedExitTimeout = TimeSpan.FromSeconds(5);

    internal async Task<HostProcessResult> RunAsync(
        HostProcessLaunchRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validate(request);
        WorkerFileProtocol.DeleteForNewRun(request.ResultPath);

        using var process = new Process
        {
            StartInfo = CreateStartInfo(request),
            EnableRaisingEvents = true
        };
        var standardOutput = new HostOutputTail();
        var standardError = new HostOutputTail();
        var startedAt = DateTimeOffset.UtcNow;
        if (!process.Start()) throw new InvalidOperationException("The Host child did not start.");
        var processId = process.Id;
        using var outputCancellation = new CancellationTokenSource();
        var outputTask = DrainAsync(process.StandardOutput, standardOutput, outputCancellation.Token);
        var errorTask = DrainAsync(process.StandardError, standardError, outputCancellation.Token);
        var timedOut = false;
        var killed = false;
        try
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(request.CompletionTimeout ?? DefaultCompletionTimeout);
            try
            {
                await process.WaitForExitAsync(deadline.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                timedOut = !cancellationToken.IsCancellationRequested;
                killed = TryKillOwnProcessTree(process);
                using var forcedDeadline = new CancellationTokenSource(ForcedExitTimeout);
                try { await process.WaitForExitAsync(forcedDeadline.Token).ConfigureAwait(false); }
                catch (OperationCanceledException) { }
            }

            if (!process.HasExited) outputCancellation.Cancel();
            await ObserveDrainAsync(outputTask, errorTask, outputCancellation.Token).ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException(cancellationToken);
            var payload = await TryReadResultAsync(request.ResultPath).ConfigureAwait(false);
            return new HostProcessResult(
                request.RunId,
                processId,
                process.HasExited ? process.ExitCode : -1,
                timedOut,
                killed,
                startedAt,
                DateTimeOffset.UtcNow,
                standardOutput.Snapshot(),
                standardError.Snapshot(),
                request.ResultPath,
                payload);
        }
        finally
        {
            if (!process.HasExited)
            {
                _ = TryKillOwnProcessTree(process);
                using var forcedDeadline = new CancellationTokenSource(ForcedExitTimeout);
                try { await process.WaitForExitAsync(forcedDeadline.Token).ConfigureAwait(false); }
                catch (OperationCanceledException) { }
            }
            if (!process.HasExited) outputCancellation.Cancel();
            await ObserveDrainAsync(outputTask, errorTask, outputCancellation.Token).ConfigureAwait(false);
        }
    }

    private static ProcessStartInfo CreateStartInfo(HostProcessLaunchRequest request)
    {
        var programPath = Path.GetFullPath(request.ProgramPath);
        var managedDll = programPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase);
        var info = new ProcessStartInfo
        {
            FileName = managedDll ? ResolveDotnetHost() : programPath,
            WorkingDirectory = Path.GetFullPath(request.WorkingDirectory),
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = new UTF8Encoding(false),
            StandardErrorEncoding = new UTF8Encoding(false)
        };
        if (managedDll) info.ArgumentList.Add(programPath);
        foreach (var argument in new[]
                 {
                     "host",
                     "--run-id", request.RunId,
                     "--equipment-count", request.EquipmentCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                     "--manifest", Path.GetFullPath(request.ManifestPath),
                     "--output", Path.GetFullPath(request.ResultPath)
                 })
            info.ArgumentList.Add(argument);
        if (request.AdditionalOptions is not null)
        {
            foreach (var option in request.AdditionalOptions.OrderBy(value => value.Key, StringComparer.OrdinalIgnoreCase))
            {
                info.ArgumentList.Add(NormalizeOption(option.Key));
                info.ArgumentList.Add(option.Value);
            }
        }
        info.Environment["DREAMINE_FACTORY_PARENT_PID"] = Environment.ProcessId.ToString(
            System.Globalization.CultureInfo.InvariantCulture);
        info.Environment["DREAMINE_FACTORY_RUN_ID"] = request.RunId;
        return info;
    }

    private static void Validate(HostProcessLaunchRequest request)
    {
        WorkerFileProtocol.ValidateIdentifier(request.RunId, nameof(request.RunId));
        if (request.EquipmentCount is < 1 or > 1_000)
            throw new ArgumentOutOfRangeException(nameof(request.EquipmentCount));
        if (!File.Exists(request.ProgramPath))
            throw new FileNotFoundException("Host program was not found.", request.ProgramPath);
        if (!Directory.Exists(request.WorkingDirectory))
            throw new DirectoryNotFoundException($"Host working directory was not found: {request.WorkingDirectory}");
        if (!File.Exists(request.ManifestPath))
            throw new FileNotFoundException("Host endpoint manifest was not found.", request.ManifestPath);
        _ = Path.GetFullPath(request.ResultPath);
        if (request.CompletionTimeout is { } timeout &&
            (timeout <= TimeSpan.Zero || timeout > TimeSpan.FromDays(7)))
            throw new ArgumentOutOfRangeException(nameof(request.CompletionTimeout));

        if (request.AdditionalOptions is null) return;
        var reserved = new HashSet<string>(new[]
        {
            "--run-id", "--equipment-count", "--manifest", "--output"
        }, StringComparer.OrdinalIgnoreCase);
        foreach (var option in request.AdditionalOptions)
        {
            var name = NormalizeOption(option.Key);
            if (reserved.Contains(name))
                throw new ArgumentException($"Additional Host option '{name}' is reserved.", nameof(request));
            if (string.IsNullOrWhiteSpace(option.Value))
                throw new ArgumentException($"Additional Host option '{name}' has an empty value.", nameof(request));
        }
    }

    private static string NormalizeOption(string name)
    {
        var normalized = name.StartsWith("--", StringComparison.Ordinal) ? name : "--" + name;
        if (normalized.Length <= 2 || normalized.Any(character =>
                !char.IsAsciiLetterOrDigit(character) && character != '-'))
            throw new ArgumentException($"Invalid Host option name '{name}'.", nameof(name));
        return normalized;
    }

    private static string ResolveDotnetHost() =>
        Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") is { Length: > 0 } path ? path : "dotnet";

    private static bool TryKillOwnProcessTree(Process process)
    {
        try
        {
            if (process.HasExited) return false;
            process.Kill(entireProcessTree: true);
            return true;
        }
        catch (Exception exception) when (exception is InvalidOperationException or NotSupportedException or
                                          System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }

    private static async Task<JsonElement?> TryReadResultAsync(string path)
    {
        try
        {
            await using var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read | FileShare.Delete,
                16 * 1024,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            using var document = await JsonDocument.ParseAsync(stream).ConfigureAwait(false);
            return document.RootElement.Clone();
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static async Task DrainAsync(
        StreamReader reader,
        HostOutputTail tail,
        CancellationToken cancellationToken)
    {
        try
        {
            while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
                tail.Append(line);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (IOException) { }
    }

    private static async Task ObserveDrainAsync(
        Task output,
        Task error,
        CancellationToken cancellationToken)
    {
        try { await Task.WhenAll(output, error).ConfigureAwait(false); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (IOException) { }
    }

    private sealed class HostOutputTail
    {
        private const int MaximumLines = 256;
        private const int MaximumCharacters = 64 * 1024;
        private readonly Queue<string> _lines = new();
        private readonly object _gate = new();
        private int _characters;

        internal void Append(string line)
        {
            var bounded = line.Length <= 4_096 ? line : line[..4_096] + "…";
            lock (_gate)
            {
                _lines.Enqueue(bounded);
                _characters += bounded.Length + Environment.NewLine.Length;
                while (_lines.Count > MaximumLines || _characters > MaximumCharacters)
                {
                    var removed = _lines.Dequeue();
                    _characters -= removed.Length + Environment.NewLine.Length;
                }
            }
        }

        internal string Snapshot()
        {
            lock (_gate) return string.Join(Environment.NewLine, _lines);
        }
    }
}
