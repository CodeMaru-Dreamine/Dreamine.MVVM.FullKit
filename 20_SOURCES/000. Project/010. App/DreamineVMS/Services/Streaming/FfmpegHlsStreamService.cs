using DreamineVMS.Models;
using DreamineVMS.Options;
using DreamineVMS.Services.Cameras;
using DreamineVMS.Services.Runtime;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace DreamineVMS.Services.Streaming;

/// <summary>
/// \brief FFmpeg 프로세스를 사용해서 RTSP 스트림을 HLS 스트림으로 변환하는 서비스입니다.
/// </summary>
public sealed class FfmpegHlsStreamService : BackgroundService, ICameraStreamService
{
    private static readonly Regex MediaSeqRegex = new(@"#EXT-X-MEDIA-SEQUENCE:(\d+)", RegexOptions.Compiled);

    /// <summary>\brief ffmpeg에 'q'를 보내고 graceful exit를 기다리는 최대 시간입니다.</summary>
    private static readonly TimeSpan GracefulExitTimeout = TimeSpan.FromMilliseconds(800);

    /// <summary>\brief Kill 이후 프로세스 종료를 기다리는 최대 시간입니다.</summary>
    private static readonly TimeSpan KillWaitTimeout = TimeSpan.FromSeconds(2);

    private readonly ILogger<FfmpegHlsStreamService> _logger;
    private readonly IVmsCameraRepository _repository;
    private readonly ICameraRuntimeStateService _runtimeState;
    private readonly FfmpegOptions _options;
    private readonly Dictionary<string, Process> _processes = new();
    private readonly Dictionary<string, long> _lastSequences = new();
    private readonly Dictionary<string, DateTime> _lastWrites = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _cameraOperationLocks = new();

    /// <summary>
    /// \brief 의도된 정지(StopAsync / Restart)로 인해 곧 종료될 프로세스의 카메라 ID 집합입니다.
    /// process.Exited 이벤트 핸들러는 이 집합에 포함된 카메라에 대해서는
    /// Faulted 상태를 덮어쓰지 않습니다.
    /// </summary>
    private readonly ConcurrentDictionary<string, byte> _intentionalStops = new();

    private readonly SemaphoreSlim _sync = new(1, 1);

    /// <summary>
    /// \brief 자식 ffmpeg 프로세스를 부모와 묶는 Windows Job Object입니다.
    /// </summary>
    private readonly ChildProcessJob? _processJob =
        OperatingSystem.IsWindows() ? new ChildProcessJob() : null;

    private bool _disposed;

