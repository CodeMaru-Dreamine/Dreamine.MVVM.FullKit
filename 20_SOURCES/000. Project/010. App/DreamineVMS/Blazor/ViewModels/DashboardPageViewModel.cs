using Dreamine.Hybrid.Interfaces;
using Dreamine.Hybrid.State;
using DreamineVMS.Messages;
using DreamineVMS.Models;
using DreamineVMS.Services.Cameras;
using DreamineVMS.Services.Runtime;
using DreamineVMS.States;

namespace DreamineVMS.Blazor.ViewModels;

/// <summary>
/// \if KO
/// <para>\brief Blazor Server Dashboard 페이지의 ViewModel입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates dashboard page view model functionality and related state.</para>
/// \endif
/// </summary>
/// <remarks>
/// \if KO
/// <para>Dashboard.razor에서 Hybrid State, Message Bus, Camera Repository, Runtime State Service를 직접 참조하지 않도록 분리합니다.</para>
/// \endif
/// \if EN
/// <para>Describes behavior and usage considerations for this member.</para>
/// \endif
/// </remarks>
public sealed class DashboardPageViewModel : IDisposable
{
    /// <summary>
    /// \if KO
    /// <para>store 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the store value.</para>
    /// \endif
    /// </summary>
    private readonly IHybridStateStore<VmsDashboardState> _store;
    /// <summary>
    /// \if KO
    /// <para>bus 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the bus value.</para>
    /// \endif
    /// </summary>
    private readonly IHybridMessageBus _bus;
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
    /// <para>is Initialized 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the is initialized value.</para>
    /// \endif
    /// </summary>
    private bool _isInitialized;

    /// <summary>
    /// \if KO
    /// <para>\brief DashboardPageViewModel 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="DashboardPageViewModel"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="store">
    /// \if KO
    /// <para>Hybrid 상태 저장소입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IHybridStateStore&lt;VmsDashboardState&gt;</c> value used for store.</para>
    /// \endif
    /// </param>
    /// <param name="bus">
    /// \if KO
    /// <para>Hybrid 메시지 버스입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IHybridMessageBus</c> value used for bus.</para>
    /// \endif
    /// </param>
    /// <param name="repository">
    /// \if KO
    /// <para>카메라 저장소입니다.</para>
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
    /// <exception cref="ArgumentNullException">
    /// \if KO
    /// <para>필수 입력 인자 중 하나가 <see langword="null"/>인 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when a required input argument is <see langword="null"/>.</para>
    /// \endif
    /// </exception>
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
    /// \if KO
    /// <para>\brief ViewModel 상태 변경 이벤트입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Occurs when state changed takes place.</para>
    /// \endif
    /// </summary>
    public event EventHandler? StateChanged;

    /// <summary>
    /// \if KO
    /// <para>\brief 전체 카메라 수입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the total camera count value.</para>
    /// \endif
    /// </summary>
    public int TotalCameraCount => _store.State.TotalCameraCount;

    /// <summary>
    /// \if KO
    /// <para>\brief 연결된 카메라 수입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the connected camera count value.</para>
    /// \endif
    /// </summary>
    public int ConnectedCameraCount => _store.State.ConnectedCameraCount;

    /// <summary>
    /// \if KO
    /// <para>\brief 녹화 중인 카메라 수입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the recording camera count value.</para>
    /// \endif
    /// </summary>
    public int RecordingCameraCount => _store.State.RecordingCameraCount;

    /// <summary>
    /// \if KO
    /// <para>\brief 마지막 이벤트 메시지입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the last event value.</para>
    /// \endif
    /// </summary>
    public string LastEvent => _store.State.LastEvent;

    /// <summary>
    /// \if KO
    /// <para>\brief 마지막 갱신 시각 표시 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the last updated text value.</para>
    /// \endif
    /// </summary>
    public string LastUpdatedText => _store.State.LastUpdated?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-";

    /// <summary>
    /// \if KO
    /// <para>\brief 화면에 표시할 카메라 목록입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the cameras value.</para>
    /// \endif
    /// </summary>
    public IReadOnlyList<DashboardCameraItemViewModel> Cameras { get; private set; }
        = Array.Empty<DashboardCameraItemViewModel>();

