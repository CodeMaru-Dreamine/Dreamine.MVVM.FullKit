using System.Net.Http;
using System.Net.Http.Json;
using DreamineVMS.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DreamineVMS.Services.Agent;

/// <summary>중앙 DreamineVMS.Web 서버와 통신하는 HTTP 클라이언트입니다.</summary>
public sealed class AgentApiClient
{
    private readonly HttpClient _http;
    private readonly ILogger<AgentApiClient> _logger;
    private readonly List<Uri> _serverUrls;
    private Uri? _activeServerUrl;

    public string? Token { get; private set; }
    public string? TenantId { get; private set; }
    public List<AgentCameraInfo> Cameras { get; private set; } = [];

    private string? _email;
    private string? _password;

    public AgentApiClient(IConfiguration config, ILogger<AgentApiClient> logger)
    {
        _logger = logger;
        _serverUrls = BuildServerCandidates(config["Agent:ServerUrl"]);
        _activeServerUrl = _serverUrls.FirstOrDefault();
        _http = new HttpClient();
        _email = config["Agent:Email"];
        _password = config["Agent:Password"];
    }

    /// <summary>런타임에 자격증명과 서버 URL을 갱신합니다. 저장 후 재연결 시 호출하세요.</summary>
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

    public string? Email => _email;
    public string? ServerUrl => _activeServerUrl?.ToString().TrimEnd('/');

    /// <summary>저장된 자격증명으로 로그인합니다.</summary>
    public Task<(bool Ok, string Error)> LoginWithStoredCredentialsAsync()
    {
        if (string.IsNullOrWhiteSpace(_email) || string.IsNullOrWhiteSpace(_password))
            return Task.FromResult((false, "이메일 또는 비밀번호가 설정되지 않았습니다."));
        return LoginAsync(_email, _password);
    }

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

    /// <summary>세그먼트 업로드. 401 반환 시 Token을 null로 설정해 재로그인을 유도합니다.</summary>
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

    public void ClearToken() => Token = null;

    private Uri GetActiveServerUrl()
    {
        return _activeServerUrl ?? _serverUrls.First();
    }

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

        void Add(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            candidates.Add(url.Trim());
        }
    }
}

public sealed class AgentCameraInfo
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Host { get; set; } = "";
    public string RtspUrl { get; set; } = "";
    public bool AutoReconnect { get; set; }
    public bool IsPublic { get; set; }
}

file sealed class AgentLoginResponse
{
    public string Token { get; set; } = "";
    public string TenantId { get; set; } = "";
    public List<AgentCameraInfo>? Cameras { get; set; }
}
