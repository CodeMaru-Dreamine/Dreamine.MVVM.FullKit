using Dreamine.Gem;
using Dreamine.Gem.Abstractions.Interfaces;
using Dreamine.Gem.Abstractions.Model;
using Dreamine.Communication.Abstractions.Enums;
using Dreamine.MVVM.ViewModels;
using Dreamine.Secs.Abstractions.Enums;
using Dreamine.Secs.Abstractions.Diagnostics;
using Dreamine.Secs.Abstractions.Hsms;
using Dreamine.Secs.Abstractions.Interfaces;
using Dreamine.Secs.Abstractions.Model;
using Dreamine.Secs.Abstractions.Validation;
using Dreamine.Secs.Com.Hsms;
using Dreamine.SecsGem.Interop.Wpf.Models;

namespace Dreamine.SecsGem.Interop.Wpf.Managers;

internal sealed class EquipmentConnectionContext : ViewModelBase, IAsyncDisposable
{
    private readonly InteropLogManager _log;
    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
    private readonly object _connectGate = new();
    private readonly object _selectionRecoveryGate = new();
    private readonly CancellationTokenSource _lifetimeCancellation = new();
    private readonly TaskCompletionSource _disposeCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static int _liveSessions;
    private static int _liveSelectionRecoveries;
    private HsmsSession? _session;
    private GemRuntime? _gemRuntime;
    private CancellationTokenSource? _connectCancellation;
    private string _connectionId = "--";
    private string _tcpState = "Disconnected";
    private string _hsmsState = "NotConnected";
    private string _gemState = "Disabled";
    private DateTimeOffset? _lastActivity;
    private string _lastError = "None";
    private int _disposed;
    private int _initialConnectInProgress;
    private int _selectionRecoveryInProgress;
    private int _establishedConnections;
    private int _awaitingReconnectAttempt;
    private int _disconnecting;
    private Task _selectionRecoveryTask = Task.CompletedTask;
    private CancellationTokenSource? _selectionRecoveryCancellation;

    public EquipmentConnectionContext(EquipmentConnectionDefinition definition, InteropLogManager log)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        Definition.Validate();
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    internal EquipmentConnectionDefinition Definition { get; }
    internal static int LiveSessionCount => Volatile.Read(ref _liveSessions);
    internal static int LiveBackgroundOperationCount => Volatile.Read(ref _liveSelectionRecoveries);
    internal HsmsSession? Session => Volatile.Read(ref _session);
    internal GemRuntime? GemRuntime => Volatile.Read(ref _gemRuntime);
    public string EquipmentId => Definition.EquipmentId;
    public string Host => Definition.Host;
    public int Port => Definition.Port;
    public string Endpoint => Definition.Endpoint;
    public SecsConnectionMode Mode => Definition.Mode;
    public ushort SessionId => Definition.SessionId;
    public string ConnectionId { get => _connectionId; private set => SetProperty(ref _connectionId, value); }
    public string TcpState { get => _tcpState; private set => SetProperty(ref _tcpState, value); }
    public string HsmsState { get => _hsmsState; private set => SetProperty(ref _hsmsState, value); }
    public string GemState { get => _gemState; private set => SetProperty(ref _gemState, value); }
    public string ResponderState => "N/A (Host)";
    public DateTimeOffset? LastActivity { get => _lastActivity; private set => SetProperty(ref _lastActivity, value); }
    public string LastError { get => _lastError; private set => SetProperty(ref _lastError, value); }
    public bool IsConnected => Session is not null && TcpState is "Connected";
    public bool IsSelected => Session?.HsmsState == HsmsConnectionState.Selected;

