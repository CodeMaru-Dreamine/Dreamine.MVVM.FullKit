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
    /// <para>Demo Client Key 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the demo client key value.</para>
    /// \endif
    /// </summary>
    private const string DemoClientKey = "test_gck_docs_Ovk5rk1EwkEbP0W43n07xlzm";
    /// <summary>
    /// \if KO
    /// <para>Demo Secret Key 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the demo secret key value.</para>
    /// \endif
    /// </summary>
    private const string DemoSecretKey = "test_gsk_docs_OaPz8L5KdmQXkzRz3y47BMw6";

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
    /// <param name="tenantStore">
    /// \if KO
    /// <para>tenant Store에 사용할 <c>IShopTenantStore</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IShopTenantStore</c> value used for tenant store.</para>
    /// \endif
    /// </param>
    /// <param name="keyProtector">
    /// \if KO
    /// <para>key Protector에 사용할 <c>PaymentKeyProtector</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>PaymentKeyProtector</c> value used for key protector.</para>
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
        IShopTenantStore tenantStore,
        PaymentKeyProtector keyProtector)
    {
        // 결제 키 설정 (config.json 업데이트)
        var config = await tenantStore.GetAsync("codemaru");
        if (config != null && !config.Payment.IsEnabled)
        {
            config.Payment.IsEnabled              = true;
            config.Payment.IsTestMode             = true;
            config.Payment.TossClientKey          = DemoClientKey;
            config.Payment.TossSecretKeyEncrypted = keyProtector.Protect(DemoSecretKey);
            config.Payment.SuccessReturnUrl       = string.Empty; // CheckoutPage에서 동적 생성
            config.Payment.FailReturnUrl          = string.Empty;
            await tenantStore.SaveAsync(config);
        }

        // 상품 시드
        using var db = dbFactory.Create("codemaru");
        if (db.Products.Any()) return;

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
        db.SaveChanges();
    }
}
