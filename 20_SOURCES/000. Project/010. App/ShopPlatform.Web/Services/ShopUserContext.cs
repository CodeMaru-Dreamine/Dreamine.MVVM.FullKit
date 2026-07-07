using System.Security.Claims;
using Dreamine.Identity;
using Microsoft.AspNetCore.Components.Authorization;

namespace ShopPlatform.Services;

public sealed record ShopCurrentUser(
    string Id,
    string Provider,
    string Email,
    string DisplayName)
{
    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(Id);

    public static ShopCurrentUser Anonymous { get; } = new("", "", "", "");
}

public sealed class ShopUserContext
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;

    public ShopUserContext(AuthenticationStateProvider authenticationStateProvider)
    {
        _authenticationStateProvider = authenticationStateProvider;
    }

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
