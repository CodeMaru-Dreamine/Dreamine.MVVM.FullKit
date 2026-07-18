/*!
 * \file FfmpegHlsService.cs
 * \brief RTSP→HLS를 ffmpeg로 수행하는 백그라운드 서비스(트랜스코딩/복사 분기, 워치독 포함).
 * \details
 *  - 스트림별 ffmpeg 프로세스 관리(시작/감시/재시작).
 *  - 트랜스코딩 시 GOP 고정 및 강제 키프레임으로 세그먼트 생성 보장.
 *  - 코덱 복사 시 AnnexB 비트스트림 필터 적용.
 *  - HLS 공통 옵션: delete_segments + program_date_time + independent_segments.
 *  - index.m3u8 갱신 정지 감지(워치독) 후 자동 재시작.
 */

using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Hosting; // IWebHostEnvironment
using Codemaru.Models;

namespace Codemaru.Services
{
	/// <summary>
	/// \if KO
	/// <para>\brief ffmpeg 프로세스를 관리하는 HostedService(워치독 포함).</para>
	/// \endif
	/// \if EN
	/// <para>Encapsulates ffmpeg hls service functionality and related state.</para>
	/// \endif
	/// </summary>
	public sealed class FfmpegHlsService : BackgroundService
	{
		/// <summary>
		/// \if KO
		/// <para>log 값을 보관합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores the log value.</para>
		/// \endif
		/// </summary>
		private readonly ILogger<FfmpegHlsService> _log;
		/// <summary>
		/// \if KO
		/// <para>opt 값을 보관합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores the opt value.</para>
		/// \endif
		/// </summary>
		private readonly FfmpegOptions _opt;
		/// <summary>
		/// \if KO
		/// <para>streams 값을 보관합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores the streams value.</para>
		/// \endif
		/// </summary>
		private readonly IReadOnlyList<StreamConfig> _streams;
		/// <summary>
		/// \if KO
		/// <para>env 값을 보관합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores the env value.</para>
		/// \endif
		/// </summary>
		private readonly IWebHostEnvironment _env;

		// 스트림명 → ffmpeg 프로세스
		/// <summary>
		/// \if KO
		/// <para>procs 값을 보관합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores the procs value.</para>
		/// \endif
		/// </summary>
		private readonly Dictionary<string, Process> _procs = new();

		// 스트림명 → 최근 m3u8 시퀀스/타임스탬프
		/// <summary>
		/// \if KO
		/// <para>last Seq 값을 보관합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores the last seq value.</para>
		/// \endif
		/// </summary>
		private readonly Dictionary<string, long> _lastSeq = new();
		/// <summary>
		/// \if KO
		/// <para>last Write 값을 보관합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores the last write value.</para>
		/// \endif
		/// </summary>
		private readonly Dictionary<string, DateTime> _lastWrite = new();

		/// <summary>
		/// \if KO
		/// <para>Media Seq Regex 값을 보관합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores the media seq regex value.</para>
		/// \endif
		/// </summary>
		private static readonly Regex MediaSeqRegex = new(@"#EXT-X-MEDIA-SEQUENCE:(\d+)", RegexOptions.Compiled);

		/// <summary>
		/// \if KO
		/// <para>\brief 생성자.</para>
		/// \endif
		/// \if EN
		/// <para>Initializes a new instance of the <see cref="FfmpegHlsService"/> class with the specified settings.</para>
		/// \endif
		/// </summary>
		/// <param name="log">
		/// \if KO
		/// <para>log에 사용할 <c>ILogger&lt;FfmpegHlsService&gt;</c> 값입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The <c>ILogger&lt;FfmpegHlsService&gt;</c> value used for log.</para>
		/// \endif
		/// </param>
		/// <param name="opt">
		/// \if KO
		/// <para>opt에 사용할 <c>IOptions&lt;FfmpegOptions&gt;</c> 값입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The <c>IOptions&lt;FfmpegOptions&gt;</c> value used for opt.</para>
		/// \endif
		/// </param>
		/// <param name="streams">
		/// \if KO
		/// <para>streams에 사용할 <c>IOptions&lt;List&lt;StreamConfig&gt;&gt;</c> 값입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The <c>IOptions&lt;List&lt;StreamConfig&gt;&gt;</c> value used for streams.</para>
		/// \endif
		/// </param>
		/// <param name="env">
		/// \if KO
		/// <para>env에 사용할 <c>IWebHostEnvironment</c> 값입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The <c>IWebHostEnvironment</c> value used for env.</para>
		/// \endif
		/// </param>
		public FfmpegHlsService(
			ILogger<FfmpegHlsService> log,
			IOptions<FfmpegOptions> opt,
			IOptions<List<StreamConfig>> streams,
			IWebHostEnvironment env)
		{
			_log = log;
			_opt = opt.Value;
			_streams = streams.Value;
			_env = env;
		}

