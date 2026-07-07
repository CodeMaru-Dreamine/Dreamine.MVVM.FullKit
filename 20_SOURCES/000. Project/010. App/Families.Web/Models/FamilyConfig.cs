namespace FamiliesApp.Models;

public sealed class FamilyConfig
{
    public string Slug { get; set; } = "";
    public string FamilyName { get; set; } = "우리 가족";
    public string Bio { get; set; } = "";
    public string CoverImageFileName { get; set; } = "";
    public string ThemeName { get; set; } = "warm";
    public string PasswordHash { get; set; } = "";
    public bool IsPublished { get; set; } = true;
    public bool ShowOnHome { get; set; } = false;
    public int PinOrder { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string OgTitle { get; set; } = "";
    public string OgDescription { get; set; } = "";
    public string OgImageFileName { get; set; } = "";
    public string OwnerUserId { get; set; } = "";
    public string OwnerProvider { get; set; } = "";
    public string OwnerEmail { get; set; } = "";
    public string OwnerDisplayName { get; set; } = "";
    public DateTime? OwnerLinkedAt { get; set; }

    /// <summary>방문자 이모지/좋아요 반응 허용 여부</summary>
    public bool AllowReactions { get; set; } = true;

    /// <summary>방문자 댓글 허용 여부</summary>
    public bool AllowComments { get; set; } = true;

    /// <summary>타임라인 / 앨범 페이지당 포스트 수 (5~100)</summary>
    public int PageSize { get; set; } = 20;

    /// <summary>이 계정 전용 이미지 업로드 최대 용량(MB). null이면 전체 설정(GlobalSettings) 값을 따름. 0이면 무제한.</summary>
    public int? MaxImageSizeMb { get; set; } = null;
    /// <summary>이 계정 전용 동영상 업로드 최대 용량(MB). null이면 전체 설정(GlobalSettings) 값을 따름. 0이면 무제한.</summary>
    public int? MaxVideoSizeMb { get; set; } = null;
}
