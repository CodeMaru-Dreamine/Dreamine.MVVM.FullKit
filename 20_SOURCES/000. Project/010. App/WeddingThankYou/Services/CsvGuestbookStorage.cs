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
	/// \file CsvGuestbookStorage.cs
	/// \brief CSV 기반 방명록 저장소 구현. 슬러그별로 별도 CSV 파일에 저장합니다.
	/// \details 세마포어 잠금 + 임시파일 교체(원자적 저장)로 데이터 안전성을 확보합니다.
	/// </summary>
	public sealed class CsvGuestbookStorage : IGuestbookStorage
	{
		private readonly string _dataDir;
		private static readonly SemaphoreSlim _gate = new(1, 1);

		/// <summary>
		/// \brief 생성자.
		/// </summary>
		/// <param name="env">호스트 환경(컨텐츠 루트 경로 계산).</param>
		public CsvGuestbookStorage(IHostEnvironment env)
		{
			_dataDir = Path.Combine(env.ContentRootPath, "App_Data", "Guestbook");
			Directory.CreateDirectory(_dataDir);
		}

		private string CsvPath(string slug) =>
			Path.Combine(_dataDir, $"{Sanitize(slug)}.csv");

		private static string Sanitize(string slug) =>
			string.Concat((slug ?? string.Empty).ToLowerInvariant().Split(Path.GetInvalidFileNameChars()));

		/// <inheritdoc/>
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

		/// <inheritdoc/>
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
		/// \brief CSV 헤더 여부를 대략 판별합니다.
		/// </summary>
		private static bool IsHeader(string line)
		{
			var l = line.Trim().ToLowerInvariant();
			return l.StartsWith("name,contact,message,createdlocal");
		}

		/// <summary>
		/// \brief CSV 필드 이스케이프(RFC4180 준수).
		/// </summary>
		private static string EscapeCsv(string value)
		{
			value ??= string.Empty;
			bool needQuote = value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
			if (!needQuote) return value;
			return $"\"{value.Replace("\"", "\"\"")}\"";
		}

		/// <summary>
		/// \brief CSV 한 줄 파싱(따옴표, 이스케이프 처리).
		/// </summary>
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
