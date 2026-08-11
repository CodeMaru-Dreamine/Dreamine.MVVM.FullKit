using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.IO;
using Dreamine.SecsGem.Interop.Wpf.Models;

namespace Dreamine.SecsGem.Interop.Wpf.Managers;

public sealed partial class ResultExportManager
{
    public async Task ExportSelfTestAsync(string path, SelfTestSummary summary, CancellationToken token)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(summary, JsonOptions), token).ConfigureAwait(false);
    }

    internal async Task ExportMultiEquipmentSelfTestAsync(string path, MultiEquipmentSelfTestSummary summary,
        CancellationToken token)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(summary, JsonOptions), token).ConfigureAwait(false);
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
        var safeLogs = logSnapshot.Where(entry => !entry.ContainsPrivateProfileData).Select(entry => entry with
        {
            Summary = Mask(entry.Summary) ?? string.Empty,
            Message = Mask(entry.Message),
            RawHex = entry.RawHex is { Length: > 4096 } hex ? hex[..4096] + "…" : entry.RawHex
        }).ToArray();
        var payload = new
        {
            GeneratedAt = DateTimeOffset.Now,
            Scope = "Interoperability evidence; not a compliance certificate. Private-profile traffic is excluded.",
            ExcludedPrivateProfileLogCount = privateLogCount,
            Scenarios = scenarios,
            Logs = safeLogs
        };
        await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(payload, JsonOptions), token).ConfigureAwait(false);
        var markdown = new StringBuilder("# Interoperability Test Result\n\n> Evidence only; this is not a compliance certificate.\n\n| ID | Scenario | Status | Detail |\n|---|---|---|---|\n");
        foreach (var item in scenarios) markdown.AppendLine($"| {item.Id} | {Escape(item.Name)} | {item.Status} | {Escape(Mask(item.Detail) ?? string.Empty)} |");
        await File.WriteAllTextAsync(markdownPath, markdown.ToString(), token).ConfigureAwait(false);
        return (jsonPath, markdownPath);
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static string? Mask(string? value)
    {
        if (value is null) return null;
        value = Ipv4Regex().Replace(value, "***.***.***.***");
        return WindowsPathRegex().Replace(value, "<LOCAL_PATH>");
    }
    private static string Escape(string value) => value.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
    [GeneratedRegex(@"\b(?:\d{1,3}\.){3}\d{1,3}\b")]
    private static partial Regex Ipv4Regex();
    [GeneratedRegex(@"(?i)\b[A-Z]:\\[^\r\n\""']+")]
    private static partial Regex WindowsPathRegex();
}
