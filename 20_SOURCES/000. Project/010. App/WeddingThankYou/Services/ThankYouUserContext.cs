using System.Security.Claims;
using Dreamine.Identity;
using Microsoft.AspNetCore.Components.Authorization;

namespace WeddingThankYou.Services;

/// <summary>
/// \if KO
/// <para>Thank You Current User 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates thank you current user functionality and related state.</para>
/// \endif
/// </summary>
public sealed record ThankYouCurrentUser(
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
    public static ThankYouCurrentUser Anonymous { get; } = new("", "", "", "");
}

/// <summary>
/// \if KO
/// <para>Thank You User Context 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates thank you user context functionality and related state.</para>
/// \endif
/// </summary>
public sealed class ThankYouUserContext
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
    /// <para>지정한 설정으로 <see cref="ThankYouUserContext"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="ThankYouUserContext"/> class with the specified settings.</para>
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
    public ThankYouUserContext(AuthenticationStateProvider authenticationStateProvider)
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
    /// <para>Get Current Async 작업에서 생성한 <c>Task&lt;ThankYouCurrentUser&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;ThankYouCurrentUser&gt;</c> result produced by the get current async operation.</para>
    /// \endif
    /// </returns>
    public async Task<ThankYouCurrentUser> GetCurrentAsync()
    {
        var state = await _authenticationStateProvider.GetAuthenticationStateAsync().ConfigureAwait(false);
        var principal = state.User;

        if (principal.Identity?.IsAuthenticated != true)
        {
            return ThankYouCurrentUser.Anonymous;
        }

        string userId = principal.FindFirstValue(DreamineIdentityExtensions.UserIdClaimType) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return ThankYouCurrentUser.Anonymous;
        }

        string provider = principal.FindFirstValue(DreamineIdentityExtensions.ProviderClaimType) ?? "Local";
        string email = principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        string displayName = principal.FindFirstValue(ClaimTypes.Name) ?? email;

        return new ThankYouCurrentUser(
            $"oauth-{userId}",
            provider,
            email,
            displayName);
    }
}
