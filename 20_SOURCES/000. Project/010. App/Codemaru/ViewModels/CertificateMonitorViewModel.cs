using Codemaru.Models.Certificates;
using Codemaru.Options;
using Codemaru.Services.Certificates;
using Microsoft.Extensions.Options;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;

namespace Codemaru.ViewModels;

/// <summary>
/// \if KO
/// <para>\brief 인증서 상태, win-acme 갱신, nginx reload를 제어하는 ViewModel입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates certificate monitor view model functionality and related state.</para>
/// \endif
/// </summary>
public sealed class CertificateMonitorViewModel : INotifyPropertyChanged
{
    /// <summary>
    /// \if KO
    /// <para>certificate Monitor Service 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the certificate monitor service value.</para>
    /// \endif
    /// </summary>
    private readonly ICertificateMonitorService _certificateMonitorService;
    /// <summary>
    /// \if KO
    /// <para>win Acme Service 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the win acme service value.</para>
    /// \endif
    /// </summary>
    private readonly IWinAcmeService _winAcmeService;
    /// <summary>
    /// \if KO
    /// <para>nginx Reload Service 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the nginx reload service value.</para>
    /// \endif
    /// </summary>
    private readonly INginxReloadService _nginxReloadService;
    /// <summary>
    /// \if KO
    /// <para>settings Writer 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the settings writer value.</para>
    /// \endif
    /// </summary>
    private readonly ICertificateSettingsWriter _settingsWriter;
    /// <summary>
    /// \if KO
    /// <para>base Options 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the base options value.</para>
    /// \endif
    /// </summary>
    private readonly CertificateMonitorOptions _baseOptions;
    /// <summary>
    /// \if KO
    /// <para>certificate Directory 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the certificate directory value.</para>
    /// \endif
    /// </summary>
    private string _certificateDirectory;
    /// <summary>
    /// \if KO
    /// <para>wacs Path 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the wacs path value.</para>
    /// \endif
    /// </summary>
    private string _wacsPath;
    /// <summary>
    /// \if KO
    /// <para>nginx Path 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the nginx path value.</para>
    /// \endif
    /// </summary>
    private string _nginxPath;
    /// <summary>
    /// \if KO
    /// <para>nginx Working Directory 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the nginx working directory value.</para>
    /// \endif
    /// </summary>
    private string _nginxWorkingDirectory;
    /// <summary>
    /// \if KO
    /// <para>status Message 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the status message value.</para>
    /// \endif
    /// </summary>
    private string _statusMessage = "Certificate monitor is ready.";
    /// <summary>
    /// \if KO
    /// <para>certificate Path 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the certificate path value.</para>
    /// \endif
    /// </summary>
    private string _certificatePath = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>subject 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the subject value.</para>
    /// \endif
    /// </summary>
    private string _subject = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>issuer 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the issuer value.</para>
    /// \endif
    /// </summary>
    private string _issuer = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>not After Text 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the not after text value.</para>
    /// \endif
    /// </summary>
    private string _notAfterText = "-";
    /// <summary>
    /// \if KO
    /// <para>remaining Days Text 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the remaining days text value.</para>
    /// \endif
    /// </summary>
    private string _remainingDaysText = "-";
    /// <summary>
    /// \if KO
    /// <para>health Text 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the health text value.</para>
    /// \endif
    /// </summary>
    private string _healthText = "Unknown";
    /// <summary>
    /// \if KO
    /// <para>health Brush 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the health brush value.</para>
    /// \endif
    /// </summary>
    private Brush _healthBrush = Brushes.Gray;
    /// <summary>
    /// \if KO
    /// <para>task Name 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the task name value.</para>
    /// \endif
    /// </summary>
    private string _taskName = "-";
    /// <summary>
    /// \if KO
    /// <para>task State 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the task state value.</para>
    /// \endif
    /// </summary>
    private string _taskState = "-";
    /// <summary>
    /// \if KO
    /// <para>last Run Time 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the last run time value.</para>
    /// \endif
    /// </summary>
    private string _lastRunTime = "-";
    /// <summary>
    /// \if KO
    /// <para>next Run Time 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the next run time value.</para>
    /// \endif
    /// </summary>
    private string _nextRunTime = "-";
    /// <summary>
    /// \if KO
    /// <para>last Task Result 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the last task result value.</para>
    /// \endif
    /// </summary>
    private string _lastTaskResult = "-";
    /// <summary>
    /// \if KO
    /// <para>command Output 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the command output value.</para>
    /// \endif
    /// </summary>
    private string _commandOutput = string.Empty;

