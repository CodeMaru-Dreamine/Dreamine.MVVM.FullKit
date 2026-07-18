using DreamineVMS.Models;
using DreamineVMS.Services.Cameras;
using DreamineVMS.Services.Runtime;
using Microsoft.JSInterop;

namespace DreamineVMS.Blazor.ViewModels;

/// <summary>
/// \if KO
/// <para>\brief Live Blazor 페이지의 ViewModel입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates live page view model functionality and related state.</para>
/// \endif
/// </summary>
/// <remarks>
/// \if KO
/// <para>FFmpeg HLS 스트림의 시작/재시작은 BackgroundService가 담당합니다. 이 ViewModel은 RuntimeState와 video element를 동기화합니다. StopAll → StartAll 사이클에서 기존 hls.js 인스턴스를 제거하고, 다시 Connected/Connecting 상태가 되면 안전하게 재연결합니다.</para>
/// \endif
/// \if EN
/// <para>Describes behavior and usage considerations for this member.</para>
/// \endif
/// </remarks>
public sealed class LivePageViewModel : IAsyncDisposable
{
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
    /// <para>runtime State 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the runtime state value.</para>
    /// \endif
    /// </summary>
    private readonly ICameraRuntimeStateService _runtimeState;
    /// <summary>
    /// \if KO
    /// <para>js Runtime 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the js runtime value.</para>
    /// \endif
    /// </summary>
    private readonly IJSRuntime _jsRuntime;
    /// <summary>
    /// \if KO
    /// <para>gate 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the gate value.</para>
    /// \endif
    /// </summary>
    private readonly object _gate = new();
    /// <summary>
    /// \if KO
    /// <para>is Initialized 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the is initialized value.</para>
    /// \endif
    /// </summary>
    private bool _isInitialized;
    /// <summary>
    /// \if KO
    /// <para>is Disposed 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the is disposed value.</para>
    /// \endif
    /// </summary>
    private bool _isDisposed;

    /// <summary>
    /// \if KO
    /// <para>\brief LivePageViewModel 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="LivePageViewModel"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="repository">
    /// \if KO
    /// <para>카메라 Repository입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IVmsCameraRepository</c> value used for repository.</para>
    /// \endif
    /// </param>
    /// <param name="runtimeState">
    /// \if KO
    /// <para>카메라 런타임 상태 서비스입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ICameraRuntimeStateService</c> value used for runtime state.</para>
    /// \endif
    /// </param>
    /// <param name="jsRuntime">
    /// \if KO
    /// <para>Blazor JSRuntime입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IJSRuntime</c> value used for js runtime.</para>
    /// \endif
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// \if KO
    /// <para>필수 입력 인자 중 하나가 <see langword="null"/>인 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when a required input argument is <see langword="null"/>.</para>
    /// \endif
    /// </exception>
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
    /// \if KO
    /// <para>\brief RuntimeState 변경으로 player 동기화가 필요할 때 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Occurs when players need sync takes place.</para>
    /// \endif
    /// </summary>
    public event EventHandler? PlayersNeedSync;

    /// <summary>
    /// \if KO
    /// <para>\brief Live 화면에 표시할 카메라 목록입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the cameras value.</para>
    /// \endif
    /// </summary>
    public IReadOnlyList<CameraDevice> Cameras { get; private set; } = Array.Empty<CameraDevice>();

    /// <summary>
    /// \if KO
    /// <para>\brief Live 화면 진입 시 카메라 목록을 로드하고 RuntimeState 변경을 구독합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the initialize operation.</para>
    /// \endif
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
    /// \if KO
    /// <para>\brief Video DOM이 생성된 뒤 player 상태를 RuntimeState와 동기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the after render async event or state change.</para>
    /// \endif
    /// </summary>
    /// <param name="firstRender">
    /// \if KO
    /// <para>첫 렌더링 여부입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>bool</c> value used for first render.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>비동기 작업입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the on after render async operation.</para>
    /// \endif
    /// </returns>
    public async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        await SynchronizePlayersAsync();
    }

    /// <summary>
    /// \if KO
    /// <para>\brief 현재 RuntimeState에 맞춰 hls.js player를 생성 또는 제거합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the synchronize players async operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>비동기 작업입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the synchronize players async operation.</para>
    /// \endif
    /// </returns>
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
    /// \if KO
    /// <para>\brief Live 화면에서 사용하는 video element ID를 반환합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the player id value.</para>
    /// \endif
    /// </summary>
    /// <param name="camera">
    /// \if KO
    /// <para>카메라 정보입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CameraDevice</c> value used for camera.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>HTML video element ID입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the get player id operation.</para>
    /// \endif
    /// </returns>
    public static string GetPlayerId(CameraDevice camera)
    {
        ArgumentNullException.ThrowIfNull(camera);
        return $"video-{camera.Id}";
    }

    /// <summary>
    /// \if KO
    /// <para>이 인스턴스가 소유한 리소스를 해제합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Releases resources owned by this instance.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Dispose Async 작업에서 생성한 <c>ValueTask</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ValueTask</c> result produced by the dispose async operation.</para>
    /// \endif
    /// </returns>
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


    /// <summary>
    /// \if KO
    /// <para>Versioned Hls Url 값을 구성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Builds the versioned hls url value.</para>
    /// \endif
    /// </summary>
    /// <param name="camera">
    /// \if KO
    /// <para>camera에 사용할 <c>CameraDevice</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CameraDevice</c> value used for camera.</para>
    /// \endif
    /// </param>
    /// <param name="state">
    /// \if KO
    /// <para>state에 사용할 <c>CameraRuntimeState</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CameraRuntimeState</c> value used for state.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Build Versioned Hls Url 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the build versioned hls url operation.</para>
    /// \endif
    /// </returns>
    private static string BuildVersionedHlsUrl(CameraDevice camera, CameraRuntimeState state)
    {
        ArgumentNullException.ThrowIfNull(camera);
        ArgumentNullException.ThrowIfNull(state);

        string separator = camera.HlsUrl.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return $"{camera.HlsUrl}{separator}session={state.RestartCount}";
    }

    /// <summary>
    /// \if KO
    /// <para>Ensure Player Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the ensure player async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="camera">
    /// \if KO
    /// <para>camera에 사용할 <c>CameraDevice</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CameraDevice</c> value used for camera.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Ensure Player Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the ensure player async operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Destroy Player Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the destroy player async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="camera">
    /// \if KO
    /// <para>camera에 사용할 <c>CameraDevice</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CameraDevice</c> value used for camera.</para>
    /// \endif
    /// </param>
    /// <param name="reason">
    /// \if KO
    /// <para>reason에 사용할 <c>string?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for reason.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Destroy Player Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the destroy player async operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Runtime State Changed 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the runtime state changed event or state change.</para>
    /// \endif
    /// </summary>
    /// <param name="sender">
    /// \if KO
    /// <para>이벤트를 발생시킨 객체입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The object that raised the event.</para>
    /// \endif
    /// </param>
    /// <param name="state">
    /// \if KO
    /// <para>state에 사용할 <c>CameraRuntimeState</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CameraRuntimeState</c> value used for state.</para>
    /// \endif
    /// </param>
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
