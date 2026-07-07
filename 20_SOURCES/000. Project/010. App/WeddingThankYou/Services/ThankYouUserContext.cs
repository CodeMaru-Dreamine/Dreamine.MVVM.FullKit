using System.Security.Claims;
using Dreamine.Identity;
using Microsoft.AspNetCore.Components.Authorization;

namespace WeddingThankYou.Services;

public sealed record ThankYouCurrentUser(
    string Id,
    string Provider,
    string Email,
    string DisplayName)
{
    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(Id);

    public static ThankYouCurrentUser Anonymous { get; } = new("", "", "", "");
}

public sealed class ThankYouUserContext
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;

    public ThankYouUserContext(AuthenticationStateProvider authenticationStateProvider)
    {
        _authenticationStateProvider = authenticationStateProvider;
    }

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
