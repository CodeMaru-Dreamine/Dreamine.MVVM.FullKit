using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dreamine.Communication.Abstractions.Enums;
using Dreamine.Secs.Abstractions.Enums;
using Dreamine.Secs.Abstractions.Model;
using Dreamine.SecsGem.FactoryScale.Export;
using Dreamine.SecsGem.FactoryScale.Infrastructure;
using Dreamine.SecsGem.FactoryScale.Metrics;
using Dreamine.SecsGem.FactoryScale.Models;
using Dreamine.SecsGem.FactoryScale.Runtime;
using Dreamine.SecsGem.FactoryScale.Scenarios;
using Dreamine.SecsGem.FactoryScale.Simulation;
using Dreamine.SecsGem.Interop.Runtime;

namespace Dreamine.SecsGem.FactoryScale.Cli;

internal sealed class FactoryCommandExecutor : IFactoryCommandExecutor
{
    private static readonly TimeSpan DefaultHeartbeatInterval = TimeSpan.FromSeconds(1);
    private const int WorkerCleanupThreadGrowthAllowance = 2;
    private const int WorkerCleanupHandleGrowthAllowance = 4;

    public Task<FactoryExecutionResult> ExecuteAsync(
        FactoryCommandLine command,
        CancellationToken cancellationToken) => command.Command switch
        {
            FactoryCommandKind.Worker => RunEquipmentProcessAsync(command, isWorker: true, cancellationToken),
            FactoryCommandKind.Equipment => RunEquipmentProcessAsync(command, isWorker: false, cancellationToken),
            FactoryCommandKind.Host => RunHostOnlyAsync(command, cancellationToken),
            FactoryCommandKind.MultiProcess => RunMultiProcessAsync(command, cancellationToken),
            FactoryCommandKind.InProcess => RunInProcessAsync(command, cancellationToken),
            FactoryCommandKind.Scenario when command.GetString("mode", "in-process")
                .Equals("multi-process", StringComparison.OrdinalIgnoreCase) =>
                RunMultiProcessAsync(command, cancellationToken),
            FactoryCommandKind.Scenario => RunInProcessAsync(command, cancellationToken),
            _ => Task.FromResult(FactoryExecutionResult.Failed(
                $"Unsupported Factory command '{command.Command}'."))
        };

    private static async Task<FactoryExecutionResult> RunInProcessAsync(
        FactoryCommandLine command,
        CancellationToken cancellationToken)
    {
        var runId = ResolveRunId(command);
        var options = BuildScenarioOptions(command, "in-process");
        var outputPath = ResolveOutputPath(command, runId, $"{NormalizeScenario(options.Scenario)}.json");
        await using var exporter = new FactoryResultExporter(options.ExportQueueCapacity);
        var result = await new FactoryScenarioCoordinator()
            .RunInProcessAsync(options, exporter, cancellationToken)
            .ConfigureAwait(false);
        await exporter.ExportResultAsync(outputPath, result, CancellationToken.None).ConfigureAwait(false);
        return MapResult(result, outputPath, cancellationToken);
    }

