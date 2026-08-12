using Dreamine.Communication.Abstractions.Enums;
using Dreamine.Secs.Abstractions.Diagnostics;
using Dreamine.Secs.Abstractions.Hsms;
using Dreamine.Secs.Abstractions.Interfaces;
using Dreamine.Secs.Abstractions.Model;
using Dreamine.Secs.Abstractions.Options;
using Dreamine.Secs.Com;
using Dreamine.Secs.Com.Hsms;
using Dreamine.SecsGem.Interop.Runtime.Logging;
using Dreamine.SecsGem.Interop.Wpf.Models;

namespace Dreamine.SecsGem.Interop.Wpf.Managers;

public sealed class ConnectionManager : IAsyncDisposable
{
    private readonly InteropLogManager _log;
    private readonly Func<ConnectionSettings, ISecsMessageSessionProvider> _providerFactory;
    private readonly WireLogManager _wireLog;
    private readonly object _passiveConnectGate = new();
    private ISecsMessageSession? _session;
    private ConnectionSettings? _lastSettings;
    private PassiveConnectOperation? _passiveConnect;

    public ConnectionManager(InteropLogManager log)
        : this(log, new WireLogManager(log))
    {
    }

    public ConnectionManager(InteropLogManager log, WireLogManager wireLog)
    {
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _wireLog = wireLog ?? throw new ArgumentNullException(nameof(wireLog));
        _providerFactory = settings => new DreamineSecsCommunicationProvider(_ => settings.ToOptions());
    }

    // EN: This seam lets the sample prove that its managers depend on the public provider contract.
    // KO: 이 경계는 샘플 Manager가 공개 provider 계약에만 의존함을 테스트할 수 있게 한다.
    internal ConnectionManager(InteropLogManager log, ISecsMessageSessionProvider provider)
        : this(log, provider, new WireLogManager(log))
    {
    }

    internal ConnectionManager(
        InteropLogManager log,
        ISecsMessageSessionProvider provider,
        WireLogManager wireLog)
        : this(log, _ => provider, wireLog)
    {
        ArgumentNullException.ThrowIfNull(provider);
    }

    internal ConnectionManager(
        InteropLogManager log,
        Func<ConnectionSettings, ISecsMessageSessionProvider> providerFactory,
        WireLogManager wireLog)
    {
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _wireLog = wireLog ?? throw new ArgumentNullException(nameof(wireLog));
        _providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
    }

    /// <summary>
    /// EN: Retains the original concrete sample surface for existing callers using the Dreamine provider.
    /// KO: Dreamine provider를 사용하는 기존 호출자를 위해 원래의 구체 샘플 표면을 유지한다.
    /// </summary>
    public HsmsSession? Session => _session as HsmsSession;

    internal ISecsMessageSession? MessageSession => _session;
    public string TcpState => _session?.State.ToString() ?? "Disconnected";
    public string HsmsState => _session?.HsmsState.ToString() ?? "NotConnected";
    public event EventHandler? StateChanged;
    public event Action<HsmsSession?>? SessionChanged;
    internal event Action<ISecsMessageSession?>? MessageSessionChanged;
    public event EventHandler<SecsMessage>? MessageReceived;

    public async Task ConnectAsync(ConnectionSettings settings, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (_session is not null) await DisconnectAsync(token).ConfigureAwait(false);

        // EN: The body policy reaches the transport before it can copy a frame payload.
        // KO: body 정책을 transport가 frame payload를 복사하기 전에 전달한다.
        settings.WireObservation = _wireLog.CreateObservationOptions(settings.LogPolicyId);
        var provider = _providerFactory(settings) ??
            throw new InvalidOperationException("The SECS message-session provider factory returned null.");
        var session = provider.CreateSession(new SecsConnectionOptions
        {
            ProviderKey = provider.Key,
            Role = settings.Role,
            Mode = settings.Mode
        }) ?? throw new InvalidOperationException("The SECS message-session provider returned null.");

        _lastSettings = settings;
        Subscribe(session);
        _session = session;
        NotifySessionChanged(session);
        RaiseStateChanged();
        try
        {
            _wireLog.Start(session, CreateWireLogIdentity(session), settings.LogPolicyId);
            var connectTask = session.ConnectAsync(token);
            if (settings.Mode == Dreamine.Secs.Abstractions.Enums.SecsConnectionMode.Passive)
            {
                await WaitForPassiveListenerAsync(session, connectTask, token).ConfigureAwait(false);
                var observer = ObservePassiveConnectAsync(session, connectTask);
                lock (_passiveConnectGate)
                    _passiveConnect = new PassiveConnectOperation(session, observer);
            }
            else
            {
                await connectTask.ConfigureAwait(false);
            }
        }
        catch
        {
            // Keep the published session available for explicit retry/disconnect, matching the
            // existing Workbench lifecycle. Its wire recorder remains owned until that cleanup.
            throw;
        }
        finally { RaiseStateChanged(); }
    }

