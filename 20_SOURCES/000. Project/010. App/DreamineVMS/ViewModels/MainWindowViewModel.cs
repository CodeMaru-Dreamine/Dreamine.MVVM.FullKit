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
/// \brief DreamineVMS 메인 윈도우 ViewModel입니다.
/// </summary>
/// <remarks>
/// 이 ViewModel은 UI 표시와 사용자 명령 라우팅만 담당합니다.
/// 대시보드 상태(VmsDashboardState) 갱신은 IVmsDashboardStateService가 책임집니다.
/// </remarks>
public sealed class MainWindowViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly IVmsCameraRepository _repository;
    private readonly ICameraRuntimeStateService _runtimeState;
    private readonly ICameraStreamService _streamService;
    private readonly IVmsDashboardStateService _dashboardStateService;
    private readonly IDisposable _dashboardSubscription;
    private readonly AgentSettingsViewModel _agentSettings;
    private CameraRowViewModel? _selectedCamera;
    private string _statusMessage = "VMS application started.";
    /// <summary>
    /// \brief MainWindowViewModel 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="repository">카메라 저장소입니다.</param>
    /// <param name="runtimeState">카메라 런타임 상태 서비스입니다.</param>
    /// <param name="streamService">카메라 스트림 서비스입니다.</param>
    /// <param name="dashboardStateService">대시보드 상태 도메인 서비스입니다.</param>
    /// <param name="messageBus">하이브리드 메시지 버스입니다.</param>
    public AgentSettingsViewModel AgentSettings => _agentSettings;

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

    /// <summary>\brief 속성 변경 이벤트입니다.</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>\brief Server Dashboard에서 Live 탭으로 이동 요청이 들어왔을 때 발생합니다.</summary>
    public event EventHandler? OpenLiveTabRequested;

    /// <summary>\brief 카메라 목록(행 ViewModel)입니다.</summary>
    public ObservableCollection<CameraRowViewModel> Cameras { get; } = new();

    /// <summary>\brief 로그 목록입니다.</summary>
    public ObservableCollection<VmsLogItem> Logs { get; } = new();

    /// <summary>\brief 선택된 카메라입니다.</summary>
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

    /// <summary>\brief 상태 메시지입니다.</summary>
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

    /// <summary>\brief 선택 카메라 연결 명령입니다.</summary>
    public ICommand ConnectSelectedCameraCommand { get; }

    /// <summary>\brief 선택 카메라 연결 해제 명령입니다.</summary>
    public ICommand DisconnectSelectedCameraCommand { get; }

    /// <summary>\brief 전체 카메라 연결 명령입니다.</summary>
    public ICommand StartAllCommand { get; }

    /// <summary>\brief 전체 카메라 연결 해제 명령입니다.</summary>
    public ICommand StopAllCommand { get; }

    /// <summary>\brief 로그 삭제 명령입니다.</summary>
    public ICommand ClearLogsCommand { get; }

    /// <inheritdoc />
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

    private async Task StartAllAsync()
    {
        await _streamService.StartAllAsync();
        AddLog("Start all cameras requested.");
    }

    private async Task StopAllAsync()
    {
        await _streamService.StopAllAsync();
        AddLog("Stop all cameras requested.");
    }

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

    private void OnCameraRuntimeStateChanged(object? sender, CameraRuntimeState state)
    {
        // 상태 스토어 갱신은 IVmsDashboardStateService가 담당.
        // 여기서는 WPF UI 로그/상태 메시지만 처리.
        DispatchToUi(() => AddLogCore(state.LastMessage));
    }

    private void AddLog(string message)
    {
        DispatchToUi(() => AddLogCore(message));
    }

    private void AddLogCore(string message)
    {
        StatusMessage = message;
        Logs.Insert(0, new VmsLogItem { Message = message });
        while (Logs.Count > 300)
        {
            Logs.RemoveAt(Logs.Count - 1);
        }
    }

    private void ClearLogs()
    {
        Logs.Clear();
        AddLog("Logs cleared.");
    }

    private void RaiseOpenLiveTabRequested()
    {
        DispatchToUi(() => OpenLiveTabRequested?.Invoke(this, EventArgs.Empty));
    }

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

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// \brief 동기 ICommand 구현입니다.
/// </summary>
public sealed class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    /// <summary>
    /// \brief RelayCommand 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="execute">실행할 동작입니다.</param>
    /// <param name="canExecute">실행 가능 여부를 반환하는 조건입니다.</param>
    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged;

    /// <inheritdoc />
    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    /// <inheritdoc />
    public void Execute(object? parameter) => _execute();

    /// <summary>
    /// \brief CanExecuteChanged 이벤트를 UI Dispatcher에서 발생시킵니다.
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
/// \brief 비동기 ICommand 구현입니다.
/// </summary>
public sealed class AsyncRelayCommand : ICommand
{
    private readonly Func<Task> _execute;
    private readonly Func<bool>? _canExecute;
    private bool _isRunning;

    /// <summary>
    /// \brief AsyncRelayCommand 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="execute">실행할 비동기 작업입니다.</param>
    /// <param name="canExecute">실행 가능 여부를 반환하는 조건입니다.</param>
    public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged;

    /// <inheritdoc />
    public bool CanExecute(object? parameter) => !_isRunning && (_canExecute?.Invoke() ?? true);

    /// <inheritdoc />
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
    /// \brief CanExecuteChanged 이벤트를 UI Dispatcher에서 발생시킵니다.
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