    private static async Task<FactoryExecutionResult> RunMultiProcessAsync(
        FactoryCommandLine command,
        CancellationToken cancellationToken)
    {
        var runId = ResolveRunId(command);
        var options = BuildScenarioOptions(command, "multi-process");
        options.Validate();
        var controlDirectory = ResolveControlDirectory(command, runId);
        var outputPath = ResolveOutputPath(command, runId, $"{NormalizeScenario(options.Scenario)}.json");
        var programPath = command.GetString("worker-executable", WorkerProcessCoordinator.ResolveCurrentProgramPath());
        var launchPlan = BuildWorkerLaunchPlan(command, options, runId, controlDirectory, programPath);

        await using var processes = new WorkerProcessCoordinator();
        await using var exporter = new FactoryResultExporter(options.ExportQueueCapacity);
        FactoryRunResult? result = null;
        IReadOnlyList<WorkerProcessResult> stopped = [];
        try
        {
            var ready = await processes.StartAllAsync(
                launchPlan.Select(value => value.Request),
                Math.Min(options.ConnectConcurrency, launchPlan.Count),
                cancellationToken).ConfigureAwait(false);
            var workerManifests = await ReadWorkerManifestsAsync(
                launchPlan,
                ready,
                controlDirectory,
                cancellationToken).ConfigureAwait(false);
            var aggregate = BuildAggregateManifest(runId, workerManifests);
            Require(aggregate.Equipment.Count == options.EquipmentCount,
                $"Workers published {aggregate.Equipment.Count}/{options.EquipmentCount} endpoints.");
            ValidateEndpointManifest(aggregate);

            var aggregateManifestPath = command.HasOption("manifest")
                ? Path.GetFullPath(command.GetString("manifest"))
                : RunArtifactPath(controlDirectory, runId, "all", "endpoints.json");
            EnsureDistinctArtifacts(outputPath, aggregateManifestPath);
            await WorkerFileProtocol.WriteJsonAtomicAsync(
                aggregateManifestPath,
                aggregate,
                cancellationToken).ConfigureAwait(false);
            Console.WriteLine($"Worker endpoint manifest: {aggregateManifestPath}");

            var normalizedScenario = NormalizeScenario(options.Scenario);
            if (normalizedScenario == "host-restart")
            {
                result = await RunMultiProcessHostRestartAsync(
                    command,
                    options,
                    processes,
                    launchPlan,
                    workerManifests,
                    aggregateManifestPath,
                    WorkerProcessCoordinator.ResolveCurrentProgramPath(),
                    controlDirectory,
                    runId,
                    cancellationToken).ConfigureAwait(false);
            }
            else
            {
                Func<FactoryHostRuntime, List<string>, CancellationToken, Task>? specialStage = null;
                if (normalizedScenario == "reconnect-storm")
                {
                    specialStage = (host, checks, token) => RunMultiProcessReconnectAsync(
                        host,
                        processes,
                        launchPlan,
                        workerManifests,
                        options.DisconnectCount == 0 ? Math.Min(100, options.EquipmentCount) : options.DisconnectCount,
                        checks,
                        token);
                }

                result = await RunHostValidationAsync(
                    options,
                    aggregate.Equipment.Select(ToDefinition).ToArray(),
                    "Multi-process self-loopback evidence; not external-equipment compatibility or compliance evidence",
                    specialStage,
                    cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            stopped = await processes.StopAllAsync(CancellationToken.None).ConfigureAwait(false);
        }

        if (result is null)
            return FactoryExecutionResult.Failed("Multi-process Factory validation did not produce a result.");

        var parsedWorkers = stopped.Select(value => (Process: value, Runtime: TryParseWorkerResult(value)))
            .ToArray();
        var workerExitCheck = parsedWorkers.Length == launchPlan.Count &&
                              parsedWorkers.All(value => IsCleanWorkerResult(value.Process, value.Runtime));
        var checks = new List<string>(result.Checks)
        {
            workerExitCheck
                ? $"All {stopped.Count} worker processes published zero-resource cleanup results and exited gracefully."
                : $"FAILED worker shutdown/cleanup: {stopped.Count}/{launchPlan.Count} results, " +
                  $"exit codes [{string.Join(", ", stopped.Select(value => value.ExitCode))}], " +
                  $"forced kills {stopped.Count(value => value.KilledAfterGracefulTimeout)}.",
            "Final/cleanup metric snapshots describe the Host process only; per-worker process and cleanup snapshots follow and are not overwritten or presented as a summed process metric."
        };
        checks.AddRange(parsedWorkers.Select(value => value.Runtime is null
            ? $"FAILED {value.Process.WorkerId}: worker result JSON was missing or invalid."
            : $"{value.Process.WorkerId} PID {value.Runtime.ProcessId} cleanup: " +
              $"sessions={value.Runtime.AfterCleanup.TrackedSessions}, " +
              $"listeners={value.Runtime.AfterCleanup.TrackedListeners}, " +
              $"operations={value.Runtime.AfterCleanup.TrackedOperations?.ToString(CultureInfo.InvariantCulture) ?? "unavailable"}, " +
              $"queue={value.Runtime.AfterCleanup.QueueDepth}, " +
              $"OS-open-sockets={value.Runtime.AfterCleanup.OperatingSystemOpenSockets?.ToString(CultureInfo.InvariantCulture) ?? "unavailable"}, " +
              $"working-set={value.Runtime.AfterCleanup.WorkingSetBytes}, " +
              $"managed-heap={value.Runtime.AfterCleanup.ManagedHeapBytes}, " +
              $"handles={value.Runtime.AfterCleanup.HandleCount?.ToString(CultureInfo.InvariantCulture) ?? "unavailable"}."));
        result = result with
        {
            Checks = checks.ToArray(),
            Status = workerExitCheck ? result.Status : FactoryRunStatus.Failed,
            FirstFailure = workerExitCheck
                ? result.FirstFailure
                : result.FirstFailure ?? "One or more equipment worker processes failed to stop cleanly."
        };

        await exporter.ExportResultAsync(outputPath, result, CancellationToken.None).ConfigureAwait(false);
        return MapResult(result, outputPath, cancellationToken);
    }

    private static async Task<FactoryExecutionResult> RunHostOnlyAsync(
        FactoryCommandLine command,
        CancellationToken cancellationToken)
    {
        if (!command.HasOption("manifest"))
            return FactoryExecutionResult.Failed(
                "Host-only mode requires --manifest so endpoint, mode, Session ID and timer values are explicit. " +
                "It never starts an external simulator automatically.");

        var runId = ResolveRunId(command);
        var manifestPath = Path.GetFullPath(command.GetString("manifest"));
        var manifest = await WorkerFileProtocol.ReadJsonAsync<FactoryEndpointManifest>(
            manifestPath,
            cancellationToken).ConfigureAwait(false);
        ValidateEndpointManifest(manifest);
        var requested = command.HasOption("equipment-count")
            ? command.GetInt32("equipment-count")
            : manifest.Equipment.Count;
        if (manifest.Equipment.Count < requested)
            return FactoryExecutionResult.Failed(
                $"Manifest contains {manifest.Equipment.Count} endpoints but {requested} were requested.");

        var options = BuildScenarioOptions(command, "multi-process") with
        {
            Scenario = "factory-smoke",
            ExecutionMode = "host-only",
            EquipmentCount = requested,
            DisconnectCount = 0
        };
        var outputPath = ResolveOutputPath(command, runId, "host-only.json");
        EnsureDistinctArtifacts(outputPath, manifestPath);
        await using var exporter = new FactoryResultExporter(options.ExportQueueCapacity);
        var result = await RunHostValidationAsync(
            options,
            manifest.Equipment.Take(requested).Select(ToDefinition).ToArray(),
            "Configured host-only endpoints; external compatibility and compliance are not asserted",
            specialStage: null,
            cancellationToken).ConfigureAwait(false);
        await exporter.ExportResultAsync(outputPath, result, CancellationToken.None).ConfigureAwait(false);
        return MapResult(result, outputPath, cancellationToken);
    }

    private static async Task<FactoryExecutionResult> RunEquipmentProcessAsync(
        FactoryCommandLine command,
        bool isWorker,
        CancellationToken cancellationToken)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var runId = ResolveRunId(command);
        var workerId = isWorker
            ? command.GetString("worker-id", $"worker-{command.GetInt32("start-index", 1):000000}")
            : "equipment";
        var startIndex = command.GetInt32("start-index", 1);
        var count = command.GetInt32("count", command.GetInt32("equipment-count", isWorker ? 1 : 100));
        if (isWorker && count > 100)
            return FactoryExecutionResult.Failed("A worker process may own at most 100 equipment endpoints.");

        var controlDirectory = ResolveControlDirectory(command, runId);
        var paths = WorkerFileProtocol.Paths(controlDirectory, runId, workerId);
        var manifestPath = RunArtifactPath(controlDirectory, runId, workerId, "endpoints.json");
        var resultPath = ResolveOutputPath(command, runId, $"{workerId}.result.json");
        EnsureDistinctArtifacts(resultPath, manifestPath);
        Directory.CreateDirectory(controlDirectory);
        foreach (var path in new[] { paths.ReadyPath, paths.HeartbeatPath, paths.StopPath, manifestPath, resultPath })
            WorkerFileProtocol.DeleteForNewRun(path);

        var rangeStart = command.GetInt32("port-range-start", 20_000);
        var rangeEnd = command.GetInt32("port-range-end", 48_000);
        var receiveCapacity = command.GetInt32("receive-queue-capacity", 4_096);
        var responders = Math.Clamp(count / 4, 4, 64);
        var fleetOptions = new FactoryEquipmentFleetOptions(
            receiveCapacity,
            responders,
            Math.Min(64, count));
        var fleet = new FactoryEquipmentFleet(
            fleetOptions,
            new ValidatedPortRangeManager(rangeStart, rangeEnd, startIndex));

        try
        {
            await fleet.StartAsync(startIndex, count, cancellationToken).ConfigureAwait(false);
            var manifest = new FactoryEndpointManifest(
                1,
                runId,
                workerId,
                Environment.ProcessId,
                DateTimeOffset.UtcNow,
                fleet.Definitions.Select(FromDefinition).ToArray(),
                "Self-loopback equipment simulator endpoints; external compatibility is not asserted");
            await WorkerFileProtocol.WriteJsonAtomicAsync(manifestPath, manifest, cancellationToken)
                .ConfigureAwait(false);
            await WorkerFileProtocol.PublishReadyAsync(
                paths,
                new WorkerReadyRecord(
                    runId,
                    workerId,
                    Environment.ProcessId,
                    startIndex,
                    count,
                    DateTimeOffset.UtcNow,
                    manifestPath),
                cancellationToken).ConfigureAwait(false);
            Console.WriteLine($"{(isWorker ? "Worker" : "Equipment fleet")} ready: " +
                              $"{count} listeners; manifest {manifestPath}");

            using var heartbeatCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var heartbeatInterval = command.GetTimeSpan("heartbeat-interval", DefaultHeartbeatInterval);
            var heartbeat = RunHeartbeatLoopAsync(
                paths,
                runId,
                workerId,
                fleet,
                heartbeatInterval,
                heartbeatCancellation.Token);
            string stopReason;
            try
            {
                stopReason = await WaitForEquipmentTerminationAsync(
                    paths,
                    runId,
                    workerId,
                    command.HasOption("duration") ? command.GetTimeSpan("duration", TimeSpan.Zero) : null,
                    cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                heartbeatCancellation.Cancel();
                try { await heartbeat.ConfigureAwait(false); }
                catch (OperationCanceledException) when (heartbeatCancellation.IsCancellationRequested) { }
            }

            var cleanup = await CleanupWorkerFleetAsync(fleet).ConfigureAwait(false);
            var cleanupPassed = cleanup.Failure is null && cleanup.After.TrackedSessions == 0 &&
                                cleanup.After.TrackedListeners == 0 && cleanup.After.TrackedOperations == 0 &&
                                cleanup.After.QueueDepth == 0 && cleanup.After.OperatingSystemOpenSockets == 0 &&
                                !FactoryScenarioCoordinator.ExceedsStabilizationGrowth(
                                    cleanup.StabilizationBaseline.ThreadCount, cleanup.After.ThreadCount,
                                    WorkerCleanupThreadGrowthAllowance) &&
                                !FactoryScenarioCoordinator.ExceedsStabilizationGrowth(
                                    cleanup.StabilizationBaseline.HandleCount, cleanup.After.HandleCount,
                                    WorkerCleanupHandleGrowthAllowance);
            var runtimeResult = new FactoryWorkerRuntimeResult(
                1,
                runId,
                workerId,
                Environment.ProcessId,
                startedAt,
                DateTimeOffset.UtcNow,
                cleanupPassed ? FactoryRunStatus.Passed : FactoryRunStatus.Failed,
                count,
                fleet.EnqueuedResponseCount,
                fleet.SentResponseCount,
                fleet.BusinessRejectCount,
                fleet.PeakQueueDepth,
                cleanup.Before,
                cleanup.After,
                manifestPath,
                stopReason,
                cleanupPassed
                    ? null
                    : cleanup.Failure ??
                      "Worker cleanup left a tracked Factory resource/OS socket or grew thread/handle counts between cleanup stabilization samples.")
            {
                CleanupStabilizationBaseline = cleanup.StabilizationBaseline
            };
            await WorkerFileProtocol.WriteJsonAtomicAsync(resultPath, runtimeResult, CancellationToken.None)
                .ConfigureAwait(false);
            return cleanupPassed
                ? FactoryExecutionResult.Success(
                    $"{(isWorker ? "Worker" : "Equipment-only simulator")} {workerId} stopped cleanly ({stopReason}).",
                    resultPath)
                : FactoryExecutionResult.Failed(
                    $"{(isWorker ? "Worker" : "Equipment-only simulator")} {workerId} cleanup failed: {runtimeResult.Failure}",
                    resultPath);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            var cleanup = await CleanupWorkerFleetAsync(fleet).ConfigureAwait(false);
            var canceled = new FactoryWorkerRuntimeResult(
                1,
                runId,
                workerId,
                Environment.ProcessId,
                startedAt,
                DateTimeOffset.UtcNow,
                FactoryRunStatus.Cancelled,
                count,
                fleet.EnqueuedResponseCount,
                fleet.SentResponseCount,
                fleet.BusinessRejectCount,
                fleet.PeakQueueDepth,
                cleanup.Before,
                cleanup.After,
                File.Exists(manifestPath) ? manifestPath : null,
                "caller-cancellation",
                cleanup.Failure is null
                    ? "Run canceled by the caller."
                    : $"Run canceled; cleanup also failed: {cleanup.Failure}")
            {
                CleanupStabilizationBaseline = cleanup.StabilizationBaseline
            };
            await WorkerFileProtocol.WriteJsonAtomicAsync(resultPath, canceled, CancellationToken.None)
                .ConfigureAwait(false);
            throw;
        }
        catch (Exception exception)
        {
            var cleanup = await CleanupWorkerFleetAsync(fleet).ConfigureAwait(false);
            var failed = new FactoryWorkerRuntimeResult(
                1,
                runId,
                workerId,
                Environment.ProcessId,
                startedAt,
                DateTimeOffset.UtcNow,
                FactoryRunStatus.Failed,
                count,
                fleet.EnqueuedResponseCount,
                fleet.SentResponseCount,
                fleet.BusinessRejectCount,
                fleet.PeakQueueDepth,
                cleanup.Before,
                cleanup.After,
                File.Exists(manifestPath) ? manifestPath : null,
                "runtime-failure",
                cleanup.Failure is null
                    ? $"{exception.GetType().Name}: {exception.Message}"
                    : $"{exception.GetType().Name}: {exception.Message}; cleanup: {cleanup.Failure}")
            {
                CleanupStabilizationBaseline = cleanup.StabilizationBaseline
            };
            await WorkerFileProtocol.WriteJsonAtomicAsync(resultPath, failed, CancellationToken.None)
                .ConfigureAwait(false);
            return FactoryExecutionResult.Failed(
                $"{(isWorker ? "Worker" : "Equipment-only simulator")} {workerId} failed: {failed.Failure}",
                resultPath);
        }
    }

    private static async Task<FactoryRunResult> RunHostValidationAsync(
        FactoryScenarioRunOptions options,
        IReadOnlyList<EquipmentConnectionDefinition> definitions,
        string evidenceScope,
        Func<FactoryHostRuntime, List<string>, CancellationToken, Task>? specialStage,
        CancellationToken cancellationToken)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var metrics = new FactoryMetricsCollector(WindowsProcessSocketMetricsProvider.Instance);
        var checks = new List<string>();
        var snapshots = new List<FactoryMetricSnapshot>();
        var baseline = FactoryCleanupSnapshot.Capture(metrics);
        snapshots.Add(baseline);
        checks.Add($"Host process baseline: working set {baseline.Process.WorkingSetBytes:N0}, handles {baseline.Process.HandleCount?.ToString(CultureInfo.InvariantCulture) ?? "unavailable"}, OS open sockets {baseline.Process.Sockets.OpenSocketCount?.ToString(CultureInfo.InvariantCulture) ?? "unavailable"}.");
        FactoryHostRuntime? host = null;
        FactoryMetricSnapshot? final = null;
        IReadOnlyList<FactoryEquipmentMetricSnapshot> equipmentMetrics = [];
        var status = FactoryRunStatus.Passed;
        string? firstFailure = null;
        try
        {
            Require(definitions.Count == options.EquipmentCount,
                $"Host received {definitions.Count}/{options.EquipmentCount} endpoint definitions.");
            foreach (var definition in definitions) definition.Validate();
            var hostOptions = new FactoryHostRuntimeOptions(
                options.ConnectConcurrency,
                options.ReconnectConcurrency,
                options.MessageConcurrency,
                Math.Max(options.ReceiveQueueCapacity, options.MessageConcurrency),
                options.PerEquipmentPendingLimit,
                Math.Max(options.GlobalPendingLimit, options.MessageConcurrency),
                options.LogQueueCapacity);
            host = new FactoryHostRuntime(hostOptions, metrics);
            host.AddEquipment(definitions);
            var connectTimer = Stopwatch.StartNew();
            var connected = await host.ConnectAllAsync(cancellationToken).ConfigureAwait(false);
            connectTimer.Stop();
            Require(connected.Count == definitions.Count && connected.Values.All(value => value.Succeeded),
                $"Only {connected.Values.Count(value => value.Succeeded)}/{definitions.Count} endpoints connected.");
            Require(host.Connections.Count(value => value.IsSelected) == definitions.Count,
                "Not every host context reached HSMS Selected.");
            checks.Add($"TCP connect + HSMS Select passed for {definitions.Count}/{definitions.Count} in {connectTimer.Elapsed}.");
            snapshots.Add(host.CaptureSnapshot(0, 0));

            var linktests = await host.LinktestAllAsync(cancellationToken).ConfigureAwait(false);
            Require(linktests.Values.All(value => value.Succeeded), "One or more Linktest transactions failed.");
            var s1f1 = await Task.WhenAll(host.Connections.Select(value =>
                host.RequestAsync(value.EquipmentId, 1, 1, null, cancellationToken))).ConfigureAwait(false);
            Require(s1f1.All(value => value.Succeeded), "One or more S1F1/S1F2 transactions failed.");
            var s1f13 = await Task.WhenAll(host.Connections.Select(value =>
                host.RequestAsync(value.EquipmentId, 1, 13, new SecsListItem(), cancellationToken)))
                .ConfigureAwait(false);
            Require(s1f13.All(value => value.Succeeded), "One or more S1F13/S1F14 transactions failed.");
            checks.Add($"Linktest, S1F1/S1F2 and S1F13/S1F14 passed for {definitions.Count} equipment.");

            await RunHostScenarioStageAsync(host, options, specialStage, checks, cancellationToken)
                .ConfigureAwait(false);
            final = host.CaptureSnapshot(0, 0);
            snapshots.Add(final);
            Require(final.CorrelationErrors == 0, "A transaction correlation error was observed.");
            Require(final.Queues.All(value => value.Dropped == 0 || value.Name == "diagnostic-log"),
                "A business queue reported dropped work.");
            Require(final.Queues.All(value => value.PeakDepth <= value.Capacity),
                "A bounded queue exceeded its declared capacity.");
            Require(final.Queues.Where(value => value.Name != "diagnostic-log")
                    .All(value => value.Depth == 0 && value.Rejected == 0),
                "A business queue did not drain or rejected protocol work.");
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
        }

        var cleanupBeforeGc = FactoryCleanupSnapshot.Capture(metrics);
        checks.Add($"Host cleanup before forced GC: sessions {cleanupBeforeGc.TrackedSessions}, " +
                   $"pending {cleanupBeforeGc.PendingTransactions}, Factory-owned live operations/workers " +
                   $"{cleanupBeforeGc.TrackedOperations} (baseline {baseline.TrackedOperations}).");
        checks.Add(OwnedWorkerEvidence("Host cleanup", cleanupBeforeGc.OwnedWorkers, baseline.OwnedWorkers));
        if (FactoryScenarioCoordinator.HasFactoryResourceLeak(cleanupBeforeGc, baseline))
        {
            status = FactoryRunStatus.Failed;
            firstFailure ??= "Tracked host resources remained after deterministic cleanup.";
        }

        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        var cleanupAfterGc = FactoryCleanupSnapshot.Capture(metrics);
        checks.Add(OwnedWorkerEvidence("Host post-GC cleanup", cleanupAfterGc.OwnedWorkers, baseline.OwnedWorkers));
        if (FactoryScenarioCoordinator.HasFactoryResourceLeak(cleanupAfterGc, baseline) ||
            FactoryScenarioCoordinator.ExceedsStabilizationGrowth(
                cleanupBeforeGc.Process.ThreadCount, cleanupAfterGc.Process.ThreadCount, 2) ||
            FactoryScenarioCoordinator.ExceedsStabilizationGrowth(
                cleanupBeforeGc.Process.HandleCount, cleanupAfterGc.Process.HandleCount, 4))
        {
            status = FactoryRunStatus.Failed;
            firstFailure ??= "Host cleanup did not return Factory-owned workers to baseline or OS resource evidence grew during stabilization.";
        }
        final ??= snapshots.LastOrDefault() ?? cleanupBeforeGc;
        return new FactoryRunResult(
            1,
            options.Scenario,
            options.ExecutionMode,
            startedAt,
            DateTimeOffset.UtcNow,
            status,
            final,
            cleanupBeforeGc,
            cleanupAfterGc,
            snapshots,
            checks,
            firstFailure,
            evidenceScope)
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

    private static async Task RunHostScenarioStageAsync(
        FactoryHostRuntime host,
        FactoryScenarioRunOptions options,
        Func<FactoryHostRuntime, List<string>, CancellationToken, Task>? specialStage,
        List<string> checks,
        CancellationToken cancellationToken)
    {
        switch (NormalizeScenario(options.Scenario))
        {
            case "factory-smoke":
            case "scale":
                await RunCollisionProbeAsync(host, checks, cancellationToken).ConfigureAwait(false);
                return;
            case "idle-factory":
                await RunIdleHostAsync(host, EffectiveDuration(options, TimeSpan.FromHours(1)), checks,
                    cancellationToken).ConfigureAwait(false);
                return;
            case "factory-normal":
                await RunRateHostAsync(host, EffectiveDuration(options, TimeSpan.FromHours(1)),
                    options.MessagesPerSecond > 0 ? options.MessagesPerSecond : options.EquipmentCount,
                    "normal", options.MessageBytes, checks, cancellationToken).ConfigureAwait(false);
                return;
            case "factory-busy":
                await RunRateHostAsync(host, EffectiveDuration(options, TimeSpan.FromHours(1)),
                    options.MessagesPerSecond > 0 ? options.MessagesPerSecond : options.EquipmentCount * 10d,
                    "busy", options.MessageBytes, checks, cancellationToken).ConfigureAwait(false);
                return;
            case "trace-burst":
                await RunRateHostAsync(host, EffectiveDuration(options, TimeSpan.FromMinutes(1)),
                    options.MessagesPerSecond > 0 ? options.MessagesPerSecond : options.EquipmentCount * 50d,
                    "trace", options.MessageBytes, checks, cancellationToken).ConfigureAwait(false);
                return;
            case "large-message":
                await RunLargeMessageHostAsync(host, options.MessageBytes, checks, cancellationToken)
                    .ConfigureAwait(false);
                return;
            case "soak":
                await RunRateHostAsync(host, EffectiveDuration(options, TimeSpan.FromHours(1)),
                    options.MessagesPerSecond > 0 ? options.MessagesPerSecond : Math.Max(1, options.EquipmentCount / 60d),
                    "soak", options.MessageBytes, checks, cancellationToken).ConfigureAwait(false);
                return;
            case "reconnect-storm" when specialStage is not null:
                await specialStage(host, checks, cancellationToken).ConfigureAwait(false);
                return;
            case "reconnect-storm":
                throw new InvalidOperationException(
                    "Reconnect-storm requires a self-loopback worker control plane; host-only mode cannot restart external endpoints.");
            case "host-restart":
                throw new InvalidOperationException(
                    "Multi-process host-restart requires a separately supervised Host process and is not represented by this in-process coordinator. " +
                    "Use the in-process host-restart scenario until that supervisor is configured.");
            case "fault-isolation":
                throw new InvalidOperationException(
                    "Multi-process fault injection is not enabled because the worker stop protocol does not accept behavior commands.");
            default:
                throw new InvalidOperationException($"Unsupported Factory scenario '{options.Scenario}'.");
        }
    }

    private static async Task RunCollisionProbeAsync(
        FactoryHostRuntime host,
        List<string> checks,
        CancellationToken cancellationToken)
    {
        var targets = host.Connections.Take(2).ToArray();
        if (targets.Length < 2)
        {
            checks.Add("Collision probe skipped because fewer than two equipment contexts were requested.");
            return;
        }

        var systemBytes = new SecsSystemBytes(1);
        var body = new SecsAsciiItem("IDENTICAL-BODY");
        var results = await Task.WhenAll(targets.Select(value =>
            host.RequestAsync(value.EquipmentId, 1, 1, body, cancellationToken, systemBytes)))
            .ConfigureAwait(false);
        Require(results.All(value => value.Succeeded && value.Response?.SystemBytes == systemBytes),
            "Identical System Bytes were not isolated by connection.");
        checks.Add("Identical Session ID/System Bytes/S1F1/body remained isolated across two connections.");
    }

    private static async Task RunIdleHostAsync(
        FactoryHostRuntime host,
        TimeSpan duration,
        List<string> checks,
        CancellationToken cancellationToken)
    {
        var timer = Stopwatch.StartNew();
        while (timer.Elapsed < duration)
        {
            var links = await host.LinktestAllAsync(cancellationToken).ConfigureAwait(false);
            Require(links.Values.All(value => value.Succeeded), "Idle Factory Linktest failed.");
            var remaining = duration - timer.Elapsed;
            if (remaining > TimeSpan.Zero)
                await Task.Delay(remaining < TimeSpan.FromMinutes(1) ? remaining : TimeSpan.FromMinutes(1),
                    cancellationToken).ConfigureAwait(false);
        }
        checks.Add($"Idle Factory held {host.Connections.Count} selected equipment for {timer.Elapsed}.");
    }

    private static async Task RunRateHostAsync(
        FactoryHostRuntime host,
        TimeSpan duration,
        double targetRate,
        string profile,
        int requestedBytes,
        List<string> checks,
        CancellationToken cancellationToken)
    {
        var timer = Stopwatch.StartNew();
        long scheduled = 0;
        var cursor = 0;
        while (timer.Elapsed < duration)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var desired = (long)Math.Floor(timer.Elapsed.TotalSeconds * targetRate);
            var due = Math.Min(desired - scheduled, Math.Max(1, host.Connections.Count * 2L));
            if (due <= 0)
            {
                await Task.Delay(10, cancellationToken).ConfigureAwait(false);
                continue;
            }

            var batch = new Task<FactoryMessageResult>[checked((int)due)];
            for (var index = 0; index < batch.Length; index++)
            {
                var equipment = host.Connections[cursor++ % host.Connections.Count];
                batch[index] = host.RequestAsync(
                    equipment.EquipmentId,
                    6,
                    11,
                    BuildHostBody(profile, scheduled + index, requestedBytes),
                    cancellationToken);
            }
            var results = await Task.WhenAll(batch).ConfigureAwait(false);
            Require(results.All(value => value.Succeeded), $"{profile} profile lost an accepted request.");
            scheduled += batch.Length;
        }

        var achieved = scheduled / Math.Max(0.001, timer.Elapsed.TotalSeconds);
        Require(achieved >= targetRate * 0.90,
            $"Achieved {achieved:F2} msg/s, below 90% of target {targetRate:F2} msg/s.");
        checks.Add($"{profile} profile completed {scheduled:N0} request/response pairs at {achieved:F2} msg/s.");
    }

    private static async Task RunLargeMessageHostAsync(
        FactoryHostRuntime host,
        int requestedBytes,
        List<string> checks,
        CancellationToken cancellationToken)
    {
        var bytes = requestedBytes == 0 ? 1024 * 1024 : requestedBytes;
        var payload = new byte[bytes];
        for (var index = 0; index < payload.Length; index += 4_096) payload[index] = (byte)(index / 4_096);
        var targets = host.Connections.Take(Math.Min(16, host.Connections.Count)).ToArray();
        var results = await Task.WhenAll(targets.Select(value =>
            host.RequestAsync(value.EquipmentId, 6, 11, new SecsBinaryItem(payload), cancellationToken)))
            .ConfigureAwait(false);
        Require(results.All(value => value.Succeeded), "One or more concurrent large messages failed.");
        checks.Add($"Transferred {targets.Length} concurrent {bytes:N0}-byte SECS-II bodies.");
    }

    private static async Task RunMultiProcessReconnectAsync(
        FactoryHostRuntime host,
        WorkerProcessCoordinator processes,
        IReadOnlyList<WorkerPlan> launchPlan,
        IReadOnlyDictionary<string, FactoryEndpointManifest> manifests,
        int requestedDisconnectCount,
        List<string> checks,
        CancellationToken cancellationToken)
    {
        var selectedPlans = new List<WorkerPlan>();
        var affected = 0;
        foreach (var plan in launchPlan)
        {
            selectedPlans.Add(plan);
            affected += plan.Request.Count;
            if (affected >= requestedDisconnectCount) break;
        }
        var equipmentIds = selectedPlans
            .SelectMany(plan => manifests[plan.Request.WorkerId].Equipment)
            .Select(value => value.EquipmentId)
            .ToArray();
        var unaffected = host.Connections.FirstOrDefault(value =>
            !equipmentIds.Contains(value.EquipmentId, StringComparer.OrdinalIgnoreCase));
        var timer = Stopwatch.StartNew();
        var stopResults = await Task.WhenAll(selectedPlans.Select(plan =>
            processes.StopAsync(plan.Request.WorkerId, cancellationToken))).ConfigureAwait(false);
        Require(stopResults.All(value => value is not null &&
                                         IsCleanWorkerResult(value, TryParseWorkerResult(value))),
            "One or more reconnect target workers did not publish zero-resource cleanup and stop gracefully.");
        checks.Add($"The {stopResults.Length} pre-restart worker processes reported zero sessions, listeners, operations, queues and OS sockets before exit.");
        await processes.StartAllAsync(
            selectedPlans.Select(value => value.Request),
            Math.Min(selectedPlans.Count, 8),
            cancellationToken).ConfigureAwait(false);

        if (unaffected is not null)
        {
            var probe = await host.RequestAsync(unaffected.EquipmentId, 1, 1, null, cancellationToken)
                .ConfigureAwait(false);
            Require(probe.Succeeded, "Unaffected equipment traffic failed during worker restart.");
        }
        var reconnect = await host.ReconnectAsync(equipmentIds, cancellationToken).ConfigureAwait(false);
        timer.Stop();
        Require(reconnect.Values.All(value => value.Succeeded), "One or more restarted worker endpoints did not reconnect.");
        checks.Add($"Worker restart recovered {equipmentIds.Length} endpoints in {timer.Elapsed}; " +
                   $"requested disconnect count was {requestedDisconnectCount} (worker-granularity restart).");
    }

    private static async Task<FactoryRunResult> RunMultiProcessHostRestartAsync(
        FactoryCommandLine command,
        FactoryScenarioRunOptions options,
        WorkerProcessCoordinator workers,
        IReadOnlyList<WorkerPlan> launchPlan,
        IReadOnlyDictionary<string, FactoryEndpointManifest> initialManifests,
        string aggregateManifestPath,
        string programPath,
        string controlDirectory,
        string runId,
        CancellationToken cancellationToken)
    {
        var executable = Path.GetFullPath(programPath);
        var workingDirectory = Path.GetDirectoryName(executable)
                               ?? throw new InvalidOperationException("Host program has no parent directory.");
        var completionTimeout = command.HasOption("ready-timeout")
            ? command.GetTimeSpan("ready-timeout", TimeSpan.FromMinutes(5))
            : TimeSpan.FromMinutes(5);
        var hostOptions = BuildHostChildOptions(options);
        var supervisor = new HostProcessCoordinator();
        var firstPath = RunArtifactPath(controlDirectory, runId, "host-1", "result.json");
        var secondPath = RunArtifactPath(controlDirectory, runId, "host-2", "result.json");
        EnsureDistinctArtifacts(firstPath, aggregateManifestPath);
        EnsureDistinctArtifacts(secondPath, aggregateManifestPath);

        var firstProcess = await supervisor.RunAsync(
            new HostProcessLaunchRequest(
                ChildHostRunId(runId, 1),
                executable,
                workingDirectory,
                aggregateManifestPath,
                firstPath,
                options.EquipmentCount,
                hostOptions,
                completionTimeout),
            cancellationToken).ConfigureAwait(false);
        var first = ParseAndValidateHostChild(firstProcess, options.EquipmentCount, "first");

        await WaitForWorkersListeningAsync(
            workers,
            launchPlan,
            initialManifests,
            controlDirectory,
            firstProcess.CompletedAtUtc,
            completionTimeout,
            cancellationToken).ConfigureAwait(false);

        var secondProcess = await supervisor.RunAsync(
            new HostProcessLaunchRequest(
                ChildHostRunId(runId, 2),
                executable,
                workingDirectory,
                aggregateManifestPath,
                secondPath,
                options.EquipmentCount,
                hostOptions,
                completionTimeout),
            cancellationToken).ConfigureAwait(false);
        var second = ParseAndValidateHostChild(secondProcess, options.EquipmentCount, "second");

        Require(firstProcess.ProcessId != secondProcess.ProcessId,
            "The operating system reused the same live Host process instead of starting a second child.");
        var firstConnections = first.EquipmentMetrics.ToDictionary(
            value => value.EquipmentId,
            value => value.ConnectionId,
            StringComparer.OrdinalIgnoreCase);
        Require(second.EquipmentMetrics.All(value =>
                firstConnections.TryGetValue(value.EquipmentId, out var previous) &&
                !string.IsNullOrWhiteSpace(value.ConnectionId) &&
                !value.ConnectionId.Equals(previous, StringComparison.Ordinal)),
            "One or more second-Host contexts inherited a first-Host ConnectionId.");
        Require(second.EquipmentMetrics.All(value => value.Selected && value.Responses > 0),
            "The second Host did not restore Selected request/response traffic for every equipment.");

        var checks = new List<string>
        {
            $"Host child 1 PID {firstProcess.ProcessId} exited normally with zero pending transactions, tracked sessions, operations, listeners and OS sockets; result {firstPath}.",
            $"All {launchPlan.Count} existing worker processes remained alive, re-created passive sessions on the same manifest endpoints, and reported Listening before Host child 2.",
            $"Host child 2 PID {secondProcess.ProcessId} was a distinct OS process and restored {second.Final.SelectedEquipment}/{options.EquipmentCount} HSMS Selected contexts plus S1F1/S1F2 and S1F13/S1F14 traffic; result {secondPath}.",
            $"All {options.EquipmentCount} ConnectionIds changed between Host child 1 and Host child 2.",
            "No transaction/System Bytes state was inherited: Host child 1 cleaned pending/control transactions to zero, Host child 2 created fresh process-local contexts and completed newly correlated traffic, then also cleaned pending/control transactions to zero.",
            "GEM runtimes were recreated with the fresh Host contexts; Selected state and baseline GEM communication were restored without restarting equipment workers."
        };
        checks.AddRange(first.Checks.Select(value => $"Host 1: {value}"));
        checks.AddRange(second.Checks.Select(value => $"Host 2: {value}"));
        checks.AddRange(second.EquipmentMetrics.Select(value =>
            $"{value.EquipmentId}: ConnectionId {firstConnections[value.EquipmentId]} -> {value.ConnectionId}; " +
            $"Selected={value.Selected}, requests={value.Requests}, responses={value.Responses}."));

        return new FactoryRunResult(
            1,
            "host-restart",
            "multi-process",
            first.StartedAtUtc,
            second.CompletedAtUtc,
            FactoryRunStatus.Passed,
            second.Final,
            second.CleanupBeforeForcedGc,
            second.CleanupAfterForcedGc,
            first.Snapshots.Concat(second.Snapshots).ToArray(),
            checks,
            null,
            "Two supervised Host OS processes against a persistent multi-process self-loopback equipment fleet; not external compatibility evidence")
        {
            EquipmentMetrics = second.EquipmentMetrics
        };
    }

    private static IReadOnlyDictionary<string, string> BuildHostChildOptions(FactoryScenarioRunOptions options) =>
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["--connect-concurrency"] = options.ConnectConcurrency.ToString(CultureInfo.InvariantCulture),
            ["--reconnect-concurrency"] = options.ReconnectConcurrency.ToString(CultureInfo.InvariantCulture),
            ["--message-concurrency"] = options.MessageConcurrency.ToString(CultureInfo.InvariantCulture),
            ["--receive-queue-capacity"] = options.ReceiveQueueCapacity.ToString(CultureInfo.InvariantCulture),
            ["--log-queue-capacity"] = options.LogQueueCapacity.ToString(CultureInfo.InvariantCulture),
            ["--export-queue-capacity"] = options.ExportQueueCapacity.ToString(CultureInfo.InvariantCulture),
            ["--pending-per-equipment"] = options.PerEquipmentPendingLimit.ToString(CultureInfo.InvariantCulture),
            ["--pending-global"] = options.GlobalPendingLimit.ToString(CultureInfo.InvariantCulture)
        };

