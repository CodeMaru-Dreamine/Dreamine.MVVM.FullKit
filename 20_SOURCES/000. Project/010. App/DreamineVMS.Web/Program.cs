using Dreamine.Identity;
using Dreamine.Identity.Options;
using DreamineVMS.Web.Blazor;
using DreamineVMS.Web.Services.Agent;
using DreamineVMS.Web.Services.Auth;
using DreamineVMS.Web.Services.Cameras;
using DreamineVMS.Web.Services.Hls;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddUserSecrets("codemaru-oauth-2ba4e1b2");

AuthOptions authOptions =
    builder.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();
string usersDbPath = ResolvePath(
    builder.Configuration[$"{AuthOptions.SectionName}:UsersDbPath"],
    Path.Combine(AppContext.BaseDirectory, "App_Data", "codemaru.db"));

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.Configure<CircuitOptions>(o => o.DetailedErrors = true);
builder.Services.AddHttpClient();
builder.Services.AddDreamineIdentityWeb(authOptions, usersDbPath);

builder.Services.AddSingleton<VmsDatabase>();
builder.Services.AddSingleton<VmsUserService>();
builder.Services.AddSingleton<VmsSessionService>();
builder.Services.AddScoped<VmsAuthState>();
builder.Services.AddSingleton<IVmsCameraRepository, SqliteCameraRepository>();
builder.Services.AddSingleton<AgentTokenService>();
builder.Services.AddSingleton<HlsSegmentStore>();

var app = builder.Build();

var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
provider.Mappings[".exe"] = "application/octet-stream";
app.UseForwardedHeaders();
app.UseStaticFiles(new StaticFileOptions { ContentTypeProvider = provider });
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// 크롤러 봇에게 OG 태그가 포함된 정적 HTML을 반환합니다.
// Blazor Server는 JS 없이 렌더링이 안 되므로 봇은 빈 페이지만 봅니다.
app.Use(async (ctx, next) =>
{
    var ua = ctx.Request.Headers.UserAgent.ToString();

    // AppleWebKit + Chrome/Safari = 실제 브라우저 (카카오톡 인앱, 삼성 인터넷 등 포함)
    // 이 경우 크롤러로 처리하지 않음
    bool looksLikeBrowser = ua.Contains("AppleWebKit") &&
        (ua.Contains("Chrome") || ua.Contains("Safari") || ua.Contains("Mobile"));

    bool isCrawler = !looksLikeBrowser && (
        ua.Contains("Kakaotalk", StringComparison.OrdinalIgnoreCase) ||
        ua.Contains("kakaostory", StringComparison.OrdinalIgnoreCase) ||
        ua.Contains("Naverbot", StringComparison.OrdinalIgnoreCase) ||
        ua.Contains("DaumApps", StringComparison.OrdinalIgnoreCase)
    ) || (
        // 이 UA들은 AppleWebKit 포함해도 항상 봇
        ua.Contains("facebookexternalhit", StringComparison.OrdinalIgnoreCase) ||
        ua.Contains("Twitterbot", StringComparison.OrdinalIgnoreCase) ||
        ua.Contains("LinkedInBot", StringComparison.OrdinalIgnoreCase) ||
        ua.Contains("Slackbot", StringComparison.OrdinalIgnoreCase) ||
        ua.Contains("Discordbot", StringComparison.OrdinalIgnoreCase)
    );

    if (!isCrawler) { await next(); return; }

    var path = ctx.Request.Path.Value ?? "";

    // / 홈페이지 OG
    if (path == "/" || path == "")
    {
        var siteUrl = $"https://{ctx.Request.Host}";
        const string title = "CodeMaru CCTV Viewer";
        const string desc = "카메라 주인은 앱 하나 설치, 보는 사람은 링크만으로 실시간 시청";
        var image = $"{siteUrl}/images/cctvviewer-og.png";
        ctx.Response.ContentType = "text/html; charset=utf-8";
        await ctx.Response.WriteAsync($"""
            <!DOCTYPE html>
            <html lang="ko">
            <head>
              <meta charset="utf-8"/>
              <title>{title}</title>
              <meta property="og:type" content="website"/>
              <meta property="og:url" content="{siteUrl}/"/>
              <meta property="og:title" content="{title}"/>
              <meta property="og:description" content="{desc}"/>
              <meta property="og:image" content="{image}"/>
              <meta property="og:site_name" content="CodeMaru CCTV Viewer"/>
              <meta name="description" content="{desc}"/>
            </head>
            <body><p><a href="{siteUrl}/">{title}</a></p></body>
            </html>
            """);
        return;
    }

    // /{slug}/live 패턴 감지
    var parts = path.Trim('/').Split('/');
    if (parts.Length == 2 && parts[1] == "live")
    {
        var slug = parts[0];
        var userSvc = ctx.RequestServices.GetRequiredService<VmsUserService>();
        var user = await userSvc.FindBySlugAsync(slug);
        if (user is not null)
        {
            var siteUrl = $"https://{ctx.Request.Host}";
            var pageUrl = $"{siteUrl}/{slug}/live";
            var title = string.IsNullOrWhiteSpace(user.OgTitle)
                ? $"{user.DisplayName} CCTV Live"
                : user.OgTitle;
            var desc = string.IsNullOrWhiteSpace(user.OgDescription)
                ? $"{user.DisplayName}의 실시간 카메라 스트림입니다."
                : user.OgDescription;
            var image = string.IsNullOrWhiteSpace(user.OgImage)
                ? $"{siteUrl}/img/og-default.png"
                : user.OgImage;

            ctx.Response.ContentType = "text/html; charset=utf-8";
            await ctx.Response.WriteAsync($"""
                <!DOCTYPE html>
                <html lang="ko">
                <head>
                  <meta charset="utf-8"/>
                  <title>{title}</title>
                  <meta property="og:type" content="website"/>
                  <meta property="og:url" content="{pageUrl}"/>
                  <meta property="og:title" content="{title}"/>
                  <meta property="og:description" content="{desc}"/>
                  <meta property="og:image" content="{image}"/>
                  <meta property="og:site_name" content="CodeMaru CCTV Viewer"/>
                  <meta name="description" content="{desc}"/>
                </head>
                <body><p><a href="{pageUrl}">{title}</a></p></body>
                </html>
                """);
            return;
        }
    }

    await next();
});

