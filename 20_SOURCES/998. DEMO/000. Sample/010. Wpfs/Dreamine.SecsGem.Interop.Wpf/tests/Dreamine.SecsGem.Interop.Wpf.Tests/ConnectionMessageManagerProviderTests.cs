using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Dreamine.Communication.Abstractions.Enums;
using Dreamine.Secs.Abstractions.Diagnostics;
using Dreamine.Secs.Abstractions.Enums;
using Dreamine.Secs.Abstractions.Hsms;
using Dreamine.Secs.Abstractions.Interfaces;
using Dreamine.Secs.Abstractions.Model;
using Dreamine.Secs.Abstractions.Options;
using Dreamine.SecsGem.Interop.Runtime.Logging;
using Dreamine.SecsGem.Interop.Runtime.Profiles;
using Dreamine.SecsGem.Interop.Wpf.Managers;
using Dreamine.SecsGem.Interop.Wpf.Models;
using Dreamine.Gem.Demo;
using Dreamine.Gem.Protocol.E30;
using Xunit;

namespace Dreamine.SecsGem.Interop.Wpf.Tests;

public sealed class ConnectionMessageManagerProviderTests
{
    [Fact]
    public void DefaultWireLogRootFallsBackWhenDocumentsCannotCreateChildDirectory()
    {
        using var directory = new TempDirectory();
        var blockedDocuments = Path.Combine(directory.Path, "documents-is-a-file");
        var localApplicationData = Path.Combine(directory.Path, "local-app-data");
        File.WriteAllText(blockedDocuments, "blocked");

        var resolved = WireLogManager.ResolveDefaultRootDirectory(
            blockedDocuments,
            localApplicationData);

        Assert.Equal(
            Path.Combine(localApplicationData, "Dreamine", "SecsGemInterop", "Logs"),
            resolved);
        Assert.True(Directory.Exists(resolved));
    }

    [Fact]
    public async Task PassiveConnectReturnsAfterListenerStartsAndWaitsForHost()
    {
        using var directory = new TempDirectory();
        var logRoot = Path.Combine(directory.Path, "not-created-yet");
        var log = new InteropLogManager();
        await using var wireLog = new WireLogManager(log, logRoot);
        await using var manager = new ConnectionManager(log, wireLog);
        var settings = Settings(37);
        settings.Port = ReservePort();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await manager.ConnectAsync(settings, timeout.Token).WaitAsync(timeout.Token);

        Assert.Equal("Listening", manager.TcpState);
        Assert.Equal("NotConnected", manager.HsmsState);
        Assert.True(Directory.Exists(logRoot));
        Assert.NotNull(manager.MessageSession);

        using var host = new TcpClient();
        await host.ConnectAsync(IPAddress.Loopback, settings.Port, timeout.Token);
        await WaitUntilAsync(() => manager.TcpState == "Connected", timeout.Token);

        await manager.DisconnectAsync(timeout.Token);
        Assert.Equal("Disconnected", manager.TcpState);
    }

    [Fact]
    public async Task ConnectionManagerCreatesProviderNeutralSessionAndPreservesConfiguredIdentity()
    {
        var session = new FakeMessageSession(new SecsSessionId(73), SecsRole.Equipment);
        var provider = new FakeMessageSessionProvider(session);
        var log = new InteropLogManager();
        using var directory = new TempDirectory();
        await using var wireLog = new WireLogManager(log, directory.Path);
        await using var manager = new ConnectionManager(log, provider, wireLog);

        await manager.ConnectAsync(Settings(73), CancellationToken.None);

        Assert.Same(session, manager.MessageSession);
        Assert.Same(session, manager.RequireMessageSession());
        Assert.Equal((ushort)73, manager.RequireMessageSession().ConnectionIdentity.SessionId.Value);
        Assert.Equal(SecsRole.Equipment, Assert.Single(provider.RequestedOptions).Role);
        Assert.Equal(SecsConnectionMode.Passive, Assert.Single(provider.RequestedOptions).Mode);
        Assert.Equal(1, session.ConnectCount);
    }

