namespace DreamineVMS.Options;

/// <summary>
/// \if KO
/// <para>\brief FFmpeg 기반 RTSP to HLS 변환 옵션입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates ffmpeg options functionality and related state.</para>
/// \endif
/// </summary>
public sealed class FfmpegOptions
{
    /// <summary>
    /// \if KO
    /// <para>\brief ffmpeg.exe 경로입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the path value.</para>
    /// \endif
    /// </summary>
    public string Path { get; set; } = @"C:\ffmpeg\bin\ffmpeg.exe";

    /// <summary>
    /// \if KO
    /// <para>\brief HLS 출력 루트입니다. 상대 경로이면 애플리케이션 기준으로 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the output root value.</para>
    /// \endif
    /// </summary>
    public string OutputRoot { get; set; } = @"wwwroot\hls";

    /// <summary>
    /// \if KO
    /// <para>\brief 애플리케이션 시작 시 HLS 변환을 자동 시작할지 여부입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the start on application startup value.</para>
    /// \endif
    /// </summary>
    public bool StartOnApplicationStartup { get; set; }

    /// <summary>
    /// \if KO
    /// <para>\brief HLS 세그먼트 길이입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the hls segment seconds value.</para>
    /// \endif
    /// </summary>
    public int HlsSegmentSeconds { get; set; } = 1;

    /// <summary>
    /// \if KO
    /// <para>\brief HLS 플레이리스트에 유지할 세그먼트 개수입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the hls list size value.</para>
    /// \endif
    /// </summary>
    public int HlsListSize { get; set; } = 4;

    /// <summary>
    /// \if KO
    /// <para>\brief Watchdog 검사 주기입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the watchdog interval seconds value.</para>
    /// \endif
    /// </summary>
    public int WatchdogIntervalSeconds { get; set; } = 2;

    /// <summary>
    /// \if KO
    /// <para>\brief m3u8 갱신 정지 판단 시간입니다. 0이면 세그먼트 설정 기준으로 계산합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the watchdog idle seconds value.</para>
    /// \endif
    /// </summary>
    public int WatchdogIdleSeconds { get; set; }

    /// <summary>
    /// \if KO
    /// <para>\brief 재시작 대기 시간입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the restart delay seconds value.</para>
    /// \endif
    /// </summary>
    public int RestartDelaySeconds { get; set; } = 2;

    /// <summary>
    /// \if KO
    /// <para>\brief 비디오 코덱입니다. copy 또는 libx264를 사용합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the video codec value.</para>
    /// \endif
    /// </summary>
    public string VideoCodec { get; set; } = "copy";

    /// <summary>
    /// \if KO
    /// <para>\brief 웹 HLS 출력 FPS입니다. 0 이하이면 FFmpeg 기본값을 사용합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the video fps value.</para>
    /// \endif
    /// </summary>
    public int VideoFps { get; set; } = 10;

    /// <summary>
    /// \if KO
    /// <para>\brief 웹 HLS 출력 최대 폭입니다. 0 이하이면 원본 해상도를 유지합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the video max width value.</para>
    /// \endif
    /// </summary>
    public int VideoMaxWidth { get; set; } = 960;

    /// <summary>
    /// \if KO
    /// <para>\brief 웹 HLS 비디오 목표 비트레이트입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the video bitrate value.</para>
    /// \endif
    /// </summary>
    public string VideoBitrate { get; set; } = "900k";

    /// <summary>
    /// \if KO
    /// <para>\brief 웹 HLS 비디오 최대 비트레이트입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the video max rate value.</para>
    /// \endif
    /// </summary>
    public string VideoMaxRate { get; set; } = "1200k";

    /// <summary>
    /// \if KO
    /// <para>\brief 웹 HLS 비디오 버퍼 크기입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the video buffer size value.</para>
    /// \endif
    /// </summary>
    public string VideoBufferSize { get; set; } = "1800k";

    /// <summary>
    /// \if KO
    /// <para>\brief 오디오 코덱입니다. an이면 오디오를 제거합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the audio codec value.</para>
    /// \endif
    /// </summary>
    public string AudioCodec { get; set; } = "an";

    /// <summary>
    /// \if KO
    /// <para>\brief 오디오 비트레이트입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the audio bitrate value.</para>
    /// \endif
    /// </summary>
    public string AudioBitrate { get; set; } = "32k";

    /// <summary>
    /// \if KO
    /// <para>\brief 오디오 샘플레이트입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the audio rate value.</para>
    /// \endif
    /// </summary>
    public int AudioRate { get; set; } = 22050;

    /// <summary>
    /// \if KO
    /// <para>\brief 오디오 채널 수입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the audio channels value.</para>
    /// \endif
    /// </summary>
    public int AudioChannels { get; set; } = 1;
}