    /// <summary>
    /// \if KO
    /// <para>\brief CertificateMonitorViewModel 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="CertificateMonitorViewModel"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="options">
    /// \if KO
    /// <para>인증서 모니터링 옵션입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The options that configure the operation.</para>
    /// \endif
    /// </param>
    /// <param name="certificateMonitorService">
    /// \if KO
    /// <para>인증서 상태 조회 서비스입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ICertificateMonitorService</c> value used for certificate monitor service.</para>
    /// \endif
    /// </param>
    /// <param name="winAcmeService">
    /// \if KO
    /// <para>win-acme 서비스입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IWinAcmeService</c> value used for win acme service.</para>
    /// \endif
    /// </param>
    /// <param name="nginxReloadService">
    /// \if KO
    /// <para>nginx reload 서비스입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>INginxReloadService</c> value used for nginx reload service.</para>
    /// \endif
    /// </param>
    /// <param name="settingsWriter">
    /// \if KO
    /// <para>로컬 설정 저장 서비스입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ICertificateSettingsWriter</c> value used for settings writer.</para>
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
    public CertificateMonitorViewModel(
        IOptions<CertificateMonitorOptions> options,
        ICertificateMonitorService certificateMonitorService,
        IWinAcmeService winAcmeService,
        INginxReloadService nginxReloadService,
        ICertificateSettingsWriter settingsWriter)
    {
        CertificateMonitorOptions value = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _certificateMonitorService = certificateMonitorService ?? throw new ArgumentNullException(nameof(certificateMonitorService));
        _winAcmeService = winAcmeService ?? throw new ArgumentNullException(nameof(winAcmeService));
        _nginxReloadService = nginxReloadService ?? throw new ArgumentNullException(nameof(nginxReloadService));
        _settingsWriter = settingsWriter ?? throw new ArgumentNullException(nameof(settingsWriter));
        _baseOptions = CloneOptions(value);

        _certificateDirectory = value.CertificateDirectory;
        _wacsPath = value.WacsPath;
        _nginxPath = value.NginxPath;
        _nginxWorkingDirectory = value.NginxWorkingDirectory;
        WarningDays = value.WarningDays;
        CriticalDays = value.CriticalDays;
        MaxCommandOutputChars = value.MaxCommandOutputChars;
        PfxPassword = value.PfxPassword ?? string.Empty;

        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        CheckRenewalTaskCommand = new AsyncRelayCommand(CheckRenewalTaskAsync);
        RunRenewCommand = new AsyncRelayCommand(() => RunRenewAsync(force: false));
        ForceRenewCommand = new AsyncRelayCommand(() => RunRenewAsync(force: true));
        ReloadNginxCommand = new AsyncRelayCommand(ReloadNginxAsync);
        SaveSettingsCommand = new AsyncRelayCommand(SaveSettingsAsync);
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
    /// <para>\brief 인증서 폴더 경로입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the certificate directory value.</para>
    /// \endif
    /// </summary>
    public string CertificateDirectory
    {
        get => _certificateDirectory;
        set => SetField(ref _certificateDirectory, value);
    }

    /// <summary>
    /// \if KO
    /// <para>\brief win-acme 실행 파일 경로입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the wacs path value.</para>
    /// \endif
    /// </summary>
    public string WacsPath
    {
        get => _wacsPath;
        set => SetField(ref _wacsPath, value);
    }

    /// <summary>
    /// \if KO
    /// <para>\brief nginx 실행 파일 경로입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the nginx path value.</para>
    /// \endif
    /// </summary>
    public string NginxPath
    {
        get => _nginxPath;
        set => SetField(ref _nginxPath, value);
    }

    /// <summary>
    /// \if KO
    /// <para>\brief nginx 작업 폴더입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the nginx working directory value.</para>
    /// \endif
    /// </summary>
    public string NginxWorkingDirectory
    {
        get => _nginxWorkingDirectory;
        set => SetField(ref _nginxWorkingDirectory, value);
    }

    /// <summary>
    /// \if KO
    /// <para>\brief PFX 인증서 암호입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the pfx password value.</para>
    /// \endif
    /// </summary>
    public string PfxPassword { get; set; }

    /// <summary>
    /// \if KO
    /// <para>\brief Warning 기준 남은 일수입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the warning days value.</para>
    /// \endif
    /// </summary>
    public int WarningDays { get; set; }

    /// <summary>
    /// \if KO
    /// <para>\brief Critical 기준 남은 일수입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the critical days value.</para>
    /// \endif
    /// </summary>
    public int CriticalDays { get; set; }

    /// <summary>
    /// \if KO
    /// <para>\brief 외부 명령 출력 최대 길이입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the max command output chars value.</para>
    /// \endif
    /// </summary>
    public int MaxCommandOutputChars { get; set; }

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
        private set => SetField(ref _statusMessage, value);
    }

