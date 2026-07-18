using DreamineVMS.Services.Agent;
using Microsoft.Extensions.Configuration;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace DreamineVMS.ViewModels;

/// <summary>
/// \if KO
/// <para>에이전트 접속 정보(서버 URL·이메일·비밀번호)를 관리하는 ViewModel입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates agent settings view model functionality and related state.</para>
/// \endif
/// </summary>
public sealed class AgentSettingsViewModel : INotifyPropertyChanged
{
    /// <summary>
    /// \if KO
    /// <para>api 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the api value.</para>
    /// \endif
    /// </summary>
    private readonly AgentApiClient _api;
    /// <summary>
    /// \if KO
    /// <para>writer 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the writer value.</para>
    /// \endif
    /// </summary>
    private readonly AgentSettingsWriter _writer;

    /// <summary>
    /// \if KO
    /// <para>server Url 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the server url value.</para>
    /// \endif
    /// </summary>
    private string _serverUrl  = "";
    /// <summary>
    /// \if KO
    /// <para>email 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the email value.</para>
    /// \endif
    /// </summary>
    private string _email      = "";
    /// <summary>
    /// \if KO
    /// <para>password 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the password value.</para>
    /// \endif
    /// </summary>
    private string _password   = "";
    /// <summary>
    /// \if KO
    /// <para>status Message 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the status message value.</para>
    /// \endif
    /// </summary>
    private string _statusMessage = "저장된 설정을 불러왔습니다.";
    /// <summary>
    /// \if KO
    /// <para>is Connected 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the is connected value.</para>
    /// \endif
    /// </summary>
    private bool   _isConnected   = false;

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="AgentSettingsViewModel"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="AgentSettingsViewModel"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="config">
    /// \if KO
    /// <para>config에 사용할 <c>IConfiguration</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IConfiguration</c> value used for config.</para>
    /// \endif
    /// </param>
    /// <param name="api">
    /// \if KO
    /// <para>api에 사용할 <c>AgentApiClient</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>AgentApiClient</c> value used for api.</para>
    /// \endif
    /// </param>
    /// <param name="writer">
    /// \if KO
    /// <para>writer에 사용할 <c>AgentSettingsWriter</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>AgentSettingsWriter</c> value used for writer.</para>
    /// \endif
    /// </param>
    public AgentSettingsViewModel(IConfiguration config, AgentApiClient api, AgentSettingsWriter writer)
    {
        _api    = api;
        _writer = writer;

        // 현재 설정값으로 초기화
        _serverUrl = config["Agent:ServerUrl"] ?? "http://cctvviewer.codemaru.co.kr";
        _email     = config["Agent:Email"]     ?? "";
        _password  = config["Agent:Password"]  ?? "";

        SaveCommand      = new AsyncRelayCommand(SaveAsync);
        TestCommand      = new AsyncRelayCommand(TestConnectionAsync);
        ReconnectCommand = new AsyncRelayCommand(ReconnectAsync);

        // 현재 연결 상태 반영
        _isConnected = _api.Token is not null;
        _statusMessage = _isConnected ? $"연결됨 — 토큰 보유 중" : "미연결 — 저장 후 '재연결'을 눌러주세요.";
    }

    /// <summary>
    /// \if KO
    /// <para>Property Changed 상황이 발생할 때 알립니다.</para>
    /// \endif
    /// \if EN
    /// <para>Occurs when property changed takes place.</para>
    /// \endif
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// \if KO
    /// <para>Server Url 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the server url value.</para>
    /// \endif
    /// </summary>
    public string ServerUrl
    {
        get => _serverUrl;
        set => SetField(ref _serverUrl, value);
    }

    /// <summary>
    /// \if KO
    /// <para>Email 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the email value.</para>
    /// \endif
    /// </summary>
    public string Email
    {
        get => _email;
        set => SetField(ref _email, value);
    }

    /// <summary>
    /// \if KO
    /// <para>Password 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the password value.</para>
    /// \endif
    /// </summary>
    public string Password
    {
        get => _password;
        set => SetField(ref _password, value);
    }

    /// <summary>
    /// \if KO
    /// <para>Status Message 값을 가져오거나 설정합니다.</para>
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
    /// <para>Is Connected 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the is connected value.</para>
    /// \endif
    /// </summary>
    public bool IsConnected
    {
        get => _isConnected;
        private set => SetField(ref _isConnected, value);
    }

    /// <summary>
    /// \if KO
    /// <para>Save Command 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the save command value.</para>
    /// \endif
    /// </summary>
    public ICommand SaveCommand      { get; }
    /// <summary>
    /// \if KO
    /// <para>Test Command 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the test command value.</para>
    /// \endif
    /// </summary>
    public ICommand TestCommand      { get; }
    /// <summary>
    /// \if KO
    /// <para>Reconnect Command 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the reconnect command value.</para>
    /// \endif
    /// </summary>
    public ICommand ReconnectCommand { get; }

    /// <summary>
    /// \if KO
    /// <para>Async 데이터를 저장합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Saves async data.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Save Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the save async operation.</para>
    /// \endif
    /// </returns>
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(ServerUrl))
        {
            StatusMessage = "서버 URL을 입력하세요.";
            return;
        }
        if (string.IsNullOrWhiteSpace(Email))
        {
            StatusMessage = "이메일을 입력하세요.";
            return;
        }
        if (string.IsNullOrWhiteSpace(Password))
        {
            StatusMessage = "비밀번호를 입력하세요.";
            return;
        }

        try
        {
            await _writer.SaveAsync(ServerUrl.Trim(), Email.Trim(), Password, CancellationToken.None)
                         .ConfigureAwait(true);

            // 실행 중인 ApiClient에도 즉시 반영
            _api.SetCredentials(ServerUrl.Trim(), Email.Trim(), Password);
            IsConnected = false;
            StatusMessage = "설정을 저장했습니다. '재연결'을 눌러 서버에 연결하세요.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"저장 실패: {ex.Message}";
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Test Connection Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the test connection async operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Test Connection Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the test connection async operation.</para>
    /// \endif
    /// </returns>
    private async Task TestConnectionAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            StatusMessage = "이메일과 비밀번호를 먼저 입력하세요.";
            return;
        }

        StatusMessage = "서버에 연결 테스트 중...";
        var (ok, err) = await _api.LoginAsync(Email.Trim(), Password).ConfigureAwait(true);
        IsConnected = ok;
        StatusMessage = ok
            ? $"연결 성공! 서버: {_api.ServerUrl}"
            : $"연결 실패: {err}";
    }

    /// <summary>
    /// \if KO
    /// <para>Reconnect Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the reconnect async operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Reconnect Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the reconnect async operation.</para>
    /// \endif
    /// </returns>
    private async Task ReconnectAsync()
    {
        StatusMessage = "재연결 시도 중...";
        IsConnected = false;
        _api.ClearToken();

        var (ok, err) = await _api.LoginWithStoredCredentialsAsync().ConfigureAwait(true);
        IsConnected = ok;
        StatusMessage = ok
            ? $"재연결 성공! 서버: {_api.ServerUrl}"
            : $"재연결 실패: {err}";
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
    /// <param name="name">
    /// \if KO
    /// <para>name에 사용할 <c>string?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for name.</para>
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
    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        return true;
    }
}
