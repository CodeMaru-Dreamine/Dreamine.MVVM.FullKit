using System.ComponentModel;
using System.Runtime.CompilerServices;
using Dreamine.Communication.Abstractions.Enums;
using Dreamine.Gem;
using Dreamine.Gem.Abstractions.Interfaces;
using Dreamine.Gem.Abstractions.Model;
using Dreamine.Secs.Abstractions.Diagnostics;
using Dreamine.Secs.Abstractions.Enums;
using Dreamine.Secs.Abstractions.Hsms;
using Dreamine.Secs.Abstractions.Interfaces;
using Dreamine.Secs.Abstractions.Model;
using Dreamine.Secs.Abstractions.Validation;
using Dreamine.Secs.Com.Hsms;

namespace Dreamine.SecsGem.Interop.Runtime;

internal sealed class EquipmentConnectionContext : INotifyPropertyChanged, IAsyncDisposable
{
    private readonly IEquipmentEventSink _events;
    private readonly IReconnectScheduler _reconnectScheduler;
    private readonly SynchronizationContext? _notificationContext;
    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
    private readonly object _connectGate = new();
    private readonly object _stateGate = new();
    private readonly CancellationTokenSource _lifetimeCancellation = new();
    private readonly TaskCompletionSource _disposeCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static int _liveSessions;
    private static int _liveReconnectOperations;
    private HsmsSession? _session;
    private GemRuntime? _gemRuntime;
    private CancellationTokenSource? _connectCancellation;
    private string _connectionId = "--";
    private string _tcpState = "Disconnected";
    private string _hsmsState = "NotConnected";
    private string _gemState = "Disabled";
    private DateTimeOffset? _lastActivity;
    private string _lastError = "None";
    private int _pendingTransactions;
    private int _activeReconnectOperations;
    private long _reconnectAttempts;
    private int _desiredConnected;
    private int _disposed;
    private int _disconnecting;

