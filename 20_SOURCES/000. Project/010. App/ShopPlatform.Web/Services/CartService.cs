using ShopPlatform.Models;

namespace ShopPlatform.Services;

/// <summary>세션 기반 장바구니 (Scoped). 실제 앱에서는 Blazor Server 회로 수명에 맞춤.</summary>
public sealed class CartService
{
    private readonly List<CartItem> _items = new();

    public IReadOnlyList<CartItem> Items => _items;
    public decimal Total => _items.Sum(i => i.Subtotal);
    public int Count => _items.Sum(i => i.Quantity);

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

    public void Remove(int productId)
        => _items.RemoveAll(i => i.ProductId == productId);

    public void UpdateQuantity(int productId, int quantity)
    {
        var item = _items.FirstOrDefault(i => i.ProductId == productId);
        if (item == null) return;
        if (quantity <= 0) Remove(productId);
        else item.Quantity = quantity;
    }

    public void Clear() => _items.Clear();
}
