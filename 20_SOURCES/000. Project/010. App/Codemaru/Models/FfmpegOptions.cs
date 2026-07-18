/*!
 * \file FfmpegOptions.cs
 * \brief 카메라별 개별 스트림 설정.
 */

namespace Codemaru.Models
{
	/// <summary>
	/// \if KO
	/// <para>\brief ffmpeg 실행 및 HLS 공통 설정.</para>
	/// \endif
	/// \if EN
	/// <para>Encapsulates ffmpeg options functionality and related state.</para>
	/// \endif
	/// </summary>
	public sealed class FfmpegOptions
	{
		/// <summary>
		/// \if KO
		/// <para>\brief ffmpeg.exe 절대경로.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the path value.</para>
		/// \endif
		/// </summary>
		public string Path { get; set; } = @"C:\ffmpeg\bin\ffmpeg.exe";

		/// <summary>
		/// \if KO
		/// <para>\brief 출력 루트(상대면 ContentRoot 기준).</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the output root value.</para>
		/// \endif
		/// </summary>
		public string OutputRoot { get; set; } = @"wwwroot\hls";

		/// <summary>
		/// \if KO
		/// <para>\brief HLS 세그먼트 길이(초).</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the hls segment seconds value.</para>
		/// \endif
		/// </summary>
		public int HlsSegmentSeconds { get; set; } = 2;

		/// <summary>
		/// \if KO
		/// <para>\brief m3u8에 유지할 세그먼트 개수.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the hls list size value.</para>
		/// \endif
		/// </summary>
		public int HlsListSize { get; set; } = 6;

		/// <summary>
		/// \if KO
		/// <para>\brief 워치독 체크 주기(초).</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the watchdog interval seconds value.</para>
		/// \endif
		/// </summary>
		public int WatchdogIntervalSeconds { get; set; } = 2;

		/// <summary>
		/// \if KO
		/// <para>\brief 갱신 정지로 판단할 유휴 시간(초). 0이면 hls_time*list_size 사용.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the watchdog idle seconds value.</para>
		/// \endif
		/// </summary>
		public int WatchdogIdleSeconds { get; set; } = 0;

		/// <summary>
		/// \if KO
		/// <para>\brief 기본 비디오 코덱("libx264" 또는 "copy").</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the video codec value.</para>
		/// \endif
		/// </summary>
		public string VideoCodec { get; set; } = "libx264";

		/// <summary>
		/// \if KO
		/// <para>\brief 기본 오디오 코덱("an"=무음, "aac" 등).</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the audio codec value.</para>
		/// \endif
		/// </summary>
		public string AudioCodec { get; set; } = "an";

		/// <summary>
		/// \if KO
		/// <para>\brief 오디오 비트레이트(예: "96k").</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the audio bitrate value.</para>
		/// \endif
		/// </summary>
		public string AudioBitrate { get; set; } = "96k";

		/// <summary>
		/// \if KO
		/// <para>\brief 오디오 샘플레이트.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the audio rate value.</para>
		/// \endif
		/// </summary>
		public int AudioRate { get; set; } = 48000;

		/// <summary>
		/// \if KO
		/// <para>\brief 오디오 채널 수.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the audio channels value.</para>
		/// \endif
		/// </summary>
		public int AudioChannels { get; set; } = 2;
	}

	/// <summary>
	/// \if KO
	/// <para>\brief 개별 스트림 설정(front/back 등).</para>
	/// \endif
	/// \if EN
	/// <para>Encapsulates stream config functionality and related state.</para>
	/// \endif
	/// </summary>
	public sealed class StreamConfig
	{
		/// <summary>
		/// \if KO
		/// <para>\brief 스트림 식별자(폴더명으로 사용).</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the name value.</para>
		/// \endif
		/// </summary>
		public string Name { get; set; } = "front";

		/// <summary>
		/// \if KO
		/// <para>\brief RTSP 주소(계정/암호 포함).</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the rtsp url value.</para>
		/// \endif
		/// </summary>
		public string RtspUrl { get; set; } = "";

		/// <summary>
		/// \if KO
		/// <para>\brief 비디오 코덱("copy" 또는 "libx264"). null이면 글로벌 옵션 사용.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the video codec value.</para>
		/// \endif
		/// </summary>
		public string? VideoCodec { get; set; }

		/// <summary>
		/// \if KO
		/// <para>\brief 오디오 코덱("an" 또는 "aac" 등). null이면 글로벌 옵션 사용.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the audio codec value.</para>
		/// \endif
		/// </summary>
		public string? AudioCodec { get; set; }
	}
}
