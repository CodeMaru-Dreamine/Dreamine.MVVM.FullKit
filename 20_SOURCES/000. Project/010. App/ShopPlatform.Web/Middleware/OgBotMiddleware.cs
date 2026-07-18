using ShopPlatform.Services;
using ShopPlatform.Models;

namespace ShopPlatform.Middleware;

/// <summary>
/// \if KO
/// <para>카카오톡·SNS 크롤러가 /{slug} 에 접근하면 Blazor 없이 OG 메타태그만 담긴 최소 HTML을 즉시 반환합니다. 일반 브라우저는 그대로 통과시킵니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates og bot middleware functionality and related state.</para>
/// \endif
/// </summary>
public sealed class OgBotMiddleware(RequestDelegate next)
{
    // 주요 SNS/메신저 크롤러 UA 키워드
    /// <summary>
    /// \if KO
    /// <para>Bot Keywords 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the bot keywords value.</para>
    /// \endif
    /// </summary>
    private static readonly string[] BotKeywords =
    [
        "kakaotalk", "facebookexternalhit", "twitterbot", "linkedinbot",
        "slackbot", "telegrambot", "whatsapp", "discordbot",
        "baiduspider", "yeti", "naverbot", "daumoa",
        "googlebot", "bingbot"
    ];

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
    /// <param name="store">
    /// \if KO
    /// <para>store에 사용할 <c>IShopTenantStore</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IShopTenantStore</c> value used for store.</para>
    /// \endif
    /// </param>
    /// <param name="opts">
    /// \if KO
    /// <para>opts에 사용할 <c>ShopOptions</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ShopOptions</c> value used for opts.</para>
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
    public async Task InvokeAsync(HttpContext ctx, IShopTenantStore store, ShopOptions opts)
    {
        var ua   = ctx.Request.Headers.UserAgent.ToString().ToLowerInvariant();
        var path = ctx.Request.Path.Value ?? string.Empty;

        // 봇이고 /{slug} 패턴이면 OG HTML 반환
        if (IsBot(ua) && IsShopPath(path, out var slug))
        {
            var config = await store.GetAsync(slug);
            if (config != null && config.IsActive)
            {
                await WriteOgHtml(ctx, config, opts, slug);
                return;
            }
        }

        await next(ctx);
    }

    /// <summary>
    /// \if KO
    /// <para>Is Bot 조건을 확인합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Determines whether is bot.</para>
    /// \endif
    /// </summary>
    /// <param name="ua">
    /// \if KO
    /// <para>ua에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for ua.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Is Bot 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the is bot condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    private static bool IsBot(string ua)
        => BotKeywords.Any(k => ua.Contains(k));

    /// <summary>
    /// \if KO
    /// <para>Reserved 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the reserved value.</para>
    /// \endif
    /// </summary>
    private static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin", "pay", "healthz", "_framework", "_blazor",
        "css", "js", "img", "shop-img", "favicon.ico"
    };

    /// <summary>
    /// \if KO
    /// <para>Is Shop Path 조건을 확인합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Determines whether is shop path.</para>
    /// \endif
    /// </summary>
    /// <param name="path">
    /// \if KO
    /// <para>path에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for path.</para>
    /// \endif
    /// </param>
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
    /// <para>Is Shop Path 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the is shop path condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    private static bool IsShopPath(string path, out string slug)
    {
        slug = string.Empty;
        var trimmed = path.Trim('/');
        // 루트(/) 또는 하위경로 제외, 단일 세그먼트만 처리
        if (string.IsNullOrEmpty(trimmed) || trimmed.Contains('/')) return false;
        if (Reserved.Contains(trimmed)) return false;
        slug = trimmed;
        return true;
    }

    /// <summary>
    /// \if KO
    /// <para>Og Html 데이터를 씁니다.</para>
    /// \endif
    /// \if EN
    /// <para>Writes og html data.</para>
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
    /// <param name="cfg">
    /// \if KO
    /// <para>cfg에 사용할 <c>ShopConfig</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ShopConfig</c> value used for cfg.</para>
    /// \endif
    /// </param>
    /// <param name="opts">
    /// \if KO
    /// <para>opts에 사용할 <c>ShopOptions</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ShopOptions</c> value used for opts.</para>
    /// \endif
    /// </param>
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
    /// <para>Write Og Html 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the write og html operation.</para>
    /// \endif
    /// </returns>
    private static async Task WriteOgHtml(HttpContext ctx, ShopConfig cfg, ShopOptions opts, string slug)
    {
        var baseUrl = opts.BaseUrl.TrimEnd('/');
        var title   = !string.IsNullOrEmpty(cfg.OgTitle)       ? cfg.OgTitle       : cfg.ShopName;
        var desc    = !string.IsNullOrEmpty(cfg.OgDescription) ? cfg.OgDescription
                    : !string.IsNullOrEmpty(cfg.Description)   ? cfg.Description
                    : $"{cfg.ShopName}에서 쇼핑하세요";
        var url     = $"{baseUrl}/{slug}";

        // OG 이미지: OgImagePath → LogoPath → 자동 생성
        string imgUrl;
        if (!string.IsNullOrEmpty(cfg.OgImagePath))
            imgUrl = cfg.OgImagePath.StartsWith("http") ? cfg.OgImagePath : $"{baseUrl}{cfg.OgImagePath}";
        else if (!string.IsNullOrEmpty(cfg.LogoPath))
            imgUrl = cfg.LogoPath.StartsWith("http") ? cfg.LogoPath : $"{baseUrl}{cfg.LogoPath}";
        else
            imgUrl = $"{baseUrl}/img/og/og-{slug}.png";

        var html = $"""
            <!DOCTYPE html>
            <html lang="ko">
            <head>
              <meta charset="utf-8" />
              <title>{HtmlEncode(title)}</title>
              <meta name="description" content="{HtmlEncode(desc)}" />
              <meta property="og:type"        content="website" />
              <meta property="og:url"         content="{url}" />
              <meta property="og:title"       content="{HtmlEncode(title)}" />
              <meta property="og:description" content="{HtmlEncode(desc)}" />
              <meta property="og:image"       content="{imgUrl}" />
              <meta property="og:image:width"  content="1200" />
              <meta property="og:image:height" content="630" />
              <meta property="og:site_name"   content="ShopPlatform" />
              <meta name="twitter:card"       content="summary_large_image" />
              <meta name="twitter:title"      content="{HtmlEncode(title)}" />
              <meta name="twitter:description" content="{HtmlEncode(desc)}" />
              <meta name="twitter:image"      content="{imgUrl}" />
            </head>
            <body>
              <script>location.href="{url}";</script>
              <a href="{url}">{HtmlEncode(title)}</a>
            </body>
            </html>
            """;

        ctx.Response.ContentType = "text/html; charset=utf-8";
        ctx.Response.StatusCode  = 200;
        await ctx.Response.WriteAsync(html);
    }

    /// <summary>
    /// \if KO
    /// <para>Html Encode 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the html encode operation.</para>
    /// \endif
    /// </summary>
    /// <param name="s">
    /// \if KO
    /// <para>이벤트를 발생시킨 객체입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The object that raised the event.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Html Encode 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the html encode operation.</para>
    /// \endif
    /// </returns>
    private static string HtmlEncode(string s)
        => System.Net.WebUtility.HtmlEncode(s);
}