    public async Task DisconnectAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var session = Interlocked.Exchange(ref _session, null);
        if (session is null) return;
        NotifySessionChanged(null);
        await DisposeSessionAndStopWireLogAsync(session).ConfigureAwait(false);
        RaiseStateChanged();
    }

    public async Task SelectAsync(CancellationToken token)
    {
        await RequireMessageSession().SelectAsync(token).ConfigureAwait(false);
        RaiseStateChanged();
    }

    public Task LinktestAsync(CancellationToken token) => RequireMessageSession().LinktestAsync(token);

    public async Task SeparateAsync(CancellationToken token)
    {
        var session = RequireMessageSession();
        try { await session.SeparateAsync(token).ConfigureAwait(false); }
        finally
        {
            if (ReferenceEquals(Interlocked.CompareExchange(ref _session, null, session), session))
                NotifySessionChanged(null);
            await DisposeSessionAndStopWireLogAsync(session).ConfigureAwait(false);
            RaiseStateChanged();
        }
    }

    public async Task ReconnectAsync(CancellationToken token)
    {
        var settings = _lastSettings ?? throw new InvalidOperationException("No previous connection settings are available.");
        await DisconnectAsync(token).ConfigureAwait(false);
        await ConnectAsync(settings, token).ConfigureAwait(false);
        if (RequireMessageSession().HsmsState == HsmsConnectionState.ConnectedNotSelected)
            await SelectAsync(token).ConfigureAwait(false);
    }

    public HsmsSession RequireSession() => Session ?? throw new InvalidOperationException(
        "The active provider does not expose a concrete Dreamine HSMS session. Use the provider-neutral message-session boundary.");

    internal ISecsMessageSession RequireMessageSession() =>
        _session ?? throw new InvalidOperationException("Connect a SECS message session first.");

    public async ValueTask DisposeAsync()
    {
        var session = Interlocked.Exchange(ref _session, null);
        if (session is null) return;
        NotifySessionChanged(null);
        await DisposeSessionAndStopWireLogAsync(session).ConfigureAwait(false);
        RaiseStateChanged();
    }

    private void Subscribe(ISecsMessageSession session)
    {
        session.MessageReceived += OnMessageReceived;
        session.DiagnosticReceived += OnDiagnosticReceived;
        session.StateChanged += OnSessionStateChanged;
    }

    private void Unsubscribe(ISecsMessageSession session)
    {
        session.MessageReceived -= OnMessageReceived;
        session.DiagnosticReceived -= OnDiagnosticReceived;
        session.StateChanged -= OnSessionStateChanged;
    }

    private void NotifySessionChanged(ISecsMessageSession? session)
    {
        MessageSessionChanged?.Invoke(session);
        SessionChanged?.Invoke(session as HsmsSession);
    }

    private void OnMessageReceived(object? sender, SecsMessage message)
    {
        _log.Message("RX", message);
        // EN: Preserve the source-session identity so delayed events cannot cross a reconnect boundary.
        // KO: 지연 이벤트가 재연결 경계를 넘지 않도록 원본 session identity를 sender로 유지한다.
        MessageReceived?.Invoke(sender, message);
        RaiseStateChanged();
    }

    private void OnDiagnosticReceived(object? sender, SecsDiagnosticEvent value)
    {
        _log.Diagnostic(value);
        var epoch = sender is ISecsMessageSession session
            ? session.ConnectionIdentity.ConnectionEpoch
            : 0;
        _wireLog.RecordDiagnostic(value, epoch);
        RaiseStateChanged();
    }

    private void OnSessionStateChanged(object? sender, SecsSessionStateChangedEventArgs value)
    {
        _wireLog.RecordState(value);
        RaiseStateChanged();
    }

    private async Task DisposeSessionAndStopWireLogAsync(ISecsMessageSession session)
    {
        Task? passiveConnectObserver = null;
        lock (_passiveConnectGate)
        {
            if (ReferenceEquals(_passiveConnect?.Session, session))
            {
                passiveConnectObserver = _passiveConnect.Observer;
                _passiveConnect = null;
            }
        }

        Exception? sessionFailure = null;
        try { await session.DisposeAsync().ConfigureAwait(false); }
        catch (Exception exception) { sessionFailure = exception; }
        finally
        {
            // EN: Dispose may emit the final state/diagnostic. Keep subscriptions until it returns,
            // then detach before stopping the recorder so the terminal observation is flushed.
            // KO: Dispose 중 마지막 상태/진단이 발생할 수 있으므로 반환까지 구독을 유지하고,
            // recorder를 중지하기 전에 해제하여 terminal 관측을 flush한다.
            Unsubscribe(session);
        }

        if (passiveConnectObserver is not null)
            await passiveConnectObserver.ConfigureAwait(false);

        Exception? logFailure = null;
        try { await _wireLog.StopAsync().ConfigureAwait(false); }
        catch (Exception exception) { logFailure = exception; }

        var health = _wireLog.LastHealth;
        var failure = health.WriterFailure ?? logFailure?.Message ?? "none";
        var healthSummary =
            $"SourceDropped={health.SourceDropped}; RecorderDropped={health.RecorderDropped}; " +
            $"Written={health.Written}; FlushCompleted={health.FlushCompleted}; Failure={failure}.";
        if (health.IsEvidenceEligible && logFailure is null)
            _log.Info("Wire Log", healthSummary);
        else
            _log.Error("Wire Log", new InvalidOperationException(healthSummary));

        if (sessionFailure is not null && logFailure is not null)
            throw new AggregateException("The SECS session and its wire logger both failed to stop.", sessionFailure, logFailure);
        if (sessionFailure is not null) throw sessionFailure;
        if (logFailure is not null) throw logFailure;
    }

    private static WireLogIdentity CreateWireLogIdentity(ISecsMessageSession session)
    {
        var identity = session.ConnectionIdentity;
        return new WireLogIdentity(
            identity.Role == Dreamine.Secs.Abstractions.Enums.SecsRole.Equipment ? "Equipment" : "Host",
            $"{identity.ProviderKey}:{identity.SessionInstanceId:N}",
            "redacted",
            identity.SessionId.Value);
    }

    private static async Task WaitForPassiveListenerAsync(
        ISecsMessageSession session,
        Task connectTask,
        CancellationToken cancellationToken)
    {
        if (session.State is ConnectionState.Listening or ConnectionState.Connected)
            return;

        var listening = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnStateChanged(object? _, SecsSessionStateChangedEventArgs __)
        {
            if (session.State is ConnectionState.Listening or ConnectionState.Connected)
                listening.TrySetResult();
        }

        session.StateChanged += OnStateChanged;
        try
        {
            OnStateChanged(session, null!);
            var completed = await Task.WhenAny(connectTask, listening.Task)
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);
            if (ReferenceEquals(completed, connectTask) || connectTask.IsCompleted)
                await connectTask.ConfigureAwait(false);
        }
        finally
        {
            session.StateChanged -= OnStateChanged;
        }
    }

    private async Task ObservePassiveConnectAsync(ISecsMessageSession session, Task connectTask)
    {
        try
        {
            await connectTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!ReferenceEquals(Volatile.Read(ref _session), session))
        {
            // Explicit disconnect/disposal owns the normal cancellation path.
        }
        catch (Exception exception)
        {
            _log.Error("Connection", exception);
        }
        finally
        {
            RaiseStateChanged();
        }
    }

    private void RaiseStateChanged() => StateChanged?.Invoke(this, EventArgs.Empty);

    private sealed record PassiveConnectOperation(ISecsMessageSession Session, Task Observer);
}
