using System.Reflection;
using System.Runtime.CompilerServices;
using System.IO;
using Dreamine.Communication.Abstractions.Enums;
using Dreamine.Secs.Abstractions.Diagnostics;
using Dreamine.Secs.Abstractions.Enums;
using Dreamine.Secs.Abstractions.Hsms;
using Dreamine.Secs.Abstractions.Interfaces;
using Dreamine.Secs.Abstractions.Model;
using Dreamine.Secs.Abstractions.Options;
using Dreamine.Secs.Com;
using Dreamine.SecsGem.Interop.Runtime;
using Dreamine.SecsGem.Interop.Wpf.Managers;
using Dreamine.SecsGem.Interop.Wpf.Models;
using Xunit;

namespace Dreamine.SecsGem.Interop.Wpf.Tests;

public sealed class Gate3ClosureRemediationTests
{
    [Fact]
    public async Task WorkbenchExportAllowListOmitsSensitiveFieldsAndValuesFromJsonAndMarkdown()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"dreamine-safe-export-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var scenario = new InteropScenarioResult("SAFE-01", "Safe export", external: false)
            {
                Status = InteropScenarioStatus.Passed,
                Detail = "peer 10.20.30.40 [2001:db8::5] host.private.internal C:\\customer\\recipe /opt/customer/recipe TOP-SECRET"
            };
            var log = new InteropLogEntry(
                DateTimeOffset.UnixEpoch,
                "Info",
                "SECS-II",
                "RX",
                "RX S1F2 from 10.20.30.40 host.private.internal TOP-SECRET",
                "decoded-message TOP-SECRET [2001:db8::5]",
                "00 01 02 DE AD BE EF",
                EquipmentId: "PRIVATE-EQUIPMENT-ID",
                ConnectionId: "PRIVATE-CONNECTION-ID",
                Endpoint: "host.private.internal:7100",
                SessionId: 73,
                CorrelationSystemBytes: 0xDEADBEEF,
                ContainsPrivateProfileData: false);

            var paths = await new ResultExportManager().ExportAsync(
                directory,
                [scenario],
                [log],
                CancellationToken.None);
            var json = await File.ReadAllTextAsync(paths.JsonPath);
            var markdown = await File.ReadAllTextAsync(paths.MarkdownPath);
            var combined = json + markdown;

            foreach (var forbidden in new[]
                     {
                         "10.20.30.40", "2001:db8::5", "host.private.internal", "C:\\customer",
                         "/opt/customer", "TOP-SECRET", "decoded-message", "00 01 02", "DE AD BE EF",
                         "PRIVATE-EQUIPMENT-ID", "PRIVATE-CONNECTION-ID"
                     })
            {
                Assert.DoesNotContain(forbidden, combined, StringComparison.OrdinalIgnoreCase);
            }

            foreach (var forbiddenProperty in new[]
                     {
                         "\"Message\"", "\"RawHex\"", "\"EquipmentId\"", "\"ConnectionId\"",
                         "\"Endpoint\"", "\"SessionId\"", "\"CorrelationSystemBytes\"",
                         "\"ContainsPrivateProfileData\""
                     })
            {
                Assert.DoesNotContain(forbiddenProperty, json, StringComparison.Ordinal);
            }
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task SelfTestExportsOmitFreeFormFailureAndCheckText()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"dreamine-safe-selftest-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            const string secret = "TOP-SECRET 10.20.30.40 C:\\customer\\recipe";
            var manager = new ResultExportManager();
            var selfTestPath = Path.Combine(directory, "self-test.json");
            var multiTestPath = Path.Combine(directory, "multi-test.json");
            var now = DateTimeOffset.UnixEpoch;
            var selfTest = new SelfTestSummary(
                now,
                now.AddSeconds(1),
                Requested: 4,
                Passed: 3,
                Failed: 1,
                TimeoutCount: 1,
                ReconnectCycles: 2,
                LinktestCount: 2,
                MessagesPerSecond: 4,
                MinimumLatencyMs: 1,
                AverageLatencyMs: 2,
                MaximumLatencyMs: 3,
                P95LatencyMs: 2.9,
                FirstFailure: secret,
                Result: "Failed",
                Checks: [$"FAILED: {secret}"]);
            var multiTest = new MultiEquipmentSelfTestSummary(
                now,
                now.AddSeconds(2),
                MaximumEquipment: 10,
                ConnectionsAttempted: 10,
                MessagesRequested: 100,
                MessagesPassed: 99,
                TimeoutCount: 1,
                ReconnectCount: 1,
                MemoryDeltaBytes: 4096,
                ConnectionsPerSecond: 5,
                MessagesPerSecond: 50,
                MinimumLatencyMs: 1,
                AverageLatencyMs: 2,
                MaximumLatencyMs: 4,
                RemainingSessions: 0,
                RemainingBackgroundOperations: 0,
                TrackedOperationTaskCount: 12,
                PeakConcurrentEquipmentOperations: 3,
                ConnectionTimings: [new EquipmentScaleTiming(10, 20)],
                Result: "Failed",
                Checks: [$"FAILED: {secret}"]);

