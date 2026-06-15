using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DreamineVMS.Services.Agent;

/// <summary>에이전트 접속 정보를 appsettings.local.json에 저장합니다.</summary>
public sealed class AgentSettingsWriter
{
    private readonly string _settingsPath;

    public AgentSettingsWriter()
    {
        _settingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.local.json");
    }

    public async Task SaveAsync(string serverUrl, string email, string password, CancellationToken cancellationToken = default)
    {
        JsonObject root = await LoadRootAsync(cancellationToken).ConfigureAwait(false);

        root["Agent"] = new JsonObject
        {
            ["ServerUrl"] = serverUrl,
            ["Email"]     = email,
            ["Password"]  = password
        };

        string json = root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_settingsPath, json, cancellationToken).ConfigureAwait(false);
    }

    private async Task<JsonObject> LoadRootAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_settingsPath)) return [];
        string json = await File.ReadAllTextAsync(_settingsPath, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(json)) return [];
        return JsonNode.Parse(json) as JsonObject ?? [];
    }
}
