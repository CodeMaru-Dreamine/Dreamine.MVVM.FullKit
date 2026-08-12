using Dreamine.Secs.Abstractions.Enums;
using Dreamine.Secs.Abstractions.Hsms;
using Dreamine.Secs.Abstractions.Interfaces;
using Dreamine.Secs.Abstractions.Model;
using Dreamine.SecsGem.Interop.Runtime.Templates;

namespace Dreamine.SecsGem.Interop.Runtime.Responders;

/// <summary>
/// Registers exact responder v1 rules on the Gate 2 dispatcher. The dispatcher context remains the
/// sole owner of Secondary reply construction and transmission.
/// </summary>
public sealed class ConfigurableResponderV1 : IAsyncDisposable
{
    private readonly ISecsMessageSession _session;
    private readonly RuleSnapshot[] _rules;
    private readonly TimeSpan _shutdownTimeout;
    private readonly TimeProvider _timeProvider;
    private readonly object _gate = new();
    private ResponderRun? _run;
    private bool _disposed;
    private ResponderFaultEventArgs? _lastFault;

    public ConfigurableResponderV1(
        ISecsMessageSession session,
        ResponderConfigurationV1 configuration,
        TimeProvider? timeProvider = null)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        ArgumentNullException.ThrowIfNull(configuration);
        configuration.Validate();
        _rules = configuration.Rules
            .Select(static rule => new RuleSnapshot(
                rule.Id,
                rule.Enabled,
                rule.CreateDialogue(),
                rule.ReplyExpected,
                rule.ReplyMode,
                rule.DelayMilliseconds,
                rule.InvocationMode,
                rule.ReplyBody?.CloneDeep()))
            .ToArray();
        _shutdownTimeout = TimeSpan.FromMilliseconds(configuration.ShutdownTimeoutMilliseconds);
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public bool IsEnabled { get { lock (_gate) return _run is not null; } }
    public int ActiveHandlerCount { get { lock (_gate) return _run?.ActiveHandlerCount ?? 0; } }
    public ResponderFaultEventArgs? LastFault => Volatile.Read(ref _lastFault);
    public event EventHandler<ResponderFaultEventArgs>? Faulted;

