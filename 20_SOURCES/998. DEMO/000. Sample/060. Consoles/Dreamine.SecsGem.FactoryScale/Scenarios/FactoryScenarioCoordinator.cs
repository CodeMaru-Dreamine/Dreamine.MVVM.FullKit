using System.Diagnostics;
using Dreamine.Secs.Abstractions.Model;
using Dreamine.SecsGem.FactoryScale.Export;
using Dreamine.SecsGem.FactoryScale.Infrastructure;
using Dreamine.SecsGem.FactoryScale.Metrics;
using Dreamine.SecsGem.FactoryScale.Models;
using Dreamine.SecsGem.FactoryScale.Runtime;
using Dreamine.SecsGem.FactoryScale.Simulation;
using Dreamine.SecsGem.Interop.Runtime;

namespace Dreamine.SecsGem.FactoryScale.Scenarios;

internal sealed record FactoryScenarioRunOptions
{
    internal string Scenario { get; init; } = "factory-smoke";
    internal string ExecutionMode { get; init; } = "in-process";
    internal int EquipmentCount { get; init; } = 100;
    internal int StartIndex { get; init; } = 1;
    internal int DisconnectCount { get; init; } = 10;
    internal TimeSpan Duration { get; init; } = TimeSpan.Zero;
    internal double MessagesPerSecond { get; init; }
    internal int MessageBytes { get; init; }
    internal int PortRangeStart { get; init; } = 20_000;
    internal int PortRangeEnd { get; init; } = 48_000;
    internal int ConnectConcurrency { get; init; } = 64;
    internal int ReconnectConcurrency { get; init; } = 16;
    internal int MessageConcurrency { get; init; } = 128;
    internal int ReceiveQueueCapacity { get; init; } = 4_096;
    internal int LogQueueCapacity { get; init; } = 2_048;
    internal int ExportQueueCapacity { get; init; } = 64;
    internal int PerEquipmentPendingLimit { get; init; } = 16;
    internal int GlobalPendingLimit { get; init; } = 4_096;
    internal TimeSpan? SnapshotInterval { get; init; }
    internal string? SnapshotDirectory { get; init; }

    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(Scenario)) throw new ArgumentException("Scenario is required.", nameof(Scenario));
        if (ExecutionMode is not ("in-process" or "multi-process")) throw new ArgumentOutOfRangeException(nameof(ExecutionMode));
        if (EquipmentCount is < 1 or > 1_000) throw new ArgumentOutOfRangeException(nameof(EquipmentCount));
        if (StartIndex < 1) throw new ArgumentOutOfRangeException(nameof(StartIndex));
        if (DisconnectCount is < 0 || DisconnectCount > EquipmentCount) throw new ArgumentOutOfRangeException(nameof(DisconnectCount));
        if (Duration < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(Duration));
        if (MessagesPerSecond < 0 || !double.IsFinite(MessagesPerSecond)) throw new ArgumentOutOfRangeException(nameof(MessagesPerSecond));
        var maximumPayloadBytes = FactoryMessageBodyFactory.MaximumPayloadBytesForScenario(Scenario);
        if (MessageBytes < 0 || MessageBytes > maximumPayloadBytes)
            throw new ArgumentOutOfRangeException(nameof(MessageBytes), MessageBytes,
                $"The requested payload exceeds the {maximumPayloadBytes:N0}-byte limit for this scenario and the configured HSMS frame maximum.");
        if (PortRangeStart is < 1 or > 65_535 || PortRangeEnd < PortRangeStart || PortRangeEnd > 65_535)
            throw new ArgumentOutOfRangeException(nameof(PortRangeStart));
        var requiredPortCount = checked(EquipmentCount +
            (ExecutionMode == "in-process" && Scenario.Equals("fault-isolation", StringComparison.OrdinalIgnoreCase)
                ? 1
                : 0));
        if (PortRangeEnd - PortRangeStart + 1 < requiredPortCount)
            throw new ArgumentException(
                $"The validated port range requires {requiredPortCount} ports, including scenario-only listeners.");
        if (SnapshotInterval is { } interval && interval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(SnapshotInterval));
        var runtimeOptions = new FactoryHostRuntimeOptions(ConnectConcurrency, ReconnectConcurrency, MessageConcurrency,
            Math.Max(ReceiveQueueCapacity, MessageConcurrency), PerEquipmentPendingLimit,
            Math.Max(GlobalPendingLimit, MessageConcurrency), LogQueueCapacity);
        runtimeOptions.Validate();
    }
}

