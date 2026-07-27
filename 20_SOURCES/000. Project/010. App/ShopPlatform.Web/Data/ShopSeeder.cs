using Microsoft.EntityFrameworkCore;
using ShopPlatform.Models;
using ShopPlatform.Services;

namespace ShopPlatform.Data;

/// <summary>
/// \if KO
/// <para>앱 시작 시 샘플 데이터 시드.</para>
/// \endif
/// \if EN
/// <para>Encapsulates shop seeder functionality and related state.</para>
/// \endif
/// </summary>
public static class ShopSeeder
{
    /// <summary>
    /// \if KO
    /// <para>Seed Codemaru Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the seed codemaru async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="dbFactory">
    /// \if KO
    /// <para>db Factory에 사용할 <c>TenantDbContextFactory</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>TenantDbContextFactory</c> value used for db factory.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Seed Codemaru Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the seed codemaru async operation.</para>
    /// \endif
    /// </returns>
    public static async Task SeedCodemaruAsync(
        TenantDbContextFactory dbFactory,
        CancellationToken cancellationToken = default)
    {
        // 상품 시드
        using var db = dbFactory.Create("codemaru");
        if (await db.Products.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        db.Products.AddRange(
            new Product
            {
                Name        = "청양 햇고추",
                Description = "국내산 청양 햇고추 500g. 매콤하고 신선한 고추를 직배송합니다.",
                Price       = 15_000m,
                Stock       = 100,
                IsActive    = true,
                ImagePath   = null
            },
            new Product
            {
                Name        = "Dreamine MVVM FullKit",
                Description = "WPF/MAUI 개발자를 위한 완성형 MVVM 프레임워크 라이선스. 소스코드 포함.",
                Price       = 59_000m,
                Stock       = 999,
                IsActive    = true,
                ImagePath   = null
            },
            new Product
            {
                Name        = "개발자 머그컵",
                Description = "코드마루 로고가 새겨진 도자기 머그컵 350ml.",
                Price       = 12_000m,
                Stock       = 50,
                IsActive    = true,
                ImagePath   = null
            }
        );
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