            await manager.ExportSelfTestAsync(selfTestPath, selfTest, CancellationToken.None);
            await manager.ExportMultiEquipmentSelfTestAsync(multiTestPath, multiTest, CancellationToken.None);
            var combined = await File.ReadAllTextAsync(selfTestPath) + await File.ReadAllTextAsync(multiTestPath);

            Assert.DoesNotContain(secret, combined, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("FirstFailure", combined, StringComparison.Ordinal);
            Assert.DoesNotContain("Checks", combined, StringComparison.Ordinal);
            Assert.Contains("\"CheckCount\": 1", combined, StringComparison.Ordinal);
            Assert.Contains("\"Result\": \"Failed\"", combined, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ScenarioManagerRunsNormalLoopbackThroughProviderNeutralSafeApis()
    {
        var sessions = new List<RecordingSession>();
        Func<ConnectionSettings, ISecsMessageSessionProvider> providerFactory = settings =>
            new RecordingProvider(
                new DreamineSecsCommunicationProvider(_ => settings.ToOptions()),
                sessions);
        var log = new InteropLogManager();
        var constructor = typeof(ScenarioManager).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            [typeof(InteropLogManager), typeof(Func<ConnectionSettings, ISecsMessageSessionProvider>)],
            modifiers: null);
        Assert.NotNull(constructor);
        var manager = (ScenarioManager)constructor!.Invoke([log, providerFactory]);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));

        var summary = await manager.RunSelfLoopbackAsync(4, 1, timeout.Token);

