using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Dreamine.Secs.Abstractions.Enums;
using Dreamine.Secs.Abstractions.Hsms;
using Dreamine.Secs.Abstractions.Model;
using Dreamine.Secs.Com.Hsms;
using Dreamine.SecsGem.Interop.Wpf.Models;

namespace Dreamine.SecsGem.Interop.Wpf.Managers;

public sealed class ScenarioManager(InteropLogManager log)
{
    public ObservableCollection<InteropScenarioResult> Scenarios { get; } = new(new[]
    {
        new InteropScenarioResult("A-01", "Self loopback: TCP + Select + Linktest", false),
        new InteropScenarioResult("A-02", "Self loopback: 1,000 correlated S1F1/S1F2", false),
        new InteropScenarioResult("A-03", "Self loopback: 100 disconnect/reconnect cycles", false),
        new InteropScenarioResult("A-04", "Self loopback: concurrent primaries and cancellation", false),
        new InteropScenarioResult("B-01", "Simulator Host / Dreamine Equipment", true),
        new InteropScenarioResult("C-01", "Simulator Equipment / Dreamine Host", true),
        new InteropScenarioResult("D-01", "Active/passive cross-check", true),
        new InteropScenarioResult("F-01", "T3/T6/T7/T8 and malformed/partial frame fault peer", true)
    });

