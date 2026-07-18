namespace ShopPlatform.Models;

/// <summary>
/// \if KO
/// <para>Product 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates product functionality and related state.</para>
/// \endif
/// </summary>
public sealed class Product
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
    /// <para>Name 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the name value.</para>
    /// \endif
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>Description 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the description value.</para>
    /// \endif
    /// </summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>Price 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the price value.</para>
    /// \endif
    /// </summary>
    public decimal Price { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Stock 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the stock value.</para>
    /// \endif
    /// </summary>
    public int Stock { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Is Unlimited Stock 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the is unlimited stock value.</para>
    /// \endif
    /// </summary>
    public bool IsUnlimitedStock { get; set; } = false; // true면 재고 수량 무시
    /// <summary>
    /// \if KO
    /// <para>Image Path 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the image path value.</para>
    /// \endif
    /// </summary>
    public string? ImagePath { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Category 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the category value.</para>
    /// \endif
    /// </summary>
    public string Category { get; set; } = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>Is Active 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the is active value.</para>
    /// \endif
    /// </summary>
    public bool IsActive { get; set; } = true;
    /// <summary>
    /// \if KO
    /// <para>Is Featured 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the is featured value.</para>
    /// \endif
    /// </summary>
    public bool IsFeatured { get; set; } = false;
    /// <summary>
    /// \if KO
    /// <para>Sort Order 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the sort order value.</para>
    /// \endif
    /// </summary>
    public int SortOrder { get; set; } = 0;
    /// <summary>
    /// \if KO
    /// <para>Detail Html 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the detail html value.</para>
    /// \endif
    /// </summary>
    public string DetailHtml { get; set; } = string.Empty;   // 상품 상세 텍스트 (HTML)
    /// <summary>
    /// \if KO
    /// <para>Video Url 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the video url value.</para>
    /// \endif
    /// </summary>
    public string VideoUrl { get; set; } = string.Empty;     // 유튜브 링크 또는 업로드 동영상 경로
    /// <summary>
    /// \if KO
    /// <para>Detail Images Json 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the detail images json value.</para>
    /// \endif
    /// </summary>
    public string DetailImagesJson { get; set; } = "[]";     // 상세 이미지 목록 (JSON 배열)
    /// <summary>
    /// \if KO
    /// <para>Created At 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the created at value.</para>
    /// \endif
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// \if KO
    /// <para>구매 가능 여부 (무한 재고이거나 재고 > 0).</para>
    /// \endif
    /// \if EN
    /// <para>Gets the is available value.</para>
    /// \endif
    /// </summary>
    public bool IsAvailable => IsUnlimitedStock || Stock > 0;
}
