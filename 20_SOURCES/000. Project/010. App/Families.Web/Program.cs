using System.IO;
using Dreamine.Hybrid.Wpf.DependencyInjection;
using Dreamine.Hybrid.Wpf.Hosting;
using Dreamine.Identity;
using Dreamine.Identity.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FamiliesApp.Blazor;
using FamiliesApp.Services;

namespace FamiliesApp;

/// <summary>
/// \if KO
/// <para>Program 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates program functionality and related state.</para>
/// \endif
/// </summary>
public static class Program
{
    /// <summary>
    /// \if KO
    /// <para>Main 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the main operation.</para>
    /// \endif
    /// </summary>
    [STAThread]
    public static void Main()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddUserSecrets("codemaru-oauth-2ba4e1b2");

        int serverPort = GetInt(builder.Configuration, "FamilyServer:Port", 5080);
        bool listenAnyIp = GetBool(builder.Configuration, "FamilyServer:ListenAnyIp", true);
        AuthOptions authOptions =
            builder.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();
        string usersDbPath = ResolvePath(
            builder.Configuration[$"{AuthOptions.SectionName}:UsersDbPath"],
            Path.Combine(AppContext.BaseDirectory, "App_Data", "codemaru.db"));

        builder.Services.AddDreamineHybridWpf();
        builder.Services.AddDreamineIdentityWpfHost();

        var familyOpts = FamilyOptions.From(builder.Configuration);
        builder.Services.AddSingleton(familyOpts);
        builder.Services.AddSingleton<IFamilyTenantStore, JsonFamilyTenantStore>();
        builder.Services.AddSingleton<IPostStore, JsonPostStore>();
        builder.Services.AddSingleton<IAlbumStore, JsonAlbumStore>();
        builder.Services.AddSingleton<IGlobalSettingsStore, JsonGlobalSettingsStore>();
        builder.Services.AddSingleton<IMediaService, LocalMediaService>();
        builder.Services.AddSingleton<IReactionStore, JsonReactionStore>();

        builder.Services.AddSingleton<Views.MainWindow>();
        builder.Services.AddHostedService<GhostAccountCleanupService>();

