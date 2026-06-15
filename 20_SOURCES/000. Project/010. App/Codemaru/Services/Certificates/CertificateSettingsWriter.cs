using Codemaru.Options;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Codemaru.Services.Certificates;

/// <summary>
/// \brief 인증서 설정을 appsettings.local.json에 저장합니다.
/// </summary>
public sealed class CertificateSettingsWriter : ICertificateSettingsWriter
{
    private readonly string _settingsPath;

    /// <summary>
    /// \brief CertificateSettingsWriter 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    public CertificateSettingsWriter()
    {
        _settingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.local.json");
    }

    /// <inheritdoc />
    public async Task SaveAsync(CertificateMonitorOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);

        JsonObject root = await LoadRootAsync(cancellationToken).ConfigureAwait(false);
        JsonArray patterns = [];
        foreach (string pattern in options.CertificateFilePatterns)
        {
            patterns.Add(pattern);
        }

        root["CertificateMonitor"] = new JsonObject
        {
            ["CertificateDirectory"] = options.CertificateDirectory,
            ["CertificateFilePatterns"] = patterns,
            ["PfxPassword"] = options.PfxPassword,
            ["WacsPath"] = options.WacsPath,
            ["NginxPath"] = options.NginxPath,
            ["NginxWorkingDirectory"] = options.NginxWorkingDirectory,
            ["NginxReloadArguments"] = options.NginxReloadArguments,
            ["WarningDays"] = options.WarningDays,
            ["CriticalDays"] = options.CriticalDays,
            ["MaxCommandOutputChars"] = options.MaxCommandOutputChars
        };

        JsonSerializerOptions serializerOptions = new() { WriteIndented = true };
        string json = root.ToJsonString(serializerOptions);
        await File.WriteAllTextAsync(_settingsPath, json, cancellationToken).ConfigureAwait(false);
    }

    private async Task<JsonObject> LoadRootAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_settingsPath))
        {
            return [];
        }

        string json = await File.ReadAllTextAsync(_settingsPath, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        JsonNode? node = JsonNode.Parse(json);
        return node as JsonObject ?? [];
    }
}