    /// <summary>
    /// \if KO
    /// <para>\brief 선택된 인증서 파일 경로입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the certificate path value.</para>
    /// \endif
    /// </summary>
    public string CertificatePath
    {
        get => _certificatePath;
        private set => SetField(ref _certificatePath, value);
    }

    /// <summary>
    /// \if KO
    /// <para>\brief 인증서 주체입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the subject value.</para>
    /// \endif
    /// </summary>
    public string Subject
    {
        get => _subject;
        private set => SetField(ref _subject, value);
    }

    /// <summary>
    /// \if KO
    /// <para>\brief 인증서 발급자입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the issuer value.</para>
    /// \endif
    /// </summary>
    public string Issuer
    {
        get => _issuer;
        private set => SetField(ref _issuer, value);
    }

    /// <summary>
    /// \if KO
    /// <para>\brief 인증서 만료 시간 표시 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the not after text value.</para>
    /// \endif
    /// </summary>
    public string NotAfterText
    {
        get => _notAfterText;
        private set => SetField(ref _notAfterText, value);
    }

    /// <summary>
    /// \if KO
    /// <para>\brief 인증서 남은 기간 표시 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the remaining days text value.</para>
    /// \endif
    /// </summary>
    public string RemainingDaysText
    {
        get => _remainingDaysText;
        private set => SetField(ref _remainingDaysText, value);
    }

    /// <summary>
    /// \if KO
    /// <para>\brief 인증서 상태 표시 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the health text value.</para>
    /// \endif
    /// </summary>
    public string HealthText
    {
        get => _healthText;
        private set => SetField(ref _healthText, value);
    }

    /// <summary>
    /// \if KO
    /// <para>\brief 인증서 상태 표시 Brush입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the health brush value.</para>
    /// \endif
    /// </summary>
    public Brush HealthBrush
    {
        get => _healthBrush;
        private set => SetField(ref _healthBrush, value);
    }

    /// <summary>
    /// \if KO
    /// <para>\brief win-acme 예약 작업 이름입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the task name value.</para>
    /// \endif
    /// </summary>
    public string TaskName
    {
        get => _taskName;
        private set => SetField(ref _taskName, value);
    }

    /// <summary>
    /// \if KO
    /// <para>\brief win-acme 예약 작업 상태입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the task state value.</para>
    /// \endif
    /// </summary>
    public string TaskState
    {
        get => _taskState;
        private set => SetField(ref _taskState, value);
    }

