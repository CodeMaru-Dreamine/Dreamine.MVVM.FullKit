using ShopPlatform.Models;

namespace ShopPlatform.Services;

/// <summary>
/// \if KO
/// <para>Blazor 회로 수명 동안 유지되는 고객 세션 (Scoped).</para>
/// \endif
/// \if EN
/// <para>Encapsulates shop customer session functionality and related state.</para>
/// \endif
/// </summary>
public sealed class ShopCustomerSession
{
    /// <summary>
    /// \if KO
    /// <para>User 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the user value.</para>
    /// \endif
    /// </summary>
    public ShopUser? User { get; private set; }
    /// <summary>
    /// \if KO
    /// <para>Is Logged In 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the is logged in value.</para>
    /// \endif
    /// </summary>
    public bool IsLoggedIn => User != null;

    /// <summary>
    /// \if KO
    /// <para>Login 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the login operation.</para>
    /// \endif
    /// </summary>
    /// <param name="user">
    /// \if KO
    /// <para>user에 사용할 <c>ShopUser</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ShopUser</c> value used for user.</para>
    /// \endif
    /// </param>
    public void Login(ShopUser user) => User = user;
    /// <summary>
    /// \if KO
    /// <para>Login From Common User 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the login from common user operation.</para>
    /// \endif
    /// </summary>
    /// <param name="user">
    /// \if KO
    /// <para>user에 사용할 <c>ShopCurrentUser</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ShopCurrentUser</c> value used for user.</para>
    /// \endif
    /// </param>
    /// <param name="profile">
    /// \if KO
    /// <para>profile에 사용할 <c>ShopCustomerProfile?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ShopCustomerProfile?</c> value used for profile.</para>
    /// \endif
    /// </param>
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

    /// <summary>
    /// \if KO
    /// <para>Logout 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the logout operation.</para>
    /// \endif
    /// </summary>
    public void Logout() => User = null;
}
