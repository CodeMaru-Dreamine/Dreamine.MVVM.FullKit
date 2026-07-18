using Codemaru.Services;
using Codemaru.States;
using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;
using System.Collections.ObjectModel;
using System.Windows;

namespace Codemaru.ViewModels;

/// <summary>
/// \if KO
/// <para>Main Window View Model 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates main window view model functionality and related state.</para>
/// \endif
/// </summary>
public sealed partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    /// <summary>
    /// \if KO
    /// <para>session 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the session value.</para>
    /// \endif
    /// </summary>
    private readonly CardHybridSession _session;
    /// <summary>
    /// \if KO
    /// <para>certificate Monitor 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the certificate monitor value.</para>
    /// \endif
    /// </summary>
    private readonly CertificateMonitorViewModel _certificateMonitor;
    /// <summary>
    /// \if KO
    /// <para>disposed 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the disposed value.</para>
    /// \endif
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// \if KO
    /// <para>title 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the title value.</para>
    /// \endif
    /// </summary>
    [DreamineProperty] private string _title = "Codemaru";
    /// <summary>
    /// \if KO
    /// <para>brand 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the brand value.</para>
    /// \endif
    /// </summary>
    [DreamineProperty] private string _brand = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>owner Name 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the owner name value.</para>
    /// \endif
    /// </summary>
    [DreamineProperty] private string _ownerName = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>landing Url 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the landing url value.</para>
    /// \endif
    /// </summary>
    [DreamineProperty] private string _landingUrl = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>current User 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the current user value.</para>
    /// \endif
    /// </summary>
    [DreamineProperty] private string _currentUser = "Guest";
    /// <summary>
    /// \if KO
    /// <para>status Message 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the status message value.</para>
    /// \endif
    /// </summary>
    [DreamineProperty] private string _statusMessage = "명함 랜딩 페이지 준비 중";
    /// <summary>
    /// \if KO
    /// <para>history Count 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the history count value.</para>
    /// \endif
    /// </summary>
    [DreamineProperty] private int _historyCount;
    /// <summary>
    /// \if KO
    /// <para>logs 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the logs value.</para>
    /// \endif
    /// </summary>
    [DreamineProperty] private ObservableCollection<string> _logs = new();

    /// <summary>
    /// \if KO
    /// <para>Certificate Monitor 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the certificate monitor value.</para>
    /// \endif
    /// </summary>
    public CertificateMonitorViewModel CertificateMonitor => _certificateMonitor;

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="MainWindowViewModel"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="MainWindowViewModel"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="session">
    /// \if KO
    /// <para>session에 사용할 <c>CardHybridSession</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CardHybridSession</c> value used for session.</para>
    /// \endif
    /// </param>
    /// <param name="certificateMonitor">
    /// \if KO
    /// <para>certificate Monitor에 사용할 <c>CertificateMonitorViewModel</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CertificateMonitorViewModel</c> value used for certificate monitor.</para>
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
    public MainWindowViewModel(CardHybridSession session, CertificateMonitorViewModel certificateMonitor)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _certificateMonitor = certificateMonitor ?? throw new ArgumentNullException(nameof(certificateMonitor));
        _session.StateChanged += OnSessionChanged;
        ApplyState(_session.State);
        Logs.Add($"[{DateTime.Now:HH:mm:ss}] CardHybrid started");
    }

    /// <summary>
    /// \if KO
    /// <para>Session Changed 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the session changed event or state change.</para>
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
    /// <para>state에 사용할 <c>CardHybridState</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CardHybridState</c> value used for state.</para>
    /// \endif
    /// </param>
    private void OnSessionChanged(object? sender, CardHybridState state)
    {
        _ = RunOnUiAsync(() => ApplyState(state));
    }

    /// <summary>
    /// \if KO
    /// <para>Apply State 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the apply state operation.</para>
    /// \endif
    /// </summary>
    /// <param name="state">
    /// \if KO
    /// <para>state에 사용할 <c>CardHybridState</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CardHybridState</c> value used for state.</para>
    /// \endif
    /// </param>
    private void ApplyState(CardHybridState state)
    {
        Brand = state.Profile.Brand;
        OwnerName = state.Profile.Name;
        LandingUrl = state.Profile.LandingUrl;
        CurrentUser = "Guest";
        HistoryCount = state.History.Count;
        StatusMessage = $"{state.Profile.Name} / {state.Profile.Role}";
        Logs.Add($"[{DateTime.Now:HH:mm:ss}] {state.Profile.LandingSlug} updated, history {HistoryCount}");
    }

    /// <summary>
    /// \if KO
    /// <para>Run On Ui Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the run on ui async operation.</para>
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
    /// <returns>
    /// \if KO
    /// <para>Run On Ui Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the run on ui async operation.</para>
    /// \endif
    /// </returns>
    private static Task RunOnUiAsync(Action action)
    {
        if (Application.Current?.Dispatcher is not { } dispatcher)
        {
            action();
            return Task.CompletedTask;
        }

        if (dispatcher.CheckAccess())
        {
            action();
            return Task.CompletedTask;
        }

        return dispatcher.InvokeAsync(action).Task;
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
        if (_disposed)
        {
            return;
        }

        _session.StateChanged -= OnSessionChanged;
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
