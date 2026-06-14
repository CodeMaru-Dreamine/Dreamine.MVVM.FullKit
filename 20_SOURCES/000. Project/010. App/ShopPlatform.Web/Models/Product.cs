namespace ShopPlatform.Models;

public sealed class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public bool IsUnlimitedStock { get; set; } = false; // true면 재고 수량 무시
    public string? ImagePath { get; set; }
    public string Category { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; } = false;
    public int SortOrder { get; set; } = 0;
    public string DetailHtml { get; set; } = string.Empty;   // 상품 상세 텍스트 (HTML)
    public string VideoUrl { get; set; } = string.Empty;     // 유튜브 링크 또는 업로드 동영상 경로
    public string DetailImagesJson { get; set; } = "[]";     // 상세 이미지 목록 (JSON 배열)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>구매 가능 여부 (무한 재고이거나 재고 > 0).</summary>
    public bool IsAvailable => IsUnlimitedStock || Stock > 0;
}