    private static FactoryRunResult ParseAndValidateHostChild(
        HostProcessResult process,
        int equipmentCount,
        string ordinal)
    {
        Require(!process.TimedOut && !process.KilledAfterTimeout && process.ExitCode == FactoryExitCodes.Success,
            $"The {ordinal} Host child did not exit normally (PID {process.ProcessId}, exit {process.ExitCode}, " +
            $"timeout={process.TimedOut}, killed={process.KilledAfterTimeout}). stderr: {process.StandardErrorTail}");
        var result = TryParseFactoryResult(process.Result)
                     ?? throw new InvalidDataException($"The {ordinal} Host child result was missing or invalid.");
        Require(result.Status == FactoryRunStatus.Passed,
            $"The {ordinal} Host child failed: {result.FirstFailure ?? "acceptance check failed"}.");
        Require(result.Final.Process.ProcessId == process.ProcessId,
            $"The {ordinal} Host result PID did not match the supervised process.");
        Require(result.Final.SelectedEquipment == equipmentCount && result.EquipmentMetrics.Count == equipmentCount,
            $"The {ordinal} Host selected or reported fewer than {equipmentCount} equipment contexts.");
        Require(result.EquipmentMetrics.All(value => value.Selected && !string.IsNullOrWhiteSpace(value.ConnectionId)),
            $"The {ordinal} Host result contained an unselected or identity-less equipment context.");
        Require(result.Final.Requests > 0 && result.Final.Requests == result.Final.Responses &&
                result.Final.CorrelationErrors == 0,
            $"The {ordinal} Host did not complete cleanly correlated baseline traffic.");
        var baseline = result.Snapshots.FirstOrDefault()
                       ?? throw new InvalidDataException($"The {ordinal} Host result omitted its process baseline.");
        ValidateHostCleanup(result.CleanupBeforeForcedGc, baseline, ordinal, "before forced GC");
        ValidateHostCleanup(result.CleanupAfterForcedGc, baseline, ordinal, "after forced GC");
        Require(!FactoryScenarioCoordinator.ExceedsStabilizationGrowth(
                    result.CleanupBeforeForcedGc.Process.ThreadCount,
                    result.CleanupAfterForcedGc.Process.ThreadCount, 2) &&
                !FactoryScenarioCoordinator.ExceedsStabilizationGrowth(
                    result.CleanupBeforeForcedGc.Process.HandleCount,
                    result.CleanupAfterForcedGc.Process.HandleCount, 4),
            $"The {ordinal} Host thread/handle evidence grew during cleanup stabilization.");
        return result;
    }