		/// <summary>
		/// \if KO
		/// <para>\brief 서비스 시작 시 각 스트림 ffmpeg 실행 + 워치독 루프 가동.</para>
		/// \endif
		/// \if EN
		/// <para>Performs the execute async operation.</para>
		/// \endif
		/// </summary>
		/// <param name="ct">
		/// \if KO
		/// <para>취소 요청을 감시하는 토큰입니다.</para>
		/// \endif
		/// \if EN
		/// <para>A token used to observe cancellation requests.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>Execute Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The <c>Task</c> result produced by the execute async operation.</para>
		/// \endif
		/// </returns>
		protected override async Task ExecuteAsync(CancellationToken ct)
		{
			Directory.CreateDirectory(GetOutputRoot());

			foreach (var s in _streams)
			{
				TryStartOne(s);
				InitWatchStats(s);
			}

			// 주기적으로 프로세스 상태와 m3u8 갱신을 점검
			while (!ct.IsCancellationRequested)
			{
				foreach (var s in _streams)
				{
					// 1) ffmpeg 죽었으면 재시작
					if (!_procs.TryGetValue(s.Name, out var p) || p.HasExited)
					{
						_log.LogWarning("ffmpeg 재시작(프로세스 종료 감지): {Name}", s.Name);
						TryStartOne(s);
						InitWatchStats(s);
						continue;
					}

					// 2) m3u8 갱신 감시(워치독)
					var (m3u8Path, _) = GetOutputPaths(s);
					if (File.Exists(m3u8Path))
					{
						var (seq, write) = ReadSeqAndTime(m3u8Path);
						var lastSeq = _lastSeq[s.Name];
						var lastWrite = _lastWrite[s.Name];

						// 시퀀스 증가 또는 수정 시간이 갱신되면 정상
						if (seq > lastSeq || write > lastWrite)
						{
							_lastSeq[s.Name] = Math.Max(seq, lastSeq);
							_lastWrite[s.Name] = write > lastWrite ? write : lastWrite;
						}
						else
						{
							// 일정 시간 이상 변화가 없으면 재시작
							var idleSec = (DateTime.UtcNow - lastWrite.ToUniversalTime()).TotalSeconds;
							var idleLimit = Math.Max(_opt.WatchdogIdleSeconds, _opt.HlsSegmentSeconds * _opt.HlsListSize);
							if (idleSec >= idleLimit)
							{
								_log.LogWarning("m3u8 갱신 정지 감지({Idle}s >= {Limit}s): {Name} → ffmpeg 재시작",
									idleSec, idleLimit, s.Name);
								SafeRestart(s);
								InitWatchStats(s);
								continue;
							}
						}
					}
					else
					{
						// 파일이 없으면 ffmpeg가 아직 시작 중이거나 실패→ 잠시 후 다시 확인
						_log.LogDebug("m3u8 미존재: {Name} ({Path})", s.Name, m3u8Path);
					}
				}

				await Task.Delay(TimeSpan.FromSeconds(_opt.WatchdogIntervalSeconds), ct);
			}
		}