    public async Task<SelfTestSummary> RunSelfLoopbackAsync(int messageCount, int reconnectCycles, CancellationToken token)
    {
        if (messageCount <= 0 || reconnectCycles <= 0) throw new ArgumentOutOfRangeException(nameof(messageCount));
        Set(0, InteropScenarioStatus.Running, "Connecting local active/passive pair.");
        Set(1, InteropScenarioStatus.Running, $"Sending {messageCount:N0} request/reply pairs.");
        Set(2, InteropScenarioStatus.Running, $"Running {reconnectCycles:N0} cycles.");
        Set(3, InteropScenarioStatus.Running, "Concurrent primary correlation check.");
        var started = DateTimeOffset.UtcNow;
        var passed = 0; var failed = 0;
        var checks = new List<string>();
        var latencies = new List<double>(messageCount);

        try
        {
            await using (var pair = await LoopbackPair.CreateAsync(token).ConfigureAwait(false))
            {
                var linktestTimer = Stopwatch.StartNew();
                for (var linktest = 0; linktest < reconnectCycles; linktest++)
                    await pair.Host.LinktestAsync(token).ConfigureAwait(false);
                linktestTimer.Stop(); Scenarios[0].Elapsed = linktestTimer.Elapsed;
                Set(0, InteropScenarioStatus.Passed, $"TCP connected, HSMS selected, {reconnectCycles:N0} Linktests succeeded.");
                checks.Add($"TCP/Select/Linktest ({reconnectCycles}): Passed");

                var throughput = Stopwatch.StartNew();
                for (var index = 0; index < messageCount; index++)
                {
                    token.ThrowIfCancellationRequested();
                    var latency = Stopwatch.StartNew();
                    var request = new SecsMessage(new(0), new(1), new(1), true, pair.Host.AllocateSystemBytes());
                    var response = await pair.Host.SendPrimaryAsync(request, token).ConfigureAwait(false);
                    latency.Stop(); latencies.Add(latency.Elapsed.TotalMilliseconds);
                    if (response.Stream.Value == 1 && response.Function.Value == 2 && response.SystemBytes == request.SystemBytes) passed++; else failed++;
                }
                throughput.Stop();
                Scenarios[1].Elapsed = throughput.Elapsed;
                Set(1, failed == 0 ? InteropScenarioStatus.Passed : InteropScenarioStatus.Failed,
                    $"{passed:N0}/{messageCount:N0} correlated replies; {messageCount / Math.Max(throughput.Elapsed.TotalSeconds, .001):N1} msg/s.");

                var concurrentTimer = Stopwatch.StartNew();
                var concurrent = Enumerable.Range(0, 20).Select(_ =>
                {
                    var request = new SecsMessage(new(0), new(1), new(1), true, pair.Host.AllocateSystemBytes());
                    return pair.Host.SendPrimaryAsync(request, token);
                });
                var responses = await Task.WhenAll(concurrent).ConfigureAwait(false);
                concurrentTimer.Stop(); Scenarios[3].Elapsed = concurrentTimer.Elapsed;
                if (responses.Select(value => value.SystemBytes.Value).Distinct().Count() == responses.Length)
                { Set(3, InteropScenarioStatus.Passed, "20 concurrent primaries were correlated without collision."); checks.Add("Concurrent correlation: Passed"); }
                else { Set(3, InteropScenarioStatus.Failed, "Duplicate correlation detected."); failed++; }
            }

            var reconnectTimer = Stopwatch.StartNew();
            for (var cycle = 0; cycle < reconnectCycles; cycle++)
            {
                token.ThrowIfCancellationRequested();
                await using var pair = await LoopbackPair.CreateAsync(token).ConfigureAwait(false);
                if (pair.Host.HsmsState != HsmsConnectionState.Selected) throw new InvalidOperationException("Reconnect cycle did not reach Selected.");
            }
            reconnectTimer.Stop(); Scenarios[2].Elapsed = reconnectTimer.Elapsed;
            await VerifyResponderRebindAsync(token).ConfigureAwait(false);
            Set(2, InteropScenarioStatus.Passed,
                $"{reconnectCycles:N0}/{reconnectCycles:N0} independent cycles and persistent responder rebind passed.");
            checks.Add("Disconnect/reconnect and responder rebind: Passed");
        }
        catch (OperationCanceledException)
        {
            Set(3, InteropScenarioStatus.WaitingForUser, "Cancelled by user; no conformance conclusion.");
            throw;
        }
        catch (Exception exception)
        {
            failed++;
            log.Error("SelfTest", exception);
            foreach (var scenario in Scenarios.Take(4).Where(value => value.Status == InteropScenarioStatus.Running))
            { scenario.Status = InteropScenarioStatus.Failed; scenario.Detail = exception.Message; }
        }

        var completed = DateTimeOffset.UtcNow;
        latencies.Sort();
        var average = latencies.Count == 0 ? 0 : latencies.Average();
        var p95 = latencies.Count == 0 ? 0 : latencies[(int)Math.Min(latencies.Count - 1, Math.Ceiling(latencies.Count * .95) - 1)];
        var summary = new SelfTestSummary(started, completed, messageCount, passed, failed, 0, reconnectCycles,
            reconnectCycles, messageCount / Math.Max((completed - started).TotalSeconds, .001),
            latencies.Count == 0 ? 0 : latencies[0], average, latencies.Count == 0 ? 0 : latencies[^1], p95,
            failed == 0 ? null : "See scenario detail and diagnostic log.", failed == 0 ? "Passed" : "Failed", checks);
        log.Info("SelfTest", $"{summary.Result}: {summary.Passed:N0} replies, {summary.ReconnectCycles:N0} reconnect cycles.");
        return summary;
    }

    public void MarkExternalWaiting()
    {
        foreach (var scenario in Scenarios.Where(value => value.External))
        { scenario.Status = InteropScenarioStatus.WaitingForUser; scenario.Detail = "Waiting for User: configure and operate the external simulator using the documented procedure."; }
    }

    public void Reset()
    {
        foreach (var scenario in Scenarios)
        { scenario.Status = InteropScenarioStatus.NotRun; scenario.Detail = "Not Run"; scenario.Elapsed = TimeSpan.Zero; }
    }

    private void Set(int index, InteropScenarioStatus status, string detail)
    { Scenarios[index].Status = status; Scenarios[index].Detail = detail; }

