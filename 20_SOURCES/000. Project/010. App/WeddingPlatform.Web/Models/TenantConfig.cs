namespace WeddingPlatform.Models;

/// <summary>
/// \if KO
/// <para>Tenant Config 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates tenant config functionality and related state.</para>
/// \endif
/// </summary>
public sealed class TenantConfig
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
    /// <para>Couple Name 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the couple name value.</para>
    /// \endif
    /// </summary>
    public string CoupleName { get; set; } = "신랑 ♥ 신부";
    /// <summary>
    /// \if KO
    /// <para>Hero Title 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the hero title value.</para>
    /// \endif
    /// </summary>
    public string HeroTitle { get; set; } = "Save The Date";
    /// <summary>
    /// \if KO
    /// <para>Subtitle 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the subtitle value.</para>
    /// \endif
    /// </summary>
    public string Subtitle { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Wedding Date 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the wedding date value.</para>
    /// \endif
    /// </summary>
    public DateTime WeddingDate { get; set; } = DateTime.Today;
    /// <summary>
    /// \if KO
    /// <para>Wedding Time 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the wedding time value.</para>
    /// \endif
    /// </summary>
    public string WeddingTime { get; set; } = "PM 12:00";
    /// <summary>
    /// \if KO
    /// <para>Venue Name 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the venue name value.</para>
    /// \endif
    /// </summary>
    public string VenueName { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Venue Address 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the venue address value.</para>
    /// \endif
    /// </summary>
    public string VenueAddress { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Venue Lat 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the venue lat value.</para>
    /// \endif
    /// </summary>
    public double VenueLat { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Venue Lng 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the venue lng value.</para>
    /// \endif
    /// </summary>
    public double VenueLng { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Story 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the story value.</para>
    /// \endif
    /// </summary>
    public string Story { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Story2 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the story2 value.</para>
    /// \endif
    /// </summary>
    public string Story2 { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Hero Image File Name 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the hero image file name value.</para>
    /// \endif
    /// </summary>
    public string HeroImageFileName { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Design Settings 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the design settings value.</para>
    /// \endif
    /// </summary>
    public DesignSettings DesignSettings { get; set; } = new();
    /// <summary>
    /// \if KO
    /// <para>청첩장 표시 스타일 — "onepage"(기본) 또는 "tabs". 하위 호환용이며 내부 사용은 DesignSettings.LayoutMode를 우선합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the invitation style value.</para>
    /// \endif
    /// </summary>
    public string InvitationStyle { get; set; } = "onepage";

    // ── 청첩장 히어로 문구 패널 위치 (상단/하단 박스, PC/모바일 별도 지정) ──
    // Vertical: "top" | "middle" | "bottom" / Horizontal: "left" | "center" | "right"
    /// <summary>
    /// \if KO
    /// <para>Invite Hero Top Vertical Desktop 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the invite hero top vertical desktop value.</para>
    /// \endif
    /// </summary>
    public string InviteHeroTopVerticalDesktop { get; set; } = "top";
    /// <summary>
    /// \if KO
    /// <para>Invite Hero Top Horizontal Desktop 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the invite hero top horizontal desktop value.</para>
    /// \endif
    /// </summary>
    public string InviteHeroTopHorizontalDesktop { get; set; } = "center";
    /// <summary>
    /// \if KO
    /// <para>Invite Hero Top Vertical Mobile 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the invite hero top vertical mobile value.</para>
    /// \endif
    /// </summary>
    public string InviteHeroTopVerticalMobile { get; set; } = "top";
    /// <summary>
    /// \if KO
    /// <para>Invite Hero Top Horizontal Mobile 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the invite hero top horizontal mobile value.</para>
    /// \endif
    /// </summary>
    public string InviteHeroTopHorizontalMobile { get; set; } = "center";
    /// <summary>
    /// \if KO
    /// <para>Invite Hero Bottom Vertical Desktop 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the invite hero bottom vertical desktop value.</para>
    /// \endif
    /// </summary>
    public string InviteHeroBottomVerticalDesktop { get; set; } = "bottom";
    /// <summary>
    /// \if KO
    /// <para>Invite Hero Bottom Horizontal Desktop 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the invite hero bottom horizontal desktop value.</para>
    /// \endif
    /// </summary>
    public string InviteHeroBottomHorizontalDesktop { get; set; } = "center";
    /// <summary>
    /// \if KO
    /// <para>Invite Hero Bottom Vertical Mobile 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the invite hero bottom vertical mobile value.</para>
    /// \endif
    /// </summary>
    public string InviteHeroBottomVerticalMobile { get; set; } = "bottom";
    /// <summary>
    /// \if KO
    /// <para>Invite Hero Bottom Horizontal Mobile 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the invite hero bottom horizontal mobile value.</para>
    /// \endif
    /// </summary>
    public string InviteHeroBottomHorizontalMobile { get; set; } = "center";

    // ── 감사장 히어로 문구 패널 위치 (PC/모바일 별도 지정) ──
    // Vertical: "top" | "middle" | "bottom" / Horizontal: "left" | "center" | "right"
    /// <summary>
    /// \if KO
    /// <para>Hero Panel Vertical Desktop 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the hero panel vertical desktop value.</para>
    /// \endif
    /// </summary>
    public string HeroPanelVerticalDesktop { get; set; } = "top";
    /// <summary>
    /// \if KO
    /// <para>Hero Panel Horizontal Desktop 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the hero panel horizontal desktop value.</para>
    /// \endif
    /// </summary>
    public string HeroPanelHorizontalDesktop { get; set; } = "center";
    /// <summary>
    /// \if KO
    /// <para>Hero Panel Vertical Mobile 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the hero panel vertical mobile value.</para>
    /// \endif
    /// </summary>
    public string HeroPanelVerticalMobile { get; set; } = "top";
    /// <summary>
    /// \if KO
    /// <para>Hero Panel Horizontal Mobile 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the hero panel horizontal mobile value.</para>
    /// \endif
    /// </summary>
    public string HeroPanelHorizontalMobile { get; set; } = "center";
    /// <summary>
    /// \if KO
    /// <para>Map Link Kakao 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the map link kakao value.</para>
    /// \endif
    /// </summary>
    public string MapLinkKakao { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Map Link Naver 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the map link naver value.</para>
    /// \endif
    /// </summary>
    public string MapLinkNaver { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Map Link Atlan 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the map link atlan value.</para>
    /// \endif
    /// </summary>
    public string MapLinkAtlan { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Map Link Tmap 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the map link tmap value.</para>
    /// \endif
    /// </summary>
    public string MapLinkTmap { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Ceremony Note 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the ceremony note value.</para>
    /// \endif
    /// </summary>
    public string CeremonyNote { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Ceremony Note Format 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the ceremony note format value.</para>
    /// \endif
    /// </summary>
    public string CeremonyNoteFormat { get; set; } = "Markdown";
    /// <summary>
    /// \if KO
    /// <para>Road Map File Name 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the road map file name value.</para>
    /// \endif
    /// </summary>
    public string RoadMapFileName { get; set; } = "";

    /// <summary>
    /// \if KO
    /// <para>마음 전하실 곳 — 신랑·신부·부모님 계좌 및 연락처 (최대 제한 없음, UI에서 4개까지 권장)</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the accounts value.</para>
    /// \endif
    /// </summary>
    public List<AccountInfo> Accounts { get; set; } = new();

    /// <summary>
    /// \if KO
    /// <para>테마 이름 — rose(기본), ivory, forest, navy. 하위 호환용이며 내부 사용은 DesignSettings.ThemeKey를 우선합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the theme name value.</para>
    /// \endif
    /// </summary>
    public string ThemeName { get; set; } = "rose";

    /// <summary>
    /// \if KO
    /// <para>배경 음악 파일명</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the music file name value.</para>
    /// \endif
    /// </summary>
    public string MusicFileName { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>배경 음악 버튼 위치 — "bottom"(기본) 또는 "top"</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the music button position value.</para>
    /// \endif
    /// </summary>
    public string MusicButtonPosition { get; set; } = "bottom";
    /// <summary>
    /// \if KO
    /// <para>Video File Name 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the video file name value.</para>
    /// \endif
    /// </summary>
    [Obsolete("Use VideoFileNames")]
    public string VideoFileName { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Video File Names 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the video file names value.</para>
    /// \endif
    /// </summary>
    public List<string> VideoFileNames { get; set; } = new();
    /// <summary>
    /// \if KO
    /// <para>이 계정 전용 동영상 업로드 최대 용량(MB). null이면 전체 설정(GlobalSettings) 값을 따름. 0이면 무제한.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the max video size mb value.</para>
    /// \endif
    /// </summary>
    public int? MaxVideoSizeMb { get; set; } = null;
    /// <summary>
    /// \if KO
    /// <para>이 계정 전용 동영상 업로드 최대 개수. null이면 전체 설정(GlobalSettings) 값을 따름. 0이면 무제한.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the max video count value.</para>
    /// \endif
    /// </summary>
    public int? MaxVideoCount { get; set; } = null;
    /// <summary>
    /// \if KO
    /// <para>이 계정 전용 이미지/영상 정책 override. null이면 등급 정책을 따릅니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the media policy override value.</para>
    /// \endif
    /// </summary>
    public MediaPolicyOverride? MediaPolicyOverride { get; set; } = null;

    // ── 링크 미리보기 (Open Graph) ──────────────────────
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
    /// <para>OG 이미지 파일명 — 비우면 히어로 이미지 사용</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the og image file name value.</para>
    /// \endif
    /// </summary>
    public string OgImageFileName { get; set; } = "";

    // ── 자동 감사장 링크 미리보기 (Open Graph) ──────────────────────
    /// <summary>
    /// \if KO
    /// <para>Thank You Og Title 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the thank you og title value.</para>
    /// \endif
    /// </summary>
    public string ThankYouOgTitle { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Thank You Og Description 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the thank you og description value.</para>
    /// \endif
    /// </summary>
    public string ThankYouOgDescription { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>자동 감사장 OG 이미지 파일명 — 비우면 히어로 이미지 사용</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the thank you og image file name value.</para>
    /// \endif
    /// </summary>
    public string ThankYouOgImageFileName { get; set; } = "";

    /// <summary>
    /// \if KO
    /// <para>Mode 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the mode value.</para>
    /// \endif
    /// </summary>
    public WeddingSiteMode Mode { get; set; } = WeddingSiteMode.Invite;
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
    public List<WeddingAdminUser> AdminUsers { get; set; } = new();
    /// <summary>
    /// \if KO
    /// <para>Thank You Url 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the thank you url value.</para>
    /// \endif
    /// </summary>
    public string ThankYouUrl { get; set; } = "";
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
    /// <para>Created At 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the created at value.</para>
    /// \endif
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
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
    /// <para>메인 노출 고정 순서 — 1이 최우선. 0이면 날짜순 자동 정렬.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the pin order value.</para>
    /// \endif
    /// </summary>
    public int PinOrder { get; set; } = 0;
    /// <summary>
    /// \if KO
    /// <para>Has Premium Plan 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the has premium plan value.</para>
    /// \endif
    /// </summary>
    public bool HasPremiumPlan { get; set; } = false;
    /// <summary>
    /// \if KO
    /// <para>Unlocked Layout Modes 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the unlocked layout modes value.</para>
    /// \endif
    /// </summary>
    public List<string> UnlockedLayoutModes { get; set; } = new();
    /// <summary>
    /// \if KO
    /// <para>Unlocked Theme Keys 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the unlocked theme keys value.</para>
    /// \endif
    /// </summary>
    public List<string> UnlockedThemeKeys { get; set; } = new();
    /// <summary>
    /// \if KO
    /// <para>Gallery File Names 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the gallery file names value.</para>
    /// \endif
    /// </summary>
    public List<string> GalleryFileNames { get; set; } = new();
    /// <summary>
    /// \if KO
    /// <para>Gallery Auto Play Seconds 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the gallery auto play seconds value.</para>
    /// \endif
    /// </summary>
    public int GalleryAutoPlaySeconds { get; set; } = 3;
}

/// <summary>
/// \if KO
/// <para>Wedding Admin User 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates wedding admin user functionality and related state.</para>
/// \endif
/// </summary>
public sealed class WeddingAdminUser
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

/// <summary>
/// \if KO
/// <para>Wedding Site Mode 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates wedding site mode functionality and related state.</para>
/// \endif
/// </summary>
public enum WeddingSiteMode
{
    /// <summary>
    /// \if KO
    /// <para>Invite 값을 나타냅니다.</para>
    /// \endif
    /// \if EN
    /// <para>Represents the invite value.</para>
    /// \endif
    /// </summary>
    Invite,
    /// <summary>
    /// \if KO
    /// <para>Thank You 값을 나타냅니다.</para>
    /// \endif
    /// \if EN
    /// <para>Represents the thank you value.</para>
    /// \endif
    /// </summary>
    ThankYou,
    /// <summary>
    /// \if KO
    /// <para>Both 값을 나타냅니다.</para>
    /// \endif
    /// \if EN
    /// <para>Represents the both value.</para>
    /// \endif
    /// </summary>
    Both
}
