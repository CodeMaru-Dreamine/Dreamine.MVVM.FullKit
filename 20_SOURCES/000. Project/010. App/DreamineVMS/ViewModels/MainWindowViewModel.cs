using Dreamine.Hybrid.Interfaces;
using DreamineVMS.Messages;
using DreamineVMS.Models;
using DreamineVMS.Services.Cameras;
using DreamineVMS.Services.Dashboard;
using DreamineVMS.Services.Runtime;
using DreamineVMS.Services.Streaming;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace DreamineVMS.ViewModels;

/// <summary>
/// \if KO
/// <para>\brief DreamineVMS 메인 윈도우 ViewModel입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates main window view model functionality and related state.</para>
/// \endif
/// </summary>
/// <remarks>
/// \if KO
/// <para>이 ViewModel은 UI 표시와 사용자 명령 라우팅만 담당합니다. 대시보드 상태(VmsDashboardState) 갱신은 IVmsDashboardStateService가 책임집니다.</para>
/// \endif
/// \if EN
/// <para>Describes behavior and usage considerations for this member.</para>
/// \endif
/// </remarks>
public sealed class MainWindowViewModel : INotifyPropertyChanged, IDisposable
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
    /// <para>stream Service 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the stream service value.</para>
    /// \endif
    /// </summary>
    private readonly ICameraStreamService _streamService;
    /// <summary>
    /// \if KO
    /// <para>dashboard State Service 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the dashboard state service value.</para>
    /// \endif
    /// </summary>
    private readonly IVmsDashboardStateService _dashboardStateService;
    /// <summary>
    /// \if KO
    /// <para>dashboard Subscription 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the dashboard subscription value.</para>
    /// \endif
    /// </summary>
    private readonly IDisposable _dashboardSubscription;
    /// <summary>
    /// \if KO
    /// <para>agent Settings 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the agent settings value.</para>
    /// \endif
    /// </summary>
    private readonly AgentSettingsViewModel _agentSettings;
    /// <summary>
    /// \if KO
    /// <para>selected Camera 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the selected camera value.</para>
    /// \endif
    /// </summary>
    private CameraRowViewModel? _selectedCamera;
    /// <summary>
    /// \if KO
    /// <para>status Message 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the status message value.</para>
    /// \endif
    /// </summary>
    private string _statusMessage = "VMS application started.";
    /// <summary>
    /// \if KO
    /// <para>\brief MainWindowViewModel 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the agent settings value.</para>
    /// \endif
    /// </summary>
    public AgentSettingsViewModel AgentSettings => _agentSettings;

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="MainWindowViewModel"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="MainWindowViewModel"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="repository">
    /// \if KO
    /// <para>repository에 사용할 <c>IVmsCameraRepository</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IVmsCameraRepository</c> value used for repository.</para>
    /// \endif
    /// </param>
    /// <param name="runtimeState">
    /// \if KO
    /// <para>runtime State에 사용할 <c>ICameraRuntimeStateService</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ICameraRuntimeStateService</c> value used for runtime state.</para>
    /// \endif
    /// </param>
    /// <param name="streamService">
    /// \if KO
    /// <para>stream Service에 사용할 <c>ICameraStreamService</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ICameraStreamService</c> value used for stream service.</para>
    /// \endif
    /// </param>
    /// <param name="dashboardStateService">
    /// \if KO
    /// <para>dashboard State Service에 사용할 <c>IVmsDashboardStateService</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IVmsDashboardStateService</c> value used for dashboard state service.</para>
    /// \endif
    /// </param>
    /// <param name="messageBus">
    /// \if KO
    /// <para>message Bus에 사용할 <c>IHybridMessageBus</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IHybridMessageBus</c> value used for message bus.</para>
    /// \endif
    /// </param>
    /// <param name="agentSettings">
    /// \if KO
    /// <para>agent Settings에 사용할 <c>AgentSettingsViewModel</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>AgentSettingsViewModel</c> value used for agent settings.</para>
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
    public MainWindowViewModel(
        IVmsCameraRepository repository,
        ICameraRuntimeStateService runtimeState,
        ICameraStreamService streamService,
        IVmsDashboardStateService dashboardStateService,
        IHybridMessageBus messageBus,
        AgentSettingsViewModel agentSettings)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _runtimeState = runtimeState ?? throw new ArgumentNullException(nameof(runtimeState));
        _streamService = streamService ?? throw new ArgumentNullException(nameof(streamService));
        _dashboardStateService = dashboardStateService ?? throw new ArgumentNullException(nameof(dashboardStateService));
        ArgumentNullException.ThrowIfNull(messageBus);
        _agentSettings = agentSettings ?? throw new ArgumentNullException(nameof(agentSettings));

        foreach (CameraDevice camera in _repository.GetAll())
        {
            CameraRuntimeState state = _runtimeState.GetState(camera.Id);
            Cameras.Add(new CameraRowViewModel(camera, state));
        }

        SelectedCamera = Cameras.FirstOrDefault();
        Logs.Add(new VmsLogItem { Message = "VMS application started." });

        ConnectSelectedCameraCommand = new AsyncRelayCommand(ConnectSelectedCameraAsync, () => SelectedCamera is not null);
        DisconnectSelectedCameraCommand = new AsyncRelayCommand(DisconnectSelectedCameraAsync, () => SelectedCamera is not null);
        StartAllCommand = new AsyncRelayCommand(StartAllAsync);
        StopAllCommand = new AsyncRelayCommand(StopAllAsync);
        ClearLogsCommand = new RelayCommand(ClearLogs);

        _runtimeState.StateChanged += OnCameraRuntimeStateChanged;
        _dashboardSubscription = messageBus.Subscribe<VmsDashboardActionRequestedMessage>(OnDashboardActionRequestedAsync);
    }

    /// <summary>
    /// \if KO
    /// <para>\brief 속성 변경 이벤트입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Occurs when property changed takes place.</para>
    /// \endif
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// \if KO
    /// <para>\brief Server Dashboard에서 Live 탭으로 이동 요청이 들어왔을 때 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Occurs when open live tab requested takes place.</para>
    /// \endif
    /// </summary>
    public event EventHandler? OpenLiveTabRequested;

    /// <summary>
    /// \if KO
    /// <para>\brief 카메라 목록(행 ViewModel)입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the cameras value.</para>
    /// \endif
    /// </summary>
    public ObservableCollection<CameraRowViewModel> Cameras { get; } = new();

    /// <summary>
    /// \if KO
    /// <para>\brief 로그 목록입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the logs value.</para>
    /// \endif
    /// </summary>
    public ObservableCollection<VmsLogItem> Logs { get; } = new();

    /// <summary>
    /// \if KO
    /// <para>\brief 선택된 카메라입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the selected camera value.</para>
    /// \endif
    /// </summary>
    public CameraRowViewModel? SelectedCamera
    {
        get => _selectedCamera;
        set
        {
            if (ReferenceEquals(_selectedCamera, value))
            {
                return;
            }

            _selectedCamera = value;
            OnPropertyChanged();
            (ConnectSelectedCameraCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (DisconnectSelectedCameraCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    /// <summary>
    /// \if KO
    /// <para>\brief 상태 메시지입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the status message value.</para>
    /// \endif
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (_statusMessage == value)
            {
                return;
            }

            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// \if KO
    /// <para>\brief 선택 카메라 연결 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the connect selected camera command value.</para>
    /// \endif
    /// </summary>
    public ICommand ConnectSelectedCameraCommand { get; }

    /// <summary>
    /// \if KO
    /// <para>\brief 선택 카메라 연결 해제 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the disconnect selected camera command value.</para>
    /// \endif
    /// </summary>
    public ICommand DisconnectSelectedCameraCommand { get; }

    /// <summary>
    /// \if KO
    /// <para>\brief 전체 카메라 연결 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the start all command value.</para>
    /// \endif
    /// </summary>
    public ICommand StartAllCommand { get; }

    /// <summary>
    /// \if KO
    /// <para>\brief 전체 카메라 연결 해제 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the stop all command value.</para>
    /// \endif
    /// </summary>
    public ICommand StopAllCommand { get; }

    /// <summary>
    /// \if KO
    /// <para>\brief 로그 삭제 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the clear logs command value.</para>
    /// \endif
    /// </summary>
    public ICommand ClearLogsCommand { get; }

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
        _dashboardSubscription.Dispose();
        _runtimeState.StateChanged -= OnCameraRuntimeStateChanged;

        foreach (CameraRowViewModel row in Cameras)
        {
            row.Dispose();
        }
        Cameras.Clear();
    }

    /// <summary>
    /// \if KO
    /// <para>Connect Selected Camera Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the connect selected camera async operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Connect Selected Camera Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the connect selected camera async operation.</para>
    /// \endif
    /// </returns>
    private async Task ConnectSelectedCameraAsync()
    {
        CameraRowViewModel? selected = SelectedCamera;
        if (selected is null)
        {
            return;
        }

        await _streamService.StartAsync(selected.Id);
        AddLog($"Connect requested: {selected.Name}");
    }

    /// <summary>
    /// \if KO
    /// <para>Disconnect Selected Camera Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the disconnect selected camera async operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Disconnect Selected Camera Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the disconnect selected camera async operation.</para>
    /// \endif
    /// </returns>
    private async Task DisconnectSelectedCameraAsync()
    {
        CameraRowViewModel? selected = SelectedCamera;
        if (selected is null)
        {
            return;
        }

        await _streamService.StopAsync(selected.Id);
        AddLog($"Disconnect requested: {selected.Name}");
    }

    /// <summary>
    /// \if KO
    /// <para>Connect Camera Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the connect camera async operation.</para>
    /// \endif
    /// </summary>
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
    /// <para>Connect Camera Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the connect camera async operation.</para>
    /// \endif
    /// </returns>
    private async Task ConnectCameraAsync(string? cameraId)
    {
        if (string.IsNullOrWhiteSpace(cameraId))
        {
            AddLog("Connect request ignored: camera id is empty.");
            return;
        }

        CameraRowViewModel? camera = Cameras.FirstOrDefault(row => string.Equals(row.Id, cameraId, StringComparison.OrdinalIgnoreCase));
        if (camera is null)
        {
            AddLog($"Connect request ignored: camera not found. id={cameraId}");
            return;
        }

        await _streamService.StartAsync(camera.Id).ConfigureAwait(false);
        AddLog($"Dashboard connect requested: {camera.Name}");
    }

    /// <summary>
    /// \if KO
    /// <para>Disconnect Camera Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the disconnect camera async operation.</para>
    /// \endif
    /// </summary>
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
    /// <para>Disconnect Camera Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the disconnect camera async operation.</para>
    /// \endif
    /// </returns>
    private async Task DisconnectCameraAsync(string? cameraId)
    {
        if (string.IsNullOrWhiteSpace(cameraId))
        {
            AddLog("Disconnect request ignored: camera id is empty.");
            return;
        }

        CameraRowViewModel? camera = Cameras.FirstOrDefault(row => string.Equals(row.Id, cameraId, StringComparison.OrdinalIgnoreCase));
        if (camera is null)
        {
            AddLog($"Disconnect request ignored: camera not found. id={cameraId}");
            return;
        }

        await _streamService.StopAsync(camera.Id).ConfigureAwait(false);
        AddLog($"Dashboard disconnect requested: {camera.Name}");
    }

    /// <summary>
    /// \if KO
    /// <para>Start All Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the start all async operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Start All Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the start all async operation.</para>
    /// \endif
    /// </returns>
    private async Task StartAllAsync()
    {
        await _streamService.StartAllAsync();
        AddLog("Start all cameras requested.");
    }

    /// <summary>
    /// \if KO
    /// <para>Stop All Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the stop all async operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Stop All Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the stop all async operation.</para>
    /// \endif
    /// </returns>
    private async Task StopAllAsync()
    {
        await _streamService.StopAllAsync();
        AddLog("Stop all cameras requested.");
    }

    /// <summary>
    /// \if KO
    /// <para>Dashboard Action Requested Async 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the dashboard action requested async event or state change.</para>
    /// \endif
    /// </summary>
    /// <param name="message">
    /// \if KO
    /// <para>처리할 메시지입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The message to process.</para>
    /// \endif
    /// </param>
    /// <param name="cancellationToken">
    /// \if KO
    /// <para>취소 요청을 감시하는 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>On Dashboard Action Requested Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the on dashboard action requested async operation.</para>
    /// \endif
    /// </returns>
    private async Task OnDashboardActionRequestedAsync(VmsDashboardActionRequestedMessage message, CancellationToken cancellationToken)
    {
        switch (message.Action)
        {
            case VmsDashboardActions.EmbeddedRefresh:
                _dashboardStateService.Refresh("Embedded dashboard refresh requested.");
                AddLog("Embedded dashboard refresh requested.");
                break;

            case VmsDashboardActions.ServerOpenLive:
                // Server Dashboard is a browser/WebView route.
                // Do not switch the WPF tab from here, otherwise the Server Dashboard tab and WPF Live tab appear to be mixed.
                AddLog("Server dashboard requested Open Live. Use /live route in the server WebView.");
                break;

            case VmsDashboardActions.CameraConnect:
                await ConnectCameraAsync(message.CameraId).ConfigureAwait(false);
                break;

            case VmsDashboardActions.CameraDisconnect:
                await DisconnectCameraAsync(message.CameraId).ConfigureAwait(false);
                break;

            case VmsDashboardActions.CameraStartAll:
                await StartAllAsync().ConfigureAwait(false);
                break;

            case VmsDashboardActions.CameraStopAll:
                await StopAllAsync().ConfigureAwait(false);
                break;

            case VmsDashboardActions.ClearLogs:
                ClearLogs();
                break;

            default:
                AddLog($"Dashboard action requested: {message.Action}");
                break;
        }
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
    /// <param name="state">
    /// \if KO
    /// <para>state에 사용할 <c>CameraRuntimeState</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CameraRuntimeState</c> value used for state.</para>
    /// \endif
    /// </param>
    private void OnCameraRuntimeStateChanged(object? sender, CameraRuntimeState state)
    {
        // 상태 스토어 갱신은 IVmsDashboardStateService가 담당.
        // 여기서는 WPF UI 로그/상태 메시지만 처리.
        DispatchToUi(() => AddLogCore(state.LastMessage));
    }

    /// <summary>
    /// \if KO
    /// <para>Log 항목을 추가합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Adds the log item.</para>
    /// \endif
    /// </summary>
    /// <param name="message">
    /// \if KO
    /// <para>처리할 메시지입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The message to process.</para>
    /// \endif
    /// </param>
    private void AddLog(string message)
    {
        DispatchToUi(() => AddLogCore(message));
    }

    /// <summary>
    /// \if KO
    /// <para>Log Core 항목을 추가합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Adds the log core item.</para>
    /// \endif
    /// </summary>
    /// <param name="message">
    /// \if KO
    /// <para>처리할 메시지입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The message to process.</para>
    /// \endif
    /// </param>
    private void AddLogCore(string message)
    {
        StatusMessage = message;
        Logs.Insert(0, new VmsLogItem { Message = message });
        while (Logs.Count > 300)
        {
            Logs.RemoveAt(Logs.Count - 1);
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Clear Logs 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the clear logs operation.</para>
    /// \endif
    /// </summary>
    private void ClearLogs()
    {
        Logs.Clear();
        AddLog("Logs cleared.");
    }

    /// <summary>
    /// \if KO
    /// <para>Raise Open Live Tab Requested 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the raise open live tab requested operation.</para>
    /// \endif
    /// </summary>
    private void RaiseOpenLiveTabRequested()
    {
        DispatchToUi(() => OpenLiveTabRequested?.Invoke(this, EventArgs.Empty));
    }

    /// <summary>
    /// \if KO
    /// <para>Dispatch To Ui 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the dispatch to ui operation.</para>
    /// \endif
    /// </summary>
    /// <param name="action">
    /// \if KO
    /// <para>action에 사용할 <c>Action</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Action</c> value used for action.</para>
    /// \endif
    /// </param>
    private static void DispatchToUi(Action action)
    {
        System.Windows.Application? app = System.Windows.Application.Current;
        if (app is null)
        {
            return;
        }

        var dispatcher = app.Dispatcher;

        // 종료 시퀀스 중에는 Invoke가 OperationCanceledException을 던지므로 무시합니다.
        if (dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
        {
            return;
        }

        if (dispatcher.CheckAccess())
        {
            action();
        }
        else
        {
            try
            {
                dispatcher.Invoke(action);
            }
            catch (OperationCanceledException) { }
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Property Changed 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the property changed event or state change.</para>
    /// \endif
    /// </summary>
    /// <param name="propertyName">
    /// \if KO
    /// <para>property Name에 사용할 <c>string?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for property name.</para>
    /// \endif
    /// </param>
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// \if KO
/// <para>\brief 동기 ICommand 구현입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates relay command functionality and related state.</para>
/// \endif
/// </summary>
public sealed class RelayCommand : ICommand
{
    /// <summary>
    /// \if KO
    /// <para>execute 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the execute value.</para>
    /// \endif
    /// </summary>
    private readonly Action _execute;
    /// <summary>
    /// \if KO
    /// <para>can Execute 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the can execute value.</para>
    /// \endif
    /// </summary>
    private readonly Func<bool>? _canExecute;

    /// <summary>
    /// \if KO
    /// <para>\brief RelayCommand 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="RelayCommand"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="execute">
    /// \if KO
    /// <para>실행할 동작입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Action</c> value used for execute.</para>
    /// \endif
    /// </param>
    /// <param name="canExecute">
    /// \if KO
    /// <para>실행 가능 여부를 반환하는 조건입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Func&lt;bool&gt;?</c> value used for can execute.</para>
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
    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <summary>
    /// \if KO
    /// <para>Can Execute Changed 상황이 발생할 때 알립니다.</para>
    /// \endif
    /// \if EN
    /// <para>Occurs when can execute changed takes place.</para>
    /// \endif
    /// </summary>
    public event EventHandler? CanExecuteChanged;

    /// <summary>
    /// \if KO
    /// <para>Can Execute 조건을 확인합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Determines whether can execute.</para>
    /// \endif
    /// </summary>
    /// <param name="parameter">
    /// \if KO
    /// <para>parameter에 사용할 <c>object?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>object?</c> value used for parameter.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Can Execute 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the can execute condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    /// <summary>
    /// \if KO
    /// <para>Execute 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the execute operation.</para>
    /// \endif
    /// </summary>
    /// <param name="parameter">
    /// \if KO
    /// <para>parameter에 사용할 <c>object?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>object?</c> value used for parameter.</para>
    /// \endif
    /// </param>
    public void Execute(object? parameter) => _execute();

    /// <summary>
    /// \if KO
    /// <para>\brief CanExecuteChanged 이벤트를 UI Dispatcher에서 발생시킵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the raise can execute changed operation.</para>
    /// \endif
    /// </summary>
    public void RaiseCanExecuteChanged()
    {
        System.Windows.Threading.Dispatcher? dispatcher =
            System.Windows.Application.Current?.Dispatcher;

        if (dispatcher is null || dispatcher.CheckAccess())
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        dispatcher.InvokeAsync(() => CanExecuteChanged?.Invoke(this, EventArgs.Empty));
    }
}

/// <summary>
/// \if KO
/// <para>\brief 비동기 ICommand 구현입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates async relay command functionality and related state.</para>
/// \endif
/// </summary>
public sealed class AsyncRelayCommand : ICommand
{
    /// <summary>
    /// \if KO
    /// <para>execute 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the execute value.</para>
    /// \endif
    /// </summary>
    private readonly Func<Task> _execute;
    /// <summary>
    /// \if KO
    /// <para>can Execute 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the can execute value.</para>
    /// \endif
    /// </summary>
    private readonly Func<bool>? _canExecute;
    /// <summary>
    /// \if KO
    /// <para>is Running 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the is running value.</para>
    /// \endif
    /// </summary>
    private bool _isRunning;

    /// <summary>
    /// \if KO
    /// <para>\brief AsyncRelayCommand 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="AsyncRelayCommand"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="execute">
    /// \if KO
    /// <para>실행할 비동기 작업입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Func&lt;Task&gt;</c> value used for execute.</para>
    /// \endif
    /// </param>
    /// <param name="canExecute">
    /// \if KO
    /// <para>실행 가능 여부를 반환하는 조건입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Func&lt;bool&gt;?</c> value used for can execute.</para>
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
    public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <summary>
    /// \if KO
    /// <para>Can Execute Changed 상황이 발생할 때 알립니다.</para>
    /// \endif
    /// \if EN
    /// <para>Occurs when can execute changed takes place.</para>
    /// \endif
    /// </summary>
    public event EventHandler? CanExecuteChanged;

    /// <summary>
    /// \if KO
    /// <para>Can Execute 조건을 확인합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Determines whether can execute.</para>
    /// \endif
    /// </summary>
    /// <param name="parameter">
    /// \if KO
    /// <para>parameter에 사용할 <c>object?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>object?</c> value used for parameter.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Can Execute 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the can execute condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    public bool CanExecute(object? parameter) => !_isRunning && (_canExecute?.Invoke() ?? true);

    /// <summary>
    /// \if KO
    /// <para>Execute 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the execute operation.</para>
    /// \endif
    /// </summary>
    /// <param name="parameter">
    /// \if KO
    /// <para>parameter에 사용할 <c>object?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>object?</c> value used for parameter.</para>
    /// \endif
    /// </param>
    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        try
        {
            _isRunning = true;
            RaiseCanExecuteChanged();

            // WPF UI 컨텍스트를 유지하기 위해 ConfigureAwait(false) 사용 안 함.
            await _execute();
        }
        catch (Exception ex)
        {
            // 비동기 명령의 미처리 예외가 UI 스레드로 던져져 앱이 죽는 것을 막습니다.
            System.Diagnostics.Debug.WriteLine($"[AsyncRelayCommand] {ex}");
        }
        finally
        {
            _isRunning = false;
            RaiseCanExecuteChanged();
        }
    }

    /// <summary>
    /// \if KO
    /// <para>\brief CanExecuteChanged 이벤트를 UI Dispatcher에서 발생시킵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the raise can execute changed operation.</para>
    /// \endif
    /// </summary>
    public void RaiseCanExecuteChanged()
    {
        System.Windows.Threading.Dispatcher? dispatcher =
            System.Windows.Application.Current?.Dispatcher;

        if (dispatcher is null || dispatcher.CheckAccess())
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        dispatcher.InvokeAsync(() => CanExecuteChanged?.Invoke(this, EventArgs.Empty));
    }
}
