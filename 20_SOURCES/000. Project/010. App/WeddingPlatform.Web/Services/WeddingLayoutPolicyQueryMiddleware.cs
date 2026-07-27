using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Wedding.Common;
using Wedding.Layouts.Contracts;

namespace WeddingPlatform.Services;

/// <summary>
/// Adds the read-only policy projection used by the standalone WPF editor.
/// No administrative mutation endpoint is exposed here.
/// </summary>
public sealed class WeddingLayoutPolicyQueryStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        ArgumentNullException.ThrowIfNull(next);

        return app =>
        {
            app.UseMiddleware<WeddingLayoutPolicyQueryMiddleware>();
            next(app);
        };
    }
}

/// <summary>
/// Returns only the non-sensitive LayoutKey classification required by
/// authoring clients. Classifier identity and reason remain private to the
/// super-administrator workflow.
/// </summary>
public sealed class WeddingLayoutPolicyQueryMiddleware
{
    public const string RoutePrefix = "/api/layout-definition-policies/";

    private static readonly JsonSerializerOptions JsonOptions =
        LayoutPackageJson.CreateOptions();

    private readonly RequestDelegate _next;
    private readonly IWeddingLayoutCatalogRegistry _registry;

    public WeddingLayoutPolicyQueryMiddleware(
        RequestDelegate next,
        IWeddingLayoutCatalogRegistry registry)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if ((!HttpMethods.IsGet(context.Request.Method)
                && !HttpMethods.IsHead(context.Request.Method))
            || !TryReadLayoutKey(context.Request.Path, out var layoutKey))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        if (!WeddingLayoutKeys.IsValid(layoutKey))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var status = ResolveStatus(layoutKey);
        if (status is null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            context.Response.Headers.CacheControl = "no-store";
            return;
        }

        var bytes = JsonSerializer.SerializeToUtf8Bytes(status, JsonOptions);
        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.ContentLength = bytes.Length;
        context.Response.Headers.CacheControl = "no-store";
        if (!HttpMethods.IsHead(context.Request.Method))
        {
            await context.Response.Body
                .WriteAsync(bytes, context.RequestAborted)
                .ConfigureAwait(false);
        }
    }

    private LayoutDefinitionPolicyStatus? ResolveStatus(string layoutKey)
    {
        var builtIn = WeddingLayoutCatalog.Instance.FindDescriptor(layoutKey);
        if (builtIn is { IsBuiltIn: true })
        {
            return new LayoutDefinitionPolicyStatus
            {
                LayoutKey = builtIn.Key,
                Tier = builtIn.Tier == WeddingLayoutTier.Premium
                    ? LayoutTier.Premium
                    : LayoutTier.Free,
                Revision = 0,
                IsBuiltIn = true,
            };
        }

        if (!_registry.DefinitionPolicies.TryGetValue(layoutKey, out var policy))
        {
            return null;
        }

        return new LayoutDefinitionPolicyStatus
        {
            LayoutKey = policy.LayoutKey,
            Tier = policy.Tier,
            Revision = policy.Revision,
            IsBuiltIn = false,
        };
    }

    private static bool TryReadLayoutKey(PathString path, out string layoutKey)
    {
        layoutKey = "";
        var value = path.Value;
        if (string.IsNullOrEmpty(value)
            || !value.StartsWith(RoutePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var encodedKey = value[RoutePrefix.Length..];
        if (encodedKey.Length == 0 || encodedKey.Contains('/'))
        {
            return false;
        }

        try
        {
            layoutKey = Uri.UnescapeDataString(encodedKey);
            return true;
        }
        catch (UriFormatException)
        {
            return false;
        }
    }
}