    internal EquipmentConnectionContext(EquipmentConnectionDefinition definition, IEquipmentEventSink? events = null,
        IReconnectScheduler? reconnectScheduler = null, SynchronizationContext? notificationContext = null)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        Definition.Validate();
        _events = new SafeEquipmentEventSink(events);
        _reconnectScheduler = reconnectScheduler ?? NullReconnectScheduler.Instance;
        _notificationContext = notificationContext;
    }

    internal EquipmentConnectionDefinition Definition { get; }
    internal static int LiveSessionCount => Volatile.Read(ref _liveSessions);
    internal static int LiveReconnectOperationCount => Volatile.Read(ref _liveReconnectOperations);
    internal static int LiveBackgroundOperationCount => LiveReconnectOperationCount;
    internal HsmsSession? Session => Volatile.Read(ref _session);
    internal GemRuntime? GemRuntime => Volatile.Read(ref _gemRuntime);
    internal bool ShouldAutoReconnect => Definition.AutoReconnect && Volatile.Read(ref _desiredConnected) != 0 &&
        Volatile.Read(ref _disposed) == 0 && Volatile.Read(ref _disconnecting) == 0 && !IsConnected;

    public string EquipmentId => Definition.EquipmentId;
    public string Host => Definition.Host;
    public int Port => Definition.Port;
    public string Endpoint => Definition.Endpoint;
    public SecsConnectionMode Mode => Definition.Mode;
    public ushort SessionId => Definition.SessionId;
    public string ConnectionId { get { lock (_stateGate) return _connectionId; } private set => SetState(ref _connectionId, value); }
    public string TcpState { get { lock (_stateGate) return _tcpState; } private set => SetState(ref _tcpState, value); }
    public string HsmsState { get { lock (_stateGate) return _hsmsState; } private set => SetState(ref _hsmsState, value); }
    public string GemState { get { lock (_stateGate) return _gemState; } private set => SetState(ref _gemState, value); }
    public string ResponderState => "N/A (Host)";
    public DateTimeOffset? LastActivity { get { lock (_stateGate) return _lastActivity; } private set => SetState(ref _lastActivity, value); }
    public string LastError { get { lock (_stateGate) return _lastError; } private set => SetState(ref _lastError, value); }
    public int PendingTransactionCount => Volatile.Read(ref _pendingTransactions);
    public int ActiveReconnectOperationCount => Volatile.Read(ref _activeReconnectOperations);
    public long ReconnectAttemptCount => Interlocked.Read(ref _reconnectAttempts);
    public bool IsConnected => Session is { State: ConnectionState.Connected };
    public bool IsSelected => Session?.HsmsState == HsmsConnectionState.Selected;

    internal EquipmentLogIdentity LogIdentity => new(EquipmentId, ConnectionId, Endpoint, SessionId);
    public event PropertyChangedEventHandler? PropertyChanged;

    public Task ConnectAsync(CancellationToken cancellationToken) => ConnectCoreAsync(cancellationToken, setDesiredState: true);

    private async Task ConnectCoreAsync(CancellationToken cancellationToken, bool setDesiredState)
    {
        ThrowIfDisposed();
        if (setDesiredState) Volatile.Write(ref _desiredConnected, 1);
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
            if (!setDesiredState && !ShouldAutoReconnect) return;
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
                throw new InvalidOperationException($"{EquipmentId} already has a connection attempt in progress.");
            if (_session is { } staleSession)
            {
                staleSession.MessageReceived -= OnMessageReceived;
                await DisposeSessionAsync(staleSession).ConfigureAwait(false);
                _session = null;
                _gemRuntime = null;
            }

            Definition.Validate();
            ConnectionId = Guid.NewGuid().ToString("N");
            LastError = "None";
            Touch();

            HsmsSession? session = null;
            try
            {
                HsmsSession? created = null;
                created = new HsmsSession(ToOptions(), diagnostics: new EquipmentDiagnosticSink(
                    _events, () => LogIdentity, diagnostic => OnDiagnostic(created, diagnostic)));
                session = created;
                Interlocked.Increment(ref _liveSessions);
                session.MessageReceived += OnMessageReceived;
                _session = session;
                RefreshStates();
                _events.Info(LogIdentity, "MultiEquipment", "Connection attempt started.");

                await session.ConnectAsync(operationCancellation.Token).ConfigureAwait(false);
                if (Definition.Mode == SecsConnectionMode.Active)
                    await session.SelectAsync(operationCancellation.Token).ConfigureAwait(false);
                RefreshStates();
                _events.Info(LogIdentity, "MultiEquipment", "Connection established.");
            }
            catch (Exception exception)
            {
                if (exception is OperationCanceledException) Volatile.Write(ref _desiredConnected, 0);
                LastError = exception.Message;
                _events.Error(LogIdentity, "MultiEquipment", exception);
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
        Volatile.Write(ref _desiredConnected, 0);
        Interlocked.Increment(ref _disconnecting);
        try
        {
            lock (_connectGate) _connectCancellation?.Cancel();
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
                _events.Info(LogIdentity, "MultiEquipment", "Connection disposed.");
            }
            finally { _lifecycleGate.Release(); }
        }
        finally { Interlocked.Decrement(ref _disconnecting); }
    }

    public async Task ReconnectAsync(CancellationToken cancellationToken)
    {
        BeginReconnectOperation();
        try
        {
            await DisconnectAsync(cancellationToken).ConfigureAwait(false);
            await ConnectCoreAsync(cancellationToken, setDesiredState: true).ConfigureAwait(false);
        }
        finally { EndReconnectOperation(); }
    }

    internal async Task RunAutomaticReconnectAttemptAsync(CancellationToken cancellationToken)
    {
        if (!ShouldAutoReconnect) return;
        BeginReconnectOperation();
        try { await ConnectCoreAsync(cancellationToken, setDesiredState: false).ConfigureAwait(false); }
        finally { EndReconnectOperation(); }
    }

    public async Task LinktestAsync(CancellationToken cancellationToken)
    {
        var session = RequireSelectedSession();
        await session.LinktestAsync(cancellationToken).ConfigureAwait(false);
        Touch();
        _events.Info(LogIdentity, "MultiEquipment", "Linktest completed.");
    }

    public Task<SecsMessage> SendPrimaryAsync(byte stream, byte function, SecsItem? item,
        CancellationToken cancellationToken, SecsSystemBytes? systemBytes = null)
    {
        var session = RequireSelectedSession();
        var request = new SecsMessage(new SecsSessionId(SessionId), new SecsStream(stream), new SecsFunction(function),
            true, systemBytes ?? session.AllocateSystemBytes(), item);
        return RequestCoreAsync(session, request, cancellationToken);
    }

    public Task SendAsync(byte stream, byte function, SecsItem? item, CancellationToken cancellationToken)
    {
        var session = RequireSelectedSession();
        var message = new SecsMessage(new SecsSessionId(SessionId), new SecsStream(stream), new SecsFunction(function),
            false, session.AllocateSystemBytes(), item);
        return SendCoreAsync(session, message, cancellationToken);
    }

    private async Task<SecsMessage> RequestCoreAsync(HsmsSession session, SecsMessage request,
        CancellationToken cancellationToken)
    {
        _events.Message(LogIdentity, "TX", request);
        Touch();
        Interlocked.Increment(ref _pendingTransactions);
        OnPropertyChanged(nameof(PendingTransactionCount));
        try
        {
            var response = await session.SendPrimaryAsync(request, cancellationToken).ConfigureAwait(false);
            _events.Message(LogIdentity, "RX", response);
            Touch();
            if (response.Stream.Value == 1 && response.Function.Value == 14 && IsAcceptedS1F14(response.Item))
                MarkGemCommunicating();
            LastError = "None";
            return response;
        }
        catch (Exception exception)
        {
            LastError = exception is SecsTransactionTimeoutException ? $"T3: {exception.Message}" : exception.Message;
            _events.Error(LogIdentity, "MultiEquipment", exception);
            throw;
        }
        finally
        {
            Interlocked.Decrement(ref _pendingTransactions);
            OnPropertyChanged(nameof(PendingTransactionCount));
        }
    }

    private async Task SendCoreAsync(HsmsSession session, SecsMessage message, CancellationToken cancellationToken)
    {
        _events.Message(LogIdentity, "TX", message);
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
            Volatile.Write(ref _desiredConnected, 0);
            _lifetimeCancellation.Cancel();
            await DisconnectAsync(CancellationToken.None).ConfigureAwait(false);
            EndReconnectOperation(force: true);
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

    internal void ReportReconnectQueueRejected()
    {
        var exception = new InvalidOperationException($"Reconnect queue capacity was exhausted for {EquipmentId}.");
        LastError = exception.Message;
        _events.Error(LogIdentity, "MultiEquipment", exception);
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
        AutoReconnect = false,
        MaximumFrameLength = Definition.MaximumFrameLength,
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
        _events.Message(LogIdentity, "RX", message);
        Touch();
    }

    private void OnDiagnostic(HsmsSession? session, SecsDiagnosticEvent diagnostic)
    {
        if (session is null || !ReferenceEquals(Session, session)) return;
        if (diagnostic.Kind == SecsDiagnosticKind.ConnectionClosed)
        {
            Interlocked.Exchange(ref _gemRuntime, null);
            GemState = "Disabled";
            if (Volatile.Read(ref _disconnecting) == 0 && Volatile.Read(ref _disposed) == 0)
            {
                LastError = diagnostic.Message;
                if (ShouldAutoReconnect) _reconnectScheduler.TrySchedule(this);
            }
        }
        else if (diagnostic.Kind == SecsDiagnosticKind.ConnectionEstablished)
        {
            _gemRuntime = CreateGemRuntime(session);
            LastError = "None";
        }
        if (diagnostic.Kind is SecsDiagnosticKind.Timeout or SecsDiagnosticKind.ProtocolError or SecsDiagnosticKind.ApplicationError)
            LastError = diagnostic.Message;
        RefreshStates();
    }

    private void BeginReconnectOperation()
    {
        Interlocked.Increment(ref _reconnectAttempts);
        Interlocked.Increment(ref _activeReconnectOperations);
        Interlocked.Increment(ref _liveReconnectOperations);
        OnPropertyChanged(nameof(ActiveReconnectOperationCount));
        OnPropertyChanged(nameof(ReconnectAttemptCount));
    }

    private void EndReconnectOperation(bool force = false)
    {
        if (force)
        {
            while (Volatile.Read(ref _activeReconnectOperations) > 0) EndReconnectOperation();
            return;
        }
        if (Interlocked.Decrement(ref _activeReconnectOperations) < 0)
        {
            Interlocked.Exchange(ref _activeReconnectOperations, 0);
            return;
        }
        Interlocked.Decrement(ref _liveReconnectOperations);
        OnPropertyChanged(nameof(ActiveReconnectOperationCount));
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

    private void SetState<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        lock (_stateGate)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return;
            field = value;
        }
        OnPropertyChanged(propertyName);
    }

    private void OnPropertyChanged(string? propertyName)
    {
        var handlers = PropertyChanged;
        if (handlers is null || string.IsNullOrEmpty(propertyName)) return;
        void Raise() => handlers(this, new PropertyChangedEventArgs(propertyName));
        if (_notificationContext is null || ReferenceEquals(SynchronizationContext.Current, _notificationContext)) Raise();
        else _notificationContext.Post(_ => Raise(), null);
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
        public Task SendAsync(SecsMessage message, CancellationToken cancellationToken = default) =>
            owner.SendCoreAsync(session, message, cancellationToken);
        public Task<SecsMessage> RequestAsync(SecsMessage message, CancellationToken cancellationToken = default) =>
            owner.RequestCoreAsync(session, message, cancellationToken);
        public SecsSystemBytes AllocateSystemBytes() => session.AllocateSystemBytes();
    }
}