        builder.Services.AddDreamineBlazorServer<AppShell>(options =>
        {
            options.Port = serverPort;
            options.ListenAnyIp = listenAnyIp;
            options.SharedServiceTypes.Add(typeof(FamilyOptions));
            options.SharedServiceTypes.Add(typeof(IFamilyTenantStore));
            options.SharedServiceTypes.Add(typeof(IPostStore));
            options.SharedServiceTypes.Add(typeof(IAlbumStore));
            options.SharedServiceTypes.Add(typeof(IGlobalSettingsStore));
            options.SharedServiceTypes.Add(typeof(IMediaService));
            options.SharedServiceTypes.Add(typeof(IReactionStore));
            // Media bytes use the dedicated multipart endpoint. Keep the interactive circuit
            // bounded so a file can never be pushed through SignalR by accident.
            options.ConfigureServices = services =>
            {
                services.AddServerSideBlazor().AddHubOptions(o =>
                {
                    o.MaximumReceiveMessageSize = 64 * 1024;
                    o.ClientTimeoutInterval = TimeSpan.FromMinutes(10);
                    o.HandshakeTimeout = TimeSpan.FromMinutes(2);
                    o.KeepAliveInterval = TimeSpan.FromSeconds(10);
                });

                services.AddAntiforgery(o =>
                {
                    o.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
                    o.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
                });

                services.Configure<CircuitOptions>(o =>
                {
                    o.DisconnectedCircuitMaxRetained = 100;
                    o.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(3);
                    // The browser performs the multipart transfer outside SignalR, but the
                    // interop call still waits for the 30-minute XHR batch result. Keep this
                    // slightly longer so a slow mobile upload is not reported as a false
                    // timeout while the browser is still sending the file.
                    o.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(35);
                    o.MaxBufferedUnacknowledgedRenderBatches = 10;
                });

                services.AddScoped<FamilyUserContext>();
                services.AddScoped<FamilyLocalization>();
                services.AddDataProtection().SetApplicationName("Families.Web.Access");
                services.AddSingleton<FamilyAccessService>();
                services.AddSingleton<FamilyDirectUploadService>();
                services.Configure<FormOptions>(form =>
                {
                    form.MultipartBodyLengthLimit = FamilyDirectUploadEndpoints.MaximumRequestBytes;
                    form.ValueLengthLimit = 1024 * 1024;
                    form.MultipartHeadersLengthLimit = 64 * 1024;
                });
            };

            var previousPipeline = options.ConfigurePipeline;
            options.ConfigurePipeline = app =>
            {
                previousPipeline?.Invoke(app);

                // Expose only media extensions from the tenant data root. Using the default
                // content-type map here would also publish config/post JSON files (including
                // password hashes and administrator metadata) below /family-data.
                Directory.CreateDirectory(familyOpts.ResolvedDataPath);
                var contentTypes = new FileExtensionContentTypeProvider();
                contentTypes.Mappings.Clear();
                contentTypes.Mappings[".jpg"] = "image/jpeg";
                contentTypes.Mappings[".jpeg"] = "image/jpeg";
                contentTypes.Mappings[".png"] = "image/png";
                contentTypes.Mappings[".webp"] = "image/webp";
                contentTypes.Mappings[".gif"] = "image/gif";
                contentTypes.Mappings[".heic"] = "image/heic";
                contentTypes.Mappings[".heif"] = "image/heif";
                contentTypes.Mappings[".avif"] = "image/avif";
                contentTypes.Mappings[".mp4"] = "video/mp4";
                contentTypes.Mappings[".webm"] = "video/webm";
                contentTypes.Mappings[".mov"] = "video/quicktime";
                contentTypes.Mappings[".m4v"] = "video/x-m4v";
                contentTypes.Mappings[".3gp"] = "video/3gpp";
                contentTypes.Mappings[".3g2"] = "video/3gpp2";

                // Tenant media is private. Authenticate the CodeMaru cookie explicitly here
                // because this static-file branch runs before the shared identity middleware,
                // then also honor the tenant-bound signed guest cookie.
                app.Use(async (context, next) =>
                {
                    if (!context.Request.Path.StartsWithSegments("/family-data", out var remaining))
                    {
                        await next(context).ConfigureAwait(false);
                        return;
                    }

                    var relative = remaining.Value?.TrimStart('/') ?? string.Empty;
                    var separator = relative.IndexOf('/');
                    var slug = separator >= 0 ? relative[..separator] : relative;
                    var assetPath = separator >= 0 ? relative[(separator + 1)..] : string.Empty;
                    var rawTarget = context.Features.Get<IHttpRequestFeature>()?.RawTarget ?? string.Empty;
                    if (!FamilyAccessService.IsValidSlug(slug)
                        || HasEncodedPathControl(rawTarget)
                        || !TryResolveTenantMediaPath(
                            familyOpts.ResolvedDataPath,
                            slug,
                            assetPath,
                            out var fullPath)
                        || !File.Exists(fullPath)
                        || !contentTypes.TryGetContentType(fullPath, out var contentType))
                    {
                        context.Response.StatusCode = StatusCodes.Status404NotFound;
                        return;
                    }

                    var tenantStore = context.RequestServices.GetRequiredService<IFamilyTenantStore>();
                    var config = await tenantStore.GetAsync(slug, context.RequestAborted).ConfigureAwait(false);
                    if (config is null)
                    {
                        context.Response.StatusCode = StatusCodes.Status404NotFound;
                        return;
                    }

                    var authentication = await context.AuthenticateAsync().ConfigureAwait(false);
                    if (authentication.Principal is not null)
                    {
                        context.User = authentication.Principal;
                    }

                    var access = context.RequestServices.GetRequiredService<FamilyAccessService>();
                    access.AddGuestClaims(context.User, context.Request.Cookies);
                    if (!access.HasAccess(config, context.User))
                    {
                        context.Response.StatusCode = StatusCodes.Status404NotFound;
                        return;
                    }

                    // Do not let a browser or an intermediary retain protected bytes after
                    // a viewer password or administrator membership changes.
                    context.Response.Headers.CacheControl = "private, no-store, max-age=0";
                    context.Response.Headers.Pragma = "no-cache";
                    context.Response.Headers.Expires = "0";
                    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                    await Results.File(fullPath, contentType, enableRangeProcessing: true)
                        .ExecuteAsync(context)
                        .ConfigureAwait(false);
                });

                // Kestrel's default request limit is lower than the configured tenant video
                // limits. Per-tenant limits are still enforced while the file is stored.
                app.Use(async (context, next) =>
                {
                    if (context.Request.Path.StartsWithSegments("/api/families/uploads"))
                    {
                        var requestSize = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
                        if (requestSize is not null && !requestSize.IsReadOnly)
                        {
                            requestSize.MaxRequestBodySize = FamilyDirectUploadEndpoints.MaximumRequestBytes;
                        }
                    }

                    await next(context).ConfigureAwait(false);
                });
            };

            var previousAfterRouting = options.ConfigurePipelineAfterRouting;
            options.ConfigurePipelineAfterRouting = app =>
            {
                previousAfterRouting?.Invoke(app);
                app.Use(async (context, next) =>
                {
                    context.RequestServices.GetRequiredService<FamilyAccessService>()
                        .AddGuestClaims(context.User, context.Request.Cookies);
                    await next(context).ConfigureAwait(false);
                });
                app.MapFamilyAccessUnlock();
                app.MapFamilyDirectUpload();
                app.MapFamilyOgImages();
            };

            options.AddDreamineIdentity(authOptions, usersDbPath);
        });

