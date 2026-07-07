using ShopPlatform.Models;

namespace ShopPlatform.Services;

public sealed class ShopCustomerLoginSync
{
    private readonly ShopUserContext _userContext;
    private readonly ShopCustomerProfileStore _profiles;
    private readonly ShopCustomerSession _session;

    public ShopCustomerLoginSync(
        ShopUserContext userContext,
        ShopCustomerProfileStore profiles,
        ShopCustomerSession session)
    {
        _userContext = userContext;
        _profiles = profiles;
        _session = session;
    }

    public async Task<bool> SyncAsync(string slug)
    {
        if (_session.IsLoggedIn)
        {
            return true;
        }

        var user = await _userContext.GetCurrentAsync().ConfigureAwait(false);
        if (!user.IsAuthenticated)
        {
            return false;
        }

        var profile = await _profiles.GetAsync(slug, user.Id).ConfigureAwait(false);
        _session.LoginFromCommonUser(user, profile);
        return true;
    }

    public async Task SaveProfileAsync(string slug, string name, string phone, string address, string email)
    {
        var user = await _userContext.GetCurrentAsync().ConfigureAwait(false);
        if (!user.IsAuthenticated)
        {
            return;
        }

        var profile = new ShopCustomerProfile
        {
            UserId = user.Id,
            Provider = user.Provider,
            Email = string.IsNullOrWhiteSpace(email) ? user.Email : email.Trim(),
            DisplayName = user.DisplayName,
            Name = string.IsNullOrWhiteSpace(name) ? user.DisplayName : name.Trim(),
            Phone = phone.Trim(),
            Address = address.Trim()
        };

        await _profiles.SaveAsync(slug, profile).ConfigureAwait(false);
        _session.LoginFromCommonUser(user, profile);
    }
}
