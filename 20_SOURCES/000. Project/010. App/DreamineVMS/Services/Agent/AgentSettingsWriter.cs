using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DreamineVMS.Services.Agent;

/// <summary>
/// \if KO
/// <para>에이전트 접속 정보를 appsettings.local.json에 저장합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates agent settings writer functionality and related state.</para>
/// \endif
/// </summary>
public sealed class AgentSettingsWriter
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
    /// <para>지정한 설정으로 <see cref="AgentSettingsWriter"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="AgentSettingsWriter"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    public AgentSettingsWriter()
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
    /// <param name="serverUrl">
    /// \if KO
    /// <para>server Url에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for server url.</para>
    /// \endif
    /// </param>
    /// <param name="email">
    /// \if KO
    /// <para>email에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for email.</para>
    /// \endif
    /// </param>
    /// <param name="password">
    /// \if KO
    /// <para>password에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for password.</para>
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
        if (!File.Exists(_settingsPath)) return [];
        string json = await File.ReadAllTextAsync(_settingsPath, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(json)) return [];
        return JsonNode.Parse(json) as JsonObject ?? [];
    }
}