// ── 에이전트 API ──────────────────────────────────────────────────────────

// 로그인 → 토큰 + 카메라 목록
app.MapPost("/api/agent/login", async (AgentLoginRequest req, HttpContext ctx,
    VmsUserService users, AgentTokenService tokens, IVmsCameraRepository repo) =>
{
    var (ok, _, user) = await users.LoginAsync(req.Email, req.Password);
    if (!ok || user is null) return Results.Unauthorized();

    var token = tokens.Issue(user);
    var cameras = repo.GetAll()
        .Where(c => c.TenantId == user.Id && c.Enabled && !c.IsDirectHls)
        .Select(c => new AgentCameraDto(c.Id, c.Name, c.Host, c.RtspUrl, c.AutoReconnect, c.IsPublic))
        .ToList();

    // 로그인(재연결) 시에만 낡은 HLS 파일 초기화
    var hlsStore = ctx.RequestServices.GetRequiredService<HlsSegmentStore>();
    foreach (var cam in cameras)
        hlsStore.ClearCameraHls(user.Id, cam.Id);

    return Results.Ok(new AgentLoginResponse(token, user.Id, cameras));
});

// 에이전트 → 서버: 로컬 카메라 목록 동기화 (에이전트가 보유한 카메라를 서버 DB에 등록)
app.MapPost("/api/agent/sync-cameras",
    async (AgentSyncRequest req, AgentTokenService tokens, IVmsCameraRepository repo, HlsSegmentStore store) =>
{
    var user = tokens.Validate(req.Token);
    if (user is null) return Results.Unauthorized();

    var existing = repo.GetAll().Where(c => c.TenantId == user.Id).ToDictionary(c => c.Id);
    int order = existing.Count > 0 ? existing.Values.Max(c => c.DisplayOrder) + 1 : 1;

    foreach (var cam in req.Cameras)
    {
        var device = new DreamineVMS.Web.Models.CameraDevice
        {
            Id = cam.Id,
            TenantId = user.Id,
            Name = cam.Name,
            Host = cam.Host,
            RtspUrl = cam.RtspUrl,
            DisplayOrder = existing.ContainsKey(cam.Id)
                ? existing[cam.Id].DisplayOrder
                : order++,
            Enabled = true,
            AutoReconnect = cam.AutoReconnect,
            IsPublic = cam.IsPublic
        };

        if (existing.ContainsKey(cam.Id))
            await repo.UpdateAsync(device);
        else
            await repo.AddAsync(device);
    }

    return Results.Ok();
});

// 에이전트 → 서버: HLS 세그먼트 업로드
app.MapPost("/api/agent/hls/{token}/{cameraId}/{filename}",
    async (string token, string cameraId, string filename,
        HttpContext ctx, AgentTokenService tokens, HlsSegmentStore store,
        ILoggerFactory loggerFactory) =>
{
    var log = loggerFactory.CreateLogger("AgentPush");
    var user = tokens.Validate(token);
    if (user is null)
    {
        log.LogWarning("[Push] 토큰 인증 실패: {cam}/{file}", cameraId, filename);
        return Results.Unauthorized();
    }
    await store.SaveSegmentAsync(user.Id, cameraId, filename, ctx.Request.Body);
    log.LogDebug("[Push] OK {cam}/{file} ({bytes}B)", cameraId, filename, ctx.Request.ContentLength ?? -1);
    return Results.Ok();
});

