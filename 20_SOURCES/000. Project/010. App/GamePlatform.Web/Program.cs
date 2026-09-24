using System.Net;
using System.Security.Claims;
using Dreamine.Identity;
using Dreamine.Identity.Options;
using GamePlatform.Application;
using GamePlatform.Components;
using GamePlatform.Domain;
using GamePlatform.Infrastructure;
using GamePlatform.Options;
using GamePlatform.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Production 프로필로 직접 실행할 때도 참조한 Dreamine Razor Class Library의
// 정적 자산(JS/CSS)을 _content 경로에서 제공하도록 합니다.
builder.WebHost.UseStaticWebAssets();

// Windows 서비스 계정이 Event Log 소스에 쓰기 권한이 없어도 웹 요청이 실패하지 않도록
// 서비스 로그는 콘솔/디버그 공급자에 한정합니다.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Configuration.AddUserSecrets("codemaru-oauth-2ba4e1b2");
builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddCommandLine(args);

var gameOptions = GameOptions.From(builder.Configuration);
builder.Services.AddSingleton(gameOptions);
builder.Services.AddHttpContextAccessor();

var authOptions = builder.Configuration
    .GetSection(AuthOptions.SectionName)
    .Get<AuthOptions>() ?? new AuthOptions();

// localhost 응답에는 .codemaru.co.kr 도메인 쿠키를 저장할 수 없습니다.
// 개발 프로필은 host-only 쿠키를 사용해 별도의 OAuth 없이 로컬 테스트 계정으로
// 게임 화면과 서버 저장 기능을 끝까지 검증할 수 있게 합니다.
if (builder.Environment.IsDevelopment())
{
    authOptions.CookieDomain = string.Empty;
}

var usersDbPath = ResolvePath(
    builder.Configuration[$"{AuthOptions.SectionName}:UsersDbPath"],
    Path.Combine(AppContext.BaseDirectory, "App_Data", "codemaru.db"));

// OAuth 흐름은 codemaru.co.kr 중앙 포털이 담당하고 게임 서비스는 공유 쿠키만 읽습니다.
builder.Services.AddDreamineIdentityWeb(authOptions.AsConsumer(), usersDbPath);

var adminEmails = builder.Configuration
    .GetSection("Administration:AllowedEmails")
    .Get<string[]>() ?? [];
builder.Services.AddAuthorization(authorization =>
    authorization.AddPolicy(
        "GameAdmin",
        policy => policy.RequireAssertion(context =>
        {
            var email = context.User.FindFirst(ClaimTypes.Email)?.Value;
            return builder.Environment.IsDevelopment()
                   && string.Equals(email, "local-game-admin@codemaru.test", StringComparison.OrdinalIgnoreCase)
                   || !string.IsNullOrWhiteSpace(email)
                   && adminEmails.Contains(email, StringComparer.OrdinalIgnoreCase);
        })));

// 게임 DB는 Identity DB와 물리적으로 분리하고 Dreamine Database Provider로 접근합니다.
builder.Services.AddSingleton<IGameProgressStore, GameProgressStore>();
builder.Services.AddSingleton<IGameSettingsStore, GameSettingsStore>();
builder.Services.AddSingleton<IGameChatStore, GameChatStore>();
builder.Services.AddSingleton<IGameChatService, GameChatService>();
builder.Services.AddSingleton<IGameAdminGrantAuditStore, GameAdminGrantAuditStore>();
builder.Services.AddSingleton<IGameAdminGrantService, GameAdminGrantService>();
builder.Services.AddSingleton<IGameAdminMessageStore, GameAdminMessageStore>();
builder.Services.AddSingleton<IGameAdminCommunicationService, GameAdminCommunicationService>();
builder.Services.AddSingleton<IGameRankingService, GameRankingService>();
builder.Services.AddSingleton<IAttackRollSource, RandomAttackRollSource>();
builder.Services.AddSingleton<IGameAttackRateLimiter, GameAttackRateLimiter>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<GamesLocalization>();
builder.Services.AddSingleton(_ => RegionCatalog.Load(
    Path.Combine(builder.Environment.ContentRootPath, "Content", "regions.json")));
builder.Services.AddSingleton(_ => AudioTrackCatalog.Load(
    Path.Combine(builder.Environment.ContentRootPath, "Content", "audio-tracks.json")));
builder.Services.AddSingleton(_ => AudioEffectCatalog.Load(
    Path.Combine(builder.Environment.ContentRootPath, "Content", "audio-effects.json")));
builder.Services.AddSingleton<IRegionAudioResolver, RegionAudioResolver>();
builder.Services.AddSingleton<IAudioAssetCatalog, AudioAssetCatalog>();
builder.Services.AddSingleton<ICombatAudioDirector, CombatAudioDirector>();
builder.Services.AddScoped<GameSession>();
builder.Services.AddScoped<IGameSettingsService, GameSettingsService>();
builder.Services.AddScoped<IAudioService, BrowserAudioService>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddResponseCompression(options => options.EnableForHttps = true);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseForwardedHeaders();
app.UseResponseCompression();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

if (app.Environment.IsDevelopment())
{
    app.MapGet("/_development/login", async (HttpContext context) =>
    {
        if (!IsLocalRequest(context))
        {
            return Results.NotFound();
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "local-development-player"),
            new Claim(DreamineIdentityExtensions.UserIdClaimType, "local-development-player"),
            new Claim(ClaimTypes.Name, "로컬 테스트 플레이어"),
            new Claim(ClaimTypes.Email, "local-game-admin@codemaru.test"),
            new Claim(DreamineIdentityExtensions.ProviderClaimType, "Development")
        };
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));

        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            });

        return Results.LocalRedirect("/maru-idle/play");
    });

    app.MapGet("/_development/logout", async (HttpContext context) =>
    {
        if (!IsLocalRequest(context))
        {
            return Results.NotFound();
        }

        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.LocalRedirect("/maru-idle");
    });

}

app.MapGet("/healthz", (IGameProgressStore _) => Results.Text("OK", "text/plain"));
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync();

static string ResolvePath(string? configuredPath, string fallback)
{
    var path = string.IsNullOrWhiteSpace(configuredPath) ? fallback : configuredPath;
    return Path.IsPathRooted(path)
        ? path
        : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
}

static bool IsLocalRequest(HttpContext context)
{
    var host = context.Request.Host.Host;
    var hasLocalHost = string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(host, "::1", StringComparison.OrdinalIgnoreCase);
    var remoteAddress = context.Connection.RemoteIpAddress;
    return hasLocalHost && remoteAddress is not null && IPAddress.IsLoopback(remoteAddress);
}
