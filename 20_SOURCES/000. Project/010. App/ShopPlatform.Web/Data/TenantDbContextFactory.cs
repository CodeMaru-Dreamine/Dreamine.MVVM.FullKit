using Microsoft.EntityFrameworkCore;
using ShopPlatform.Models;

namespace ShopPlatform.Data;

/// <summary>슬러그별 TenantDbContext 를 생성·마이그레이션. Singleton.</summary>
public sealed class TenantDbContextFactory
{
    private readonly ShopOptions _opts;
    private readonly ILogger<TenantDbContextFactory> _logger;

    public TenantDbContextFactory(ShopOptions opts, ILogger<TenantDbContextFactory> logger)
    {
        _opts = opts;
        _logger = logger;
    }

    /// <summary>슬러그에 해당하는 DB 컨텍스트를 반환. DB 파일·스키마 없으면 자동 생성.</summary>
    public TenantDbContext Create(string slug)
    {
        var dbPath = GetDbPath(slug);
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        var opts = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;

        var ctx = new TenantDbContext(opts);
        ctx.Database.EnsureCreated();
        ApplyColumnMigrations(ctx);
        return ctx;
    }

    /// <summary>
    /// EnsureCreated는 스키마 변경을 적용하지 않으므로,
    /// 모델에 추가된 컬럼을 기존 DB에 수동으로 추가합니다.
    /// SQLite는 ALTER TABLE ... ADD COLUMN만 지원 (DROP/MODIFY 불가).
    /// </summary>
    private void ApplyColumnMigrations(TenantDbContext ctx)
    {
        // Products 테이블 컬럼 목록 조회
        var existingColumns = ctx.Database
            .SqlQueryRaw<string>("SELECT name FROM pragma_table_info('Products')")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!existingColumns.Contains("IsFeatured"))
        {
            ctx.Database.ExecuteSqlRaw(
                "ALTER TABLE Products ADD COLUMN IsFeatured INTEGER NOT NULL DEFAULT 0");
            _logger.LogInformation("Migration: Products.IsFeatured 컬럼 추가");
        }

        if (!existingColumns.Contains("SortOrder"))
        {
            ctx.Database.ExecuteSqlRaw(
                "ALTER TABLE Products ADD COLUMN SortOrder INTEGER NOT NULL DEFAULT 0");
            _logger.LogInformation("Migration: Products.SortOrder 컬럼 추가");
        }

        if (!existingColumns.Contains("DetailHtml"))
        {
            ctx.Database.ExecuteSqlRaw(
                "ALTER TABLE Products ADD COLUMN DetailHtml TEXT NOT NULL DEFAULT ''");
            _logger.LogInformation("Migration: Products.DetailHtml 컬럼 추가");
        }

        if (!existingColumns.Contains("VideoUrl"))
        {
            ctx.Database.ExecuteSqlRaw(
                "ALTER TABLE Products ADD COLUMN VideoUrl TEXT NOT NULL DEFAULT ''");
            _logger.LogInformation("Migration: Products.VideoUrl 컬럼 추가");
        }

        if (!existingColumns.Contains("IsUnlimitedStock"))
        {
            ctx.Database.ExecuteSqlRaw(
                "ALTER TABLE Products ADD COLUMN IsUnlimitedStock INTEGER NOT NULL DEFAULT 0");
            _logger.LogInformation("Migration: Products.IsUnlimitedStock 컬럼 추가");
        }

        if (!existingColumns.Contains("DetailImagesJson"))
        {
            ctx.Database.ExecuteSqlRaw(
                "ALTER TABLE Products ADD COLUMN DetailImagesJson TEXT NOT NULL DEFAULT '[]'");
            _logger.LogInformation("Migration: Products.DetailImagesJson 컬럼 추가");
        }
    }

    public string GetDbPath(string slug)
        => Path.Combine(_opts.ResolvedDataPath, slug, "shop.db");
}
