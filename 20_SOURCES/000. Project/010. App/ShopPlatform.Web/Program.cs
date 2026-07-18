using Dreamine.Identity;
using Dreamine.Identity.Options;
using Microsoft.EntityFrameworkCore;
using ShopPlatform.Data;
using ShopPlatform.Middleware;
using ShopPlatform.Models;
using ShopPlatform.Payments;
using ShopPlatform.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddUserSecrets("codemaru-oauth-2ba4e1b2");

// ── 옵션 ─────────────────────────────────────────────────────────────
var shopOpts = ShopOptions.From(builder.Configuration);
builder.Services.AddSingleton(shopOpts);

// ── Dreamine.Identity (CodeMaru 공용 쿠키 로그인) ─────────────────────
AuthOptions authOptions =
    builder.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();
string usersDbPath = ResolvePath(
    builder.Configuration[$"{AuthOptions.SectionName}:UsersDbPath"],
    Path.Combine(AppContext.BaseDirectory, "App_Data", "codemaru.db"));
builder.Services.AddDreamineIdentityWeb(authOptions, usersDbPath);

// DataProtection은 Dreamine.Identity가 CodeMaru 공용 경로에 이미 등록.
// 결제 시크릿 키(PaymentKeyProtector)는 동일 KeyRing을 "ShopPlatform.PaymentKeys"
// 목적 문자열로 격리해서 그대로 사용한다.

// ── 멀티테넌트 서비스 ─────────────────────────────────────────────────
builder.Services.AddSingleton<IShopTenantStore, JsonShopTenantStore>();
builder.Services.AddScoped<TenantContext>();
builder.Services.AddSingleton<PaymentKeyProtector>();
builder.Services.AddSingleton<TenantDbContextFactory>();

// ── 결제 ─────────────────────────────────────────────────────────────
builder.Services.AddHttpClient();

// ── 장바구니 + 고객 세션 (Scoped = Blazor 회로 수명) ──────────────────
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<ShopCustomerSession>();
builder.Services.AddScoped<ShopUserContext>();
builder.Services.AddSingleton<ShopCustomerProfileStore>();
builder.Services.AddScoped<ShopCustomerLoginSync>();

// ── Blazor Server ─────────────────────────────────────────────────────
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ── 압축 ─────────────────────────────────────────────────────────────
builder.Services.AddResponseCompression(o => o.EnableForHttps = true);

var app = builder.Build();

// ── 미들웨어 파이프라인 ───────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// 리버스 프록시 뒤에서 X-Forwarded-* 헤더 인식 (쿠키 SameSite/Secure 판정용)
app.UseForwardedHeaders();
app.UseResponseCompression();
app.UseStaticFiles();

// 샵별 업로드 이미지 서빙: App_Data/Shops/{slug}/images/ → /shop-img/{slug}/
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(shopOpts.ResolvedDataPath),
    RequestPath = "/shop-img"
});

// 테넌트 미들웨어: 경로에서 slug 추출 → TenantContext 주입
app.UseMiddleware<TenantMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

// 카카오·SNS 봇 → OG 메타태그 전용 HTML 반환
app.UseMiddleware<OgBotMiddleware>();

// ── 헬스체크 ──────────────────────────────────────────────────────────
app.MapGet("/healthz", () => Results.Text("OK", "text/plain"));

// ── 결제 콜백 엔드포인트 (/pay/{slug}/return, /pay/{slug}/fail) ────────
app.MapGet("/pay/{slug}/return", async (
    string slug,
    string paymentKey, string orderId, int amount,
    IShopTenantStore store,
    PaymentKeyProtector protector,
    IHttpClientFactory httpFactory,
    TenantDbContextFactory dbFactory) =>
{
    var config = await store.GetAsync(slug);
    if (config == null) return Results.NotFound();

    // 테넌트별 게이트웨이 생성
    IPaymentGateway gateway = config.Payment.IsEnabled
        ? new TossPaymentGateway(httpFactory.CreateClient(), config.Payment, protector)
        : new DummyPaymentGateway();

    var result = await gateway.ConfirmAsync(paymentKey, orderId, amount);
    if (!result.Succeeded)
        return Results.Redirect($"/{slug}/checkout?error={Uri.EscapeDataString(result.Error ?? "결제 실패")}");

    // 주문 상태 업데이트 + 재고 차감
    using var db = dbFactory.Create(slug);
    var order = db.Orders
        .Include(o => o.Lines)
        .FirstOrDefault(o => o.OrderNo == orderId);
    if (order != null)
    {
        order.Status = "paid";
        order.TransactionId = result.TransactionId;
        DeductStock(db, order.Lines);
        await db.SaveChangesAsync();
    }

    return Results.Redirect($"/{slug}/order/{orderId}?paid=1");
});