		/// <summary>
		/// \if KO
		/// <para>\brief 서비스 중지 시 모든 ffmpeg 종료.</para>
		/// \endif
		/// \if EN
		/// <para>Performs the stop async operation.</para>
		/// \endif
		/// </summary>
		/// <param name="cancellationToken">
		/// \if KO
		/// <para>취소 요청을 감시하는 토큰입니다.</para>
		/// \endif
		/// \if EN
		/// <para>A token used to observe cancellation requests.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>Stop Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The <c>Task</c> result produced by the stop async operation.</para>
		/// \endif
		/// </returns>
		public override Task StopAsync(CancellationToken cancellationToken)
		{
			foreach (var p in _procs.Values)
			{
				try
				{
					if (!p.HasExited)
					{
						p.Kill(entireProcessTree: true);
						p.WaitForExit(2000);
					}
				}
				catch { /* ignore */ }
			}
			_procs.Clear();
			return Task.CompletedTask;
		}

		/// <summary>
		/// \if KO
		/// <para>\brief HLS 출력 루트의 절대경로를 반환합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets the output root value.</para>
		/// \endif
		/// </summary>
		/// <returns>
		/// \if KO
		/// <para>Get Output Root 작업에서 생성한 <c>string</c> 결과입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The <c>string</c> result produced by the get output root operation.</para>
		/// \endif
		/// </returns>
		private string GetOutputRoot()
		{
			// appsettings에 상대경로("wwwroot\\hls")인 경우 ContentRoot 기준으로 절대경로화
			return Path.IsPathRooted(_opt.OutputRoot)
				? _opt.OutputRoot
				: Path.Combine(_env.ContentRootPath, _opt.OutputRoot);
		}

		/// <summary>
		/// \if KO
		/// <para>\brief 스트림별 m3u8/세그먼트 경로를 반환합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets the output paths value.</para>
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
		/// <para>Get Output Paths 작업에서 생성한 <c>(string m3u8Path, string segFmt)</c> 결과입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The <c>(string m3u8Path, string segFmt)</c> result produced by the get output paths operation.</para>
		/// \endif
		/// </returns>
		private (string m3u8Path, string segFmt) GetOutputPaths(StreamConfig s)
		{
			var outRoot = GetOutputRoot();
			var outDir = Path.Combine(outRoot, s.Name);
			Directory.CreateDirectory(outDir);

			// 가독성 및 캐싱 안전성 위해 seg_%05d.ts 권장
			var m3u8Path = Path.Combine(outDir, "index.m3u8");
			var segFmt = Path.Combine(outDir, "seg_%05d.ts");
			return (m3u8Path, segFmt);
		}

		/// <summary>
		/// \if KO
		/// <para>\brief 한 개 스트림 ffmpeg 시작(이미 있으면 종료 후 재시작).</para>
		/// \endif
		/// \if EN
		/// <para>Performs the safe restart operation.</para>
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
		private void SafeRestart(StreamConfig s)
		{
			if (_procs.TryGetValue(s.Name, out var old))
			{
				try
				{
					if (!old.HasExited)
					{
						old.Kill(entireProcessTree: true);
						old.WaitForExit(2000);
					}
				}
				catch { /* ignore */ }
			}
			TryStartOne(s);
		}

