using ShopPlatform.Models;

namespace ShopPlatform.Services;

/// <summary>
/// \if KO
/// <para>Shop Customer Login Sync 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates shop customer login sync functionality and related state.</para>
/// \endif
/// </summary>
public sealed class ShopCustomerLoginSync
{
    /// <summary>
    /// \if KO
    /// <para>user Context 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the user context value.</para>
    /// \endif
    /// </summary>
    private readonly ShopUserContext _userContext;
    /// <summary>
    /// \if KO
    /// <para>profiles 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the profiles value.</para>
    /// \endif
    /// </summary>
    private readonly ShopCustomerProfileStore _profiles;
    /// <summary>
    /// \if KO
    /// <para>session 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the session value.</para>
    /// \endif
    /// </summary>
    private readonly ShopCustomerSession _session;

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="ShopCustomerLoginSync"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="ShopCustomerLoginSync"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="userContext">
    /// \if KO
    /// <para>user Context에 사용할 <c>ShopUserContext</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ShopUserContext</c> value used for user context.</para>
    /// \endif
    /// </param>
    /// <param name="profiles">
    /// \if KO
    /// <para>profiles에 사용할 <c>ShopCustomerProfileStore</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ShopCustomerProfileStore</c> value used for profiles.</para>
    /// \endif
    /// </param>
    /// <param name="session">
    /// \if KO
    /// <para>session에 사용할 <c>ShopCustomerSession</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ShopCustomerSession</c> value used for session.</para>
    /// \endif
    /// </param>
    public ShopCustomerLoginSync(
        ShopUserContext userContext,
        ShopCustomerProfileStore profiles,
        ShopCustomerSession session)
    {
        _userContext = userContext;
        _profiles = profiles;
        _session = session;
    }

    /// <summary>
    /// \if KO
    /// <para>Sync Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the sync async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="slug">
    /// \if KO
    /// <para>slug에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for slug.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Sync Async 작업에서 생성한 <c>Task&lt;bool&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;bool&gt;</c> result produced by the sync async operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Profile Async 데이터를 저장합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Saves profile async data.</para>
    /// \endif
    /// </summary>
    /// <param name="slug">
    /// \if KO
    /// <para>slug에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for slug.</para>
    /// \endif
    /// </param>
    /// <param name="name">
    /// \if KO
    /// <para>name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for name.</para>
    /// \endif
    /// </param>
    /// <param name="phone">
    /// \if KO
    /// <para>phone에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for phone.</para>
    /// \endif
    /// </param>
    /// <param name="address">
    /// \if KO
    /// <para>address에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for address.</para>
    /// \endif
    /// </param>
    /// <param name="email">
    /// \if KO
    /// <para>email에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for email.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Save Profile Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the save profile async operation.</para>
    /// \endif
    /// </returns>
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
