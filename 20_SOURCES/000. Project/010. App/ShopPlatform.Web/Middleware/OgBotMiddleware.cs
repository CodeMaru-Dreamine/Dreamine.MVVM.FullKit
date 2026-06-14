using ShopPlatform.Services;
using ShopPlatform.Models;

namespace ShopPlatform.Middleware;

/// <summary>
/// 카카오톡·SNS 크롤러가 /{slug} 에 접근하면
/// Blazor 없이 OG 메타태그만 담긴 최소 HTML을 즉시 반환합니다.
/// 일반 브라우저는 그대로 통과시킵니다.
/// </summary>
public sealed class OgBotMiddleware(RequestDelegate next)
{
    // 주요 SNS/메신저 크롤러 UA 키워드
    private static readonly string[] BotKeywords =
    [
        "kakaotalk", "facebookexternalhit", "twitterbot", "linkedinbot",
        "slackbot", "telegrambot", "whatsapp", "discordbot",
        "baiduspider", "yeti", "naverbot", "daumoa",
        "googlebot", "bingbot"
    ];

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

    private static bool IsBot(string ua)
        => BotKeywords.Any(k => ua.Contains(k));

    private static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin", "pay", "healthz", "_framework", "_blazor",
        "css", "js", "img", "shop-img", "favicon.ico"
    };

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

    private static string HtmlEncode(string s)
        => System.Net.WebUtility.HtmlEncode(s);
}
