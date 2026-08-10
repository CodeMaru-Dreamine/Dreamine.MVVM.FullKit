using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Dreamine.SecsGem.Interop.Wpf.Models;

/// <summary>
/// Read-only projection of a headless FactoryScale result. This UI model deliberately
/// has no reference to the FactoryScale executable or its load-generation types.
/// </summary>
public sealed record FactoryResultSummary
{
    public static FactoryResultSummary NotRun { get; } = new();

    public bool IsImported { get; init; }
    public string SchemaVersion { get; init; } = "--";
    public string Scenario { get; init; } = "Not Run";
    public string ExecutionMode { get; init; } = "--";
    public string Status { get; init; } = "Not Run";
    public string StartedAt { get; init; } = "--";
    public string CompletedAt { get; init; } = "--";
    public string Elapsed { get; init; } = "--";
    public string RequestedEquipment { get; init; } = "--";
    public string ConnectedEquipment { get; init; } = "--";
    public string SelectedEquipment { get; init; } = "--";
    public string FailedEquipment { get; init; } = "--";
    public string ReconnectingEquipment { get; init; } = "--";
    public string MessagesPerSecond { get; init; } = "--";
    public string RequestsPerSecond { get; init; } = "--";
    public string ResponsesPerSecond { get; init; } = "--";
    public string MessageTotals { get; init; } = "--";
    public string TimeoutFailureSummary { get; init; } = "--";
    public string LatencyP50 { get; init; } = "--";
    public string LatencyP95 { get; init; } = "--";
    public string LatencyP99 { get; init; } = "--";
    public string LatencyDetail { get; init; } = "--";
    public string WorkingSet { get; init; } = "--";
    public string PrivateMemory { get; init; } = "--";
    public string ManagedHeap { get; init; } = "--";
    public string HandleCount { get; init; } = "--";
    public string ThreadCount { get; init; } = "--";
    public string SocketSummary { get; init; } = "--";
    public string RuntimeResourceSummary { get; init; } = "--";
    public string QueueSummary { get; init; } = "--";
    public string CleanupState { get; init; } = "Not Measured";
    public string CleanupBefore { get; init; } = "--";
    public string CleanupAfter { get; init; } = "--";
    public string CleanupSocketSummary { get; init; } = "--";
    public string Checks { get; init; } = "No imported checks.";
    public string FirstFailure { get; init; } = "None reported.";
    public string EvidenceScope { get; init; } = "Headless self-loopback evidence only.";
}

internal static class FactoryResultJsonParser
{
    private const long MaximumFileBytes = 64L * 1024 * 1024;
    private const string DefaultEvidenceScope =
        "Self-loopback evidence; not external-equipment compatibility or compliance evidence";

    internal static async Task<FactoryResultSummary> ParseAsync(string path, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var fullPath = Path.GetFullPath(path);
        var file = new FileInfo(fullPath);
        if (!file.Exists) throw new FileNotFoundException("Factory result JSON was not found.", fullPath);
        if (file.Length > MaximumFileBytes)
            throw new InvalidDataException("Factory result JSON exceeds the 64 MiB import limit.");

        await using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read,
            64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var document = await JsonDocument.ParseAsync(stream,
            new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Disallow, MaxDepth = 64 },
            cancellationToken).ConfigureAwait(false);

        var root = document.RootElement;
        RequireObject(root, "Factory result");
        var schemaVersion = RequiredInt32(root, "schemaVersion");
        if (schemaVersion != 1)
            throw new InvalidDataException($"Unsupported Factory result schema version: {schemaVersion}.");

        var scenario = RequiredString(root, "scenario");
        var executionMode = RequiredString(root, "executionMode");
        var status = ReadStatus(root);
        var startedAt = RequiredDateTimeOffset(root, "startedAtUtc");
        var completedAt = RequiredDateTimeOffset(root, "completedAtUtc");
        if (completedAt < startedAt)
            throw new InvalidDataException("Factory result completedAtUtc precedes startedAtUtc.");

