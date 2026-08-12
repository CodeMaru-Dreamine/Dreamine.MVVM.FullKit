using System.Runtime.CompilerServices;
using System.IO;
using Dreamine.Communication.Abstractions.Enums;
using Dreamine.Secs.Abstractions.Diagnostics;
using Dreamine.Secs.Abstractions.Enums;
using Dreamine.Secs.Abstractions.Hsms;
using Dreamine.Secs.Abstractions.Interfaces;
using Dreamine.Secs.Abstractions.Model;
using Dreamine.SecsGem.Interop.Runtime.Responders;
using Dreamine.SecsGem.Interop.Runtime.Persistence;
using Dreamine.SecsGem.Interop.Runtime.Scenarios;
using Dreamine.SecsGem.Interop.Runtime.Templates;
using Xunit;

namespace Dreamine.SecsGem.Interop.Wpf.Tests;

public sealed class ScenarioResponderRuntimeTests
{
    [Fact]
    public async Task ScenarioJsonRoundTripsConcreteVersionOneStepsAndRejectsNewerSchemas()
    {
        var scenario = Scenario(
            new SendScenarioStepV1
            {
                Id = "send",
                Target = ScenarioExecutionBindingV1.CurrentConnectionTarget,
                Stream = 1,
                PrimaryFunction = 1,
                SecondaryFunction = 2,
                Body = Ascii("PING")
            },
            new ExpectScenarioStepV1
            {
                Id = "expect",
                Target = ScenarioExecutionBindingV1.CurrentConnectionTarget,
                Source = ScenarioMessageSourceV1.LastReply,
                Matcher = new ScenarioMessageMatcherV1
                {
                    SessionId = 7,
                    Stream = 1,
                    Function = 2,
                    ReplyExpected = false,
                    Correlation = ScenarioCorrelationV1.LastSent,
                    BodyMatch = ScenarioBodyMatchV1.Exact,
                    Body = Ascii("PONG")
                }
            },
            new RepeatScenarioStepV1
            {
                Id = "repeat",
                Target = ScenarioExecutionBindingV1.CurrentConnectionTarget,
                Count = 2,
                Steps =
                [
                    new DelayScenarioStepV1
                    {
                        Id = "pause",
                        Target = ScenarioExecutionBindingV1.CurrentConnectionTarget,
                        DelayMilliseconds = 1
                    }
                ]
            });

        var directory = Path.Combine(Path.GetTempPath(), $"dreamine-scenario-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "scenario.json");
        var store = new ScenarioFileStoreV1();
        try
        {
            await store.SaveAsync(path, scenario);
            var restored = await store.LoadAsync(path);

            Assert.Equal(ScenarioDefinitionV1.CurrentSchemaVersion, restored.Version);
            Assert.Collection(
                restored.Steps,
                step => Assert.IsType<SendScenarioStepV1>(step),
                step => Assert.IsType<ExpectScenarioStepV1>(step),
                step => Assert.IsType<RepeatScenarioStepV1>(step));

            var json = await File.ReadAllTextAsync(path);
            await File.WriteAllTextAsync(path, json.Replace("\"version\": 1", "\"version\": 2", StringComparison.Ordinal));
            await Assert.ThrowsAsync<JsonSchemaVersionException>(() => store.LoadAsync(path));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task RunnerExecutesLifecycleW0W1ExpectationsAndBoundedRepeat()
    {
        var session = new FakeMessageSession(SecsRole.Host, new SecsSessionId(7));
        session.ReplyFactory = request => new SecsMessage(
            request.SessionId,
            request.Stream,
            new SecsFunction((byte)(request.Function.Value + 1)),
            false,
            request.SystemBytes,
            new SecsAsciiItem("PONG"));
        session.InboundAfterW0 = new SecsMessage(
            new SecsSessionId(7),
            new SecsStream(6),
            new SecsFunction(11),
            false,
            new SecsSystemBytes(91),
            new SecsListItem(new SecsUInt16Item(1), new SecsAsciiItem("EVENT")));
        var scenario = Scenario(
            new ConnectScenarioStepV1 { Id = "connect", Target = "$current" },
            new WaitForStateScenarioStepV1 { Id = "connected", Target = "$current", State = ScenarioWaitStateV1.Connected },
            new SelectScenarioStepV1 { Id = "select", Target = "$current" },
            new WaitForStateScenarioStepV1 { Id = "selected", Target = "$current", State = ScenarioWaitStateV1.Selected },
            new LinktestScenarioStepV1 { Id = "linktest", Target = "$current" },
            new SendScenarioStepV1
            {
                Id = "w1",
                Target = "$current",
                Stream = 1,
                PrimaryFunction = 1,
                SecondaryFunction = 2,
                Body = Ascii("PING")
            },
            new ExpectScenarioStepV1
            {
                Id = "reply",
                Target = "$current",
                Source = ScenarioMessageSourceV1.LastReply,
                Matcher = new ScenarioMessageMatcherV1
                {
                    SessionId = 7,
                    Stream = 1,
                    Function = 2,
                    ReplyExpected = false,
                    Correlation = ScenarioCorrelationV1.LastSent,
                    BodyMatch = ScenarioBodyMatchV1.Exact,
                    Body = Ascii("PONG")
                }
            },
            new SendScenarioStepV1
            {
                Id = "w0",
                Target = "$current",
                Stream = 6,
                PrimaryFunction = 11,
                Body = List(new SecsItemTemplateNode(SecsItemFormat.UInt16, ["1"]))
            },
            new ExpectScenarioStepV1
            {
                Id = "message",
                Target = "$current",
                Source = ScenarioMessageSourceV1.NextMessage,
                Matcher = new ScenarioMessageMatcherV1
                {
                    Stream = 6,
                    Function = 11,
                    ReplyExpected = false,
                    BodyMatch = ScenarioBodyMatchV1.Structural,
                    Body = List(
                        new SecsItemTemplateNode(SecsItemFormat.UInt16, ["999"]),
                        Ascii("OTHER"))
                }
            },
            new RepeatScenarioStepV1
            {
                Id = "repeat",
                Target = "$current",
                Count = 3,
                Steps = [new DelayScenarioStepV1 { Id = "delay", Target = "$current", DelayMilliseconds = 1 }]
            },
            new SeparateScenarioStepV1 { Id = "separate", Target = "$current" },
            new DisconnectScenarioStepV1 { Id = "disconnect", Target = "$current" });

        var result = await new ScenarioRunnerV1().RunAsync(scenario, session, CancellationToken.None);

        Assert.Equal(ScenarioRunStatusV1.Passed, result.Status);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(1, session.ConnectCount);
        Assert.Equal(1, session.SelectCount);
        Assert.Equal(1, session.LinktestCount);
        Assert.Single(session.W1Messages);
        Assert.Single(session.W0Messages);
        Assert.Equal(1, session.SafeW1CallCount);
        Assert.Equal(1, session.SafeW0CallCount);
        Assert.Equal(0, session.AllocateSystemBytesCallCount);
        Assert.Equal(0, session.ExpertW1CallCount);
        Assert.Equal(0, session.ExpertW0CallCount);
        Assert.Equal(1, session.SeparateCount);
        Assert.Equal(1, session.DisconnectCount);
        Assert.Contains(result.Steps, step => step.Path == "repeat[3]/delay" && step.Status == ScenarioStepStatusV1.Passed);
    }

    [Fact]
    public async Task RunnerAppliesStepDeadlineEvenWithoutCallerCancellation()
    {
        var session = new FakeMessageSession(SecsRole.Host, new SecsSessionId(0));
        var scenario = Scenario(new DelayScenarioStepV1
        {
            Id = "too-slow",
            Target = "$current",
            DelayMilliseconds = 5_000,
            TimeoutMilliseconds = 30
        });

        var result = await new ScenarioRunnerV1().RunAsync(scenario, session, CancellationToken.None);

        Assert.Equal(ScenarioRunStatusV1.TimedOut, result.Status);
        Assert.NotEqual(0, result.ExitCode);
        Assert.Equal("step_timeout", Assert.Single(result.Steps).ErrorCode);
    }

    [Fact]
    public async Task RunnerReturnsStructuredCancelledOutcome()
    {
        var session = new FakeMessageSession(SecsRole.Host, new SecsSessionId(0));
        var scenario = Scenario(new DelayScenarioStepV1
        {
            Id = "cancel",
            Target = "$current",
            DelayMilliseconds = 5_000
        });
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(30));

        var result = await new ScenarioRunnerV1().RunAsync(scenario, session, cancellation.Token);

        Assert.Equal(ScenarioRunStatusV1.Cancelled, result.Status);
        Assert.NotEqual(0, result.ExitCode);
    }

    [Fact]
    public async Task RunnerDowngradesOtherwisePassedRunWhenInboundMessagesWereDropped()
    {
        var session = new FakeMessageSession(SecsRole.Host, new SecsSessionId(7))
        {
            InboundBurstAfterW0 =
            [
                Primary(7, 6, 11, false, 1),
                Primary(7, 6, 11, false, 2),
                Primary(7, 6, 11, false, 3)
            ]
        };
        var scenario = Scenario(new SendScenarioStepV1
        {
            Id = "burst",
            Target = "$current",
            Stream = 6,
            PrimaryFunction = 11
        });

        var result = await new ScenarioRunnerV1(inboundQueueCapacity: 1)
            .RunAsync(scenario, session, CancellationToken.None);

        Assert.Equal(ScenarioRunStatusV1.Failed, result.Status);
        Assert.Equal(ScenarioExitCodesV1.Failed, result.ExitCode);
        Assert.Equal("inbound_messages_dropped", result.ErrorCode);
        Assert.Contains("dropped", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, result.DroppedInboundMessageCount);
        Assert.Equal(ScenarioStepStatusV1.Passed, Assert.Single(result.Steps).Status);
    }

    [Fact]
    public async Task RunnerRejectsRepeatExpansionBeyondVersionOneBound()
    {
        var session = new FakeMessageSession(SecsRole.Host, new SecsSessionId(0));
        var scenario = Scenario(new RepeatScenarioStepV1
        {
            Id = "outer",
            Target = "$current",
            Count = 1_000,
            Steps =
            [
                new RepeatScenarioStepV1
                {
                    Id = "inner",
                    Target = "$current",
                    Count = 1_000,
                    Steps = [new DelayScenarioStepV1 { Id = "delay", Target = "$current", DelayMilliseconds = 0 }]
                }
            ]
        });

        var result = await new ScenarioRunnerV1().RunAsync(scenario, session, CancellationToken.None);

        Assert.Equal(ScenarioRunStatusV1.Invalid, result.Status);
        Assert.Equal(ScenarioExitCodesV1.Invalid, result.ExitCode);
        Assert.Equal("invalid_scenario", result.ErrorCode);
    }

    [Fact]
    public async Task RunnerDoesNotInventW0SystemBytesForLastSentCorrelation()
    {
        var session = new FakeMessageSession(SecsRole.Host, new SecsSessionId(0), selected: true)
        {
            InboundAfterW0 = Primary(0, 1, 1, false, 99)
        };
        var scenario = Scenario(
            new SendScenarioStepV1
            {
                Id = "w0",
                Target = "$current",
                Stream = 1,
                PrimaryFunction = 1
            },
            new ExpectScenarioStepV1
            {
                Id = "correlation",
                Target = "$current",
                Source = ScenarioMessageSourceV1.NextMessage,
                Matcher = new ScenarioMessageMatcherV1
                {
                    Stream = 1,
                    Function = 1,
                    Correlation = ScenarioCorrelationV1.LastSent
                }
            });

        var result = await new ScenarioRunnerV1().RunAsync(scenario, session, CancellationToken.None);

        Assert.Equal(ScenarioRunStatusV1.Failed, result.Status);
        Assert.Equal("assertion_failed", result.ErrorCode);
        Assert.Contains("No sent message", Assert.IsType<string>(result.ErrorMessage), StringComparison.Ordinal);
        Assert.Equal(0, session.AllocateSystemBytesCallCount);
        Assert.Equal(0, session.ExpertW0CallCount);
    }

    [Fact]
    public async Task RunnerRejectsCyclicItemTemplateWithoutRecursingUnboundedly()
    {
        var cyclic = new SecsItemTemplateNode(SecsItemFormat.List);
        cyclic.Children.Add(cyclic);
        var scenario = Scenario(new SendScenarioStepV1
        {
            Id = "cycle",
            Target = "$current",
            Stream = 1,
            PrimaryFunction = 1,
            Body = cyclic
        });

        var result = await new ScenarioRunnerV1().RunAsync(
            scenario,
            new FakeMessageSession(SecsRole.Host, new SecsSessionId(0), selected: true),
            CancellationToken.None);

        Assert.Equal(ScenarioRunStatusV1.Invalid, result.Status);
        Assert.Equal("invalid_scenario", result.ErrorCode);
        Assert.Contains("cycle", Assert.IsType<string>(result.ErrorMessage), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ResponderMatchesExactWBitUsesDispatcherReplyOwnerAndHonorsOnce()
    {
        var session = new FakeMessageSession(SecsRole.Equipment, new SecsSessionId(7), selected: true);
        var configuration = Responder(new ResponderRuleV1
        {
            Id = "are-you-there",
            Stream = 1,
            PrimaryFunction = 1,
            ReplyExpected = true,
            ReplyMode = ResponderReplyModeV1.Immediate,
            InvocationMode = ResponderInvocationModeV1.Once,
            ReplyBody = Ascii("OK")
        });
        await using var responder = new ConfigurableResponderV1(session, configuration);
        responder.Enable();

        var first = await session.Dispatcher.DispatchAsync(Primary(7, 1, 1, true, 1));
        var second = await session.Dispatcher.DispatchAsync(Primary(7, 1, 1, true, 2));
        var wrongW = await session.Dispatcher.DispatchAsync(Primary(7, 1, 1, false, 3));

        Assert.Equal(1, first.ReplyCount);
        Assert.Equal("OK", Assert.IsType<SecsAsciiItem>(first.ReplyBody).Value);
        Assert.Equal(0, second.ReplyCount);
        Assert.Equal(0, wrongW.ReplyCount);
        Assert.True(responder.IsEnabled);

        var shutdown = await responder.DisableAsync();
        Assert.Equal(ResponderShutdownStatusV1.Completed, shutdown.Status);
        Assert.False(responder.IsEnabled);
        Assert.False(session.Dispatcher.HasRegistrations);
    }

    [Fact]
    public async Task ResponderDisableCancelsDelayedReplyAndDrainsWithinDeadline()
    {
        var session = new FakeMessageSession(SecsRole.Equipment, new SecsSessionId(0), selected: true);
        var configuration = Responder(new ResponderRuleV1
        {
            Id = "delayed",
            Stream = 1,
            PrimaryFunction = 3,
            ReplyExpected = true,
            ReplyMode = ResponderReplyModeV1.Delayed,
            DelayMilliseconds = 30_000,
            InvocationMode = ResponderInvocationModeV1.Repeat,
            ReplyBody = Ascii("LATE")
        });
        await using var responder = new ConfigurableResponderV1(session, configuration);
        responder.Enable();
        var dispatch = session.Dispatcher.DispatchAsync(Primary(0, 1, 3, true, 1));
        await WaitUntilAsync(() => responder.ActiveHandlerCount == 1);

        var shutdown = await responder.DisableAsync();
        var context = await dispatch;

        Assert.Equal(ResponderShutdownStatusV1.Completed, shutdown.Status);
        Assert.Equal(0, context.ReplyCount);
        Assert.Equal(0, responder.ActiveHandlerCount);
    }

    [Fact]
    public async Task ResponderNoReplyRuleRepeatsWithoutTakingReplyOwnership()
    {
        var session = new FakeMessageSession(SecsRole.Equipment, new SecsSessionId(0), selected: true);
        await using var responder = new ConfigurableResponderV1(session, Responder(new ResponderRuleV1
        {
            Id = "intentional-no-reply",
            Stream = 2,
            PrimaryFunction = 1,
            ReplyExpected = true,
            ReplyMode = ResponderReplyModeV1.NoReply,
            InvocationMode = ResponderInvocationModeV1.Repeat
        }));
        responder.Enable();

        var first = await session.Dispatcher.DispatchAsync(Primary(0, 2, 1, true, 1));
        var second = await session.Dispatcher.DispatchAsync(Primary(0, 2, 1, true, 2));

        Assert.Equal(0, first.ReplyCount);
        Assert.Equal(0, second.ReplyCount);
    }

    [Fact]
    public async Task ResponderObservesReplyFailureWithoutLeakingItFromDispatcher()
    {
        var session = new FakeMessageSession(SecsRole.Equipment, new SecsSessionId(0), selected: true);
        session.Dispatcher.ReplyBehavior = static (_, _) =>
            ValueTask.FromException(new IOException("reply failed"));
        await using var responder = new ConfigurableResponderV1(session, Responder(new ResponderRuleV1
        {
            Id = "fault",
            Stream = 3,
            PrimaryFunction = 1,
            ReplyExpected = true,
            ReplyMode = ResponderReplyModeV1.Immediate,
            InvocationMode = ResponderInvocationModeV1.Repeat
        }));
        ResponderFaultEventArgs? observed = null;
        responder.Faulted += (_, value) => observed = value;
        responder.Enable();

        var context = await session.Dispatcher.DispatchAsync(Primary(0, 3, 1, true, 1));

        Assert.Equal(1, context.ReplyCount);
        Assert.NotNull(observed);
        Assert.Equal("fault", observed.RuleId);
        Assert.IsType<IOException>(observed.Exception);
        Assert.Same(observed, responder.LastFault);
    }

    [Fact]
    public async Task ResponderShutdownReturnsTimedOutWhenAReplyOwnerIgnoresCancellation()
    {
        var session = new FakeMessageSession(SecsRole.Equipment, new SecsSessionId(0), selected: true);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        session.Dispatcher.ReplyBehavior = async (_, _) => await release.Task;
        await using var responder = new ConfigurableResponderV1(session, Responder(30, new ResponderRuleV1
        {
            Id = "stuck",
            Stream = 4,
            PrimaryFunction = 1,
            ReplyExpected = true,
            ReplyMode = ResponderReplyModeV1.Immediate,
            InvocationMode = ResponderInvocationModeV1.Repeat
        }));
        responder.Enable();
        var dispatch = session.Dispatcher.DispatchAsync(Primary(0, 4, 1, true, 1));
        await WaitUntilAsync(() => responder.ActiveHandlerCount == 1);

        var shutdown = await responder.DisableAsync();

        Assert.Equal(ResponderShutdownStatusV1.TimedOut, shutdown.Status);
        Assert.Equal(1, shutdown.RemainingHandlerCount);
        Assert.IsType<TimeoutException>(responder.LastFault?.Exception);
        release.TrySetResult();
        await dispatch;
    }

    [Theory]
    [InlineData(SecsRole.Host, true)]
    [InlineData(SecsRole.Equipment, false)]
    public async Task ResponderRequiresSelectedEquipmentRole(SecsRole role, bool selected)
    {
        var session = new FakeMessageSession(role, new SecsSessionId(0), selected);
        await using var responder = new ConfigurableResponderV1(session, Responder(new ResponderRuleV1
        {
            Id = "rule",
            Stream = 1,
            PrimaryFunction = 1,
            ReplyExpected = true,
            ReplyMode = ResponderReplyModeV1.NoReply,
            InvocationMode = ResponderInvocationModeV1.Repeat
        }));

        Assert.Throws<InvalidOperationException>(responder.Enable);
    }

    private static ScenarioDefinitionV1 Scenario(params ScenarioStepV1[] steps) => new()
    {
        Schema = ScenarioDefinitionV1.SchemaName,
        Version = ScenarioDefinitionV1.CurrentSchemaVersion,
        Id = "runtime-v1",
        Name = "Runtime v1",
        Binding = ScenarioExecutionBindingV1.CurrentConnection(),
        RunTimeoutMilliseconds = 10_000,
        Steps = [.. steps]
    };

    private static ResponderConfigurationV1 Responder(params ResponderRuleV1[] rules) => Responder(1_000, rules);

    private static ResponderConfigurationV1 Responder(int shutdownTimeoutMilliseconds, params ResponderRuleV1[] rules) => new()
    {
        Schema = ResponderConfigurationV1.SchemaName,
        Version = ResponderConfigurationV1.CurrentSchemaVersion,
        ShutdownTimeoutMilliseconds = shutdownTimeoutMilliseconds,
        Rules = [.. rules]
    };

    private static SecsItemTemplateNode Ascii(string value) => new(SecsItemFormat.Ascii, [value]);

    private static SecsItemTemplateNode List(params SecsItemTemplateNode[] children)
    {
        var list = new SecsItemTemplateNode(SecsItemFormat.List);
        list.Children.AddRange(children);
        return list;
    }

    private static SecsMessage Primary(ushort sessionId, byte stream, byte function, bool w, uint systemBytes) =>
        new(new SecsSessionId(sessionId), new SecsStream(stream), new SecsFunction(function), w, new SecsSystemBytes(systemBytes));

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        while (!condition()) await Task.Delay(5, timeout.Token);
    }

    private sealed class FakeMessageSession : ISecsMessageSession
    {
        private readonly Guid _instanceId = Guid.NewGuid();
        private readonly SecsRole _role;
        private readonly SecsSessionId _sessionId;
        private long _epoch;
        private uint _safeSystemBytes;

        public FakeMessageSession(SecsRole role, SecsSessionId sessionId, bool selected = false)
        {
            _role = role;
            _sessionId = sessionId;
            State = selected ? ConnectionState.Connected : ConnectionState.Disconnected;
            HsmsState = selected ? HsmsConnectionState.Selected : HsmsConnectionState.NotConnected;
            _epoch = selected ? 1 : 0;
            Dispatcher.ConnectionIdentity = ConnectionIdentity;
        }

        public FakePrimaryDispatcher Dispatcher { get; } = new();
        public Func<SecsMessage, SecsMessage>? ReplyFactory { get; set; }
        public SecsMessage? InboundAfterW0 { get; set; }
        public IReadOnlyList<SecsMessage>? InboundBurstAfterW0 { get; set; }
        public List<SecsMessage> W0Messages { get; } = [];
        public List<SecsMessage> W1Messages { get; } = [];
        public int ConnectCount { get; private set; }
        public int SelectCount { get; private set; }
        public int LinktestCount { get; private set; }
        public int SeparateCount { get; private set; }
        public int DisconnectCount { get; private set; }
        public int SafeW0CallCount { get; private set; }
        public int SafeW1CallCount { get; private set; }
        public int AllocateSystemBytesCallCount { get; private set; }
        public int ExpertW0CallCount { get; private set; }
        public int ExpertW1CallCount { get; private set; }
        public string ProviderKey => "fake";
        public ConnectionState State { get; private set; }
        public HsmsConnectionState HsmsState { get; private set; }
        public SecsConnectionIdentity ConnectionIdentity =>
            new(ProviderKey, _instanceId, _epoch, _sessionId, _role, SecsConnectionMode.Active);
        public ISecsPrimaryDispatcher PrimaryDispatcher => Dispatcher;
        public bool IsWireObservationEnabled => false;
        public long DroppedWireObservationCount => 0;
        public event EventHandler<SecsMessage>? MessageReceived;
        public event EventHandler<SecsDiagnosticEvent>? DiagnosticReceived { add { } remove { } }
        public event EventHandler<SecsSessionStateChangedEventArgs>? StateChanged;

        public Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ConnectCount++;
            var previousState = State;
            var previousHsms = HsmsState;
            State = ConnectionState.Connected;
            HsmsState = HsmsConnectionState.ConnectedNotSelected;
            _epoch++;
            RaiseState(previousState, previousHsms);
            return Task.CompletedTask;
        }

        public Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            DisconnectCount++;
            SetDisconnected();
            return Task.CompletedTask;
        }

        public Task SelectAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SelectCount++;
            var previousState = State;
            var previousHsms = HsmsState;
            State = ConnectionState.Connected;
            HsmsState = HsmsConnectionState.Selected;
            RaiseState(previousState, previousHsms);
            return Task.CompletedTask;
        }

        public Task DeselectAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var previousState = State;
            var previousHsms = HsmsState;
            HsmsState = HsmsConnectionState.ConnectedNotSelected;
            RaiseState(previousState, previousHsms);
            return Task.CompletedTask;
        }

