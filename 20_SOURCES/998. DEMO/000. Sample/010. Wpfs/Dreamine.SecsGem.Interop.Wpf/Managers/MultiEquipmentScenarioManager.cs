using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Buffers.Binary;
using Dreamine.Communication.Abstractions.Enums;
using Dreamine.Secs.Abstractions.Enums;
using Dreamine.Secs.Abstractions.Hsms;
using Dreamine.Secs.Abstractions.Model;
using Dreamine.Secs.Com.Hsms;
using Dreamine.SecsGem.Interop.Runtime;
using Dreamine.SecsGem.Interop.Wpf.Models;

namespace Dreamine.SecsGem.Interop.Wpf.Managers;

internal sealed class MultiEquipmentScenarioManager(InteropLogManager log)
{
    internal async Task<MultiEquipmentSelfTestSummary> RunAsync(int reconnectCycles, CancellationToken cancellationToken)
    {
        if (reconnectCycles <= 0) throw new ArgumentOutOfRangeException(nameof(reconnectCycles));
        var startedAt = DateTimeOffset.UtcNow;
        var checks = new List<string>();
        var latencies = new List<double>();
        var connectionsAttempted = 0;
        var messagesRequested = 0;
        var messagesPassed = 0;
        var timeoutCount = 0;
        var reconnectCount = 0;
        var connectionSeconds = 0d;
        var timedConnections = 0;
        var messageBurstSeconds = 0d;
        var connectionTimings = new List<EquipmentScaleTiming>();
        var hosts = new List<MultiEquipmentHost>();
        var memoryBefore = Process.GetCurrentProcess().PrivateMemorySize64;
        var baselineSessions = EquipmentConnectionContext.LiveSessionCount;
        var baselineContextOperations = EquipmentConnectionContext.LiveBackgroundOperationCount;
        var baselinePeerSessions = LoopbackEquipmentPeer.LiveSessionCount;
        var baselinePeerOperations = LoopbackEquipmentPeer.LiveBackgroundOperationCount;

        MultiEquipmentHost CreateHost()
        {
            var host = new MultiEquipmentHost(log.EquipmentSink);
            hosts.Add(host);
            return host;
        }

        try
        {
            foreach (var count in new[] { 1, 2, 10, 50 })
            {
                await using var host = CreateHost();
                await using var fleet = await LoopbackFleet.CreateAsync(host, count, cancellationToken: cancellationToken).ConfigureAwait(false);
                connectionsAttempted += count;
                timedConnections += count;
                connectionSeconds += fleet.ConnectionElapsed.TotalSeconds;
                connectionTimings.Add(new(count, fleet.ConnectionElapsed.TotalMilliseconds));
                var broadcast = await host.BroadcastAsync(1, 1, null, cancellationToken).ConfigureAwait(false);
                messagesRequested += count;
                messagesPassed += broadcast.Count(value => value.Value.Succeeded);
                latencies.AddRange(broadcast.Values.Where(value => value.Succeeded).Select(value => value.Elapsed.TotalMilliseconds));
                Require(broadcast.Count == count && broadcast.Values.All(value => value.Succeeded), $"{count}-equipment S1F1 broadcast failed.");
                if (count == 2)
                {
                    var firstOnly = await host.Get("EQ-001").SendPrimaryAsync(1, 13, new SecsListItem(), cancellationToken)
                        .ConfigureAwait(false);
                    messagesRequested++;
                    messagesPassed++;
                    Require(firstOnly.Function.Value == 14 && host.Get("EQ-001").GemState == "EnabledCommunicating" &&
                        host.Get("EQ-002").GemState == "EnabledNotCommunicating",
                        "Changing one equipment GEM state affected a peer equipment.");
                    checks.Add("Per-equipment GEM state divergence: isolated");
                }
                var communication = await host.BroadcastAsync(1, 13, new SecsListItem(), cancellationToken).ConfigureAwait(false);
                messagesRequested += count;
                messagesPassed += communication.Count(value => value.Value.Succeeded);
                Require(communication.Count == count && communication.Values.All(value => value.Succeeded),
                    $"{count}-equipment S1F13/S1F14 broadcast failed.");
                Require(host.Connections.Select(value => value.GemRuntime).Distinct().Count() == count,
                    $"{count}-equipment GEM runtimes were not independently scoped.");
                Require(host.Connections.All(value => value.GemState == "EnabledCommunicating"),
                    $"{count}-equipment GEM communication states were not independently established.");
                var linktests = await host.LinktestAllAsync(cancellationToken).ConfigureAwait(false);
                Require(linktests.Count == count && linktests.Values.All(value => value.Succeeded), $"{count}-equipment Linktest failed.");
                checks.Add($"Host 1 ↔ Equipment {count}: connected, selected, S1F1/S1F2, S1F13/S1F14, linktested");
                var disconnects = await host.DisconnectAllAsync(cancellationToken).ConfigureAwait(false);
                Require(disconnects.Count == count && disconnects.Values.All(value => value.Succeeded),
                    $"{count}-equipment Disconnect All failed.");
            }

            await using (var host = CreateHost())
            await using (var fleet = await LoopbackFleet.CreateAsync(host, 3, reverseRegistrationOrder: true,
                cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                connectionsAttempted += 3;
                Require(host.Connections.Select(value => value.EquipmentId).SequenceEqual(new[] { "EQ-003", "EQ-002", "EQ-001" }) &&
                    host.Connections.All(value => value.IsSelected), "Reverse equipment registration order was not preserved or connected.");
                var reversed = await host.BroadcastAsync(1, 1, null, cancellationToken).ConfigureAwait(false);
                messagesRequested += 3;
                messagesPassed += reversed.Count(value => value.Value.Succeeded);
                Require(reversed.Values.All(value => value.Succeeded), "Reverse-order equipment broadcast failed.");
                checks.Add("Reverse equipment registration/connect order: isolated and operational");
            }

            await using (var host = CreateHost())
            await using (var fleet = await LoopbackFleet.CreateAsync(host, 10, slowEquipmentIndex: 9,
                cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                connectionsAttempted += 10;
                var burstTimer = Stopwatch.StartNew();
                for (var round = 0; round < 100; round++)
                {
                    var results = await host.BroadcastAsync(1, 1, null, cancellationToken).ConfigureAwait(false);
                    messagesRequested += 10;
                    messagesPassed += results.Count(value => value.Value.Succeeded);
                    latencies.AddRange(results.Values.Where(value => value.Succeeded).Select(value => value.Elapsed.TotalMilliseconds));
                    Require(results.Values.All(value => value.Succeeded), $"Broadcast round {round + 1} failed.");
                }
                burstTimer.Stop();
                messageBurstSeconds = burstTimer.Elapsed.TotalSeconds;
                checks.Add("10 equipment × 100 S1F1/S1F2: 1,000 isolated replies");

                var responseGate = fleet.Peers[9].HoldNextResponse();
                Task<SecsMessage>? slowReply = null;
                Task<SecsMessage>? fastReply = null;
                try
                {
                    slowReply = host.Get("EQ-010").SendPrimaryAsync(1, 1, null, cancellationToken);
                    await responseGate.Entered.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);
                    fastReply = host.Get("EQ-001").SendPrimaryAsync(1, 1, null, cancellationToken);
                    Require(ReferenceEquals(await Task.WhenAny(slowReply, fastReply).ConfigureAwait(false), fastReply),
                        "A held equipment response blocked an independent fast equipment.");
                }
                finally { responseGate.Release(); }
                Require(slowReply is not null && fastReply is not null, "Slow/fast requests were not both created.");
                await Task.WhenAll(slowReply!, fastReply!).ConfigureAwait(false);
                messagesRequested += 2;
                messagesPassed += 2;
                checks.Add("Slow/fast equipment: later fast request completed before the earlier slow request");

                var first = host.Connections[0];
                var second = host.Connections[1];
                var collision = await Task.WhenAll(
                    first.SendPrimaryAsync(1, 1, null, cancellationToken, new SecsSystemBytes(1)),
                    second.SendPrimaryAsync(1, 1, null, cancellationToken, new SecsSystemBytes(1))).ConfigureAwait(false);
                messagesRequested += 2;
                messagesPassed += 2;
                Require(ReadAscii(collision[0]) == first.EquipmentId && ReadAscii(collision[1]) == second.EquipmentId,
                    "Same Session ID/System Bytes collision crossed a connection boundary.");
                Require(first.ConnectionId != second.ConnectionId, "Collision contexts did not have distinct ConnectionIds.");
                checks.Add("SessionId=0 + SystemBytes=1 collision: connection-scoped correlation passed");

                var stableIds = host.Connections.Skip(1).ToDictionary(value => value.EquipmentId, value => value.ConnectionId);
                for (var cycle = 0; cycle < reconnectCycles; cycle++)
                {
                    var previousConnectionId = first.ConnectionId;
                    await fleet.ReconnectAsync(0, cancellationToken).ConfigureAwait(false);
                    reconnectCount++;
                    Require(first.ConnectionId != previousConnectionId, "Reconnect reused its ConnectionId.");
                    var response = await first.SendPrimaryAsync(1, 1, null, cancellationToken).ConfigureAwait(false);
                    Require(ReadAscii(response) == first.EquipmentId, "Reconnected equipment returned an invalid response.");
                    await first.LinktestAsync(cancellationToken).ConfigureAwait(false);
                    messagesRequested++;
                    messagesPassed++;
                }
                foreach (var context in host.Connections.Skip(1))
                {
                    Require(context.IsSelected, $"{context.EquipmentId} lost selection during another equipment reconnect.");
                    Require(context.ConnectionId == stableIds[context.EquipmentId], $"{context.EquipmentId} connection was replaced unexpectedly.");
                }
                await fleet.ReconnectManyAsync(new[] { 0, 1, 2 }, cancellationToken).ConfigureAwait(false);
                reconnectCount += 3;
                var postReconnect = await host.LinktestAllAsync(cancellationToken).ConfigureAwait(false);
                Require(postReconnect.Values.All(value => value.Succeeded), "Concurrent reconnect isolation failed.");
                checks.Add($"One-of-ten reconnect × {reconnectCycles} and three concurrent reconnects: isolated");
            }

            await using (var host = CreateHost())
            await using (var fleet = await LoopbackFleet.CreateAsync(host, 2, noResponseEquipmentIndex: 0,
                slowEquipmentIndex: 1, t3Seconds: 5, cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                connectionsAttempted += 2;
                var disconnected = host.Get("EQ-001");
                var unaffected = host.Get("EQ-002");
                var oldConnectionId = disconnected.ConnectionId;
                var oldRuntime = disconnected.GemRuntime;
                var pending = disconnected.SendPrimaryAsync(1, 1, null, cancellationToken);
                var peerTransaction = unaffected.SendPrimaryAsync(1, 1, null, cancellationToken);
                await Task.Delay(10, cancellationToken).ConfigureAwait(false);
                await disconnected.DisconnectAsync(cancellationToken).ConfigureAwait(false);
                var peerReply = await peerTransaction.ConfigureAwait(false);
                var oldTransactionFailure = await CaptureExceptionAsync(pending).ConfigureAwait(false);
                messagesRequested += 2;
                messagesPassed++;
                Require(ReadAscii(peerReply) == unaffected.EquipmentId && oldTransactionFailure is not null,
                    "Disconnecting one equipment did not isolate its pending transaction from a peer transaction.");
                Require(unaffected.IsSelected && !disconnected.IsConnected,
                    "Selective disconnect changed another equipment state.");
                await unaffected.LinktestAsync(cancellationToken).ConfigureAwait(false);

                await fleet.ReconnectAsync(0, cancellationToken, noResponseOverride: false).ConfigureAwait(false);
                reconnectCount++;
                connectionsAttempted++;
                var freshReply = await disconnected.SendPrimaryAsync(1, 1, null, cancellationToken).ConfigureAwait(false);
                messagesRequested++;
                messagesPassed++;
                Require(ReadAscii(freshReply) == disconnected.EquipmentId &&
                    disconnected.ConnectionId != oldConnectionId && !ReferenceEquals(disconnected.GemRuntime, oldRuntime),
                    "A reconnected equipment inherited its prior transaction, connection, or GEM runtime state.");
                checks.Add("Selective disconnect: pending transaction failed locally while peer traffic continued; reconnect used fresh state");
            }

            await using (var host = CreateHost())
            await using (var fleet = await LoopbackFleet.CreateAsync(host, 2, cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                connectionsAttempted += 2;
                var selected = host.Get("EQ-001");
                var separated = host.Get("EQ-002");
                await separated.Session!.SeparateAsync(cancellationToken).ConfigureAwait(false);
                await WaitUntilAsync(() => !separated.IsSelected, cancellationToken).ConfigureAwait(false);
                await selected.LinktestAsync(cancellationToken).ConfigureAwait(false);
                Require(selected.IsSelected && !separated.IsSelected,
                    "Selected and NotSelected equipment states were not isolated.");
                checks.Add("Selected/NotSelected state: isolated per equipment");
            }

            await using (var host = CreateHost())
            await using (var fleet = await LoopbackFleet.CreateAsync(host, 2, autoReconnectEquipmentIndex: 0,
                cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                connectionsAttempted += 2;
                var target = host.Get("EQ-001");
                var peer = host.Get("EQ-002");
                var oldConnectionId = target.ConnectionId;
                var oldRuntime = target.GemRuntime;
                var peerConnectionId = peer.ConnectionId;
                var connectedNotSelected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                System.ComponentModel.PropertyChangedEventHandler stateObserver = (_, args) =>
                {
                    if (args.PropertyName == nameof(EquipmentConnectionContext.HsmsState) &&
                        target.HsmsState == "ConnectedNotSelected") connectedNotSelected.TrySetResult();
                };
                target.PropertyChanged += stateObserver;
                try
                {
                    await fleet.DropAndRestartForAutoReconnectAsync(0, cancellationToken).ConfigureAwait(false);
                    await connectedNotSelected.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
                    await WaitUntilAsync(() => target.IsSelected, cancellationToken).ConfigureAwait(false);
                }
                finally { target.PropertyChanged -= stateObserver; }
                reconnectCount++;
                connectionsAttempted++;
                Require(target.ConnectionId != oldConnectionId && !ReferenceEquals(target.GemRuntime, oldRuntime),
                    "Automatic reconnect reused prior connection or GEM runtime state.");
                Require(peer.ConnectionId == peerConnectionId && peer.IsSelected,
                    "Automatic reconnect affected the peer equipment.");
                var response = await target.SendPrimaryAsync(1, 1, null, cancellationToken).ConfigureAwait(false);
                await target.LinktestAsync(cancellationToken).ConfigureAwait(false);
                messagesRequested++;
                messagesPassed++;
                Require(ReadAscii(response) == target.EquipmentId, "Automatic reconnect did not restore message correlation.");
                checks.Add("AutoReconnect remote drop: observed ConnectedNotSelected, fresh state, Select recovery, peer isolation");
            }

            await using (var host = CreateHost())
            await using (var fleet = await LoopbackFleet.CreateAsync(host, 2, noResponseEquipmentIndex: 1,
                t3Seconds: 1, cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                connectionsAttempted += 2;
                var results = await host.BroadcastAsync(1, 1, null, cancellationToken).ConfigureAwait(false);
                messagesRequested += 2;
                messagesPassed += results.Count(value => value.Value.Succeeded);
                timeoutCount += results.Count(value => !value.Value.Succeeded);
                Require(results["EQ-001"].Succeeded && !results["EQ-002"].Succeeded,
                    "A non-responsive equipment was not isolated from a responsive equipment.");
                Require(host.Get("EQ-001").LastError == "None" && host.Get("EQ-002").LastError != "None",
                    "LastError crossed an equipment boundary.");
                Require(host.Get("EQ-002").LastError.Contains("T3", StringComparison.OrdinalIgnoreCase),
                    "The non-responsive equipment did not report the injected T3 timeout.");
                await host.Get("EQ-001").LinktestAsync(cancellationToken).ConfigureAwait(false);
                checks.Add("Single-equipment T3 timeout: peer session, LastError and receive loop isolated");
            }

            await using (var host = CreateHost())
            {
                var goodPeer = await LoopbackEquipmentPeer.StartAsync(new EquipmentConnectionDefinition
                {
                    EquipmentId = "EQ-GOOD", Port = ReservePort(), SessionId = 0
                }, cancellationToken: cancellationToken).ConfigureAwait(false);
                await using (goodPeer.ConfigureAwait(false))
                {
                    host.Add(goodPeer.Definition);
                    host.Add(new EquipmentConnectionDefinition
                    {
                        EquipmentId = "EQ-OFFLINE", Port = ReservePort(), SessionId = 0, T3Seconds = 1,
                        T5Seconds = 1, T6Seconds = 1, T7Seconds = 2, T8Seconds = 1
                    });
                    var results = await host.ConnectAllAsync(cancellationToken).ConfigureAwait(false);
                    await goodPeer.ConnectionTask.ConfigureAwait(false);
                    connectionsAttempted += 2;
                    Require(results["EQ-GOOD"].Succeeded && !results["EQ-OFFLINE"].Succeeded,
                        "Connect All did not isolate an unavailable endpoint.");
                    await host.Get("EQ-GOOD").LinktestAsync(cancellationToken).ConfigureAwait(false);
                    checks.Add("Connect failure: available equipment remained selected and operational");
                }
            }

            foreach (var faultMode in Enum.GetValues<RawFaultMode>())
            {
                await using var host = CreateHost();
                var goodDefinition = new EquipmentConnectionDefinition
                {
                    EquipmentId = "EQ-GOOD", Port = ReservePort(), SessionId = 0, T3Seconds = 2,
                    T5Seconds = 1, T6Seconds = 1, T7Seconds = 2, T8Seconds = 1
                };
                await using var goodPeer = await LoopbackEquipmentPeer.StartAsync(goodDefinition,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
                await using var faultPeer = new RawFaultPeer(faultMode);
                host.Add(goodDefinition);
                host.Add(new EquipmentConnectionDefinition
                {
                    EquipmentId = "EQ-FAULT", Port = faultPeer.Port, SessionId = 0, T3Seconds = 2,
                    T5Seconds = 1, T6Seconds = 1, T7Seconds = 2, T8Seconds = 1
                });
                var connectResults = await host.ConnectAllAsync(cancellationToken).ConfigureAwait(false);
                await goodPeer.ConnectionTask.ConfigureAwait(false);
                connectionsAttempted += 2;
                if (faultMode == RawFaultMode.T6Timeout)
                    Require(!connectResults["EQ-FAULT"].Succeeded &&
                        connectResults["EQ-FAULT"].Detail.Contains("T6", StringComparison.OrdinalIgnoreCase),
                        "T6 timeout peer unexpectedly selected or reported the wrong failure.");
                else
                    await WaitUntilAsync(() => host.Get("EQ-FAULT").TcpState is "Faulted" or "Disconnected",
                        cancellationToken).ConfigureAwait(false);
                if (faultMode == RawFaultMode.T8PartialFrame)
                    Require(host.Get("EQ-FAULT").LastError.Contains("T8", StringComparison.OrdinalIgnoreCase),
                        "T8 partial-frame peer did not report a T8 timeout.");
                if (faultMode == RawFaultMode.MalformedFrame)
                    Require(host.Get("EQ-FAULT").LastError != "None",
                        "Malformed-frame peer did not report a protocol error.");
                await host.Get("EQ-GOOD").LinktestAsync(cancellationToken).ConfigureAwait(false);
                Require(host.Get("EQ-GOOD").IsSelected, $"{faultMode} affected the healthy equipment.");
                timeoutCount += faultMode is RawFaultMode.T6Timeout or RawFaultMode.T8PartialFrame ? 1 : 0;
                checks.Add($"{faultMode}: healthy equipment remained selected and operational");
            }

            await using (var host = CreateHost())
            await using (var fleet = await LoopbackFleet.CreateAsync(host, 2, cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                connectionsAttempted += 2;
                var callbackObserved = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                EventHandler<SecsMessage> throwingHandler = (_, _) =>
                {
                    callbackObserved.TrySetResult();
                    throw new InvalidOperationException("Injected callback failure.");
                };
                host.Get("EQ-002").Session!.MessageReceived += throwingHandler;
                await fleet.Peers[1].SendOneWayAsync(6, 11, new SecsAsciiItem("CALLBACK-FAULT"), cancellationToken).ConfigureAwait(false);
                await callbackObserved.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);
                host.Get("EQ-002").Session!.MessageReceived -= throwingHandler;
                await WaitUntilAsync(() => host.Get("EQ-002").LastError.Contains("Injected callback failure", StringComparison.Ordinal),
                    cancellationToken).ConfigureAwait(false);

                var large = new SecsBinaryItem(new byte[1024 * 1024]);
                var largeReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                EventHandler<SecsMessage> largeHandler = (_, message) =>
                {
                    if (message.Item is SecsBinaryItem binary && binary.Values.Length == 1024 * 1024)
                        largeReceived.TrySetResult();
                };
                host.Get("EQ-002").Session!.MessageReceived += largeHandler;
                await fleet.Peers[1].SendOneWayAsync(6, 11, large, cancellationToken).ConfigureAwait(false);
                await largeReceived.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
                host.Get("EQ-002").Session!.MessageReceived -= largeHandler;
                messagesRequested++;
                messagesPassed++;
                await host.Get("EQ-001").LinktestAsync(cancellationToken).ConfigureAwait(false);
                checks.Add("Callback exception and Equipment→Host 1 MiB message: peer equipment remained operational");
            }
        }
        catch (Exception exception)
        {
            log.Error("MultiEquipmentSelfTest", exception);
            checks.Add($"FAILED: {exception.Message}");
            return Build("Failed");
        }

        return Build("Passed");

        MultiEquipmentSelfTestSummary Build(string result)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var completedAt = DateTimeOffset.UtcNow;
            latencies.Sort();
            var memoryDelta = Process.GetCurrentProcess().PrivateMemorySize64 - memoryBefore;
            var remainingSessions = Math.Max(0, EquipmentConnectionContext.LiveSessionCount - baselineSessions) +
                Math.Max(0, LoopbackEquipmentPeer.LiveSessionCount - baselinePeerSessions);
            var remainingOperations = Math.Max(0,
                EquipmentConnectionContext.LiveBackgroundOperationCount - baselineContextOperations) +
                Math.Max(0, LoopbackEquipmentPeer.LiveBackgroundOperationCount - baselinePeerOperations) +
                hosts.Sum(value => value.BackgroundOperationCount);
            var trackedOperationTasks = hosts.Sum(value => value.ScheduledEquipmentOperationCount);
            var peakOperations = hosts.Count == 0 ? 0 : hosts.Max(value => value.PeakEquipmentOperationCount);
            if (remainingSessions != 0 || remainingOperations != 0)
            {
                result = "Failed";
                checks.Add($"FAILED: cleanup left {remainingSessions} session(s) and {remainingOperations} tracked operation(s).");
            }
            return new(startedAt, completedAt, 50, connectionsAttempted, messagesRequested, messagesPassed,
                timeoutCount, reconnectCount, memoryDelta, timedConnections / Math.Max(connectionSeconds, .001),
                1000 / Math.Max(messageBurstSeconds, .001), latencies.Count == 0 ? 0 : latencies[0],
                latencies.Count == 0 ? 0 : latencies.Average(), latencies.Count == 0 ? 0 : latencies[^1],
                remainingSessions, remainingOperations, trackedOperationTasks, peakOperations,
                connectionTimings, result, checks);
        }
    }

    private static string? ReadAscii(SecsMessage message) => message.Item is SecsAsciiItem ascii ? ascii.Value : null;
    private static async Task<Exception?> CaptureExceptionAsync(Task task)
    {
        try { await task.ConfigureAwait(false); return null; }
        catch (Exception exception) { return exception; }
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    internal sealed class LoopbackFleet : IAsyncDisposable
    {
        private readonly MultiEquipmentHost _host;
        private readonly List<LoopbackEquipmentPeer> _peers;
        private readonly int? _noResponseEquipmentIndex;
        private readonly int? _slowEquipmentIndex;
        private readonly int _t3Seconds;

        private LoopbackFleet(MultiEquipmentHost host, List<LoopbackEquipmentPeer> peers, TimeSpan connectionElapsed,
            int? noResponseEquipmentIndex, int? slowEquipmentIndex, int t3Seconds)
        {
            _host = host; _peers = peers; ConnectionElapsed = connectionElapsed;
            _noResponseEquipmentIndex = noResponseEquipmentIndex; _slowEquipmentIndex = slowEquipmentIndex;
            _t3Seconds = t3Seconds;
        }

        internal TimeSpan ConnectionElapsed { get; }
        internal IReadOnlyList<LoopbackEquipmentPeer> Peers => _peers;

        internal static async Task<LoopbackFleet> CreateAsync(MultiEquipmentHost host, int count,
            int? noResponseEquipmentIndex = null, int? slowEquipmentIndex = null, int t3Seconds = 3,
            int? autoReconnectEquipmentIndex = null, bool reverseRegistrationOrder = false,
            CancellationToken cancellationToken = default)
        {
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
            var peers = new List<LoopbackEquipmentPeer>(count);
            try
            {
                var registrationOrder = Enumerable.Range(0, count);
                if (reverseRegistrationOrder) registrationOrder = registrationOrder.Reverse();
                foreach (var index in registrationOrder)
                {
                    var definition = new EquipmentConnectionDefinition
                    {
                        EquipmentId = $"EQ-{index + 1:000}", Host = "127.0.0.1", Port = ReservePort(),
                        Mode = SecsConnectionMode.Active, SessionId = 0, T3Seconds = t3Seconds,
                        T5Seconds = 1, T6Seconds = 2, T7Seconds = 3, T8Seconds = 2,
                        AutoReconnect = index == autoReconnectEquipmentIndex
                    };
                    host.Add(definition);
                    peers.Add(await LoopbackEquipmentPeer.StartAsync(definition,
                        noResponse: index == noResponseEquipmentIndex, slow: index == slowEquipmentIndex,
                        cancellationToken).ConfigureAwait(false));
                }

                var timer = Stopwatch.StartNew();
                var results = await host.ConnectAllAsync(cancellationToken).ConfigureAwait(false);
                await Task.WhenAll(peers.Select(value => value.ConnectionTask)).ConfigureAwait(false);
                timer.Stop();
                Require(results.Count == count && results.Values.All(value => value.Succeeded),
                    $"Only {results.Count(value => value.Value.Succeeded)}/{count} equipment connections succeeded.");
                return new(host, peers, timer.Elapsed, noResponseEquipmentIndex, slowEquipmentIndex, t3Seconds);
            }
            catch (Exception failure)
            {
                var cleanupFailures = await DisposePeersAsync(peers).ConfigureAwait(false);
                if (cleanupFailures.Count > 0)
                    throw new AggregateException("Fleet creation failed and peer cleanup also reported failures.", [failure, .. cleanupFailures]);
                throw;
            }
        }

        internal Task ReconnectAsync(int index, CancellationToken cancellationToken, bool? noResponseOverride = null) =>
            ReconnectCoreAsync(index, cancellationToken, noResponseOverride);

        internal Task ReconnectManyAsync(IEnumerable<int> indices, CancellationToken cancellationToken) =>
            Task.WhenAll(indices.Select(index => ReconnectCoreAsync(index, cancellationToken)));

        internal async Task DropAndRestartForAutoReconnectAsync(int index, CancellationToken cancellationToken)
        {
            var definition = _host.Connections[index].Definition;
            if (!definition.AutoReconnect) throw new InvalidOperationException($"{definition.EquipmentId} does not enable AutoReconnect.");
            await _peers[index].DisposeAsync().ConfigureAwait(false);
            await WaitUntilAsync(() => !_host.Connections[index].IsConnected, cancellationToken).ConfigureAwait(false);
            var peer = await LoopbackEquipmentPeer.StartAsync(definition,
                noResponse: index == _noResponseEquipmentIndex, slow: index == _slowEquipmentIndex,
                cancellationToken).ConfigureAwait(false);
            _peers[index] = peer;
            await peer.ConnectionTask.ConfigureAwait(false);
        }

        private async Task ReconnectCoreAsync(int index, CancellationToken cancellationToken, bool? noResponseOverride = null)
        {
            var context = _host.Connections[index];
            await context.DisconnectAsync(cancellationToken).ConfigureAwait(false);
            await _peers[index].DisposeAsync().ConfigureAwait(false);
            var peer = await LoopbackEquipmentPeer.StartAsync(context.Definition,
                noResponse: noResponseOverride ?? index == _noResponseEquipmentIndex, slow: index == _slowEquipmentIndex,
                cancellationToken).ConfigureAwait(false);
            _peers[index] = peer;
            await context.ConnectAsync(cancellationToken).ConfigureAwait(false);
            await peer.ConnectionTask.ConfigureAwait(false);
        }

        public async ValueTask DisposeAsync()
        {
            var failures = new List<Exception>();
            try { await _host.DisconnectAllAsync(CancellationToken.None).ConfigureAwait(false); }
            catch (ObjectDisposedException) { }
            catch (Exception exception) { failures.Add(exception); }
            failures.AddRange(await DisposePeersAsync(_peers).ConfigureAwait(false));
            if (failures.Count > 0) throw new AggregateException("Loopback fleet cleanup reported failures.", failures);
        }

        private static async Task<List<Exception>> DisposePeersAsync(IEnumerable<LoopbackEquipmentPeer> peers)
        {
            var failures = new List<Exception>();
            foreach (var peer in peers)
            {
                try { await peer.DisposeAsync().ConfigureAwait(false); }
                catch (Exception exception) { failures.Add(exception); }
            }
            return failures;
        }
    }

    internal sealed class LoopbackEquipmentPeer : IAsyncDisposable
    {
        private readonly HsmsSession _session;
        private readonly ConcurrentDictionary<int, Task> _responses = new();
        private readonly bool _noResponse;
        private readonly bool _slow;
        private readonly object _responseGateSync = new();
        private ResponseGate? _nextResponseGate;
        private static int _liveSessions;
        private static int _liveBackgroundOperations;
        private int _responseId;
        private int _disposed;

        private LoopbackEquipmentPeer(EquipmentConnectionDefinition definition, HsmsSession session,
            Task connectionTask, bool noResponse, bool slow)
        {
            Definition = definition; _session = session; ConnectionTask = connectionTask;
            _noResponse = noResponse; _slow = slow;
            Interlocked.Increment(ref _liveSessions);
            _session.MessageReceived += OnMessageReceived;
        }

        internal static int LiveSessionCount => Volatile.Read(ref _liveSessions);
        internal static int LiveBackgroundOperationCount => Volatile.Read(ref _liveBackgroundOperations);
        internal EquipmentConnectionDefinition Definition { get; }
        internal Task ConnectionTask { get; }

        internal ResponseGate HoldNextResponse()
        {
            lock (_responseGateSync)
            {
                if (_nextResponseGate is not null) throw new InvalidOperationException("A response is already held.");
                return _nextResponseGate = new ResponseGate();
            }
        }

        internal Task SendOneWayAsync(byte stream, byte function, SecsItem? item, CancellationToken cancellationToken)
        {
            var message = new SecsMessage(new SecsSessionId(Definition.SessionId), new SecsStream(stream),
                new SecsFunction(function), false, _session.AllocateSystemBytes(), item);
            return _session.SendAsync(message, cancellationToken);
        }

        internal static async Task<LoopbackEquipmentPeer> StartAsync(EquipmentConnectionDefinition definition,
            bool noResponse = false, bool slow = false, CancellationToken cancellationToken = default)
        {
            var session = new HsmsSession(new HsmsSessionOptions
            {
                Host = definition.Host, Port = definition.Port, Mode = SecsConnectionMode.Passive,
                Role = SecsRole.Equipment, SessionId = new SecsSessionId(definition.SessionId),
                Timers = new HsmsTimerOptions
                {
                    T3 = TimeSpan.FromSeconds(definition.T3Seconds), T5 = TimeSpan.FromSeconds(definition.T5Seconds),
                    T6 = TimeSpan.FromSeconds(definition.T6Seconds), T7 = TimeSpan.FromSeconds(definition.T7Seconds),
                    T8 = TimeSpan.FromSeconds(definition.T8Seconds)
                }
            });
            var connectTask = session.ConnectAsync(cancellationToken);
            try
            {
                await WaitUntilAsync(() => session.State == ConnectionState.Listening, cancellationToken).ConfigureAwait(false);
                return new(definition, session, connectTask, noResponse, slow);
            }
            catch
            {
                await session.DisposeAsync().ConfigureAwait(false);
                throw;
            }
        }

        private void OnMessageReceived(object? sender, SecsMessage message)
        {
            if (!message.Function.IsPrimary || !message.ReplyExpected || _noResponse) return;
            var id = Interlocked.Increment(ref _responseId);
            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _responses[id] = completion.Task;
            _ = RespondTrackedAsync(id, message, completion);
        }

        private async Task RespondTrackedAsync(int id, SecsMessage message, TaskCompletionSource completion)
        {
            Interlocked.Increment(ref _liveBackgroundOperations);
            try
            {
                await RespondAsync(message).ConfigureAwait(false);
                completion.TrySetResult();
            }
            catch (Exception exception) { completion.TrySetException(exception); }
            finally
            {
                Interlocked.Decrement(ref _liveBackgroundOperations);
                _responses.TryRemove(id, out _);
            }
        }

        private async Task RespondAsync(SecsMessage request)
        {
            ResponseGate? responseGate;
            lock (_responseGateSync)
            {
                responseGate = _nextResponseGate;
                _nextResponseGate = null;
            }
            if (responseGate is not null)
            {
                responseGate.MarkEntered();
                await responseGate.WaitForReleaseAsync().ConfigureAwait(false);
            }
            else if (_slow) await Task.Delay(50).ConfigureAwait(false);
            SecsItem item = request.Stream.Value == 1 && request.Function.Value == 13
                ? new SecsListItem(new SecsBinaryItem(0), new SecsListItem(
                    new SecsAsciiItem(Definition.EquipmentId), new SecsAsciiItem("0.1")))
                : new SecsAsciiItem(Definition.EquipmentId);
            var response = new SecsMessage(request.SessionId, request.Stream,
                new SecsFunction((byte)(request.Function.Value + 1)), false, request.SystemBytes, item);
            await _session.SendAsync(response).ConfigureAwait(false);
        }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            lock (_responseGateSync) _nextResponseGate?.Release();
            _session.MessageReceived -= OnMessageReceived;
            await _session.DisposeAsync().ConfigureAwait(false);
            Interlocked.Decrement(ref _liveSessions);
            var responses = _responses.Values.ToArray();
            if (responses.Length > 0)
            {
                try { await Task.WhenAll(responses).ConfigureAwait(false); }
                catch (Exception) { }
            }
        }

        internal sealed class ResponseGate
        {
            private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
            private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
            internal Task Entered => _entered.Task;
            internal void MarkEntered() => _entered.TrySetResult();
            internal void Release() => _release.TrySetResult();
            internal Task WaitForReleaseAsync() => _release.Task;
        }
    }

    internal enum RawFaultMode
    {
        T6Timeout,
        T8PartialFrame,
        MalformedFrame,
        ConnectionClosedAfterSelect
    }

    internal sealed class RawFaultPeer : IAsyncDisposable
    {
        private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
        private readonly CancellationTokenSource _cancellation = new();
        private readonly Task _runTask;
        private TcpClient? _client;

        internal RawFaultPeer(RawFaultMode mode)
        {
            _listener.Start();
            Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
            _runTask = RunAsync(mode, _cancellation.Token);
        }

        internal int Port { get; }

        private async Task RunAsync(RawFaultMode mode, CancellationToken cancellationToken)
        {
            try
            {
                _client = await _listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
                var stream = _client.GetStream();
                var selectRequest = await ReadFrameAsync(stream, cancellationToken).ConfigureAwait(false);
                if (mode == RawFaultMode.T6Timeout)
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
                    return;
                }

                var selectResponse = selectRequest.ToArray();
                if (selectResponse.Length < 14) throw new InvalidDataException("Select request was shorter than an HSMS control frame.");
                selectResponse[9] = 2;
                await stream.WriteAsync(selectResponse, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                await Task.Delay(25, cancellationToken).ConfigureAwait(false);

                if (mode == RawFaultMode.ConnectionClosedAfterSelect)
                {
                    _client.Dispose();
                    return;
                }

                if (mode == RawFaultMode.T8PartialFrame)
                {
                    await stream.WriteAsync(new byte[] { 0 }, cancellationToken).ConfigureAwait(false);
                    await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                    await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await stream.WriteAsync(new byte[] { 0, 0, 0, 1, 0 }, cancellationToken).ConfigureAwait(false);
                    await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
            catch (IOException) { }
            catch (SocketException) { }
        }

        private static async Task<byte[]> ReadFrameAsync(NetworkStream stream, CancellationToken cancellationToken)
        {
            var prefix = new byte[4];
            await stream.ReadExactlyAsync(prefix, cancellationToken).ConfigureAwait(false);
            var length = BinaryPrimitives.ReadInt32BigEndian(prefix);
            if (length is < 10 or > 1024) throw new InvalidDataException($"Unexpected HSMS control length {length}.");
            var frame = new byte[length + 4];
            prefix.CopyTo(frame, 0);
            await stream.ReadExactlyAsync(frame.AsMemory(4, length), cancellationToken).ConfigureAwait(false);
            return frame;
        }

        public async ValueTask DisposeAsync()
        {
            _cancellation.Cancel();
            _client?.Dispose();
            _listener.Stop();
            try { await _runTask.ConfigureAwait(false); } catch (Exception) { }
            _cancellation.Dispose();
        }
    }

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
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        while (!condition()) await Task.Delay(5, timeout.Token).ConfigureAwait(false);
    }
}
