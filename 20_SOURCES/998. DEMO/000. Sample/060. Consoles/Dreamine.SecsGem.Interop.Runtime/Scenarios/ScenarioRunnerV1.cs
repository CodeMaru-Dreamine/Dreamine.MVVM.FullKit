using System.Threading.Channels;
using Dreamine.Communication.Abstractions.Enums;
using Dreamine.Secs.Abstractions.Hsms;
using Dreamine.Secs.Abstractions.Interfaces;
using Dreamine.Secs.Abstractions.Model;
using Dreamine.SecsGem.Interop.Runtime.Persistence;
using Dreamine.SecsGem.Interop.Runtime.Templates;

namespace Dreamine.SecsGem.Interop.Runtime.Scenarios;

/// <summary>Executes one bounded scenario v1 document against one provider-neutral message session.</summary>
public sealed class ScenarioRunnerV1
{
    private readonly TimeProvider _timeProvider;
    private readonly int _inboundQueueCapacity;

    public ScenarioRunnerV1(TimeProvider? timeProvider = null, int inboundQueueCapacity = 128)
    {
        if (inboundQueueCapacity is < 1 or > ScenarioLimitsV1.MaximumInboundQueueCapacity)
            throw new ArgumentOutOfRangeException(nameof(inboundQueueCapacity));
        _timeProvider = timeProvider ?? TimeProvider.System;
        _inboundQueueCapacity = inboundQueueCapacity;
    }

