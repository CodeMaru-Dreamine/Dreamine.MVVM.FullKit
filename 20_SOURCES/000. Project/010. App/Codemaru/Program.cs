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
        builder.Services.AddScoped<CardHybridCircuitSession>();
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
            options.SharedServiceTypes.Add(typeof(ICardProfileStore));
            options.ConfigureServices = services =>
            {
                services.AddScoped<CardHybridCircuitSession>();

                // DreamineBlazorServerHostedService는 Blazor 서버용으로 완전히 별도의
                // DI 컨테이너를 새로 만들기 때문에, 위에서 바깥쪽 호스트에 등록한
                // Configure<Contact.MailSettings>(...)가 전달되지 않는다(SharedServiceTypes에
                // 명시한 것만 복사됨). 그래서 Contact.razor가 주입받는 IOptions<MailSettings>가
                // 항상 기본값(빈 문자열)이 되어 "Host/User/Password 값이 비어 있습니다" 가드가
                // appsettings.json 내용과 무관하게 항상 걸렸다. 여기서 다시 등록해서 고친다.
                services.Configure<Contact.MailSettings>(builder.Configuration.GetSection("MailSettings"));
            };
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
            "QR 코드 하나로 내 정보를 전달합니다. 스캔만 하면 모바일 랜딩 페이지가 열리고 vCard로 연락처 저장까지. 앞뒤 명함 디자인 편집, AI 배경 제거, SVG 내보내기 무료 제공.",
            "https://codemaru.co.kr/img/cardhybrid_og.png",
            "https://codemaru.co.kr/cardhybrid"),
        ["/guide"] = (
            "이용 설명서 | CodeMaru",
            "CardHybrid 명함, Wedding 청첩장, Families 가족 앨범, CCTV Viewer, Shop Store, Portfolio, Dreamine 프레임워크의 상세 이용 가이드를 제공합니다.",
            "https://codemaru.co.kr/img/codemaru_og.png",
            "https://codemaru.co.kr/guide"),
        ["/guide/cardhybrid"] = (
            "CardHybrid 이용 설명서 — QR 명함 · vCard · 랜딩 페이지 | CodeMaru",
            "CardHybrid 시작하기부터 QR 생성, vCard 저장, 명함 디자인 편집, AI 배경 제거, SVG 내보내기까지 단계별 사용 방법을 안내합니다.",
            "https://codemaru.co.kr/img/cardhybrid_og.png",
            "https://codemaru.co.kr/guide/cardhybrid"),
        ["/guide/wedding"] = (
            "Wedding 이용 설명서 — 디지털 청첩장 만들기 | CodeMaru",
            "5분이면 완성되는 디지털 청첩장. 지도·갤러리·방명록·배경음악·계좌 안내까지 설정하는 방법을 단계별로 안내합니다.",
            "https://codemaru.co.kr/img/codemaru_og.png",
            "https://codemaru.co.kr/guide/wedding"),
        ["/guide/families"] = (
            "Families 이용 설명서 — 가족 앨범 플랫폼 | CodeMaru",
            "가족끼리만 공유하는 비공개 앨범·타임라인. 포스트 작성, 앨범 폴더, 이모지 반응, 댓글 기능 사용법을 안내합니다.",
            "https://codemaru.co.kr/img/codemaru_og.png",
            "https://codemaru.co.kr/guide/families"),
        ["/guide/cctv"] = (
            "CCTV Viewer 이용 설명서 — 실시간 카메라 | CodeMaru",
            "DreamineVMS 에이전트 설치부터 RTSP 카메라 등록, 공개 라이브 링크 발급까지 단계별 사용 방법을 안내합니다.",
            "https://codemaru.co.kr/img/codemaru_og.png",
            "https://codemaru.co.kr/guide/cctv"),
        ["/guide/shop"] = (
            "Shop Store 이용 설명서 — 직영 쇼핑몰 | CodeMaru",
            "농산물·소프트웨어·개발 용역 구매 방법, 장바구니, 주문·환불 정책 안내.",
            "https://codemaru.co.kr/img/codemaru_og.png",
            "https://codemaru.co.kr/guide/shop"),
        ["/guide/portfolio"] = (
            "Portfolio 이용 설명서 — 개발자 포트폴리오 | CodeMaru",
            "장민수 개발자의 프로젝트·이력서·기술 스택 포트폴리오 사이트 구성과 이용 방법을 안내합니다.",
            "https://codemaru.co.kr/img/codemaru_og.png",
            "https://codemaru.co.kr/guide/portfolio"),
        ["/guide/dreamine"] = (
            "Dreamine 이용 설명서 — MVVM 프레임워크 | CodeMaru",
            "WPF·Blazor 하이브리드를 위한 오픈소스 MVVM 프레임워크 Dreamine의 패키지 구성과 빠른 시작 방법을 안내합니다.",
            "https://codemaru.co.kr/img/codemaru_og.png",
            "https://codemaru.co.kr/guide/dreamine"),
        ["/contact"] = (
            "문의하기 | CodeMaru",
            "CodeMaru 서비스에 대한 문의, 제안, 협업 요청을 남겨주세요. 24시간 이내에 답변드립니다.",
            "https://codemaru.co.kr/img/codemaru_og.png",
            "https://codemaru.co.kr/contact"),
        ["/privacy"] = (
            "개인정보처리방침 | CodeMaru",
            "CodeMaru가 수집·이용·보관하는 개인정보에 대한 처리방침을 안내합니다.",
            "https://codemaru.co.kr/img/codemaru_og.png",
            "https://codemaru.co.kr/privacy"),
        ["/terms"] = (
            "이용약관 | CodeMaru",
            "CodeMaru 서비스 이용에 관한 약관을 안내합니다.",
            "https://codemaru.co.kr/img/codemaru_og.png",
            "https://codemaru.co.kr/terms"),
    };

    private static async Task OgTagMiddleware(HttpContext context, RequestDelegate next)
    {
        var ua = context.Request.Headers.UserAgent.ToString();
        // 카카오 인앱브라우저(KAKAOTALK/x.x)는 브라우저이므로 제외 — 크롤러만 감지
        bool isBot = ua.Contains("Kakaotalk-Scrap", StringComparison.OrdinalIgnoreCase)
                  || ua.Contains("kakaostory-og-reader", StringComparison.OrdinalIgnoreCase)
                  || ua.Contains("Naver", StringComparison.OrdinalIgnoreCase)
                  || ua.Contains("Googlebot", StringComparison.OrdinalIgnoreCase)
                  || ua.Contains("Mediapartners-Google", StringComparison.OrdinalIgnoreCase)
                  || ua.Contains("AdsBot-Google", StringComparison.OrdinalIgnoreCase)
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