    [Fact]
    public async Task WireCaptureStartsBeforeConnectAndStopsCleanlyAfterSessionDisposal()
    {
        using var directory = new TempDirectory();
        var lifecycle = new List<string>();
        var session = new FakeMessageSession(new SecsSessionId(73), SecsRole.Equipment, "first", lifecycle);
        var provider = new FakeMessageSessionProvider(session);
        var log = new InteropLogManager();
        await using var wireLog = new WireLogManager(log, directory.Path);
        await using var manager = new ConnectionManager(log, provider, wireLog);
        var settings = Settings(73);
        settings.LogPolicyId = ConnectionLogPolicyIds.ExcludedV1;

        await manager.ConnectAsync(settings, CancellationToken.None);
        await manager.DisconnectAsync(CancellationToken.None);

        Assert.NotNull(settings.WireObservation);
        Assert.Equal(HsmsWireCaptureMode.Excluded, settings.WireObservation!.DefaultCaptureMode);
        Assert.True(lifecycle.IndexOf("first:wire-start") < lifecycle.IndexOf("first:connect"));
        Assert.True(lifecycle.IndexOf("first:connect") < lifecycle.IndexOf("first:dispose"));
        Assert.True(wireLog.LastHealth.FlushCompleted);
        Assert.Null(wireLog.LastHealth.WriterFailure);
        Assert.Equal(1, session.DisposeCount);
        Assert.Contains(log.Entries, value =>
            value.Category == "Wire Log" &&
            value.Level == "Info" &&
            value.Summary.Contains("SourceDropped=0", StringComparison.Ordinal) &&
            value.Summary.Contains("FlushCompleted=True", StringComparison.Ordinal));
        var records = new List<WireLogRecord>();
        await foreach (var record in WireLogReader.ReadAsync(Assert.Single(wireLog.FinalizedSegments)))
            records.Add(record);
        Assert.Contains(records, value => value.Kind == WireLogRecordKind.StateTransition);
        Assert.Contains(records, value =>
            value.Kind == WireLogRecordKind.Diagnostic &&
            value.DiagnosticKind == SecsDiagnosticKind.ConnectionEstablished &&
            value.ConnectionEpoch == 1);
    }

    [Fact]
    public async Task TerminalDisposeStateAndDiagnosticAreCapturedBeforeWireLogStops()
    {
        using var directory = new TempDirectory();
        var lifecycle = new List<string>();
        var session = new FakeMessageSession(new SecsSessionId(73), SecsRole.Equipment, "terminal", lifecycle)
        {
            EmitTerminalEventsOnDispose = true
        };
        var log = new InteropLogManager();
        await using var wireLog = new WireLogManager(log, directory.Path);
        await using var manager = new ConnectionManager(
            log,
            new FakeMessageSessionProvider(session),
            wireLog);

        await manager.ConnectAsync(Settings(73), CancellationToken.None);
        await manager.DisconnectAsync(CancellationToken.None);

        var records = new List<WireLogRecord>();
        await foreach (var record in WireLogReader.ReadAsync(Assert.Single(wireLog.FinalizedSegments)))
            records.Add(record);
        Assert.Contains(records, value =>
            value.Kind == WireLogRecordKind.Diagnostic &&
            value.DiagnosticKind == SecsDiagnosticKind.ConnectionClosed);
        Assert.Contains(records, value =>
            value.Kind == WireLogRecordKind.StateTransition &&
            value.CurrentConnectionState == ConnectionState.Disconnected);
        Assert.Contains(log.Entries, value =>
            value.Category == "HSMS" &&
            value.Summary.Contains("terminal", StringComparison.Ordinal));
        Assert.True(lifecycle.IndexOf("terminal:dispose") < lifecycle.IndexOf("terminal:terminal-events"));
        Assert.True(lifecycle.IndexOf("terminal:terminal-events") < lifecycle.IndexOf("terminal:wire-stop"));
    }

