using Codemaru.Options;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Codemaru.Services.Certificates;

/// <summary>
/// \if KO
/// <para>\brief 인증서 설정을 appsettings.local.json에 저장합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates certificate settings writer functionality and related state.</para>
/// \endif
/// </summary>
public sealed class CertificateSettingsWriter : ICertificateSettingsWriter
{
    /// <summary>
    /// \if KO
    /// <para>settings Path 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the settings path value.</para>
    /// \endif
    /// </summary>
    private readonly string _settingsPath;

    /// <summary>
    /// \if KO
    /// <para>\brief CertificateSettingsWriter 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="CertificateSettingsWriter"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    public CertificateSettingsWriter()
    {
        _settingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.local.json");
    }

    /// <summary>
    /// \if KO
    /// <para>Async 데이터를 저장합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Saves async data.</para>
    /// \endif
    /// </summary>
    /// <param name="options">
    /// \if KO
    /// <para>동작을 구성하는 설정입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The options that configure the operation.</para>
    /// \endif
    /// </param>
    /// <param name="cancellationToken">
    /// \if KO
    /// <para>취소 요청을 감시하는 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Save Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the save async operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Root Async 데이터를 불러옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Loads root async data.</para>
    /// \endif
    /// </summary>
    /// <param name="cancellationToken">
    /// \if KO
    /// <para>취소 요청을 감시하는 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Load Root Async 작업에서 생성한 <c>Task&lt;JsonObject&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;JsonObject&gt;</c> result produced by the load root async operation.</para>
    /// \endif
    /// </returns>
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