    internal EquipmentLogIdentity LogIdentity => new(EquipmentId, ConnectionId, Endpoint, SessionId);

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        var operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetimeCancellation.Token);
        lock (_connectGate)
        {
            if (_connectCancellation is not null)
            {
                operationCancellation.Dispose();
                throw new InvalidOperationException($"{EquipmentId} already has a connection attempt in progress.");
            }
            _connectCancellation = operationCancellation;
        }
        var gateHeld = false;
        try
        {
            await _lifecycleGate.WaitAsync(operationCancellation.Token).ConfigureAwait(false);
            gateHeld = true;
            if (_session is { State: ConnectionState.Connected } connectedSession)
            {
                if (connectedSession.HsmsState == HsmsConnectionState.Selected) return;
                if (Definition.Mode != SecsConnectionMode.Active)
                    throw new InvalidOperationException($"{EquipmentId} is connected but not selected.");
                await connectedSession.SelectAsync(operationCancellation.Token).ConfigureAwait(false);
                RefreshStates();
                return;
            }
            if (_session is { State: ConnectionState.Connecting or ConnectionState.Listening })
                throw new InvalidOperationException($"{EquipmentId} already has a connection or reconnect attempt in progress.");
            if (_session is { } staleSession)
            {
                staleSession.MessageReceived -= OnMessageReceived;
                await DisposeSessionAsync(staleSession).ConfigureAwait(false);
                _session = null;
                _gemRuntime = null;
            }
            Definition.Validate();
            ConnectionId = Guid.NewGuid().ToString("N");
            Interlocked.Exchange(ref _establishedConnections, 0);
            Interlocked.Exchange(ref _awaitingReconnectAttempt, 0);
            LastError = "None";
            Touch();

            HsmsSession? session = null;
            Interlocked.Exchange(ref _initialConnectInProgress, 1);
            try
            {
                HsmsSession? created = null;
                created = new HsmsSession(ToOptions(), diagnostics: new EquipmentInteropDiagnosticSink(
                    _log, () => LogIdentity, diagnostic => OnDiagnostic(created, diagnostic)));
                session = created;
                Interlocked.Increment(ref _liveSessions);
                session.MessageReceived += OnMessageReceived;
                _session = session;
                _gemRuntime = CreateGemRuntime(session);
                RefreshStates();
                _log.Info("MultiEquipment", "Connection attempt started.", LogIdentity);

                await session.ConnectAsync(operationCancellation.Token).ConfigureAwait(false);
                if (Definition.Mode == SecsConnectionMode.Active)
                    await session.SelectAsync(operationCancellation.Token).ConfigureAwait(false);
                RefreshStates();
                _log.Info("MultiEquipment", "Connection established.", LogIdentity);
            }
            catch (Exception exception)
            {
                LastError = exception.Message;
                _log.Error("MultiEquipment", exception, LogIdentity);
                if (session is not null && Definition.AutoReconnect && exception is not OperationCanceledException)
                {
                    Interlocked.Exchange(ref _gemRuntime, null);
                    RefreshStates();
                    throw;
                }
                if (session is not null)
                {
                    session.MessageReceived -= OnMessageReceived;
                    await DisposeSessionAsync(session).ConfigureAwait(false);
                }
                _session = null;
                _gemRuntime = null;
                RefreshStates();
                throw;
            }
            finally
            {
                Interlocked.Exchange(ref _initialConnectInProgress, 0);
            }
        }
        finally
        {
            if (gateHeld) _lifecycleGate.Release();
            lock (_connectGate)
            {
                if (ReferenceEquals(_connectCancellation, operationCancellation)) _connectCancellation = null;
            }
            operationCancellation.Dispose();
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _disconnecting);
        try
        {
            lock (_connectGate) _connectCancellation?.Cancel();
            Task recovery;
            lock (_selectionRecoveryGate)
            {
                _selectionRecoveryCancellation?.Cancel();
                recovery = _selectionRecoveryTask;
            }
            try { await recovery.WaitAsync(cancellationToken).ConfigureAwait(false); }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception) { }
            await _lifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var session = Interlocked.Exchange(ref _session, null);
                Interlocked.Exchange(ref _gemRuntime, null);
                if (session is null)
                {
                    RefreshStates();
                    return;
                }

                session.MessageReceived -= OnMessageReceived;
                await DisposeSessionAsync(session).ConfigureAwait(false);
                Touch();
                RefreshStates();
                _log.Info("MultiEquipment", "Connection disposed.", LogIdentity);
            }
            finally { _lifecycleGate.Release(); }
        }
        finally { Interlocked.Decrement(ref _disconnecting); }
    }

    public async Task ReconnectAsync(CancellationToken cancellationToken)
    {
        await DisconnectAsync(cancellationToken).ConfigureAwait(false);
        await ConnectAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task LinktestAsync(CancellationToken cancellationToken)
    {
        var session = RequireSelectedSession();
        await session.LinktestAsync(cancellationToken).ConfigureAwait(false);
        Touch();
        _log.Info("MultiEquipment", "Linktest completed.", LogIdentity);
    }

    public async Task<SecsMessage> SendPrimaryAsync(byte stream, byte function, SecsItem? item,
        CancellationToken cancellationToken, SecsSystemBytes? systemBytes = null)
    {
        var session = RequireSelectedSession();
        var request = new SecsMessage(new SecsSessionId(SessionId), new SecsStream(stream), new SecsFunction(function),
            true, systemBytes ?? session.AllocateSystemBytes(), item);
        _log.Message("TX", request, LogIdentity);
        Touch();
        try
        {
            var response = await session.SendPrimaryAsync(request, cancellationToken).ConfigureAwait(false);
            _log.Message("RX", response, LogIdentity);
            Touch();
            if (response.Stream.Value == 1 && response.Function.Value == 14 && IsAcceptedS1F14(response.Item))
                MarkGemCommunicating();
            LastError = "None";
            return response;
        }
        catch (Exception exception)
        {
            LastError = exception is SecsTransactionTimeoutException ? $"T3: {exception.Message}" : exception.Message;
            _log.Error("MultiEquipment", exception, LogIdentity);
            throw;
        }
    }

    public async Task SendAsync(byte stream, byte function, SecsItem? item, CancellationToken cancellationToken)
    {
        var session = RequireSelectedSession();
        var message = new SecsMessage(new SecsSessionId(SessionId), new SecsStream(stream), new SecsFunction(function),
            false, session.AllocateSystemBytes(), item);
        _log.Message("TX", message, LogIdentity);
        await session.SendAsync(message, cancellationToken).ConfigureAwait(false);
        Touch();
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            await _disposeCompletion.Task.ConfigureAwait(false);
            return;
        }
        try
        {
            _lifetimeCancellation.Cancel();
            await DisconnectAsync(CancellationToken.None).ConfigureAwait(false);
            Task recovery;
            lock (_selectionRecoveryGate) recovery = _selectionRecoveryTask;
            try { await recovery.ConfigureAwait(false); } catch (Exception) { }
            lock (_selectionRecoveryGate)
            {
                _selectionRecoveryCancellation?.Dispose();
                _selectionRecoveryCancellation = null;
            }
            _disposeCompletion.TrySetResult();
        }
        catch (Exception exception)
        {
            _disposeCompletion.TrySetException(exception);
            throw;
        }
        finally
        {
            _lifetimeCancellation.Dispose();
            _lifecycleGate.Dispose();
        }
    }

    private HsmsSession RequireSelectedSession()
    {
        ThrowIfDisposed();
        var session = Session ?? throw new InvalidOperationException($"{EquipmentId} is not connected.");
        if (session.HsmsState != HsmsConnectionState.Selected)
            throw new InvalidOperationException($"{EquipmentId} is not selected.");
        return session;
    }

    private HsmsSessionOptions ToOptions() => new()
    {
        Host = Definition.Host,
        Port = Definition.Port,
        Mode = Definition.Mode,
        Role = SecsRole.Host,
        SessionId = new SecsSessionId(Definition.SessionId),
        AutoReconnect = Definition.AutoReconnect,
        Timers = new HsmsTimerOptions
        {
            T3 = TimeSpan.FromSeconds(Definition.T3Seconds),
            T5 = TimeSpan.FromSeconds(Definition.T5Seconds),
            T6 = TimeSpan.FromSeconds(Definition.T6Seconds),
            T7 = TimeSpan.FromSeconds(Definition.T7Seconds),
            T8 = TimeSpan.FromSeconds(Definition.T8Seconds)
        }
    };

    private void OnMessageReceived(object? sender, SecsMessage message)
    {
        _log.Message("RX", message, LogIdentity);
        Touch();
    }

    private void OnDiagnostic(HsmsSession? session, SecsDiagnosticEvent diagnostic)
    {
        if (session is null || !ReferenceEquals(Session, session)) return;
        if (diagnostic.Kind == SecsDiagnosticKind.ConnectionClosed)
        {
            Interlocked.Exchange(ref _gemRuntime, null);
            Interlocked.Exchange(ref _awaitingReconnectAttempt, 1);
            GemState = "Disabled";
            if (Volatile.Read(ref _disconnecting) == 0 && Volatile.Read(ref _disposed) == 0)
                LastError = diagnostic.Message;
        }
        else if (diagnostic.Kind == SecsDiagnosticKind.ConnectionAttempt &&
            Interlocked.Exchange(ref _awaitingReconnectAttempt, 0) != 0)
        {
            ConnectionId = Guid.NewGuid().ToString("N");
        }
        else if (diagnostic.Kind == SecsDiagnosticKind.ConnectionEstablished)
        {
            Interlocked.Increment(ref _establishedConnections);
            _gemRuntime = CreateGemRuntime(session);
            LastError = "None";
            if (Definition.AutoReconnect && Definition.Mode == SecsConnectionMode.Active &&
                Volatile.Read(ref _initialConnectInProgress) == 0 && Volatile.Read(ref _disconnecting) == 0)
                ScheduleSelectionRecovery(session);
        }
        if (diagnostic.Kind is SecsDiagnosticKind.Timeout or SecsDiagnosticKind.ProtocolError or SecsDiagnosticKind.ApplicationError)
            LastError = diagnostic.Message;
        RefreshStates();
    }

    private void ScheduleSelectionRecovery(HsmsSession session)
    {
        lock (_selectionRecoveryGate)
        {
            if (!_selectionRecoveryTask.IsCompleted) return;
            _selectionRecoveryCancellation?.Dispose();
            _selectionRecoveryCancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeCancellation.Token);
            _selectionRecoveryTask = RestoreSelectionAsync(session, _selectionRecoveryCancellation.Token);
        }
    }

    private async Task RestoreSelectionAsync(HsmsSession session, CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _selectionRecoveryInProgress, 1) != 0) return;
        Interlocked.Increment(ref _liveSelectionRecoveries);
        try
        {
            await Task.Yield();
            if (!ReferenceEquals(Session, session) || session.HsmsState != HsmsConnectionState.ConnectedNotSelected) return;
            await session.SelectAsync(cancellationToken).ConfigureAwait(false);
            RefreshStates();
            _log.Info("MultiEquipment", "Auto-reconnect restored HSMS selection with fresh connection state.", LogIdentity);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception exception)
        {
            LastError = exception.Message;
            _log.Error("MultiEquipment", exception, LogIdentity);
        }
        finally
        {
            Interlocked.Decrement(ref _liveSelectionRecoveries);
            Interlocked.Exchange(ref _selectionRecoveryInProgress, 0);
        }
    }

    private void MarkGemCommunicating()
    {
        var communication = GemRuntime?.Communication;
        if (communication is null) return;
        try { communication.Accept(); }
        catch (InvalidOperationException) { }
        GemState = communication.State.ToString();
    }

    private GemRuntime CreateGemRuntime(HsmsSession session)
    {
        var runtime = new GemRuntime(new ContextGemTransport(session, this), new GemEquipmentIdentity(EquipmentId, "HARNESS"));
        runtime.Communication.Enable(equipmentInitiated: false);
        return runtime;
    }

    private static bool IsAcceptedS1F14(SecsItem? item) =>
        item is SecsListItem { Count: > 0 } list && list.Items[0] is SecsBinaryItem ack &&
        ack.Values.Length > 0 && ack.Values.Span[0] == 0;

    private void RefreshStates()
    {
        var session = Session;
        TcpState = session?.State.ToString() ?? "Disconnected";
        HsmsState = session?.HsmsState.ToString() ?? "NotConnected";
        GemState = GemRuntime?.Communication.State.ToString() ?? "Disabled";
        OnPropertyChanged(nameof(IsConnected));
        OnPropertyChanged(nameof(IsSelected));
        Touch();
    }

    private void Touch() => LastActivity = DateTimeOffset.Now;
    private static async Task DisposeSessionAsync(HsmsSession session)
    {
        await session.DisposeAsync().ConfigureAwait(false);
        Interlocked.Decrement(ref _liveSessions);
    }
    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);

    private sealed class ContextGemTransport(HsmsSession session, EquipmentConnectionContext owner) : IGemMessageTransport
    {
        public ISecsConnection Connection => session;
        public SecsSessionId SessionId => new(owner.SessionId);
        public event EventHandler<SecsMessage>? MessageReceived
        {
            add => session.MessageReceived += value;
            remove => session.MessageReceived -= value;
        }
        public Task SendAsync(SecsMessage message, CancellationToken cancellationToken = default)
        {
            owner._log.Message("TX", message, owner.LogIdentity);
            return session.SendAsync(message, cancellationToken);
        }
        public Task<SecsMessage> RequestAsync(SecsMessage message, CancellationToken cancellationToken = default)
        {
            owner._log.Message("TX", message, owner.LogIdentity);
            return session.SendPrimaryAsync(message, cancellationToken);
        }
        public SecsSystemBytes AllocateSystemBytes() => session.AllocateSystemBytes();
    }
}