    [Fact]
    public async Task UnhealthyWireCaptureIsVisibleAsASeparateErrorWithoutPayloadOrEndpoint()
    {
        using var directory = new TempDirectory();
        var session = new FakeMessageSession(new SecsSessionId(73), SecsRole.Equipment)
        {
            SourceDroppedWireObservationCount = 2
        };
        var log = new InteropLogManager();
        await using var wireLog = new WireLogManager(log, directory.Path);
        await using var manager = new ConnectionManager(log, new FakeMessageSessionProvider(session), wireLog);

        await manager.ConnectAsync(Settings(73), CancellationToken.None);
        await manager.DisconnectAsync(CancellationToken.None);

        var health = Assert.Single(log.Entries, value => value.Category == "Wire Log");
        Assert.Equal("Error", health.Level);
        Assert.Contains("SourceDropped=2", health.Summary, StringComparison.Ordinal);
        Assert.DoesNotContain("127.0.0.1", health.Summary, StringComparison.Ordinal);
        Assert.DoesNotContain("7100", health.Summary, StringComparison.Ordinal);
        Assert.Null(health.RawHex);
    }

    [Fact]
    public async Task ReconnectStopsOldWireSourceBeforeStartingReplacementAndDisposesEachOnce()
    {
        using var directory = new TempDirectory();
        var lifecycle = new List<string>();
        var first = new FakeMessageSession(new SecsSessionId(73), SecsRole.Equipment, "first", lifecycle);
        var second = new FakeMessageSession(new SecsSessionId(73), SecsRole.Equipment, "second", lifecycle);
        var provider = new FakeMessageSessionProvider(first, second);
        var log = new InteropLogManager();
        await using var wireLog = new WireLogManager(log, directory.Path);
        var manager = new ConnectionManager(log, provider, wireLog);

        await manager.ConnectAsync(Settings(73), CancellationToken.None);
        await manager.ConnectAsync(Settings(73), CancellationToken.None);
        await manager.DisposeAsync();
        await manager.DisposeAsync();

        Assert.True(lifecycle.IndexOf("first:dispose") < lifecycle.IndexOf("second:wire-start"));
        Assert.Equal(1, first.DisposeCount);
        Assert.Equal(1, second.DisposeCount);
        Assert.True(wireLog.LastHealth.FlushCompleted);
    }

    [Fact]
    public async Task UserW0UsesSafeSessionApiWithoutExpertAllocationOrSidZero()
    {
        var session = new FakeMessageSession(new SecsSessionId(73), SecsRole.Equipment);
        var log = new InteropLogManager();
        using var directory = new TempDirectory();
        await using var wireLog = new WireLogManager(log, directory.Path);
        await using var connection = await ConnectAsync(session, log, wireLog);
        using var messages = new MessageManager(connection, log);
        var item = new SecsAsciiItem("payload");

        var reply = await messages.SendAsync(3, 5, false, item, CancellationToken.None);

        Assert.Null(reply);
        var sent = Assert.Single(session.SafeW0Messages);
        Assert.Equal((ushort)73, sent.SessionId.Value);
        Assert.Equal((byte)3, sent.Stream.Value);
        Assert.Equal((byte)5, sent.Function.Value);
        Assert.Same(item, sent.Item);
        Assert.Equal(0, session.AllocateSystemBytesCallCount);
        Assert.Equal(0, session.ExpertSendCallCount);
        Assert.Equal(0, session.ExpertRequestCallCount);
    }

    [Fact]
    public async Task UserW1UsesExactSafeDialogueWithoutExpertApis()
    {
        var session = new FakeMessageSession(new SecsSessionId(91), SecsRole.Host);
        var log = new InteropLogManager();
        using var directory = new TempDirectory();
        await using var wireLog = new WireLogManager(log, directory.Path);
        await using var connection = await ConnectAsync(session, log, wireLog);
        using var messages = new MessageManager(connection, log);

        var reply = await messages.SendAsync(2, 17, true, null, CancellationToken.None);

        var dialogue = Assert.Single(session.SafeW1Dialogues);
        Assert.Equal((byte)2, dialogue.Stream.Value);
        Assert.Equal((byte)17, dialogue.PrimaryFunction.Value);
        Assert.Equal((byte)18, dialogue.SecondaryFunction!.Value.Value);
        Assert.NotNull(reply);
        Assert.Equal((ushort)91, reply!.SessionId.Value);
        Assert.Equal((byte)18, reply.Function.Value);
        Assert.Equal(0, session.AllocateSystemBytesCallCount);
        Assert.Equal(0, session.ExpertSendCallCount);
        Assert.Equal(0, session.ExpertRequestCallCount);
    }