        var final = RequiredObject(root, "final");
        var rateSource = SelectRateSource(root, final, scenario);
        var latency = OptionalObject(final, "responseLatency");
        var process = OptionalObject(final, "process");
        var sockets = process is { } processValue ? OptionalObject(processValue, "sockets") : null;
        var cleanupBefore = RequiredObject(root, "cleanupBeforeForcedGc");
        var cleanupAfter = RequiredObject(root, "cleanupAfterForcedGc");
        var cleanupBeforeProcess = OptionalObject(cleanupBefore, "process");
        var cleanupAfterProcess = OptionalObject(cleanupAfter, "process");
        var cleanupBeforeSockets = cleanupBeforeProcess is { } beforeProcess
            ? OptionalObject(beforeProcess, "sockets")
            : null;
        var cleanupAfterSockets = cleanupAfterProcess is { } afterProcess
            ? OptionalObject(afterProcess, "sockets")
            : null;

        return new FactoryResultSummary
        {
            IsImported = true,
            SchemaVersion = schemaVersion.ToString(CultureInfo.InvariantCulture),
            Scenario = scenario,
            ExecutionMode = executionMode,
            Status = status,
            StartedAt = FormatUtc(startedAt),
            CompletedAt = FormatUtc(completedAt),
            Elapsed = FormatElapsed(completedAt - startedAt),
            RequestedEquipment = FormatCount(ReadInt64(final, "requestedEquipment")),
            ConnectedEquipment = FormatCount(ReadInt64(final, "connectedEquipment")),
            SelectedEquipment = FormatCount(ReadInt64(final, "selectedEquipment")),
            FailedEquipment = FormatCount(ReadInt64(final, "failedEquipment")),
            ReconnectingEquipment = FormatCount(ReadInt64(final, "reconnectingEquipment")),
            MessagesPerSecond = FormatRate(ReadDouble(rateSource, "messagesPerSecond"), "msg/s"),
            RequestsPerSecond = FormatRate(ReadDouble(rateSource, "requestsPerSecond"), "req/s"),
            ResponsesPerSecond = FormatRate(ReadDouble(rateSource, "responsesPerSecond"), "rsp/s"),
            MessageTotals = FormatMessageTotals(final),
            TimeoutFailureSummary = FormatOutcome(final),
            LatencyP50 = FormatMilliseconds(ReadDouble(latency, "p50Milliseconds")),
            LatencyP95 = FormatMilliseconds(ReadDouble(latency, "p95Milliseconds")),
            LatencyP99 = FormatMilliseconds(ReadDouble(latency, "p99Milliseconds")),
            LatencyDetail = FormatLatencyDetail(latency),
            WorkingSet = FormatBytes(ReadInt64(process, "workingSetBytes")),
            PrivateMemory = FormatBytes(ReadInt64(process, "privateMemoryBytes")),
            ManagedHeap = FormatBytes(ReadInt64(process, "managedHeapBytes")),
            HandleCount = FormatCount(ReadInt64(process, "handleCount")),
            ThreadCount = FormatCount(ReadInt64(process, "threadCount")),
            SocketSummary = FormatSockets(sockets),
            RuntimeResourceSummary = FormatRuntimeResources(final),
            QueueSummary = FormatQueues(final),
            CleanupState = DetermineCleanupState(cleanupBefore),
            CleanupBefore = FormatCleanup(cleanupBefore),
            CleanupAfter = FormatCleanup(cleanupAfter),
            CleanupSocketSummary = $"Before: {FormatSockets(cleanupBeforeSockets)}{Environment.NewLine}" +
                                   $"After forced GC: {FormatSockets(cleanupAfterSockets)}",
            Checks = FormatChecks(root),
            FirstFailure = OptionalString(root, "firstFailure") ?? "None reported.",
            EvidenceScope = OptionalString(root, "evidenceScope") ?? DefaultEvidenceScope
        };
    }

    private static JsonElement SelectRateSource(JsonElement root, JsonElement final, string scenario)
    {
        if (!IsSustainedRateScenario(scenario) || HasApplicationRate(final)) return final;
        if (!root.TryGetProperty("snapshots", out var snapshots) || snapshots.ValueKind != JsonValueKind.Array)
            return final;

        JsonElement? latestActive = null;
        foreach (var snapshot in snapshots.EnumerateArray())
        {
            if (snapshot.ValueKind == JsonValueKind.Object && HasApplicationRate(snapshot))
                latestActive = snapshot;
        }

        return latestActive ?? final;
    }

    private static bool IsSustainedRateScenario(string scenario) => scenario.ToLowerInvariant() is
        "factory-normal" or "normal-factory" or "factory-busy" or "busy-factory" or "trace-burst" or "soak";

    private static bool HasApplicationRate(JsonElement snapshot) =>
        TryReadPositiveRate(snapshot, "requestsPerSecond") ||
        TryReadPositiveRate(snapshot, "responsesPerSecond");

    private static bool TryReadPositiveRate(JsonElement snapshot, string propertyName) =>
        snapshot.TryGetProperty(propertyName, out var value) &&
        value.ValueKind == JsonValueKind.Number &&
        value.TryGetDouble(out var rate) &&
        double.IsFinite(rate) && rate > 0;

    private static string ReadStatus(JsonElement root)
    {
        if (!root.TryGetProperty("status", out var element))
            throw new InvalidDataException("Factory result is missing status.");
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var numericStatus))
            return numericStatus switch
            {
                0 => "Not Run",
                1 => "Passed",
                2 => "Failed",
                3 => "Cancelled",
                4 => "Experimental",
                _ => "Unknown"
            };
        if (element.ValueKind == JsonValueKind.String)
        {
            var statusText = element.GetString()?.Trim();
            return statusText?.ToLowerInvariant() switch
            {
                "notrun" or "not run" => "Not Run",
                "passed" => "Passed",
                "failed" => "Failed",
                "cancelled" or "canceled" => "Cancelled",
                "experimental" => "Experimental",
                _ => "Unknown"
            };
        }
        throw new InvalidDataException("Factory result status must be a number or string.");
    }

    private static string FormatMessageTotals(JsonElement final) =>
        $"Requests {FormatCount(ReadInt64(final, "requests"))} · " +
        $"Responses {FormatCount(ReadInt64(final, "responses"))} · " +
        $"Sent {FormatBytes(ReadInt64(final, "bytesSent"))} · " +
        $"Received {FormatBytes(ReadInt64(final, "bytesReceived"))}";

    private static string FormatOutcome(JsonElement final) =>
        $"Timeouts {FormatCount(ReadInt64(final, "timeouts"))} · " +
        $"Failures {FormatCount(ReadInt64(final, "failures"))} · " +
        $"Correlation errors {FormatCount(ReadInt64(final, "correlationErrors"))} · " +
        $"Dropped diagnostics {FormatCount(ReadInt64(final, "droppedDiagnosticLogs"))}";

    private static string FormatLatencyDetail(JsonElement? latency) =>
        latency is null
            ? "Not measured"
            : $"Average {FormatMilliseconds(ReadDouble(latency, "averageMilliseconds"))} · " +
              $"Max {FormatMilliseconds(ReadDouble(latency, "maximumMilliseconds"))} · " +
              $"Samples {FormatCount(ReadInt64(latency, "sampleCount"))} · " +
              $"Resolution {OptionalString(latency.Value, "resolution") ?? "--"}";

    private static string FormatRuntimeResources(JsonElement final) =>
        $"Pending transactions {FormatCount(ReadInt64(final, "pendingTransactions"))} " +
        $"(peak {FormatCount(ReadInt64(final, "peakPendingTransactions"))}) · " +
        $"Control {FormatCount(ReadInt64(final, "pendingControlTransactions"))} " +
        $"(peak {FormatCount(ReadInt64(final, "peakPendingControlTransactions"))}) · " +
        $"Operations {FormatCount(ReadInt64(final, "trackedOperations"))} · " +
        $"Reconnect operations {FormatCount(ReadInt64(final, "reconnectOperations"))} · " +
        $"Sessions {FormatCount(ReadInt64(final, "trackedSessions"))} · " +
        $"Listeners {FormatCount(ReadInt64(final, "trackedListeners"))}";

    private static string FormatQueues(JsonElement final)
    {
        if (!final.TryGetProperty("queues", out var queues) || queues.ValueKind != JsonValueKind.Array)
            return "Not measured";

        var summaries = new List<string>();
        foreach (var queue in queues.EnumerateArray())
        {
            if (queue.ValueKind != JsonValueKind.Object) continue;
            var name = OptionalString(queue, "name")?.Trim();
            if (string.IsNullOrWhiteSpace(name)) name = "unnamed";
            summaries.Add($"{name}: {FormatCount(ReadInt64(queue, "depth"))}/" +
                          $"{FormatCount(ReadInt64(queue, "capacity"))} (peak {FormatCount(ReadInt64(queue, "peakDepth"))}, " +
                          $"rejected {FormatCount(ReadInt64(queue, "rejected"))}, dropped {FormatCount(ReadInt64(queue, "dropped"))})");
        }

        return summaries.Count == 0 ? "Not measured" : string.Join(Environment.NewLine, summaries);
    }

    private static string DetermineCleanupState(JsonElement cleanupBefore)
    {
        var counters = new[]
        {
            ReadInt64(cleanupBefore, "trackedSessions"),
            ReadInt64(cleanupBefore, "trackedListeners"),
            ReadInt64(cleanupBefore, "pendingTransactions"),
            ReadInt64(cleanupBefore, "pendingControlTransactions"),
            ReadInt64(cleanupBefore, "trackedOperations"),
            ReadInt64(cleanupBefore, "reconnectOperations")
        };
        if (counters.Any(value => value is null)) return "Not Measured";
        return counters.Any(value => value != 0) ? "Residual State" : "Clear";
    }

    private static string FormatCleanup(JsonElement snapshot) =>
        $"Sessions {FormatCount(ReadInt64(snapshot, "trackedSessions"))} · " +
        $"Listeners {FormatCount(ReadInt64(snapshot, "trackedListeners"))} · " +
        $"Transactions {FormatCount(ReadInt64(snapshot, "pendingTransactions"))} · " +
        $"Control {FormatCount(ReadInt64(snapshot, "pendingControlTransactions"))} · " +
        $"Operations {FormatCount(ReadInt64(snapshot, "trackedOperations"))} · " +
        $"Reconnect {FormatCount(ReadInt64(snapshot, "reconnectOperations"))}";

    private static string FormatSockets(JsonElement? sockets)
    {
        if (sockets is null) return "Not measured";
        var open = FormatCount(ReadInt64(sockets, "openSocketCount"));
        var listeners = FormatCount(ReadInt64(sockets, "listenerCount"));
        var established = FormatCount(ReadInt64(sockets, "establishedCount"));
        var timeWait = FormatCount(ReadInt64(sockets, "timeWaitCount"));
        var source = ReadSocketSource(sockets.Value);
        var ownership = ReadBoolean(sockets, "isProcessOwnedMeasurement") == true ? "process-owned" : "not process-owned";
        return $"Open {open} · Listening {listeners} · Established {established} · TIME_WAIT {timeWait} · {source}, {ownership}";
    }

    private static string ReadSocketSource(JsonElement sockets)
    {
        if (!sockets.TryGetProperty("source", out var element)) return "source unavailable";
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var numeric))
            return numeric switch
            {
                0 => "unavailable",
                1 => "host registry",
                2 => "OS process table",
                3 => "injected probe",
                _ => "unknown source"
            };
        if (element.ValueKind == JsonValueKind.String)
            return element.GetString() ?? "source unavailable";
        return "source unavailable";
    }

    private static string FormatChecks(JsonElement root)
    {
        if (!root.TryGetProperty("checks", out var checks) || checks.ValueKind != JsonValueKind.Array)
            return "No imported checks.";
        var builder = new StringBuilder();
        foreach (var item in checks.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(item.GetString())) continue;
            if (builder.Length > 0) builder.AppendLine();
            builder.Append("• ").Append(item.GetString()!.Trim());
        }
        return builder.Length == 0 ? "No imported checks." : builder.ToString();
    }

    private static JsonElement RequiredObject(JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException($"Factory result is missing object '{propertyName}'.");
        return value;
    }

    private static void RequireObject(JsonElement element, string description)
    {
        if (element.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException($"{description} must be a JSON object.");
    }

    private static JsonElement? OptionalObject(JsonElement parent, string propertyName) =>
        parent.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Object ? value : null;

    private static string RequiredString(JsonElement parent, string propertyName) =>
        OptionalString(parent, propertyName) is { Length: > 0 } value
            ? value
            : throw new InvalidDataException($"Factory result is missing string '{propertyName}'.");

    private static string? OptionalString(JsonElement parent, string propertyName) =>
        parent.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static int RequiredInt32(JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out var value) || !value.TryGetInt32(out var result))
            throw new InvalidDataException($"Factory result is missing integer '{propertyName}'.");
        return result;
    }

    private static long? ReadInt64(JsonElement? parent, string propertyName)
    {
        if (parent is not { } element || !element.TryGetProperty(propertyName, out var value) ||
            value.ValueKind == JsonValueKind.Null || !value.TryGetInt64(out var result)) return null;
        return result;
    }

    private static double? ReadDouble(JsonElement? parent, string propertyName)
    {
        if (parent is not { } element || !element.TryGetProperty(propertyName, out var value) ||
            value.ValueKind == JsonValueKind.Null || !value.TryGetDouble(out var result) || !double.IsFinite(result)) return null;
        return result;
    }

    private static bool? ReadBoolean(JsonElement? parent, string propertyName)
    {
        if (parent is not { } element || !element.TryGetProperty(propertyName, out var value)) return null;
        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null
        };
    }

    private static DateTimeOffset RequiredDateTimeOffset(JsonElement parent, string propertyName)
    {
        var text = RequiredString(parent, propertyName);
        if (!DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var result))
            throw new InvalidDataException($"Factory result has invalid timestamp '{propertyName}'.");
        return result;
    }

    private static string FormatUtc(DateTimeOffset value) =>
        value.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture);

    private static string FormatElapsed(TimeSpan elapsed) => elapsed.TotalDays >= 1
        ? elapsed.ToString("d'.'hh':'mm':'ss'.'fff", CultureInfo.InvariantCulture)
        : elapsed.ToString("hh':'mm':'ss'.'fff", CultureInfo.InvariantCulture);

    private static string FormatCount(long? value) => value?.ToString("N0", CultureInfo.InvariantCulture) ?? "--";

    private static string FormatRate(double? value, string unit) =>
        value is { } number ? $"{number.ToString("N2", CultureInfo.InvariantCulture)} {unit}" : "--";

    private static string FormatMilliseconds(double? value) =>
        value is { } number ? $"{number.ToString("N3", CultureInfo.InvariantCulture)} ms" : "--";

    private static string FormatBytes(long? value)
    {
        if (value is null) return "--";
        string[] units = ["B", "KiB", "MiB", "GiB", "TiB"];
        var number = (double)value.Value;
        var unit = 0;
        while (Math.Abs(number) >= 1024 && unit < units.Length - 1)
        {
            number /= 1024;
            unit++;
        }
        return $"{number.ToString(unit == 0 ? "N0" : "N2", CultureInfo.InvariantCulture)} {units[unit]}";
    }
}