    /// <summary>Validates Equipment/Selected state and installs each enabled exact S/F/W rule.</summary>
    public void Enable()
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_run is not null) return;
            var identity = _session.ConnectionIdentity;
            if (identity.Role != SecsRole.Equipment)
                throw new InvalidOperationException("Configurable responder v1 can be enabled only for an Equipment session.");
            if (_session.HsmsState != HsmsConnectionState.Selected || identity.ConnectionEpoch <= 0)
                throw new InvalidOperationException("Configurable responder v1 requires a Selected session with a positive connection epoch.");

            var run = new ResponderRun(identity);
            try
            {
                foreach (var rule in _rules.Where(static value => value.Enabled))
                {
                    var state = new RuleRunState(rule);
                    run.RuleStates.Add(state);
                    run.Registrations.Add(_session.PrimaryDispatcher.Register(
                        rule.Dialogue,
                        (context, cancellationToken) => HandleAsync(run, state, context, cancellationToken)));
                }
                _run = run;
            }
            catch
            {
                run.BeginStop();
                throw;
            }
        }
    }

    /// <summary>Cancels delayed work, removes registrations, and waits only until the configured shutdown deadline.</summary>
    public async Task<ResponderShutdownResultV1> DisableAsync(CancellationToken cancellationToken = default)
    {
        ResponderRun? run;
        lock (_gate)
        {
            run = _run;
            if (run is null)
                return new ResponderShutdownResultV1(ResponderShutdownStatusV1.AlreadyStopped, 0);
            _run = null;
        }

        // Registration disposal and cancellation may invoke provider/user callbacks.
        // Never run those callbacks while holding the lifecycle lock.
        run.BeginStop();
        ObserveStopFault(run);

        return await WaitForShutdownAsync(run, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        ResponderRun? run;
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            run = _run;
            _run = null;
        }
        if (run is not null)
        {
            run.BeginStop();
            ObserveStopFault(run);
            await WaitForShutdownAsync(run, CancellationToken.None).ConfigureAwait(false);
        }
    }

    private async ValueTask HandleAsync(
        ResponderRun run,
        RuleRunState ruleState,
        ISecsPrimaryContext context,
        CancellationToken dispatcherCancellation)
    {
        if (!run.TryEnter()) return;
        try
        {
            var rule = ruleState.Rule;
            if (context.Primary.ReplyExpected != rule.ReplyExpected) return;
            if (!IsCurrentSelectedContext(run.Identity, context.ConnectionIdentity)) return;
            if (rule.InvocationMode == ResponderInvocationModeV1.Once &&
                Interlocked.CompareExchange(ref ruleState.Consumed, 1, 0) != 0)
                return;
            if (rule.ReplyMode == ResponderReplyModeV1.NoReply) return;

            using var execution = CancellationTokenSource.CreateLinkedTokenSource(
                run.Cancellation.Token,
                dispatcherCancellation);
            if (rule.ReplyMode == ResponderReplyModeV1.Delayed)
            {
                await Task.Delay(
                    TimeSpan.FromMilliseconds(rule.DelayMilliseconds),
                    _timeProvider,
                    execution.Token).ConfigureAwait(false);
            }
            await context.ReplyAsync(rule.ReplyBody?.BuildItem(), execution.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (run.Cancellation.IsCancellationRequested || dispatcherCancellation.IsCancellationRequested)
        {
            // Lifecycle cancellation is an observed normal shutdown path.
        }
        catch (Exception exception)
        {
            PublishFault(ruleState.Rule.Id, exception);
        }
        finally
        {
            run.Exit();
        }
    }

    private bool IsCurrentSelectedContext(
        SecsConnectionIdentity enabledIdentity,
        SecsConnectionIdentity contextIdentity)
    {
        var current = _session.ConnectionIdentity;
        return _session.HsmsState == HsmsConnectionState.Selected &&
               current.Role == SecsRole.Equipment &&
               contextIdentity.Role == SecsRole.Equipment &&
               current.SessionInstanceId == enabledIdentity.SessionInstanceId &&
               contextIdentity.SessionInstanceId == enabledIdentity.SessionInstanceId &&
               current.ConnectionEpoch == enabledIdentity.ConnectionEpoch &&
               contextIdentity.ConnectionEpoch == enabledIdentity.ConnectionEpoch &&
               current.SessionId == enabledIdentity.SessionId &&
               contextIdentity.SessionId == enabledIdentity.SessionId;
    }

    private async Task<ResponderShutdownResultV1> WaitForShutdownAsync(
        ResponderRun run,
        CancellationToken cancellationToken)
    {
        using var deadline = new CancellationTokenSource(_shutdownTimeout, _timeProvider);
        using var wait = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadline.Token);
        try
        {
            await run.Drained.Task.WaitAsync(wait.Token).ConfigureAwait(false);
            return new ResponderShutdownResultV1(ResponderShutdownStatusV1.Completed, 0);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new ResponderShutdownResultV1(
                ResponderShutdownStatusV1.Cancelled,
                run.ActiveHandlerCount,
                "Responder shutdown wait was cancelled by the caller.");
        }
        catch (OperationCanceledException)
        {
            var exception = new TimeoutException(
                $"Responder shutdown exceeded the bounded deadline of {_shutdownTimeout.TotalMilliseconds:N0} ms with {run.ActiveHandlerCount} handler(s) active.");
            PublishFault("$shutdown", exception);
            return new ResponderShutdownResultV1(
                ResponderShutdownStatusV1.TimedOut,
                run.ActiveHandlerCount,
                exception.Message);
        }
    }

    private void PublishFault(string ruleId, Exception exception)
    {
        var observed = new ResponderFaultEventArgs(ruleId, exception, _timeProvider.GetUtcNow());
        Volatile.Write(ref _lastFault, observed);
        var handlers = Faulted;
        if (handlers is null) return;
        foreach (EventHandler<ResponderFaultEventArgs> handler in handlers.GetInvocationList())
        {
            try { handler(this, observed); }
            catch { /* A diagnostic observer cannot fault protocol dispatch. */ }
        }
    }

    private void ObserveStopFault(ResponderRun run)
    {
        if (run.StopFault is { } exception) PublishFault("$shutdown", exception);
    }

    private sealed record RuleSnapshot(
        string Id,
        bool Enabled,
        SecsDialogueDefinition Dialogue,
        bool ReplyExpected,
        ResponderReplyModeV1 ReplyMode,
        int DelayMilliseconds,
        ResponderInvocationModeV1 InvocationMode,
        SecsItemTemplateNode? ReplyBody);

    private sealed class RuleRunState(RuleSnapshot rule)
    {
        public RuleSnapshot Rule { get; } = rule;
        public int Consumed;
    }

    private sealed class ResponderRun(SecsConnectionIdentity identity)
    {
        private int _stopping;
        private int _activeHandlers;
        private int _cancellationCompleted;
        private int _cancellationDisposed;
        public SecsConnectionIdentity Identity { get; } = identity;
        public CancellationTokenSource Cancellation { get; } = new();
        public List<IDisposable> Registrations { get; } = [];
        public List<RuleRunState> RuleStates { get; } = [];
        public TaskCompletionSource Drained { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int ActiveHandlerCount => Volatile.Read(ref _activeHandlers);
        public Exception? StopFault { get; private set; }

        public bool TryEnter()
        {
            if (Volatile.Read(ref _stopping) != 0) return false;
            Interlocked.Increment(ref _activeHandlers);
            if (Volatile.Read(ref _stopping) == 0) return true;
            Exit();
            return false;
        }

        public void Exit()
        {
            if (Interlocked.Decrement(ref _activeHandlers) == 0 && Volatile.Read(ref _stopping) != 0)
                CompleteDrain();
        }

        public void BeginStop()
        {
            if (Interlocked.Exchange(ref _stopping, 1) != 0) return;
            List<Exception>? failures = null;
            foreach (var registration in Registrations)
            {
                try { registration.Dispose(); }
                catch (Exception exception) { (failures ??= []).Add(exception); }
            }
            Registrations.Clear();
            try { Cancellation.Cancel(); }
            catch (Exception exception) { (failures ??= []).Add(exception); }
            finally { Volatile.Write(ref _cancellationCompleted, 1); }
            if (failures is not null)
                StopFault = failures.Count == 1 ? failures[0] : new AggregateException(failures);
            if (Volatile.Read(ref _activeHandlers) == 0) CompleteDrain();
        }

        private void CompleteDrain()
        {
            Drained.TrySetResult();
            if (Volatile.Read(ref _cancellationCompleted) != 0 &&
                Interlocked.Exchange(ref _cancellationDisposed, 1) == 0)
                Cancellation.Dispose();
        }
    }
}
