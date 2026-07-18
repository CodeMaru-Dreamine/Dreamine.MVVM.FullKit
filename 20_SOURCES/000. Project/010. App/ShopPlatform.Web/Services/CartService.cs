using ShopPlatform.Models;

namespace ShopPlatform.Services;

/// <summary>
/// \if KO
/// <para>세션 기반 장바구니 (Scoped). 실제 앱에서는 Blazor Server 회로 수명에 맞춤.</para>
/// \endif
/// \if EN
/// <para>Encapsulates cart service functionality and related state.</para>
/// \endif
/// </summary>
public sealed class CartService
{
    /// <summary>
    /// \if KO
    /// <para>items 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the items value.</para>
    /// \endif
    /// </summary>
    private readonly List<CartItem> _items = new();

    /// <summary>
    /// \if KO
    /// <para>Items 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the items value.</para>
    /// \endif
    /// </summary>
    public IReadOnlyList<CartItem> Items => _items;
    /// <summary>
    /// \if KO
    /// <para>Total 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the total value.</para>
    /// \endif
    /// </summary>
    public decimal Total => _items.Sum(i => i.Subtotal);
    /// <summary>
    /// \if KO
    /// <para>Count 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the count value.</para>
    /// \endif
    /// </summary>
    public int Count => _items.Sum(i => i.Quantity);

    /// <summary>
    /// \if KO
    /// <para>항목을 추가합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Adds the item.</para>
    /// \endif
    /// </summary>
    /// <param name="product">
    /// \if KO
    /// <para>product에 사용할 <c>Product</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Product</c> value used for product.</para>
    /// \endif
    /// </param>
    /// <param name="quantity">
    /// \if KO
    /// <para>quantity에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for quantity.</para>
    /// \endif
    /// </param>
    public void Add(Product product, int quantity = 1)
    {
        var existing = _items.FirstOrDefault(i => i.ProductId == product.Id);
        if (existing != null)
            existing.Quantity += quantity;
        else
            _items.Add(new CartItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Quantity = quantity
            });
    }

    /// <summary>
    /// \if KO
    /// <para>항목을 제거합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Removes the item.</para>
    /// \endif
    /// </summary>
    /// <param name="productId">
    /// \if KO
    /// <para>product Id에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for product id.</para>
    /// \endif
    /// </param>
    public void Remove(int productId)
        => _items.RemoveAll(i => i.ProductId == productId);

    /// <summary>
    /// \if KO
    /// <para>Update Quantity 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the update quantity operation.</para>
    /// \endif
    /// </summary>
    /// <param name="productId">
    /// \if KO
    /// <para>product Id에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for product id.</para>
    /// \endif
    /// </param>
    /// <param name="quantity">
    /// \if KO
    /// <para>quantity에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for quantity.</para>
    /// \endif
    /// </param>
    public void UpdateQuantity(int productId, int quantity)
    {
        var item = _items.FirstOrDefault(i => i.ProductId == productId);
        if (item == null) return;
        if (quantity <= 0) Remove(productId);
        else item.Quantity = quantity;
    }

    /// <summary>
    /// \if KO
    /// <para>Clear 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the clear operation.</para>
    /// \endif
    /// </summary>
    public void Clear() => _items.Clear();
}
