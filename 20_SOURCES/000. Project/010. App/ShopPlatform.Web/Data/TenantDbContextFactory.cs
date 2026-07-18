using Microsoft.EntityFrameworkCore;
using ShopPlatform.Models;

namespace ShopPlatform.Data;

/// <summary>
/// \if KO
/// <para>슬러그별 TenantDbContext 를 생성·마이그레이션. Singleton.</para>
/// \endif
/// \if EN
/// <para>Encapsulates tenant db context factory functionality and related state.</para>
/// \endif
/// </summary>
public sealed class TenantDbContextFactory
{
    /// <summary>
    /// \if KO
    /// <para>opts 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the opts value.</para>
    /// \endif
    /// </summary>
    private readonly ShopOptions _opts;
    /// <summary>
    /// \if KO
    /// <para>logger 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the logger value.</para>
    /// \endif
    /// </summary>
    private readonly ILogger<TenantDbContextFactory> _logger;

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="TenantDbContextFactory"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="TenantDbContextFactory"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="opts">
    /// \if KO
    /// <para>opts에 사용할 <c>ShopOptions</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ShopOptions</c> value used for opts.</para>
    /// \endif
    /// </param>
    /// <param name="logger">
    /// \if KO
    /// <para>logger에 사용할 <c>ILogger&lt;TenantDbContextFactory&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ILogger&lt;TenantDbContextFactory&gt;</c> value used for logger.</para>
    /// \endif
    /// </param>
    public TenantDbContextFactory(ShopOptions opts, ILogger<TenantDbContextFactory> logger)
    {
        _opts = opts;
        _logger = logger;
    }

    /// <summary>
    /// \if KO
    /// <para>슬러그에 해당하는 DB 컨텍스트를 반환. DB 파일·스키마 없으면 자동 생성.</para>
    /// \endif
    /// \if EN
    /// <para>Creates the value.</para>
    /// \endif
    /// </summary>
    /// <param name="slug">
    /// \if KO
    /// <para>slug에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for slug.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Create 작업에서 생성한 <c>TenantDbContext</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>TenantDbContext</c> result produced by the create operation.</para>
    /// \endif
    /// </returns>
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
    /// \if KO
    /// <para>EnsureCreated는 스키마 변경을 적용하지 않으므로, 모델에 추가된 컬럼을 기존 DB에 수동으로 추가합니다. SQLite는 ALTER TABLE ... ADD COLUMN만 지원 (DROP/MODIFY 불가).</para>
    /// \endif
    /// \if EN
    /// <para>Performs the apply column migrations operation.</para>
    /// \endif
    /// </summary>
    /// <param name="ctx">
    /// \if KO
    /// <para>ctx에 사용할 <c>TenantDbContext</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>TenantDbContext</c> value used for ctx.</para>
    /// \endif
    /// </param>
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

    /// <summary>
    /// \if KO
    /// <para>Db Path 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the db path value.</para>
    /// \endif
    /// </summary>
    /// <param name="slug">
    /// \if KO
    /// <para>slug에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for slug.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get Db Path 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the get db path operation.</para>
    /// \endif
    /// </returns>
    public string GetDbPath(string slug)
        => Path.Combine(_opts.ResolvedDataPath, slug, "shop.db");
}
