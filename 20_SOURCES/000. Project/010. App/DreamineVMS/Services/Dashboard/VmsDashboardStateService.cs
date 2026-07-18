using Dreamine.Hybrid.Interfaces;
using DreamineVMS.Models;
using DreamineVMS.Services.Cameras;
using DreamineVMS.Services.Runtime;
using DreamineVMS.States;

namespace DreamineVMS.Services.Dashboard;

/// <summary>
/// \if KO
/// <para>\brief 카메라 Repository / RuntimeState 변경을 구독하여 VmsDashboardState를 자동 갱신하는 도메인 서비스입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates vms dashboard state service functionality and related state.</para>
/// \endif
/// </summary>
/// <remarks>
/// \if KO
/// <para>이 서비스는 UI 계층(WPF ViewModel, Blazor ViewModel)에 의존하지 않습니다. WPF Window가 떠 있는지와 무관하게 상태 스토어가 항상 최신을 유지합니다.</para>
/// \endif
/// \if EN
/// <para>Describes behavior and usage considerations for this member.</para>
/// \endif
/// </remarks>
public sealed class VmsDashboardStateService : IVmsDashboardStateService, IDisposable
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
    /// <para>state Store 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the state store value.</para>
    /// \endif
    /// </summary>
    private readonly IHybridStateStore<VmsDashboardState> _stateStore;
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
    /// <para>is Started 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the is started value.</para>
    /// \endif
    /// </summary>
    private bool _isStarted;
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
    /// <para>\brief VmsDashboardStateService 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="VmsDashboardStateService"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
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
    /// <param name="stateStore">
    /// \if KO
    /// <para>대시보드 상태 저장소입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IHybridStateStore&lt;VmsDashboardState&gt;</c> value used for state store.</para>
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
    public VmsDashboardStateService(
        IVmsCameraRepository repository,
        ICameraRuntimeStateService runtimeState,
        IHybridStateStore<VmsDashboardState> stateStore)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _runtimeState = runtimeState ?? throw new ArgumentNullException(nameof(runtimeState));
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
    }

    /// <summary>
    /// \if KO
    /// <para>Start 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the start operation.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>Refresh 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the refresh operation.</para>
    /// \endif
    /// </summary>
    /// <param name="lastEvent">
    /// \if KO
    /// <para>last Event에 사용할 <c>string?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for last event.</para>
    /// \endif
    /// </param>
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
    /// <param name="e">
    /// \if KO
    /// <para>이벤트와 관련된 데이터를 포함합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Contains data associated with the event.</para>
    /// \endif
    /// </param>
    private void OnRuntimeStateChanged(object? sender, CameraRuntimeState e)
    {
        Refresh(e.LastMessage);
    }
}
