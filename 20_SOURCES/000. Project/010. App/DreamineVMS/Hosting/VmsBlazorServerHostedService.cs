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
/// \brief DreamineVMS 전용 Blazor Server HostedService입니다.
/// </summary>
/// <typeparam name="TRootComponent">루트 Razor 컴포넌트 타입입니다.</typeparam>
public sealed class VmsBlazorServerHostedService<TRootComponent> : IHostedService
    where TRootComponent : IComponent
{
    private readonly IServiceProvider _rootServiceProvider;
    private readonly VmsServerOptions _options;
    private IHost? _webHost;

    /// <summary>
    /// \brief VmsBlazorServerHostedService 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="rootServiceProvider">WPF Host의 루트 서비스 공급자입니다.</param>
    /// <param name="options">VMS Server 옵션입니다.</param>
    public VmsBlazorServerHostedService(
        IServiceProvider rootServiceProvider,
        IOptions<VmsServerOptions> options)
    {
        _rootServiceProvider = rootServiceProvider ?? throw new ArgumentNullException(nameof(rootServiceProvider));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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
    /// \brief WPF Host와 Blazor Server가 공유해야 하는 서비스를 등록합니다.
    /// </summary>
    /// <param name="services">Blazor Server 서비스 컬렉션입니다.</param>
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
    /// \brief Blazor Server 전용 ViewModel 서비스를 등록합니다.
    /// </summary>
    /// <param name="services">Blazor Server 서비스 컬렉션입니다.</param>
    private static void RegisterBlazorViewModels(IServiceCollection services)
    {
        services.AddScoped<LivePageViewModel>();
        services.AddScoped<DashboardPageViewModel>();
        services.AddScoped<VmsLocalDashboardViewModel>();
    }

    /// <summary>
    /// \brief WPF Host에 이미 등록된 서비스를 Blazor Server DI에 공유 등록합니다.
    /// </summary>
    /// <typeparam name="TService">공유할 서비스 타입입니다.</typeparam>
    /// <param name="services">Blazor Server 서비스 컬렉션입니다.</param>
    private void AddShared<TService>(IServiceCollection services)
        where TService : class
    {
        TService service = _rootServiceProvider.GetRequiredService<TService>();
        services.AddSingleton(service);
    }

    /// <summary>
    /// \brief HLS 파일 확장자에 대한 Content-Type Provider를 생성합니다.
    /// </summary>
    /// <returns>HLS Content-Type Provider입니다.</returns>
    private static FileExtensionContentTypeProvider CreateHlsContentTypeProvider()
    {
        FileExtensionContentTypeProvider contentTypes = new();

        contentTypes.Mappings[".m3u8"] = "application/vnd.apple.mpegurl";
        contentTypes.Mappings[".ts"] = "video/mp2t";
        contentTypes.Mappings[".mp4"] = "video/mp4";

        return contentTypes;
    }

    /// <summary>
    /// \brief HLS 요청에서 Range 헤더를 제거합니다.
    /// </summary>
    /// <param name="app">WebApplication 인스턴스입니다.</param>
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
    /// \brief HLS 정적 파일 제공 설정을 적용합니다.
    /// </summary>
    /// <param name="app">WebApplication 인스턴스입니다.</param>
    /// <param name="contentTypes">Content-Type Provider입니다.</param>
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
    /// \brief 요청 경로가 HLS 파일인지 확인합니다.
    /// </summary>
    /// <param name="path">요청 경로입니다.</param>
    /// <returns>HLS 파일이면 true입니다.</returns>
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