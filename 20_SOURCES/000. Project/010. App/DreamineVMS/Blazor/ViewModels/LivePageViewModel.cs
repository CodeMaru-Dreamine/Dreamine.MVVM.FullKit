using DreamineVMS.Models;
using DreamineVMS.Services.Cameras;
using DreamineVMS.Services.Runtime;
using Microsoft.JSInterop;

namespace DreamineVMS.Blazor.ViewModels;

/// <summary>
/// \brief Live Blazor 페이지의 ViewModel입니다.
/// </summary>
/// <remarks>
/// FFmpeg HLS 스트림의 시작/재시작은 BackgroundService가 담당하고,
/// 이 ViewModel은 이미 생성된 HLS 스트림을 화면에 연결하고
/// 카메라가 Stop→Start 사이클을 겪을 때 hls.js를 다시 초기화하는 책임을 갖습니다.
/// </remarks>
public sealed class LivePageViewModel : IAsyncDisposable
{
    private readonly IVmsCameraRepository _repository;
    private readonly ICameraRuntimeStateService _runtimeState;
    private readonly IJSRuntime _jsRuntime;
    private readonly Dictionary<string, CameraConnectionState> _lastSeenStates = new();
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

            foreach (CameraDevice camera in Cameras)
            {
                CameraRuntimeState state = _runtimeState.GetState(camera.Id);
                _lastSeenStates[camera.Id] = state.State;
            }

            _runtimeState.StateChanged += OnRuntimeStateChanged;
            _isInitialized = true;
        }
    }

    /// <summary>
    /// \brief Video DOM이 생성된 뒤 hls.js 플레이어를 연결합니다.
    /// </summary>
    /// <param name="firstRender">첫 렌더링 여부입니다.</param>
    public async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        foreach (CameraDevice camera in Cameras)
        {
            await InitPlayerAsync(camera);
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
            }
        }

        foreach (CameraDevice camera in Cameras)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync(
                    "dreamineVmsHls.destroy",
                    GetPlayerId(camera));
            }
            catch
            {
                // 화면 종료 중 JS 런타임이 이미 끊긴 경우는 무시합니다.
            }
        }
    }

    private async Task InitPlayerAsync(CameraDevice camera)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync(
                "dreamineVmsHls.init",
                GetPlayerId(camera),
                camera.HlsUrl);
        }
        catch (JSDisconnectedException)
        {
            // Circuit이 끊긴 상태는 무시합니다.
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LivePageViewModel] init failed: {camera.Id}: {ex}");
        }
    }

    private void OnRuntimeStateChanged(object? sender, CameraRuntimeState state)
    {
        // 화면에 표시하는 카메라만 관심 대상.
        if (Cameras.All(c => c.Id != state.CameraId))
        {
            return;
        }

        CameraConnectionState previous;
        bool transitionedToConnected;

        lock (_gate)
        {
            if (_isDisposed)
            {
                return;
            }

            previous = _lastSeenStates.GetValueOrDefault(state.CameraId, CameraConnectionState.Disconnected);
            _lastSeenStates[state.CameraId] = state.State;

            // "Disconnected/Faulted/Connecting → Connected" 전환에서만 init 재호출.
            // Connected에서 Connected로 가는 노이즈는 무시.
            transitionedToConnected =
                previous != CameraConnectionState.Connected &&
                state.State == CameraConnectionState.Connected;
        }

        if (!transitionedToConnected)
        {
            return;
        }

        CameraDevice? camera = Cameras.FirstOrDefault(c => c.Id == state.CameraId);
        if (camera is null)
        {
            return;
        }

        // 비동기 호출은 락 밖에서.
        _ = ReinitializePlayerAsync(camera);
    }

    private async Task ReinitializePlayerAsync(CameraDevice camera)
    {
        // Connected 직후에는 m3u8가 아직 초기 세그먼트를 다 갖추지 못한 상태일 수 있습니다.
        // 짧게 대기 후 init을 호출합니다. (hls-interop.js의 waitUntilReady가 이중 안전망)
        await Task.Delay(500);
        await InitPlayerAsync(camera);
    }
}