    /// <summary>
    /// \if KO
    /// <para>\brief ViewModel을 초기화하고 상태 변경 이벤트를 구독합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the initialize operation.</para>
    /// \endif
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
    /// \if KO
    /// <para>\brief 레거시 호환용 Live View 요청입니다. 서버 Dashboard에서는 href="/live" 라우팅을 우선 사용합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the open live async operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>비동기 작업입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the open live async operation.</para>
    /// \endif
    /// </returns>
    public Task OpenLiveAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// \if KO
    /// <para>\brief 선택한 카메라 연결을 WPF Shell에 요청합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the connect camera async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="cameraId">
    /// \if KO
    /// <para>카메라 ID입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for camera id.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>비동기 작업입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the connect camera async operation.</para>
    /// \endif
    /// </returns>
    public Task ConnectCameraAsync(string cameraId)
    {
        return PublishAsync(VmsDashboardActions.CameraConnect, cameraId);
    }

    /// <summary>
    /// \if KO
    /// <para>\brief 선택한 카메라 연결 해제를 WPF Shell에 요청합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the disconnect camera async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="cameraId">
    /// \if KO
    /// <para>카메라 ID입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for camera id.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>비동기 작업입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the disconnect camera async operation.</para>
    /// \endif
    /// </returns>
    public Task DisconnectCameraAsync(string cameraId)
    {
        return PublishAsync(VmsDashboardActions.CameraDisconnect, cameraId);
    }

    /// <summary>
    /// \if KO
    /// <para>\brief 전체 카메라 연결을 WPF Shell에 요청합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the start all async operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>비동기 작업입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the start all async operation.</para>
    /// \endif
    /// </returns>
    public Task StartAllAsync()
    {
        return PublishAsync(VmsDashboardActions.CameraStartAll);
    }

    /// <summary>
    /// \if KO
    /// <para>\brief 전체 카메라 연결 해제를 WPF Shell에 요청합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the stop all async operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>비동기 작업입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the stop all async operation.</para>
    /// \endif
    /// </returns>
    public Task StopAllAsync()
    {
        return PublishAsync(VmsDashboardActions.CameraStopAll);
    }

    /// <summary>
    /// \if KO
    /// <para>\brief WPF Shell 로그 삭제를 요청합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the clear logs async operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>비동기 작업입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the clear logs async operation.</para>
    /// \endif
    /// </returns>
    public Task ClearLogsAsync()
    {
        return PublishAsync(VmsDashboardActions.ClearLogs);
    }

    /// <summary>
    /// \if KO
    /// <para>Publish Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the publish async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="action">
    /// \if KO
    /// <para>action에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for action.</para>
    /// \endif
    /// </param>
    /// <param name="cameraId">
    /// \if KO
    /// <para>camera Id에 사용할 <c>string?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for camera id.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Publish Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the publish async operation.</para>
    /// \endif
    /// </returns>
    private Task PublishAsync(string action, string? cameraId = null)
    {
        return _bus.PublishAsync(new VmsDashboardActionRequestedMessage
        {
            Action = action,
            CameraId = cameraId
        });
    }

    /// <summary>
    /// \if KO
    /// <para>이 인스턴스가 소유한 리소스를 해제합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Releases resources owned by this instance.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>Refresh Cameras 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the refresh cameras operation.</para>
    /// \endif
    /// </summary>
    private void RefreshCameras()
    {
        Cameras = _repository.GetAll()
            .OrderBy(camera => camera.DisplayOrder)
            .Select(CreateCameraItem)
            .ToArray();
    }

    /// <summary>
    /// \if KO
    /// <para>Camera Item 값을 생성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Creates the camera item value.</para>
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
    /// <para>Create Camera Item 작업에서 생성한 <c>DashboardCameraItemViewModel</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>DashboardCameraItemViewModel</c> result produced by the create camera item operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Dashboard State Changed 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the dashboard state changed event or state change.</para>
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
    /// <param name="e">
    /// \if KO
    /// <para>이벤트와 관련된 데이터를 포함합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Contains data associated with the event.</para>
    /// \endif
    /// </param>
    private void OnDashboardStateChanged(
        object? sender,
        HybridStateChangedEventArgs<VmsDashboardState> e)
    {
        NotifyStateChanged();
    }

    /// <summary>
    /// \if KO
    /// <para>Camera Runtime State Changed 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the camera runtime state changed event or state change.</para>
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
    /// <param name="e">
    /// \if KO
    /// <para>이벤트와 관련된 데이터를 포함합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Contains data associated with the event.</para>
    /// \endif
    /// </param>
    private void OnCameraRuntimeStateChanged(object? sender, CameraRuntimeState e)
    {
        RefreshCameras();
        NotifyStateChanged();
    }