    /// <summary>
    /// \if KO
    /// <para>\brief 예약 작업 마지막 실행 시간입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the last run time value.</para>
    /// \endif
    /// </summary>
    public string LastRunTime
    {
        get => _lastRunTime;
        private set => SetField(ref _lastRunTime, value);
    }

    /// <summary>
    /// \if KO
    /// <para>\brief 예약 작업 다음 실행 시간입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the next run time value.</para>
    /// \endif
    /// </summary>
    public string NextRunTime
    {
        get => _nextRunTime;
        private set => SetField(ref _nextRunTime, value);
    }

    /// <summary>
    /// \if KO
    /// <para>\brief 예약 작업 마지막 실행 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the last task result value.</para>
    /// \endif
    /// </summary>
    public string LastTaskResult
    {
        get => _lastTaskResult;
        private set => SetField(ref _lastTaskResult, value);
    }

    /// <summary>
    /// \if KO
    /// <para>\brief 최근 외부 명령 출력입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the command output value.</para>
    /// \endif
    /// </summary>
    public string CommandOutput
    {
        get => _commandOutput;
        private set => SetField(ref _commandOutput, value);
    }

    /// <summary>
    /// \if KO
    /// <para>\brief 인증서 상태 새로고침 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the refresh command value.</para>
    /// \endif
    /// </summary>
    public ICommand RefreshCommand { get; }

    /// <summary>
    /// \if KO
    /// <para>\brief win-acme 예약 작업 조회 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the check renewal task command value.</para>
    /// \endif
    /// </summary>
    public ICommand CheckRenewalTaskCommand { get; }

    /// <summary>
    /// \if KO
    /// <para>\brief win-acme 일반 갱신 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the run renew command value.</para>
    /// \endif
    /// </summary>
    public ICommand RunRenewCommand { get; }

    /// <summary>
    /// \if KO
    /// <para>\brief win-acme 강제 갱신 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the force renew command value.</para>
    /// \endif
    /// </summary>
    public ICommand ForceRenewCommand { get; }

    /// <summary>
    /// \if KO
    /// <para>\brief nginx reload 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the reload nginx command value.</para>
    /// \endif
    /// </summary>
    public ICommand ReloadNginxCommand { get; }

    /// <summary>
    /// \if KO
    /// <para>\brief 로컬 설정 저장 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the save settings command value.</para>
    /// \endif
    /// </summary>
    public ICommand SaveSettingsCommand { get; }

    /// <summary>
    /// \if KO
    /// <para>Refresh Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the refresh async operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Refresh Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the refresh async operation.</para>
    /// \endif
    /// </returns>
    private async Task RefreshAsync()
    {
        CertificateStatusInfo status = await _certificateMonitorService
            .GetStatusAsync(CreateOptions(), CancellationToken.None)
            .ConfigureAwait(true);

        CertificatePath = string.IsNullOrWhiteSpace(status.CertificatePath) ? "-" : status.CertificatePath;
        Subject = string.IsNullOrWhiteSpace(status.Subject) ? "-" : status.Subject;
        Issuer = string.IsNullOrWhiteSpace(status.Issuer) ? "-" : status.Issuer;
        NotAfterText = status.NotAfter?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-";
        RemainingDaysText = status.RemainingDays.HasValue ? $"{status.RemainingDays.Value} days" : "-";
        HealthText = status.State.ToString();
        HealthBrush = ResolveBrush(status.State);
        StatusMessage = status.Message;
    }

