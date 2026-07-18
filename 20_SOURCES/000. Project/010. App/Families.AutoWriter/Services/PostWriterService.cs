using System.IO;
using System.Text.RegularExpressions;
using System.Text.Json;
using FamiliesAutoWriter.Models;


namespace FamiliesAutoWriter.Services;

/// <summary>
/// \if KO
/// <para>Post Writer Service 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates post writer service functionality and related state.</para>
/// \endif
/// </summary>
public sealed class PostWriterService
{
    // JsonStringEnumConverter 미사용 → MediaPosition을 숫자(0/1)로 저장
    // Families.Web이 숫자 형식을 기본으로 읽기 때문
    /// <summary>
    /// \if KO
    /// <para>opts 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the opts value.</para>
    /// \endif
    /// </summary>
    private static readonly JsonSerializerOptions _opts = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// \if KO
    /// <para>App Data Root 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the app data root value.</para>
    /// \endif
    /// </summary>
    public string AppDataRoot { get; set; } = "";

    /// <summary>
    /// \if KO
    /// <para>Albums 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the albums value.</para>
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
    /// <para>Get Albums 작업에서 생성한 <c>IReadOnlyList&lt;AlbumInfo&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IReadOnlyList&lt;AlbumInfo&gt;</c> result produced by the get albums operation.</para>
    /// \endif
    /// </returns>
    public IReadOnlyList<AlbumInfo> GetAlbums(string slug)
    {
        var dir = Path.Combine(AppDataRoot, "Family", slug, "albums");
        if (!Directory.Exists(dir)) return [];
        var list = new List<AlbumInfo>();
        foreach (var f in Directory.GetFiles(dir, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(f);
                var a = JsonSerializer.Deserialize<AlbumInfo>(json, _opts);
                if (a != null) list.Add(a);
            }
            catch { }
        }
        return list.OrderBy(a => a.SortOrder).ThenBy(a => a.Name).ToList();
    }

    /// <summary>
    /// \if KO
    /// <para>Slugs 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the slugs value.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Get Slugs 작업에서 생성한 <c>IReadOnlyList&lt;string&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IReadOnlyList&lt;string&gt;</c> result produced by the get slugs operation.</para>
    /// \endif
    /// </returns>
    public IReadOnlyList<string> GetSlugs()
    {
        var root = Path.Combine(AppDataRoot, "Family");
        if (!Directory.Exists(root)) return [];
        return Directory.GetDirectories(root)
            .Select(Path.GetFileName)
            .Where(s => s != null)
            .Cast<string>()
            .ToList();
    }

    /// <summary>
    /// \if KO
    /// <para>Post Async 데이터를 저장합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Saves post async data.</para>
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
    /// <param name="post">
    /// \if KO
    /// <para>post에 사용할 <c>PostEntry</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>PostEntry</c> value used for post.</para>
    /// \endif
    /// </param>
    /// <param name="localPhotoPaths">
    /// \if KO
    /// <para>local Photo Paths에 사용할 <c>IEnumerable&lt;string&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IEnumerable&lt;string&gt;</c> value used for local photo paths.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Save Post Async 작업에서 생성한 <c>Task&lt;bool&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;bool&gt;</c> result produced by the save post async operation.</para>
    /// \endif
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// \if KO
    /// <para>현재 객체 상태에서 Save Post Async 작업을 수행할 수 없는 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when the save post async operation is not valid for the current object state.</para>
    /// \endif
    /// </exception>
    /// <exception cref="IOException">
    /// \if KO
    /// <para>Save Post Async 작업을 완료할 수 없는 경우 <c>IOException</c>이 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown as <c>IOException</c> when the save post async operation cannot be completed.</para>
    /// \endif
    /// </exception>
    public async Task<bool> SavePostAsync(string slug, PostEntry post, IEnumerable<string> localPhotoPaths)
    {
        if (string.IsNullOrWhiteSpace(AppDataRoot))
            throw new InvalidOperationException("App_Data 경로가 비어 있습니다.");
        if (string.IsNullOrWhiteSpace(slug))
            throw new InvalidOperationException("가족 슬러그가 비어 있습니다.");
        if (string.IsNullOrWhiteSpace(post.Id))
            post.Id = Guid.NewGuid().ToString("N");

        var tenantDir = Path.Combine(AppDataRoot, "Family", slug);
        var postsDir = Path.Combine(tenantDir, "posts");
        var mediaDir = Path.Combine(tenantDir, "media", post.Id);

        Directory.CreateDirectory(postsDir);
        Directory.CreateDirectory(mediaDir);

        // 로컬 사진 복사
        foreach (var src in localPhotoPaths)
        {
            if (!File.Exists(src)) continue;
            var fn = Path.GetFileName(src);
            var dst = Path.Combine(mediaDir, fn);
            File.Copy(src, dst, overwrite: true);
            if (!post.PhotoFileNames.Contains(fn))
                post.PhotoFileNames.Add(fn);
        }

        var path = Path.Combine(postsDir, $"{Sanitize(post.Id)}.json");
        var tmp = Path.Combine(postsDir, $"{Sanitize(post.Id)}.{Guid.NewGuid():N}.tmp");
        try
        {
            await using (var fs = File.Create(tmp))
                await JsonSerializer.SerializeAsync(fs, post, _opts);
            File.Move(tmp, path, overwrite: true);
        }
        catch (Exception ex)
        {
            throw new IOException($"JSON 저장 실패: {path} ({ex.Message})", ex);
        }
        finally
        {
            try
            {
                if (File.Exists(tmp)) File.Delete(tmp);
            }
            catch { }
        }
        return true;
    }

