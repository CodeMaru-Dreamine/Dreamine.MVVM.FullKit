using System.Xml.Linq;
using WeddingPlatform.Models;

namespace WeddingPlatform.Services;

/// <summary>
/// 청첩장 검색 노출 정책과 robots.txt 및 sitemap.xml 생성을 한 곳에서 관리합니다.
/// </summary>
public static class WeddingSeoService
{
    public const string SiteBaseUrl = "https://wedding.codemaru.co.kr";

    private static readonly HashSet<string> LegacyPublicSampleSlugs =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "seojun-hayeon",
            "jihoon-sua",
            "jiho-yujin",
            "minjun-seoyeon",
            "doyun-harin"
        };

    /// <summary>
    /// 기존 설정에 SEO 필드가 없는 경우에도 지정된 공개 샘플 5개만 최초 허용합니다.
    /// 관리자가 값을 한 번 저장하면 명시적인 true/false가 항상 우선합니다.
    /// </summary>
    public static bool IsIndexingEnabled(TenantConfig? tenant)
    {
        if (tenant is null || !tenant.IsPublished)
        {
            return false;
        }

        return tenant.AllowSearchIndexing
            ?? LegacyPublicSampleSlugs.Contains(tenant.Slug);
    }

    public static string ResolveSearchTitle(TenantConfig? tenant)
    {
        if (!string.IsNullOrWhiteSpace(tenant?.SearchTitle))
        {
            return tenant.SearchTitle.Trim();
        }

        var coupleName = string.IsNullOrWhiteSpace(tenant?.CoupleName)
            ? "모바일 청첩장"
            : tenant.CoupleName.Trim();
        return $"{coupleName} 모바일 청첩장";
    }

    public static string ResolveSearchDescription(TenantConfig? tenant)
    {
        if (!string.IsNullOrWhiteSpace(tenant?.SearchDescription))
        {
            return tenant.SearchDescription.Trim();
        }

        if (tenant is null)
        {
            return "사진, 음악, 지도와 방명록을 담은 CodeMaru 무료 모바일 청첩장입니다.";
        }

        var venue = string.IsNullOrWhiteSpace(tenant.VenueName)
            ? ""
            : $" {tenant.VenueName.Trim()}에서";
        var coupleName = string.IsNullOrWhiteSpace(tenant.CoupleName)
            ? "두 사람"
            : tenant.CoupleName.Trim();
        return $"{tenant.WeddingDate:yyyy년 M월 d일}{venue} 열리는 {coupleName}의 결혼식에 초대합니다.";
    }

    public static string BuildRobotsText() =>
        """
        User-agent: *
        Allow: /
        Disallow: /admin
        Disallow: /*/admin
        Disallow: /_identity/
        Disallow: /account

        Sitemap: https://wedding.codemaru.co.kr/sitemap.xml
        """;

    public static async Task<string> BuildSitemapXmlAsync(
        ITenantStore tenantStore,
        CancellationToken ct = default)
    {
        var tenants = await tenantStore.GetAllAsync(ct).ConfigureAwait(false);
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

        var root = new XElement(ns + "urlset",
            new XElement(ns + "url",
                new XElement(ns + "loc", SiteBaseUrl + "/")));

        foreach (var tenant in tenants
                     .Where(IsIndexingEnabled)
                     .OrderBy(x => x.Slug, StringComparer.OrdinalIgnoreCase))
        {
            var url = $"{SiteBaseUrl}/{Uri.EscapeDataString(tenant.Slug)}";
            var lastModified = tenant.UpdatedAt ?? tenant.CreatedAt;
            root.Add(new XElement(ns + "url",
                new XElement(ns + "loc", url),
                new XElement(ns + "lastmod", lastModified.ToString("yyyy-MM-dd"))));
        }

        return new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            root).ToString();
    }
}