    /// <summary>
    /// \if KO
    /// <para>Check Renewal Task Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the check renewal task async operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Check Renewal Task Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the check renewal task async operation.</para>
    /// \endif
    /// </returns>
    private async Task CheckRenewalTaskAsync()
    {
        WinAcmeTaskInfo task = await _winAcmeService
            .GetRenewalTaskAsync(CancellationToken.None)
            .ConfigureAwait(true);

        TaskName = FormatTaskName(task);
        TaskState = string.IsNullOrWhiteSpace(task.State) ? "-" : task.State;
        LastRunTime = string.IsNullOrWhiteSpace(task.LastRunTime) ? "-" : task.LastRunTime;
        NextRunTime = string.IsNullOrWhiteSpace(task.NextRunTime) ? "-" : task.NextRunTime;
        LastTaskResult = string.IsNullOrWhiteSpace(task.LastTaskResult) ? "-" : task.LastTaskResult;
        StatusMessage = task.Message;
    }

    /// <summary>
    /// \if KO
    /// <para>Run Renew Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the run renew async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="force">
    /// \if KO
    /// <para>force에 사용할 <c>bool</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>bool</c> value used for force.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Run Renew Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the run renew async operation.</para>
    /// \endif
    /// </returns>
    private async Task RunRenewAsync(bool force)
    {
        ProcessExecutionResult result = await _winAcmeService
            .RunRenewAsync(CreateOptions(), force, CancellationToken.None)
            .ConfigureAwait(true);

        ApplyProcessResult(force ? "Force renew" : "Renew", result);
        await RefreshAsync().ConfigureAwait(true);
    }

    /// <summary>
    /// \if KO
    /// <para>Reload Nginx Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the reload nginx async operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Reload Nginx Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the reload nginx async operation.</para>
    /// \endif
    /// </returns>
    private async Task ReloadNginxAsync()
    {
        ProcessExecutionResult result = await _nginxReloadService
            .ReloadAsync(CreateOptions(), CancellationToken.None)
            .ConfigureAwait(true);

        ApplyProcessResult("Nginx reload", result);
    }

    /// <summary>
    /// \if KO
    /// <para>Settings Async 데이터를 저장합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Saves settings async data.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Save Settings Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the save settings async operation.</para>
    /// \endif
    /// </returns>
    private async Task SaveSettingsAsync()
    {
        await _settingsWriter.SaveAsync(CreateOptions(), CancellationToken.None).ConfigureAwait(true);
        StatusMessage = "Certificate settings saved to appsettings.local.json.";
    }

    /// <summary>
    /// \if KO
    /// <para>Apply Process Result 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the apply process result operation.</para>
    /// \endif
    /// </summary>
    /// <param name="actionName">
    /// \if KO
    /// <para>action Name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for action name.</para>
    /// \endif
    /// </param>
    /// <param name="result">
    /// \if KO
    /// <para>result에 사용할 <c>ProcessExecutionResult</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ProcessExecutionResult</c> value used for result.</para>
    /// \endif
    /// </param>
    private void ApplyProcessResult(string actionName, ProcessExecutionResult result)
    {
        StatusMessage = $"{actionName}: {result.Message}";
        CommandOutput = string.Join(Environment.NewLine, new[]
        {
            $"[{actionName}] ExitCode={result.ExitCode}, Success={result.IsSuccess}",
            result.Output,
            result.Error
        }.Where(line => !string.IsNullOrWhiteSpace(line)));
    }

    /// <summary>
    /// \if KO
    /// <para>Options 값을 생성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Creates the options value.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Create Options 작업에서 생성한 <c>CertificateMonitorOptions</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CertificateMonitorOptions</c> result produced by the create options operation.</para>
    /// \endif
    /// </returns>
    private CertificateMonitorOptions CreateOptions()
    {
        return new CertificateMonitorOptions
        {
            CertificateDirectory = CertificateDirectory,
            CertificateFilePatterns = _baseOptions.CertificateFilePatterns.ToArray(),
            PfxPassword = string.IsNullOrWhiteSpace(PfxPassword) ? null : PfxPassword,
            WacsPath = WacsPath,
            NginxPath = NginxPath,
            NginxWorkingDirectory = NginxWorkingDirectory,
            NginxReloadArguments = string.IsNullOrWhiteSpace(_baseOptions.NginxReloadArguments)
                ? "-s reload"
                : _baseOptions.NginxReloadArguments,
            WarningDays = Math.Max(1, WarningDays),
            CriticalDays = Math.Max(1, CriticalDays),
            MaxCommandOutputChars = MaxCommandOutputChars <= 0 ? 6000 : MaxCommandOutputChars
        };
    }