app.MapGet("/pay/{slug}/fail", (string slug, string? code, string? message) =>
    Results.Redirect($"/{slug}/checkout?error={Uri.EscapeDataString(message ?? "결제가 취소되었습니다.")}"));

// ── Blazor Razor Components ───────────────────────────────────────────
app.MapRazorComponents<ShopPlatform.Components.App>()
   .AddInteractiveServerRenderMode();

// ── 기본 OG 이미지 생성 ───────────────────────────────────
var wwwroot = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
ShopPlatform.Services.OgImageGenerator.EnsureDefault(wwwroot);

// ── 샘플 데이터 시드 ──────────────────────────────────────
await ShopPlatform.Data.ShopSeeder.SeedCodemaruAsync(
    app.Services.GetRequiredService<TenantDbContextFactory>(),
    app.Services.GetRequiredService<IShopTenantStore>(),
    app.Services.GetRequiredService<PaymentKeyProtector>());

app.Run();

// 재고 차감 — 무한 재고 상품은 건드리지 않음
#pragma warning disable CS1587
/// \cond LOCAL_FUNCTION_DOCUMENTATION
/// <summary>
/// \if KO
/// <para>주문 항목의 수량만큼 재고를 차감하며 무한 재고 상품은 건너뜁니다.</para>
/// \endif
/// \if EN
/// <para>Deducts ordered quantities from stock while skipping unlimited-stock products.</para>
/// \endif
/// </summary>
/// <param name="db">
/// \if KO
/// <para>상품 재고를 저장하는 테넌트 데이터베이스 컨텍스트입니다.</para>
/// \endif
/// \if EN
/// <para>The tenant database context that stores product inventory.</para>
/// \endif
/// </param>
/// <param name="lines">
/// \if KO
/// <para>재고에 반영할 주문 항목입니다.</para>
/// \endif
/// \if EN
/// <para>The order lines to apply to inventory.</para>
/// \endif
/// </param>
/// \endcond
static void DeductStock(TenantDbContext db, IEnumerable<OrderLine> lines)
{
    foreach (var line in lines)
    {
        var product = db.Products.Find(line.ProductId);
        if (product == null || product.IsUnlimitedStock) continue;
        product.Stock = Math.Max(0, product.Stock - line.Quantity);
    }
}
#pragma warning restore CS1587

// appsettings 경로가 상대 경로면 실행 파일 기준으로 절대 경로로 확장
#pragma warning disable CS1587
/// \cond LOCAL_FUNCTION_DOCUMENTATION
/// <summary>
/// \if KO
/// <para>구성 경로를 실행 파일 기준의 절대 경로로 해석합니다.</para>
/// \endif
/// \if EN
/// <para>Resolves a configured path to an absolute path relative to the executable.</para>
/// \endif
/// </summary>
/// <param name="configuredPath">
/// \if KO
/// <para>구성에서 읽은 선택적 경로입니다.</para>
/// \endif
/// \if EN
/// <para>The optional configured path.</para>
/// \endif
/// </param>
/// <param name="fallback">
/// \if KO
/// <para>구성 경로가 없을 때 사용할 기본 경로입니다.</para>
/// \endif
/// \if EN
/// <para>The fallback used when the configured path is absent.</para>
/// \endif
/// </param>
/// <returns>
/// \if KO
/// <para>해석된 절대 경로입니다.</para>
/// \endif
/// \if EN
/// <para>The resolved absolute path.</para>
/// \endif
/// </returns>
/// \endcond
static string ResolvePath(string? configuredPath, string fallback)
{
    if (string.IsNullOrWhiteSpace(configuredPath))
    {
        return fallback;
    }

    return Path.IsPathRooted(configuredPath)
        ? configuredPath
        : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredPath));
}
#pragma warning restore CS1587
