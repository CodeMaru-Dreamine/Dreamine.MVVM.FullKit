using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using WeddingThankYou.Models;

namespace WeddingThankYou.Services
{
	/// <summary>
	/// \if KO
	/// <para>\file CsvGuestbookStorage.cs \brief CSV 기반 방명록 저장소 구현. 슬러그별로 별도 CSV 파일에 저장합니다. \details 세마포어 잠금 + 임시파일 교체(원자적 저장)로 데이터 안전성을 확보합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Encapsulates csv guestbook storage functionality and related state.</para>
	/// \endif
	/// </summary>
	public sealed class CsvGuestbookStorage : IGuestbookStorage
	{
		/// <summary>
		/// \if KO
		/// <para>data Dir 값을 보관합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores the data dir value.</para>
		/// \endif
		/// </summary>
		private readonly string _dataDir;
		/// <summary>
		/// \if KO
		/// <para>gate 값을 보관합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Stores the gate value.</para>
		/// \endif
		/// </summary>
		private static readonly SemaphoreSlim _gate = new(1, 1);

		/// <summary>
		/// \if KO
		/// <para>\brief 생성자.</para>
		/// \endif
		/// \if EN
		/// <para>Initializes a new instance of the <see cref="CsvGuestbookStorage"/> class with the specified settings.</para>
		/// \endif
		/// </summary>
		/// <param name="env">
		/// \if KO
		/// <para>호스트 환경(컨텐츠 루트 경로 계산).</para>
		/// \endif
		/// \if EN
		/// <para>The <c>IHostEnvironment</c> value used for env.</para>
		/// \endif
		/// </param>
		public CsvGuestbookStorage(IHostEnvironment env)
		{
			_dataDir = Path.Combine(env.ContentRootPath, "App_Data", "Guestbook");
			Directory.CreateDirectory(_dataDir);
		}

		/// <summary>
		/// \if KO
		/// <para>Csv Path 작업을 수행합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Performs the csv path operation.</para>
		/// \endif
		/// </summary>
		/// <param name="slug">
		/// \if KO
		/// <para>slug에 사용할 <c>string</c> 값입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The <c>string</c> value used for slug.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>Csv Path 작업에서 생성한 <c>string</c> 결과입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The <c>string</c> result produced by the csv path operation.</para>
		/// \endif
		/// </returns>
		private string CsvPath(string slug) =>
			Path.Combine(_dataDir, $"{Sanitize(slug)}.csv");

		/// <summary>
		/// \if KO
		/// <para>Sanitize 작업을 수행합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Performs the sanitize operation.</para>
		/// \endif
		/// </summary>
		/// <param name="slug">
		/// \if KO
		/// <para>slug에 사용할 <c>string</c> 값입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The <c>string</c> value used for slug.</para>
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
		private static string Sanitize(string slug) =>
			string.Concat((slug ?? string.Empty).ToLowerInvariant().Split(Path.GetInvalidFileNameChars()));

		/// <summary>
		/// \if KO
		/// <para>Async 데이터를 불러옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Loads async data.</para>
		/// \endif
		/// </summary>
		/// <param name="slug">
		/// \if KO
		/// <para>slug에 사용할 <c>string</c> 값입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The <c>string</c> value used for slug.</para>
		/// \endif
		/// </param>
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
		/// <para>Load Async 작업에서 생성한 <c>Task&lt;IReadOnlyList&lt;GuestbookEntry&gt;&gt;</c> 결과입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The <c>Task&lt;IReadOnlyList&lt;GuestbookEntry&gt;&gt;</c> result produced by the load async operation.</para>
		/// \endif
		/// </returns>
		public async Task<IReadOnlyList<GuestbookEntry>> LoadAsync(string slug, CancellationToken ct = default)
		{
			var csvPath = CsvPath(slug);
			await _gate.WaitAsync(ct).ConfigureAwait(false);
			try
			{
				var result = new List<GuestbookEntry>();
				if (!File.Exists(csvPath)) return result;

				using var fs = new FileStream(csvPath, FileMode.Open, FileAccess.Read, FileShare.Read);
				using var sr = new StreamReader(fs, Encoding.UTF8);
				string? line;

				// 헤더 지원: 첫 줄이 헤더 같으면 스킵
				bool first = true;
				while ((line = await sr.ReadLineAsync().ConfigureAwait(false)) != null)
				{
					ct.ThrowIfCancellationRequested();
					if (string.IsNullOrWhiteSpace(line)) continue;

					if (first && IsHeader(line)) { first = false; continue; }
					first = false;

					var f = ParseCsvLine(line);
					if (f.Count < 4) continue;

					if (!DateTime.TryParseExact(f[3], "yyyy-MM-dd HH:mm:ss",
							CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var when))
						when = DateTime.Now;

					result.Add(new GuestbookEntry
					{
						Name = f[0],
						Contact = f[1],
						Message = f[2],
						CreatedLocal = when
					});
				}
				return result;
			}
			finally
			{
				_gate.Release();
			}
		}

