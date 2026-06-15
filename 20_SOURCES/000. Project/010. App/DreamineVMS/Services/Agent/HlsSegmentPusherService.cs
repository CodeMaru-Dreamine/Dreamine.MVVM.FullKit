using DreamineVMS.Services.Cameras;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;

namespace DreamineVMS.Services.Agent;

/// <summary>
/// 에이전트 모드: 자동 로그인 → 로컬 카메라를 서버에 등록 → HLS 세그먼트 업로드.
/// 카메라별로 독립 루프를 병렬 실행하여 한 카메라 업로드가 다른 카메라를 블로킹하지 않음.
/// </summary>
public sealed class HlsSegmentPusherService : BackgroundService
{
    private readonly AgentApiClient _api;
    private readonly IVmsCameraRepository _repository;
    private readonly IConfiguration _config;
    private readonly ILogger<HlsSegmentPusherService> _logger;
    private readonly SemaphoreSlim _loginLock = new(1, 1);

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

    private string GetOutputRoot()
    {
        var root = _config["Ffmpeg:OutputRoot"] ?? "hls";
        return Path.IsPathRooted(root) ? root : Path.Combine(AppContext.BaseDirectory, root);
    }
}
