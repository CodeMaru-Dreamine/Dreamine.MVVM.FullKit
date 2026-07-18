namespace ShopPlatform.Models;

/// <summary>
/// \if KO
/// <para>Shop User 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates shop user functionality and related state.</para>
/// \endif
/// </summary>
public sealed class ShopUser
{
    /// <summary>
    /// \if KO
    /// <para>Id 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the id value.</para>
    /// \endif
    /// </summary>
    public int Id { get; set; }
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
    /// <para>Password Hash 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the password hash value.</para>
    /// \endif
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;
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
    /// <para>Created At 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the created at value.</para>
    /// \endif
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