    private async Task VerifyResponderRebindAsync(CancellationToken token)
    {
        await using var connection = new ConnectionManager(log);
        var messages = new MessageManager(connection, log);
        var settings = new ConnectionSettings
        {
            Host = "127.0.0.1",
            Mode = SecsConnectionMode.Passive,
            Role = SecsRole.Equipment,
            SessionId = 0,
            T3Seconds = 10,
            T5Seconds = 2,
            T6Seconds = 5,
            T7Seconds = 10,
            T8Seconds = 5
        };

        for (var cycle = 0; cycle < 2; cycle++)
        {
            settings.Port = ReservePort();
            var passiveConnect = connection.ConnectAsync(settings, token);
            await WaitUntilAsync(() => connection.TcpState == "Listening", token).ConfigureAwait(false);
            if (cycle == 0) messages.EnableEquipmentResponder();

            await using var host = CreateSession(settings.Port, SecsConnectionMode.Active, SecsRole.Host);
            await host.ConnectAsync(token).ConfigureAwait(false);
            await passiveConnect.ConfigureAwait(false);
            await host.SelectAsync(token).ConfigureAwait(false);
            var request = new SecsMessage(new(0), new(1), new(13), true, host.AllocateSystemBytes(), new SecsListItem());
            var response = await host.SendPrimaryAsync(request, token).ConfigureAwait(false);
            if (response.Stream.Value != 1 || response.Function.Value != 14 || response.SystemBytes != request.SystemBytes)
                throw new InvalidOperationException($"Equipment responder rebind cycle {cycle + 1} returned an invalid S1F14.");
            await connection.DisconnectAsync(token).ConfigureAwait(false);
        }
    }

    private sealed class LoopbackPair(HsmsSession equipment, HsmsSession host) : IAsyncDisposable
    {
        public HsmsSession Equipment { get; } = equipment;
        public HsmsSession Host { get; } = host;
        public static async Task<LoopbackPair> CreateAsync(CancellationToken token)
        {
            var port = ReservePort();
            var equipment = CreateSession(port, SecsConnectionMode.Passive, SecsRole.Equipment);
            var host = CreateSession(port, SecsConnectionMode.Active, SecsRole.Host);
            equipment.MessageReceived += (_, message) =>
            {
                if (message.Stream.Value != 1 || message.Function.Value != 1 || !message.ReplyExpected) return;
                var response = new SecsMessage(message.SessionId, new(1), new(2), false, message.SystemBytes,
                    new SecsListItem(new SecsAsciiItem("DREAMINE-INTEROP"), new SecsAsciiItem("0.1")));
                _ = equipment.SendAsync(response, token);
            };
            try
            {
                var passiveConnect = equipment.ConnectAsync(token);
                await WaitUntilAsync(() => equipment.State.ToString() == "Listening", token).ConfigureAwait(false);
                await host.ConnectAsync(token).ConfigureAwait(false);
                await passiveConnect.ConfigureAwait(false);
                await host.SelectAsync(token).ConfigureAwait(false);
                return new LoopbackPair(equipment, host);
            }
            catch { await host.DisposeAsync(); await equipment.DisposeAsync(); throw; }
        }
        public async ValueTask DisposeAsync() { await Host.DisposeAsync(); await Equipment.DisposeAsync(); }
        private static HsmsSession CreateSession(int port, SecsConnectionMode mode, SecsRole role) => ScenarioManager.CreateSession(port, mode, role);
    }

    private static HsmsSession CreateSession(int port, SecsConnectionMode mode, SecsRole role) => new(new HsmsSessionOptions
    {
        Host = "127.0.0.1", Port = port, Mode = mode, Role = role, SessionId = new(0),
        Timers = new HsmsTimerOptions { T3 = TimeSpan.FromSeconds(10), T5 = TimeSpan.FromSeconds(2), T6 = TimeSpan.FromSeconds(5), T7 = TimeSpan.FromSeconds(10), T8 = TimeSpan.FromSeconds(5) }
    });
    private static int ReservePort() { var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start(); var port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop(); return port; }
    private static async Task WaitUntilAsync(Func<bool> condition, CancellationToken token)
    { while (!condition()) { token.ThrowIfCancellationRequested(); await Task.Delay(5, token).ConfigureAwait(false); } }
}