    private static void ValidateHostCleanup(
        FactoryMetricSnapshot cleanup,
        FactoryMetricSnapshot baseline,
        string ordinal,
        string phase)
    {
        Require(!FactoryScenarioCoordinator.HasFactoryResourceLeak(cleanup, baseline),
            $"The {ordinal} Host cleanup {phase} did not return to its measured baseline: pending={cleanup.PendingTransactions}, " +
            $"control={cleanup.PendingControlTransactions}, operations={cleanup.TrackedOperations}, " +
            $"reconnect={cleanup.ReconnectOperations}, sessions={cleanup.TrackedSessions}, " +
            $"listeners={cleanup.TrackedListeners}, sockets={cleanup.Process.Sockets.OpenSocketCount?.ToString(CultureInfo.InvariantCulture) ?? "unavailable"}.");
    }

    private static async Task WaitForWorkersListeningAsync(
        WorkerProcessCoordinator workers,
        IReadOnlyList<WorkerPlan> launchPlan,
        IReadOnlyDictionary<string, FactoryEndpointManifest> expectedManifests,
        string controlDirectory,
        DateTimeOffset after,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(timeout);
        try
        {
            while (true)
            {
                deadline.Token.ThrowIfCancellationRequested();
                var listening = true;
                foreach (var plan in launchPlan)
                {
                    var heartbeat = await workers.TryReadHeartbeatAsync(plan.Request.WorkerId, deadline.Token)
                        .ConfigureAwait(false);
                    if (heartbeat is null || heartbeat.Timestamp < after || heartbeat.Connected != 0 ||
                        !heartbeat.State.Equals("Listening", StringComparison.Ordinal))
                    {
                        listening = false;
                        break;
                    }

                    var path = RunArtifactPath(
                        controlDirectory,
                        plan.Request.RunId,
                        plan.Request.WorkerId,
                        "endpoints.json");
                    var current = await WorkerFileProtocol.ReadJsonAsync<FactoryEndpointManifest>(path, deadline.Token)
                        .ConfigureAwait(false);
                    ValidateEndpointManifest(current);
                    var expected = expectedManifests[plan.Request.WorkerId];
                    if (current.ProcessId != expected.ProcessId || current.Equipment.Count != expected.Equipment.Count ||
                        current.Equipment.Any(value => !expected.Equipment.Any(candidate =>
                            candidate.EquipmentId.Equals(value.EquipmentId, StringComparison.OrdinalIgnoreCase) &&
                            candidate.Host.Equals(value.Host, StringComparison.OrdinalIgnoreCase) &&
                            candidate.Port == value.Port)))
                    {
                        listening = false;
                        break;
                    }
                }
                if (listening) return;
                await Task.Delay(50, deadline.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Equipment workers did not return to Listening within {timeout} after Host exit.");
        }
    }

    private static FactoryRunResult? TryParseFactoryResult(JsonElement? payload)
    {
        if (payload is not { } value) return null;
        try
        {
            return value.Deserialize<FactoryRunResult>(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            });
        }
        catch (JsonException) { return null; }
    }

    private static string ChildHostRunId(string runId, int ordinal)
    {
        var suffix = $"-host-{ordinal}";
        var prefixLength = Math.Min(runId.Length, 64 - suffix.Length);
        return runId[..prefixLength] + suffix;
    }

    private static async Task RunHeartbeatLoopAsync(
        WorkerControlPaths paths,
        string runId,
        string workerId,
        FactoryEquipmentFleet fleet,
        TimeSpan interval,
        CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(interval);
        while (true)
        {
            string? maintenanceFailure = null;
            try { _ = await fleet.RestartDisconnectedAsync(cancellationToken).ConfigureAwait(false); }
            catch (Exception exception) when (exception is IOException or System.Net.Sockets.SocketException)
            {
                maintenanceFailure = $"{exception.GetType().Name}: {exception.Message}";
            }
            var connected = fleet.OpenSocketCount;
            var listeners = fleet.ListenerCount;
            var state = connected > 0
                ? "Connected"
                : listeners == fleet.Peers.Count
                    ? "Listening"
                    : maintenanceFailure is null ? "Recovering" : $"Recovering: {maintenanceFailure}";
            await WorkerFileProtocol.PublishHeartbeatAsync(
                paths,
                new WorkerHeartbeatRecord(
                    runId,
                    workerId,
                    Environment.ProcessId,
                    DateTimeOffset.UtcNow,
                    state,
                    connected,
                    0,
                    fleet.EnqueuedResponseCount,
                    fleet.SentResponseCount,
                    0,
                    checked((int)Math.Min(int.MaxValue, fleet.QueueDepth))),
                cancellationToken).ConfigureAwait(false);
            if (!await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false)) return;
        }
    }

