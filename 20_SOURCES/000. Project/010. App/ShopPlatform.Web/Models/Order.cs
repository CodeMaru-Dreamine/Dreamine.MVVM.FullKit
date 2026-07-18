namespace ShopPlatform.Models;

/// <summary>
/// \if KO
/// <para>Order 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates order functionality and related state.</para>
/// \endif
/// </summary>
public sealed class Order
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
    /// <para>Order No 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the order no value.</para>
    /// \endif
    /// </summary>
    public string OrderNo { get; set; } = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>Customer Name 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the customer name value.</para>
    /// \endif
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>Customer Email 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the customer email value.</para>
    /// \endif
    /// </summary>
    public string CustomerEmail { get; set; } = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>Customer Phone 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the customer phone value.</para>
    /// \endif
    /// </summary>
    public string CustomerPhone { get; set; } = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>Shipping Address 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the shipping address value.</para>
    /// \endif
    /// </summary>
    public string ShippingAddress { get; set; } = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>Total Amount 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the total amount value.</para>
    /// \endif
    /// </summary>
    public decimal TotalAmount { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Status 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the status value.</para>
    /// \endif
    /// </summary>
    public string Status { get; set; } = "pending";
    /// <summary>
    /// \if KO
    /// <para>Transaction Id 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the transaction id value.</para>
    /// \endif
    /// </summary>
    public string? TransactionId { get; set; }
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
    /// <para>Lines 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the lines value.</para>
    /// \endif
    /// </summary>
    public List<OrderLine> Lines { get; set; } = new();
}

/// <summary>
/// \if KO
/// <para>Order Line 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates order line functionality and related state.</para>
/// \endif
/// </summary>
public sealed class OrderLine
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
    /// <para>Order Id 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the order id value.</para>
    /// \endif
    /// </summary>
    public int OrderId { get; set; }
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