        public Task LinktestAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LinktestCount++;
            return Task.CompletedTask;
        }

        public Task SeparateAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SeparateCount++;
            SetDisconnected();
            return Task.CompletedTask;
        }

        public SecsSystemBytes AllocateSystemBytes()
        {
            AllocateSystemBytesCallCount++;
            return new SecsSystemBytes(0xfeed_beef);
        }

        public Task SendAsync(SecsMessage message, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ExpertW0CallCount++;
            return Task.CompletedTask;
        }

        public Task<SecsMessage> SendPrimaryAsync(SecsMessage message, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ExpertW1CallCount++;
            return Task.FromResult(ReplyFactory?.Invoke(message) ?? new SecsMessage(
                message.SessionId,
                message.Stream,
                new SecsFunction((byte)(message.Function.Value + 1)),
                false,
                message.SystemBytes));
        }

        public Task SendAsync(SecsStream stream, SecsFunction function, SecsItem? item = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SafeW0CallCount++;
            W0Messages.Add(new SecsMessage(_sessionId, stream, function, false, new SecsSystemBytes(++_safeSystemBytes), item));
            if (InboundAfterW0 is { } inbound) MessageReceived?.Invoke(this, inbound);
            if (InboundBurstAfterW0 is { } burst)
                foreach (var message in burst) MessageReceived?.Invoke(this, message);
            return Task.CompletedTask;
        }

        public Task<SecsMessage> RequestAsync(SecsDialogueDefinition dialogue, SecsItem? item = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SafeW1CallCount++;
            var request = new SecsMessage(
                _sessionId,
                dialogue.Stream,
                dialogue.PrimaryFunction,
                true,
                new SecsSystemBytes(++_safeSystemBytes),
                item);
            W1Messages.Add(request);
            return Task.FromResult(ReplyFactory?.Invoke(request) ?? new SecsMessage(
                request.SessionId,
                request.Stream,
                dialogue.SecondaryFunction!.Value,
                false,
                request.SystemBytes));
        }

        public async IAsyncEnumerable<HsmsWireObservation> ReadWireObservationsAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            cancellationToken.ThrowIfCancellationRequested();
            yield break;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        private void SetDisconnected()
        {
            var previousState = State;
            var previousHsms = HsmsState;
            State = ConnectionState.Disconnected;
            HsmsState = HsmsConnectionState.NotConnected;
            RaiseState(previousState, previousHsms);
        }

        private void RaiseState(ConnectionState previousState, HsmsConnectionState previousHsms) =>
            StateChanged?.Invoke(this, new SecsSessionStateChangedEventArgs(
                previousState,
                State,
                previousHsms,
                HsmsState,
                ConnectionIdentity));
    }

    private sealed class FakePrimaryDispatcher : ISecsPrimaryDispatcher
    {
        private readonly object _gate = new();
        private readonly List<Registration> _registrations = [];

        public long DroppedPrimaryCount => 0;
        public SecsConnectionIdentity? ConnectionIdentity { get; set; }
        public Func<SecsItem?, CancellationToken, ValueTask>? ReplyBehavior { get; set; }
        public bool HasRegistrations { get { lock (_gate) return _registrations.Count != 0; } }

        public IDisposable Register(
            SecsDialogueDefinition dialogue,
            Func<ISecsPrimaryContext, CancellationToken, ValueTask> handler)
        {
            var registration = new Registration(this, dialogue, handler);
            lock (_gate) _registrations.Add(registration);
            return registration;
        }

        public IDisposable RegisterFallback(Func<ISecsPrimaryContext, CancellationToken, ValueTask> handler) =>
            throw new NotSupportedException();

        public async Task<FakePrimaryContext> DispatchAsync(SecsMessage primary)
        {
            Registration registration;
            lock (_gate)
                registration = _registrations.Single(value =>
                    value.Dialogue.Stream == primary.Stream &&
                    value.Dialogue.PrimaryFunction == primary.Function);
            var context = new FakePrimaryContext(
                primary,
                registration.Dialogue,
                ConnectionIdentity ?? throw new InvalidOperationException("The fake dispatcher has no session identity."),
                ReplyBehavior);
            await registration.Handler(context, CancellationToken.None);
            return context;
        }

        private void Remove(Registration registration)
        {
            lock (_gate) _registrations.Remove(registration);
        }

        private sealed class Registration(
            FakePrimaryDispatcher owner,
            SecsDialogueDefinition dialogue,
            Func<ISecsPrimaryContext, CancellationToken, ValueTask> handler) : IDisposable
        {
            private int _disposed;
            public SecsDialogueDefinition Dialogue { get; } = dialogue;
            public Func<ISecsPrimaryContext, CancellationToken, ValueTask> Handler { get; } = handler;
            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) == 0) owner.Remove(this);
            }
        }
    }

    private sealed class FakePrimaryContext(
        SecsMessage primary,
        SecsDialogueDefinition dialogue,
        SecsConnectionIdentity identity,
        Func<SecsItem?, CancellationToken, ValueTask>? replyBehavior) : ISecsPrimaryContext
    {
        private int _replyCount;
        public SecsConnectionIdentity ConnectionIdentity { get; } = identity;
        public SecsMessage Primary { get; } = primary;
        public bool CanReply => Primary.ReplyExpected && dialogue.ReplyExpected;
        public int ReplyCount => Volatile.Read(ref _replyCount);
        public SecsItem? ReplyBody { get; private set; }
        public async ValueTask ReplyAsync(SecsItem? item = null, CancellationToken cancellationToken = default)
        {
            if (!CanReply) throw new InvalidOperationException("Reply ownership is unavailable.");
            if (Interlocked.Increment(ref _replyCount) != 1) throw new InvalidOperationException("Reply ownership was reused.");
            ReplyBody = item;
            if (replyBehavior is not null)
                await replyBehavior(item, cancellationToken);
            else
                cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