    /// <summary>
    /// \if KO
    /// <para>Notify State Changed 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the notify state changed operation.</para>
    /// \endif
    /// </summary>
    private void NotifyStateChanged()
    {
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>
/// \if KO
/// <para>\brief Dashboard 화면에 표시할 카메라 항목 ViewModel입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates dashboard camera item view model functionality and related state.</para>
/// \endif
/// </summary>
public sealed class DashboardCameraItemViewModel
{
    /// <summary>
    /// \if KO
    /// <para>\brief DashboardCameraItemViewModel 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="DashboardCameraItemViewModel"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="id">
    /// \if KO
    /// <para>카메라 ID입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for id.</para>
    /// \endif
    /// </param>
    /// <param name="name">
    /// \if KO
    /// <para>카메라 이름입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for name.</para>
    /// \endif
    /// </param>
    /// <param name="host">
    /// \if KO
    /// <para>카메라 Host입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for host.</para>
    /// \endif
    /// </param>
    /// <param name="hlsUrl">
    /// \if KO
    /// <para>HLS URL입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for hls url.</para>
    /// \endif
    /// </param>
    /// <param name="displayOrder">
    /// \if KO
    /// <para>표시 순서입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for display order.</para>
    /// \endif
    /// </param>
    /// <param name="enabled">
    /// \if KO
    /// <para>사용 여부입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>bool</c> value used for enabled.</para>
    /// \endif
    /// </param>
    /// <param name="state">
    /// \if KO
    /// <para>카메라 연결 상태입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CameraConnectionState</c> value used for state.</para>
    /// \endif
    /// </param>
    /// <param name="lastMessage">
    /// \if KO
    /// <para>마지막 상태 메시지입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for last message.</para>
    /// \endif
    /// </param>
    /// <param name="lastError">
    /// \if KO
    /// <para>마지막 오류 메시지입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for last error.</para>
    /// \endif
    /// </param>
    /// <param name="lastUpdated">
    /// \if KO
    /// <para>마지막 갱신 시각입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>DateTimeOffset</c> value used for last updated.</para>
    /// \endif
    /// </param>
    /// <param name="restartCount">
    /// \if KO
    /// <para>재시작 횟수입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for restart count.</para>
    /// \endif
    /// </param>
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
    /// \if KO
    /// <para>\brief 카메라 ID입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the id value.</para>
    /// \endif
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// \if KO
    /// <para>\brief 카메라 이름입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the name value.</para>
    /// \endif
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// \if KO
    /// <para>\brief 카메라 Host입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the host value.</para>
    /// \endif
    /// </summary>
    public string Host { get; }

    /// <summary>
    /// \if KO
    /// <para>\brief HLS URL입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the hls url value.</para>
    /// \endif
    /// </summary>
    public string HlsUrl { get; }

    /// <summary>
    /// \if KO
    /// <para>\brief 표시 순서입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the display order value.</para>
    /// \endif
    /// </summary>
    public int DisplayOrder { get; }

    /// <summary>
    /// \if KO
    /// <para>\brief 사용 여부입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the enabled value.</para>
    /// \endif
    /// </summary>
    public bool Enabled { get; }

    /// <summary>
    /// \if KO
    /// <para>\brief 카메라 연결 상태입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the state value.</para>
    /// \endif
    /// </summary>
    public CameraConnectionState State { get; }

    /// <summary>
    /// \if KO
    /// <para>\brief 마지막 상태 메시지입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the last message value.</para>
    /// \endif
    /// </summary>
    public string LastMessage { get; }

    /// <summary>
    /// \if KO
    /// <para>\brief 마지막 오류 메시지입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the last error value.</para>
    /// \endif
    /// </summary>
    public string? LastError { get; }

    /// <summary>
    /// \if KO
    /// <para>\brief 마지막 갱신 시각입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the last updated value.</para>
    /// \endif
    /// </summary>
    public DateTimeOffset LastUpdated { get; }

    /// <summary>
    /// \if KO
    /// <para>\brief 재시작 횟수입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the restart count value.</para>
    /// \endif
    /// </summary>
    public int RestartCount { get; }

    /// <summary>
    /// \if KO
    /// <para>\brief 상태 CSS 클래스입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the state css class value.</para>
    /// \endif
    /// </summary>
    public string StateCssClass => State.ToString().ToLowerInvariant();

    /// <summary>
    /// \if KO
    /// <para>\brief 상태 표시 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the state text value.</para>
    /// \endif
    /// </summary>
    public string StateText => State.ToString();

    /// <summary>
    /// \if KO
    /// <para>\brief 마지막 갱신 시각 표시 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the last updated text value.</para>
    /// \endif
    /// </summary>
    public string LastUpdatedText => LastUpdated.ToString("yyyy-MM-dd HH:mm:ss");
}