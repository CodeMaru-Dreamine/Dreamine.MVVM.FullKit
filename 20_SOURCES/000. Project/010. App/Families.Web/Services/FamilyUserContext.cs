using System.Security.Claims;
using Dreamine.Identity;
using Microsoft.AspNetCore.Components.Authorization;

namespace FamiliesApp.Services;

public sealed record FamilyCurrentUser(
    string Id,
    string Provider,
    string Email,
    string DisplayName)
{
    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(Id);

    public static FamilyCurrentUser Anonymous { get; } = new("", "", "", "");
}

public sealed class FamilyUserContext
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;

    public FamilyUserContext(AuthenticationStateProvider authenticationStateProvider)
    {
        _authenticationStateProvider = authenticationStateProvider;
    }

    public async Task<FamilyCurrentUser> GetCurrentAsync()
    {
        var state = await _authenticationStateProvider.GetAuthenticationStateAsync().ConfigureAwait(false);
        var principal = state.User;

        if (principal.Identity?.IsAuthenticated != true)
        {
            return FamilyCurrentUser.Anonymous;
        }

        string userId = principal.FindFirstValue(DreamineIdentityExtensions.UserIdClaimType) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return FamilyCurrentUser.Anonymous;
        }

        string provider = principal.FindFirstValue(DreamineIdentityExtensions.ProviderClaimType) ?? "Local";
        string email = principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        string displayName = principal.FindFirstValue(ClaimTypes.Name) ?? email;

        return new FamilyCurrentUser(
            $"oauth-{userId}",
            provider,
            email,
            displayName);
    }
}
