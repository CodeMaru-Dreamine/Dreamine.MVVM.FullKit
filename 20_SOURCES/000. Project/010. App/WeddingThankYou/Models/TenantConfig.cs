namespace WeddingThankYou.Models;

/// <summary>
/// \file TenantConfig.cs
/// \brief 슬러그(테넌트)별 웨딩 감사장 설정 모델.
/// \details WeddingPlatform.Web의 TenantConfig를 ThankYou 전용으로 이식.
/// </summary>
public sealed class TenantConfig
{
    public string Slug { get; set; } = "";
    public string CoupleName { get; set; } = "신랑 ♥ 신부";
    public string HeroTitle { get; set; } = "Thank You";
    public string Subtitle { get; set; } = "";
    public DateTime WeddingDate { get; set; } = DateTime.Today;
    public string WeddingTime { get; set; } = "PM 12:00";
    public string VenueName { get; set; } = "";
    public string VenueAddress { get; set; } = "";
    public double VenueLat { get; set; }
    public double VenueLng { get; set; }
    public string Story { get; set; } = "";
    public string Story2 { get; set; } = "";
    public string HeroImageFileName { get; set; } = "";

    // ── 히어로 문구 패널 위치 (PC/모바일 별도 지정) ──────────
    // Vertical: "top" | "middle" | "bottom" / Horizontal: "left" | "center" | "right"
    public string HeroPanelVerticalDesktop { get; set; } = "top";
    public string HeroPanelHorizontalDesktop { get; set; } = "center";
    public string HeroPanelVerticalMobile { get; set; } = "top";
    public string HeroPanelHorizontalMobile { get; set; } = "center";
    public string MapLinkKakao { get; set; } = "";
    public string MapLinkNaver { get; set; } = "";
    public string MapLinkAtlan { get; set; } = "";
    public string MapLinkTmap { get; set; } = "";
    public string CeremonyNote { get; set; } = "";
    public string CeremonyNoteFormat { get; set; } = "Markdown";
    public string RoadMapFileName { get; set; } = "";

    /// <summary>마음 전하실 곳 — 신랑·신부·부모님 계좌 및 연락처</summary>
    public List<AccountInfo> Accounts { get; set; } = new();

    /// <summary>테마 이름 — rose(기본), ivory, forest, navy, blush</summary>
    public string ThemeName { get; set; } = "rose";

    /// <summary>배경 음악 파일명</summary>
    public string MusicFileName { get; set; } = "";
    /// <summary>배경 음악 버튼 위치 — "bottom"(기본) 또는 "top"</summary>
    public string MusicButtonPosition { get; set; } = "bottom";
    public List<string> VideoFileNames { get; set; } = new();
    /// <summary>이 계정 전용 동영상 업로드 최대 용량(MB). null이면 전체 설정(GlobalSettings) 값을 따름. 0이면 무제한.</summary>
    public int? MaxVideoSizeMb { get; set; } = null;
    /// <summary>이 계정 전용 동영상 업로드 최대 개수. null이면 전체 설정(GlobalSettings) 값을 따름. 0이면 무제한.</summary>
    public int? MaxVideoCount { get; set; } = null;

    // ── 링크 미리보기 (Open Graph) ──────────────────────
    public string OgTitle { get; set; } = "";
    public string OgDescription { get; set; } = "";
    /// <summary>OG 이미지 파일명 — 비우면 히어로 이미지 사용</summary>
    public string OgImageFileName { get; set; } = "";

    public string PasswordHash { get; set; } = "";
    public string ThankYouUrl { get; set; } = "";
    public bool IsPublished { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public List<string> GalleryFileNames { get; set; } = new();
    public int GalleryAutoPlaySeconds { get; set; } = 3;

    /// <summary>메인 페이지(공개 목록)에 노출할지 여부</summary>
    public bool ShowOnHome { get; set; } = false;
    /// <summary>메인 노출 고정 순서 — 1이 최우선. 0이면 날짜순 자동 정렬.</summary>
    public int PinOrder { get; set; } = 0;
}
