namespace FamiliesApp.Models;

/// <summary>
/// \if KO
/// <para>Family Config 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates family config functionality and related state.</para>
/// \endif
/// </summary>
public sealed class FamilyConfig
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
    /// <para>Family Name 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the family name value.</para>
    /// \endif
    /// </summary>
    public string FamilyName { get; set; } = "우리 가족";
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
    /// <para>Cover Image File Name 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the cover image file name value.</para>
    /// \endif
    /// </summary>
    public string CoverImageFileName { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Theme Name 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the theme name value.</para>
    /// \endif
    /// </summary>
    public string ThemeName { get; set; } = "warm";
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
    /// <para>Is Published 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the is published value.</para>
    /// \endif
    /// </summary>
    public bool IsPublished { get; set; } = true;
    /// <summary>
    /// \if KO
    /// <para>Show On Home 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the show on home value.</para>
    /// \endif
    /// </summary>
    public bool ShowOnHome { get; set; } = false;
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
    /// <summary>
    /// \if KO
    /// <para>Og Image File Name 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the og image file name value.</para>
    /// \endif
    /// </summary>
    public string OgImageFileName { get; set; } = "";
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
    public List<FamilyAdminUser> AdminUsers { get; set; } = new();

    /// <summary>
    /// \if KO
    /// <para>방문자 이모지/좋아요 반응 허용 여부</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the allow reactions value.</para>
    /// \endif
    /// </summary>
    public bool AllowReactions { get; set; } = true;

    /// <summary>
    /// \if KO
    /// <para>방문자 댓글 허용 여부</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the allow comments value.</para>
    /// \endif
    /// </summary>
    public bool AllowComments { get; set; } = true;

    /// <summary>
    /// \if KO
    /// <para>타임라인 / 앨범 페이지당 포스트 수 (5~100)</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the page size value.</para>
    /// \endif
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// \if KO
    /// <para>이 계정 전용 이미지 업로드 최대 용량(MB). null이면 전체 설정(GlobalSettings) 값을 따름. 0이면 무제한.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the max image size mb value.</para>
    /// \endif
    /// </summary>
    public int? MaxImageSizeMb { get; set; } = null;
    /// <summary>
    /// \if KO
    /// <para>이 계정 전용 동영상 업로드 최대 용량(MB). null이면 전체 설정(GlobalSettings) 값을 따름. 0이면 무제한.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the max video size mb value.</para>
    /// \endif
    /// </summary>
    public int? MaxVideoSizeMb { get; set; } = null;
}

/// <summary>
/// \if KO
/// <para>Family Admin User 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates family admin user functionality and related state.</para>
/// \endif
/// </summary>
public sealed class FamilyAdminUser
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
