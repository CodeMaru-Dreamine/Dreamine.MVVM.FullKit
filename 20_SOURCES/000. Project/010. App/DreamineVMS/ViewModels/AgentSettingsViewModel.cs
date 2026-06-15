using DreamineVMS.Services.Agent;
using Microsoft.Extensions.Configuration;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace DreamineVMS.ViewModels;

/// <summary>에이전트 접속 정보(서버 URL·이메일·비밀번호)를 관리하는 ViewModel입니다.</summary>
public sealed class AgentSettingsViewModel : INotifyPropertyChanged
{
    private readonly AgentApiClient _api;
    private readonly AgentSettingsWriter _writer;

    private string _serverUrl  = "";
    private string _email      = "";
    private string _password   = "";
    private string _statusMessage = "저장된 설정을 불러왔습니다.";
    private bool   _isConnected   = false;

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

    public event PropertyChangedEventHandler? PropertyChanged;

    public string ServerUrl
    {
        get => _serverUrl;
        set => SetField(ref _serverUrl, value);
    }

    public string Email
    {
        get => _email;
        set => SetField(ref _email, value);
    }

    public string Password
    {
        get => _password;
        set => SetField(ref _password, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    public bool IsConnected
    {
        get => _isConnected;
        private set => SetField(ref _isConnected, value);
    }

    public ICommand SaveCommand      { get; }
    public ICommand TestCommand      { get; }
    public ICommand ReconnectCommand { get; }

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

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        return true;
    }
}
