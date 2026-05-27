using DreamineVMS.Models;
using DreamineVMS.Services.Cameras;
using DreamineVMS.Services.Runtime;
using Microsoft.JSInterop;

namespace DreamineVMS.Blazor.ViewModels;

/// <summary>
/// \brief Live Blazor 페이지의 ViewModel입니다.
/// </summary>
/// <remarks>
/// FFmpeg HLS 스트림의 시작/재시작은 BackgroundService가 담당합니다.
/// 이 ViewModel은 RuntimeState와 video element를 동기화합니다.
/// StopAll → StartAll 사이클에서 기존 hls.js 인스턴스를 제거하고,
/// 다시 Connected/Connecting 상태가 되면 안전하게 재연결합니다.
/// </remarks>
public sealed class LivePageViewModel : IAsyncDisposable
{
    private readonly IVmsCameraRepository _repository;
    private readonly ICameraRuntimeStateService _runtimeState;
    private readonly IJSRuntime _jsRuntime;
    private readonly object _gate = new();
    private bool _isInitialized;
    private bool _isDisposed;

    /// <summary>
    /// \brief LivePageViewModel 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="repository">카메라 Repository입니다.</param>
    /// <param name="runtimeState">카메라 런타임 상태 서비스입니다.</param>
    /// <param name="jsRuntime">Blazor JSRuntime입니다.</param>
    public LivePageViewModel(
        IVmsCameraRepository repository,
        ICameraRuntimeStateService runtimeState,
        IJSRuntime jsRuntime)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _runtimeState = runtimeState ?? throw new ArgumentNullException(nameof(runtimeState));
        _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
    }

    /// <summary>
    /// \brief RuntimeState 변경으로 player 동기화가 필요할 때 발생합니다.
    /// </summary>
    public event EventHandler? PlayersNeedSync;

    /// <summary>
    /// \brief Live 화면에 표시할 카메라 목록입니다.
    /// </summary>
    public IReadOnlyList<CameraDevice> Cameras { get; private set; } = Array.Empty<CameraDevice>();

    /// <summary>
    /// \brief Live 화면 진입 시 카메라 목록을 로드하고 RuntimeState 변경을 구독합니다.
    /// </summary>
    public void Initialize()
    {
        lock (_gate)
        {
            if (_isInitialized || _isDisposed)
            {
                return;
            }

            Cameras = _repository.GetAll()
                .Where(camera => camera.Enabled)
                .OrderBy(camera => camera.DisplayOrder)
                .Take(5)
                .ToArray();

            _runtimeState.StateChanged += OnRuntimeStateChanged;
            _isInitialized = true;
        }
    }

    /// <summary>
    /// \brief Video DOM이 생성된 뒤 player 상태를 RuntimeState와 동기화합니다.
    /// </summary>
    /// <param name="firstRender">첫 렌더링 여부입니다.</param>
    /// <returns>비동기 작업입니다.</returns>
    public async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        await SynchronizePlayersAsync();
    }

    /// <summary>
    /// \brief 현재 RuntimeState에 맞춰 hls.js player를 생성 또는 제거합니다.
    /// </summary>
    /// <returns>비동기 작업입니다.</returns>
    public async Task SynchronizePlayersAsync()
    {
        if (_isDisposed)
        {
            return;
        }

        foreach (CameraDevice camera in Cameras)
        {
            CameraRuntimeState state = _runtimeState.GetState(camera.Id);

            if (state.State is CameraConnectionState.Connected or CameraConnectionState.Connecting)
            {
                await EnsurePlayerAsync(camera);
                continue;
            }

            await DestroyPlayerAsync(camera, state.LastMessage);
        }
    }

    /// <summary>
    /// \brief Live 화면에서 사용하는 video element ID를 반환합니다.
    /// </summary>
    /// <param name="camera">카메라 정보입니다.</param>
    /// <returns>HTML video element ID입니다.</returns>
    public static string GetPlayerId(CameraDevice camera)
    {
        ArgumentNullException.ThrowIfNull(camera);
        return $"video-{camera.Id}";
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        lock (_gate)
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            if (_isInitialized)
            {
                _runtimeState.StateChanged -= OnRuntimeStateChanged;
                _isInitialized = false;
            }
        }

        foreach (CameraDevice camera in Cameras)
        {
            await DestroyPlayerAsync(camera, "Live view disposed.");
        }
    }


    private static string BuildVersionedHlsUrl(CameraDevice camera, CameraRuntimeState state)
    {
        ArgumentNullException.ThrowIfNull(camera);
        ArgumentNullException.ThrowIfNull(state);

        string separator = camera.HlsUrl.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return $"{camera.HlsUrl}{separator}session={state.RestartCount}";
    }

    private async Task EnsurePlayerAsync(CameraDevice camera)
    {
        try
        {
            CameraRuntimeState state = _runtimeState.GetState(camera.Id);
            string source = BuildVersionedHlsUrl(camera, state);

            await _jsRuntime.InvokeVoidAsync(
                "dreamineVmsHls.init",
                GetPlayerId(camera),
                source);
        }
        catch (JSDisconnectedException)
        {
            // Circuit이 끊긴 상태는 무시합니다.
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LivePageViewModel] player init failed: {camera.Id}: {ex}");
        }
    }

    private async Task DestroyPlayerAsync(CameraDevice camera, string? reason)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync(
                "dreamineVmsHls.destroy",
                GetPlayerId(camera),
                string.IsNullOrWhiteSpace(reason) ? "Stream stopped." : reason);
        }
        catch (JSDisconnectedException)
        {
            // Circuit이 끊긴 상태는 무시합니다.
        }
        catch
        {
            // 화면 종료 중 JS 런타임이 이미 끊긴 경우는 무시합니다.
        }
    }

    private void OnRuntimeStateChanged(object? sender, CameraRuntimeState state)
    {
        if (Cameras.All(c => c.Id != state.CameraId))
        {
            return;
        }

        if (_isDisposed)
        {
            return;
        }

        PlayersNeedSync?.Invoke(this, EventArgs.Empty);
    }
}
