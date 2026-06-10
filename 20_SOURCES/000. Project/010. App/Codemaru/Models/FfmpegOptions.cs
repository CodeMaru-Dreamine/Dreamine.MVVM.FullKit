/*!
 * \file StreamConfig.cs
 * \brief 카메라별 개별 스트림 설정.
 */

namespace Codemaru.Models
{
	/// <summary>\brief ffmpeg 실행 및 HLS 공통 설정.</summary>
	public sealed class FfmpegOptions
	{
		/// <summary>\brief ffmpeg.exe 절대경로.</summary>
		public string Path { get; set; } = @"C:\ffmpeg\bin\ffmpeg.exe";

		/// <summary>\brief 출력 루트(상대면 ContentRoot 기준).</summary>
		public string OutputRoot { get; set; } = @"wwwroot\hls";

		/// <summary>\brief HLS 세그먼트 길이(초).</summary>
		public int HlsSegmentSeconds { get; set; } = 2;

		/// <summary>\brief m3u8에 유지할 세그먼트 개수.</summary>
		public int HlsListSize { get; set; } = 6;

		/// <summary>\brief 워치독 체크 주기(초).</summary>
		public int WatchdogIntervalSeconds { get; set; } = 2;

		/// <summary>\brief 갱신 정지로 판단할 유휴 시간(초). 0이면 hls_time*list_size 사용.</summary>
		public int WatchdogIdleSeconds { get; set; } = 0;

		/// <summary>\brief 기본 비디오 코덱("libx264" 또는 "copy").</summary>
		public string VideoCodec { get; set; } = "libx264";

		/// <summary>\brief 기본 오디오 코덱("an"=무음, "aac" 등).</summary>
		public string AudioCodec { get; set; } = "an";

		/// <summary>\brief 오디오 비트레이트(예: "96k").</summary>
		public string AudioBitrate { get; set; } = "96k";

		/// <summary>\brief 오디오 샘플레이트.</summary>
		public int AudioRate { get; set; } = 48000;

		/// <summary>\brief 오디오 채널 수.</summary>
		public int AudioChannels { get; set; } = 2;
	}

	/// <summary>\brief 개별 스트림 설정(front/back 등).</summary>
	public sealed class StreamConfig
	{
		/// <summary>\brief 스트림 식별자(폴더명으로 사용).</summary>
		public string Name { get; set; } = "front";

		/// <summary>\brief RTSP 주소(계정/암호 포함).</summary>
		public string RtspUrl { get; set; } = "";

		/// <summary>\brief 비디오 코덱("copy" 또는 "libx264"). null이면 글로벌 옵션 사용.</summary>
		public string? VideoCodec { get; set; }

		/// <summary>\brief 오디오 코덱("an" 또는 "aac" 등). null이면 글로벌 옵션 사용.</summary>
		public string? AudioCodec { get; set; }
	}
}
