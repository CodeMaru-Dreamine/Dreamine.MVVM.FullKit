using ShopPlatform.Models;

namespace ShopPlatform.Services;

/// <summary>Blazor 회로 수명 동안 유지되는 고객 세션 (Scoped).</summary>
public sealed class ShopCustomerSession
{
    public ShopUser? User { get; private set; }
    public bool IsLoggedIn => User != null;

    public void Login(ShopUser user) => User = user;
    public void LoginFromCommonUser(ShopCurrentUser user, ShopCustomerProfile? profile)
    {
        User = new ShopUser
        {
            Email = profile?.Email ?? user.Email,
            Name = !string.IsNullOrWhiteSpace(profile?.Name)
                ? profile.Name
                : !string.IsNullOrWhiteSpace(user.DisplayName) ? user.DisplayName : user.Email,
            Phone = profile?.Phone ?? string.Empty,
            Address = profile?.Address ?? string.Empty
        };
    }

    public void Logout() => User = null;
}
