using System.IO;

namespace DreamineVMS.Web.Services.Hls;

/// <summary>에이전트가 푸시한 HLS 세그먼트를 파일로 저장하고 서빙합니다.</summary>
public sealed class HlsSegmentStore
{
    private const int RetainedSegmentCount = 45;
    private static readonly TimeSpan RetainedSegmentAge = TimeSpan.FromMinutes(2);

    private readonly string _root;

    public HlsSegmentStore(IWebHostEnvironment env)
    {
        _root = Path.Combine(env.ContentRootPath, "App_Data", "hls");
        Directory.CreateDirectory(_root);
    }

    public string GetCameraDir(string tenantId, string cameraId)
    {
        var dir = Path.Combine(_root, Sanitize(tenantId), Sanitize(cameraId));
        Directory.CreateDirectory(dir);
        return dir;
    }

    public async Task SaveSegmentAsync(string tenantId, string cameraId, string filename, Stream data)
    {
        if (!IsAllowedFilename(filename)) return;
        var dir = GetCameraDir(tenantId, cameraId);
        var path = Path.Combine(dir, filename);

        // 임시 파일에 쓴 뒤 원자적 rename → 읽기 중 충돌 방지
        var tmp = path + ".tmp";
        await using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            await data.CopyToAsync(fs);
        }

        File.Move(tmp, path, overwrite: true);

        if (filename.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase))
            CleanupOldSegments(dir);
    }

    public (Stream? Stream, string ContentType) GetFile(string tenantId, string cameraId, string filename)
    {
        if (!IsAllowedFilename(filename)) return (null, "");
        var path = Path.Combine(_root, Sanitize(tenantId), Sanitize(cameraId), filename);
        if (!File.Exists(path)) return (null, "");
        var ct = filename.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase)
            ? "application/vnd.apple.mpegurl"
            : "video/mp2t";
        try
        {
            // MemoryStream으로 읽어 파일 핸들 즉시 해제 → 쓰기와 동시 접근 허용
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            var memory = new MemoryStream((int)Math.Min(fs.Length, int.MaxValue));
            fs.CopyTo(memory);
            memory.Position = 0;
            return (memory, ct);
        }
        catch { return (null, ""); }
    }

    public void ClearCameraHls(string tenantId, string cameraId)
    {
        try
        {
            var dir = Path.Combine(_root, Sanitize(tenantId), Sanitize(cameraId));
            if (!Directory.Exists(dir)) return;
            foreach (var f in Directory.GetFiles(dir))
                try { File.Delete(f); } catch { }
        }
        catch { }
    }

    // 라이브 시청자가 네트워크 지연으로 뒤처져도 필요한 세그먼트를 받을 수 있도록
    // 개수와 시간을 모두 고려해서 완만하게 정리합니다.
    private static void CleanupOldSegments(string dir)
    {
        try
        {
            var thresholdUtc = DateTime.UtcNow - RetainedSegmentAge;
            var segments = Directory.GetFiles(dir, "*.ts")
                .OrderBy(File.GetLastWriteTimeUtc)
                .ToArray();

            foreach (var f in segments.Take(Math.Max(0, segments.Length - RetainedSegmentCount)))
            {
                try { File.Delete(f); } catch { }
            }

            foreach (var f in segments.Where(f => File.GetLastWriteTimeUtc(f) < thresholdUtc))
            {
                try { File.Delete(f); } catch { }
            }
        }
        catch { }
    }

    private static string Sanitize(string s) =>
        string.Concat(s.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_'));

    private static bool IsAllowedFilename(string f) =>
        f.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase) ||
        f.EndsWith(".ts", StringComparison.OrdinalIgnoreCase);
}
