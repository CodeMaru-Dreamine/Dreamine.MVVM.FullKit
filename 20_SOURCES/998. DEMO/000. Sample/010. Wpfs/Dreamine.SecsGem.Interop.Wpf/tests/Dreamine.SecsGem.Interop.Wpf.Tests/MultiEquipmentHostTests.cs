using System.Net;
using System.Net.Sockets;
using System.IO;
using Dreamine.Secs.Abstractions.Enums;
using Dreamine.Secs.Abstractions.Model;
using Dreamine.SecsGem.Interop.Runtime;
using Dreamine.SecsGem.Interop.Wpf.Managers;
using Dreamine.SecsGem.Interop.Wpf.Models;
using Xunit;

namespace Dreamine.SecsGem.Interop.Wpf.Tests;

public sealed class MultiEquipmentHostTests
{
    [Fact]
    public void OptionsRejectUnboundedOrZeroConcurrency()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MultiEquipmentHostOptions { ConnectConcurrency = 0 }.Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => new MultiEquipmentHostOptions { MessageConcurrency = 0 }.Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => new MultiEquipmentHostOptions { ReconnectConcurrency = 0 }.Validate());
        new MultiEquipmentHostOptions { ConnectConcurrency = 1, MessageConcurrency = 2, ReconnectConcurrency = 3 }.Validate();
    }

    [Fact]
    public void DefinitionsRejectValuesThatCoreCannotAccept()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => WithSessionId(32768).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => new EquipmentConnectionDefinition
        {
            EquipmentId = "EQ-MODE", Host = "127.0.0.1", Port = ReservePort(), Mode = SecsConnectionMode.Unspecified
        }.Validate());
        Assert.Throws<ArgumentException>(() => new EquipmentConnectionDefinition
        {
            EquipmentId = "EQ-RECONNECT", Host = "127.0.0.1", Port = ReservePort(), Mode = SecsConnectionMode.Passive,
            AutoReconnect = true
        }.Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => new EquipmentConnectionDefinition
        {
            EquipmentId = "EQ-TIMER", Host = "127.0.0.1", Port = ReservePort(), T8Seconds = 121
        }.Validate());

        static EquipmentConnectionDefinition WithSessionId(ushort sessionId) => new()
        {
            EquipmentId = "EQ-SID", Host = "127.0.0.1", Port = 7101, SessionId = sessionId
        };
    }

    [Fact]
    public async Task BoundedOperationsRespectInjectedConcurrency()
    {
        var log = new InteropLogManager();
        await using var host = new MultiEquipmentHost(
            new EquipmentConnectionRegistry(new EquipmentSessionFactory(log.EquipmentSink)),
            new MultiEquipmentHostOptions { ConnectConcurrency = 2, MessageConcurrency = 3, ReconnectConcurrency = 1 });
        for (var index = 0; index < 8; index++) host.Add(Definition($"EQ-{index + 1:000}", ReservePort()));
        var active = 0;
        var maximum = 0;
        var maximumGate = new object();

        var results = await host.RunBoundedAsync(host.Connections, host.Options.MessageConcurrency, async (_, token) =>
        {
            var current = Interlocked.Increment(ref active);
            lock (maximumGate) maximum = Math.Max(maximum, current);
            try { await Task.Delay(40, token); }
            finally { Interlocked.Decrement(ref active); }
        }, CancellationToken.None);

        Assert.Equal(8, results.Count);
        Assert.Equal(3, maximum);
        Assert.Equal(3, host.PeakEquipmentOperationCount);
        Assert.Equal(8, host.ScheduledEquipmentOperationCount);

        active = 0;
        maximum = 0;
        await host.RunBoundedAsync(host.Connections, host.Options.ReconnectConcurrency, async (_, token) =>
        {
            var current = Interlocked.Increment(ref active);
            lock (maximumGate) maximum = Math.Max(maximum, current);
            try { await Task.Delay(15, token); }
            finally { Interlocked.Decrement(ref active); }
        }, CancellationToken.None);
        Assert.Equal(1, maximum);
        Assert.Equal(16, host.ScheduledEquipmentOperationCount);
    }

    [Fact]
    public async Task ConfigurationRoundTripContainsOnlyConnectionSettings()
    {
        var path = Path.Combine(Path.GetTempPath(), $"dreamine-multi-{Guid.NewGuid():N}.json");
        try
        {
            var log = new InteropLogManager();
            await using var source = new MultiEquipmentHost(new EquipmentConnectionRegistry(new EquipmentSessionFactory(log.EquipmentSink)),
                new MultiEquipmentHostOptions { ConnectConcurrency = 4, MessageConcurrency = 7, ReconnectConcurrency = 2 });
            source.Add(new EquipmentConnectionDefinition
            {
                EquipmentId = "EQ-A", Host = "127.0.0.1", Port = ReservePort(), Mode = SecsConnectionMode.Active,
                SessionId = 7, AutoReconnect = true, T3Seconds = 11, T5Seconds = 12, T6Seconds = 13,
                T7Seconds = 14, T8Seconds = 15
            });
            source.Add(new EquipmentConnectionDefinition
            {
                EquipmentId = "EQ-B", Host = "0.0.0.0", Port = ReservePort(), Mode = SecsConnectionMode.Passive,
                SessionId = 8, T3Seconds = 21, T5Seconds = 22, T6Seconds = 23, T7Seconds = 24, T8Seconds = 25
            });
            await source.ExportConfigurationAsync(path, CancellationToken.None);
            var json = await File.ReadAllTextAsync(path);
            Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("credential", json, StringComparison.OrdinalIgnoreCase);

            var targetLog = new InteropLogManager();
            await using (var incompatible = new MultiEquipmentHost(targetLog.EquipmentSink))
            {
                await Assert.ThrowsAsync<InvalidDataException>(() =>
                    incompatible.ImportConfigurationAsync(path, CancellationToken.None));
                Assert.Empty(incompatible.Connections);
            }

            await using var target = new MultiEquipmentHost(targetLog.EquipmentSink, new MultiEquipmentHostOptions
            {
                ReconnectConcurrency = 2,
                ReconnectQueueCapacity = 4096
            });
            await target.ImportConfigurationAsync(path, CancellationToken.None);
            Assert.Equal(new[] { "EQ-A", "EQ-B" }, target.Connections.Select(value => value.EquipmentId));
            Assert.Equal((4, 7, 2), (target.Options.ConnectConcurrency, target.Options.MessageConcurrency,
                target.Options.ReconnectConcurrency));
            var restored = target.Get("EQ-A").Definition;
            Assert.Equal(("127.0.0.1", SecsConnectionMode.Active, (ushort)7, true),
                (restored.Host, restored.Mode, restored.SessionId, restored.AutoReconnect));
            Assert.Equal((11, 12, 13, 14, 15),
                (restored.T3Seconds, restored.T5Seconds, restored.T6Seconds, restored.T7Seconds, restored.T8Seconds));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public async Task FiftyEquipmentConnectSelectMessageAndDisposeWithoutAggregateLeak()
    {
        var log = new InteropLogManager();
        var host = new MultiEquipmentHost(log.EquipmentSink);
        var fleet = await MultiEquipmentScenarioManager.LoopbackFleet.CreateAsync(host, 50);
        try
        {
            Assert.Equal(50, host.ActiveSessionCount);
            Assert.All(host.Connections, context => Assert.True(context.IsSelected));
            var results = await host.BroadcastAsync(1, 1, null, CancellationToken.None);
            Assert.Equal(50, results.Count);
            Assert.All(results.Values, result => Assert.True(result.Succeeded, result.Detail));
            await host.DisposeAsync();
            Assert.Equal(0, host.ActiveSessionCount);
            Assert.Equal(0, host.BackgroundOperationCount);
        }
        finally { await fleet.DisposeAsync(); }
        await host.DisposeAsync();
        Assert.Equal(0, host.BackgroundOperationCount);
    }

    [Fact]
    public async Task SameSessionAndSystemBytesAreCorrelatedByConnection()
    {
        var log = new InteropLogManager();
        await using var host = new MultiEquipmentHost(log.EquipmentSink);
        await using var fleet = await MultiEquipmentScenarioManager.LoopbackFleet.CreateAsync(host, 2);
        var first = host.Connections[0];
        var second = host.Connections[1];

        var replies = await Task.WhenAll(
            first.SendPrimaryAsync(1, 1, null, CancellationToken.None, new SecsSystemBytes(1)),
            second.SendPrimaryAsync(1, 1, null, CancellationToken.None, new SecsSystemBytes(1)));

        Assert.Equal("EQ-001", Assert.IsType<SecsAsciiItem>(replies[0].Item).Value);
        Assert.Equal("EQ-002", Assert.IsType<SecsAsciiItem>(replies[1].Item).Value);
        Assert.NotEqual(first.ConnectionId, second.ConnectionId);
        var collisionLogs = log.Entries.Where(value => value.CorrelationSystemBytes == 1 && value.Category == "SECS-II").ToArray();
        Assert.Contains(collisionLogs, value => value.EquipmentId == "EQ-001" && value.ConnectionId == first.ConnectionId);
        Assert.Contains(collisionLogs, value => value.EquipmentId == "EQ-002" && value.ConnectionId == second.ConnectionId);
        Assert.Equal(2, collisionLogs.Select(value => value.CorrelationKey).Distinct().Count());
    }

    [Fact]
    public async Task TimeoutAndLastErrorRemainOnFailingEquipment()
    {
        var log = new InteropLogManager();
        await using var host = new MultiEquipmentHost(log.EquipmentSink);
        await using var fleet = await MultiEquipmentScenarioManager.LoopbackFleet.CreateAsync(host, 2,
            noResponseEquipmentIndex: 1, t3Seconds: 1);
        var results = await host.BroadcastAsync(1, 1, null, CancellationToken.None);

        Assert.True(results["EQ-001"].Succeeded, results["EQ-001"].Detail);
        Assert.False(results["EQ-002"].Succeeded);
        Assert.Equal("None", host.Get("EQ-001").LastError);
        Assert.NotEqual("None", host.Get("EQ-002").LastError);
        await host.Get("EQ-001").LinktestAsync(CancellationToken.None);
        Assert.True(host.Get("EQ-001").IsSelected);
    }

    [Fact]
    public async Task OneOfTenReconnectDoesNotReplaceOtherConnectionsOrGemRuntime()
    {
        var log = new InteropLogManager();
        await using var host = new MultiEquipmentHost(log.EquipmentSink);
        await using var fleet = await MultiEquipmentScenarioManager.LoopbackFleet.CreateAsync(host, 10);
        var target = host.Connections[0];
        var stable = host.Connections.Skip(1).ToDictionary(value => value.EquipmentId,
            value => (value.ConnectionId, Runtime: value.GemRuntime));

        for (var cycle = 0; cycle < 5; cycle++)
        {
            var oldConnection = target.ConnectionId;
            var oldRuntime = target.GemRuntime;
            await fleet.ReconnectAsync(0, CancellationToken.None);
            Assert.NotEqual(oldConnection, target.ConnectionId);
            Assert.NotSame(oldRuntime, target.GemRuntime);
            await target.LinktestAsync(CancellationToken.None);
        }

        foreach (var context in host.Connections.Skip(1))
        {
            Assert.Equal(stable[context.EquipmentId].ConnectionId, context.ConnectionId);
            Assert.Same(stable[context.EquipmentId].Runtime, context.GemRuntime);
            Assert.True(context.IsSelected);
        }
    }

    [Fact]
    public async Task DisposeCancelsPendingPassiveAcceptAndWaitsForOperations()
    {
        var log = new InteropLogManager();
        var host = new MultiEquipmentHost(log.EquipmentSink);
        var context = host.Add(new EquipmentConnectionDefinition
        {
            EquipmentId = "EQ-PASSIVE", Host = "127.0.0.1", Port = ReservePort(),
            Mode = SecsConnectionMode.Passive, SessionId = 0, T3Seconds = 1, T5Seconds = 1,
            T6Seconds = 1, T7Seconds = 2, T8Seconds = 1
        });
        var connect = host.ConnectAllAsync(CancellationToken.None);
        await WaitUntilAsync(() => context.TcpState == "Listening");
        Assert.Equal(1, host.BackgroundOperationCount);

        await host.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => connect);
        Assert.Equal(0, host.BackgroundOperationCount);
    }

    [Fact]
    public async Task RequiredIsolationScenarioReportsMeasuredCleanup()
    {
        var summary = await new MultiEquipmentScenarioManager(new InteropLogManager())
            .RunAsync(3, CancellationToken.None);

        Assert.Equal("Passed", summary.Result);
        Assert.Equal(50, summary.MaximumEquipment);
        Assert.Equal(0, summary.RemainingSessions);
        Assert.Equal(0, summary.RemainingBackgroundOperations);
        Assert.True(summary.TrackedOperationTaskCount > 0);
        Assert.InRange(summary.PeakConcurrentEquipmentOperations, 1, 20);
        Assert.Equal(new[] { 1, 2, 10, 50 }, summary.ConnectionTimings.Select(value => value.EquipmentCount));
        Assert.Contains(summary.Checks, value => value.Contains("SessionId=0 + SystemBytes=1", StringComparison.Ordinal));
        Assert.Contains(summary.Checks, value => value.Contains("AutoReconnect remote drop", StringComparison.Ordinal));
        Assert.Contains(summary.Checks, value => value.Contains("ConnectionClosedAfterSelect", StringComparison.Ordinal));
        Assert.Contains(summary.Checks, value => value.Contains("Equipment→Host 1 MiB", StringComparison.Ordinal));
    }

    private static EquipmentConnectionDefinition Definition(string id, int port) => new()
    {
        EquipmentId = id, Host = "127.0.0.1", Port = port, Mode = SecsConnectionMode.Active,
        SessionId = 0, T3Seconds = 2, T5Seconds = 1, T6Seconds = 2, T7Seconds = 3, T8Seconds = 2
    };

    private static int ReservePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!condition()) await Task.Delay(5, timeout.Token);
    }
}
