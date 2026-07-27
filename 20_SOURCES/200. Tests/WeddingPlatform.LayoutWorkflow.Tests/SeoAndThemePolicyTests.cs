using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Wedding.Common;
using WeddingPlatform.Models;
using WeddingPlatform.Services;
using Xunit;

namespace WeddingPlatform.LayoutWorkflow.Tests;

public sealed class SeoAndThemePolicyTests
{
    private static readonly string[] PublicSampleSlugs =
    [
        "seojun-hayeon",
        "jihoon-sua",
        "jiho-yujin",
        "minjun-seoyeon",
        "doyun-harin",
    ];

    [Fact]
    public void LegacySeoFallbackIndexesOnlyTheFivePublishedSamples()
    {
        foreach (var slug in PublicSampleSlugs)
        {
            Assert.True(WeddingSeoService.IsIndexingEnabled(
                new TenantConfig { Slug = slug, IsPublished = true }));
        }

        Assert.False(WeddingSeoService.IsIndexingEnabled(
            new TenantConfig { Slug = "new-couple", IsPublished = true }));
        Assert.False(WeddingSeoService.IsIndexingEnabled(
            new TenantConfig
            {
                Slug = PublicSampleSlugs[0],
                IsPublished = true,
                AllowSearchIndexing = false,
            }));
        Assert.True(WeddingSeoService.IsIndexingEnabled(
            new TenantConfig
            {
                Slug = "new-couple",
                IsPublished = true,
                AllowSearchIndexing = true,
            }));
        Assert.False(WeddingSeoService.IsIndexingEnabled(
            new TenantConfig
            {
                Slug = PublicSampleSlugs[0],
                IsPublished = false,
                AllowSearchIndexing = true,
            }));
    }

    [Fact]
    public async Task SitemapContainsOnlyEnabledPublishedInvitations()
    {
        var includedSample = new TenantConfig
        {
            Slug = PublicSampleSlugs[0],
            IsPublished = true,
            CreatedAt = new DateTime(2026, 7, 1),
            UpdatedAt = new DateTime(2026, 7, 20),
        };
        var includedExplicit = new TenantConfig
        {
            Slug = "new-couple",
            IsPublished = true,
            AllowSearchIndexing = true,
            CreatedAt = new DateTime(2026, 7, 2),
        };
        var excludedExplicit = new TenantConfig
        {
            Slug = PublicSampleSlugs[1],
            IsPublished = true,
            AllowSearchIndexing = false,
        };
        var excludedUnpublished = new TenantConfig
        {
            Slug = PublicSampleSlugs[2],
            IsPublished = false,
            AllowSearchIndexing = true,
        };

        var xml = await WeddingSeoService.BuildSitemapXmlAsync(
            new InMemoryTenantStore(
                includedSample,
                includedExplicit,
                excludedExplicit,
                excludedUnpublished));

        var document = XDocument.Parse(xml);
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var urls = document
            .Root!
            .Elements(ns + "url")
            .Select(element => new
            {
                Location = (string?)element.Element(ns + "loc"),
                LastModified = (string?)element.Element(ns + "lastmod"),
            })
            .ToArray();

        Assert.Equal(3, urls.Length);
        Assert.Contains(urls, item =>
            item.Location == $"{WeddingSeoService.SiteBaseUrl}/{includedSample.Slug}"
            && item.LastModified == "2026-07-20");
        Assert.Contains(urls, item =>
            item.Location == $"{WeddingSeoService.SiteBaseUrl}/{includedExplicit.Slug}"
            && item.LastModified == "2026-07-02");
        Assert.DoesNotContain(urls, item =>
            item.Location?.EndsWith('/' + excludedExplicit.Slug, StringComparison.Ordinal) == true);
        Assert.DoesNotContain(urls, item =>
            item.Location?.EndsWith('/' + excludedUnpublished.Slug, StringComparison.Ordinal) == true);
    }

