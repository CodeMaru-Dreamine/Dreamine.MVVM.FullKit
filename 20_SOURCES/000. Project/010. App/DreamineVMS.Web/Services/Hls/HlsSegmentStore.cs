using System.IO;

namespace DreamineVMS.Web.Services.Hls;

/// <summary>
/// \if KO
/// <para>에이전트가 푸시한 HLS 세그먼트를 파일로 저장하고 서빙합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates hls segment store functionality and related state.</para>
/// \endif
/// </summary>
public sealed class HlsSegmentStore
{
    /// <summary>
    /// \if KO
    /// <para>Retained Segment Count 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the retained segment count value.</para>
    /// \endif
    /// </summary>
    private const int RetainedSegmentCount = 45;
    /// <summary>
    /// \if KO
    /// <para>Retained Segment Age 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the retained segment age value.</para>
    /// \endif
    /// </summary>
    private static readonly TimeSpan RetainedSegmentAge = TimeSpan.FromMinutes(2);

    /// <summary>
    /// \if KO
    /// <para>root 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the root value.</para>
    /// \endif
    /// </summary>
    private readonly string _root;

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="HlsSegmentStore"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="HlsSegmentStore"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="env">
    /// \if KO
    /// <para>env에 사용할 <c>IWebHostEnvironment</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IWebHostEnvironment</c> value used for env.</para>
    /// \endif
    /// </param>
    public HlsSegmentStore(IWebHostEnvironment env)
    {
        _root = Path.Combine(env.ContentRootPath, "App_Data", "hls");
        Directory.CreateDirectory(_root);
    }

    /// <summary>
    /// \if KO
    /// <para>Camera Dir 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the camera dir value.</para>
    /// \endif
    /// </summary>
    /// <param name="tenantId">
    /// \if KO
    /// <para>tenant Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for tenant id.</para>
    /// \endif
    /// </param>
    /// <param name="cameraId">
    /// \if KO
    /// <para>camera Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for camera id.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get Camera Dir 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the get camera dir operation.</para>
    /// \endif
    /// </returns>
    public string GetCameraDir(string tenantId, string cameraId)
    {
        var dir = Path.Combine(_root, Sanitize(tenantId), Sanitize(cameraId));
        Directory.CreateDirectory(dir);
        return dir;
    }

    /// <summary>
    /// \if KO
    /// <para>Segment Async 데이터를 저장합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Saves segment async data.</para>
    /// \endif
    /// </summary>
    /// <param name="tenantId">
    /// \if KO
    /// <para>tenant Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for tenant id.</para>
    /// \endif
    /// </param>
    /// <param name="cameraId">
    /// \if KO
    /// <para>camera Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for camera id.</para>
    /// \endif
    /// </param>
    /// <param name="filename">
    /// \if KO
    /// <para>filename에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for filename.</para>
    /// \endif
    /// </param>
    /// <param name="data">
    /// \if KO
    /// <para>data에 사용할 <c>Stream</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Stream</c> value used for data.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Save Segment Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the save segment async operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>File 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the file value.</para>
    /// \endif
    /// </summary>
    /// <param name="tenantId">
    /// \if KO
    /// <para>tenant Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for tenant id.</para>
    /// \endif
    /// </param>
    /// <param name="cameraId">
    /// \if KO
    /// <para>camera Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for camera id.</para>
    /// \endif
    /// </param>
    /// <param name="filename">
    /// \if KO
    /// <para>filename에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for filename.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get File 작업에서 생성한 <c>(Stream? Stream, string ContentType)</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>(Stream? Stream, string ContentType)</c> result produced by the get file operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Clear Camera Hls 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the clear camera hls operation.</para>
    /// \endif
    /// </summary>
    /// <param name="tenantId">
    /// \if KO
    /// <para>tenant Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for tenant id.</para>
    /// \endif
    /// </param>
    /// <param name="cameraId">
    /// \if KO
    /// <para>camera Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for camera id.</para>
    /// \endif
    /// </param>
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
    /// <summary>
    /// \if KO
    /// <para>Cleanup Old Segments 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the cleanup old segments operation.</para>
    /// \endif
    /// </summary>
    /// <param name="dir">
    /// \if KO
    /// <para>dir에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for dir.</para>
    /// \endif
    /// </param>
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

    /// <summary>
    /// \if KO
    /// <para>Sanitize 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the sanitize operation.</para>
    /// \endif
    /// </summary>
    /// <param name="s">
    /// \if KO
    /// <para>이벤트를 발생시킨 객체입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The object that raised the event.</para>
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
    private static string Sanitize(string s) =>
        string.Concat(s.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_'));

    /// <summary>
    /// \if KO
    /// <para>Is Allowed Filename 조건을 확인합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Determines whether is allowed filename.</para>
    /// \endif
    /// </summary>
    /// <param name="f">
    /// \if KO
    /// <para>f에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for f.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Is Allowed Filename 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the is allowed filename condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    private static bool IsAllowedFilename(string f) =>
        f.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase) ||
        f.EndsWith(".ts", StringComparison.OrdinalIgnoreCase);
}