		/// <summary>
		/// \if KO
		/// <para>Async 데이터를 저장합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Saves async data.</para>
		/// \endif
		/// </summary>
		/// <param name="slug">
		/// \if KO
		/// <para>slug에 사용할 <c>string</c> 값입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The <c>string</c> value used for slug.</para>
		/// \endif
		/// </param>
		/// <param name="entries">
		/// \if KO
		/// <para>entries에 사용할 <c>IReadOnlyList&lt;GuestbookEntry&gt;</c> 값입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The <c>IReadOnlyList&lt;GuestbookEntry&gt;</c> value used for entries.</para>
		/// \endif
		/// </param>
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
		/// <para>Save Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The <c>Task</c> result produced by the save async operation.</para>
		/// \endif
		/// </returns>
		public async Task SaveAsync(string slug, IReadOnlyList<GuestbookEntry> entries, CancellationToken ct = default)
		{
			var csvPath = CsvPath(slug);
			await _gate.WaitAsync(ct).ConfigureAwait(false);
			try
			{
				var tmp = csvPath + ".tmp";
				using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
				using (var sw = new StreamWriter(fs, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
				{
					// 헤더 작성
					await sw.WriteLineAsync("Name,Contact,Message,CreatedLocal").ConfigureAwait(false);

					foreach (var e in entries)
					{
						ct.ThrowIfCancellationRequested();
						var line = string.Join(",",
							EscapeCsv(e.Name),
							EscapeCsv(e.Contact),
							EscapeCsv(e.Message),
							EscapeCsv(e.CreatedLocal.ToString("yyyy-MM-dd HH:mm:ss")));
						await sw.WriteLineAsync(line).ConfigureAwait(false);
					}
				}

				File.Copy(tmp, csvPath, overwrite: true);
				File.Delete(tmp);
			}
			finally
			{
				_gate.Release();
			}
		}

		/// <summary>
		/// \if KO
		/// <para>\brief CSV 헤더 여부를 대략 판별합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Determines whether is header.</para>
		/// \endif
		/// </summary>
		/// <param name="line">
		/// \if KO
		/// <para>line에 사용할 <c>string</c> 값입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The <c>string</c> value used for line.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>Is Header 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
		/// \endif
		/// \if EN
		/// <para><see langword="true"/> when the is header condition is satisfied; otherwise, <see langword="false"/>.</para>
		/// \endif
		/// </returns>
		private static bool IsHeader(string line)
		{
			var l = line.Trim().ToLowerInvariant();
			return l.StartsWith("name,contact,message,createdlocal");
		}

		/// <summary>
		/// \if KO
		/// <para>\brief CSV 필드 이스케이프(RFC4180 준수).</para>
		/// \endif
		/// \if EN
		/// <para>Performs the escape csv operation.</para>
		/// \endif
		/// </summary>
		/// <param name="value">
		/// \if KO
		/// <para>적용할 값입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The value to apply.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>Escape Csv 작업에서 생성한 <c>string</c> 결과입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The <c>string</c> result produced by the escape csv operation.</para>
		/// \endif
		/// </returns>
		private static string EscapeCsv(string value)
		{
			value ??= string.Empty;
			bool needQuote = value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
			if (!needQuote) return value;
			return $"\"{value.Replace("\"", "\"\"")}\"";
		}

		/// <summary>
		/// \if KO
		/// <para>\brief CSV 한 줄 파싱(따옴표, 이스케이프 처리).</para>
		/// \endif
		/// \if EN
		/// <para>Performs the parse csv line operation.</para>
		/// \endif
		/// </summary>
		/// <param name="line">
		/// \if KO
		/// <para>line에 사용할 <c>string</c> 값입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The <c>string</c> value used for line.</para>
		/// \endif
		/// </param>
		/// <returns>
		/// \if KO
		/// <para>Parse Csv Line 작업에서 생성한 <c>List&lt;string&gt;</c> 결과입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The <c>List&lt;string&gt;</c> result produced by the parse csv line operation.</para>
		/// \endif
		/// </returns>
		private static List<string> ParseCsvLine(string line)
		{
			var list = new List<string>();
			if (string.IsNullOrEmpty(line)) return list;

			var sb = new StringBuilder();
			bool inQuotes = false;

			for (int i = 0; i < line.Length; i++)
			{
				var ch = line[i];
				if (inQuotes)
				{
					if (ch == '"')
					{
						if (i + 1 < line.Length && line[i + 1] == '"')
						{
							sb.Append('"'); i++; // "" -> "
						}
						else inQuotes = false;
					}
					else sb.Append(ch);
				}
				else
				{
					if (ch == ',') { list.Add(sb.ToString()); sb.Clear(); }
					else if (ch == '"') inQuotes = true;
					else sb.Append(ch);
				}
			}
			list.Add(sb.ToString());
			return list;
		}
	}
}
