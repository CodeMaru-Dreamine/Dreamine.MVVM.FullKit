namespace DreamineVMS.Options;

/// <summary>
/// \brief FFmpeg 기반 RTSP to HLS 변환 옵션입니다.
/// </summary>
public sealed class FfmpegOptions
{
    /// <summary>\brief ffmpeg.exe 경로입니다.</summary>
    public string Path { get; set; } = @"C:\ffmpeg\bin\ffmpeg.exe";

    /// <summary>\brief HLS 출력 루트입니다. 상대 경로이면 애플리케이션 기준으로 처리합니다.</summary>
    public string OutputRoot { get; set; } = @"wwwroot\hls";

    /// <summary>\brief 애플리케이션 시작 시 HLS 변환을 자동 시작할지 여부입니다.</summary>
    public bool StartOnApplicationStartup { get; set; }

    /// <summary>\brief HLS 세그먼트 길이입니다.</summary>
    public int HlsSegmentSeconds { get; set; } = 1;

    /// <summary>\brief HLS 플레이리스트에 유지할 세그먼트 개수입니다.</summary>
    public int HlsListSize { get; set; } = 4;

    /// <summary>\brief Watchdog 검사 주기입니다.</summary>
    public int WatchdogIntervalSeconds { get; set; } = 2;

    /// <summary>\brief m3u8 갱신 정지 판단 시간입니다. 0이면 세그먼트 설정 기준으로 계산합니다.</summary>
    public int WatchdogIdleSeconds { get; set; }

    /// <summary>\brief 재시작 대기 시간입니다.</summary>
    public int RestartDelaySeconds { get; set; } = 2;

    /// <summary>\brief 비디오 코덱입니다. copy 또는 libx264를 사용합니다.</summary>
    public string VideoCodec { get; set; } = "copy";

    /// <summary>\brief 오디오 코덱입니다. an이면 오디오를 제거합니다.</summary>
    public string AudioCodec { get; set; } = "an";

    /// <summary>\brief 오디오 비트레이트입니다.</summary>
    public string AudioBitrate { get; set; } = "32k";

    /// <summary>\brief 오디오 샘플레이트입니다.</summary>
    public int AudioRate { get; set; } = 22050;

    /// <summary>\brief 오디오 채널 수입니다.</summary>
    public int AudioChannels { get; set; } = 1;
}
