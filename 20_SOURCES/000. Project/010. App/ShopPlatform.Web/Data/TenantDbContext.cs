using Microsoft.EntityFrameworkCore;
using ShopPlatform.Models;

namespace ShopPlatform.Data;

/// <summary>
/// \if KO
/// <para>테넌트별 SQLite DbContext. 상품/주문/주문라인 테이블.</para>
/// \endif
/// \if EN
/// <para>Encapsulates tenant db context functionality and related state.</para>
/// \endif
/// </summary>
public sealed class TenantDbContext : DbContext
{
    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="TenantDbContext"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="TenantDbContext"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="options">
    /// \if KO
    /// <para>동작을 구성하는 설정입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The options that configure the operation.</para>
    /// \endif
    /// </param>
    public TenantDbContext(DbContextOptions<TenantDbContext> options) : base(options) { }

    /// <summary>
    /// \if KO
    /// <para>Products 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the products value.</para>
    /// \endif
    /// </summary>
    public DbSet<Product> Products => Set<Product>();
    /// <summary>
    /// \if KO
    /// <para>Orders 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the orders value.</para>
    /// \endif
    /// </summary>
    public DbSet<Order> Orders => Set<Order>();
    /// <summary>
    /// \if KO
    /// <para>Order Lines 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the order lines value.</para>
    /// \endif
    /// </summary>
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    /// <summary>
    /// \if KO
    /// <para>Users 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the users value.</para>
    /// \endif
    /// </summary>
    public DbSet<ShopUser> Users => Set<ShopUser>();

    /// <summary>
    /// \if KO
    /// <para>Model Creating 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the model creating event or state change.</para>
    /// \endif
    /// </summary>
    /// <param name="mb">
    /// \if KO
    /// <para>mb에 사용할 <c>ModelBuilder</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ModelBuilder</c> value used for mb.</para>
    /// \endif
    /// </param>
    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Product>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Price).HasColumnType("TEXT"); // SQLite decimal 호환
        });

        mb.Entity<Order>(e =>
        {
            e.HasKey(o => o.Id);
            e.Property(o => o.TotalAmount).HasColumnType("TEXT");
            e.HasMany(o => o.Lines)
             .WithOne()
             .HasForeignKey(l => l.OrderId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<OrderLine>(e =>
        {
            e.HasKey(l => l.Id);
            e.Property(l => l.UnitPrice).HasColumnType("TEXT");
            e.Ignore(l => l.Subtotal);
        });

        mb.Entity<ShopUser>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Email).IsUnique();
        });
    }
}
