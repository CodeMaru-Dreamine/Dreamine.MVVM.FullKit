using Dreamine.Hybrid.Interfaces;
using Dreamine.Hybrid.State;
using DreamineVMS.Messages;
using DreamineVMS.Models;
using DreamineVMS.Services.Cameras;
using DreamineVMS.Services.Runtime;
using DreamineVMS.States;

namespace DreamineVMS.Blazor.ViewModels;

/// <summary>
/// \brief Blazor Server Dashboard 페이지의 ViewModel입니다.
/// </summary>
/// <remarks>
/// Dashboard.razor에서 Hybrid State, Message Bus, Camera Repository,
/// Runtime State Service를 직접 참조하지 않도록 분리합니다.
/// </remarks>
public sealed class DashboardPageViewModel : IDisposable
{
    private readonly IHybridStateStore<VmsDashboardState> _store;
    private readonly IHybridMessageBus _bus;
    private readonly IVmsCameraRepository _repository;
    private readonly ICameraRuntimeStateService _runtimeState;
    private bool _isInitialized;

    /// <summary>
    /// \brief DashboardPageViewModel 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="store">Hybrid 상태 저장소입니다.</param>
    /// <param name="bus">Hybrid 메시지 버스입니다.</param>
    /// <param name="repository">카메라 저장소입니다.</param>
    /// <param name="runtimeState">카메라 런타임 상태 서비스입니다.</param>
    public DashboardPageViewModel(
        IHybridStateStore<VmsDashboardState> store,
        IHybridMessageBus bus,
        IVmsCameraRepository repository,
        ICameraRuntimeStateService runtimeState)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _runtimeState = runtimeState ?? throw new ArgumentNullException(nameof(runtimeState));
    }

    /// <summary>
    /// \brief ViewModel 상태 변경 이벤트입니다.
    /// </summary>
    public event EventHandler? StateChanged;

    /// <summary>
    /// \brief 전체 카메라 수입니다.
    /// </summary>
    public int TotalCameraCount => _store.State.TotalCameraCount;

    /// <summary>
    /// \brief 연결된 카메라 수입니다.
    /// </summary>
    public int ConnectedCameraCount => _store.State.ConnectedCameraCount;

    /// <summary>
    /// \brief 녹화 중인 카메라 수입니다.
    /// </summary>
    public int RecordingCameraCount => _store.State.RecordingCameraCount;

    /// <summary>
    /// \brief 마지막 이벤트 메시지입니다.
    /// </summary>
    public string LastEvent => _store.State.LastEvent;

    /// <summary>
    /// \brief 마지막 갱신 시각 표시 문자열입니다.
    /// </summary>
    public string LastUpdatedText => _store.State.LastUpdated?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-";

    /// <summary>
    /// \brief 화면에 표시할 카메라 목록입니다.
    /// </summary>
    public IReadOnlyList<DashboardCameraItemViewModel> Cameras { get; private set; }
        = Array.Empty<DashboardCameraItemViewModel>();

    /// <summary>
    /// \brief ViewModel을 초기화하고 상태 변경 이벤트를 구독합니다.
    /// </summary>
    public void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        RefreshCameras();

        _store.StateChanged += OnDashboardStateChanged;
        _runtimeState.StateChanged += OnCameraRuntimeStateChanged;

        _isInitialized = true;
    }

    /// <summary>
    /// \brief 레거시 호환용 Live View 요청입니다. 서버 Dashboard에서는 href="/live" 라우팅을 우선 사용합니다.
    /// </summary>
    /// <returns>비동기 작업입니다.</returns>
    public Task OpenLiveAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// \brief 선택한 카메라 연결을 WPF Shell에 요청합니다.
    /// </summary>
    /// <param name="cameraId">카메라 ID입니다.</param>
    /// <returns>비동기 작업입니다.</returns>
    public Task ConnectCameraAsync(string cameraId)
    {
        return PublishAsync(VmsDashboardActions.CameraConnect, cameraId);
    }

    /// <summary>
    /// \brief 선택한 카메라 연결 해제를 WPF Shell에 요청합니다.
    /// </summary>
    /// <param name="cameraId">카메라 ID입니다.</param>
    /// <returns>비동기 작업입니다.</returns>
    public Task DisconnectCameraAsync(string cameraId)
    {
        return PublishAsync(VmsDashboardActions.CameraDisconnect, cameraId);
    }

    /// <summary>
    /// \brief 전체 카메라 연결을 WPF Shell에 요청합니다.
    /// </summary>
    /// <returns>비동기 작업입니다.</returns>
    public Task StartAllAsync()
    {
        return PublishAsync(VmsDashboardActions.CameraStartAll);
    }

    /// <summary>
    /// \brief 전체 카메라 연결 해제를 WPF Shell에 요청합니다.
    /// </summary>
    /// <returns>비동기 작업입니다.</returns>
    public Task StopAllAsync()
    {
        return PublishAsync(VmsDashboardActions.CameraStopAll);
    }

    /// <summary>
    /// \brief WPF Shell 로그 삭제를 요청합니다.
    /// </summary>
    /// <returns>비동기 작업입니다.</returns>
    public Task ClearLogsAsync()
    {
        return PublishAsync(VmsDashboardActions.ClearLogs);
    }

    private Task PublishAsync(string action, string? cameraId = null)
    {
        return _bus.PublishAsync(new VmsDashboardActionRequestedMessage
        {
            Action = action,
            CameraId = cameraId
        });
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_isInitialized)
        {
            return;
        }

        _store.StateChanged -= OnDashboardStateChanged;
        _runtimeState.StateChanged -= OnCameraRuntimeStateChanged;

        _isInitialized = false;
    }

    private void RefreshCameras()
    {
        Cameras = _repository.GetAll()
            .OrderBy(camera => camera.DisplayOrder)
            .Select(CreateCameraItem)
            .ToArray();
    }

    private DashboardCameraItemViewModel CreateCameraItem(CameraDevice camera)
    {
        CameraRuntimeState runtimeState = _runtimeState.GetState(camera.Id);

        return new DashboardCameraItemViewModel(
            id: camera.Id,
            name: camera.Name,
            host: camera.Host,
            hlsUrl: camera.HlsUrl,
            displayOrder: camera.DisplayOrder,
            enabled: camera.Enabled,
            state: runtimeState.State,
            lastMessage: runtimeState.LastMessage,
            lastError: runtimeState.LastError,
            lastUpdated: runtimeState.LastUpdated,
            restartCount: runtimeState.RestartCount);
    }

    private void OnDashboardStateChanged(
        object? sender,
        HybridStateChangedEventArgs<VmsDashboardState> e)
    {
        NotifyStateChanged();
    }

    private void OnCameraRuntimeStateChanged(object? sender, CameraRuntimeState e)
    {
        RefreshCameras();
        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>
/// \brief Dashboard 화면에 표시할 카메라 항목 ViewModel입니다.
/// </summary>
public sealed class DashboardCameraItemViewModel
{
    /// <summary>
    /// \brief DashboardCameraItemViewModel 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="id">카메라 ID입니다.</param>
    /// <param name="name">카메라 이름입니다.</param>
    /// <param name="host">카메라 Host입니다.</param>
    /// <param name="hlsUrl">HLS URL입니다.</param>
    /// <param name="displayOrder">표시 순서입니다.</param>
    /// <param name="enabled">사용 여부입니다.</param>
    /// <param name="state">카메라 연결 상태입니다.</param>
    /// <param name="lastMessage">마지막 상태 메시지입니다.</param>
    /// <param name="lastError">마지막 오류 메시지입니다.</param>
    /// <param name="lastUpdated">마지막 갱신 시각입니다.</param>
    /// <param name="restartCount">재시작 횟수입니다.</param>
    public DashboardCameraItemViewModel(
        string id,
        string name,
        string host,
        string hlsUrl,
        int displayOrder,
        bool enabled,
        CameraConnectionState state,
        string lastMessage,
        string? lastError,
        DateTimeOffset lastUpdated,
        int restartCount)
    {
        Id = id;
        Name = name;
        Host = host;
        HlsUrl = hlsUrl;
        DisplayOrder = displayOrder;
        Enabled = enabled;
        State = state;
        LastMessage = lastMessage;
        LastError = lastError;
        LastUpdated = lastUpdated;
        RestartCount = restartCount;
    }

    /// <summary>
    /// \brief 카메라 ID입니다.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// \brief 카메라 이름입니다.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// \brief 카메라 Host입니다.
    /// </summary>
    public string Host { get; }

    /// <summary>
    /// \brief HLS URL입니다.
    /// </summary>
    public string HlsUrl { get; }

    /// <summary>
    /// \brief 표시 순서입니다.
    /// </summary>
    public int DisplayOrder { get; }

    /// <summary>
    /// \brief 사용 여부입니다.
    /// </summary>
    public bool Enabled { get; }

    /// <summary>
    /// \brief 카메라 연결 상태입니다.
    /// </summary>
    public CameraConnectionState State { get; }

    /// <summary>
    /// \brief 마지막 상태 메시지입니다.
    /// </summary>
    public string LastMessage { get; }

    /// <summary>
    /// \brief 마지막 오류 메시지입니다.
    /// </summary>
    public string? LastError { get; }

    /// <summary>
    /// \brief 마지막 갱신 시각입니다.
    /// </summary>
    public DateTimeOffset LastUpdated { get; }

    /// <summary>
    /// \brief 재시작 횟수입니다.
    /// </summary>
    public int RestartCount { get; }

    /// <summary>
    /// \brief 상태 CSS 클래스입니다.
    /// </summary>
    public string StateCssClass => State.ToString().ToLowerInvariant();

    /// <summary>
    /// \brief 상태 표시 문자열입니다.
    /// </summary>
    public string StateText => State.ToString();

    /// <summary>
    /// \brief 마지막 갱신 시각 표시 문자열입니다.
    /// </summary>
    public string LastUpdatedText => LastUpdated.ToString("yyyy-MM-dd HH:mm:ss");
}