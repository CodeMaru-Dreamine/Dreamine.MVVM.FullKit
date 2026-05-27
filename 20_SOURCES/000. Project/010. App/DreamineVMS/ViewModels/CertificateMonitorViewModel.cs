using DreamineVMS.Models.Certificates;
using DreamineVMS.Options;
using DreamineVMS.Services.Certificates;
using Microsoft.Extensions.Options;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;

namespace DreamineVMS.ViewModels;

/// <summary>
/// \brief 인증서 상태, win-acme 갱신, nginx reload를 제어하는 ViewModel입니다.
/// </summary>
public sealed class CertificateMonitorViewModel : INotifyPropertyChanged
{
    private readonly ICertificateMonitorService _certificateMonitorService;
    private readonly IWinAcmeService _winAcmeService;
    private readonly INginxReloadService _nginxReloadService;
    private readonly ICertificateSettingsWriter _settingsWriter;
    private readonly CertificateMonitorOptions _baseOptions;
    private string _certificateDirectory;
    private string _wacsPath;
    private string _nginxPath;
    private string _nginxWorkingDirectory;
    private string _statusMessage = "Certificate monitor is ready.";
    private string _certificatePath = string.Empty;
    private string _subject = string.Empty;
    private string _issuer = string.Empty;
    private string _notAfterText = "-";
    private string _remainingDaysText = "-";
    private string _healthText = "Unknown";
    private Brush _healthBrush = Brushes.Gray;
    private string _taskName = "-";
    private string _taskState = "-";
    private string _lastRunTime = "-";
    private string _nextRunTime = "-";
    private string _lastTaskResult = "-";
    private string _commandOutput = string.Empty;

    /// <summary>
    /// \brief CertificateMonitorViewModel 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="options">인증서 모니터링 옵션입니다.</param>
    /// <param name="certificateMonitorService">인증서 상태 조회 서비스입니다.</param>
    /// <param name="winAcmeService">win-acme 서비스입니다.</param>
    /// <param name="nginxReloadService">nginx reload 서비스입니다.</param>
    /// <param name="settingsWriter">로컬 설정 저장 서비스입니다.</param>
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

    /// <summary>\brief 속성 변경 이벤트입니다.</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>\brief 인증서 폴더 경로입니다.</summary>
    public string CertificateDirectory
    {
        get => _certificateDirectory;
        set => SetField(ref _certificateDirectory, value);
    }

    /// <summary>\brief win-acme 실행 파일 경로입니다.</summary>
    public string WacsPath
    {
        get => _wacsPath;
        set => SetField(ref _wacsPath, value);
    }

    /// <summary>\brief nginx 실행 파일 경로입니다.</summary>
    public string NginxPath
    {
        get => _nginxPath;
        set => SetField(ref _nginxPath, value);
    }

    /// <summary>\brief nginx 작업 폴더입니다.</summary>
    public string NginxWorkingDirectory
    {
        get => _nginxWorkingDirectory;
        set => SetField(ref _nginxWorkingDirectory, value);
    }

    /// <summary>\brief PFX 인증서 암호입니다.</summary>
    public string PfxPassword { get; set; }

    /// <summary>\brief Warning 기준 남은 일수입니다.</summary>
    public int WarningDays { get; set; }

    /// <summary>\brief Critical 기준 남은 일수입니다.</summary>
    public int CriticalDays { get; set; }

    /// <summary>\brief 외부 명령 출력 최대 길이입니다.</summary>
    public int MaxCommandOutputChars { get; set; }

    /// <summary>\brief 상태 메시지입니다.</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    /// <summary>\brief 선택된 인증서 파일 경로입니다.</summary>
    public string CertificatePath
    {
        get => _certificatePath;
        private set => SetField(ref _certificatePath, value);
    }

    /// <summary>\brief 인증서 주체입니다.</summary>
    public string Subject
    {
        get => _subject;
        private set => SetField(ref _subject, value);
    }

