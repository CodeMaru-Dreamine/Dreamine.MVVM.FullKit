using Dreamine.Hybrid.Interfaces;
using Dreamine.Hybrid.State;
using DreamineVMS.Blazor.ViewModels;
using DreamineVMS.Models;
using DreamineVMS.Options;
using DreamineVMS.Services.Auth;
using DreamineVMS.Services.Cameras;
using DreamineVMS.Services.Dashboard;
using DreamineVMS.Services.Runtime;
using DreamineVMS.Services.Streaming;
using DreamineVMS.States;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.IO;
using System.Reflection;

namespace DreamineVMS.Hosting;

/// <summary>
/// \if KO
/// <para>\brief DreamineVMS 전용 Blazor Server HostedService입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates vms blazor server hosted service functionality and related state.</para>
/// \endif
/// </summary>
/// <typeparam name="TRootComponent">
/// \if KO
/// <para>루트 Razor 컴포넌트 타입입니다.</para>
/// \endif
/// \if EN
/// <para>The TRootComponent type parameter.</para>
/// \endif
/// </typeparam>
public sealed class VmsBlazorServerHostedService<TRootComponent> : IHostedService
    where TRootComponent : IComponent
{
    /// <summary>
    /// \if KO
    /// <para>root Service Provider 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the root service provider value.</para>
    /// \endif
    /// </summary>
    private readonly IServiceProvider _rootServiceProvider;
    /// <summary>
    /// \if KO
    /// <para>options 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the options value.</para>
    /// \endif
    /// </summary>
    private readonly VmsServerOptions _options;
    /// <summary>
    /// \if KO
    /// <para>web Host 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the web host value.</para>
    /// \endif
    /// </summary>
    private IHost? _webHost;

    /// <summary>
    /// \if KO
    /// <para>\brief VmsBlazorServerHostedService 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="VmsBlazorServerHostedService"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="rootServiceProvider">
    /// \if KO
    /// <para>WPF Host의 루트 서비스 공급자입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IServiceProvider</c> value used for root service provider.</para>
    /// \endif
    /// </param>
    /// <param name="options">
    /// \if KO
    /// <para>VMS Server 옵션입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The options that configure the operation.</para>
    /// \endif
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// \if KO
    /// <para>필수 입력 인자 중 하나가 <see langword="null"/>인 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when a required input argument is <see langword="null"/>.</para>
    /// \endif
    /// </exception>
    public VmsBlazorServerHostedService(
        IServiceProvider rootServiceProvider,
        IOptions<VmsServerOptions> options)
    {
        _rootServiceProvider = rootServiceProvider ?? throw new ArgumentNullException(nameof(rootServiceProvider));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// \if KO
    /// <para>Start Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the start async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="cancellationToken">
    /// \if KO
    /// <para>취소 요청을 감시하는 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Start Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the start async operation.</para>
    /// \endif
    /// </returns>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Assembly blazorAssembly = typeof(TRootComponent).Assembly;

        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = Array.Empty<string>(),
            ContentRootPath = AppContext.BaseDirectory,
            ApplicationName = blazorAssembly.GetName().Name
        });

        StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration);

        builder.Services
            .AddRazorComponents()
            .AddInteractiveServerComponents();

        builder.Services.Configure<CircuitOptions>(options =>
        {
            options.DetailedErrors = true;
        });

        RegisterSharedServices(builder.Services);
        RegisterBlazorViewModels(builder.Services);

        builder.WebHost.ConfigureKestrel(options =>
        {
            if (_options.ListenAnyIp)
            {
                options.ListenAnyIP(_options.Port);
            }
            else
            {
                options.ListenLocalhost(_options.Port);
            }
        });

        WebApplication app = builder.Build();

        FileExtensionContentTypeProvider contentTypes = CreateHlsContentTypeProvider();

        RemoveRangeHeadersForHls(app);
        UseHlsStaticFiles(app, contentTypes);

        app.UseRouting();
        app.UseAntiforgery();

        app.MapGet("/healthz", () => Results.Text("OK", "text/plain"));

        app.MapRazorComponents<TRootComponent>()
           .AddInteractiveServerRenderMode();

        _webHost = app;

        await app.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// \if KO
    /// <para>Stop Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the stop async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="cancellationToken">
    /// \if KO
    /// <para>취소 요청을 감시하는 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Stop Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the stop async operation.</para>
    /// \endif
    /// </returns>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_webHost is null)
        {
            return;
        }

        try
        {
            await _webHost.StopAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _webHost.Dispose();
            _webHost = null;
        }
    }

    /// <summary>
    /// \if KO
    /// <para>\brief WPF Host와 Blazor Server가 공유해야 하는 서비스를 등록합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the register shared services operation.</para>
    /// \endif
    /// </summary>
    /// <param name="services">
    /// \if KO
    /// <para>Blazor Server 서비스 컬렉션입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IServiceCollection</c> value used for services.</para>
    /// \endif
    /// </param>
    private void RegisterSharedServices(IServiceCollection services)
    {
        AddShared<IHybridMessageBus>(services);
        AddShared<IHybridStateStore<VmsDashboardState>>(services);
        AddShared<IVmsCameraRepository>(services);
        AddShared<ICameraRuntimeStateService>(services);
        AddShared<ICameraStreamService>(services);
        AddShared<IVmsDashboardStateService>(services);

    }

    /// <summary>
    /// \if KO
    /// <para>\brief Blazor Server 전용 ViewModel 서비스를 등록합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the register blazor view models operation.</para>
    /// \endif
    /// </summary>
    /// <param name="services">
    /// \if KO
    /// <para>Blazor Server 서비스 컬렉션입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IServiceCollection</c> value used for services.</para>
    /// \endif
    /// </param>
    private static void RegisterBlazorViewModels(IServiceCollection services)
    {
        services.AddScoped<LivePageViewModel>();
        services.AddScoped<DashboardPageViewModel>();
        services.AddScoped<VmsLocalDashboardViewModel>();
    }

    /// <summary>
    /// \if KO
    /// <para>\brief WPF Host에 이미 등록된 서비스를 Blazor Server DI에 공유 등록합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Adds the shared item.</para>
    /// \endif
    /// </summary>
    /// <typeparam name="TService">
    /// \if KO
    /// <para>공유할 서비스 타입입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The TService type parameter.</para>
    /// \endif
    /// </typeparam>
    /// <param name="services">
    /// \if KO
    /// <para>Blazor Server 서비스 컬렉션입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IServiceCollection</c> value used for services.</para>
    /// \endif
    /// </param>
    private void AddShared<TService>(IServiceCollection services)
        where TService : class
    {
        TService service = _rootServiceProvider.GetRequiredService<TService>();
        services.AddSingleton(service);
    }

    /// <summary>
    /// \if KO
    /// <para>\brief HLS 파일 확장자에 대한 Content-Type Provider를 생성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Creates the hls content type provider value.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>HLS Content-Type Provider입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>FileExtensionContentTypeProvider</c> result produced by the create hls content type provider operation.</para>
    /// \endif
    /// </returns>
    private static FileExtensionContentTypeProvider CreateHlsContentTypeProvider()
    {
        FileExtensionContentTypeProvider contentTypes = new();

        contentTypes.Mappings[".m3u8"] = "application/vnd.apple.mpegurl";
        contentTypes.Mappings[".ts"] = "video/mp2t";
        contentTypes.Mappings[".mp4"] = "video/mp4";

        return contentTypes;
    }

    /// <summary>
    /// \if KO
    /// <para>\brief HLS 요청에서 Range 헤더를 제거합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Removes the range headers for hls item.</para>
    /// \endif
    /// </summary>
    /// <param name="app">
    /// \if KO
    /// <para>WebApplication 인스턴스입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>WebApplication</c> value used for app.</para>
    /// \endif
    /// </param>
    private static void RemoveRangeHeadersForHls(WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            PathString path = context.Request.Path;

            if (IsHlsPath(path))
            {
                context.Request.Headers.Remove("Range");
                context.Request.Headers.Remove("If-Range");
            }

            await next().ConfigureAwait(false);
        });
    }

    /// <summary>
    /// \if KO
    /// <para>\brief HLS 정적 파일 제공 설정을 적용합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the use hls static files operation.</para>
    /// \endif
    /// </summary>
    /// <param name="app">
    /// \if KO
    /// <para>WebApplication 인스턴스입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>WebApplication</c> value used for app.</para>
    /// \endif
    /// </param>
    /// <param name="contentTypes">
    /// \if KO
    /// <para>Content-Type Provider입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>FileExtensionContentTypeProvider</c> value used for content types.</para>
    /// \endif
    /// </param>
    private static void UseHlsStaticFiles(
        WebApplication app,
        FileExtensionContentTypeProvider contentTypes)
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            ContentTypeProvider = contentTypes,
            OnPrepareResponse = context =>
            {
                string extension = Path.GetExtension(context.File.PhysicalPath ?? string.Empty)
                    .ToLowerInvariant();

                if (extension is ".m3u8" or ".ts")
                {
                    IHeaderDictionary headers = context.Context.Response.Headers;

                    headers["Cache-Control"] = "no-store, must-revalidate";
                    headers["Pragma"] = "no-cache";
                    headers["Expires"] = "0";
                    headers["Accept-Ranges"] = "none";
                }
            }
        });
    }

    /// <summary>
    /// \if KO
    /// <para>\brief 요청 경로가 HLS 파일인지 확인합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Determines whether is hls path.</para>
    /// \endif
    /// </summary>
    /// <param name="path">
    /// \if KO
    /// <para>요청 경로입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>PathString</c> value used for path.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>HLS 파일이면 true입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the is hls path condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    private static bool IsHlsPath(PathString path)
    {
        if (!path.HasValue)
        {
            return false;
        }

        return path.Value.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase)
            || path.Value.EndsWith(".ts", StringComparison.OrdinalIgnoreCase);
    }
}