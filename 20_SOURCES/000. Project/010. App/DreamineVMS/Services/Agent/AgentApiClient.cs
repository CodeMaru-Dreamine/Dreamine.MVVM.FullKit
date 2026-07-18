using System.Net.Http;
using System.Net.Http.Json;
using DreamineVMS.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DreamineVMS.Services.Agent;

/// <summary>
/// \if KO
/// <para>중앙 DreamineVMS.Web 서버와 통신하는 HTTP 클라이언트입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates agent api client functionality and related state.</para>
/// \endif
/// </summary>
public sealed class AgentApiClient
{
    /// <summary>
    /// \if KO
    /// <para>http 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the http value.</para>
    /// \endif
    /// </summary>
    private readonly HttpClient _http;
    /// <summary>
    /// \if KO
    /// <para>logger 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the logger value.</para>
    /// \endif
    /// </summary>
    private readonly ILogger<AgentApiClient> _logger;
    /// <summary>
    /// \if KO
    /// <para>server Urls 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the server urls value.</para>
    /// \endif
    /// </summary>
    private readonly List<Uri> _serverUrls;
    /// <summary>
    /// \if KO
    /// <para>active Server Url 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the active server url value.</para>
    /// \endif
    /// </summary>
    private Uri? _activeServerUrl;

    /// <summary>
    /// \if KO
    /// <para>Token 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the token value.</para>
    /// \endif
    /// </summary>
    public string? Token { get; private set; }
    /// <summary>
    /// \if KO
    /// <para>Tenant Id 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the tenant id value.</para>
    /// \endif
    /// </summary>
    public string? TenantId { get; private set; }
    /// <summary>
    /// \if KO
    /// <para>Cameras 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the cameras value.</para>
    /// \endif
    /// </summary>
    public List<AgentCameraInfo> Cameras { get; private set; } = [];

    /// <summary>
    /// \if KO
    /// <para>email 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the email value.</para>
    /// \endif
    /// </summary>
    private string? _email;
    /// <summary>
    /// \if KO
    /// <para>password 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the password value.</para>
    /// \endif
    /// </summary>
    private string? _password;

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="AgentApiClient"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="AgentApiClient"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="config">
    /// \if KO
    /// <para>config에 사용할 <c>IConfiguration</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IConfiguration</c> value used for config.</para>
    /// \endif
    /// </param>
    /// <param name="logger">
    /// \if KO
    /// <para>logger에 사용할 <c>ILogger&lt;AgentApiClient&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ILogger&lt;AgentApiClient&gt;</c> value used for logger.</para>
    /// \endif
    /// </param>
    public AgentApiClient(IConfiguration config, ILogger<AgentApiClient> logger)
    {
        _logger = logger;
        _serverUrls = BuildServerCandidates(config["Agent:ServerUrl"]);
        _activeServerUrl = _serverUrls.FirstOrDefault();
        _http = new HttpClient();
        _email = config["Agent:Email"];
        _password = config["Agent:Password"];
    }

    /// <summary>
    /// \if KO
    /// <para>런타임에 자격증명과 서버 URL을 갱신합니다. 저장 후 재연결 시 호출하세요.</para>
    /// \endif
    /// \if EN
    /// <para>Sets the credentials value.</para>
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
    public void SetCredentials(string serverUrl, string email, string password)
    {
        _email = email;
        _password = password;
        var newUrls = BuildServerCandidates(serverUrl);
        _serverUrls.Clear();
        _serverUrls.AddRange(newUrls);
        _activeServerUrl = _serverUrls.FirstOrDefault();
        Token = null; // 재로그인 유도
    }

    /// <summary>
    /// \if KO
    /// <para>Email 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the email value.</para>
    /// \endif
    /// </summary>
    public string? Email => _email;
    /// <summary>
    /// \if KO
    /// <para>Server Url 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the server url value.</para>
    /// \endif
    /// </summary>
    public string? ServerUrl => _activeServerUrl?.ToString().TrimEnd('/');

    /// <summary>
    /// \if KO
    /// <para>저장된 자격증명으로 로그인합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the login with stored credentials async operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Login With Stored Credentials Async 작업에서 생성한 <c>Task&lt;(bool Ok, string Error)&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;(bool Ok, string Error)&gt;</c> result produced by the login with stored credentials async operation.</para>
    /// \endif
    /// </returns>
    public Task<(bool Ok, string Error)> LoginWithStoredCredentialsAsync()
    {
        if (string.IsNullOrWhiteSpace(_email) || string.IsNullOrWhiteSpace(_password))
            return Task.FromResult((false, "이메일 또는 비밀번호가 설정되지 않았습니다."));
        return LoginAsync(_email, _password);
    }

