using System.IO;
using System.Text;
using System.Text.Json;
using Dreamine.SecsGem.Interop.Wpf.Models;

namespace Dreamine.SecsGem.Interop.Wpf.Managers;

public sealed partial class ResultExportManager
{
    public async Task ExportSelfTestAsync(string path, SelfTestSummary summary, CancellationToken token)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var payload = new SelfTestExport(
            summary.StartedAt,
            summary.CompletedAt,
            summary.Requested,
            summary.Passed,
            summary.Failed,
            summary.TimeoutCount,
            summary.ReconnectCycles,
            summary.LinktestCount,
            summary.MessagesPerSecond,
            summary.MinimumLatencyMs,
            summary.AverageLatencyMs,
            summary.MaximumLatencyMs,
            summary.P95LatencyMs,
            SafeResult(summary.Result),
            summary.Checks.Count);
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(payload, JsonOptions), token).ConfigureAwait(false);
    }

    internal async Task ExportMultiEquipmentSelfTestAsync(string path, MultiEquipmentSelfTestSummary summary,
        CancellationToken token)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var payload = new MultiEquipmentSelfTestExport(
            summary.StartedAt,
            summary.CompletedAt,
            summary.MaximumEquipment,
            summary.ConnectionsAttempted,
            summary.MessagesRequested,
            summary.MessagesPassed,
            summary.TimeoutCount,
            summary.ReconnectCount,
            summary.MemoryDeltaBytes,
            summary.ConnectionsPerSecond,
            summary.MessagesPerSecond,
            summary.MinimumLatencyMs,
            summary.AverageLatencyMs,
            summary.MaximumLatencyMs,
            summary.RemainingSessions,
            summary.RemainingBackgroundOperations,
            summary.TrackedOperationTaskCount,
            summary.PeakConcurrentEquipmentOperations,
            summary.ConnectionTimings.Select(value => new EquipmentTimingExport(
                value.EquipmentCount,
                value.ConnectionElapsedMs)).ToArray(),
            SafeResult(summary.Result),
            summary.Checks.Count);
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(payload, JsonOptions), token).ConfigureAwait(false);
    }

    public async Task<(string JsonPath, string MarkdownPath)> ExportAsync(string directory,
        IEnumerable<InteropScenarioResult> scenarios, IEnumerable<InteropLogEntry> logs, CancellationToken token)
    {
        Directory.CreateDirectory(directory);
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var jsonPath = Path.Combine(directory, $"interop-{stamp}.json");
        var markdownPath = Path.Combine(directory, $"interop-{stamp}.md");
        var logSnapshot = logs.ToArray();
        var privateLogCount = logSnapshot.Count(entry => entry.ContainsPrivateProfileData);
        var safeScenarios = scenarios.Select((item, index) => new ExportScenario(
            SafeScenarioId(item.Id, index),
            item.Status.ToString(),
            item.External,
            item.Elapsed.TotalMilliseconds)).ToArray();
        var safeLogs = logSnapshot
            .Where(entry => !entry.ContainsPrivateProfileData)
            .Select(entry => new ExportLog(
                entry.Timestamp,
                SafeLevel(entry.Level),
                SafeCategory(entry.Category),
                SafeDirection(entry.Direction),
                SafeSxFy(entry),
                entry.Result))
            .ToArray();
        var payload = new ExportPayload(
            DateTimeOffset.Now,
            "Interoperability evidence; not a compliance certificate. Private-profile traffic is excluded.",
            privateLogCount,
            safeScenarios,
            safeLogs);
        await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(payload, JsonOptions), token).ConfigureAwait(false);
        var markdown = new StringBuilder(
            "# Interoperability Test Result\n\n> Evidence only; this is not a compliance certificate. " +
            "Free-form text, endpoints, identifiers, decoded bodies, and raw frames are intentionally omitted.\n\n" +
            "| ID | Status | External | Elapsed (ms) |\n|---|---|---:|---:|\n");
        foreach (var item in safeScenarios)
            markdown.AppendLine($"| {Escape(item.Id)} | {item.Status} | {item.External} | {item.ElapsedMilliseconds:F3} |");
        await File.WriteAllTextAsync(markdownPath, markdown.ToString(), token).ConfigureAwait(false);
        return (jsonPath, markdownPath);
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static string SafeLevel(string value) => value is "Info" or "Error" ? value : "Other";
    private static string SafeCategory(string value) => value is
        "SECS-II" or "HSMS" or "Wire Log" or "SelfTest" or "Scenario" ? value : "Other";
    private static string SafeDirection(string value) => value is
        "RX" or "TX" or "Inbound" or "Outbound" ? value : "--";
    private static string SafeResult(string value) => value is
        "Passed" or "Failed" or "Cancelled" ? value : "Other";
    private static string SafeScenarioId(string value, int index)
    {
        if (value.Length is < 1 or > 32) return $"Scenario-{index + 1}";
        foreach (var character in value)
            if (!char.IsAsciiLetterOrDigit(character) && character is not '-' and not '_')
                return $"Scenario-{index + 1}";
        return value;
    }
    private static string SafeSxFy(InteropLogEntry entry)
    {
        var value = entry.SxFy;
        if (value.Length is < 4 or > 9 || value[0] != 'S') return "--";
        var separator = value.IndexOf('F', 1);
        return separator > 1 && separator < value.Length - 1 &&
               value.AsSpan(1, separator - 1).IndexOfAnyExceptInRange('0', '9') < 0 &&
               value.AsSpan(separator + 1).IndexOfAnyExceptInRange('0', '9') < 0
            ? value
            : "--";
    }
    private static string Escape(string value) => value.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");

    private sealed record ExportPayload(
        DateTimeOffset GeneratedAt,
        string Scope,
        int ExcludedPrivateProfileLogCount,
        IReadOnlyList<ExportScenario> Scenarios,
        IReadOnlyList<ExportLog> Logs);

    private sealed record ExportScenario(
        string Id,
        string Status,
        bool External,
        double ElapsedMilliseconds);

    private sealed record ExportLog(
        DateTimeOffset Timestamp,
        string Level,
        string Category,
        string Direction,
        string SxFy,
        string Result);

    private sealed record SelfTestExport(
        DateTimeOffset StartedAt,
        DateTimeOffset CompletedAt,
        int Requested,
        int Passed,
        int Failed,
        int TimeoutCount,
        int ReconnectCycles,
        int LinktestCount,
        double MessagesPerSecond,
        double MinimumLatencyMs,
        double AverageLatencyMs,
        double MaximumLatencyMs,
        double P95LatencyMs,
        string Result,
        int CheckCount);

    private sealed record MultiEquipmentSelfTestExport(
        DateTimeOffset StartedAt,
        DateTimeOffset CompletedAt,
        int MaximumEquipment,
        int ConnectionsAttempted,
        int MessagesRequested,
        int MessagesPassed,
        int TimeoutCount,
        int ReconnectCount,
        long MemoryDeltaBytes,
        double ConnectionsPerSecond,
        double MessagesPerSecond,
        double MinimumLatencyMs,
        double AverageLatencyMs,
        double MaximumLatencyMs,
        int RemainingSessions,
        int RemainingBackgroundOperations,
        long TrackedOperationTaskCount,
        int PeakConcurrentEquipmentOperations,
        IReadOnlyList<EquipmentTimingExport> ConnectionTimings,
        string Result,
        int CheckCount);

    private sealed record EquipmentTimingExport(int EquipmentCount, double ConnectionElapsedMs);
}
