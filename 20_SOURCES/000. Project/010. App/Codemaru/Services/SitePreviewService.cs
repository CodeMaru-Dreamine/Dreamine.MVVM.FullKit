using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Codemaru.Services;

/// <summary>
/// \if KO
/// <para>외부 사이트의 og:image를 주기적으로 가져와 wwwroot/img/previews/ 에 캐시합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates site preview service functionality and related state.</para>
/// \endif
/// </summary>
public sealed class SitePreviewService : BackgroundService
{
    /// <summary>
    /// \if KO
    /// <para>Sites 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the sites value.</para>
    /// \endif
    /// </summary>
    private static readonly (string Key, string Url)[] Sites =
    [
        ("dreamine",   "https://dreamine.kr/"),
        ("wedding",    "https://wedding.codemaru.co.kr"),
        ("thankyou",   "https://thankyou.codemaru.co.kr/"),
        ("families",   "https://families.codemaru.co.kr"),
        ("portfolio",  "https://portfolio.codemaru.co.kr/"),
        ("cctv",       "https://cctvviewer.codemaru.co.kr"),
        ("shop",       "https://shop.codemaru.co.kr/"),
    ];

    /// <summary>
    /// \if KO
    /// <para>Og Image Regex 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the og image regex value.</para>
    /// \endif
    /// </summary>
    private static readonly Regex OgImageRegex = new(
        @"<meta[^>]+property=[""']og:image[""'][^>]+content=[""']([^""']+)[""']",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// \if KO
    /// <para>Og Image Regex2 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the og image regex2 value.</para>
    /// \endif
    /// </summary>
    private static readonly Regex OgImageRegex2 = new(
        @"<meta[^>]+content=[""']([^""']+)[""'][^>]+property=[""']og:image[""']",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// \if KO
    /// <para>www Root 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the www root value.</para>
    /// \endif
    /// </summary>
    private readonly string _wwwRoot;
    /// <summary>
    /// \if KO
    /// <para>logger 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the logger value.</para>
    /// \endif
    /// </summary>
    private readonly ILogger<SitePreviewService> _logger;
    /// <summary>
    /// \if KO
    /// <para>interval 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the interval value.</para>
    /// \endif
    /// </summary>
    private readonly TimeSpan _interval;
    /// <summary>
    /// \if KO
    /// <para>http 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the http value.</para>
    /// \endif
    /// </summary>
    private readonly HttpClient _http;

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="SitePreviewService"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="SitePreviewService"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="logger">
    /// \if KO
    /// <para>logger에 사용할 <c>ILogger&lt;SitePreviewService&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ILogger&lt;SitePreviewService&gt;</c> value used for logger.</para>
    /// \endif
    /// </param>
    /// <param name="config">
    /// \if KO
    /// <para>config에 사용할 <c>IConfiguration</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IConfiguration</c> value used for config.</para>
    /// \endif
    /// </param>
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

    /// <summary>
    /// \if KO
    /// <para>Execute Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the execute async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="stoppingToken">
    /// \if KO
    /// <para>stopping Token에 사용할 <c>CancellationToken</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CancellationToken</c> value used for stopping token.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Execute Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the execute async operation.</para>
    /// \endif
    /// </returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await FetchAllAsync(stoppingToken);

            try { await Task.Delay(_interval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Fetch All Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the fetch all async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="ct">
    /// \if KO
    /// <para>취소 요청을 감시하는 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Fetch All Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the fetch all async operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Fetch One Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the fetch one async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="key">
    /// \if KO
    /// <para>key에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for key.</para>
    /// \endif
    /// </param>
    /// <param name="siteUrl">
    /// \if KO
    /// <para>site Url에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for site url.</para>
    /// \endif
    /// </param>
    /// <param name="outputDir">
    /// \if KO
    /// <para>output Dir에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for output dir.</para>
    /// \endif
    /// </param>
    /// <param name="ct">
    /// \if KO
    /// <para>취소 요청을 감시하는 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Fetch One Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the fetch one async operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>이 인스턴스가 소유한 리소스를 해제합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Releases resources owned by this instance.</para>
    /// \endif
    /// </summary>
    public override void Dispose()
    {
        _http.Dispose();
        base.Dispose();
    }
}