    public async Task<ScenarioRunResultV1> RunAsync(
        ScenarioDefinitionV1 scenario,
        ISecsMessageSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(session);
        var started = _timeProvider.GetUtcNow();
        try
        {
            scenario.Validate();
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or
                                           NotSupportedException or OverflowException or JsonPersistenceException)
        {
            return new ScenarioRunResultV1(
                ScenarioRunStatusV1.Invalid,
                ScenarioExitCodesV1.Invalid,
                started,
                _timeProvider.GetUtcNow(),
                [],
                "invalid_scenario",
                exception.Message);
        }

        var inbound = Channel.CreateBounded<SecsMessage>(new BoundedChannelOptions(_inboundQueueCapacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait,
            AllowSynchronousContinuations = false
        });
        long dropped = 0;
        void OnMessage(object? _, SecsMessage message)
        {
            if (!inbound.Writer.TryWrite(message)) Interlocked.Increment(ref dropped);
        }

        session.MessageReceived += OnMessage;
        using var runDeadline = new CancellationTokenSource(
            TimeSpan.FromMilliseconds(scenario.RunTimeoutMilliseconds),
            _timeProvider);
        using var run = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, runDeadline.Token);
        var state = new ExecutionState(session, inbound.Reader, cancellationToken, runDeadline);
        try
        {
            await ExecuteSequenceAsync(
                scenario.Steps,
                prefix: null,
                run.Token,
                state,
                []).ConfigureAwait(false);
            var droppedCount = Volatile.Read(ref dropped);
            if (droppedCount > 0)
            {
                return new ScenarioRunResultV1(
                    ScenarioRunStatusV1.Failed,
                    ScenarioExitCodesV1.Failed,
                    started,
                    _timeProvider.GetUtcNow(),
                    state.Results.ToArray(),
                    "inbound_messages_dropped",
                    $"The bounded inbound queue dropped {droppedCount} message(s); the run cannot be evidence of a pass.",
                    droppedCount);
            }
            return new ScenarioRunResultV1(
                ScenarioRunStatusV1.Passed,
                ScenarioExitCodesV1.Passed,
                started,
                _timeProvider.GetUtcNow(),
                state.Results.ToArray(),
                DroppedInboundMessageCount: Volatile.Read(ref dropped));
        }
        catch (ScenarioExecutionStoppedException stopped)
        {
            return new ScenarioRunResultV1(
                stopped.RunStatus,
                ExitCode(stopped.RunStatus),
                started,
                _timeProvider.GetUtcNow(),
                state.Results.ToArray(),
                stopped.ErrorCode,
                stopped.Message,
                Volatile.Read(ref dropped));
        }
        finally
        {
            session.MessageReceived -= OnMessage;
            inbound.Writer.TryComplete();
        }
    }

    private async Task ExecuteSequenceAsync(
        IReadOnlyList<ScenarioStepV1> steps,
        string? prefix,
        CancellationToken parentToken,
        ExecutionState state,
        IReadOnlyList<CancellationTokenSource> ancestorDeadlines)
    {
        foreach (var step in steps)
        {
            var path = prefix is null ? step.Id : $"{prefix}/{step.Id}";
            await ExecuteStepAsync(step, path, parentToken, state, ancestorDeadlines).ConfigureAwait(false);
        }
    }

    private async Task ExecuteStepAsync(
        ScenarioStepV1 step,
        string path,
        CancellationToken parentToken,
        ExecutionState state,
        IReadOnlyList<CancellationTokenSource> ancestorDeadlines)
    {
        var started = _timeProvider.GetUtcNow();
        using var deadline = new CancellationTokenSource(
            TimeSpan.FromMilliseconds(step.TimeoutMilliseconds),
            _timeProvider);
        using var execution = CancellationTokenSource.CreateLinkedTokenSource(parentToken, deadline.Token);
        try
        {
            execution.Token.ThrowIfCancellationRequested();
            switch (step)
            {
                case ConnectScenarioStepV1:
                    await state.Session.ConnectAsync(execution.Token).ConfigureAwait(false);
                    break;
                case WaitForStateScenarioStepV1 wait:
                    await WaitForStateAsync(state.Session, wait.State, execution.Token).ConfigureAwait(false);
                    break;
                case SelectScenarioStepV1:
                    await state.Session.SelectAsync(execution.Token).ConfigureAwait(false);
                    break;
                case LinktestScenarioStepV1:
                    await state.Session.LinktestAsync(execution.Token).ConfigureAwait(false);
                    break;
                case SendScenarioStepV1 send:
                    await SendAsync(send, state, execution.Token).ConfigureAwait(false);
                    break;
                case ExpectScenarioStepV1 expect:
                    await ExpectAsync(expect, state, execution.Token).ConfigureAwait(false);
                    break;
                case DelayScenarioStepV1 delay:
                    await Task.Delay(
                        TimeSpan.FromMilliseconds(delay.DelayMilliseconds),
                        _timeProvider,
                        execution.Token).ConfigureAwait(false);
                    break;
                case SeparateScenarioStepV1:
                    await state.Session.SeparateAsync(execution.Token).ConfigureAwait(false);
                    break;
                case DisconnectScenarioStepV1:
                    await state.Session.DisconnectAsync(execution.Token).ConfigureAwait(false);
                    break;
                case RepeatScenarioStepV1 repeat:
                {
                    var nestedDeadlines = new CancellationTokenSource[ancestorDeadlines.Count + 1];
                    for (var index = 0; index < ancestorDeadlines.Count; index++)
                        nestedDeadlines[index] = ancestorDeadlines[index];
                    nestedDeadlines[^1] = deadline;
                    for (var iteration = 1; iteration <= repeat.Count; iteration++)
                        await ExecuteSequenceAsync(
                            repeat.Steps,
                            $"{path}[{iteration}]",
                            execution.Token,
                            state,
                            nestedDeadlines).ConfigureAwait(false);
                    break;
                }
                default:
                    throw new NotSupportedException($"Scenario step '{step.GetType().Name}' is not supported by runner v1.");
            }

            state.Results.Add(new ScenarioStepResultV1(
                path,
                ScenarioStepStatusV1.Passed,
                started,
                _timeProvider.GetUtcNow()));
        }
        catch (ScenarioExecutionStoppedException stopped)
        {
            state.Results.Add(new ScenarioStepResultV1(
                path,
                stopped.StepStatus,
                started,
                _timeProvider.GetUtcNow(),
                stopped.ErrorCode,
                stopped.Message));
            throw;
        }
        catch (OperationCanceledException exception)
        {
            var cancellation = ClassifyCancellation(state, deadline, ancestorDeadlines);
            var stopped = cancellation switch
            {
                CancellationClassification.Caller => new ScenarioExecutionStoppedException(
                    ScenarioRunStatusV1.Cancelled, ScenarioStepStatusV1.Cancelled, "cancelled", "The scenario was cancelled by the caller.", exception),
                CancellationClassification.RunDeadline => new ScenarioExecutionStoppedException(
                    ScenarioRunStatusV1.TimedOut, ScenarioStepStatusV1.TimedOut, "run_timeout", "The scenario run deadline expired.", exception),
                _ => new ScenarioExecutionStoppedException(
                    ScenarioRunStatusV1.TimedOut, ScenarioStepStatusV1.TimedOut, "step_timeout", $"The deadline for step '{path}' expired.", exception)
            };
            state.Results.Add(new ScenarioStepResultV1(
                path,
                stopped.StepStatus,
                started,
                _timeProvider.GetUtcNow(),
                stopped.ErrorCode,
                stopped.Message));
            throw stopped;
        }
        catch (Exception exception)
        {
            var errorCode = exception is ScenarioAssertionException ? "assertion_failed" : "step_failed";
            var stopped = new ScenarioExecutionStoppedException(
                ScenarioRunStatusV1.Failed,
                ScenarioStepStatusV1.Failed,
                errorCode,
                exception.Message,
                exception);
            state.Results.Add(new ScenarioStepResultV1(
                path,
                stopped.StepStatus,
                started,
                _timeProvider.GetUtcNow(),
                stopped.ErrorCode,
                stopped.Message));
            throw stopped;
        }
    }

    private static async Task SendAsync(
        SendScenarioStepV1 step,
        ExecutionState state,
        CancellationToken cancellationToken)
    {
        var dialogue = step.CreateDialogue();
        var item = step.Body?.BuildItem();
        state.LastSent = null;
        state.LastReply = null;
        if (dialogue.ReplyExpected)
        {
            var reply = await state.Session.RequestAsync(dialogue, item, cancellationToken).ConfigureAwait(false);
            state.LastReply = reply;
            // The safe W1 API owns System Bytes allocation and validates the correlated Secondary.
            // Preserve that proven correlation without bypassing the high-level session contract.
            state.LastSent = new SecsMessage(
                state.Session.ConnectionIdentity.SessionId,
                dialogue.Stream,
                dialogue.PrimaryFunction,
                true,
                reply.SystemBytes,
                item);
        }
        else
        {
            await state.Session.SendAsync(
                dialogue.Stream,
                dialogue.PrimaryFunction,
                item,
                cancellationToken).ConfigureAwait(false);
            // W0 exposes no allocation receipt. A later LastSent correlation assertion therefore
            // fails explicitly instead of inventing or guessing System Bytes.
        }
    }

    private static async Task ExpectAsync(
        ExpectScenarioStepV1 step,
        ExecutionState state,
        CancellationToken cancellationToken)
    {
        var message = step.Source switch
        {
            ScenarioMessageSourceV1.LastReply => state.LastReply ??
                throw new ScenarioAssertionException("No W1 reply is available for this expectation."),
            ScenarioMessageSourceV1.NextMessage => await state.Inbound.ReadAsync(cancellationToken).ConfigureAwait(false),
            _ => throw new NotSupportedException($"Message source '{step.Source}' is not supported.")
        };
        AssertMessage(step.Matcher, message, state.LastSent);
    }

    private static void AssertMessage(
        ScenarioMessageMatcherV1 matcher,
        SecsMessage actual,
        SecsMessage? lastSent)
    {
        if (matcher.SessionId is { } sessionId && actual.SessionId.Value != sessionId)
            throw Mismatch("Session ID", sessionId, actual.SessionId.Value);
        if (matcher.Stream is { } stream && actual.Stream.Value != stream)
            throw Mismatch("Stream", stream, actual.Stream.Value);
        if (matcher.Function is { } function && actual.Function.Value != function)
            throw Mismatch("Function", function, actual.Function.Value);
        if (matcher.ReplyExpected is { } replyExpected && actual.ReplyExpected != replyExpected)
            throw Mismatch("W-bit", replyExpected, actual.ReplyExpected);
        switch (matcher.Correlation)
        {
            case ScenarioCorrelationV1.Ignore:
                break;
            case ScenarioCorrelationV1.LastSent:
                if (lastSent is null)
                    throw new ScenarioAssertionException("No sent message is available for correlation.");
                if (actual.SystemBytes != lastSent.SystemBytes)
                    throw Mismatch("System Bytes", lastSent.SystemBytes.Value, actual.SystemBytes.Value);
                break;
            case ScenarioCorrelationV1.Exact:
                if (actual.SystemBytes.Value != matcher.SystemBytes)
                    throw Mismatch("System Bytes", matcher.SystemBytes, actual.SystemBytes.Value);
                break;
            default:
                throw new NotSupportedException($"Correlation matcher '{matcher.Correlation}' is not supported.");
        }

        switch (matcher.BodyMatch)
        {
            case ScenarioBodyMatchV1.Ignore:
                break;
            case ScenarioBodyMatchV1.Absent:
                if (actual.Item is not null) throw new ScenarioAssertionException("Expected no message body.");
                break;
            case ScenarioBodyMatchV1.Exact:
                if (actual.Item is null || matcher.Body is null || !ExactItemEquals(matcher.Body.BuildItem(), actual.Item))
                    throw new ScenarioAssertionException("The message body did not exactly match the v1 template.");
                break;
            case ScenarioBodyMatchV1.Structural:
                if (actual.Item is null || matcher.Body is null || !StructuralItemEquals(matcher.Body, actual.Item))
                    throw new ScenarioAssertionException("The message body did not match the v1 structural template.");
                break;
            default:
                throw new NotSupportedException($"Body matcher '{matcher.BodyMatch}' is not supported.");
        }
    }

    private static ScenarioAssertionException Mismatch(string field, object? expected, object? actual) =>
        new($"{field} mismatch: expected '{expected}', actual '{actual}'.");

    private static bool StructuralItemEquals(SecsItemTemplateNode expected, SecsItem actual)
    {
        if (expected.Format != actual.Format) return false;
        if (actual is not SecsListItem list) return expected.BuildItem().Count == actual.Count;
        if (expected.Children.Count != list.Count) return false;
        for (var index = 0; index < list.Count; index++)
            if (!StructuralItemEquals(expected.Children[index], list.Items[index])) return false;
        return true;
    }

    private static bool ExactItemEquals(SecsItem expected, SecsItem actual)
    {
        if (expected.GetType() != actual.GetType() || expected.Count != actual.Count) return false;
        return (expected, actual) switch
        {
            (SecsListItem left, SecsListItem right) => ListEquals(left, right),
            (SecsAsciiItem left, SecsAsciiItem right) => StringComparer.Ordinal.Equals(left.Value, right.Value),
            (SecsBinaryItem left, SecsBinaryItem right) => left.Values.Span.SequenceEqual(right.Values.Span),
            (SecsBooleanItem left, SecsBooleanItem right) => left.Values.Span.SequenceEqual(right.Values.Span),
            (SecsJis8Item left, SecsJis8Item right) => left.Values.Span.SequenceEqual(right.Values.Span),
            (SecsInt8Item left, SecsInt8Item right) => left.Values.Span.SequenceEqual(right.Values.Span),
            (SecsInt16Item left, SecsInt16Item right) => left.Values.Span.SequenceEqual(right.Values.Span),
            (SecsInt32Item left, SecsInt32Item right) => left.Values.Span.SequenceEqual(right.Values.Span),
            (SecsInt64Item left, SecsInt64Item right) => left.Values.Span.SequenceEqual(right.Values.Span),
            (SecsUInt8Item left, SecsUInt8Item right) => left.Values.Span.SequenceEqual(right.Values.Span),
            (SecsUInt16Item left, SecsUInt16Item right) => left.Values.Span.SequenceEqual(right.Values.Span),
            (SecsUInt32Item left, SecsUInt32Item right) => left.Values.Span.SequenceEqual(right.Values.Span),
            (SecsUInt64Item left, SecsUInt64Item right) => left.Values.Span.SequenceEqual(right.Values.Span),
            (SecsFloat32Item left, SecsFloat32Item right) => left.Values.Span.SequenceEqual(right.Values.Span),
            (SecsFloat64Item left, SecsFloat64Item right) => left.Values.Span.SequenceEqual(right.Values.Span),
            _ => false
        };
    }

    private static bool ListEquals(SecsListItem expected, SecsListItem actual)
    {
        for (var index = 0; index < expected.Count; index++)
            if (!ExactItemEquals(expected.Items[index], actual.Items[index])) return false;
        return true;
    }

    private static async Task WaitForStateAsync(
        ISecsMessageSession session,
        ScenarioWaitStateV1 expected,
        CancellationToken cancellationToken)
    {
        if (HasState(session, expected)) return;
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnStateChanged(object? _, SecsSessionStateChangedEventArgs __)
        {
            if (HasState(session, expected)) completion.TrySetResult();
        }

        session.StateChanged += OnStateChanged;
        try
        {
            if (HasState(session, expected)) return;
            await completion.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            session.StateChanged -= OnStateChanged;
        }
    }

    private static bool HasState(ISecsMessageSession session, ScenarioWaitStateV1 expected) => expected switch
    {
        ScenarioWaitStateV1.Connected => session.State == ConnectionState.Connected &&
                                         session.HsmsState is HsmsConnectionState.ConnectedNotSelected or HsmsConnectionState.Selected,
        ScenarioWaitStateV1.Selected => session.State == ConnectionState.Connected &&
                                        session.HsmsState == HsmsConnectionState.Selected,
        _ => false
    };

    private static CancellationClassification ClassifyCancellation(
        ExecutionState state,
        CancellationTokenSource ownDeadline,
        IReadOnlyList<CancellationTokenSource> ancestorDeadlines)
    {
        if (state.CallerCancellation.IsCancellationRequested) return CancellationClassification.Caller;
        if (state.RunDeadline.IsCancellationRequested) return CancellationClassification.RunDeadline;
        if (ownDeadline.IsCancellationRequested) return CancellationClassification.StepDeadline;
        for (var index = ancestorDeadlines.Count - 1; index >= 0; index--)
            if (ancestorDeadlines[index].IsCancellationRequested) return CancellationClassification.StepDeadline;
        return CancellationClassification.Caller;
    }

    private static int ExitCode(ScenarioRunStatusV1 status) => status switch
    {
        ScenarioRunStatusV1.Passed => ScenarioExitCodesV1.Passed,
        ScenarioRunStatusV1.Invalid => ScenarioExitCodesV1.Invalid,
        ScenarioRunStatusV1.TimedOut => ScenarioExitCodesV1.TimedOut,
        ScenarioRunStatusV1.Cancelled => ScenarioExitCodesV1.Cancelled,
        _ => ScenarioExitCodesV1.Failed
    };

    private sealed class ExecutionState(
        ISecsMessageSession session,
        ChannelReader<SecsMessage> inbound,
        CancellationToken callerCancellation,
        CancellationTokenSource runDeadline)
    {
        public ISecsMessageSession Session { get; } = session;
        public ChannelReader<SecsMessage> Inbound { get; } = inbound;
        public CancellationToken CallerCancellation { get; } = callerCancellation;
        public CancellationTokenSource RunDeadline { get; } = runDeadline;
        public List<ScenarioStepResultV1> Results { get; } = [];
        public SecsMessage? LastSent { get; set; }
        public SecsMessage? LastReply { get; set; }
    }

    private enum CancellationClassification { Caller, RunDeadline, StepDeadline }

    private sealed class ScenarioExecutionStoppedException(
        ScenarioRunStatusV1 runStatus,
        ScenarioStepStatusV1 stepStatus,
        string errorCode,
        string message,
        Exception? innerException = null) : Exception(message, innerException)
    {
        public ScenarioRunStatusV1 RunStatus { get; } = runStatus;
        public ScenarioStepStatusV1 StepStatus { get; } = stepStatus;
        public string ErrorCode { get; } = errorCode;
    }

    private sealed class ScenarioAssertionException(string message) : Exception(message);
}