    [Fact]
    public void RobotsAndThemePoliciesMatchThePublishedProductRules()
    {
        var robots = WeddingSeoService.BuildRobotsText();
        Assert.Contains(
            $"Sitemap: {WeddingSeoService.SiteBaseUrl}/sitemap.xml",
            robots,
            StringComparison.Ordinal);
        Assert.Contains("Disallow: /*/admin", robots, StringComparison.Ordinal);

        Assert.Equal(5, WeddingThemeCatalog.Options.Count);
        Assert.All(
            WeddingThemeCatalog.Options,
            option => Assert.Equal(WeddingThemeTier.Free, option.Tier));

        var policy = new WeddingThemeAccessPolicy();
        Assert.All(
            WeddingThemeCatalog.Options,
            option => Assert.True(policy.CanUse(option.Key, new WeddingThemeAccessState())));
        Assert.False(policy.CanUse(
            WeddingThemeCatalog.CustomThemeKey,
            new WeddingThemeAccessState { HasPremiumPlan = false }));
        Assert.True(policy.CanUse(
            WeddingThemeCatalog.CustomThemeKey,
            new WeddingThemeAccessState { HasPremiumPlan = true }));
    }

    [Fact]
    public void StaticSeoFallbackFilesMatchTheDefaultPublicPolicy()
    {
        var fallbackRoot = Path.Combine(AppContext.BaseDirectory, "SeoFallback");
        var robots = File.ReadAllText(Path.Combine(fallbackRoot, "robots.txt"))
            .ReplaceLineEndings("\n")
            .Trim();
        var generatedRobots = WeddingSeoService.BuildRobotsText()
            .ReplaceLineEndings("\n")
            .Trim();

        Assert.Equal(generatedRobots, robots);

        var sitemap = XDocument.Load(Path.Combine(fallbackRoot, "sitemap.xml"));
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var locations = sitemap
            .Root!
            .Elements(ns + "url")
            .Select(element => (string?)element.Element(ns + "loc"))
            .Where(location => location is not null)
            .Cast<string>()
            .ToArray();
        var expected = new[] { WeddingSeoService.SiteBaseUrl + "/" }
            .Concat(PublicSampleSlugs
                .OrderBy(slug => slug, StringComparer.OrdinalIgnoreCase)
                .Select(slug => $"{WeddingSeoService.SiteBaseUrl}/{slug}"))
            .ToArray();

        Assert.Equal(expected, locations);
    }

