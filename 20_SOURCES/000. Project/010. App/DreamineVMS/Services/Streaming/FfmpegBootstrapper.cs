using System.IO;
using System.IO.Compression;
using System.Net.Http;

namespace DreamineVMS.Services.Streaming;

/// <summary>
/// \brief FFmpeg 실행 파일이 없을 경우 GitHub에서 자동으로 다운로드합니다.
/// </summary>
public static class FfmpegBootstrapper
{
    private const string ReleaseUrl =
        "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip";

    /// <summary>
    /// \brief FFmpeg 경로를 확인하고, 없으면 다운로드한 후 최종 경로를 반환합니다.
    /// </summary>
    public static async Task<string> EnsureAsync(string configuredPath, IProgress<string>? progress = null)
    {
        // 1. 설정 경로에 존재
        if (File.Exists(configuredPath))
            return configuredPath;

        // 2. 앱 디렉토리 내 ffmpeg\ffmpeg.exe
        var localPath = Path.Combine(AppContext.BaseDirectory, "ffmpeg", "ffmpeg.exe");
        if (File.Exists(localPath))
            return localPath;

        // 3. PATH 환경변수에서 검색
        foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
        {
            var candidate = Path.Combine(dir.Trim(), "ffmpeg.exe");
            if (File.Exists(candidate))
                return candidate;
        }

        // 4. GitHub에서 다운로드
        progress?.Report("FFmpeg를 찾을 수 없습니다. GitHub에서 자동 다운로드를 시작합니다...");

        var ffmpegDir = Path.Combine(AppContext.BaseDirectory, "ffmpeg");
        Directory.CreateDirectory(ffmpegDir);
        var zipPath = Path.Combine(ffmpegDir, "ffmpeg_download.zip");

        try
        {
            using var http = new HttpClient();
            http.Timeout = TimeSpan.FromMinutes(10);
            http.DefaultRequestHeaders.UserAgent.ParseAdd("DreamineVMS/1.0");

            progress?.Report("다운로드 중... (약 100 MB, 잠시 기다려 주세요)");

            using var response = await http.GetAsync(ReleaseUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            await using (var src = await response.Content.ReadAsStreamAsync())
            await using (var dst = File.Create(zipPath))
            {
                await src.CopyToAsync(dst);
            }

            progress?.Report("압축 해제 중...");

            using var zip = ZipFile.OpenRead(zipPath);
            var entry = zip.Entries.FirstOrDefault(e =>
                e.FullName.EndsWith("/bin/ffmpeg.exe", StringComparison.OrdinalIgnoreCase));

            if (entry is null)
                throw new FileNotFoundException("zip 파일 안에서 ffmpeg.exe를 찾지 못했습니다.");

            entry.ExtractToFile(localPath, overwrite: true);
            progress?.Report("FFmpeg 설치 완료.");
            return localPath;
        }
        finally
        {
            try { File.Delete(zipPath); } catch { /* ignore */ }
        }
    }
}
