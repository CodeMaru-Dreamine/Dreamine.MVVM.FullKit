using System.IO;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components.Forms;

namespace DreamineWeb.Services;

/// <summary>
/// \if KO
/// <para>Playground Media Service 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates playground media service functionality and related state.</para>
/// \endif
/// </summary>
public sealed class PlaygroundMediaService
{
    /// <summary>
    /// \if KO
    /// <para>Unsafe Name 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the unsafe name value.</para>
    /// \endif
    /// </summary>
    private static readonly Regex UnsafeName = new(@"[^a-zA-Z0-9._-]+", RegexOptions.Compiled);
    /// <summary>
    /// \if KO
    /// <para>upload Root 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the upload root value.</para>
    /// \endif
    /// </summary>
    private readonly string _uploadRoot;

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="PlaygroundMediaService"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="PlaygroundMediaService"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    public PlaygroundMediaService()
    {
        _uploadRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot", "uploads", "playground");
    }

    /// <summary>
    /// \if KO
    /// <para>Async 데이터를 저장합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Saves async data.</para>
    /// \endif
    /// </summary>
    /// <param name="file">
    /// \if KO
    /// <para>file에 사용할 <c>IBrowserFile</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IBrowserFile</c> value used for file.</para>
    /// \endif
    /// </param>
    /// <param name="demoId">
    /// \if KO
    /// <para>demo Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for demo id.</para>
    /// \endif
    /// </param>
    /// <param name="platform">
    /// \if KO
    /// <para>platform에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for platform.</para>
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
    /// <para>Save Async 작업에서 생성한 <c>Task&lt;string&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;string&gt;</c> result produced by the save async operation.</para>
    /// \endif
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// \if KO
    /// <para>현재 객체 상태에서 Save Async 작업을 수행할 수 없는 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when the save async operation is not valid for the current object state.</para>
    /// \endif
    /// </exception>
    public async Task<string> SaveAsync(IBrowserFile file, string demoId, string platform, CancellationToken ct = default)
    {
        if (file.Size <= 0) throw new InvalidOperationException("Empty files cannot be uploaded.");

        var safeDemoId = SafeSegment(demoId, "demo");
        var safePlatform = SafeSegment(platform, "platform");
        var extension = Path.GetExtension(file.Name);
        if (string.IsNullOrWhiteSpace(extension)) extension = GuessExtension(file.ContentType);

        var targetDir = Path.Combine(_uploadRoot, safeDemoId);
        Directory.CreateDirectory(targetDir);

        var fileName = $"{safePlatform}-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..8]}{extension.ToLowerInvariant()}";
        var path = Path.Combine(targetDir, fileName);

        await using var input = file.OpenReadStream(maxAllowedSize: 200 * 1024 * 1024, cancellationToken: ct);
        await using var output = File.Create(path);
        await input.CopyToAsync(output, ct);

        return $"/uploads/playground/{safeDemoId}/{fileName}";
    }

    /// <summary>
    /// \if KO
    /// <para>Safe Segment 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the safe segment operation.</para>
    /// \endif
    /// </summary>
    /// <param name="value">
    /// \if KO
    /// <para>적용할 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The value to apply.</para>
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
    /// <para>Safe Segment 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the safe segment operation.</para>
    /// \endif
    /// </returns>
    private static string SafeSegment(string? value, string fallback)
    {
        value = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        var safe = UnsafeName.Replace(value, "-").Trim('-', '.');
        return string.IsNullOrWhiteSpace(safe) ? fallback : safe;
    }

    /// <summary>
    /// \if KO
    /// <para>Guess Extension 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the guess extension operation.</para>
    /// \endif
    /// </summary>
    /// <param name="contentType">
    /// \if KO
    /// <para>content Type에 사용할 <c>string?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for content type.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Guess Extension 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the guess extension operation.</para>
    /// \endif
    /// </returns>
    private static string GuessExtension(string? contentType) => contentType?.ToLowerInvariant() switch
    {
        "image/png" => ".png",
        "image/jpeg" => ".jpg",
        "image/gif" => ".gif",
        "image/webp" => ".webp",
        "image/svg+xml" => ".svg",
        "video/mp4" => ".mp4",
        "video/webm" => ".webm",
        "video/quicktime" => ".mov",
        _ => ".bin"
    };
}
