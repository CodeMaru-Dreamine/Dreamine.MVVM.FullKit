using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Dreamine.Communication.Abstractions.Enums;
using Dreamine.Secs.Abstractions.Enums;
using Dreamine.Secs.Abstractions.Hsms;
using Dreamine.Secs.Abstractions.Interfaces;
using Dreamine.Secs.Abstractions.Model;
using Dreamine.Secs.Abstractions.Options;
using Dreamine.Secs.Com;
using Dreamine.SecsGem.Interop.Wpf.Models;

namespace Dreamine.SecsGem.Interop.Wpf.Managers;

public sealed class ScenarioManager
{
    private const ushort SelfTestSessionId = 7;
    private readonly InteropLogManager _log;
    private readonly Func<ConnectionSettings, ISecsMessageSessionProvider> _providerFactory;

    public ScenarioManager(InteropLogManager log)
        : this(log, settings => new DreamineSecsCommunicationProvider(_ => settings.ToOptions()))
    {
    }

    internal ScenarioManager(
        InteropLogManager log,
        Func<ConnectionSettings, ISecsMessageSessionProvider> providerFactory)
    {
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
    }

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
            await using (var pair = await LoopbackPair.CreateAsync(_providerFactory, token).ConfigureAwait(false))
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
                    var response = await pair.Host.RequestAsync(Dialogue(1, 1), null, token).ConfigureAwait(false);
                    latency.Stop(); latencies.Add(latency.Elapsed.TotalMilliseconds);
                    if (response.SessionId.Value == SelfTestSessionId &&
                        response.Stream.Value == 1 && response.Function.Value == 2)
                        passed++;
                    else
                        failed++;
                }
                throughput.Stop();
                Scenarios[1].Elapsed = throughput.Elapsed;
                Set(1, failed == 0 ? InteropScenarioStatus.Passed : InteropScenarioStatus.Failed,
                    $"{passed:N0}/{messageCount:N0} correlated replies; {messageCount / Math.Max(throughput.Elapsed.TotalSeconds, .001):N1} msg/s.");

                var concurrentTimer = Stopwatch.StartNew();
                var concurrent = Enumerable.Range(0, 20)
                    .Select(_ => pair.Host.RequestAsync(Dialogue(1, 1), null, token));
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
                await using var pair = await LoopbackPair.CreateAsync(_providerFactory, token).ConfigureAwait(false);
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
            _log.Error("SelfTest", exception);
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
        _log.Info("SelfTest", $"{summary.Result}: {summary.Passed:N0} replies, {summary.ReconnectCycles:N0} reconnect cycles.");
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
        var wireRoot = Path.Combine(Path.GetTempPath(), "DreamineInteropSelfTestLogs");
        Directory.CreateDirectory(wireRoot);
        await using var wireLog = new WireLogManager(_log, wireRoot);
        await using var connection = new ConnectionManager(_log, _providerFactory, wireLog);
        using var messages = new MessageManager(connection, _log);
        var settings = Settings(0, SecsConnectionMode.Passive, SecsRole.Equipment);

        for (var cycle = 0; cycle < 2; cycle++)
        {
            settings.Port = ReservePort();
            var passiveConnect = connection.ConnectAsync(settings, token);
            await WaitUntilOrCompletionAsync(
                () => connection.TcpState == "Listening",
                passiveConnect,
                token).ConfigureAwait(false);
            if (cycle == 0) messages.EnableEquipmentResponder();

            await using var host = CreateSession(
                _providerFactory,
                Settings(settings.Port, SecsConnectionMode.Active, SecsRole.Host));
            await host.ConnectAsync(token).ConfigureAwait(false);
            await passiveConnect.ConfigureAwait(false);
            await host.SelectAsync(token).ConfigureAwait(false);
            var response = await host.RequestAsync(
                Dialogue(1, 13),
                new SecsListItem(),
                token).ConfigureAwait(false);
            if (response.SessionId.Value != SelfTestSessionId ||
                response.Stream.Value != 1 || response.Function.Value != 14)
                throw new InvalidOperationException($"Equipment responder rebind cycle {cycle + 1} returned an invalid S1F14.");
            await connection.DisconnectAsync(token).ConfigureAwait(false);
        }
    }

    private sealed class LoopbackPair(
        ISecsMessageSession equipment,
        ISecsMessageSession host,
        IDisposable responderRegistration) : IAsyncDisposable
    {
        public ISecsMessageSession Equipment { get; } = equipment;
        public ISecsMessageSession Host { get; } = host;

        public static async Task<LoopbackPair> CreateAsync(
            Func<ConnectionSettings, ISecsMessageSessionProvider> providerFactory,
            CancellationToken token)
        {
            var port = ReservePort();
            var equipment = CreateSession(
                providerFactory,
                Settings(port, SecsConnectionMode.Passive, SecsRole.Equipment));
            var host = CreateSession(
                providerFactory,
                Settings(port, SecsConnectionMode.Active, SecsRole.Host));
            var registration = equipment.PrimaryDispatcher.Register(
                Dialogue(1, 1),
                static async (context, cancellationToken) =>
                {
                    if (!context.CanReply) return;
                    await context.ReplyAsync(
                        new SecsListItem(
                            new SecsAsciiItem("DREAMINE-INTEROP"),
                            new SecsAsciiItem("0.1")),
                        cancellationToken).ConfigureAwait(false);
                });
            try
            {
                var passiveConnect = equipment.ConnectAsync(token);
                await WaitUntilOrCompletionAsync(
                    () => equipment.State == ConnectionState.Listening,
                    passiveConnect,
                    token).ConfigureAwait(false);
                await host.ConnectAsync(token).ConfigureAwait(false);
                await passiveConnect.ConfigureAwait(false);
                await host.SelectAsync(token).ConfigureAwait(false);
                return new LoopbackPair(equipment, host, registration);
            }
            catch
            {
                registration.Dispose();
                await host.DisposeAsync();
                await equipment.DisposeAsync();
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            responderRegistration.Dispose();
            await Host.DisposeAsync();
            await Equipment.DisposeAsync();
        }
    }

    private static ISecsMessageSession CreateSession(
        Func<ConnectionSettings, ISecsMessageSessionProvider> providerFactory,
        ConnectionSettings settings)
    {
        var provider = providerFactory(settings) ??
            throw new InvalidOperationException("The self-test provider factory returned null.");
        return provider.CreateSession(new SecsConnectionOptions
        {
            ProviderKey = provider.Key,
            Role = settings.Role,
            Mode = settings.Mode
        }) ?? throw new InvalidOperationException("The self-test provider returned null.");
    }

    private static ConnectionSettings Settings(int port, SecsConnectionMode mode, SecsRole role) => new()
    {
        Host = "127.0.0.1",
        Port = port,
        Mode = mode,
        Role = role,
        SessionId = SelfTestSessionId,
        T3Seconds = 10,
        T5Seconds = 2,
        T6Seconds = 5,
        T7Seconds = 10,
        T8Seconds = 5
    };

    private static SecsDialogueDefinition Dialogue(byte stream, byte primaryFunction) => new(
        new SecsStream(stream),
        new SecsFunction(primaryFunction),
        new SecsFunction(checked((byte)(primaryFunction + 1))));

    private static int ReservePort() { var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start(); var port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop(); return port; }
    private static async Task WaitUntilAsync(Func<bool> condition, CancellationToken token)
    { while (!condition()) { token.ThrowIfCancellationRequested(); await Task.Delay(5, token).ConfigureAwait(false); } }

    private static async Task WaitUntilOrCompletionAsync(
        Func<bool> condition,
        Task operation,
        CancellationToken token)
    {
        while (!condition())
        {
            token.ThrowIfCancellationRequested();
            if (operation.IsCompleted)
            {
                await operation.ConfigureAwait(false);
                if (!condition())
                    throw new InvalidOperationException("The passive connection completed without entering Listening state.");
            }
            await Task.Delay(5, token).ConfigureAwait(false);
        }
    }
}
