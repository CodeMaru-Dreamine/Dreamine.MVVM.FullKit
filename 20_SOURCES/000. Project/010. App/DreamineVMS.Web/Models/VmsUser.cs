namespace DreamineVMS.Web.Models;

/// <summary>
/// \if KO
/// <para>Vms User 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates vms user functionality and related state.</para>
/// \endif
/// </summary>
public sealed class VmsUser
{
    /// <summary>
    /// \if KO
    /// <para>Id 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the id value.</para>
    /// \endif
    /// </summary>
    public string Id { get; init; } = "";
    /// <summary>
    /// \if KO
    /// <para>Email 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the email value.</para>
    /// \endif
    /// </summary>
    public string Email { get; init; } = "";
    /// <summary>
    /// \if KO
    /// <para>Display Name 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the display name value.</para>
    /// \endif
    /// </summary>
    public string DisplayName { get; init; } = "";
    /// <summary>
    /// \if KO
    /// <para>Public Slug 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the public slug value.</para>
    /// \endif
    /// </summary>
    public string PublicSlug { get; init; } = "";
    /// <summary>
    /// \if KO
    /// <para>Password Hash 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the password hash value.</para>
    /// \endif
    /// </summary>
    public string PasswordHash { get; init; } = "";
    /// <summary>
    /// \if KO
    /// <para>Password Salt 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the password salt value.</para>
    /// \endif
    /// </summary>
    public string PasswordSalt { get; init; } = "";
    /// <summary>
    /// \if KO
    /// <para>Created At 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the created at value.</para>
    /// \endif
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }
    /// <summary>
    /// \if KO
    /// <para>Live Layout 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the live layout value.</para>
    /// \endif
    /// </summary>
    public string LiveLayout { get; init; } = "auto";
    /// <summary>
    /// \if KO
    /// <para>Og Title 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the og title value.</para>
    /// \endif
    /// </summary>
    public string OgTitle { get; init; } = "";
    /// <summary>
    /// \if KO
    /// <para>Og Description 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the og description value.</para>
    /// \endif
    /// </summary>
    public string OgDescription { get; init; } = "";
    /// <summary>
    /// \if KO
    /// <para>Og Image 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the og image value.</para>
    /// \endif
    /// </summary>
    public string OgImage { get; init; } = "";
}
