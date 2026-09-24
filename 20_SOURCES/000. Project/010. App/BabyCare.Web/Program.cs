using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using System.Net;
using BabyCare.Application;
using BabyCare.Components;
using BabyCare.Infrastructure;
using Dreamine.Identity;
using Dreamine.Identity.Options;
using Dreamine.UI.Blazor;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseStaticWebAssets();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
var data = Path.GetFullPath(builder.Configuration["BabyCare:DataPath"] ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data"));
Directory.CreateDirectory(data);
var auth = builder.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new();
if (auth.UseCentralPortal)
{
    if (string.IsNullOrWhiteSpace(auth.DataProtectionKeysPath))
        throw new InvalidOperationException("통합로그인은 기존 CodeMaru의 Authentication:DataProtectionKeysPath 설정이 필요합니다.");
    auth = auth.AsConsumer();
}
if (string.IsNullOrWhiteSpace(auth.DataProtectionKeysPath)) auth.DataProtectionKeysPath = Path.Combine(data, "IdentityKeys");
builder.Services.AddDreamineIdentityWeb(auth, Path.GetFullPath(builder.Configuration["Authentication:UsersDbPath"] ?? Path.Combine(data, "users.db")));
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
    options.KnownProxies.Add(IPAddress.Loopback);
    options.KnownProxies.Add(IPAddress.IPv6Loopback);
});
if (auth.UseCentralPortal)
    builder.Services.Configure<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme,
        options => options.Cookie.SecurePolicy = CookieSecurePolicy.Always);
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddScoped<DreamineDialogService>();
builder.Services.AddScoped<BabyCareText>();
builder.Services.AddSingleton(new CareCatalog(Path.Combine(builder.Environment.ContentRootPath, "Content", "care-kinds.json")));
builder.Services.AddSingleton(sp => new CareStore(Path.Combine(data, "babycare.db"), sp.GetRequiredService<CareCatalog>()));
builder.Services.AddSingleton(sp => new CarePhotos(Path.Combine(data, "photos"), sp.GetRequiredService<CareStore>()));
builder.Services.AddSingleton<ICareInterpreter, CodexCareInterpreter>();
var app = builder.Build();
app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
    app.UseHttpsRedirection();
}
// Only loopback proxies can supply the original HTTPS scheme.
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
if (!auth.UseCentralPortal) app.MapDreamineIdentityEndpoints();
else
{
    // Old bookmarks must also enter the central portal, never a local OAuth handler.
    app.MapGet("/_identity/login", () => Results.Redirect(CentralLoginUrl()));
    app.MapGet("/signin/{provider}", () => Results.Redirect(CentralLoginUrl()));
}
app.MapGet("/auth/login", (HttpContext context) => context.User.Identity?.IsAuthenticated == true
    ? Results.LocalRedirect("/") : auth.UseCentralPortal
        ? Results.Redirect(CentralLoginUrl())
        : Results.Challenge(new AuthenticationProperties { RedirectUri = "/" }));
app.MapGet("/healthz", () => Results.Text("OK"));
app.MapCarePhotos();
app.MapGet("/error", () => Results.Problem("요청을 처리하지 못했습니다. 다시 시도해주세요."));
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();

string CentralLoginUrl() => DreamineIdentityPortal.CreateUrl("login",
    builder.Configuration["BabyCare:PublicUrl"] ?? "https://babycare.codemaru.co.kr/", "ko");
