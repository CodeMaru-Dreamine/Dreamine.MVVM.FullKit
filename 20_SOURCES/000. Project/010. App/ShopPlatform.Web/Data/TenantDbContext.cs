using Microsoft.EntityFrameworkCore;
using ShopPlatform.Models;

namespace ShopPlatform.Data;

/// <summary>테넌트별 SQLite DbContext. 상품/주문/주문라인 테이블.</summary>
public sealed class TenantDbContext : DbContext
{
    public TenantDbContext(DbContextOptions<TenantDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    public DbSet<ShopUser> Users => Set<ShopUser>();

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