    /// <summary>
    /// \if KO
    /// <para>Clone Options 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the clone options operation.</para>
    /// \endif
    /// </summary>
    /// <param name="source">
    /// \if KO
    /// <para>source에 사용할 <c>CertificateMonitorOptions</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CertificateMonitorOptions</c> value used for source.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Clone Options 작업에서 생성한 <c>CertificateMonitorOptions</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CertificateMonitorOptions</c> result produced by the clone options operation.</para>
    /// \endif
    /// </returns>
    private static CertificateMonitorOptions CloneOptions(CertificateMonitorOptions source)
    {
        return new CertificateMonitorOptions
        {
            CertificateDirectory = source.CertificateDirectory,
            CertificateFilePatterns = source.CertificateFilePatterns.ToArray(),
            PfxPassword = source.PfxPassword,
            WacsPath = source.WacsPath,
            NginxPath = source.NginxPath,
            NginxWorkingDirectory = source.NginxWorkingDirectory,
            NginxReloadArguments = source.NginxReloadArguments,
            WarningDays = source.WarningDays,
            CriticalDays = source.CriticalDays,
            MaxCommandOutputChars = source.MaxCommandOutputChars
        };
    }

    /// <summary>
    /// \if KO
    /// <para>Format Task Name 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the format task name operation.</para>
    /// \endif
    /// </summary>
    /// <param name="task">
    /// \if KO
    /// <para>task에 사용할 <c>WinAcmeTaskInfo</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>WinAcmeTaskInfo</c> value used for task.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Format Task Name 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the format task name operation.</para>
    /// \endif
    /// </returns>
    private static string FormatTaskName(WinAcmeTaskInfo task)
    {
        if (string.IsNullOrWhiteSpace(task.TaskName))
        {
            return "-";
        }

        if (string.IsNullOrWhiteSpace(task.TaskPath) || task.TaskPath == @"\")
        {
            return task.TaskName;
        }

        return $"{task.TaskPath}{task.TaskName}";
    }

    /// <summary>
    /// \if KO
    /// <para>Resolve Brush 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the resolve brush operation.</para>
    /// \endif
    /// </summary>
    /// <param name="state">
    /// \if KO
    /// <para>state에 사용할 <c>CertificateHealthState</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CertificateHealthState</c> value used for state.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Resolve Brush 작업에서 생성한 <c>Brush</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Brush</c> result produced by the resolve brush operation.</para>
    /// \endif
    /// </returns>
    private static Brush ResolveBrush(CertificateHealthState state)
    {
        return state switch
        {
            CertificateHealthState.Ok => Brushes.ForestGreen,
            CertificateHealthState.Warning => Brushes.DarkOrange,
            CertificateHealthState.Critical => Brushes.OrangeRed,
            CertificateHealthState.Expired => Brushes.Firebrick,
            CertificateHealthState.Error => Brushes.Firebrick,
            _ => Brushes.Gray
        };
    }

    /// <summary>
    /// \if KO
    /// <para>Field 값을 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Sets the field value.</para>
    /// \endif
    /// </summary>
    /// <typeparam name="T">
    /// \if KO
    /// <para>T 형식 매개변수입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The T type parameter.</para>
    /// \endif
    /// </typeparam>
    /// <param name="field">
    /// \if KO
    /// <para>field에 사용할 <c>T</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>T</c> value used for field.</para>
    /// \endif
    /// </param>
    /// <param name="value">
    /// \if KO
    /// <para>적용할 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The value to apply.</para>
    /// \endif
    /// </param>
    /// <param name="propertyName">
    /// \if KO
    /// <para>property Name에 사용할 <c>string?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for property name.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Set Field 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the set field condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
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
