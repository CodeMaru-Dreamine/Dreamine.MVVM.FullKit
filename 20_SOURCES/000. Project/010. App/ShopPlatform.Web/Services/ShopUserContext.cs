using System.Security.Claims;
using Dreamine.Identity;
using Microsoft.AspNetCore.Components.Authorization;

namespace ShopPlatform.Services;

/// <summary>
/// \if KO
/// <para>Shop Current User 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates shop current user functionality and related state.</para>
/// \endif
/// </summary>
public sealed record ShopCurrentUser(
    string Id,
    string Provider,
    string Email,
    string DisplayName)
{
    /// <summary>
    /// \if KO
    /// <para>Is Authenticated 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the is authenticated value.</para>
    /// \endif
    /// </summary>
    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(Id);

    /// <summary>
    /// \if KO
    /// <para>Anonymous 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the anonymous value.</para>
    /// \endif
    /// </summary>
    public static ShopCurrentUser Anonymous { get; } = new("", "", "", "");
}

/// <summary>
/// \if KO
/// <para>Shop User Context 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates shop user context functionality and related state.</para>
/// \endif
/// </summary>
public sealed class ShopUserContext
{
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
    /// <para>지정한 설정으로 <see cref="ShopUserContext"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="ShopUserContext"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="authenticationStateProvider">
    /// \if KO
    /// <para>authentication State Provider에 사용할 <c>AuthenticationStateProvider</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>AuthenticationStateProvider</c> value used for authentication state provider.</para>
    /// \endif
    /// </param>
    public ShopUserContext(AuthenticationStateProvider authenticationStateProvider)
    {
        _authenticationStateProvider = authenticationStateProvider;
    }

    /// <summary>
    /// \if KO
    /// <para>Current Async 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the current async value.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Get Current Async 작업에서 생성한 <c>Task&lt;ShopCurrentUser&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;ShopCurrentUser&gt;</c> result produced by the get current async operation.</para>
    /// \endif
    /// </returns>
    public async Task<ShopCurrentUser> GetCurrentAsync()
    {
        var state = await _authenticationStateProvider.GetAuthenticationStateAsync().ConfigureAwait(false);
        var principal = state.User;

        if (principal.Identity?.IsAuthenticated != true)
        {
            return ShopCurrentUser.Anonymous;
        }

        string userId = principal.FindFirstValue(DreamineIdentityExtensions.UserIdClaimType) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return ShopCurrentUser.Anonymous;
        }

        string provider = principal.FindFirstValue(DreamineIdentityExtensions.ProviderClaimType) ?? "Local";
        string email = principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        string displayName = principal.FindFirstValue(ClaimTypes.Name) ?? email;

        return new ShopCurrentUser(
            $"oauth-{userId}",
            provider,
            email,
            displayName);
    }
}
