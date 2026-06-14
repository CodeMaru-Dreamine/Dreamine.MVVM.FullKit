using ShopPlatform.Services;

namespace ShopPlatform.Middleware;

/// <summary>
/// 요청 경로에서 {slug} 를 추출해 TenantContext 에 주입.
/// 경로 패턴: /{slug}/... 또는 /{slug}
/// 예약어 ("admin", "healthz", "_blazor", "_framework") 는 테넌트로 처리하지 않음.
/// </summary>
public sealed class TenantMiddleware
{
    private static readonly HashSet<string> _reserved = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin", "healthz", "_blazor", "_framework", "_content", "favicon.ico", "robots.txt", "sitemap.xml"
    };

    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext ctx, TenantContext tenantCtx, IShopTenantStore store)
    {
        var path = ctx.Request.Path.Value ?? "/";
        var segments = path.TrimStart('/').Split('/', 2);
        var slug = segments[0];

        if (!string.IsNullOrEmpty(slug) && !_reserved.Contains(slug))
        {
            var config = await store.GetAsync(slug);
            if (config != null)
                tenantCtx.Config = config;
        }

        await _next(ctx);
    }
}
