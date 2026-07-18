using ShopPlatform.Services;

namespace ShopPlatform.Middleware;

/// <summary>
/// \if KO
/// <para>요청 경로에서 {slug} 를 추출해 TenantContext 에 주입. 경로 패턴: /{slug}/... 또는 /{slug} 예약어 ("admin", "healthz", "_blazor", "_framework") 는 테넌트로 처리하지 않음.</para>
/// \endif
/// \if EN
/// <para>Encapsulates tenant middleware functionality and related state.</para>
/// \endif
/// </summary>
public sealed class TenantMiddleware
{
    /// <summary>
    /// \if KO
    /// <para>reserved 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the reserved value.</para>
    /// \endif
    /// </summary>
    private static readonly HashSet<string> _reserved = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin", "healthz", "_blazor", "_framework", "_content", "favicon.ico", "robots.txt", "sitemap.xml"
    };

    /// <summary>
    /// \if KO
    /// <para>next 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the next value.</para>
    /// \endif
    /// </summary>
    private readonly RequestDelegate _next;

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="TenantMiddleware"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="TenantMiddleware"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="next">
    /// \if KO
    /// <para>next에 사용할 <c>RequestDelegate</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>RequestDelegate</c> value used for next.</para>
    /// \endif
    /// </param>
    public TenantMiddleware(RequestDelegate next) => _next = next;

    /// <summary>
    /// \if KO
    /// <para>Invoke Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the invoke async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="ctx">
    /// \if KO
    /// <para>ctx에 사용할 <c>HttpContext</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>HttpContext</c> value used for ctx.</para>
    /// \endif
    /// </param>
    /// <param name="tenantCtx">
    /// \if KO
    /// <para>tenant Ctx에 사용할 <c>TenantContext</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>TenantContext</c> value used for tenant ctx.</para>
    /// \endif
    /// </param>
    /// <param name="store">
    /// \if KO
    /// <para>store에 사용할 <c>IShopTenantStore</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IShopTenantStore</c> value used for store.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Invoke Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the invoke async operation.</para>
    /// \endif
    /// </returns>
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