    [Fact]
    public async Task DynamicSitemapTakesPriorityOverTheStaticFallback()
    {
        var nextCallCount = 0;
        var tenantStore = new InMemoryTenantStore(
            new TenantConfig
            {
                Slug = "new-public-couple",
                IsPublished = true,
                AllowSearchIndexing = true,
                CreatedAt = new DateTime(2026, 7, 26),
            });
        var middleware = new WeddingSeoMiddleware(
            _ =>
            {
                nextCallCount++;
                return Task.CompletedTask;
            },
            tenantStore,
            NullLogger<WeddingSeoMiddleware>.Instance);
        var context = CreateSeoContext(HttpMethods.Get, "/sitemap.xml");

        await middleware.InvokeAsync(context);

        Assert.Equal(0, nextCallCount);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal("application/xml; charset=utf-8", context.Response.ContentType);
        Assert.Equal("public, max-age=300", context.Response.Headers.CacheControl.ToString());
        var body = ReadResponseBody(context);
        Assert.Contains(
            $"{WeddingSeoService.SiteBaseUrl}/new-public-couple",
            body,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task SeoHeadResponseReturnsHeadersWithoutWritingABody()
    {
        var middleware = new WeddingSeoMiddleware(
            _ => Task.CompletedTask,
            new InMemoryTenantStore(),
            NullLogger<WeddingSeoMiddleware>.Instance);
        var context = CreateSeoContext(HttpMethods.Head, "/robots.txt");

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal("text/plain; charset=utf-8", context.Response.ContentType);
        Assert.True(context.Response.ContentLength > 0);
        Assert.Equal(0, context.Response.Body.Length);
    }

    [Fact]
    public async Task SitemapGenerationFailureFallsThroughToTheStaticFilePipeline()
    {
        const string fallbackContent = "STATIC-SITEMAP-FALLBACK";
        var nextCallCount = 0;
        var middleware = new WeddingSeoMiddleware(
            async context =>
            {
                nextCallCount++;
                await context.Response.Body.WriteAsync(
                    Encoding.UTF8.GetBytes(fallbackContent));
            },
            new ThrowingTenantStore(),
            NullLogger<WeddingSeoMiddleware>.Instance);
        var context = CreateSeoContext(HttpMethods.Get, "/sitemap.xml");

        await middleware.InvokeAsync(context);

        Assert.Equal(1, nextCallCount);
        Assert.Equal(fallbackContent, ReadResponseBody(context));
    }

    [Fact]
    public void PremiumLayoutAccessUsesOnlyTheAccountPlanAndMigratesLegacyGrants()
    {
        var policy = new WeddingLayoutAccessPolicy();
        var premiumOptions = WeddingLayoutCatalog.Options
            .Where(option => option.Tier == WeddingLayoutTier.Premium)
            .ToArray();
        var freeOptions = WeddingLayoutCatalog.Options
            .Where(option => option.Tier == WeddingLayoutTier.Free)
            .ToArray();

        Assert.NotEmpty(premiumOptions);
        Assert.NotEmpty(freeOptions);
        Assert.All(
            freeOptions,
            option => Assert.True(policy.CanUse(option, new WeddingLayoutAccessState())));

        var legacyGrantOnly = new WeddingLayoutAccessState
        {
            UnlockedLayoutKeys = premiumOptions.Select(option => option.Key).ToArray(),
            UnlockedLayouts = premiumOptions.Select(option => option.Mode).ToArray(),
        };
        Assert.All(
            premiumOptions,
            option => Assert.False(policy.CanUse(option, legacyGrantOnly)));
        Assert.All(
            premiumOptions,
            option => Assert.True(policy.CanUse(
                option,
                new WeddingLayoutAccessState { HasPremiumPlan = true })));

        var futurePremiumLayout = premiumOptions[0] with
        {
            CatalogKey = "future-premium-layout",
            Version = "2.0.0",
        };
        Assert.False(policy.CanUse(futurePremiumLayout, legacyGrantOnly));
        Assert.True(policy.CanUse(
            futurePremiumLayout,
            new WeddingLayoutAccessState { HasPremiumPlan = true }));

        var legacyJson =
            $"{{\"Slug\":\"legacy-premium\",\"HasPremiumPlan\":false," +
            $"\"UnlockedLayoutModes\":[\"{premiumOptions[0].Key}\"]}}";
        var legacyTenant = JsonSerializer.Deserialize<TenantConfig>(legacyJson);

        Assert.NotNull(legacyTenant);
        Assert.False(legacyTenant.HasPremiumPlan);
        Assert.Equal([premiumOptions[0].Key], legacyTenant.UnlockedLayoutModes);
        InvitationDesignCatalog.Normalize(legacyTenant);

        Assert.True(legacyTenant.HasPremiumPlan);
        Assert.Empty(legacyTenant.UnlockedLayoutModes);

        var freeTenant = new TenantConfig
        {
            Slug = "free-account",
            HasPremiumPlan = false,
        };
        InvitationDesignCatalog.Normalize(freeTenant);

        Assert.False(freeTenant.HasPremiumPlan);
        Assert.Empty(freeTenant.UnlockedLayoutModes);
    }

    private static DefaultHttpContext CreateSeoContext(string method, string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static string ReadResponseBody(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(
            context.Response.Body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            leaveOpen: true);
        return reader.ReadToEnd();
    }

    private sealed class InMemoryTenantStore(params TenantConfig[] tenants) : ITenantStore
    {
        private readonly Dictionary<string, TenantConfig> _tenants = tenants.ToDictionary(
            tenant => tenant.Slug,
            StringComparer.OrdinalIgnoreCase);

        public Task<TenantConfig?> GetAsync(string slug, CancellationToken ct = default) =>
            Task.FromResult(_tenants.GetValueOrDefault(slug));

        public Task<IReadOnlyList<TenantConfig>> GetAllAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<TenantConfig>>(_tenants.Values.ToArray());

        public Task SaveAsync(TenantConfig config, CancellationToken ct = default)
        {
            _tenants[config.Slug] = config;
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string slug, CancellationToken ct = default) =>
            Task.FromResult(_tenants.ContainsKey(slug));

        public Task DeleteAsync(string slug, CancellationToken ct = default)
        {
            _tenants.Remove(slug);
            return Task.CompletedTask;
        }

        public string GetTenantDataPath(string slug) => slug;
    }

    private sealed class ThrowingTenantStore : ITenantStore
    {
        public Task<TenantConfig?> GetAsync(string slug, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<TenantConfig>> GetAllAsync(CancellationToken ct = default) =>
            Task.FromException<IReadOnlyList<TenantConfig>>(
                new IOException("SEO tenant data is unavailable."));

        public Task SaveAsync(TenantConfig config, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<bool> ExistsAsync(string slug, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task DeleteAsync(string slug, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public string GetTenantDataPath(string slug) =>
            throw new NotSupportedException();
    }
}