// 브라우저 → 서버: HLS 재생
app.MapGet("/hls/{tenantId}/{cameraId}/{filename}",
    (string tenantId, string cameraId, string filename, HlsSegmentStore store,
     HttpContext ctx,
     ILoggerFactory loggerFactory) =>
{
    var log = loggerFactory.CreateLogger("HlsServe");
    var (stream, ct) = store.GetFile(tenantId, cameraId, filename);
    if (stream is null)
    {
        log.LogWarning("[Serve] 404 {cam}/{file}", cameraId, filename);
        return Results.NotFound();
    }

    if (filename.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase))
    {
        ctx.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate, max-age=0";
        ctx.Response.Headers.Pragma = "no-cache";
        ctx.Response.Headers.Expires = "0";
    }
    else
    {
        ctx.Response.Headers.CacheControl = "public, max-age=60, immutable";
    }

    return Results.Stream(stream, ct);
});

// ── OG 이미지 업로드 ──────────────────────────────────────────────────────
app.MapPost("/api/upload/og-image",
    async (HttpContext ctx, AgentTokenService tokens, IWebHostEnvironment env) =>
{
    // 세션 토큰으로 인증 (쿠키에서 읽기)
    var sessionToken = ctx.Request.Headers["X-Session-Token"].ToString();
    if (string.IsNullOrWhiteSpace(sessionToken)) return Results.Unauthorized();

    var sessSvc = ctx.RequestServices.GetRequiredService<VmsSessionService>();
    var user = sessSvc.ValidateToken(sessionToken);
    if (user is null) return Results.Unauthorized();

    if (!ctx.Request.HasFormContentType) return Results.BadRequest("multipart form required");
    var form = await ctx.Request.ReadFormAsync();
    var file = form.Files.GetFile("file");
    if (file is null || file.Length == 0) return Results.BadRequest("no file");

    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
    if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp" or ".gif"))
        return Results.BadRequest("허용되지 않는 파일 형식입니다.");

    if (file.Length > 5 * 1024 * 1024) return Results.BadRequest("파일이 너무 큽니다 (최대 5MB).");

    var saveDir = Path.Combine(env.WebRootPath, "images", "og");
    Directory.CreateDirectory(saveDir);

    var fileName = $"{user.PublicSlug}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}{ext}";
    var savePath = Path.Combine(saveDir, fileName);
    await using var fs = File.Create(savePath);
    await file.CopyToAsync(fs);

    var url = $"https://{ctx.Request.Host}/images/og/{fileName}";
    return Results.Ok(new { url });
});

// ── Blazor ────────────────────────────────────────────────────────────────
app.MapRazorComponents<AppShell>()
   .AddInteractiveServerRenderMode();

app.Run();

#pragma warning disable CS1587
/// \cond LOCAL_FUNCTION_DOCUMENTATION
/// <summary>
/// \if KO
/// <para>구성된 경로를 절대 경로로 확인하고, 값이 없으면 기본 경로를 반환합니다.</para>
/// \endif
/// \if EN
/// <para>Resolves the configured path to an absolute path, or returns the fallback when absent.</para>
/// \endif
/// </summary>
/// <param name="configuredPath">
/// \if KO
/// <para>구성에서 읽은 선택적 경로입니다.</para>
/// \endif
/// \if EN
/// <para>The optional path read from configuration.</para>
/// \endif
/// </param>
/// <param name="fallback">
/// \if KO
/// <para>구성 경로가 없을 때 사용할 기본 경로입니다.</para>
/// \endif
/// \if EN
/// <para>The fallback path used when no configured path is provided.</para>
/// \endif
/// </param>
/// <returns>
/// \if KO
/// <para>해석된 절대 경로입니다.</para>
/// \endif
/// \if EN
/// <para>The resolved absolute path.</para>
/// \endif
/// </returns>
/// \endcond
static string ResolvePath(string? configuredPath, string fallback)
{
    if (string.IsNullOrWhiteSpace(configuredPath))
    {
        return fallback;
    }

    return Path.IsPathRooted(configuredPath)
        ? configuredPath
        : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredPath));
}
#pragma warning restore CS1587

/// <summary>
/// \if KO
/// <para>Agent Login Request 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates agent login request functionality and related state.</para>
/// \endif
/// </summary>
record AgentLoginRequest(string Email, string Password);
/// <summary>
/// \if KO
/// <para>Agent Camera Dto 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates agent camera dto functionality and related state.</para>
/// \endif
/// </summary>
record AgentCameraDto(string Id, string Name, string Host, string RtspUrl, bool AutoReconnect, bool IsPublic);
/// <summary>
/// \if KO
/// <para>Agent Login Response 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates agent login response functionality and related state.</para>
/// \endif
/// </summary>
record AgentLoginResponse(string Token, string TenantId, List<AgentCameraDto> Cameras);
/// <summary>
/// \if KO
/// <para>Agent Sync Request 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates agent sync request functionality and related state.</para>
/// \endif
/// </summary>
record AgentSyncRequest(string Token, List<AgentCameraDto> Cameras);