		/// <summary>
		/// \if KO
		/// <para>\brief ffmpeg 시작 시도(성공 시 딕셔너리 등록).</para>
		/// \endif
		/// \if EN
		/// <para>Attempts to start one and returns whether the operation succeeds.</para>
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
		private void TryStartOne(StreamConfig s)
		{
			var (m3u8Path, segFmt) = GetOutputPaths(s);

			// 입력 옵션: TCP 강제(안정), 타임스탬프 보정
			var inputOpts = "-rtsp_transport tcp -rtsp_flags prefer_tcp -fflags +genpts -use_wallclock_as_timestamps 1";

			// 인코딩/복사 분기
			var v = (s.VideoCodec ?? _opt.VideoCodec).ToLowerInvariant();
			var a = (s.AudioCodec ?? _opt.AudioCodec).ToLowerInvariant();

			// 비디오 파트
			// - 트랜스코딩: GOP 고정 (hls_time 2초라면 gop≈50@25fps / 60@30fps 등으로 맞추세요)
			var videoPart =
				v == "copy"
				? "-c:v copy -bsf:v h264_mp4toannexb"
				: "-c:v libx264 -preset veryfast -tune zerolatency -profile:v high -level 4.1 -pix_fmt yuv420p " +
				  "-g 50 -keyint_min 50 -sc_threshold 0 -force_key_frames \"expr:gte(t,n_forced*2)\"";

			// 오디오 파트
			var audioPart =
				a == "an" ? "-an"
				: $"-c:a {a} -b:a {_opt.AudioBitrate} -ar {_opt.AudioRate} -ac {_opt.AudioChannels}";

			// HLS 파트(독립 세그먼트 + 프로그램 시간 + 삭제)
			var hlsPart = string.Join(' ', new[]
			{
				"-f hls",
				$"-hls_time {_opt.HlsSegmentSeconds}",
				$"-hls_list_size {_opt.HlsListSize}",
				"-hls_flags delete_segments+program_date_time+independent_segments",
				$"-hls_segment_filename \"{segFmt}\"",
				$"\"{m3u8Path}\""
			});

			// 전체 인자 조합
			var args = string.Join(' ', new[]
			{
				inputOpts,
				$"-i \"{s.RtspUrl}\"",
				videoPart,
				audioPart,
				hlsPart
			});

			var psi = new ProcessStartInfo
			{
				FileName = _opt.Path,
				Arguments = args,
				UseShellExecute = false,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				CreateNoWindow = true
			};

			var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
			proc.ErrorDataReceived += (_, e) =>
			{
				if (!string.IsNullOrEmpty(e.Data))
					_log.LogInformation("[{name}] {line}", s.Name, e.Data);
			};
			proc.OutputDataReceived += (_, e) =>
			{
				if (!string.IsNullOrEmpty(e.Data))
					_log.LogDebug("[{name}] {line}", s.Name, e.Data);
			};
			proc.Exited += (_, __) =>
			{
				_log.LogWarning("ffmpeg 종료 감지: {Name} (ExitCode={Code})", s.Name, proc.ExitCode);
			};

			if (proc.Start())
			{
				proc.BeginErrorReadLine();
				proc.BeginOutputReadLine();
				_procs[s.Name] = proc;
				_log.LogInformation("ffmpeg 시작: {Name} → {Out}", s.Name, Path.GetDirectoryName(m3u8Path));
			}
			else
			{
				_log.LogError("ffmpeg 시작 실패: {Name}", s.Name);
			}
		}

		/// <summary>
		/// \if KO
		/// <para>\brief 워치독 초기 통계값을 리셋합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Performs the init watch stats operation.</para>
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
		private void InitWatchStats(StreamConfig s)
		{
			_lastSeq[s.Name] = -1;
			_lastWrite[s.Name] = DateTime.MinValue;
		}

		/// <summary>
		/// \if KO
		/// <para>\brief m3u8에서 MEDIA-SEQUENCE와 파일 수정시각을 읽습니다. \return (seq, lastWrite)</para>
		/// \endif
		/// \if EN
		/// <para>Reads seq and time data.</para>
		/// \endif
		/// </summary>
		/// <param name="m3u8Path">
		/// \if KO
		/// <para>m3u8 Path에 사용할 <c>string</c> 값입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The <c>string</c> value used for m3u8 path.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>Read Seq And Time 작업에서 생성한 <c>(long seq, DateTime lastWrite)</c> 결과입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The <c>(long seq, DateTime lastWrite)</c> result produced by the read seq and time operation.</para>
		/// \endif
		/// </returns>
		private (long seq, DateTime lastWrite) ReadSeqAndTime(string m3u8Path)
		{
			try
			{
				var text = File.ReadAllText(m3u8Path);
				var m = MediaSeqRegex.Match(text);
				var seq = m.Success ? long.Parse(m.Groups[1].Value) : -1;
				var info = new FileInfo(m3u8Path);
				return (seq, info.LastWriteTimeUtc);
			}
			catch (Exception ex)
			{
				_log.LogDebug("m3u8 읽기 실패: {Path} - {Msg}", m3u8Path, ex.Message);
				return (-1, DateTime.MinValue);
			}
		}
	}
}
