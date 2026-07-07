using System.Security.Claims;
using Dreamine.Identity;
using Microsoft.AspNetCore.Components.Authorization;

namespace WeddingPlatform.Services;

public sealed record WeddingCurrentUser(
    string Id,
    string Provider,
    string Email,
    string DisplayName)
{
    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(Id);

    public static WeddingCurrentUser Anonymous { get; } = new("", "", "", "");
}

public sealed class WeddingUserContext
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;

    public WeddingUserContext(AuthenticationStateProvider authenticationStateProvider)
    {
        _authenticationStateProvider = authenticationStateProvider;
    }

    public async Task<WeddingCurrentUser> GetCurrentAsync()
    {
        var state = await _authenticationStateProvider.GetAuthenticationStateAsync().ConfigureAwait(false);
        var principal = state.User;

        if (principal.Identity?.IsAuthenticated != true)
        {
            return WeddingCurrentUser.Anonymous;
        }

        string userId = principal.FindFirstValue(DreamineIdentityExtensions.UserIdClaimType) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return WeddingCurrentUser.Anonymous;
        }

        string provider = principal.FindFirstValue(DreamineIdentityExtensions.ProviderClaimType) ?? "Local";
        string email = principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        string displayName = principal.FindFirstValue(ClaimTypes.Name) ?? email;

        return new WeddingCurrentUser(
            $"oauth-{userId}",
            provider,
            email,
            displayName);
    }
}