    [Fact]
    public async Task ResponderClaimsNothingWhileOffThenRepliesExactlyOnceWhenEnabled()
    {
        var session = new FakeMessageSession(new SecsSessionId(73), SecsRole.Equipment);
        var log = new InteropLogManager();
        using var directory = new TempDirectory();
        await using var wireLog = new WireLogManager(log, directory.Path);
        await using var connection = await ConnectAsync(session, log, wireLog);
        using var messages = new MessageManager(connection, log);
        var primary = Primary(73, 1, 3, 0x1234, new SecsListItem(new SecsInt16Item(1)));

        Assert.Null(await session.Dispatcher.TryDispatchAsync(primary));

        messages.EnableEquipmentResponder();
        Assert.Null(await session.Dispatcher.TryDispatchAsync(Primary(73, 2, 41, 0x9999)));
        var context = Assert.IsType<FakePrimaryContext>(await session.Dispatcher.TryDispatchAsync(primary));

        Assert.Equal(1, context.ReplyCount);
        Assert.Equal((ushort)73, context.ConnectionIdentity.SessionId.Value);
        var body = Assert.IsType<SecsListItem>(context.ReplyBody);
        Assert.Equal("COMMUNICATING", Assert.IsType<SecsAsciiItem>(Assert.Single(body.Items)).Value);
        Assert.Equal(0, session.AllocateSystemBytesCallCount);
        Assert.Equal(0, session.ExpertSendCallCount);
        Assert.Equal(0, session.ExpertRequestCallCount);
        await Assert.ThrowsAsync<InvalidOperationException>(() => context.ReplyAsync().AsTask());

        messages.DisableEquipmentResponder();
        Assert.Null(await session.Dispatcher.TryDispatchAsync(primary));
    }

    [Fact]
    public async Task EducationalResponderAcceptsSessionIdZero()
    {
        var session = new FakeMessageSession(new SecsSessionId(0), SecsRole.Equipment);
        var log = new InteropLogManager();
        using var directory = new TempDirectory();
        await using var wireLog = new WireLogManager(log, directory.Path);
        await using var connection = await ConnectAsync(session, log, wireLog);
        using var messages = new MessageManager(connection, log);

        messages.EnableEquipmentResponder(EquipmentResponderProfileKind.EducationalDemoOnly);

        Assert.Equal(EquipmentResponderProfileKind.EducationalDemoOnly, messages.ActiveEquipmentResponderProfile);
        Assert.Equal(7, session.Dispatcher.RegistrationCount);
        var establish = Assert.IsType<FakePrimaryContext>(await session.Dispatcher.TryDispatchAsync(
            Primary(0, 1, 13, 0x1233, new SecsListItem())));
        Assert.Equal(1, establish.ReplyCount);
        var context = Assert.IsType<FakePrimaryContext>(await session.Dispatcher.TryDispatchAsync(
            Primary(0, 1, 1, 0x1234)));
        Assert.Equal(1, context.ReplyCount);
        Assert.Equal((ushort)0, context.ConnectionIdentity.SessionId.Value);
    }

