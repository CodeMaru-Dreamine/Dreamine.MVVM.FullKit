using Codemaru.Models;
using Microsoft.Extensions.Options;
using System.IO;

namespace Codemaru.Services;

/// <summary>
/// \if KO
/// <para>Landing Page Publish Result 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates landing page publish result functionality and related state.</para>
/// \endif
/// </summary>
public sealed record LandingPagePublishResult(string FilePath, string PublicPath, string PublicUrl);

/// <summary>
/// \if KO
/// <para>Landing Page Publish Options 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates landing page publish options functionality and related state.</para>
/// \endif
/// </summary>
public sealed class LandingPagePublishOptions
{
    /// <summary>
    /// \if KO
    /// <para>Web Root Path 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the web root path value.</para>
    /// \endif
    /// </summary>
    public string WebRootPath { get; set; } = string.Empty;
}

/// <summary>
/// \if KO
/// <para>Landing Page Publisher 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates landing page publisher functionality and related state.</para>
/// \endif
/// </summary>
public sealed class LandingPagePublisher
{
    /// <summary>
    /// \if KO
    /// <para>web Root Path 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the web root path value.</para>
    /// \endif
    /// </summary>
    private readonly string _webRootPath;

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="LandingPagePublisher"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="LandingPagePublisher"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="options">
    /// \if KO
    /// <para>동작을 구성하는 설정입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The options that configure the operation.</para>
    /// \endif
    /// </param>
    public LandingPagePublisher(IOptions<LandingPagePublishOptions> options)
    {
        var configuredPath = options.Value.WebRootPath;
        _webRootPath = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(AppContext.BaseDirectory, "wwwroot")
            : Path.GetFullPath(configuredPath);
    }

    /// <summary>
    /// \if KO
    /// <para>Publish Index Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the publish index async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="profile">
    /// \if KO
    /// <para>profile에 사용할 <c>CardProfile</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CardProfile</c> value used for profile.</para>
    /// \endif
    /// </param>
    /// <param name="html">
    /// \if KO
    /// <para>html에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for html.</para>
    /// \endif
    /// </param>
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
    /// <para>Publish Index Async 작업에서 생성한 <c>Task&lt;LandingPagePublishResult&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;LandingPagePublishResult&gt;</c> result produced by the publish index async operation.</para>
    /// \endif
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// \if KO
    /// <para>현재 객체 상태에서 Publish Index Async 작업을 수행할 수 없는 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when the publish index async operation is not valid for the current object state.</para>
    /// \endif
    /// </exception>
    public Task<LandingPagePublishResult> PublishIndexAsync(
        CardProfile profile,
        string html,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);

        if (string.IsNullOrWhiteSpace(html))
        {
            throw new InvalidOperationException("저장할 HTML이 없습니다.");
        }

        var slugSegments = NormalizeSlug(profile.LandingSlug);
        if (slugSegments.Length == 0)
        {
            throw new InvalidOperationException("랜딩 슬러그를 입력해 주세요.");
        }

        var targetDirectory = _webRootPath;
        foreach (var segment in slugSegments)
        {
            targetDirectory = Path.Combine(targetDirectory, segment);
        }
        Directory.CreateDirectory(targetDirectory);

        var filePath = Path.Combine(targetDirectory, "index.html");
        return WriteAsync(profile, html, filePath, slugSegments, cancellationToken);
    }

    /// <summary>
    /// \if KO
    /// <para>Async 데이터를 씁니다.</para>
    /// \endif
    /// \if EN
    /// <para>Writes async data.</para>
    /// \endif
    /// </summary>
    /// <param name="profile">
    /// \if KO
    /// <para>profile에 사용할 <c>CardProfile</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CardProfile</c> value used for profile.</para>
    /// \endif
    /// </param>
    /// <param name="html">
    /// \if KO
    /// <para>html에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for html.</para>
    /// \endif
    /// </param>
    /// <param name="filePath">
    /// \if KO
    /// <para>file Path에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for file path.</para>
    /// \endif
    /// </param>
    /// <param name="slugSegments">
    /// \if KO
    /// <para>slug Segments에 사용할 <c>string[]</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string[]</c> value used for slug segments.</para>
    /// \endif
    /// </param>
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
    /// <para>Write Async 작업에서 생성한 <c>Task&lt;LandingPagePublishResult&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;LandingPagePublishResult&gt;</c> result produced by the write async operation.</para>
    /// \endif
    /// </returns>
    private static async Task<LandingPagePublishResult> WriteAsync(
        CardProfile profile,
        string html,
        string filePath,
        string[] slugSegments,
        CancellationToken cancellationToken)
    {
        await File.WriteAllTextAsync(filePath, html, cancellationToken).ConfigureAwait(false);

        var publicPath = "/" + string.Join("/", slugSegments) + "/index.html";
        var publicUrl = BuildPublicUrl(profile, slugSegments);

        return new LandingPagePublishResult(filePath, publicPath, publicUrl);
    }

    /// <summary>
    /// \if KO
    /// <para>Normalize Slug 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the normalize slug operation.</para>
    /// \endif
    /// </summary>
    /// <param name="slug">
    /// \if KO
    /// <para>slug에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for slug.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Normalize Slug 작업에서 생성한 <c>string[]</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string[]</c> result produced by the normalize slug operation.</para>
    /// \endif
    /// </returns>
    private static string[] NormalizeSlug(string slug)
    {
        return CardLandingPath.Split(slug)
            .Select(CardLandingPath.Slugify)
            .Where(static segment => !string.IsNullOrWhiteSpace(segment))
            .ToArray();
    }

    /// <summary>
    /// \if KO
    /// <para>Public Url 값을 구성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Builds the public url value.</para>
    /// \endif
    /// </summary>
    /// <param name="profile">
    /// \if KO
    /// <para>profile에 사용할 <c>CardProfile</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CardProfile</c> value used for profile.</para>
    /// \endif
    /// </param>
    /// <param name="slugSegments">
    /// \if KO
    /// <para>slug Segments에 사용할 <c>string[]</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string[]</c> value used for slug segments.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Build Public Url 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the build public url operation.</para>
    /// \endif
    /// </returns>
    private static string BuildPublicUrl(CardProfile profile, string[] slugSegments)
    {
        var website = string.IsNullOrWhiteSpace(profile.Website)
            ? string.Empty
            : profile.Website.Trim().TrimEnd('/');

        var slug = string.Join("/", slugSegments);
        if (string.IsNullOrWhiteSpace(website))
        {
            return "/" + slug + "/";
        }

        return website.EndsWith($"/{slug}", StringComparison.OrdinalIgnoreCase)
            ? website + "/"
            : $"{website}/{slug}/";
    }
}
