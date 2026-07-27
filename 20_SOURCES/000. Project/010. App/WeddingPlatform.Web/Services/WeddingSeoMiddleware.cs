using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace WeddingPlatform.Services;

/// <summary>
/// 동적 SEO 응답 미들웨어를 기본 정적 파일 미들웨어보다 앞에 배치합니다.
/// </summary>
public sealed class WeddingSeoStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        ArgumentNullException.ThrowIfNull(next);

        return app =>
        {
            app.UseMiddleware<WeddingSeoMiddleware>();
            next(app);
        };
    }
}

/// <summary>
/// robots.txt와 sitemap.xml은 동적으로 생성하고, 생성 실패 시 wwwroot 파일로 넘깁니다.
/// </summary>
public sealed class WeddingSeoMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ITenantStore _tenantStore;
    private readonly ILogger<WeddingSeoMiddleware> _logger;

    public WeddingSeoMiddleware(
        RequestDelegate next,
        ITenantStore tenantStore,
        ILogger<WeddingSeoMiddleware> logger)
    {
        _next = next;
        _tenantStore = tenantStore;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!HttpMethods.IsGet(context.Request.Method)
            && !HttpMethods.IsHead(context.Request.Method))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var path = context.Request.Path.Value;
        if (!string.Equals(path, "/robots.txt", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(path, "/sitemap.xml", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        try
        {
            var isRobots = string.Equals(
                path,
                "/robots.txt",
                StringComparison.OrdinalIgnoreCase);
            var content = isRobots
                ? WeddingSeoService.BuildRobotsText()
                : await WeddingSeoService
                    .BuildSitemapXmlAsync(_tenantStore, context.RequestAborted)
                    .ConfigureAwait(false);

            await WriteUtf8ResponseAsync(
                    context,
                    content,
                    isRobots
                        ? "text/plain; charset=utf-8"
                        : "application/xml; charset=utf-8")
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "동적 SEO 응답 생성에 실패하여 wwwroot fallback 파일을 사용합니다. Path={Path}",
                context.Request.Path);

            if (context.Response.HasStarted)
            {
                throw;
            }

            await _next(context).ConfigureAwait(false);
        }
    }

    private static async Task WriteUtf8ResponseAsync(
        HttpContext context,
        string content,
        string contentType)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = contentType;
        context.Response.ContentLength = bytes.Length;
        context.Response.Headers.CacheControl = "public, max-age=300";

        if (!HttpMethods.IsHead(context.Request.Method))
        {
            await context.Response.Body
                .WriteAsync(bytes, context.RequestAborted)
                .ConfigureAwait(false);
        }
    }
}
