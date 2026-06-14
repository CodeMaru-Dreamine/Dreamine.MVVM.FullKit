using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Codemaru.Services;

/// <summary>
/// 외부 사이트의 og:image를 주기적으로 가져와 wwwroot/img/previews/ 에 캐시합니다.
/// </summary>
public sealed class SitePreviewService : BackgroundService
{
    private static readonly (string Key, string Url)[] Sites =
    [
        ("dreamine",   "https://dreamine.kr/"),
        ("wedding",    "https://wedding.codemaru.co.kr"),
        ("families",   "https://families.codemaru.co.kr"),
        ("portfolio",  "https://portfolio.codemaru.co.kr/"),
        ("cctv",       "https://cctvviewer.codemaru.co.kr"),
        ("shop",       "https://shop.codemaru.co.kr/"),
    ];

    private static readonly Regex OgImageRegex = new(
        @"<meta[^>]+property=[""']og:image[""'][^>]+content=[""']([^""']+)[""']",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex OgImageRegex2 = new(
        @"<meta[^>]+content=[""']([^""']+)[""'][^>]+property=[""']og:image[""']",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly string _wwwRoot;
    private readonly ILogger<SitePreviewService> _logger;
    private readonly TimeSpan _interval;
    private readonly HttpClient _http;

    public SitePreviewService(ILogger<SitePreviewService> logger, IConfiguration config)
    {
        _logger = logger;
        _interval = TimeSpan.FromHours(config.GetValue("SitePreview:RefreshHours", 6));
        _wwwRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        _http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = true });
        _http.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (compatible; CodemaruBot/1.0; +https://codemaru.co.kr)");
        _http.Timeout = TimeSpan.FromSeconds(15);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await FetchAllAsync(stoppingToken);

            try { await Task.Delay(_interval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task FetchAllAsync(CancellationToken ct)
    {
        var outputDir = Path.Combine(_wwwRoot, "img", "previews");
        Directory.CreateDirectory(outputDir);

        foreach (var (key, url) in Sites)
        {
            if (ct.IsCancellationRequested) break;
            await FetchOneAsync(key, url, outputDir, ct);
        }
    }

    private async Task FetchOneAsync(string key, string siteUrl, string outputDir, CancellationToken ct)
    {
        var destPath = Path.Combine(outputDir, $"{key}.jpg");
        var tempPath = destPath + ".tmp";

        try
        {
            // 1. 사이트 HTML 가져오기
            var html = await _http.GetStringAsync(siteUrl, ct);

            // 2. og:image URL 파싱
            var match = OgImageRegex.Match(html);
            if (!match.Success) match = OgImageRegex2.Match(html);
            if (!match.Success)
            {
                _logger.LogWarning("og:image 없음: {Key}", key);
                return;
            }

            var imageUrl = match.Groups[1].Value.Trim();

            // 상대 경로면 절대 경로로 변환
            if (!imageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                var base64 = new Uri(siteUrl);
                imageUrl = new Uri(base64, imageUrl).ToString();
            }

            // 3. 이미지 다운로드
            var imageBytes = await _http.GetByteArrayAsync(imageUrl, ct);
            await File.WriteAllBytesAsync(tempPath, imageBytes, ct);
            File.Move(tempPath, destPath, overwrite: true);

            _logger.LogInformation("OG 이미지 캐시 완료: {Key} ← {ImageUrl}", key, imageUrl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OG 이미지 fetch 실패: {Key} ({Url})", key, siteUrl);
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    public override void Dispose()
    {
        _http.Dispose();
        base.Dispose();
    }
}
