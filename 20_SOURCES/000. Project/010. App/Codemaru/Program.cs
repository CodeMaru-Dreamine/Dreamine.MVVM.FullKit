using Codemaru.Blazor;
using Codemaru.Blazor.Pages;
using Codemaru.Models;
using Codemaru.Options;
using Codemaru.Services;
using Codemaru.Services.Certificates;
using Codemaru.ViewModels;
using Codemaru.Views;
using Dreamine.Hybrid.Wpf.DependencyInjection;
using Dreamine.Hybrid.Wpf.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Codemaru;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        int serverPort = GetInt(builder.Configuration, "CodemaruServer:Port", 5040);
        bool listenAnyIp = GetBool(builder.Configuration, "CodemaruServer:ListenAnyIp", true);

        builder.Services.AddDreamineHybridWpf();

        builder.Services.AddSingleton<IQrSvgGenerator, FreeQrSvgGenerator>();
        builder.Services.AddSingleton<ImageBackgroundRemover>();
        builder.Services.AddSingleton<LandingPageExporter>();
        builder.Services.AddSingleton<LandingPagePublisher>();
        builder.Services.AddSingleton<CardProfileImporter>();
        builder.Services.AddSingleton<ICardProfileStore, JsonCardProfileStore>();
        builder.Services.AddSingleton<CardHybridSession>();
        builder.Services.Configure<CertificateMonitorOptions>(
            builder.Configuration.GetSection("CertificateMonitor"));
        builder.Services.AddSingleton<IProcessRunner, ProcessRunner>();
        builder.Services.AddSingleton<ICertificateMonitorService, X509CertificateMonitorService>();
        builder.Services.AddSingleton<IWinAcmeService, WinAcmeService>();
        builder.Services.AddSingleton<INginxReloadService, NginxReloadService>();
        builder.Services.AddSingleton<ICertificateSettingsWriter, CertificateSettingsWriter>();
        builder.Services.AddSingleton<CertificateMonitorViewModel>();

        builder.Services.AddSingleton<MainWindow>();
        builder.Services.AddSingleton<MainWindowViewModel>();
        builder.Services.Configure<Contact.MailSettings>(
            builder.Configuration.GetSection("MailSettings"));
        builder.Services.Configure<FfmpegOptions>(
            builder.Configuration.GetSection("Ffmpeg"));
        builder.Services.Configure<List<StreamConfig>>(
            builder.Configuration.GetSection("Streams"));
        builder.Services.AddHostedService<SitePreviewService>();

        builder.Services.AddDreamineBlazorServer<AppShell>(options =>
        {
            options.Port = serverPort;
            options.ListenAnyIp = listenAnyIp;
            options.SharedServiceTypes.Add(typeof(CardHybridSession));
            options.SharedServiceTypes.Add(typeof(IQrSvgGenerator));
            options.SharedServiceTypes.Add(typeof(ImageBackgroundRemover));
            options.SharedServiceTypes.Add(typeof(LandingPageExporter));
            options.SharedServiceTypes.Add(typeof(LandingPagePublisher));
            options.SharedServiceTypes.Add(typeof(CardProfileImporter));
            options.ConfigurePipeline = app => app.Use(next =>
                ctx => OgTagMiddleware(ctx, next));
        });


        builder.Build().RunDreamineWpfApp<App>();
    }

    // 크롤러(카카오톡·네이버·구글 봇 등)가 접근하면 경로별 OG 태그가 포함된 최소 HTML을 반환합니다.
    private static readonly Dictionary<string, (string Title, string Desc, string Image, string Url)> OgMeta = new()
    {
        ["/"] = (
            "CodeMaru — 명함 · 청첩장 · 가족 플랫폼 · CCTV",
            "CodeMaru는 일상을 더 스마트하게 만드는 서비스를 만듭니다. 명함 관리 · 디지털 청첩장 · 가족 플랫폼 · 실시간 CCTV까지, 모두 여기에 있습니다.",
            "https://codemaru.co.kr/img/codemaru_og.png",
            "https://codemaru.co.kr/"),
        ["/cardhybrid"] = (
            "CardHybrid — 무료 명함 QR · vCard · 랜딩 페이지 | CodeMaru",
            "명함을 건네는 순간이 달라집니다. QR 스캔 하나로 내 소개 페이지 · 연락처 저장 · PDF 명함까지 — 지갑 속 종이 명함은 이제 그만.",
            "https://codemaru.co.kr/img/cardhybrid_og.png",
            "https://codemaru.co.kr/cardhybrid"),
        ["/contact"] = (
            "문의하기 | CodeMaru",
            "CodeMaru에 궁금한 점이나 제안 사항을 남겨주세요.",
            "https://codemaru.co.kr/img/codemaru_og.png",
            "https://codemaru.co.kr/contact"),
    };

    private static async Task OgTagMiddleware(HttpContext context, RequestDelegate next)
    {
        var ua = context.Request.Headers.UserAgent.ToString();
        // 카카오 인앱브라우저(KAKAOTALK/x.x)는 브라우저이므로 제외 — 크롤러만 감지
        bool isBot = ua.Contains("Kakaotalk-Scrap", StringComparison.OrdinalIgnoreCase)
                  || ua.Contains("kakaostory-og-reader", StringComparison.OrdinalIgnoreCase)
                  || ua.Contains("Naver", StringComparison.OrdinalIgnoreCase)
                  || ua.Contains("Googlebot", StringComparison.OrdinalIgnoreCase)
                  || ua.Contains("facebookexternalhit", StringComparison.OrdinalIgnoreCase)
                  || ua.Contains("Twitterbot", StringComparison.OrdinalIgnoreCase)
                  || ua.Contains("Discordbot", StringComparison.OrdinalIgnoreCase)
                  || ua.Contains("Slackbot", StringComparison.OrdinalIgnoreCase)
                  || ua.Contains("LinkedInBot", StringComparison.OrdinalIgnoreCase)
                  || ua.Contains("Baiduspider", StringComparison.OrdinalIgnoreCase);

        var method = context.Request.Method;
        if (!isBot || (!string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(method, "HEAD", StringComparison.OrdinalIgnoreCase)))
        {
            await next(context);
            return;
        }

        var path = context.Request.Path.Value ?? "/";
        var normalizedPath = path == "/" ? "/" : path.TrimEnd('/');
        if (!OgMeta.TryGetValue(normalizedPath, out var og))
        {
            await next(context);
            return;
        }

        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync($"""
            <!DOCTYPE html>
            <html lang="ko">
            <head>
              <meta charset="utf-8" />
              <title>{og.Title}</title>
              <meta name="description" content="{og.Desc}" />
              <meta property="og:title" content="{og.Title}" />
              <meta property="og:description" content="{og.Desc}" />
              <meta property="og:image" content="{og.Image}" />
              <meta property="og:url" content="{og.Url}" />
              <meta property="og:type" content="website" />
              <meta property="og:site_name" content="CodeMaru" />
              <meta property="og:locale" content="ko_KR" />
              <meta name="twitter:card" content="summary_large_image" />
              <meta name="twitter:title" content="{og.Title}" />
              <meta name="twitter:description" content="{og.Desc}" />
              <meta name="twitter:image" content="{og.Image}" />
            </head>
            <body></body>
            </html>
            """);
    }

    private static int GetInt(IConfiguration configuration, string key, int fallback)
    {
        string? value = configuration[key];

        return int.TryParse(value, out int parsed)
            ? parsed
            : fallback;
    }

    private static bool GetBool(IConfiguration configuration, string key, bool fallback)
    {
        string? value = configuration[key];

        return bool.TryParse(value, out bool parsed)
            ? parsed
            : fallback;
    }
}
