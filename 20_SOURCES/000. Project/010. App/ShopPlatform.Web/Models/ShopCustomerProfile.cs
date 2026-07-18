namespace ShopPlatform.Models;

/// <summary>
/// \if KO
/// <para>Shop Customer Profile 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates shop customer profile functionality and related state.</para>
/// \endif
/// </summary>
public sealed class ShopCustomerProfile
{
    /// <summary>
    /// \if KO
    /// <para>User Id 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the user id value.</para>
    /// \endif
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>Provider 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the provider value.</para>
    /// \endif
    /// </summary>
    public string Provider { get; set; } = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>Email 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the email value.</para>
    /// \endif
    /// </summary>
    public string Email { get; set; } = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>Display Name 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the display name value.</para>
    /// \endif
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>Name 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the name value.</para>
    /// \endif
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>Phone 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the phone value.</para>
    /// \endif
    /// </summary>
    public string Phone { get; set; } = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>Address 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the address value.</para>
    /// \endif
    /// </summary>
    public string Address { get; set; } = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>Updated At 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the updated at value.</para>
    /// \endif
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
