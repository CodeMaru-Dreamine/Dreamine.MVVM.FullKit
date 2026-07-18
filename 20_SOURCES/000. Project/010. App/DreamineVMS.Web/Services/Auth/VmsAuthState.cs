using DreamineVMS.Web.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace DreamineVMS.Web.Services.Auth;

/// <summary>
/// \if KO
/// <para>\brief Blazor 서킷(탭)마다 현재 로그인 사용자를 보유하는 Scoped 서비스입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates vms auth state functionality and related state.</para>
/// \endif
/// </summary>
public sealed class VmsAuthState
{
    /// <summary>
    /// \if KO
    /// <para>sessions 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the sessions value.</para>
    /// \endif
    /// </summary>
    private readonly VmsSessionService _sessions;
    /// <summary>
    /// \if KO
    /// <para>authentication State Provider 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the authentication state provider value.</para>
    /// \endif
    /// </summary>
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    /// <summary>
    /// \if KO
    /// <para>users 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the users value.</para>
    /// \endif
    /// </summary>
    private readonly VmsUserService _users;
    /// <summary>
    /// \if KO
    /// <para>token 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the token value.</para>
    /// \endif
    /// </summary>
    private string? _token;

    /// <summary>
    /// \if KO
    /// <para>Current User 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the current user value.</para>
    /// \endif
    /// </summary>
    public VmsUser? CurrentUser { get; private set; }
    /// <summary>
    /// \if KO
    /// <para>Is Authenticated 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the is authenticated value.</para>
    /// \endif
    /// </summary>
    public bool IsAuthenticated => CurrentUser is not null;

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="VmsAuthState"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="VmsAuthState"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="sessions">
    /// \if KO
    /// <para>sessions에 사용할 <c>VmsSessionService</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>VmsSessionService</c> value used for sessions.</para>
    /// \endif
    /// </param>
    /// <param name="authenticationStateProvider">
    /// \if KO
    /// <para>authentication State Provider에 사용할 <c>AuthenticationStateProvider</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>AuthenticationStateProvider</c> value used for authentication state provider.</para>
    /// \endif
    /// </param>
    /// <param name="users">
    /// \if KO
    /// <para>users에 사용할 <c>VmsUserService</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>VmsUserService</c> value used for users.</para>
    /// \endif
    /// </param>
    public VmsAuthState(
        VmsSessionService sessions,
        AuthenticationStateProvider authenticationStateProvider,
        VmsUserService users)
    {
        _sessions = sessions;
        _authenticationStateProvider = authenticationStateProvider;
        _users = users;
    }

    /// <summary>
    /// \if KO
    /// <para>페이지 초기화 시 공통 로그인 쿠키 또는 localStorage 토큰으로 세션을 복원합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the restore async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="js">
    /// \if KO
    /// <para>js에 사용할 <c>IJSRuntime</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IJSRuntime</c> value used for js.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Restore Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the restore async operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>로그인 성공 후 호출합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the sign in async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="js">
    /// \if KO
    /// <para>js에 사용할 <c>IJSRuntime</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IJSRuntime</c> value used for js.</para>
    /// \endif
    /// </param>
    /// <param name="user">
    /// \if KO
    /// <para>user에 사용할 <c>VmsUser</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>VmsUser</c> value used for user.</para>
    /// \endif
    /// </param>
    /// <param name="token">
    /// \if KO
    /// <para>token에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for token.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Sign In Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the sign in async operation.</para>
    /// \endif
    /// </returns>
    public async Task SignInAsync(IJSRuntime js, VmsUser user, string token)
    {
        _token = token;
        CurrentUser = user;
        await js.InvokeVoidAsync("localStorage.setItem", "vms_session", token);
    }

    /// <summary>
    /// \if KO
    /// <para>로그아웃합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the sign out async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="js">
    /// \if KO
    /// <para>js에 사용할 <c>IJSRuntime</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IJSRuntime</c> value used for js.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Sign Out Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the sign out async operation.</para>
    /// \endif
    /// </returns>
    public async Task SignOutAsync(IJSRuntime js)
    {
        _sessions.RemoveSession(_token);
        _token = null;
        CurrentUser = null;
        await js.InvokeVoidAsync("localStorage.removeItem", "vms_session");
    }
}
