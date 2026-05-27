using Dreamine.Hybrid.Interfaces;
using Dreamine.Hybrid.State;
using DreamineVMS.Messages;
using DreamineVMS.States;

namespace DreamineVMS.Blazor.ViewModels;

/// <summary>
/// \brief WPF Shell 내부에 Embedded 방식으로 표시되는 VMS Local Dashboard ViewModel입니다.
/// </summary>
/// <remarks>
/// VmsLocalDashboard.razor에서 Hybrid State와 Message Bus를 직접 참조하지 않도록 분리합니다.
/// </remarks>
public sealed class VmsLocalDashboardViewModel : IDisposable
{
    private readonly IHybridStateStore<VmsDashboardState> _store;
    private readonly IHybridMessageBus _bus;
    private bool _isInitialized;

    /// <summary>
    /// \brief VmsLocalDashboardViewModel 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="store">Hybrid 상태 저장소입니다.</param>
    /// <param name="bus">Hybrid 메시지 버스입니다.</param>
    public VmsLocalDashboardViewModel(
        IHybridStateStore<VmsDashboardState> store,
        IHybridMessageBus bus)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
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
    /// \brief ViewModel을 초기화하고 상태 변경 이벤트를 구독합니다.
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
    /// \brief WPF Shell에 Dashboard Refresh 요청을 전송합니다.
    /// </summary>
    /// <returns>비동기 작업입니다.</returns>
    public Task RefreshAsync()
    {
        return PublishAsync(VmsDashboardActions.EmbeddedRefresh);
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

    private Task PublishAsync(string action)
    {
        return _bus.PublishAsync(new VmsDashboardActionRequestedMessage
        {
            Action = action
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
        _isInitialized = false;
    }

    private void OnDashboardStateChanged(
        object? sender,
        HybridStateChangedEventArgs<VmsDashboardState> e)
    {
        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}