    // 기존 포스트 타이틀 목록 — 중복 방지용
    /// <summary>
    /// \if KO
    /// <para>Existing Titles 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the existing titles value.</para>
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
    /// <para>Get Existing Titles 작업에서 생성한 <c>IReadOnlyList&lt;string&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IReadOnlyList&lt;string&gt;</c> result produced by the get existing titles operation.</para>
    /// \endif
    /// </returns>
    public IReadOnlyList<string> GetExistingTitles(string slug)
    {
        var dir = Path.Combine(AppDataRoot, "Family", slug, "posts");
        if (!Directory.Exists(dir)) return [];
        var titles = new List<string>();
        foreach (var f in Directory.GetFiles(dir, "*.json"))
        {
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(f));
                if (TryGetPropertyIgnoreCase(doc.RootElement, "title", out var t))
                {
                    var v = t.GetString();
                    if (!string.IsNullOrWhiteSpace(v)) titles.Add(v);
                }
            }
            catch { }
        }
        return titles;
    }

    /// <summary>
    /// \if KO
    /// <para>Cooking Timeline State 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the cooking timeline state value.</para>
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
    /// <para>Get Cooking Timeline State 작업에서 생성한 <c>(DateTime? LastPostedAt, int LastNumber)</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>(DateTime? LastPostedAt, int LastNumber)</c> result produced by the get cooking timeline state operation.</para>
    /// \endif
    /// </returns>
    public (DateTime? LastPostedAt, int LastNumber) GetCookingTimelineState(string slug)
    {
        var dir = Path.Combine(AppDataRoot, "Family", slug, "posts");
        if (!Directory.Exists(dir)) return (null, 0);

        DateTime? lastPostedAt = null;
        var lastNumber = 0;

        foreach (var f in Directory.GetFiles(dir, "*.json"))
        {
            try
            {
                var post = JsonSerializer.Deserialize<PostEntry>(File.ReadAllText(f), _opts);
                if (post is null || !IsCookingPost(post.Title)) continue;

                if (lastPostedAt is null || post.PostedAt > lastPostedAt.Value)
                    lastPostedAt = post.PostedAt;

                lastNumber = Math.Max(lastNumber, ExtractCookingNumber(post.Title));
            }
            catch { }
        }

        return (lastPostedAt, lastNumber);
    }

    /// <summary>
    /// \if KO
    /// <para>Is Cooking Post 조건을 확인합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Determines whether is cooking post.</para>
    /// \endif
    /// </summary>
    /// <param name="title">
    /// \if KO
    /// <para>title에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for title.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Is Cooking Post 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the is cooking post condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    private static bool IsCookingPost(string title) =>
        title.Contains("1983", StringComparison.OrdinalIgnoreCase) &&
        title.Contains("집밥육아", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// \if KO
    /// <para>Extract Cooking Number 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the extract cooking number operation.</para>
    /// \endif
    /// </summary>
    /// <param name="title">
    /// \if KO
    /// <para>title에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for title.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Extract Cooking Number 작업에서 생성한 <c>int</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> result produced by the extract cooking number operation.</para>
    /// \endif
    /// </returns>
    private static int ExtractCookingNumber(string title)
    {
        var match = Regex.Match(title, @"#(?<n>\d+)");
        return match.Success && int.TryParse(match.Groups["n"].Value, out var n) ? n : 0;
    }

    /// <summary>
    /// \if KO
    /// <para>Get Property Ignore Case 작업을 시도하고 성공 여부를 반환합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Attempts to get property ignore case and returns whether the operation succeeds.</para>
    /// \endif
    /// </summary>
    /// <param name="element">
    /// \if KO
    /// <para>element에 사용할 <c>JsonElement</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>JsonElement</c> value used for element.</para>
    /// \endif
    /// </param>
    /// <param name="name">
    /// \if KO
    /// <para>name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for name.</para>
    /// \endif
    /// </param>
    /// <param name="value">
    /// \if KO
    /// <para>적용할 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The value to apply.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Try Get Property Ignore Case 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the try get property ignore case condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    private static bool TryGetPropertyIgnoreCase(JsonElement element, string name, out JsonElement value)
    {
        foreach (var prop in element.EnumerateObject())
        {
            if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = prop.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    /// <summary>
    /// \if KO
    /// <para>Sanitize 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the sanitize operation.</para>
    /// \endif
    /// </summary>
    /// <param name="id">
    /// \if KO
    /// <para>id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for id.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Sanitize 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the sanitize operation.</para>
    /// \endif
    /// </returns>
    private static string Sanitize(string id) =>
        string.Concat(id.Split(Path.GetInvalidFileNameChars()));
}