    /// <summary>
    /// \if KO
    /// <para>Login Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the login async operation.</para>
    /// \endif
    /// </summary>
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
    /// <returns>
    /// \if KO
    /// <para>Login Async 작업에서 생성한 <c>Task&lt;(bool Ok, string Error)&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;(bool Ok, string Error)&gt;</c> result produced by the login async operation.</para>
    /// \endif
    /// </returns>
    public async Task<(bool Ok, string Error)> LoginAsync(string email, string password)
    {
        foreach (Uri serverUrl in EnumerateServerUrls())
        {
            try
            {
                var resp = await _http.PostAsJsonAsync(
                    new Uri(serverUrl, "/api/agent/login"),
                    new { Email = email, Password = password });

                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogWarning("[Agent] 로그인 실패: {Server} responded {StatusCode}.",
                        serverUrl, (int)resp.StatusCode);
                    continue;
                }

                var data = await resp.Content.ReadFromJsonAsync<AgentLoginResponse>();
                if (data is null)
                {
                    _logger.LogWarning("[Agent] 로그인 실패: {Server} returned an empty response.", serverUrl);
                    continue;
                }

                _activeServerUrl = serverUrl;
                Token = data.Token;
                TenantId = data.TenantId;
                Cameras = data.Cameras ?? [];
                _logger.LogInformation("[Agent] 중앙 서버 연결: {Server}", serverUrl);
                return (true, "");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Agent] 중앙 서버 연결 실패: {Server}", serverUrl);
            }
        }

        Token = null;
        return (false, "중앙 서버 연결 실패");
    }

    /// <summary>
    /// \if KO
    /// <para>Sync Cameras Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the sync cameras async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="cameras">
    /// \if KO
    /// <para>cameras에 사용할 <c>IEnumerable&lt;AgentCameraInfo&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IEnumerable&lt;AgentCameraInfo&gt;</c> value used for cameras.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Sync Cameras Async 작업에서 생성한 <c>Task&lt;bool&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;bool&gt;</c> result produced by the sync cameras async operation.</para>
    /// \endif
    /// </returns>
    public async Task<bool> SyncCamerasAsync(IEnumerable<AgentCameraInfo> cameras)
    {
        if (Token is null) return false;
        try
        {
            Uri serverUrl = GetActiveServerUrl();
            var payload = new
            {
                Token,
                Cameras = cameras.Select(c => new
                {
                    c.Id, c.Name, c.Host, c.RtspUrl, c.AutoReconnect, c.IsPublic
                })
            };
            var resp = await _http.PostAsJsonAsync(new Uri(serverUrl, "/api/agent/sync-cameras"), payload);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("[Agent] 카메라 동기화 실패: {Server} responded {StatusCode}.",
                    serverUrl, (int)resp.StatusCode);
            }

            return resp.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Agent] 카메라 동기화 중 오류");
            return false;
        }
    }

    /// <summary>
    /// \if KO
    /// <para>세그먼트 업로드. 401 반환 시 Token을 null로 설정해 재로그인을 유도합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the push segment async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="cameraId">
    /// \if KO
    /// <para>camera Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for camera id.</para>
    /// \endif
    /// </param>
    /// <param name="filename">
    /// \if KO
    /// <para>filename에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for filename.</para>
    /// \endif
    /// </param>
    /// <param name="data">
    /// \if KO
    /// <para>data에 사용할 <c>byte[]</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>byte[]</c> value used for data.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Push Segment Async 작업에서 생성한 <c>Task&lt;bool&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;bool&gt;</c> result produced by the push segment async operation.</para>
    /// \endif
    /// </returns>
    public async Task<bool> PushSegmentAsync(string cameraId, string filename, byte[] data)
    {
        if (Token is null) return false;
        try
        {
            Uri serverUrl = GetActiveServerUrl();
            using var content = new ByteArrayContent(data);
            var resp = await _http.PostAsync(
                new Uri(serverUrl, $"/api/agent/hls/{Token}/{cameraId}/{filename}"),
                content);

            if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                Token = null; // 재로그인 트리거
                _logger.LogWarning("[Agent] 세그먼트 업로드 인증 만료: {CameraId}/{Filename}", cameraId, filename);
                return false;
            }

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("[Agent] 세그먼트 업로드 실패: {CameraId}/{Filename} responded {StatusCode}.",
                    cameraId, filename, (int)resp.StatusCode);
            }

            return resp.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Token = null; // 서버 포트가 바뀌었거나 재시작된 경우 다음 루프에서 재로그인합니다.
            _logger.LogWarning(ex, "[Agent] 세그먼트 업로드 중 오류: {CameraId}/{Filename}", cameraId, filename);
            return false;
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Clear Token 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the clear token operation.</para>
    /// \endif
    /// </summary>
    public void ClearToken() => Token = null;

    /// <summary>
    /// \if KO
    /// <para>Active Server Url 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the active server url value.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Get Active Server Url 작업에서 생성한 <c>Uri</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Uri</c> result produced by the get active server url operation.</para>
    /// \endif
    /// </returns>
    private Uri GetActiveServerUrl()
    {
        return _activeServerUrl ?? _serverUrls.First();
    }

    /// <summary>
    /// \if KO
    /// <para>Enumerate Server Urls 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the enumerate server urls operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Enumerate Server Urls 작업에서 생성한 <c>IEnumerable&lt;Uri&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IEnumerable&lt;Uri&gt;</c> result produced by the enumerate server urls operation.</para>
    /// \endif
    /// </returns>
    private IEnumerable<Uri> EnumerateServerUrls()
    {
        if (_activeServerUrl is not null)
        {
            yield return _activeServerUrl;
        }

        foreach (Uri serverUrl in _serverUrls)
        {
            if (_activeServerUrl is not null && serverUrl == _activeServerUrl)
            {
                continue;
            }

            yield return serverUrl;
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Server Candidates 값을 구성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Builds the server candidates value.</para>
    /// \endif
    /// </summary>
    /// <param name="configuredUrl">
    /// \if KO
    /// <para>configured Url에 사용할 <c>string?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for configured url.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Build Server Candidates 작업에서 생성한 <c>List&lt;Uri&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>List&lt;Uri&gt;</c> result produced by the build server candidates operation.</para>
    /// \endif
    /// </returns>
    private static List<Uri> BuildServerCandidates(string? configuredUrl)
    {
        var candidates = new List<string>();
        Add(configuredUrl);
        Add("http://localhost:6080");
        Add("https://localhost:6443");

        return candidates
            .Select(item => new Uri(item.EndsWith('/') ? item : item + "/"))
            .Distinct()
            .ToList();

        #pragma warning disable CS1587
        /// \cond LOCAL_FUNCTION_DOCUMENTATION
        /// <summary>
        /// \if KO
        /// <para>비어 있지 않은 서버 URL을 후보 목록에 추가합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Adds a non-empty server URL to the candidate list.</para>
        /// \endif
        /// </summary>
        /// <param name="url">
        /// \if KO
        /// <para>추가할 선택적 서버 URL입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The optional server URL to add.</para>
        /// \endif
        /// </param>
        /// \endcond
        void Add(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            candidates.Add(url.Trim());
        }
        #pragma warning restore CS1587
    }
}

/// <summary>
/// \if KO
/// <para>Agent Camera Info 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates agent camera info functionality and related state.</para>
/// \endif
/// </summary>
public sealed class AgentCameraInfo
{
    /// <summary>
    /// \if KO
    /// <para>Id 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the id value.</para>
    /// \endif
    /// </summary>
    public string Id { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Name 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the name value.</para>
    /// \endif
    /// </summary>
    public string Name { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Host 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the host value.</para>
    /// \endif
    /// </summary>
    public string Host { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Rtsp Url 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the rtsp url value.</para>
    /// \endif
    /// </summary>
    public string RtspUrl { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Auto Reconnect 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the auto reconnect value.</para>
    /// \endif
    /// </summary>
    public bool AutoReconnect { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Is Public 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the is public value.</para>
    /// \endif
    /// </summary>
    public bool IsPublic { get; set; }
}

/// <summary>
/// \if KO
/// <para>Agent Login Response 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates agent login response functionality and related state.</para>
/// \endif
/// </summary>
file sealed class AgentLoginResponse
{
    /// <summary>
    /// \if KO
    /// <para>Token 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the token value.</para>
    /// \endif
    /// </summary>
    public string Token { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Tenant Id 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the tenant id value.</para>
    /// \endif
    /// </summary>
    public string TenantId { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Cameras 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the cameras value.</para>
    /// \endif
    /// </summary>
    public List<AgentCameraInfo>? Cameras { get; set; }
}
