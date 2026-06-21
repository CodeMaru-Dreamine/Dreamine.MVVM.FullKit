using System.IO;
using System.Text.RegularExpressions;
using System.Text.Json;
using FamiliesAutoWriter.Models;


namespace FamiliesAutoWriter.Services;

public sealed class PostWriterService
{
    // JsonStringEnumConverter 미사용 → MediaPosition을 숫자(0/1)로 저장
    // Families.Web이 숫자 형식을 기본으로 읽기 때문
    private static readonly JsonSerializerOptions _opts = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    public string AppDataRoot { get; set; } = "";

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

    private static bool IsCookingPost(string title) =>
        title.Contains("1983", StringComparison.OrdinalIgnoreCase) &&
        title.Contains("집밥육아", StringComparison.OrdinalIgnoreCase);

    private static int ExtractCookingNumber(string title)
    {
        var match = Regex.Match(title, @"#(?<n>\d+)");
        return match.Success && int.TryParse(match.Groups["n"].Value, out var n) ? n : 0;
    }

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

    private static string Sanitize(string id) =>
        string.Concat(id.Split(Path.GetInvalidFileNameChars()));
}
