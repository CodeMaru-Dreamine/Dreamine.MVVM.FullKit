using Dreamine.Hybrid.Interfaces;
using DreamineVMS.Models;
using DreamineVMS.Services.Cameras;
using DreamineVMS.Services.Runtime;
using DreamineVMS.States;

namespace DreamineVMS.Services.Dashboard;

/// <summary>
/// \brief 카메라 Repository / RuntimeState 변경을 구독하여
///        VmsDashboardState를 자동 갱신하는 도메인 서비스입니다.
/// </summary>
/// <remarks>
/// 이 서비스는 UI 계층(WPF ViewModel, Blazor ViewModel)에 의존하지 않습니다.
/// WPF Window가 떠 있는지와 무관하게 상태 스토어가 항상 최신을 유지합니다.
/// </remarks>
public sealed class VmsDashboardStateService : IVmsDashboardStateService, IDisposable
{
    private readonly IVmsCameraRepository _repository;
    private readonly ICameraRuntimeStateService _runtimeState;
    private readonly IHybridStateStore<VmsDashboardState> _stateStore;
    private readonly object _gate = new();
    private bool _isStarted;
    private bool _isDisposed;

    /// <summary>
    /// \brief VmsDashboardStateService 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="repository">카메라 저장소입니다.</param>
    /// <param name="runtimeState">카메라 런타임 상태 서비스입니다.</param>
    /// <param name="stateStore">대시보드 상태 저장소입니다.</param>
    public VmsDashboardStateService(
        IVmsCameraRepository repository,
        ICameraRuntimeStateService runtimeState,
        IHybridStateStore<VmsDashboardState> stateStore)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _runtimeState = runtimeState ?? throw new ArgumentNullException(nameof(runtimeState));
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
    }

    /// <inheritdoc />
    public void Start()
    {
        lock (_gate)
        {
            if (_isStarted || _isDisposed)
            {
                return;
            }

            _runtimeState.StateChanged += OnRuntimeStateChanged;
            _isStarted = true;
        }

        // 시작 시 1회 초기 동기화.
        Refresh("VMS application started.");
    }

    /// <inheritdoc />
    public void Refresh(string? lastEvent = null)
    {
        if (_isDisposed)
        {
            return;
        }

        IReadOnlyList<CameraDevice> cameras = _repository.GetAll();
        IReadOnlyList<CameraRuntimeState> states = _runtimeState.GetAll();

        int connectedCount = states.Count(state => state.State == CameraConnectionState.Connected);

        VmsDashboardState previous = _stateStore.State;
        string finalLastEvent = lastEvent ?? previous.LastEvent;

        _stateStore.SetState(new VmsDashboardState
        {
            TotalCameraCount = cameras.Count,
            ConnectedCameraCount = connectedCount,
            RecordingCameraCount = 0,
            LastEvent = finalLastEvent,
            LastUpdated = DateTimeOffset.Now
        });
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_gate)
        {
            if (_isDisposed)
            {
                return;
            }

            if (_isStarted)
            {
                _runtimeState.StateChanged -= OnRuntimeStateChanged;
                _isStarted = false;
            }

            _isDisposed = true;
        }
    }

    private void OnRuntimeStateChanged(object? sender, CameraRuntimeState e)
    {
        Refresh(e.LastMessage);
    }
}