    /// <summary>\brief 인증서 발급자입니다.</summary>
    public string Issuer
    {
        get => _issuer;
        private set => SetField(ref _issuer, value);
    }

    /// <summary>\brief 인증서 만료 시간 표시 문자열입니다.</summary>
    public string NotAfterText
    {
        get => _notAfterText;
        private set => SetField(ref _notAfterText, value);
    }

    /// <summary>\brief 인증서 남은 기간 표시 문자열입니다.</summary>
    public string RemainingDaysText
    {
        get => _remainingDaysText;
        private set => SetField(ref _remainingDaysText, value);
    }

    /// <summary>\brief 인증서 상태 표시 문자열입니다.</summary>
    public string HealthText
    {
        get => _healthText;
        private set => SetField(ref _healthText, value);
    }

    /// <summary>\brief 인증서 상태 표시 Brush입니다.</summary>
    public Brush HealthBrush
    {
        get => _healthBrush;
        private set => SetField(ref _healthBrush, value);
    }

    /// <summary>\brief win-acme 예약 작업 이름입니다.</summary>
    public string TaskName
    {
        get => _taskName;
        private set => SetField(ref _taskName, value);
    }

    /// <summary>\brief win-acme 예약 작업 상태입니다.</summary>
    public string TaskState
    {
        get => _taskState;
        private set => SetField(ref _taskState, value);
    }

    /// <summary>\brief 예약 작업 마지막 실행 시간입니다.</summary>
    public string LastRunTime
    {
        get => _lastRunTime;
        private set => SetField(ref _lastRunTime, value);
    }

    /// <summary>\brief 예약 작업 다음 실행 시간입니다.</summary>
    public string NextRunTime
    {
        get => _nextRunTime;
        private set => SetField(ref _nextRunTime, value);
    }

    /// <summary>\brief 예약 작업 마지막 실행 결과입니다.</summary>
    public string LastTaskResult
    {
        get => _lastTaskResult;
        private set => SetField(ref _lastTaskResult, value);
    }

    /// <summary>\brief 최근 외부 명령 출력입니다.</summary>
    public string CommandOutput
    {
        get => _commandOutput;
        private set => SetField(ref _commandOutput, value);
    }

    /// <summary>\brief 인증서 상태 새로고침 명령입니다.</summary>
    public ICommand RefreshCommand { get; }

    /// <summary>\brief win-acme 예약 작업 조회 명령입니다.</summary>
    public ICommand CheckRenewalTaskCommand { get; }

    /// <summary>\brief win-acme 일반 갱신 명령입니다.</summary>
    public ICommand RunRenewCommand { get; }

    /// <summary>\brief win-acme 강제 갱신 명령입니다.</summary>
    public ICommand ForceRenewCommand { get; }

    /// <summary>\brief nginx reload 명령입니다.</summary>
    public ICommand ReloadNginxCommand { get; }

    /// <summary>\brief 로컬 설정 저장 명령입니다.</summary>
    public ICommand SaveSettingsCommand { get; }

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

    private async Task RunRenewAsync(bool force)
    {
        ProcessExecutionResult result = await _winAcmeService
            .RunRenewAsync(CreateOptions(), force, CancellationToken.None)
            .ConfigureAwait(true);

        ApplyProcessResult(force ? "Force renew" : "Renew", result);
        await RefreshAsync().ConfigureAwait(true);
    }

    private async Task ReloadNginxAsync()
    {
        ProcessExecutionResult result = await _nginxReloadService
            .ReloadAsync(CreateOptions(), CancellationToken.None)
            .ConfigureAwait(true);

        ApplyProcessResult("Nginx reload", result);
    }

    private async Task SaveSettingsAsync()
    {
        await _settingsWriter.SaveAsync(CreateOptions(), CancellationToken.None).ConfigureAwait(true);
        StatusMessage = "Certificate settings saved to appsettings.local.json.";
    }

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

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