    [Fact]
    public async Task SelectedE30DemoProfileReplacesEducationalDemoWithoutOverlappingClaims()
    {
        var session = new FakeMessageSession(new SecsSessionId(73), SecsRole.Equipment);
        var log = new InteropLogManager();
        using var directory = new TempDirectory();
        await using var wireLog = new WireLogManager(log, directory.Path);
        await using var connection = await ConnectAsync(session, log, wireLog);
        using var messages = new MessageManager(connection, log);

        messages.EnableEquipmentResponder(EquipmentResponderProfileKind.E30DerivedSubsetDemo);

        Assert.Equal(EquipmentResponderProfileKind.E30DerivedSubsetDemo, messages.ActiveEquipmentResponderProfile);
        Assert.Equal(E30DerivedSubsetManifest.IncludedDialogues.Count - 1, session.Dispatcher.RegistrationCount);
        var e30Context = Assert.IsType<FakePrimaryContext>(await session.Dispatcher.TryDispatchAsync(
            Primary(73, 1, 13, 0x100, new SecsListItem())));
        var e30Reply = Assert.IsType<SecsListItem>(e30Context.ReplyBody);
        Assert.Equal(
            E30DemoEquipmentProfile.Create().Identity.ModelNumber,
            E30WireCodec.ReadCommunicationAcknowledgement(e30Reply).Identity?.ModelNumber);

        messages.EnableEquipmentResponder(EquipmentResponderProfileKind.EducationalDemoOnly);

        Assert.Equal(EquipmentResponderProfileKind.EducationalDemoOnly, messages.ActiveEquipmentResponderProfile);
        Assert.Equal(7, session.Dispatcher.RegistrationCount);
        Assert.Null(await session.Dispatcher.TryDispatchAsync(Primary(73, 2, 41, 0x101)));
        var basicContext = Assert.IsType<FakePrimaryContext>(await session.Dispatcher.TryDispatchAsync(
            Primary(73, 1, 13, 0x102, new SecsListItem())));
        var basicReply = Assert.IsType<SecsListItem>(basicContext.ReplyBody);
        Assert.NotEqual(
            E30DemoEquipmentProfile.Create().Identity.ModelNumber,
            E30WireCodec.ReadCommunicationAcknowledgement(basicReply).Identity?.ModelNumber);
    }

    [Fact]
    public async Task ResponderRegistrationsMoveToReplacementSessionAndDisposeCleanly()
    {
        var first = new FakeMessageSession(new SecsSessionId(73), SecsRole.Equipment);
        var second = new FakeMessageSession(new SecsSessionId(73), SecsRole.Equipment);
        var provider = new FakeMessageSessionProvider(first, second);
        var log = new InteropLogManager();
        using var directory = new TempDirectory();
        await using var wireLog = new WireLogManager(log, directory.Path);
        await using var connection = new ConnectionManager(log, provider, wireLog);
        await connection.ConnectAsync(Settings(73), CancellationToken.None);
        var messages = new MessageManager(connection, log);
        messages.EnableEquipmentResponder();
        Assert.True(first.Dispatcher.HasRegistrations);

        await connection.ConnectAsync(Settings(73), CancellationToken.None);

        Assert.False(first.Dispatcher.HasRegistrations);
        Assert.True(second.Dispatcher.HasRegistrations);
        messages.Dispose();
        Assert.False(second.Dispatcher.HasRegistrations);
    }

    [Fact]
    public async Task SameSessionReconnectRequiresCommunicationEstablishmentForTheNewEpoch()
    {
        var session = new FakeMessageSession(new SecsSessionId(73), SecsRole.Equipment);
        var log = new InteropLogManager();
        using var directory = new TempDirectory();
        await using var wireLog = new WireLogManager(log, directory.Path);
        await using var connection = await ConnectAsync(session, log, wireLog);
        using var messages = new MessageManager(connection, log);
        messages.EnableEquipmentResponder();

        Assert.Equal(1, (await session.Dispatcher.TryDispatchAsync(Primary(73, 1, 13, 1)))!.ReplyCount);
        Assert.Equal(1, (await session.Dispatcher.TryDispatchAsync(Primary(73, 1, 1, 2)))!.ReplyCount);

        await session.ConnectAsync(CancellationToken.None);

        Assert.Equal(0, (await session.Dispatcher.TryDispatchAsync(Primary(73, 1, 1, 3)))!.ReplyCount);
        Assert.Equal(1, (await session.Dispatcher.TryDispatchAsync(Primary(73, 1, 13, 4)))!.ReplyCount);
        Assert.Equal(1, (await session.Dispatcher.TryDispatchAsync(Primary(73, 1, 1, 5)))!.ReplyCount);
    }

