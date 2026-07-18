namespace PortfolioApp.Models;

/// <summary>
/// \if KO
/// <para>Portfolio Config 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates portfolio config functionality and related state.</para>
/// \endif
/// </summary>
public class PortfolioConfig
{
    /// <summary>
    /// \if KO
    /// <para>Slug 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the slug value.</para>
    /// \endif
    /// </summary>
    public string Slug { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Owner Name 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the owner name value.</para>
    /// \endif
    /// </summary>
    public string OwnerName { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Title 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the title value.</para>
    /// \endif
    /// </summary>
    public string Title { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Bio 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the bio value.</para>
    /// \endif
    /// </summary>
    public string Bio { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Profile Image File Name 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the profile image file name value.</para>
    /// \endif
    /// </summary>
    public string ProfileImageFileName { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Theme Name 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the theme name value.</para>
    /// \endif
    /// </summary>
    public string ThemeName { get; set; } = "dark";
    /// <summary>
    /// \if KO
    /// <para>Password Hash 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the password hash value.</para>
    /// \endif
    /// </summary>
    public string PasswordHash { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Owner User Id 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the owner user id value.</para>
    /// \endif
    /// </summary>
    public string OwnerUserId { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Owner Provider 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the owner provider value.</para>
    /// \endif
    /// </summary>
    public string OwnerProvider { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Owner Email 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the owner email value.</para>
    /// \endif
    /// </summary>
    public string OwnerEmail { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Owner Display Name 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the owner display name value.</para>
    /// \endif
    /// </summary>
    public string OwnerDisplayName { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Owner Linked At 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the owner linked at value.</para>
    /// \endif
    /// </summary>
    public DateTime? OwnerLinkedAt { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Admin Users 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the admin users value.</para>
    /// \endif
    /// </summary>
    public List<PortfolioAdminUser> AdminUsers { get; set; } = new();
    /// <summary>
    /// \if KO
    /// <para>Show On Home 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the show on home value.</para>
    /// \endif
    /// </summary>
    public bool ShowOnHome { get; set; } = true;
    /// <summary>
    /// \if KO
    /// <para>Pin Order 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the pin order value.</para>
    /// \endif
    /// </summary>
    public int PinOrder { get; set; } = 0;
    /// <summary>
    /// \if KO
    /// <para>Created At 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the created at value.</para>
    /// \endif
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // 소셜/연락처
    /// <summary>
    /// \if KO
    /// <para>Github Url 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the github url value.</para>
    /// \endif
    /// </summary>
    public string GithubUrl { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Linked In Url 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the linked in url value.</para>
    /// \endif
    /// </summary>
    public string LinkedInUrl { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Contact Email 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the contact email value.</para>
    /// \endif
    /// </summary>
    public string ContactEmail { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Allow Contact 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the allow contact value.</para>
    /// \endif
    /// </summary>
    public bool AllowContact { get; set; } = true;

    // OG
    /// <summary>
    /// \if KO
    /// <para>Og Title 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the og title value.</para>
    /// \endif
    /// </summary>
    public string OgTitle { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Og Description 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the og description value.</para>
    /// \endif
    /// </summary>
    public string OgDescription { get; set; } = "";
}

/// <summary>
/// \if KO
/// <para>Portfolio Admin User 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates portfolio admin user functionality and related state.</para>
/// \endif
/// </summary>
public sealed class PortfolioAdminUser
{
    /// <summary>
    /// \if KO
    /// <para>User Id 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the user id value.</para>
    /// \endif
    /// </summary>
    public string UserId { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Provider 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the provider value.</para>
    /// \endif
    /// </summary>
    public string Provider { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Email 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the email value.</para>
    /// \endif
    /// </summary>
    public string Email { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Display Name 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the display name value.</para>
    /// \endif
    /// </summary>
    public string DisplayName { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Role 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the role value.</para>
    /// \endif
    /// </summary>
    public string Role { get; set; } = "Admin";
    /// <summary>
    /// \if KO
    /// <para>Added At 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the added at value.</para>
    /// \endif
    /// </summary>
    public DateTime AddedAt { get; set; } = DateTime.Now;
}
