using System.Security.Claims;
using Dreamine.Identity;
using Microsoft.AspNetCore.Components.Authorization;

namespace PortfolioApp.Services;

public sealed record PortfolioCurrentUser(
    string Id,
    string Provider,
    string Email,
    string DisplayName)
{
    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(Id);

    public static PortfolioCurrentUser Anonymous { get; } = new("", "", "", "");
}

public sealed class PortfolioUserContext
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;

    public PortfolioUserContext(AuthenticationStateProvider authenticationStateProvider)
    {
        _authenticationStateProvider = authenticationStateProvider;
    }

    public async Task<PortfolioCurrentUser> GetCurrentAsync()
    {
        var state = await _authenticationStateProvider.GetAuthenticationStateAsync().ConfigureAwait(false);
        var principal = state.User;

        if (principal.Identity?.IsAuthenticated != true)
        {
            return PortfolioCurrentUser.Anonymous;
        }

        string userId = principal.FindFirstValue(DreamineIdentityExtensions.UserIdClaimType) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return PortfolioCurrentUser.Anonymous;
        }

        string provider = principal.FindFirstValue(DreamineIdentityExtensions.ProviderClaimType) ?? "Local";
        string email = principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        string displayName = principal.FindFirstValue(ClaimTypes.Name) ?? email;

        return new PortfolioCurrentUser(
            $"oauth-{userId}",
            provider,
            email,
            displayName);
    }
}