        var failureDetail = string.Join(
            Environment.NewLine,
            manager.Scenarios.Select(value => $"{value.Id}: {value.Status}: {value.Detail}")
                .Concat(log.Entries.Select(value => $"{value.Level}/{value.Category}: {value.Summary} {value.Message}")));
        Assert.True(summary.Result == "Passed", failureDetail);
        Assert.NotEmpty(sessions);
        Assert.All(sessions, session => Assert.NotEqual((ushort)0, session.ConnectionIdentity.SessionId.Value));
        Assert.Equal(0, sessions.Sum(session => session.AllocateSystemBytesCount));
        Assert.Equal(0, sessions.Sum(session => session.ExpertSendCount));
        Assert.Equal(0, sessions.Sum(session => session.ExpertRequestCount));
        Assert.True(sessions.Sum(session => session.SafeRequestCount) >= 4);
        Assert.True(sessions.Sum(session => session.DispatcherRegistrationCount) >= 1);
        Assert.All(sessions, session => Assert.Equal(1, session.DisposeCount));
    }

    [Fact]
    public async Task MultiEquipmentContextUsesInjectedProviderAndSafeSessionSurface()
    {
        var session = new FakeEquipmentSession(new SecsSessionId(23));
        var provider = new SingleSessionProvider(session);
        var sink = new RecordingEquipmentSink();
        Func<EquipmentConnectionDefinition, ISecsMessageSessionProvider> providerFactory = _ => provider;
        var constructor = typeof(EquipmentConnectionContext).GetConstructors(
                BindingFlags.Instance | BindingFlags.NonPublic)
            .SingleOrDefault(candidate =>
                candidate.GetParameters().Length == 5 &&
                candidate.GetParameters()[^1].ParameterType ==
                typeof(Func<EquipmentConnectionDefinition, ISecsMessageSessionProvider>));
        Assert.NotNull(constructor);
        var definition = new EquipmentConnectionDefinition
        {
            EquipmentId = "EQ-PROVIDER",
            Host = "127.0.0.1",
            Port = 7100,
            Mode = SecsConnectionMode.Active,
            SessionId = 23
        };
        var context = (EquipmentConnectionContext)constructor!.Invoke(
            [definition, sink, null, null, providerFactory]);

        await context.ConnectAsync(CancellationToken.None);
        var reply = await context.SendPrimaryAsync(1, 1, null, CancellationToken.None);
        await context.SendAsync(6, 11, null, CancellationToken.None);

        Assert.Equal((ushort)23, reply.SessionId.Value);
        Assert.True(context.IsSelected);
        Assert.Null(context.Session); // Compatibility projection stays concrete-provider-only.
        var messageSession = typeof(EquipmentConnectionContext).GetProperty(
            "MessageSession",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(messageSession);
        Assert.Same(session, messageSession!.GetValue(context));
        Assert.Equal(1, provider.CreateSessionCount);
        Assert.Equal((ushort)23, Assert.Single(provider.Options).Role == SecsRole.Host
            ? session.ConnectionIdentity.SessionId.Value
            : (ushort)0);
        Assert.Equal(1, session.SafeRequestCount);
        Assert.Equal(1, session.SafeSendCount);
        Assert.Equal(0, session.AllocateSystemBytesCount);
        Assert.Equal(0, session.ExpertRequestCount);
        Assert.Equal(0, session.ExpertSendCount);

        await context.DisposeAsync();

        Assert.Equal(1, session.DisposeCount);
        Assert.Contains(sink.Diagnostics, diagnostic =>
            diagnostic.Kind == SecsDiagnosticKind.ConnectionClosed &&
            diagnostic.Message.Contains("terminal", StringComparison.Ordinal));
    }

    private sealed class RecordingProvider(
        ISecsMessageSessionProvider inner,
        List<RecordingSession> sessions) : ISecsMessageSessionProvider
    {
        public string Key => inner.Key;

        public ISecsConnection CreateConnection(SecsConnectionOptions options) => CreateSession(options);

        public ISecsMessageSession CreateSession(SecsConnectionOptions options)
        {
            var session = new RecordingSession(inner.CreateSession(options));
            lock (sessions) sessions.Add(session);
            return session;
        }
    }

    private sealed class RecordingSession : ISecsMessageSession
    {
        private readonly ISecsMessageSession _inner;
        private readonly RecordingDispatcher _dispatcher;

        public RecordingSession(ISecsMessageSession inner)
        {
            _inner = inner;
            _dispatcher = new RecordingDispatcher(inner.PrimaryDispatcher);
        }

        public int AllocateSystemBytesCount { get; private set; }
        public int ExpertSendCount { get; private set; }
        public int ExpertRequestCount { get; private set; }
        public int SafeSendCount { get; private set; }
        public int SafeRequestCount { get; private set; }
        public int DisposeCount { get; private set; }
        public int DispatcherRegistrationCount => _dispatcher.RegistrationCount;
        public string ProviderKey => _inner.ProviderKey;
        public ConnectionState State => _inner.State;
        public SecsConnectionIdentity ConnectionIdentity => _inner.ConnectionIdentity;
        public HsmsConnectionState HsmsState => _inner.HsmsState;
        public ISecsPrimaryDispatcher PrimaryDispatcher => _dispatcher;
        public bool IsWireObservationEnabled => _inner.IsWireObservationEnabled;
        public long DroppedWireObservationCount => _inner.DroppedWireObservationCount;
        public event EventHandler<SecsMessage>? MessageReceived
        {
            add => _inner.MessageReceived += value;
            remove => _inner.MessageReceived -= value;
        }
        public event EventHandler<SecsDiagnosticEvent>? DiagnosticReceived
        {
            add => _inner.DiagnosticReceived += value;
            remove => _inner.DiagnosticReceived -= value;
        }
        public event EventHandler<SecsSessionStateChangedEventArgs>? StateChanged
        {
            add => _inner.StateChanged += value;
            remove => _inner.StateChanged -= value;
        }
        public Task ConnectAsync(CancellationToken cancellationToken = default) => _inner.ConnectAsync(cancellationToken);
        public Task DisconnectAsync(CancellationToken cancellationToken = default) => _inner.DisconnectAsync(cancellationToken);
        public Task SelectAsync(CancellationToken cancellationToken = default) => _inner.SelectAsync(cancellationToken);
        public Task DeselectAsync(CancellationToken cancellationToken = default) => _inner.DeselectAsync(cancellationToken);
        public Task LinktestAsync(CancellationToken cancellationToken = default) => _inner.LinktestAsync(cancellationToken);
        public Task SeparateAsync(CancellationToken cancellationToken = default) => _inner.SeparateAsync(cancellationToken);
        public SecsSystemBytes AllocateSystemBytes()
        {
            AllocateSystemBytesCount++;
            return _inner.AllocateSystemBytes();
        }
        public Task SendAsync(SecsMessage message, CancellationToken cancellationToken = default)
        {
            ExpertSendCount++;
            return _inner.SendAsync(message, cancellationToken);
        }
        public Task<SecsMessage> SendPrimaryAsync(SecsMessage message, CancellationToken cancellationToken = default)
        {
            ExpertRequestCount++;
            return _inner.SendPrimaryAsync(message, cancellationToken);
        }
        public Task SendAsync(SecsStream stream, SecsFunction function, SecsItem? item = null,
            CancellationToken cancellationToken = default)
        {
            SafeSendCount++;
            return _inner.SendAsync(stream, function, item, cancellationToken);
        }
        public Task<SecsMessage> RequestAsync(SecsDialogueDefinition dialogue, SecsItem? item = null,
            CancellationToken cancellationToken = default)
        {
            SafeRequestCount++;
            return _inner.RequestAsync(dialogue, item, cancellationToken);
        }
        public IAsyncEnumerable<HsmsWireObservation> ReadWireObservationsAsync(
            CancellationToken cancellationToken = default) =>
            _inner.ReadWireObservationsAsync(cancellationToken);
        public async ValueTask DisposeAsync()
        {
            DisposeCount++;
            await _inner.DisposeAsync();
        }
    }

    private sealed class RecordingDispatcher(ISecsPrimaryDispatcher inner) : ISecsPrimaryDispatcher
    {
        public int RegistrationCount { get; private set; }
        public long DroppedPrimaryCount => inner.DroppedPrimaryCount;
        public IDisposable Register(
            SecsDialogueDefinition dialogue,
            Func<ISecsPrimaryContext, CancellationToken, ValueTask> handler)
        {
            RegistrationCount++;
            return inner.Register(dialogue, handler);
        }
        public IDisposable RegisterFallback(Func<ISecsPrimaryContext, CancellationToken, ValueTask> handler) =>
            inner.RegisterFallback(handler);
    }

    private sealed class SingleSessionProvider(FakeEquipmentSession session) : ISecsMessageSessionProvider
    {
        public string Key => "fake-equipment";
        public int CreateSessionCount { get; private set; }
        public List<SecsConnectionOptions> Options { get; } = [];
        public ISecsConnection CreateConnection(SecsConnectionOptions options) => CreateSession(options);
        public ISecsMessageSession CreateSession(SecsConnectionOptions options)
        {
            CreateSessionCount++;
            Options.Add(options);
            return session;
        }
    }

    private sealed class FakeEquipmentSession(SecsSessionId sessionId) : ISecsMessageSession
    {
        private readonly Guid _instanceId = Guid.NewGuid();
        private long _epoch;
        private uint _safeSystemBytes;
        public int AllocateSystemBytesCount { get; private set; }
        public int ExpertSendCount { get; private set; }
        public int ExpertRequestCount { get; private set; }
        public int SafeSendCount { get; private set; }
        public int SafeRequestCount { get; private set; }
        public int DisposeCount { get; private set; }
        public string ProviderKey => "fake-equipment";
        public ConnectionState State { get; private set; } = ConnectionState.Disconnected;
        public SecsConnectionIdentity ConnectionIdentity => new(
            ProviderKey, _instanceId, _epoch, sessionId, SecsRole.Host, SecsConnectionMode.Active);
        public HsmsConnectionState HsmsState { get; private set; } = HsmsConnectionState.NotConnected;
        public ISecsPrimaryDispatcher PrimaryDispatcher { get; } = new EmptyDispatcher();
        public bool IsWireObservationEnabled => false;
        public long DroppedWireObservationCount => 0;
        public event EventHandler<SecsMessage>? MessageReceived { add { } remove { } }
        public event EventHandler<SecsDiagnosticEvent>? DiagnosticReceived;
        public event EventHandler<SecsSessionStateChangedEventArgs>? StateChanged;

        public Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var previousState = State;
            var previousHsms = HsmsState;
            State = ConnectionState.Connected;
            HsmsState = HsmsConnectionState.Selected;
            _epoch++;
            StateChanged?.Invoke(this, new SecsSessionStateChangedEventArgs(
                previousState, State, previousHsms, HsmsState, ConnectionIdentity));
            DiagnosticReceived?.Invoke(this, new SecsDiagnosticEvent(
                SecsDiagnosticKind.ConnectionEstablished, "provider-neutral connected", HsmsState));
            return Task.CompletedTask;
        }
        public Task DisconnectAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SelectAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeselectAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LinktestAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SeparateAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public SecsSystemBytes AllocateSystemBytes()
        {
            AllocateSystemBytesCount++;
            return new SecsSystemBytes(0xDEADBEEF);
        }
        public Task SendAsync(SecsMessage message, CancellationToken cancellationToken = default)
        {
            ExpertSendCount++;
            return Task.CompletedTask;
        }
        public Task<SecsMessage> SendPrimaryAsync(SecsMessage message, CancellationToken cancellationToken = default)
        {
            ExpertRequestCount++;
            return Task.FromResult(new SecsMessage(
                message.SessionId, message.Stream, new SecsFunction((byte)(message.Function.Value + 1)),
                false, message.SystemBytes));
        }
        public Task SendAsync(SecsStream stream, SecsFunction function, SecsItem? item = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SafeSendCount++;
            _safeSystemBytes++;
            return Task.CompletedTask;
        }
        public Task<SecsMessage> RequestAsync(SecsDialogueDefinition dialogue, SecsItem? item = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SafeRequestCount++;
            return Task.FromResult(new SecsMessage(
                sessionId, dialogue.Stream, dialogue.SecondaryFunction!.Value, false,
                new SecsSystemBytes(++_safeSystemBytes)));
        }
        public async IAsyncEnumerable<HsmsWireObservation> ReadWireObservationsAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            cancellationToken.ThrowIfCancellationRequested();
            yield break;
        }
        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            var previousState = State;
            var previousHsms = HsmsState;
            State = ConnectionState.Disconnected;
            HsmsState = HsmsConnectionState.NotConnected;
            StateChanged?.Invoke(this, new SecsSessionStateChangedEventArgs(
                previousState, State, previousHsms, HsmsState, ConnectionIdentity));
            DiagnosticReceived?.Invoke(this, new SecsDiagnosticEvent(
                SecsDiagnosticKind.ConnectionClosed, "terminal provider-neutral close", HsmsState));
            return ValueTask.CompletedTask;
        }
    }

    private sealed class EmptyDispatcher : ISecsPrimaryDispatcher
    {
        public long DroppedPrimaryCount => 0;
        public IDisposable Register(
            SecsDialogueDefinition dialogue,
            Func<ISecsPrimaryContext, CancellationToken, ValueTask> handler) => new EmptyDisposable();
        public IDisposable RegisterFallback(Func<ISecsPrimaryContext, CancellationToken, ValueTask> handler) =>
            new EmptyDisposable();
        private sealed class EmptyDisposable : IDisposable { public void Dispose() { } }
    }

    private sealed class RecordingEquipmentSink : IEquipmentEventSink
    {
        public List<SecsDiagnosticEvent> Diagnostics { get; } = [];
        public void Diagnostic(EquipmentLogIdentity identity, SecsDiagnosticEvent value) => Diagnostics.Add(value);
        public void Message(EquipmentLogIdentity identity, string direction, SecsMessage message) { }
        public void Info(EquipmentLogIdentity identity, string category, string summary) { }
        public void Error(EquipmentLogIdentity identity, string category, Exception exception) { }
    }
}
