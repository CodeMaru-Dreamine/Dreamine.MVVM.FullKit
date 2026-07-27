using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Wedding.Common;
using Wedding.Layouts.Contracts;
using WeddingPlatform.Services;
using Xunit;

namespace WeddingPlatform.LayoutWorkflow.Tests;

public sealed class LayoutPolicyQueryMiddlewareTests
{
    [Fact]
    public async Task BuiltInPolicy_ReturnsProtectedReadOnlyStatus()
    {
        var context = CreateContext("/api/layout-definition-policies/card");
        var middleware = new WeddingLayoutPolicyQueryMiddleware(
            _ => Task.CompletedTask,
            new StubRegistry());

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        var status = await ReadStatusAsync(context);
        Assert.Equal("card", status.LayoutKey);
        Assert.Equal(LayoutTier.Premium, status.Tier);
        Assert.Equal(0, status.Revision);
        Assert.True(status.IsBuiltIn);
    }

    [Fact]
    public async Task CustomPolicy_ReturnsServerPolicyWithoutAdministrativeDetails()
    {
        var policy = new LayoutDefinitionPolicy
        {
            SchemaVersion = LayoutDefinitionPolicy.SupportedSchemaVersion,
            LayoutKey = "romantic-book",
            Tier = LayoutTier.Free,
            ClassifiedBy = "secret-admin",
            ClassifiedAtUtc = DateTimeOffset.UtcNow,
            Reason = "internal reason",
            Revision = 7,
        };
        var context = CreateContext(
            "/api/layout-definition-policies/romantic-book");
        var middleware = new WeddingLayoutPolicyQueryMiddleware(
            _ => Task.CompletedTask,
            new StubRegistry(policy));

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        var status = await ReadStatusAsync(context);
        Assert.Equal("romantic-book", status.LayoutKey);
        Assert.Equal(LayoutTier.Free, status.Tier);
        Assert.Equal(7, status.Revision);
        Assert.False(status.IsBuiltIn);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, leaveOpen: true);
        var json = await reader.ReadToEndAsync();
        Assert.DoesNotContain("secret-admin", json, StringComparison.Ordinal);
        Assert.DoesNotContain("internal reason", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnknownPolicy_ReturnsNotFound()
    {
        var context = CreateContext(
            "/api/layout-definition-policies/not-classified");
        var middleware = new WeddingLayoutPolicyQueryMiddleware(
            _ => Task.CompletedTask,
            new StubRegistry());

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    private static DefaultHttpContext CreateContext(string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<LayoutDefinitionPolicyStatus> ReadStatusAsync(
        DefaultHttpContext context)
    {
        context.Response.Body.Position = 0;
        var status = await JsonSerializer.DeserializeAsync<LayoutDefinitionPolicyStatus>(
            context.Response.Body,
            LayoutPackageJson.CreateOptions());
        return Assert.IsType<LayoutDefinitionPolicyStatus>(status);
    }

    private sealed class StubRegistry : IWeddingLayoutCatalogRegistry
    {
        public StubRegistry(params LayoutDefinitionPolicy[] policies)
        {
            DefinitionPolicies = policies.ToDictionary(
                x => x.LayoutKey,
                StringComparer.OrdinalIgnoreCase);
        }

        public WeddingLayoutCatalog Current => WeddingLayoutCatalog.Instance;

        public IReadOnlyDictionary<WeddingLayoutReleaseId, WeddingLayoutPublishedPackage>
            PublishedPackages { get; } =
                new Dictionary<WeddingLayoutReleaseId, WeddingLayoutPublishedPackage>();

        public IReadOnlyDictionary<string, LayoutDefinitionPolicy>
            DefinitionPolicies { get; }

        public string LayoutPackagesRoot => "";

        public event EventHandler<WeddingLayoutCatalogChangedEventArgs>? Changed
        {
            add { }
            remove { }
        }

        public Task<WeddingLayoutReloadResult> ReloadAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new WeddingLayoutReloadResult(
                Succeeded: true,
                Changed: false,
                PublishedReleaseCount: 0,
                Error: null));
    }
}
