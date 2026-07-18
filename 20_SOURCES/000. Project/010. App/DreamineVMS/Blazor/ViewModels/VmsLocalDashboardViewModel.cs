using Dreamine.Hybrid.Interfaces;
using Dreamine.Hybrid.State;
using DreamineVMS.Messages;
using DreamineVMS.States;

namespace DreamineVMS.Blazor.ViewModels;

/// <summary>
/// \if KO
/// <para>\brief WPF Shell 내부에 Embedded 방식으로 표시되는 VMS Local Dashboard ViewModel입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates vms local dashboard view model functionality and related state.</para>
/// \endif
/// </summary>
/// <remarks>
/// \if KO
/// <para>VmsLocalDashboard.razor에서 Hybrid State와 Message Bus를 직접 참조하지 않도록 분리합니다.</para>
/// \endif
/// \if EN
/// <para>Describes behavior and usage considerations for this member.</para>
/// \endif
/// </remarks>
public sealed class VmsLocalDashboardViewModel : IDisposable
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
    /// <para>is Initialized 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the is initialized value.</para>
    /// \endif
    /// </summary>
    private bool _isInitialized;

    /// <summary>
    /// \if KO
    /// <para>\brief VmsLocalDashboardViewModel 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="VmsLocalDashboardViewModel"/> class with the specified settings.</para>
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
    /// <exception cref="ArgumentNullException">
    /// \if KO
    /// <para>필수 입력 인자 중 하나가 <see langword="null"/>인 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when a required input argument is <see langword="null"/>.</para>
    /// \endif
    /// </exception>
    public VmsLocalDashboardViewModel(
        IHybridStateStore<VmsDashboardState> store,
        IHybridMessageBus bus)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
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

        _store.StateChanged += OnDashboardStateChanged;
        _isInitialized = true;
    }

    /// <summary>
    /// \if KO
    /// <para>\brief WPF Shell에 Dashboard Refresh 요청을 전송합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the refresh async operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>비동기 작업입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the refresh async operation.</para>
    /// \endif
    /// </returns>
    public Task RefreshAsync()
    {
        return PublishAsync(VmsDashboardActions.EmbeddedRefresh);
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
    /// <returns>
    /// \if KO
    /// <para>Publish Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the publish async operation.</para>
    /// \endif
    /// </returns>
    private Task PublishAsync(string action)
    {
        return _bus.PublishAsync(new VmsDashboardActionRequestedMessage
        {
            Action = action
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
        _isInitialized = false;
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