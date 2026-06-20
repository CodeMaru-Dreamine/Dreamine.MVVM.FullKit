using System.IO;
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
        var tmp = path + ".tmp";
        await using (var fs = File.Create(tmp))
            await JsonSerializer.SerializeAsync(fs, post, _opts);
        File.Copy(tmp, path, overwrite: true);
        File.Delete(tmp);
        return true;
    }

    private static string Sanitize(string id) =>
        string.Concat(id.Split(Path.GetInvalidFileNameChars()));
}
