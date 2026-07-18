namespace ShopPlatform.Models;

/// <summary>
/// \if KO
/// <para>Cart Item 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates cart item functionality and related state.</para>
/// \endif
/// </summary>
public sealed class CartItem
{
    /// <summary>
    /// \if KO
    /// <para>Product Id 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the product id value.</para>
    /// \endif
    /// </summary>
    public int ProductId { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Product Name 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the product name value.</para>
    /// \endif
    /// </summary>
    public string ProductName { get; set; } = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>Unit Price 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the unit price value.</para>
    /// \endif
    /// </summary>
    public decimal UnitPrice { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Quantity 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the quantity value.</para>
    /// \endif
    /// </summary>
    public int Quantity { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Subtotal 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the subtotal value.</para>
    /// \endif
    /// </summary>
    public decimal Subtotal => UnitPrice * Quantity;
}