        // OG 플랫폼 이미지 자동 생성 (없을 때만)
        var wwwroot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        try { OgImageGenerator.EnsureGenerated(wwwroot); } catch { /* 실패해도 앱 계속 실행 */ }

        var host = builder.Build();
        if (GetBool(builder.Configuration, "FamilyServer:Headless", false))
        {
            // Server deployments and automated browser checks do not need a WebView2 window.
            // Desktop behavior remains unchanged unless the setting is explicitly enabled.
            RunHeadless(host);
        }
        else
        {
            host.RunDreamineWpfApp<App>();
        }
    }

    /// <summary>
    /// Runs the embedded web host without creating the desktop WebView window.
    /// </summary>
    /// <param name="host">The configured application host.</param>
    private static void RunHeadless(IHost host)
    {
        try
        {
            host.RunAsync().GetAwaiter().GetResult();
        }
        finally
        {
            host.Dispose();
        }
    }

    private static bool HasEncodedPathControl(string rawTarget)
    {
        var queryIndex = rawTarget.IndexOf('?');
        var rawPath = queryIndex >= 0 ? rawTarget[..queryIndex] : rawTarget;
        return rawPath.Contains("%2e", StringComparison.OrdinalIgnoreCase)
               || rawPath.Contains("%2f", StringComparison.OrdinalIgnoreCase)
               || rawPath.Contains("%5c", StringComparison.OrdinalIgnoreCase)
               || rawPath.Contains("%25", StringComparison.OrdinalIgnoreCase)
               || rawPath.Contains('\\')
               || rawPath.Contains("//", StringComparison.Ordinal);
    }

    private static bool TryResolveTenantMediaPath(
        string dataRoot,
        string slug,
        string assetPath,
        out string fullPath)
    {
        fullPath = string.Empty;
        if (string.IsNullOrWhiteSpace(assetPath)
            || assetPath.Contains('\\')
            || assetPath.Contains('\0'))
        {
            return false;
        }

        var segments = assetPath.Split('/', StringSplitOptions.None);
        if (segments.Any(segment =>
                string.IsNullOrWhiteSpace(segment)
                || segment is "." or ".."
                || segment.Contains(':')))
        {
            return false;
        }

        try
        {
            var tenantRoot = Path.GetFullPath(Path.Combine(dataRoot, slug));
            var candidate = Path.GetFullPath(Path.Combine(
                tenantRoot,
                Path.Combine(segments)));
            var tenantPrefix = tenantRoot.TrimEnd(
                                   Path.DirectorySeparatorChar,
                                   Path.AltDirectorySeparatorChar)
                               + Path.DirectorySeparatorChar;
            if (!candidate.StartsWith(tenantPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            fullPath = candidate;
            return true;
        }
        catch (Exception exception) when (
            exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Int 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the int value.</para>
    /// \endif
    /// </summary>
    /// <param name="cfg">
    /// \if KO
    /// <para>cfg에 사용할 <c>IConfiguration</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IConfiguration</c> value used for cfg.</para>
    /// \endif
    /// </param>
    /// <param name="key">
    /// \if KO
    /// <para>key에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for key.</para>
    /// \endif
    /// </param>
    /// <param name="fallback">
    /// \if KO
    /// <para>fallback에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for fallback.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get Int 작업에서 생성한 <c>int</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> result produced by the get int operation.</para>
    /// \endif
    /// </returns>
    private static int GetInt(IConfiguration cfg, string key, int fallback) =>
        int.TryParse(cfg[key], out int v) ? v : fallback;

    /// <summary>
    /// \if KO
    /// <para>Bool 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the bool value.</para>
    /// \endif
    /// </summary>
    /// <param name="cfg">
    /// \if KO
    /// <para>cfg에 사용할 <c>IConfiguration</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IConfiguration</c> value used for cfg.</para>
    /// \endif
    /// </param>
    /// <param name="key">
    /// \if KO
    /// <para>key에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for key.</para>
    /// \endif
    /// </param>
    /// <param name="fallback">
    /// \if KO
    /// <para>fallback에 사용할 <c>bool</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>bool</c> value used for fallback.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get Bool 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the get bool condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    private static bool GetBool(IConfiguration cfg, string key, bool fallback) =>
        bool.TryParse(cfg[key], out bool v) ? v : fallback;

    /// <summary>
    /// \if KO
    /// <para>Resolve Path 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the resolve path operation.</para>
    /// \endif
    /// </summary>
    /// <param name="configuredPath">
    /// \if KO
    /// <para>configured Path에 사용할 <c>string?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for configured path.</para>
    /// \endif
    /// </param>
    /// <param name="fallback">
    /// \if KO
    /// <para>fallback에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for fallback.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Resolve Path 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the resolve path operation.</para>
    /// \endif
    /// </returns>
    private static string ResolvePath(string? configuredPath, string fallback)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return fallback;
        }

        return Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredPath));
    }
}