internal sealed class FactoryScenarioCoordinator
{
    internal async Task<FactoryRunResult> RunInProcessAsync(
        FactoryScenarioRunOptions options,
        FactoryResultExporter exporter,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(exporter);
        options.Validate();
        var startedAt = DateTimeOffset.UtcNow;
        var checks = new List<string>();
        var snapshots = new List<FactoryMetricSnapshot>();
        var metrics = new FactoryMetricsCollector(WindowsProcessSocketMetricsProvider.Instance);
        var baseline = FactoryCleanupSnapshot.Capture(metrics);
        snapshots.Add(baseline);
        checks.Add($"Process baseline: working set {baseline.Process.WorkingSetBytes:N0}, managed heap {baseline.Process.ManagedHeapBytes:N0}, handles {baseline.Process.HandleCount?.ToString() ?? "unavailable"}, OS open sockets {baseline.Process.Sockets.OpenSocketCount?.ToString() ?? "unavailable"}.");
        FactoryHostRuntime? host = null;
        FactoryEquipmentFleet? fleet = null;
        FactoryMetricSnapshot? final = null;
        FactoryMetricSnapshot cleanupBeforeGc;
        FactoryMetricSnapshot cleanupAfterGc;
        IReadOnlyList<FactoryEquipmentMetricSnapshot> equipmentMetrics = [];
        var status = FactoryRunStatus.Passed;
        string? firstFailure = null;

        try
        {
            var hostOptions = new FactoryHostRuntimeOptions(
                options.ConnectConcurrency,
                options.ReconnectConcurrency,
                options.MessageConcurrency,
                Math.Max(options.ReceiveQueueCapacity, options.MessageConcurrency),
                options.PerEquipmentPendingLimit,
                Math.Max(options.GlobalPendingLimit, options.MessageConcurrency),
                options.LogQueueCapacity);
            var fleetOptions = new FactoryEquipmentFleetOptions(
                Math.Max(options.ReceiveQueueCapacity, options.MessageConcurrency),
                Math.Clamp(options.MessageConcurrency / 4, 4, 64),
                options.ConnectConcurrency,
                T3Seconds: options.Scenario.Equals("fault-isolation", StringComparison.OrdinalIgnoreCase) ? 1 : 10);
            var first = options.StartIndex;
            fleet = new FactoryEquipmentFleet(fleetOptions,
                new ValidatedPortRangeManager(options.PortRangeStart, options.PortRangeEnd, options.StartIndex),
                equipmentNumber => SelectBehavior(options.Scenario, equipmentNumber - first));
            host = new FactoryHostRuntime(hostOptions, metrics);

            await fleet.StartAsync(options.StartIndex, options.EquipmentCount, cancellationToken).ConfigureAwait(false);
            checks.Add($"Started {options.EquipmentCount} real loopback TCP listeners through the validated port manager.");
            host.AddEquipment(fleet.Definitions);
            var connectTimer = Stopwatch.StartNew();
            var connect = await host.ConnectAllAsync(cancellationToken).ConfigureAwait(false);
            await fleet.WaitForConnectionsAsync(cancellationToken).ConfigureAwait(false);
            connectTimer.Stop();
            Require(connect.Count == options.EquipmentCount && connect.Values.All(value => value.Succeeded),
                $"Only {connect.Values.Count(value => value.Succeeded)}/{options.EquipmentCount} host connections succeeded.");
            Require(host.Connections.Count(value => value.IsSelected) == options.EquipmentCount,
                "Not every connected equipment reached HSMS Selected.");
            checks.Add($"TCP connect + HSMS Select passed for {options.EquipmentCount}/{options.EquipmentCount} in {connectTimer.Elapsed}.");

            snapshots.Add(host.CaptureSnapshot(fleet.LiveSessionCount, fleet.ListenerCount,
                fleet.CaptureQueueMetrics(), exporter.CaptureMetrics()));

            if (!options.Scenario.Equals("fault-isolation", StringComparison.OrdinalIgnoreCase))
                await RunProtocolBaselineAsync(host, checks, cancellationToken).ConfigureAwait(false);

            switch (NormalizeScenario(options.Scenario))
            {
                case "factory-smoke":
                case "scale":
                    await RunScaleStageAsync(host, fleet, options, checks, cancellationToken).ConfigureAwait(false);
                    break;
                case "idle-factory":
                    await RunIdleAsync(host, fleet, exporter, options, snapshots, checks, cancellationToken)
                        .ConfigureAwait(false);
                    break;
                case "factory-normal":
                    await RunRateProfileAsync(host, fleet, exporter, options,
                        DefaultDuration(options, TimeSpan.FromHours(1)),
                        options.MessagesPerSecond > 0 ? options.MessagesPerSecond : options.EquipmentCount,
                        "normal", snapshots, checks, cancellationToken).ConfigureAwait(false);
                    break;
                case "factory-busy":
                    await RunRateProfileAsync(host, fleet, exporter, options,
                        DefaultDuration(options, TimeSpan.FromHours(1)),
                        options.MessagesPerSecond > 0 ? options.MessagesPerSecond : options.EquipmentCount * 10d,
                        "busy", snapshots, checks, cancellationToken).ConfigureAwait(false);
                    break;
                case "trace-burst":
                    await RunRateProfileAsync(host, fleet, exporter, options,
                        DefaultDuration(options, TimeSpan.FromMinutes(1)),
                        options.MessagesPerSecond > 0 ? options.MessagesPerSecond : options.EquipmentCount * 50d,
                        "trace", snapshots, checks, cancellationToken).ConfigureAwait(false);
                    break;
                case "large-message":
                    await RunLargeMessageAsync(host, options, checks, cancellationToken).ConfigureAwait(false);
                    break;
                case "reconnect-storm":
                    await RunReconnectStormAsync(host, fleet, options, checks, cancellationToken).ConfigureAwait(false);
                    break;
                case "host-restart":
                    host = await RunHostRestartAsync(host, fleet, hostOptions, metrics, checks, cancellationToken)
                        .ConfigureAwait(false);
                    break;
                case "fault-isolation":
                    await RunFaultIsolationAsync(host, fleet, options, checks, cancellationToken).ConfigureAwait(false);
                    var healthy = host.Connections.Skip(5).First();
                    var protocolFaults = new ProtocolFaultInjectionSuite(new ValidatedPortRangeManager(
                        options.PortRangeStart, options.PortRangeEnd, options.StartIndex + options.EquipmentCount));
                    checks.AddRange(await protocolFaults.RunAsync(async token =>
                    {
                        var probe = await host.RequestAsync(healthy.EquipmentId, 1, 1, null, token)
                            .ConfigureAwait(false);
                        Require(probe.Succeeded, "A healthy equipment stopped responding during raw fault injection.");
                        await host.Host.Get(healthy.EquipmentId).LinktestAsync(token).ConfigureAwait(false);
                    }, cancellationToken).ConfigureAwait(false));
                    break;
                case "soak":
                    await RunRateProfileAsync(host, fleet, exporter, options,
                        DefaultDuration(options, TimeSpan.FromHours(1)),
                        options.MessagesPerSecond > 0 ? options.MessagesPerSecond : Math.Max(1, options.EquipmentCount / 60d),
                        "soak", snapshots, checks, cancellationToken).ConfigureAwait(false);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported Factory scenario '{options.Scenario}'.");
            }

            var terminal = host.CaptureSnapshot(fleet.LiveSessionCount, fleet.ListenerCount,
                fleet.CaptureQueueMetrics(), exporter.CaptureMetrics());
            final = PreserveLatestRateInterval(terminal, snapshots, options.Scenario);
            snapshots.Add(final);
            Require(final.CorrelationErrors == 0, "A transaction correlation error was observed.");
            Require(final.Queues.All(value => value.Dropped == 0 || value.Name == "diagnostic-log"),
                "A business queue reported dropped work.");
            Require(final.Queues.All(value => value.PeakDepth <= value.Capacity),
                "A bounded queue reported a depth greater than its declared capacity.");
            Require(final.Queues.Where(value => value.Name != "diagnostic-log")
                    .All(value => value.Depth == 0 && value.Rejected == 0),
                "A business queue did not drain or explicitly rejected accepted protocol work.");
            Require(fleet.BusinessRejectCount == 0 || NormalizeScenario(options.Scenario) == "fault-isolation",
                $"Equipment receive processing rejected {fleet.BusinessRejectCount} business messages.");
            if (HasSustainedMemoryGrowth(snapshots))
                throw new InvalidOperationException("Working set increased monotonically beyond the sustained-growth threshold.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            status = FactoryRunStatus.Cancelled;
            firstFailure = "Run canceled by the caller.";
        }
        catch (Exception exception)
        {
            status = FactoryRunStatus.Failed;
            firstFailure = $"{exception.GetType().Name}: {exception.Message}";
            checks.Add($"FAILED: {firstFailure}");
        }
        finally
        {
            if (host is not null)
            {
                equipmentMetrics = host.CaptureEquipmentMetrics();
                try { await host.DisconnectAllAsync(CancellationToken.None).ConfigureAwait(false); }
                catch (Exception exception) { RecordCleanupFailure(exception); }
                try { await host.DisposeAsync().ConfigureAwait(false); }
                catch (Exception exception) { RecordCleanupFailure(exception); }
            }
            if (fleet is not null)
            {
                try { await fleet.DisposeAsync().ConfigureAwait(false); }
                catch (Exception exception) { RecordCleanupFailure(exception); }
            }
        }

        cleanupBeforeGc = FactoryCleanupSnapshot.Capture(metrics);
        checks.Add($"Cleanup before forced GC: tracked sessions {cleanupBeforeGc.TrackedSessions}, pending transactions {cleanupBeforeGc.PendingTransactions}, Factory-owned live operations/workers {cleanupBeforeGc.TrackedOperations} (baseline {baseline.TrackedOperations}), OS open sockets {cleanupBeforeGc.Process.Sockets.OpenSocketCount?.ToString() ?? "unavailable"}.");
        checks.Add(OwnedWorkerEvidence("Cleanup", cleanupBeforeGc.OwnedWorkers, baseline.OwnedWorkers));
        checks.Add($"Cleanup delta from process baseline before forced GC: working set {cleanupBeforeGc.Process.WorkingSetBytes - baseline.Process.WorkingSetBytes:+#,#;-#,#;0}, managed heap {cleanupBeforeGc.Process.ManagedHeapBytes - baseline.Process.ManagedHeapBytes:+#,#;-#,#;0}, handles {Delta(cleanupBeforeGc.Process.HandleCount, baseline.Process.HandleCount)}.");
        if (HasFactoryResourceLeak(cleanupBeforeGc, baseline))
        {
            status = FactoryRunStatus.Failed;
            firstFailure ??= "Tracked resources remained after deterministic cleanup, before forced GC.";
        }

        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        cleanupAfterGc = FactoryCleanupSnapshot.Capture(metrics);
        checks.Add($"Cleanup after forced GC: working set {cleanupAfterGc.Process.WorkingSetBytes:N0}, managed heap {cleanupAfterGc.Process.ManagedHeapBytes:N0}, threads {cleanupAfterGc.Process.ThreadCount?.ToString() ?? "unavailable"}, handles {cleanupAfterGc.Process.HandleCount?.ToString() ?? "unavailable"}.");
        checks.Add(OwnedWorkerEvidence("Post-GC cleanup", cleanupAfterGc.OwnedWorkers, baseline.OwnedWorkers));
        if (HasFactoryResourceLeak(cleanupAfterGc, baseline) ||
            ExceedsStabilizationGrowth(cleanupBeforeGc.Process.ThreadCount, cleanupAfterGc.Process.ThreadCount, 2) ||
            ExceedsStabilizationGrowth(cleanupBeforeGc.Process.HandleCount, cleanupAfterGc.Process.HandleCount, 4))
        {
            status = FactoryRunStatus.Failed;
            firstFailure ??= "Factory-owned resources or OS thread/handle evidence grew during cleanup stabilization.";
        }

        final ??= snapshots.LastOrDefault() ?? FactoryCleanupSnapshot.Capture(metrics);
        return new FactoryRunResult(1, options.Scenario, options.ExecutionMode, startedAt, DateTimeOffset.UtcNow,
            status, final, cleanupBeforeGc, cleanupAfterGc, snapshots, checks, firstFailure)
        {
            EquipmentMetrics = equipmentMetrics
        };

        void RecordCleanupFailure(Exception exception)
        {
            status = FactoryRunStatus.Failed;
            firstFailure ??= $"Cleanup failed: {exception.GetType().Name}: {exception.Message}";
            checks.Add($"FAILED cleanup: {exception.GetType().Name}: {exception.Message}");
        }
    }

    private static async Task RunProtocolBaselineAsync(
        FactoryHostRuntime host,
        List<string> checks,
        CancellationToken cancellationToken)
    {
        var linktests = await host.LinktestAllAsync(cancellationToken).ConfigureAwait(false);
        Require(linktests.Values.All(value => value.Succeeded), "One or more Linktest transactions failed.");
        checks.Add($"Linktest passed for {linktests.Count}/{linktests.Count} equipment.");

        var s1f1 = await RequestAllAsync(host, 1, 1, _ => null, cancellationToken).ConfigureAwait(false);
        Require(s1f1.All(value => value.Succeeded), "One or more S1F1/S1F2 transactions failed.");
        var s1f13 = await RequestAllAsync(host, 1, 13, _ => new SecsListItem(), cancellationToken).ConfigureAwait(false);
        Require(s1f13.All(value => value.Succeeded), "One or more S1F13/S1F14 transactions failed.");
        checks.Add($"S1F1/S1F2 and S1F13/S1F14 passed for {host.Connections.Count} equipment.");
    }

    private static async Task RunScaleStageAsync(
        FactoryHostRuntime host,
        FactoryEquipmentFleet fleet,
        FactoryScenarioRunOptions options,
        List<string> checks,
        CancellationToken cancellationToken)
    {
        var collisionTargets = host.Connections.Take(2).ToArray();
        if (collisionTargets.Length == 2)
        {
            var body = new SecsAsciiItem("IDENTICAL-BODY");
            var replies = await Task.WhenAll(collisionTargets.Select(context =>
                host.RequestAsync(context.EquipmentId, 1, 1, body, cancellationToken, new SecsSystemBytes(1))))
                .ConfigureAwait(false);
            Require(replies.All(value => value.Succeeded && value.Response?.SystemBytes.Value == 1),
                "Identical System Bytes collision isolation failed.");
            Require(replies.All(value => value.Response is not null &&
                ResponseContainsEquipmentId(value.Response.Item, value.EquipmentId)),
                "A Secondary completed a Primary from another equipment connection.");
            checks.Add("Session ID 0 + System Bytes 1 + identical S1F1/body remained isolated by equipment connection.");
        }

        var disconnectCount = Math.Clamp(options.DisconnectCount == 0 ? Math.Max(1, options.EquipmentCount / 10) : options.DisconnectCount,
            1, options.EquipmentCount);
        await ReconnectSubsetAsync(host, fleet, disconnectCount, checks, cancellationToken).ConfigureAwait(false);
    }

    private static async Task RunReconnectStormAsync(
        FactoryHostRuntime host,
        FactoryEquipmentFleet fleet,
        FactoryScenarioRunOptions options,
        List<string> checks,
        CancellationToken cancellationToken)
    {
        var disconnectCount = Math.Clamp(options.DisconnectCount == 0 ? Math.Min(100, options.EquipmentCount) : options.DisconnectCount,
            1, options.EquipmentCount);
        await ReconnectSubsetAsync(host, fleet, disconnectCount, checks, cancellationToken).ConfigureAwait(false);
    }

    private static async Task ReconnectSubsetAsync(
        FactoryHostRuntime host,
        FactoryEquipmentFleet fleet,
        int disconnectCount,
        List<string> checks,
        CancellationToken cancellationToken)
    {
        var targets = host.Connections.Take(disconnectCount).Select(value => value.EquipmentId).ToArray();
        var previousIds = targets.ToDictionary(value => value, value => host.Host.Get(value).ConnectionId,
            StringComparer.OrdinalIgnoreCase);
        var unaffected = host.Connections.Skip(disconnectCount).FirstOrDefault();
        var timer = Stopwatch.StartNew();
        var disconnect = await host.DisconnectAsync(targets, cancellationToken).ConfigureAwait(false);
        Require(disconnect.Values.All(value => value.Succeeded), "A reconnect-storm target did not disconnect cleanly.");
        await fleet.RestartAsync(targets, cancellationToken).ConfigureAwait(false);
        if (unaffected is not null)
        {
            var probe = await host.RequestAsync(unaffected.EquipmentId, 1, 1, null, cancellationToken).ConfigureAwait(false);
            Require(probe.Succeeded, "An unaffected equipment stopped communicating during the reconnect storm.");
        }
        var reconnect = await host.ConnectReconnectTargetsAsync(targets, cancellationToken).ConfigureAwait(false);
        await fleet.WaitForConnectionsAsync(cancellationToken).ConfigureAwait(false);
        timer.Stop();
        Require(reconnect.Values.All(value => value.Succeeded), "One or more reconnect-storm targets did not recover.");
        Require(targets.All(value => host.Host.Get(value).ConnectionId != previousIds[value]),
            "A reconnect reused the previous ConnectionId.");
        Require(host.PeakReconnectOperationCount > 0 &&
                host.PeakReconnectOperationCount <= host.Host.Options.ReconnectConcurrency,
            "Reconnect concurrency exceeded its configured bound.");
        checks.Add($"Reconnect storm recovered {disconnectCount} equipment in {timer.Elapsed}; peak reconnect concurrency was {host.PeakReconnectOperationCount}/{host.Host.Options.ReconnectConcurrency}, unaffected traffic passed and ConnectionIds were replaced.");
    }

    private static async Task RunIdleAsync(
        FactoryHostRuntime host,
        FactoryEquipmentFleet fleet,
        FactoryResultExporter exporter,
        FactoryScenarioRunOptions options,
        List<FactoryMetricSnapshot> snapshots,
        List<string> checks,
        CancellationToken cancellationToken)
    {
        var duration = DefaultDuration(options, TimeSpan.FromHours(1));
        var timer = Stopwatch.StartNew();
        var nextLinktest = TimeSpan.Zero;
        var due = SnapshotTimes(options, duration).GetEnumerator();
        var hasDue = due.MoveNext();
        while (timer.Elapsed < duration)
        {
            if (timer.Elapsed >= nextLinktest)
            {
                var result = await host.LinktestAllAsync(cancellationToken).ConfigureAwait(false);
                Require(result.Values.All(value => value.Succeeded), "Idle Factory Linktest failed.");
                nextLinktest += TimeSpan.FromMinutes(1);
            }
            if (hasDue && timer.Elapsed >= due.Current)
            {
                await CaptureSnapshotAsync(host, fleet, exporter, options, snapshots, due.Current, cancellationToken)
                    .ConfigureAwait(false);
                hasDue = due.MoveNext();
            }
            var delay = Min(TimeSpan.FromSeconds(1), duration - timer.Elapsed);
            if (delay > TimeSpan.Zero) await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }
        checks.Add($"Idle Factory held {options.EquipmentCount} selected equipment for {timer.Elapsed} with one Linktest per minute.");
    }

    private static async Task RunRateProfileAsync(
        FactoryHostRuntime host,
        FactoryEquipmentFleet fleet,
        FactoryResultExporter exporter,
        FactoryScenarioRunOptions options,
        TimeSpan duration,
        double targetRate,
        string profile,
        List<FactoryMetricSnapshot> snapshots,
        List<string> checks,
        CancellationToken cancellationToken)
    {
        if (targetRate <= 0) throw new ArgumentOutOfRangeException(nameof(targetRate));
        var baseline = host.Metrics.CaptureSnapshot();
        var timer = Stopwatch.StartNew();
        long scheduled = 0;
        var cursor = 0;
        var observedOutOfOrderResponse = false;
        var due = SnapshotTimes(options, duration).GetEnumerator();
        var hasDue = due.MoveNext();
        while (timer.Elapsed < duration)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var desired = (long)Math.Floor(timer.Elapsed.TotalSeconds * targetRate);
            var count = Math.Min(desired - scheduled, Math.Max(1, options.MessageConcurrency * 4L));
            if (count <= 0)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(10), cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var batch = new Task<FactoryMessageResult>[checked((int)count)];
                for (var index = 0; index < batch.Length; index++)
                {
                    var equipment = host.Connections[cursor++ % host.Connections.Count];
                    var sequence = scheduled + index;
                    SecsSystemBytes? explicitSystemBytes = profile is "busy" or "trace"
                        ? new SecsSystemBytes(checked((uint)(sequence + 1)))
                        : null;
                    batch[index] = host.RequestAsync(equipment.EquipmentId, 6, 11,
                        BuildBody(profile, sequence, options.MessageBytes), cancellationToken, explicitSystemBytes);
                }
                var results = await Task.WhenAll(batch).ConfigureAwait(false);
                Require(results.All(value => value.Succeeded),
                    $"{profile} profile lost or failed {results.Count(value => !value.Succeeded)} accepted messages.");
                if (results.Length > 1 && results.Zip(results.Skip(1),
                        (left, right) => left.CompletionOrdinal > right.CompletionOrdinal).Any(value => value))
                    observedOutOfOrderResponse = true;
                scheduled += batch.Length;
            }

            if (hasDue && timer.Elapsed >= due.Current)
            {
                await CaptureSnapshotAsync(host, fleet, exporter, options, snapshots, due.Current, cancellationToken)
                    .ConfigureAwait(false);
                hasDue = due.MoveNext();
            }
        }
        var completed = host.Metrics.ResponseCount - baseline.Responses;
        var achieved = completed / Math.Max(0.001, timer.Elapsed.TotalSeconds);
        Require(completed == scheduled, $"{scheduled - completed} accepted messages had no response.");
        Require(achieved >= targetRate * 0.90,
            $"Achieved {achieved:F2} msg/s, below 90% of the {targetRate:F2} msg/s target.");
        if (profile == "busy")
        {
            Require(observedOutOfOrderResponse, "Busy profile did not observe an intentionally reordered response completion.");
            Require(host.PeakPendingTransactionCount > 1,
                "Busy profile did not exercise concurrent pending transactions.");
        }
        checks.Add($"{profile} profile completed {completed:N0} request/response pairs in {timer.Elapsed}; achieved {achieved:F2} msg/s against {targetRate:F2} msg/s target.");
        if (profile == "busy")
            checks.Add($"Busy profile observed out-of-order response completion with peak pending transactions {host.PeakPendingTransactionCount}.");
    }

    private static async Task RunLargeMessageAsync(
        FactoryHostRuntime host,
        FactoryScenarioRunOptions options,
        List<string> checks,
        CancellationToken cancellationToken)
    {
        var bytes = options.MessageBytes == 0 ? 1024 * 1024 : options.MessageBytes;
        var targets = host.Connections.Take(Math.Min(16, host.Connections.Count)).ToArray();
        var results = await Task.WhenAll(targets.Select(value =>
            host.RequestAsync(value.EquipmentId, 6, 11,
                FactoryMessageBodyFactory.Create("large-message", 0, bytes), cancellationToken)))
            .ConfigureAwait(false);
        Require(results.All(value => value.Succeeded), "One or more concurrent large messages failed.");
        checks.Add($"Transferred {targets.Length} concurrent {bytes:N0}-byte SECS-II bodies without a transaction loss.");
    }

    private static async Task RunFaultIsolationAsync(
        FactoryHostRuntime host,
        FactoryEquipmentFleet fleet,
        FactoryScenarioRunOptions options,
        List<string> checks,
        CancellationToken cancellationToken)
    {
        Require(options.EquipmentCount >= 6, "Fault isolation requires at least six equipment contexts.");
        var results = await Task.WhenAll(host.Connections.Take(6).Select(value =>
            host.RequestAsync(value.EquipmentId, 6, 11, new SecsAsciiItem("FAULT-PROBE"), cancellationToken)))
            .ConfigureAwait(false);
        Require(!results[0].Succeeded, "The no-response equipment did not produce its expected T3 failure.");
        Require(results[1].Succeeded, "The deliberately slow equipment did not return within T3.");
        Require(!results[2].Succeeded, "The callback-exception equipment unexpectedly returned a response.");
        Require(results[3].Succeeded, "A throwing diagnostic sink escaped its isolation boundary.");
        Require(!results[4].Succeeded, "The close-before-response equipment unexpectedly completed its Primary.");
        Require(results[5].Succeeded, "The healthy control equipment failed during injected peer faults.");
        Require(fleet.BusinessRejectCount == 1,
            $"Expected exactly one injected callback rejection, observed {fleet.BusinessRejectCount}.");
        var healthy = host.Connections.Skip(5).First();
        var healthyResult = await host.RequestAsync(healthy.EquipmentId, 1, 1, null, cancellationToken).ConfigureAwait(false);
        Require(healthyResult.Succeeded, "A healthy equipment failed after faults were injected into other contexts.");
        await host.Host.Get(healthy.EquipmentId).LinktestAsync(cancellationToken).ConfigureAwait(false);
        checks.Add("Each no-response, slow, callback, diagnostic-sink and connection-close outcome matched its assigned equipment; a healthy peer continued S1F1 and Linktest.");
    }

    private static async Task<FactoryHostRuntime> RunHostRestartAsync(
        FactoryHostRuntime host,
        FactoryEquipmentFleet fleet,
        FactoryHostRuntimeOptions hostOptions,
        FactoryMetricsCollector metrics,
        List<string> checks,
        CancellationToken cancellationToken)
    {
        var previous = host.Connections.ToDictionary(value => value.EquipmentId, value => value.ConnectionId,
            StringComparer.OrdinalIgnoreCase);
        var ids = previous.Keys.ToArray();
        await host.DisconnectAllAsync(cancellationToken).ConfigureAwait(false);
        await host.DisposeAsync().ConfigureAwait(false);
        await fleet.RestartAsync(ids, cancellationToken).ConfigureAwait(false);
        var replacement = new FactoryHostRuntime(hostOptions, metrics);
        try
        {
            replacement.AddEquipment(fleet.Definitions);
            var connected = await replacement.ConnectAllAsync(cancellationToken).ConfigureAwait(false);
            await fleet.WaitForConnectionsAsync(cancellationToken).ConfigureAwait(false);
            Require(connected.Values.All(value => value.Succeeded), "Host restart did not restore every equipment connection.");
            Require(ids.All(value => replacement.Host.Get(value).ConnectionId != previous[value]),
                "Host restart reused a previous ConnectionId.");
            Require(replacement.PendingTransactionCount == 0, "Host restart inherited a pending transaction.");
            var probe = await replacement.RequestAsync(ids[0], 1, 1, null, cancellationToken).ConfigureAwait(false);
            Require(probe.Succeeded, "Host restart did not restore SECS-II request/response traffic.");
            checks.Add("Host restart rebuilt all contexts with fresh ConnectionIds, zero inherited transactions and restored Selected traffic.");
            return replacement;
        }
        catch (Exception failure)
        {
            try { await replacement.DisposeAsync().ConfigureAwait(false); }
            catch (Exception cleanup) { throw new AggregateException(failure, cleanup); }
            throw;
        }
    }

    private static async Task<IReadOnlyList<FactoryMessageResult>> RequestAllAsync(
        FactoryHostRuntime host,
        byte stream,
        byte function,
        Func<EquipmentConnectionContext, SecsItem?> itemFactory,
        CancellationToken cancellationToken) =>
        await Task.WhenAll(host.Connections.Select(value => host.RequestAsync(value.EquipmentId, stream, function,
            itemFactory(value), cancellationToken))).ConfigureAwait(false);

    private static async Task CaptureSnapshotAsync(
        FactoryHostRuntime host,
        FactoryEquipmentFleet fleet,
        FactoryResultExporter exporter,
        FactoryScenarioRunOptions options,
        List<FactoryMetricSnapshot> snapshots,
        TimeSpan due,
        CancellationToken cancellationToken)
    {
        var snapshot = host.CaptureSnapshot(fleet.LiveSessionCount, fleet.ListenerCount,
            fleet.CaptureQueueMetrics(), exporter.CaptureMetrics());
        snapshots.Add(snapshot);
        Console.WriteLine($"[{DateTimeOffset.Now:HH:mm:ss}] {options.Scenario} {due}: selected={snapshot.SelectedEquipment}, msg/s={snapshot.MessagesPerSecond:F1}, p95={snapshot.ResponseLatency.P95Milliseconds:F3} ms, working-set={snapshot.Process.WorkingSetBytes:N0}.");
        if (string.IsNullOrWhiteSpace(options.SnapshotDirectory)) return;
        var path = Path.Combine(Path.GetFullPath(options.SnapshotDirectory),
            $"{NormalizeScenario(options.Scenario)}-{Math.Max(1, (int)due.TotalMinutes):0000}m.json");
        await exporter.ExportSnapshotAsync(path, snapshot, cancellationToken).ConfigureAwait(false);
    }

    private static IReadOnlyList<TimeSpan> SnapshotTimes(FactoryScenarioRunOptions options, TimeSpan duration)
    {
        if (options.SnapshotInterval is not { } interval) return FactorySnapshotSchedule.Create(duration);
        var result = new List<TimeSpan>();
        for (var due = interval; due <= duration; due += interval) result.Add(due);
        return result;
    }

    private static FactoryMetricSnapshot PreserveLatestRateInterval(
        FactoryMetricSnapshot terminal,
        IReadOnlyList<FactoryMetricSnapshot> snapshots,
        string scenario)
    {
        var normalized = NormalizeScenario(scenario);
        if (normalized is not ("factory-normal" or "factory-busy" or "trace-burst" or "soak") ||
            terminal.RequestsPerSecond > 0 || terminal.ResponsesPerSecond > 0)
            return terminal;

        var active = snapshots.LastOrDefault(value =>
            value.RequestsPerSecond > 0 || value.ResponsesPerSecond > 0);
        return active is null
            ? terminal
            : terminal with
            {
                RequestsPerSecond = active.RequestsPerSecond,
                ResponsesPerSecond = active.ResponsesPerSecond,
                MessagesPerSecond = active.MessagesPerSecond,
                BytesPerSecond = active.BytesPerSecond
            };
    }

    private static SecsItem BuildBody(string profile, long sequence, int requestedBytes) =>
        FactoryMessageBodyFactory.Create(profile, sequence, requestedBytes);

    private static FactoryEquipmentBehavior SelectBehavior(string scenario, int zeroBasedIndex)
    {
        if (NormalizeScenario(scenario) != "fault-isolation") return FactoryEquipmentBehavior.Normal;
        return zeroBasedIndex switch
        {
            0 => FactoryEquipmentBehavior.NoResponse,
            1 => FactoryEquipmentBehavior.SlowResponse,
            2 => FactoryEquipmentBehavior.CallbackException,
            3 => FactoryEquipmentBehavior.DiagnosticSinkException,
            4 => FactoryEquipmentBehavior.CloseBeforeResponse,
            _ => FactoryEquipmentBehavior.Normal
        };
    }

    internal static bool HasFactoryResourceLeak(
        FactoryMetricSnapshot cleanup,
        FactoryMetricSnapshot baseline)
    {
        ArgumentNullException.ThrowIfNull(cleanup);
        ArgumentNullException.ThrowIfNull(baseline);
        var socketGrowth = cleanup.Process.Sockets is { IsProcessOwnedMeasurement: true, OpenSocketCount: { } current } &&
                           baseline.Process.Sockets is { IsProcessOwnedMeasurement: true, OpenSocketCount: { } initial } &&
                           current > initial;
        return cleanup.TrackedSessions != baseline.TrackedSessions ||
               cleanup.PendingTransactions != baseline.PendingTransactions ||
               cleanup.PendingControlTransactions != baseline.PendingControlTransactions ||
               cleanup.TrackedOperations != baseline.TrackedOperations ||
               cleanup.ReconnectOperations != baseline.ReconnectOperations ||
               cleanup.TrackedListeners > baseline.TrackedListeners ||
               cleanup.OwnedWorkers != baseline.OwnedWorkers ||
               socketGrowth;
    }

    internal static bool ExceedsStabilizationGrowth(int? before, int? after, int allowance) =>
        allowance < 0
            ? throw new ArgumentOutOfRangeException(nameof(allowance))
            : before is { } initial && after is { } current && current > initial + allowance;

    private static string OwnedWorkerEvidence(
        string phase,
        FactoryOwnedWorkerSnapshot current,
        FactoryOwnedWorkerSnapshot baseline) =>
        $"{phase} Factory-owned worker counts (current/baseline): " +
        $"Host message {current.HostMessageWorkers}/{baseline.HostMessageWorkers}, " +
        $"Equipment responder {current.EquipmentResponderWorkers}/{baseline.EquipmentResponderWorkers}, " +
        $"diagnostic drain {current.DiagnosticDrainWorkers}/{baseline.DiagnosticDrainWorkers}, " +
        $"result export {current.ResultExportWorkers}/{baseline.ResultExportWorkers}. " +
        "These counters cover Factory-owned loops only, not all CLR Tasks.";

    private static string NormalizeScenario(string scenario) => scenario.ToLowerInvariant() switch
    {
        "normal-factory" => "factory-normal",
        "busy-factory" => "factory-busy",
        _ => scenario.ToLowerInvariant()
    };

    private static TimeSpan DefaultDuration(FactoryScenarioRunOptions options, TimeSpan fallback) =>
        options.Duration > TimeSpan.Zero ? options.Duration : fallback;

    private static bool ResponseContainsEquipmentId(SecsItem? item, string equipmentId) => item switch
    {
        SecsAsciiItem ascii => ascii.Value.Equals(equipmentId, StringComparison.Ordinal),
        SecsListItem list => list.Items.Any(value => ResponseContainsEquipmentId(value, equipmentId)),
        _ => false
    };

    private static bool HasSustainedMemoryGrowth(IReadOnlyList<FactoryMetricSnapshot> snapshots)
    {
        // Exclude the empty-process baseline and the initial connected-fleet
        // sample. Startup/JIT growth is not a sustained soak leak signal.
        if (snapshots.Count < 7) return false;
        var tail = snapshots.Skip(2).TakeLast(Math.Min(6, snapshots.Count - 2))
            .Select(value => value.Process.WorkingSetBytes).ToArray();
        var monotonic = tail.Zip(tail.Skip(1), (left, right) => right > left).All(value => value);
        return monotonic && tail[^1] - tail[0] > 64L * 1024 * 1024;
    }

    private static string Delta(int? current, int? baseline) => current is { } value && baseline is { } initial
        ? (value - initial).ToString("+#,0;-#,0;0", System.Globalization.CultureInfo.InvariantCulture)
        : "unavailable";

    private static TimeSpan Min(TimeSpan left, TimeSpan right) => left <= right ? left : right;

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