    private static async Task<WorkerCleanupOutcome> CleanupWorkerFleetAsync(FactoryEquipmentFleet fleet)
    {
        var before = CaptureWorkerResources(fleet);
        string? failure = null;
        try { await fleet.DisposeAsync().ConfigureAwait(false); }
        catch (Exception exception) { failure = $"{exception.GetType().Name}: {exception.Message}"; }
        var stabilizationBaseline = CaptureWorkerResources(fleet);
        var after = stabilizationBaseline;
        if (failure is null)
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
            after = await CaptureStableWorkerResourcesAsync(fleet, after).ConfigureAwait(false);
        }
        return new WorkerCleanupOutcome(before, stabilizationBaseline, after, failure);
    }

    private static async Task<FactoryWorkerResourceSnapshot> CaptureStableWorkerResourcesAsync(
        FactoryEquipmentFleet fleet,
        FactoryWorkerResourceSnapshot initial)
    {
        var previous = initial;
        var stableSamples = 0;
        var deadline = Stopwatch.StartNew();
        while (deadline.Elapsed < TimeSpan.FromSeconds(2))
        {
            await Task.Delay(100).ConfigureAwait(false);
            var current = CaptureWorkerResources(fleet);
            stableSamples = HasSameCleanupOwnershipAndOsCounts(previous, current)
                ? stableSamples + 1
                : 0;
            previous = current;
            if (stableSamples >= 3 && current.OperatingSystemOpenSockets is 0 or null) break;
        }
        return previous;
    }

    private static bool HasSameCleanupOwnershipAndOsCounts(
        FactoryWorkerResourceSnapshot left,
        FactoryWorkerResourceSnapshot right) =>
        left.TrackedSessions == right.TrackedSessions &&
        left.TrackedListeners == right.TrackedListeners &&
        left.TrackedOperations == right.TrackedOperations &&
        left.QueueDepth == right.QueueDepth &&
        left.OperatingSystemOpenSockets == right.OperatingSystemOpenSockets &&
        left.OperatingSystemListeners == right.OperatingSystemListeners &&
        left.OperatingSystemEstablished == right.OperatingSystemEstablished &&
        left.ThreadCount == right.ThreadCount &&
        left.HandleCount == right.HandleCount;

    private static FactoryWorkerResourceSnapshot CaptureWorkerResources(FactoryEquipmentFleet fleet)
    {
        using var process = Process.GetCurrentProcess();
        process.Refresh();
        var sockets = WindowsProcessSocketMetricsProvider.Instance.Capture(process);
        int? threadCount;
        int? handleCount;
        try { threadCount = process.Threads.Count; }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            threadCount = null;
        }
        try { handleCount = process.HandleCount; }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            handleCount = null;
        }
        return new FactoryWorkerResourceSnapshot(
            DateTimeOffset.UtcNow,
            fleet.LiveSessionCount,
            fleet.ListenerCount,
            FactoryEquipmentFleet.LiveResponderWorkerCount,
            fleet.QueueDepth,
            sockets.OpenSocketCount,
            sockets.ListenerCount,
            sockets.EstablishedCount,
            process.WorkingSet64,
            GC.GetTotalMemory(false),
            GC.GetTotalAllocatedBytes(false),
            GC.CollectionCount(0),
            GC.CollectionCount(1),
            GC.CollectionCount(2),
            threadCount,
            handleCount);
    }

    private static async Task<string> WaitForEquipmentTerminationAsync(
        WorkerControlPaths paths,
        string runId,
        string workerId,
        TimeSpan? duration,
        CancellationToken cancellationToken)
    {
        using var waitCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var stop = WorkerFileProtocol.WaitForStopRequestAsync(
            paths,
            runId,
            workerId,
            TimeSpan.FromMilliseconds(100),
            waitCancellation.Token);
        var durationTask = duration is { } bounded
            ? Task.Delay(bounded, waitCancellation.Token)
            : Task.Delay(Timeout.InfiniteTimeSpan, waitCancellation.Token);
        var parentExit = WaitForParentExitAsync(waitCancellation.Token);
        var winner = await Task.WhenAny(stop, durationTask, parentExit).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        var reason = ReferenceEquals(winner, stop)
            ? "stop-request"
            : ReferenceEquals(winner, durationTask)
                ? "duration-complete"
                : "parent-exit";
        await winner.ConfigureAwait(false);
        waitCancellation.Cancel();
        return reason;
    }

    private static async Task WaitForParentExitAsync(CancellationToken cancellationToken)
    {
        var value = Environment.GetEnvironmentVariable("DREAMINE_FACTORY_PARENT_PID");
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parentId) || parentId <= 0)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
            return;
        }

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                using var parent = Process.GetProcessById(parentId);
                if (parent.HasExited) return;
            }
            catch (ArgumentException) { return; }
            catch (InvalidOperationException) { return; }
            await Task.Delay(250, cancellationToken).ConfigureAwait(false);
        }
    }

    private static IReadOnlyList<WorkerPlan> BuildWorkerLaunchPlan(
        FactoryCommandLine command,
        FactoryScenarioRunOptions options,
        string runId,
        string controlDirectory,
        string programPath)
    {
        var requestedWorkers = command.GetInt32("worker-count", 0);
        var requestedPerWorker = command.GetInt32("equipment-per-worker", 0);
        var workerCount = requestedWorkers > 0
            ? Math.Min(requestedWorkers, options.EquipmentCount)
            : (int)Math.Ceiling(options.EquipmentCount / (double)(requestedPerWorker > 0 ? requestedPerWorker : 100));
        var perWorker = requestedPerWorker > 0
            ? requestedPerWorker
            : (int)Math.Ceiling(options.EquipmentCount / (double)workerCount);
        Require(perWorker <= 100, "Worker capacity exceeds the 100-equipment process bound.");
        Require(workerCount * perWorker >= options.EquipmentCount,
            "Worker capacity is smaller than the requested equipment count.");

        var availablePorts = options.PortRangeEnd - options.PortRangeStart + 1;
        Require(availablePorts >= options.EquipmentCount,
            "The validated port range is smaller than the requested equipment count.");
        var executable = Path.GetFullPath(programPath);
        var workingDirectory = Path.GetDirectoryName(executable)
                               ?? throw new InvalidOperationException("Worker executable has no parent directory.");
        var plans = new List<WorkerPlan>();
        var remaining = options.EquipmentCount;
        var nextIndex = options.StartIndex;
        for (var workerIndex = 0; workerIndex < workerCount && remaining > 0; workerIndex++)
        {
            var count = Math.Min(perWorker, remaining);
            var rangeStart = options.PortRangeStart + (int)((long)availablePorts * workerIndex / workerCount);
            var rangeEnd = options.PortRangeStart +
                           (int)((long)availablePorts * (workerIndex + 1) / workerCount) - 1;
            Require(rangeEnd - rangeStart + 1 >= count,
                $"Worker {workerIndex + 1} port partition is smaller than its endpoint count.");
            var workerId = $"worker-{workerIndex + 1:000}";
            var resultPath = RunArtifactPath(controlDirectory, runId, workerId, "result.json");
            var additional = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["--port-range-start"] = rangeStart.ToString(CultureInfo.InvariantCulture),
                ["--port-range-end"] = rangeEnd.ToString(CultureInfo.InvariantCulture),
                ["--receive-queue-capacity"] = options.ReceiveQueueCapacity.ToString(CultureInfo.InvariantCulture),
                ["--log-queue-capacity"] = options.LogQueueCapacity.ToString(CultureInfo.InvariantCulture),
                ["--heartbeat-interval"] = command.HasOption("heartbeat-interval")
                    ? command.GetString("heartbeat-interval")
                    : DefaultHeartbeatInterval.ToString("c", CultureInfo.InvariantCulture)
            };
            plans.Add(new WorkerPlan(
                new WorkerLaunchRequest(
                    runId,
                    workerId,
                    executable,
                    workingDirectory,
                    nextIndex,
                    count,
                    controlDirectory,
                    resultPath,
                    additional,
                    command.HasOption("ready-timeout")
                        ? command.GetTimeSpan("ready-timeout", TimeSpan.FromSeconds(30))
                        : null,
                    command.HasOption("shutdown-timeout")
                        ? command.GetTimeSpan("shutdown-timeout", TimeSpan.FromSeconds(15))
                        : null),
                rangeStart,
                rangeEnd));
            nextIndex += count;
            remaining -= count;
        }
        return plans;
    }

    private static async Task<IReadOnlyDictionary<string, FactoryEndpointManifest>> ReadWorkerManifestsAsync(
        IReadOnlyList<WorkerPlan> plans,
        IReadOnlyDictionary<string, WorkerReadyRecord> ready,
        string controlDirectory,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, FactoryEndpointManifest>(StringComparer.OrdinalIgnoreCase);
        foreach (var plan in plans)
        {
            if (!ready.TryGetValue(plan.Request.WorkerId, out var record) ||
                string.IsNullOrWhiteSpace(record.EndpointManifestPath))
                throw new InvalidDataException($"Worker '{plan.Request.WorkerId}' did not publish an endpoint manifest path.");
            var manifestPath = EnsureChildArtifact(controlDirectory, record.EndpointManifestPath);
            var manifest = await WorkerFileProtocol.ReadJsonAsync<FactoryEndpointManifest>(
                manifestPath,
                cancellationToken).ConfigureAwait(false);
            if (!manifest.RunId.Equals(plan.Request.RunId, StringComparison.Ordinal) ||
                !manifest.WorkerId.Equals(plan.Request.WorkerId, StringComparison.OrdinalIgnoreCase) ||
                manifest.ProcessId != record.ProcessId || manifest.Equipment.Count != plan.Request.Count)
                throw new InvalidDataException($"Worker '{plan.Request.WorkerId}' published a mismatched endpoint manifest.");
            ValidateEndpointManifest(manifest);
            result.Add(plan.Request.WorkerId, manifest);
        }
        return result;
    }

    private static FactoryEndpointManifest BuildAggregateManifest(
        string runId,
        IReadOnlyDictionary<string, FactoryEndpointManifest> manifests) => new(
        1,
        runId,
        "all",
        Environment.ProcessId,
        DateTimeOffset.UtcNow,
        manifests.OrderBy(value => value.Key, StringComparer.OrdinalIgnoreCase)
            .SelectMany(value => value.Value.Equipment)
            .OrderBy(value => value.EquipmentId, StringComparer.OrdinalIgnoreCase)
            .ToArray(),
        "Multi-process self-loopback endpoint manifest; not external compatibility evidence");

    private static void ValidateEndpointManifest(FactoryEndpointManifest manifest)
    {
        if (manifest.SchemaVersion != 1) throw new InvalidDataException("Unsupported endpoint manifest schema.");
        if (manifest.Equipment.Count == 0) throw new InvalidDataException("Endpoint manifest is empty.");
        var duplicateId = manifest.Equipment.GroupBy(value => value.EquipmentId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(value => value.Count() > 1);
        if (duplicateId is not null)
            throw new InvalidDataException($"Duplicate EquipmentId '{duplicateId.Key}' in endpoint manifest.");
        var duplicateEndpoint = manifest.Equipment.GroupBy(
                value => $"{value.Host}:{value.Port}",
                StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(value => value.Count() > 1);
        if (duplicateEndpoint is not null)
            throw new InvalidDataException($"Duplicate endpoint '{duplicateEndpoint.Key}' in endpoint manifest.");
        foreach (var endpoint in manifest.Equipment) ToDefinition(endpoint).Validate();
    }

    private static FactoryScenarioRunOptions BuildScenarioOptions(
        FactoryCommandLine command,
        string executionMode)
    {
        var scenario = command.Scenario ?? "factory-smoke";
        var equipmentCount = command.GetInt32("equipment-count", DefaultEquipmentCount(scenario));
        var disconnectCount = command.HasOption("disconnect-count")
            ? command.GetInt32("disconnect-count")
            : 0;
        return new FactoryScenarioRunOptions
        {
            Scenario = scenario,
            ExecutionMode = executionMode,
            EquipmentCount = equipmentCount,
            StartIndex = command.GetInt32("start-index", 1),
            DisconnectCount = disconnectCount,
            Duration = command.HasOption("duration")
                ? command.GetTimeSpan("duration", TimeSpan.Zero)
                : TimeSpan.Zero,
            MessagesPerSecond = command.GetDouble("messages-per-second", 0),
            MessageBytes = command.GetInt32("message-bytes", 0),
            PortRangeStart = command.GetInt32("port-range-start", 20_000),
            PortRangeEnd = command.GetInt32("port-range-end", 48_000),
            ConnectConcurrency = command.GetInt32("connect-concurrency", 64),
            ReconnectConcurrency = command.GetInt32("reconnect-concurrency", 16),
            MessageConcurrency = command.GetInt32("message-concurrency", 128),
            ReceiveQueueCapacity = command.GetInt32("receive-queue-capacity", 4_096),
            LogQueueCapacity = command.GetInt32("log-queue-capacity", 2_048),
            ExportQueueCapacity = command.GetInt32("export-queue-capacity", 64),
            PerEquipmentPendingLimit = command.GetInt32("pending-per-equipment", 16),
            GlobalPendingLimit = command.GetInt32("pending-global", 4_096),
            SnapshotInterval = command.HasOption("snapshot-interval")
                ? command.GetTimeSpan("snapshot-interval", TimeSpan.FromMinutes(1))
                : null,
            SnapshotDirectory = command.HasOption("snapshot-directory")
                ? Path.GetFullPath(command.GetString("snapshot-directory"))
                : null
        };
    }

    private static FactoryExecutionResult MapResult(
        FactoryRunResult result,
        string outputPath,
        CancellationToken cancellationToken)
    {
        if (result.Status == FactoryRunStatus.Cancelled && cancellationToken.IsCancellationRequested)
            throw new OperationCanceledException(cancellationToken);
        return result.Status switch
        {
            FactoryRunStatus.Passed => FactoryExecutionResult.Success(
                $"{result.ExecutionMode} '{result.Scenario}' passed.", outputPath),
            FactoryRunStatus.Cancelled => FactoryExecutionResult.Failed(
                $"{result.ExecutionMode} '{result.Scenario}' was canceled.", outputPath),
            _ => FactoryExecutionResult.Rejected(
                $"{result.ExecutionMode} '{result.Scenario}' failed: {result.FirstFailure ?? "acceptance check failed"}",
                outputPath)
        };
    }

    private static FactoryWorkerRuntimeResult? TryParseWorkerResult(WorkerProcessResult process)
    {
        if (process.Result is not { } payload) return null;
        try
        {
            return payload.Deserialize<FactoryWorkerRuntimeResult>(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException) { return null; }
    }

    private static bool IsCleanWorkerResult(
        WorkerProcessResult process,
        FactoryWorkerRuntimeResult? runtime) =>
        process.ExitCode == FactoryExitCodes.Success && !process.KilledAfterGracefulTimeout &&
        runtime is not null && runtime.Status == FactoryRunStatus.Passed &&
        runtime.RunId.Equals(process.RunId, StringComparison.Ordinal) &&
        runtime.WorkerId.Equals(process.WorkerId, StringComparison.OrdinalIgnoreCase) &&
        runtime.ProcessId == process.ProcessId &&
        runtime.AfterCleanup.TrackedSessions == 0 &&
        runtime.AfterCleanup.TrackedListeners == 0 &&
        runtime.AfterCleanup.TrackedOperations == 0 &&
        runtime.AfterCleanup.QueueDepth == 0 &&
        runtime.AfterCleanup.OperatingSystemOpenSockets == 0 &&
        runtime.CleanupStabilizationBaseline is { } stabilizationBaseline &&
        !FactoryScenarioCoordinator.ExceedsStabilizationGrowth(
            stabilizationBaseline.ThreadCount, runtime.AfterCleanup.ThreadCount,
            WorkerCleanupThreadGrowthAllowance) &&
        !FactoryScenarioCoordinator.ExceedsStabilizationGrowth(
            stabilizationBaseline.HandleCount, runtime.AfterCleanup.HandleCount,
            WorkerCleanupHandleGrowthAllowance);

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

    private static FactoryEndpointRecord FromDefinition(EquipmentConnectionDefinition value) => new(
        value.EquipmentId,
        value.Host,
        value.Port,
        value.Mode.ToString(),
        value.SessionId,
        value.AutoReconnect,
        value.T3Seconds,
        value.T5Seconds,
        value.T6Seconds,
        value.T7Seconds,
        value.T8Seconds,
        value.MaximumFrameLength);

    private static EquipmentConnectionDefinition ToDefinition(FactoryEndpointRecord value)
    {
        if (!Enum.TryParse<SecsConnectionMode>(value.Mode, ignoreCase: true, out var mode))
            throw new InvalidDataException($"Endpoint '{value.EquipmentId}' has unknown mode '{value.Mode}'.");
        return new EquipmentConnectionDefinition
        {
            EquipmentId = value.EquipmentId,
            Host = value.Host,
            Port = value.Port,
            Mode = mode,
            SessionId = value.SessionId,
            AutoReconnect = value.AutoReconnect,
            T3Seconds = value.T3Seconds,
            T5Seconds = value.T5Seconds,
            T6Seconds = value.T6Seconds,
            T7Seconds = value.T7Seconds,
            T8Seconds = value.T8Seconds,
            MaximumFrameLength = value.MaximumFrameLength
        };
    }

    private static SecsItem BuildHostBody(string profile, long sequence, int requestedBytes) => profile switch
    {
        "trace" => new SecsListItem(
            new SecsInt32Item(Enumerable.Range(0, 64).Select(value => checked((int)(sequence + value))).ToArray()),
            new SecsListItem(new SecsFloat64Item(sequence, sequence + 0.25), new SecsAsciiItem("TRACE"))),
        "busy" => new SecsListItem(new SecsInt64Item(sequence), new SecsAsciiItem("EVENT")),
        "normal" => new SecsListItem(new SecsAsciiItem("STATUS"), new SecsInt64Item(sequence)),
        _ when requestedBytes > 0 => new SecsBinaryItem(new byte[Math.Min(requestedBytes, 64 * 1024)]),
        _ => new SecsAsciiItem("SOAK")
    };

    private static int DefaultEquipmentCount(string scenario) => NormalizeScenario(scenario) switch
    {
        "idle-factory" => 1_000,
        "factory-normal" => 500,
        "factory-busy" => 100,
        "trace-burst" => 20,
        "reconnect-storm" => 500,
        "large-message" => 20,
        "fault-isolation" => 6,
        _ => 100
    };

    private static string ResolveRunId(FactoryCommandLine command) => command.GetString(
        "run-id",
        $"run-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}-{Environment.ProcessId}");

    private static string ResolveControlDirectory(FactoryCommandLine command, string runId) =>
        command.HasOption("control-directory")
            ? Path.GetFullPath(command.GetString("control-directory"))
            : Path.Combine(Path.GetTempPath(), "Dreamine.FactoryScale", runId);

    private static string ResolveOutputPath(
        FactoryCommandLine command,
        string runId,
        string fileName) => command.HasOption("output")
        ? Path.GetFullPath(command.GetString("output"))
        : Path.Combine(Path.GetTempPath(), "Dreamine.FactoryScale", runId, fileName);

    private static string RunArtifactPath(
        string directory,
        string runId,
        string workerId,
        string suffix)
    {
        WorkerFileProtocol.ValidateIdentifier(runId, nameof(runId));
        WorkerFileProtocol.ValidateIdentifier(workerId, nameof(workerId));
        var fullDirectory = Path.GetFullPath(directory);
        return EnsureChildArtifact(fullDirectory, Path.Combine(fullDirectory, $"{runId}.{workerId}.{suffix}"));
    }

    private static string EnsureChildArtifact(string directory, string path)
    {
        var fullDirectory = Path.GetFullPath(directory);
        var fullPath = Path.GetFullPath(path);
        var prefix = fullDirectory.EndsWith(Path.DirectorySeparatorChar)
            ? fullDirectory
            : fullDirectory + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Worker artifact escaped the configured control directory.");
        return fullPath;
    }

    private static void EnsureDistinctArtifacts(string left, string right)
    {
        if (Path.GetFullPath(left).Equals(Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Result and endpoint manifest paths must be different files.");
    }

    private static TimeSpan EffectiveDuration(FactoryScenarioRunOptions options, TimeSpan fallback) =>
        options.Duration > TimeSpan.Zero ? options.Duration : fallback;

    private static string NormalizeScenario(string scenario) => scenario.ToLowerInvariant() switch
    {
        "normal-factory" => "factory-normal",
        "busy-factory" => "factory-busy",
        _ => scenario.ToLowerInvariant()
    };

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed record WorkerPlan(WorkerLaunchRequest Request, int PortRangeStart, int PortRangeEnd);
    private sealed record WorkerCleanupOutcome(
        FactoryWorkerResourceSnapshot Before,
        FactoryWorkerResourceSnapshot StabilizationBaseline,
        FactoryWorkerResourceSnapshot After,
        string? Failure);
}

internal sealed record FactoryEndpointManifest(
    int SchemaVersion,
    string RunId,
    string WorkerId,
    int ProcessId,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<FactoryEndpointRecord> Equipment,
    string EvidenceScope);

internal sealed record FactoryEndpointRecord(
    string EquipmentId,
    string Host,
    int Port,
    string Mode,
    ushort SessionId,
    bool AutoReconnect,
    int T3Seconds,
    int T5Seconds,
    int T6Seconds,
    int T7Seconds,
    int T8Seconds,
    int MaximumFrameLength);

internal sealed record FactoryWorkerRuntimeResult(
    int SchemaVersion,
    string RunId,
    string WorkerId,
    int ProcessId,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    FactoryRunStatus Status,
    int EquipmentCount,
    long Requests,
    long Responses,
    long Rejected,
    long PeakQueueDepth,
    FactoryWorkerResourceSnapshot BeforeCleanup,
    FactoryWorkerResourceSnapshot AfterCleanup,
    string? EndpointManifestPath,
    string StopReason,
    string? Failure)
{
    public FactoryWorkerResourceSnapshot? CleanupStabilizationBaseline { get; init; }
}

internal sealed record FactoryWorkerResourceSnapshot(
    DateTimeOffset TimestampUtc,
    int TrackedSessions,
    int TrackedListeners,
    int? TrackedOperations,
    long QueueDepth,
    int? OperatingSystemOpenSockets,
    int? OperatingSystemListeners,
    int? OperatingSystemEstablished,
    long WorkingSetBytes,
    long ManagedHeapBytes,
    long TotalAllocatedBytes,
    int Gen0Collections,
    int Gen1Collections,
    int Gen2Collections,
    int? ThreadCount,
    int? HandleCount);
