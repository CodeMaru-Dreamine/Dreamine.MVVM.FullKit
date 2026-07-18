using DreamineVMS.Services.Cameras;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;

namespace DreamineVMS.Services.Agent;

/// <summary>
/// \if KO
/// <para>에이전트 모드: 자동 로그인 → 로컬 카메라를 서버에 등록 → HLS 세그먼트 업로드. 카메라별로 독립 루프를 병렬 실행하여 한 카메라 업로드가 다른 카메라를 블로킹하지 않음.</para>
/// \endif
/// \if EN
/// <para>Encapsulates hls segment pusher service functionality and related state.</para>
/// \endif
/// </summary>
public sealed class HlsSegmentPusherService : BackgroundService
{
    /// <summary>
    /// \if KO
    /// <para>api 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the api value.</para>
    /// \endif
    /// </summary>
    private readonly AgentApiClient _api;
    /// <summary>
    /// \if KO
    /// <para>repository 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the repository value.</para>
    /// \endif
    /// </summary>
    private readonly IVmsCameraRepository _repository;
    /// <summary>
    /// \if KO
    /// <para>config 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the config value.</para>
    /// \endif
    /// </summary>
    private readonly IConfiguration _config;
    /// <summary>
    /// \if KO
    /// <para>logger 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the logger value.</para>
    /// \endif
    /// </summary>
    private readonly ILogger<HlsSegmentPusherService> _logger;
    /// <summary>
    /// \if KO
    /// <para>login Lock 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the login lock value.</para>
    /// \endif
    /// </summary>
    private readonly SemaphoreSlim _loginLock = new(1, 1);

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="HlsSegmentPusherService"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="HlsSegmentPusherService"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="api">
    /// \if KO
    /// <para>api에 사용할 <c>AgentApiClient</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>AgentApiClient</c> value used for api.</para>
    /// \endif
    /// </param>
    /// <param name="repository">
    /// \if KO
    /// <para>repository에 사용할 <c>IVmsCameraRepository</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IVmsCameraRepository</c> value used for repository.</para>
    /// \endif
    /// </param>
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
    /// <para>logger에 사용할 <c>ILogger&lt;HlsSegmentPusherService&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ILogger&lt;HlsSegmentPusherService&gt;</c> value used for logger.</para>
    /// \endif
    /// </param>
    public HlsSegmentPusherService(
        AgentApiClient api,
        IVmsCameraRepository repository,
        IConfiguration config,
        ILogger<HlsSegmentPusherService> logger)
    {
        _api = api;
        _repository = repository;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// \if KO
    /// <para>Execute Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the execute async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="stoppingToken">
    /// \if KO
    /// <para>stopping Token에 사용할 <c>CancellationToken</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CancellationToken</c> value used for stopping token.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Execute Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the execute async operation.</para>
    /// \endif
    /// </returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 자격증명이 설정될 때까지 대기 (앱 실행 후 Agent Settings에서 입력해도 동작)
        while (!stoppingToken.IsCancellationRequested)
        {
            if (!string.IsNullOrWhiteSpace(_config["Agent:Email"]) &&
                !string.IsNullOrWhiteSpace(_config["Agent:Password"]))
                break;

            _logger.LogInformation("[Agent] Agent:Email/Password 미설정 — 30초 후 재확인.");
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }

        if (stoppingToken.IsCancellationRequested) return;

        var outputRoot = GetOutputRoot();
        _logger.LogInformation("[Agent] 세그먼트 업로드 시작. 루트: {Root}", outputRoot);

        // 카메라 목록을 동적으로 관리하는 메인 루프
        var runningCameras = new Dictionary<string, CancellationTokenSource>();

        while (!stoppingToken.IsCancellationRequested)
        {
            await EnsureLoggedInAsync(stoppingToken);
            if (_api.Token is null) { await Task.Delay(5000, stoppingToken); continue; }

            await SyncCamerasAsync();

            // 활성 카메라 목록과 비교해서 추가/제거
            var currentIds = _repository.GetAll()
                .Where(c => c.Enabled && !c.IsDirectHls)
                .Select(c => c.Id)
                .ToHashSet();

            // 제거된 카메라 루프 취소
            foreach (var id in runningCameras.Keys.Except(currentIds).ToList())
            {
                runningCameras[id].Cancel();
                runningCameras.Remove(id);
                _logger.LogInformation("[Agent] 카메라 루프 중지: {Id}", id);
            }

            // 새 카메라 루프 시작
            foreach (var id in currentIds.Except(runningCameras.Keys))
            {
                var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                runningCameras[id] = cts;
                _ = Task.Run(() => RunCameraLoopAsync(id, outputRoot, cts.Token), cts.Token);
                _logger.LogInformation("[Agent] 카메라 루프 시작: {Id}", id);
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }

        // 종료 시 모든 루프 취소
        foreach (var cts in runningCameras.Values) cts.Cancel();
    }

    /// <summary>
    /// \if KO
    /// <para>Run Camera Loop Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the run camera loop async operation.</para>
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
    /// <param name="outputRoot">
    /// \if KO
    /// <para>output Root에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for output root.</para>
    /// \endif
    /// </param>
    /// <param name="ct">
    /// \if KO
    /// <para>취소 요청을 감시하는 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Run Camera Loop Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the run camera loop async operation.</para>
    /// \endif
    /// </returns>
    private async Task RunCameraLoopAsync(string cameraId, string outputRoot, CancellationToken ct)
    {
        var pushed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var camDir = Path.Combine(outputRoot, cameraId);
        byte[]? lastPushedM3u8 = null;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (_api.Token is null)
                {
                    await EnsureLoggedInAsync(ct);
                    if (_api.Token is null)
                    {
                        await Task.Delay(2000, ct);
                        continue;
                    }

                    await SyncCamerasAsync();
                }

                if (!Directory.Exists(camDir))
                {
                    await Task.Delay(1000, ct);
                    continue;
                }

                var m3u8Path = Path.Combine(camDir, "index.m3u8");
                if (!File.Exists(m3u8Path))
                {
                    await Task.Delay(500, ct);
                    continue;
                }

                byte[] m3u8Bytes;
                List<string> refs;
                try
                {
                    m3u8Bytes = await File.ReadAllBytesAsync(m3u8Path, ct);
                    refs = ParseSegmentsFromM3u8(m3u8Bytes);
                }
                catch (IOException)
                {
                    await Task.Delay(100, ct);
                    continue;
                }

                if (refs.Count == 0)
                {
                    await Task.Delay(500, ct);
                    continue;
                }

                bool allRefsPushed = true;
                foreach (string filename in refs)
                {
                    if (ct.IsCancellationRequested || _api.Token is null)
                    {
                        allRefsPushed = false;
                        break;
                    }

                    if (pushed.Contains(filename))
                    {
                        continue;
                    }

                    string segmentPath = Path.Combine(camDir, filename);
                    if (!File.Exists(segmentPath))
                    {
                        allRefsPushed = false;
                        break;
                    }

                    bool ok = await TryPushFileAsync(cameraId, segmentPath, filename);
                    if (ok)
                    {
                        pushed.Add(filename);
                    }
                    else
                    {
                        allRefsPushed = false;
                        break;
                    }
                }

                if (allRefsPushed && !m3u8Bytes.AsSpan().SequenceEqual(lastPushedM3u8))
                {
                    bool ok = await _api.PushSegmentAsync(cameraId, "index.m3u8", m3u8Bytes);
                    if (ok)
                    {
                        lastPushedM3u8 = m3u8Bytes;
                    }
                }

                if (pushed.Count > 200)
                {
                    pushed.RemoveWhere(filename => !refs.Contains(filename, StringComparer.OrdinalIgnoreCase));
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Agent] [{Cam}] 스캔 중 오류", cameraId);
            }

            await Task.Delay(200, ct);
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Login With Retry Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the login with retry async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="ct">
    /// \if KO
    /// <para>취소 요청을 감시하는 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Login With Retry Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the login with retry async operation.</para>
    /// \endif
    /// </returns>
    private async Task LoginWithRetryAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _api.Token is null)
        {
            var (ok, err) = await _api.LoginWithStoredCredentialsAsync();
            if (ok) { _logger.LogInformation("[Agent] 로그인 성공."); return; }
            _logger.LogWarning("[Agent] 로그인 실패: {Err}. 10초 후 재시도.", err);
            await Task.Delay(TimeSpan.FromSeconds(10), ct);
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Ensure Logged In Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the ensure logged in async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="ct">
    /// \if KO
    /// <para>취소 요청을 감시하는 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Ensure Logged In Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the ensure logged in async operation.</para>
    /// \endif
    /// </returns>
    private async Task EnsureLoggedInAsync(CancellationToken ct)
    {
        if (_api.Token is not null)
        {
            return;
        }

        await _loginLock.WaitAsync(ct);
        try
        {
            if (_api.Token is not null)
            {
                return;
            }

            await LoginWithRetryAsync(ct);
        }
        finally
        {
            _loginLock.Release();
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Sync Cameras Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the sync cameras async operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Sync Cameras Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the sync cameras async operation.</para>
    /// \endif
    /// </returns>
    private async Task SyncCamerasAsync()
    {
        var localCams = _repository.GetAll()
            .Where(c => c.Enabled && !c.IsDirectHls)
            .Select(c => new AgentCameraInfo
            {
                Id = c.Id, Name = c.Name, Host = c.Host,
                RtspUrl = c.RtspUrl, AutoReconnect = c.AutoReconnect, IsPublic = c.IsPublic
            }).ToList();

        var ok = await _api.SyncCamerasAsync(localCams);
        _logger.LogInformation("[Agent] 카메라 {Count}개 서버 동기화 {Result}.",
            localCams.Count, ok ? "성공" : "실패");
    }

    /// <summary>
    /// \if KO
    /// <para>Push File Async 작업을 시도하고 성공 여부를 반환합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Attempts to push file async and returns whether the operation succeeds.</para>
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
    /// <param name="filePath">
    /// \if KO
    /// <para>file Path에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for file path.</para>
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
    /// <returns>
    /// \if KO
    /// <para>Try Push File Async 작업에서 생성한 <c>Task&lt;bool&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;bool&gt;</c> result produced by the try push file async operation.</para>
    /// \endif
    /// </returns>
    private async Task<bool> TryPushFileAsync(string cameraId, string filePath, string filename)
    {
        try
        {
            var data = await File.ReadAllBytesAsync(filePath);
            return await _api.PushSegmentAsync(cameraId, filename, data);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Agent] [{Cam}] 세그먼트 파일 읽기 실패: {File}", cameraId, filename);
            return false;
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Parse Segments From M3u8 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the parse segments from m3u8 operation.</para>
    /// \endif
    /// </summary>
    /// <param name="bytes">
    /// \if KO
    /// <para>bytes에 사용할 <c>byte[]</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>byte[]</c> value used for bytes.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Parse Segments From M3u8 작업에서 생성한 <c>List&lt;string&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>List&lt;string&gt;</c> result produced by the parse segments from m3u8 operation.</para>
    /// \endif
    /// </returns>
    private static List<string> ParseSegmentsFromM3u8(byte[] bytes)
    {
        try
        {
            return System.Text.Encoding.UTF8.GetString(bytes)
                .Split('\n')
                .Where(l => l.TrimEnd().EndsWith(".ts", StringComparison.OrdinalIgnoreCase))
                .Select(l => Path.GetFileName(l.Trim()))
                .Where(f => !string.IsNullOrEmpty(f))
                .ToList()!;
        }
        catch { return []; }
    }

    /// <summary>
    /// \if KO
    /// <para>Output Root 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the output root value.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Get Output Root 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the get output root operation.</para>
    /// \endif
    /// </returns>
    private string GetOutputRoot()
    {
        var root = _config["Ffmpeg:OutputRoot"] ?? "hls";
        return Path.IsPathRooted(root) ? root : Path.Combine(AppContext.BaseDirectory, root);
    }
}
