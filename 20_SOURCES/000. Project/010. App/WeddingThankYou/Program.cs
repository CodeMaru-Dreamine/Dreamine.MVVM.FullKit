using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Dreamine.Hybrid.Wpf.DependencyInjection;
using Dreamine.Hybrid.Wpf.Hosting;
using Dreamine.Identity;
using Dreamine.Identity.Options;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WeddingThankYou.Blazor;
using WeddingThankYou.Services;

namespace WeddingThankYou;

/// <summary>
/// \if KO
/// <para>\file Program.cs \brief WPF 데스크톱 호스트 안에 Blazor Server(ThankYou/Admin)를 띄우는 진입점. \details WeddingPlatform.Web과 동일한 Dreamine.Hybrid.Wpf 구조를 사용합니다.</para>
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
        try
        {
            Run();
        }
        catch (Exception ex)
        {
            WriteStartupFailureLog(ex);
            System.Windows.MessageBox.Show(
                $"Wedding ThankYou 실행 중 오류가 발생했습니다.\n\n{ex.Message}\n\n자세한 내용은 로그를 확인하세요.",
                "Wedding ThankYou",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Run 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the run operation.</para>
    /// \endif
    /// </summary>
    private static void Run()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddUserSecrets("codemaru-oauth-2ba4e1b2");

        int serverPort = GetInt(builder.Configuration, "WeddingServer:Port", 4088);
        bool listenAnyIp = GetBool(builder.Configuration, "WeddingServer:ListenAnyIp", true);
        bool useEmbeddedWebView = GetBool(builder.Configuration, "WeddingShell:UseEmbeddedWebView", true);
        if (!IsTcpPortAvailable(serverPort, listenAnyIp))
        {
            serverPort = GetFreeLoopbackPort();
            listenAnyIp = false;
        }

        AuthOptions authOptions =
            builder.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();
        string usersDbPath = ResolvePath(
            builder.Configuration[$"{AuthOptions.SectionName}:UsersDbPath"],
            Path.Combine(AppContext.BaseDirectory, "App_Data", "codemaru.db"));

        builder.Services.AddDreamineHybridWpf();
        builder.Services.AddDreamineIdentityWpfHost();

        // ── Wedding 서비스 ─────────────────────────────────────────
        var weddingOpts = WeddingOptions.From(builder.Configuration);
        builder.Services.AddSingleton(weddingOpts);
        builder.Services.AddSingleton<ITenantStore, JsonTenantStore>();
        builder.Services.AddSingleton<IGuestbookStorage, CsvGuestbookStorage>();
        builder.Services.AddSingleton<IGlobalSettingsStore, JsonGlobalSettingsStore>();
        builder.Services.AddSingleton<IMediaQuotaPolicyResolver, MediaQuotaPolicyResolver>();
        builder.Services.AddSingleton<IImageOptimizationService, SystemDrawingImageOptimizationService>();
        builder.Services.AddSingleton<IMediaMigrationService, JsonMediaMigrationService>();
        builder.Services.AddSingleton<IMediaUsageQueryService, FileSystemMediaUsageQueryService>();
        builder.Services.AddSingleton<ISuperAdminSessionTokenService, SuperAdminSessionTokenService>();
        builder.Services.AddSingleton<IPhotoService, LocalPhotoService>();

        builder.Services.AddSingleton<Views.MainWindow>();

        builder.Services.AddDreamineBlazorServer<AppShell>(options =>
        {
            options.Port = serverPort;
            options.ListenAnyIp = listenAnyIp;
            options.UseEmbeddedWebView = useEmbeddedWebView;
            // Wedding 서비스 Blazor 호스트에 공유 (ViewModel은 AutoRegisterViewModels로 자동 등록)
            options.SharedServiceTypes.Add(typeof(WeddingOptions));
            options.SharedServiceTypes.Add(typeof(ITenantStore));
            options.SharedServiceTypes.Add(typeof(IGuestbookStorage));
            options.SharedServiceTypes.Add(typeof(IGlobalSettingsStore));
            options.SharedServiceTypes.Add(typeof(IMediaQuotaPolicyResolver));
            options.SharedServiceTypes.Add(typeof(IImageOptimizationService));
            options.SharedServiceTypes.Add(typeof(IMediaMigrationService));
            options.SharedServiceTypes.Add(typeof(IMediaUsageQueryService));
            options.SharedServiceTypes.Add(typeof(ISuperAdminSessionTokenService));
            options.SharedServiceTypes.Add(typeof(IPhotoService));
            // 업로드된 사진/미디어를 /wedding-data/ URL로 제공
            options.AddPhysicalStaticFiles(weddingOpts.ResolvedDataPath, "/wedding-data");

            // InputFile(동영상 등 대용량 업로드)은 SignalR 회선을 통해 청크 단위로 전송되는데,
            // 기본 SignalR 메시지 크기 제한(32KB)과 짧은 타임아웃 때문에 큰 파일은 매우 느리거나
            // 응답 없이 멈춘 것처럼 보입니다. 업로드 용량 검증은 LocalPhotoService에서 별도로
            // 하고 있으므로 전송 한도 자체는 풀어줍니다.
            options.ConfigureServices = services =>
            {
                services.AddServerSideBlazor()
                    .AddHubOptions(o =>
                    {
                        o.MaximumReceiveMessageSize = null; // 무제한 (용량 검증은 LocalPhotoService에서 수행)
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
                    o.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(1);
                    o.MaxBufferedUnacknowledgedRenderBatches = 10;
                });

                services.AddScoped<ThankYouUserContext>();
            };

            options.AddDreamineIdentity(authOptions, usersDbPath);
        });

        builder.Build().RunDreamineWpfApp<App>();
    }

    /// <summary>
    /// \if KO
    /// <para>Is Tcp Port Available 조건을 확인합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Determines whether is tcp port available.</para>
    /// \endif
    /// </summary>
    /// <param name="port">
    /// \if KO
    /// <para>port에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for port.</para>
    /// \endif
    /// </param>
    /// <param name="listenAnyIp">
    /// \if KO
    /// <para>listen Any Ip에 사용할 <c>bool</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>bool</c> value used for listen any ip.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Is Tcp Port Available 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the is tcp port available condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    private static bool IsTcpPortAvailable(int port, bool listenAnyIp)
    {
        if (port <= 0) return false;

        TcpListener? listener = null;
        try
        {
            listener = new TcpListener(listenAnyIp ? IPAddress.Any : IPAddress.Loopback, port);
            listener.Start();
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
        finally
        {
            listener?.Stop();
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Free Loopback Port 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the free loopback port value.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Get Free Loopback Port 작업에서 생성한 <c>int</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> result produced by the get free loopback port operation.</para>
    /// \endif
    /// </returns>
    private static int GetFreeLoopbackPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Startup Failure Log 데이터를 씁니다.</para>
    /// \endif
    /// \if EN
    /// <para>Writes startup failure log data.</para>
    /// \endif
    /// </summary>
    /// <param name="ex">
    /// \if KO
    /// <para>ex에 사용할 <c>Exception</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Exception</c> value used for ex.</para>
    /// \endif
    /// </param>
    private static void WriteStartupFailureLog(Exception ex)
    {
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "WeddingThankYou",
                "Logs");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "startup-error.log");
            File.AppendAllText(path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex}\r\n\r\n");
        }
        catch
        {
            // 마지막 안전망: 오류 로그 작성 실패가 앱 종료 원인이 되지 않게 둡니다.
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
