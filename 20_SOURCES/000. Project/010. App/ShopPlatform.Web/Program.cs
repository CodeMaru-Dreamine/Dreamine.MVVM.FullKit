using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using ShopPlatform.Data;
using ShopPlatform.Middleware;
using ShopPlatform.Models;
using ShopPlatform.Payments;
using ShopPlatform.Services;

var builder = WebApplication.CreateBuilder(args);

// ── 옵션 ─────────────────────────────────────────────────────────────
var shopOpts = ShopOptions.From(builder.Configuration);
builder.Services.AddSingleton(shopOpts);

// ── DataProtection (결제 시크릿 키 암호화용) ──────────────────────────
var keysPath = Path.Combine(shopOpts.ResolvedDataPath, ".keys");
Directory.CreateDirectory(keysPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
    .SetApplicationName("ShopPlatform");

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
static void DeductStock(TenantDbContext db, IEnumerable<OrderLine> lines)
{
    foreach (var line in lines)
    {
        var product = db.Products.Find(line.ProductId);
        if (product == null || product.IsUnlimitedStock) continue;
        product.Stock = Math.Max(0, product.Stock - line.Quantity);
    }
}