    /// <summary>
    /// \brief FfmpegHlsStreamService 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="logger">로거입니다.</param>
    /// <param name="repository">카메라 저장소입니다.</param>
    /// <param name="runtimeState">카메라 런타임 상태 서비스입니다.</param>
    /// <param name="options">FFmpeg 옵션입니다.</param>
    public FfmpegHlsStreamService(
        ILogger<FfmpegHlsStreamService> logger,
        IVmsCameraRepository repository,
        ICameraRuntimeStateService runtimeState,
        IOptions<FfmpegOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _runtimeState = runtimeState ?? throw new ArgumentNullException(nameof(runtimeState));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Directory.CreateDirectory(GetOutputRoot());

        if (_options.StartOnApplicationStartup)
        {
            await StartAllAsync(stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await WatchdogAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            try
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(Math.Max(1, _options.WatchdogIntervalSeconds)),
                    stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    /// <inheritdoc />
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await StopAllAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
        _processJob?.Dispose();
    }

    /// <summary>
    /// \brief 서비스 Dispose 시 남은 ffmpeg 프로세스와 Job Object를 정리합니다.
    /// </summary>
    public override void Dispose()
    {
        if (_disposed)
        {
            base.Dispose();
            return;
        }

        _disposed = true;

        bool acquired = false;
        try
        {
            acquired = _sync.Wait(TimeSpan.FromSeconds(2));

            foreach (string cameraId in _processes.Keys.ToArray())
            {
                _intentionalStops.TryAdd(cameraId, 0);
                ForceKillProcessUnsafe(cameraId);
            }
        }
        catch
        {
            // 정리 단계의 예외는 무시합니다.
        }
        finally
        {
            if (acquired)
            {
                _sync.Release();
            }
        }

        foreach (SemaphoreSlim cameraLock in _cameraOperationLocks.Values)
        {
            try { cameraLock.Dispose(); } catch { }
        }
        _cameraOperationLocks.Clear();

        _processJob?.Dispose();
        _sync.Dispose();

        base.Dispose();
    }

    /// <inheritdoc />
    public async Task StartAllAsync(CancellationToken cancellationToken = default)
    {
        // 직렬로 시작합니다. 시작 자체는 빠르고, FFmpeg 동시 spawn으로 인한
        // 초기 burst를 줄이는 효과가 있습니다.
        foreach (CameraDevice camera in _repository.GetAll().Where(camera => camera.Enabled && !camera.IsDirectHls))
        {
            await StartAsync(camera.Id, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task StopAllAsync(CancellationToken cancellationToken = default)
    {
        // 카메라별 Stop을 병렬 실행합니다. 각 카메라의 Kill/WaitForExitAsync는
        // 비동기이므로 N대 정지가 1대 정지와 거의 같은 시간 안에 완료됩니다.
        CameraDevice[] cameras = _repository.GetAll().ToArray();
        Task[] tasks = cameras
            .Select(camera => StopAsync(camera.Id, cancellationToken))
            .ToArray();

        await Task.WhenAll(tasks);
    }

    /// <inheritdoc />
    public async Task StartAsync(string cameraId, CancellationToken cancellationToken = default)
    {
        SemaphoreSlim cameraLock = GetCameraOperationLock(cameraId);
        await cameraLock.WaitAsync(cancellationToken);
        try
        {
            await StartInternalAsync(cameraId, cancellationToken);
        }
        finally
        {
            cameraLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task StopAsync(string cameraId, CancellationToken cancellationToken = default)
    {
        SemaphoreSlim cameraLock = GetCameraOperationLock(cameraId);
        await cameraLock.WaitAsync(cancellationToken);
        try
        {
            await StopInternalAsync(cameraId, cancellationToken);
        }
        finally
        {
            cameraLock.Release();
        }
    }

    private async Task StartInternalAsync(string cameraId, CancellationToken cancellationToken)
    {
        CameraDevice? camera = _repository.GetAll().FirstOrDefault(item => item.Id == cameraId);
        if (camera is null)
        {
            throw new InvalidOperationException($"Camera '{cameraId}' was not found.");
        }

        // StartAll 직전 StopAll이 눌렸거나 빠르게 Stop→Start가 반복되는 경우를 위해
        // 같은 카메라의 Stop/Start는 cameraOperationLock으로 직렬화합니다.
        Process? toStop = null;
        await _sync.WaitAsync(cancellationToken);
        try
        {
            if (_processes.TryGetValue(camera.Id, out Process? existing))
            {
                if (!existing.HasExited)
                {
                    _intentionalStops.TryAdd(camera.Id, 0);
                    _processes.Remove(camera.Id);
                    toStop = existing;
                }
                else
                {
                    _processes.Remove(camera.Id);
                    try { existing.Dispose(); } catch { }
                }
            }
        }
        finally
        {
            _sync.Release();
        }

        if (toStop is not null)
        {
            _runtimeState.SetState(camera.Id, CameraConnectionState.Disconnected, $"Restarting camera: {camera.Name}");
            await Task.Delay(TimeSpan.FromMilliseconds(700), cancellationToken);
            await ShutdownProcessAsync(camera.Id, toStop, cancellationToken);
            CleanupOldSegmentFiles(camera.Id, TimeSpan.FromMinutes(30));
        }

        await _sync.WaitAsync(cancellationToken);
        try
        {
            _intentionalStops.TryRemove(camera.Id, out _);
            _lastSequences.Remove(camera.Id);
            _lastWrites.Remove(camera.Id);
            StartProcessUnsafe(camera);
        }
        finally
        {
            _sync.Release();
        }
    }

    private async Task StopInternalAsync(string cameraId, CancellationToken cancellationToken)
    {
        Process? toStop = null;
        await _sync.WaitAsync(cancellationToken);
        try
        {
            if (_processes.TryGetValue(cameraId, out toStop))
            {
                _intentionalStops.TryAdd(cameraId, 0);
                _processes.Remove(cameraId);
            }
        }
        finally
        {
            _sync.Release();
        }

        if (toStop is not null)
        {
            _runtimeState.SetState(cameraId, CameraConnectionState.Disconnected, $"Stopping camera: {cameraId}");
            await Task.Delay(TimeSpan.FromMilliseconds(700), cancellationToken);
            await ShutdownProcessAsync(cameraId, toStop, cancellationToken);
            CleanupOldSegmentFiles(cameraId, TimeSpan.FromMinutes(30));
        }

        await _sync.WaitAsync(cancellationToken);
        try
        {
            _lastSequences.Remove(cameraId);
            _lastWrites.Remove(cameraId);
        }
        finally
        {
            _sync.Release();
        }

        _runtimeState.SetState(cameraId, CameraConnectionState.Disconnected, $"Camera stopped: {cameraId}");
        _intentionalStops.TryRemove(cameraId, out _);
    }

    private async Task WatchdogAsync(CancellationToken cancellationToken)
    {
        foreach (CameraDevice camera in _repository.GetAll().Where(camera => camera.Enabled && camera.AutoReconnect && !camera.IsDirectHls))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            // 의도적 정지 진행 중인 카메라는 자동 재시작 대상에서 제외합니다.
            if (_intentionalStops.ContainsKey(camera.Id))
            {
                continue;
            }

            Process? deadProcess = null;
            bool needRestart = false;

            await _sync.WaitAsync(cancellationToken);
            try
            {
                if (!_processes.TryGetValue(camera.Id, out Process? process) || process.HasExited)
                {
                    if (_processes.ContainsKey(camera.Id))
                    {
                        _runtimeState.IncrementRestart(camera.Id, $"FFmpeg process exited. Restarting: {camera.Name}");
                        deadProcess = _processes[camera.Id];
                        _processes.Remove(camera.Id);
                        needRestart = true;
                    }
                    else
                    {
                        // 한 번도 시작 안 됐거나 깨끗하게 정지된 카메라는 watchdog 대상 아님.
                        continue;
                    }
                }
                else
                {
                    string m3u8Path = GetPlaylistPath(camera.Id);
                    if (!File.Exists(m3u8Path))
                    {
                        continue;
                    }

                    (long sequence, DateTime writeTime) = ReadSequenceAndWriteTime(m3u8Path);
                    long lastSequence = _lastSequences.GetValueOrDefault(camera.Id, -1);
                    DateTime lastWrite = _lastWrites.GetValueOrDefault(camera.Id, DateTime.MinValue);

                    if (sequence > lastSequence || writeTime > lastWrite)
                    {
                        _lastSequences[camera.Id] = Math.Max(sequence, lastSequence);
                        _lastWrites[camera.Id] = writeTime > lastWrite ? writeTime : lastWrite;
                        _runtimeState.SetState(camera.Id, CameraConnectionState.Connected, $"HLS updated: {camera.Name}");
                        continue;
                    }

                    double idleSeconds = (DateTime.UtcNow - lastWrite.ToUniversalTime()).TotalSeconds;
                    int idleLimit = Math.Max(
                        _options.WatchdogIdleSeconds > 0 ? _options.WatchdogIdleSeconds : 6,
                        _options.HlsSegmentSeconds * _options.HlsListSize);

                    if (idleSeconds >= idleLimit)
                    {
                        _runtimeState.IncrementRestart(camera.Id, $"HLS idle timeout. Restarting: {camera.Name}");
                        deadProcess = process;
                        _processes.Remove(camera.Id);
                        needRestart = true;
                    }
                }
            }
            finally
            {
                _sync.Release();
            }

            if (deadProcess is not null)
            {
                await ShutdownProcessAsync(camera.Id, deadProcess, cancellationToken);
            }

            if (needRestart)
            {
                try
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(Math.Max(0, _options.RestartDelaySeconds)),
                        cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                await _sync.WaitAsync(cancellationToken);
                try
                {
                    StartProcessUnsafe(camera);
                }
                finally
                {
                    _sync.Release();
                }
            }
        }
    }

    private void StartProcessUnsafe(CameraDevice camera)
    {
        if (camera.IsDirectHls)
        {
            _runtimeState.SetState(camera.Id, CameraConnectionState.Connected, $"Direct HLS: {camera.Name}");
            return;
        }

        if (string.IsNullOrWhiteSpace(camera.RtspUrl) || camera.RtspUrl.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
        {
            _runtimeState.SetState(camera.Id, CameraConnectionState.Faulted, $"RTSP URL is not configured: {camera.Name}");
            return;
        }

        if (!File.Exists(_options.Path))
        {
            _runtimeState.SetState(camera.Id, CameraConnectionState.Faulted, $"ffmpeg.exe was not found: {_options.Path}");
            return;
        }

        Directory.CreateDirectory(GetCameraOutputDirectory(camera.Id));
        PreparePlaylistForNewSession(camera.Id);
        CleanupOldSegmentFiles(camera.Id, TimeSpan.FromMinutes(30));

        string arguments = BuildArguments(camera);
        ProcessStartInfo startInfo = new()
        {
            FileName = _options.Path,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            RedirectStandardInput = true   // graceful 'q' 종료를 위해 추가
        };

        Process process = new() { StartInfo = startInfo, EnableRaisingEvents = true };
        process.Exited += (_, _) =>
        {
            // 의도된 정지(StopAsync / Restart)에 의한 종료는 Faulted로 표시하지 않습니다.
            if (_intentionalStops.ContainsKey(camera.Id))
            {
                return;
            }

            _runtimeState.SetState(camera.Id, CameraConnectionState.Faulted, $"FFmpeg exited: {camera.Name}");
        };

        process.ErrorDataReceived += (_, args) =>
        {
            if (string.IsNullOrWhiteSpace(args.Data))
            {
                return;
            }

            _logger.LogWarning("[ffmpeg:{CameraId}] {Line}", camera.Id, args.Data);
        };
        process.OutputDataReceived += (_, _) => { /* stdout은 사용하지 않습니다. */ };

        try
        {
            _runtimeState.SetState(camera.Id, CameraConnectionState.Connecting, $"Starting HLS: {camera.Name}");
            process.Start();

            _processJob?.AddProcess(process);

            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
            _processes[camera.Id] = process;
            InitWatchStats(camera.Id);
            _logger.LogInformation("Started FFmpeg HLS for camera {CameraId}.", camera.Id);
        }
        catch (Exception ex)
        {
            _runtimeState.SetState(camera.Id, CameraConnectionState.Faulted, $"Failed to start FFmpeg: {camera.Name}", ex.Message);
            _logger.LogError(ex, "Failed to start FFmpeg for camera {CameraId}.", camera.Id);
            process.Dispose();
        }
    }

    /// <summary>
    /// \brief ffmpeg 프로세스를 graceful → kill 순서로 비동기 종료합니다.
    /// </summary>
    /// <param name="cameraId">카메라 ID입니다.</param>
    /// <param name="process">종료할 프로세스입니다.</param>
    /// <param name="cancellationToken">취소 토큰입니다.</param>
    /// <returns>비동기 작업입니다.</returns>
    private async Task ShutdownProcessAsync(string cameraId, Process process, CancellationToken cancellationToken)
    {
        try
        {
            if (process.HasExited)
            {
                return;
            }

            // 1) stdin에 'q'를 보내 ffmpeg가 깨끗하게 종료할 기회를 줍니다.
            //    파일이 깨지지 않게 마지막 세그먼트를 flush하고 빠져나옵니다.
            bool gracefulOk = false;
            try
            {
                if (process.StartInfo.RedirectStandardInput && !process.HasExited)
                {
                    await process.StandardInput.WriteAsync('q');
                    await process.StandardInput.FlushAsync(cancellationToken);
                    process.StandardInput.Close();
                }

                using CancellationTokenSource gracefulCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                gracefulCts.CancelAfter(GracefulExitTimeout);

                try
                {
                    await process.WaitForExitAsync(gracefulCts.Token);
                    gracefulOk = true;
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    // graceful timeout — Kill로 폴백.
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Graceful stop attempt failed for {CameraId}.", cameraId);
            }

            // 2) 아직 살아있으면 강제 종료.
            if (!gracefulOk && !process.HasExited)
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Kill failed for {CameraId}.", cameraId);
                }

                try
                {
                    using CancellationTokenSource killCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    killCts.CancelAfter(KillWaitTimeout);
                    await process.WaitForExitAsync(killCts.Token);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("FFmpeg did not exit within timeout for {CameraId}.", cameraId);
                }
            }
        }
        finally
        {
            try { process.Dispose(); } catch { /* ignore */ }
        }
    }

    /// <summary>
    /// \brief Dispose 등 비동기 호출이 불가능한 시점에 사용하는 동기 강제 종료입니다.
    /// </summary>
    /// <param name="cameraId">카메라 ID입니다.</param>
    private void ForceKillProcessUnsafe(string cameraId)
    {
        if (!_processes.TryGetValue(cameraId, out Process? process))
        {
            return;
        }

        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(1000);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to force-kill FFmpeg process for {CameraId}.", cameraId);
        }
        finally
        {
            try { process.Dispose(); } catch { /* ignore */ }
            _processes.Remove(cameraId);
        }
    }


    private SemaphoreSlim GetCameraOperationLock(string cameraId)
    {
        return _cameraOperationLocks.GetOrAdd(cameraId, _ => new SemaphoreSlim(1, 1));
    }

    private string BuildArguments(CameraDevice camera)
    {
        string playlistPath = Quote(GetPlaylistPath(camera.Id));
        string sessionStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(System.Globalization.CultureInfo.InvariantCulture);
        string segmentPath = Quote(Path.Combine(GetCameraOutputDirectory(camera.Id), $"seg_{sessionStamp}_%05d.ts"));
        string input = Quote(camera.RtspUrl);
        string videoCodec = (_options.VideoCodec ?? "libx264").ToLowerInvariant();
        string audioCodec = (_options.AudioCodec ?? "an").ToLowerInvariant();
        int segmentSeconds = Math.Max(1, _options.HlsSegmentSeconds);
        int listSize = Math.Max(4, _options.HlsListSize);
        int videoFps = Math.Max(1, _options.VideoFps);
        int keyFrameInterval = segmentSeconds * videoFps;

        List<string> parts = new()
        {
            "-hide_banner",
            "-loglevel warning",
            "-rtsp_transport tcp",
            "-rtsp_flags prefer_tcp",
            "-analyzeduration 2000000",
            "-probesize 2000000",
            "-fflags +genpts+discardcorrupt+nobuffer",
            "-flags low_delay",
            "-i", input,
            "-map 0:v:0",
            "-map 0:a?",
            "-dn",
            "-sn"
        };

        if (videoCodec == "copy")
        {
            parts.Add("-c:v copy");
        }
        else
        {
            parts.Add("-c:v libx264");
            parts.Add("-preset ultrafast");
            parts.Add("-tune zerolatency");
            parts.Add("-profile:v baseline");
            parts.Add("-level:v 3.1");
            parts.Add("-pix_fmt yuv420p");
            if (_options.VideoMaxWidth > 0)
            {
                parts.Add($"-vf {Quote($"scale='min({_options.VideoMaxWidth},iw)':-2,fps={videoFps}")}");
            }
            else
            {
                parts.Add($"-r {videoFps}");
            }
            parts.Add($"-b:v {_options.VideoBitrate}");
            parts.Add($"-maxrate {_options.VideoMaxRate}");
            parts.Add($"-bufsize {_options.VideoBufferSize}");
            parts.Add($"-g {keyFrameInterval}");
            parts.Add($"-keyint_min {keyFrameInterval}");
            parts.Add("-sc_threshold 0");
            parts.Add("-bf 0");
            parts.Add($"-force_key_frames {Quote($"expr:gte(t,n_forced*{segmentSeconds})")}");
        }

        if (audioCodec == "an")
        {
            parts.Add("-an");
        }
        else
        {
            parts.Add($"-c:a {audioCodec}");
            parts.Add($"-b:a {_options.AudioBitrate}");
            parts.Add($"-ar {_options.AudioRate}");
            parts.Add($"-ac {_options.AudioChannels}");
        }

        parts.Add("-muxdelay 0");
        parts.Add("-muxpreload 0");
        parts.Add("-f hls");
        parts.Add("-hls_segment_type mpegts");
        parts.Add("-hls_allow_cache 0");
        parts.Add($"-hls_time {segmentSeconds}");
        parts.Add($"-hls_list_size {listSize}");
        // Windows + WebView2 환경에서는 video 태그가 .ts 파일 핸들을 오래 잡는 경우가 있습니다.
        // delete_segments를 사용하면 ffmpeg가 WebView2가 읽는 세그먼트를 삭제하려다
        // Permission denied가 발생하고 playlist/segment 수명이 꼬일 수 있습니다.
        // 실행 중 삭제는 하지 않고, 세션별 고유 파일명 + 지연 정리 방식으로 안정화합니다.
        parts.Add("-hls_flags omit_endlist+independent_segments+temp_file");
        parts.Add($"-hls_segment_filename {segmentPath}");
        parts.Add(playlistPath);

        return string.Join(' ', parts);
    }

    private void InitWatchStats(string cameraId)
    {
        string path = GetPlaylistPath(cameraId);
        if (File.Exists(path))
        {
            (long sequence, DateTime writeTime) = ReadSequenceAndWriteTime(path);
            _lastSequences[cameraId] = sequence;
            _lastWrites[cameraId] = writeTime;
        }
        else
        {
            _lastSequences[cameraId] = -1;
            _lastWrites[cameraId] = DateTime.UtcNow;
        }
    }

    private static (long sequence, DateTime writeTime) ReadSequenceAndWriteTime(string path)
    {
        DateTime writeTime = File.GetLastWriteTimeUtc(path);
        try
        {
            string text = File.ReadAllText(path);
            Match match = MediaSeqRegex.Match(text);
            long sequence = match.Success && long.TryParse(match.Groups[1].Value, out long value) ? value : -1;
            return (sequence, writeTime);
        }
        catch
        {
            return (-1, writeTime);
        }
    }

    private string GetOutputRoot()
    {
        return Path.IsPathRooted(_options.OutputRoot)
            ? _options.OutputRoot
            : Path.Combine(AppContext.BaseDirectory, _options.OutputRoot);
    }

    private string GetCameraOutputDirectory(string cameraId)
    {
        return Path.Combine(GetOutputRoot(), cameraId);
    }

    private string GetPlaylistPath(string cameraId)
    {
        return Path.Combine(GetCameraOutputDirectory(cameraId), "index.m3u8");
    }

    private void PreparePlaylistForNewSession(string cameraId)
    {
        string directory = GetCameraOutputDirectory(cameraId);
        Directory.CreateDirectory(directory);

        string playlistPath = GetPlaylistPath(cameraId);
        string preparingPlaylist = string.Join(Environment.NewLine,
            "#EXTM3U",
            "#EXT-X-VERSION:3",
            "#EXT-X-TARGETDURATION:1",
            "#EXT-X-MEDIA-SEQUENCE:0",
            string.Empty);

        try
        {
            File.WriteAllText(playlistPath, preparingPlaylist);
        }
        catch (IOException ex)
        {
            _logger.LogDebug(ex, "Playlist reset skipped because the file is currently locked: {CameraId}.", cameraId);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogDebug(ex, "Playlist reset skipped because access was denied: {CameraId}.", cameraId);
        }
    }

    private void CleanupOldSegmentFiles(string cameraId, TimeSpan minimumAge)
    {
        string directory = GetCameraOutputDirectory(cameraId);
        if (!Directory.Exists(directory))
        {
            return;
        }

        DateTime thresholdUtc = DateTime.UtcNow - minimumAge;

        foreach (string file in Directory.EnumerateFiles(directory, "seg_*.ts"))
        {
            try
            {
                if (File.GetLastWriteTimeUtc(file) > thresholdUtc)
                {
                    continue;
                }

                File.Delete(file);
            }
            catch (IOException)
            {
                // WebView2/Chrome이 읽는 중인 세그먼트는 다음 정리 주기에 다시 시도합니다.
            }
            catch (UnauthorizedAccessException)
            {
                // 파일 핸들 또는 백신/인덱서 점유 가능성이 있으므로 무시합니다.
            }
        }
    }

    private static string Quote(string value)
    {
        return $"\"{value}\"";
    }
}