    [Fact]
    public async Task MessageManagerDoesNotAddLegacyMessageEventOrFireAndForgetFailurePath()
    {
        var session = new FakeMessageSession(new SecsSessionId(73), SecsRole.Equipment);
        var log = new InteropLogManager();
        using var directory = new TempDirectory();
        await using var wireLog = new WireLogManager(log, directory.Path);
        await using var connection = await ConnectAsync(session, log, wireLog);
        var subscriptionCount = session.MessageReceivedAddCount;
        using var messages = new MessageManager(connection, log);

        Assert.Equal(subscriptionCount, session.MessageReceivedAddCount);
        messages.EnableEquipmentResponder();
        session.Dispatcher.ReplyBehavior = static (_, _) => ValueTask.FromException(
            new InvalidOperationException("simulated responder failure"));

        await session.Dispatcher.TryDispatchAsync(Primary(73, 1, 11, 0x44));

        Assert.Contains(log.Entries, value =>
            value.Category == "Responder" &&
            value.Level == "Error" &&
            value.Summary.Contains("simulated responder failure", StringComparison.Ordinal));
    }

    private static async Task<ConnectionManager> ConnectAsync(
        FakeMessageSession session,
        InteropLogManager log,
        WireLogManager wireLog)
    {
        var manager = new ConnectionManager(log, new FakeMessageSessionProvider(session), wireLog);
        await manager.ConnectAsync(Settings(session.ConnectionIdentity.SessionId.Value), CancellationToken.None);
        return manager;
    }

    private static ConnectionSettings Settings(ushort sessionId) => new()
    {
        Host = "127.0.0.1",
        Port = 7100,
        Role = SecsRole.Equipment,
        Mode = SecsConnectionMode.Passive,
        SessionId = sessionId
    };

    private static int ReservePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static async Task WaitUntilAsync(Func<bool> condition, CancellationToken cancellationToken)
    {
        while (!condition())
            await Task.Delay(10, cancellationToken);
    }

    private static SecsMessage Primary(
        ushort sessionId,
        byte stream,
        byte function,
        uint systemBytes,
        SecsItem? item = null) =>
        new(new SecsSessionId(sessionId), new SecsStream(stream), new SecsFunction(function), true,
            new SecsSystemBytes(systemBytes), item);

    private sealed class FakeMessageSessionProvider(params FakeMessageSession[] sessions) : ISecsMessageSessionProvider
    {
        private readonly Queue<FakeMessageSession> _sessions = new(sessions);
        public string Key => "fake";
        public List<SecsConnectionOptions> RequestedOptions { get; } = [];
        public ISecsConnection CreateConnection(SecsConnectionOptions options) => CreateSession(options);
        public ISecsMessageSession CreateSession(SecsConnectionOptions options)
        {
            RequestedOptions.Add(options);
            return _sessions.Dequeue();
        }
    }

    private sealed class FakeMessageSession : ISecsMessageSession
    {
        private readonly Guid _instanceId = Guid.NewGuid();
        private readonly SecsSessionId _sessionId;
        private readonly SecsRole _role;
        private EventHandler<SecsMessage>? _messageReceived;
        private EventHandler<SecsDiagnosticEvent>? _diagnosticReceived;
        private long _epoch;
        private uint _safeSystemBytes;

        private readonly string _name;
        private readonly List<string>? _lifecycle;
        private readonly Channel<HsmsWireObservation> _wire = Channel.CreateUnbounded<HsmsWireObservation>();

        public FakeMessageSession(
            SecsSessionId sessionId,
            SecsRole role,
            string name = "session",
            List<string>? lifecycle = null)
        {
            _sessionId = sessionId;
            _role = role;
            _name = name;
            _lifecycle = lifecycle;
            Dispatcher.IdentityAccessor = () => ConnectionIdentity;
        }

