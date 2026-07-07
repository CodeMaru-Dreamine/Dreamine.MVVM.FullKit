using DreamineVMS.Web.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace DreamineVMS.Web.Services.Auth;

/// <summary>
/// \brief Blazor 서킷(탭)마다 현재 로그인 사용자를 보유하는 Scoped 서비스입니다.
/// </summary>
public sealed class VmsAuthState
{
    private readonly VmsSessionService _sessions;
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly VmsUserService _users;
    private string? _token;

    public VmsUser? CurrentUser { get; private set; }
    public bool IsAuthenticated => CurrentUser is not null;

    public VmsAuthState(
        VmsSessionService sessions,
        AuthenticationStateProvider authenticationStateProvider,
        VmsUserService users)
    {
        _sessions = sessions;
        _authenticationStateProvider = authenticationStateProvider;
        _users = users;
    }

    /// <summary>페이지 초기화 시 공통 로그인 쿠키 또는 localStorage 토큰으로 세션을 복원합니다.</summary>
    public async Task RestoreAsync(IJSRuntime js)
    {
        if (IsAuthenticated) return;

        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        var sharedUser = await _users.EnsureExternalUserAsync(authState.User);
        if (sharedUser is not null)
        {
            CurrentUser = sharedUser;
            _token = _sessions.CreateSession(sharedUser);
            await js.InvokeVoidAsync("localStorage.setItem", "vms_session", _token);
            return;
        }

        try
        {
            var token = await js.InvokeAsync<string?>("localStorage.getItem", "vms_session");
            var user = _sessions.ValidateToken(token);
            if (user is not null)
            {
                _token = token;
                CurrentUser = user;
            }
        }
        catch { /* 서킷 아직 연결 안 된 경우 무시 */ }
    }

    /// <summary>로그인 성공 후 호출합니다.</summary>
    public async Task SignInAsync(IJSRuntime js, VmsUser user, string token)
    {
        _token = token;
        CurrentUser = user;
        await js.InvokeVoidAsync("localStorage.setItem", "vms_session", token);
    }

    /// <summary>로그아웃합니다.</summary>
    public async Task SignOutAsync(IJSRuntime js)
    {
        _sessions.RemoveSession(_token);
        _token = null;
        CurrentUser = null;
        await js.InvokeVoidAsync("localStorage.removeItem", "vms_session");
    }
}