        public FakePrimaryDispatcher Dispatcher { get; } = new();
        public List<SecsMessage> SafeW0Messages { get; } = [];
        public List<SecsDialogueDefinition> SafeW1Dialogues { get; } = [];
        public int ConnectCount { get; private set; }
        public int DisposeCount { get; private set; }
        public int AllocateSystemBytesCallCount { get; private set; }
        public int ExpertSendCallCount { get; private set; }
        public int ExpertRequestCallCount { get; private set; }
        public int MessageReceivedAddCount { get; private set; }
        public long SourceDroppedWireObservationCount { get; init; }
        public bool EmitTerminalEventsOnDispose { get; init; }
        public string ProviderKey => "fake";
        public ConnectionState State { get; private set; } = ConnectionState.Disconnected;
        public HsmsConnectionState HsmsState { get; private set; } = HsmsConnectionState.NotConnected;
        public SecsConnectionIdentity ConnectionIdentity =>
            new(ProviderKey, _instanceId, _epoch, _sessionId, _role, SecsConnectionMode.Passive);
        public ISecsPrimaryDispatcher PrimaryDispatcher => Dispatcher;
        public bool IsWireObservationEnabled => false;
        public long DroppedWireObservationCount => SourceDroppedWireObservationCount;
        public event EventHandler<SecsMessage>? MessageReceived
        {
            add { MessageReceivedAddCount++; _messageReceived += value; }
            remove { _messageReceived -= value; }
        }
        public event EventHandler<SecsDiagnosticEvent>? DiagnosticReceived
        {
            add => _diagnosticReceived += value;
            remove => _diagnosticReceived -= value;
        }
        public event EventHandler<SecsSessionStateChangedEventArgs>? StateChanged;

        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_lifecycle is not null)
            {
                using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                using var wait = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadline.Token);
                while (!_lifecycle.Contains($"{_name}:wire-start"))
                    await Task.Delay(1, wait.Token);
                _lifecycle.Add($"{_name}:connect");
            }
            ConnectCount++;
            var previousState = State;
            var previousHsms = HsmsState;
            State = ConnectionState.Connected;
            HsmsState = HsmsConnectionState.Selected;
            _epoch++;
            StateChanged?.Invoke(this, new SecsSessionStateChangedEventArgs(
                previousState, State, previousHsms, HsmsState, ConnectionIdentity));
            _diagnosticReceived?.Invoke(this, new SecsDiagnosticEvent(
                SecsDiagnosticKind.ConnectionEstablished,
                "Fake connection established.",
                HsmsState));
        }

        public Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            State = ConnectionState.Disconnected;
            HsmsState = HsmsConnectionState.NotConnected;
            return Task.CompletedTask;
        }

        public Task SelectAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeselectAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LinktestAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SeparateAsync(CancellationToken cancellationToken = default) => DisconnectAsync(cancellationToken);

        public SecsSystemBytes AllocateSystemBytes()
        {
            AllocateSystemBytesCallCount++;
            return new SecsSystemBytes(0xf00d);
        }

        public Task SendAsync(SecsMessage message, CancellationToken cancellationToken = default)
        {
            ExpertSendCallCount++;
            return Task.CompletedTask;
        }

        public Task<SecsMessage> SendPrimaryAsync(SecsMessage message, CancellationToken cancellationToken = default)
        {
            ExpertRequestCallCount++;
            return Task.FromResult(new SecsMessage(
                message.SessionId, message.Stream, new SecsFunction((byte)(message.Function.Value + 1)), false,
                message.SystemBytes));
        }

        public Task SendAsync(
            SecsStream stream,
            SecsFunction function,
            SecsItem? item = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SafeW0Messages.Add(new SecsMessage(
                _sessionId, stream, function, false, new SecsSystemBytes(++_safeSystemBytes), item));
            return Task.CompletedTask;
        }

        public Task<SecsMessage> RequestAsync(
            SecsDialogueDefinition dialogue,
            SecsItem? item = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SafeW1Dialogues.Add(dialogue);
            var systemBytes = new SecsSystemBytes(++_safeSystemBytes);
            return Task.FromResult(new SecsMessage(
                _sessionId, dialogue.Stream, dialogue.SecondaryFunction!.Value, false, systemBytes));
        }

        public async IAsyncEnumerable<HsmsWireObservation> ReadWireObservationsAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            _lifecycle?.Add($"{_name}:wire-start");
            try
            {
                await foreach (var observation in _wire.Reader.ReadAllAsync(cancellationToken))
                    yield return observation;
            }
            finally
            {
                _lifecycle?.Add($"{_name}:wire-stop");
            }
        }

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            _lifecycle?.Add($"{_name}:dispose");
            if (EmitTerminalEventsOnDispose)
            {
                var previousState = State;
                var previousHsms = HsmsState;
                State = ConnectionState.Disconnected;
                HsmsState = HsmsConnectionState.NotConnected;
                StateChanged?.Invoke(this, new SecsSessionStateChangedEventArgs(
                    previousState, State, previousHsms, HsmsState, ConnectionIdentity));
                _diagnosticReceived?.Invoke(this, new SecsDiagnosticEvent(
                    SecsDiagnosticKind.ConnectionClosed,
                    "terminal dispose diagnostic",
                    HsmsState));
                _lifecycle?.Add($"{_name}:terminal-events");
            }
            _wire.Writer.TryComplete();
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FakePrimaryDispatcher : ISecsPrimaryDispatcher
    {
        private readonly object _gate = new();
        private readonly List<Registration> _registrations = [];
        public Func<SecsConnectionIdentity>? IdentityAccessor { get; set; }
        public Func<SecsItem?, CancellationToken, ValueTask>? ReplyBehavior { get; set; }
        public long DroppedPrimaryCount => 0;
        public bool HasRegistrations { get { lock (_gate) return _registrations.Count != 0; } }
        public int RegistrationCount { get { lock (_gate) return _registrations.Count; } }

        public IDisposable Register(
            SecsDialogueDefinition dialogue,
            Func<ISecsPrimaryContext, CancellationToken, ValueTask> handler)
        {
            var registration = new Registration(this, dialogue, handler);
            lock (_gate) _registrations.Add(registration);
            return registration;
        }

        public IDisposable RegisterFallback(Func<ISecsPrimaryContext, CancellationToken, ValueTask> handler) =>
            AddRegistration(null, handler);

        private IDisposable AddRegistration(
            SecsDialogueDefinition? dialogue,
            Func<ISecsPrimaryContext, CancellationToken, ValueTask> handler)
        {
            var registration = new Registration(this, dialogue, handler);
            lock (_gate)
            {
                if (dialogue is null && _registrations.Any(static value => value.Dialogue is null))
                    throw new InvalidOperationException("A fallback is already registered.");
                _registrations.Add(registration);
            }
            return registration;
        }

        public async Task<FakePrimaryContext?> TryDispatchAsync(SecsMessage primary)
        {
            Registration? registration;
            lock (_gate)
                registration = _registrations.SingleOrDefault(value =>
                    value.Dialogue is not null &&
                    value.Dialogue.Stream == primary.Stream &&
                    value.Dialogue.PrimaryFunction == primary.Function) ??
                    _registrations.SingleOrDefault(static value => value.Dialogue is null);
            if (registration is null) return null;
            var dialogue = registration.Dialogue ?? new SecsDialogueDefinition(
                primary.Stream,
                primary.Function,
                primary.ReplyExpected ? new SecsFunction((byte)(primary.Function.Value + 1)) : null);
            var context = new FakePrimaryContext(
                primary,
                dialogue,
                IdentityAccessor?.Invoke() ?? throw new InvalidOperationException("Missing fake identity."),
                ReplyBehavior);
            await registration.Handler(context, registration.Token);
            return context;
        }

        private void Remove(Registration registration)
        {
            lock (_gate) _registrations.Remove(registration);
        }

        private sealed class Registration(
            FakePrimaryDispatcher owner,
            SecsDialogueDefinition? dialogue,
            Func<ISecsPrimaryContext, CancellationToken, ValueTask> handler) : IDisposable
        {
            private readonly CancellationTokenSource _lifetime = new();
            private int _disposed;
            public SecsDialogueDefinition? Dialogue { get; } = dialogue;
            public Func<ISecsPrimaryContext, CancellationToken, ValueTask> Handler { get; } = handler;
            public CancellationToken Token => _lifetime.Token;
            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
                owner.Remove(this);
                _lifetime.Cancel();
                _lifetime.Dispose();
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
            if (Interlocked.Increment(ref _replyCount) != 1)
                throw new InvalidOperationException("Reply ownership was already used.");
            ReplyBody = item;
            if (replyBehavior is not null) await replyBehavior(item, cancellationToken);
            else cancellationToken.ThrowIfCancellationRequested();
        }
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"dreamine-connection-wire-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }
        public void Dispose()
        {
            if (Directory.Exists(Path)) Directory.Delete(Path, recursive: true);
        }
    }
}
