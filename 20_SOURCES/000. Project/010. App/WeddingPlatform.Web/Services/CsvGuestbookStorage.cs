using System.Globalization;
using System.IO;
using System.Text;
using WeddingPlatform.Models;

namespace WeddingPlatform.Services;

/// <summary>
/// \if KO
/// <para>Csv Guestbook Storage 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates csv guestbook storage functionality and related state.</para>
/// \endif
/// </summary>
public sealed class CsvGuestbookStorage : IGuestbookStorage
{
    /// <summary>
    /// \if KO
    /// <para>tenants 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the tenants value.</para>
    /// \endif
    /// </summary>
    private readonly ITenantStore _tenants;
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
    /// <para>지정한 설정으로 <see cref="CsvGuestbookStorage"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="CsvGuestbookStorage"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="tenants">
    /// \if KO
    /// <para>tenants에 사용할 <c>ITenantStore</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ITenantStore</c> value used for tenants.</para>
    /// \endif
    /// </param>
    public CsvGuestbookStorage(ITenantStore tenants) => _tenants = tenants;

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
        var path = CsvPath(slug);
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var result = new List<GuestbookEntry>();
            if (!File.Exists(path)) return result;

            using var sr = new StreamReader(path, Encoding.UTF8);
            bool first = true;
            string? line;
            while ((line = await sr.ReadLineAsync().ConfigureAwait(false)) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (first && line.TrimStart().StartsWith("Name,")) { first = false; continue; }
                first = false;

                var f = ParseCsvLine(line);
                if (f.Count < 4) continue;
                if (!DateTime.TryParseExact(f[3], "yyyy-MM-dd HH:mm:ss",
                        CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var when))
                    when = DateTime.Now;

                result.Add(new GuestbookEntry { Name = f[0], Contact = f[1], Message = f[2], CreatedLocal = when });
            }
            return result;
        }
        finally { _gate.Release(); }
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
        var path = CsvPath(slug);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var tmp = path + ".tmp";

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            using (var sw = new StreamWriter(tmp, false, new UTF8Encoding(false)))
            {
                await sw.WriteLineAsync("Name,Contact,Message,CreatedLocal").ConfigureAwait(false);
                foreach (var e in entries)
                {
                    ct.ThrowIfCancellationRequested();
                    await sw.WriteLineAsync(string.Join(",",
                        Esc(e.Name), Esc(e.Contact), Esc(e.Message),
                        Esc(e.CreatedLocal.ToString("yyyy-MM-dd HH:mm:ss")))).ConfigureAwait(false);
                }
            }
            File.Copy(tmp, path, overwrite: true);
            File.Delete(tmp);
        }
        finally { _gate.Release(); }
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
        Path.Combine(_tenants.GetTenantDataPath(slug), "guestbook.csv");

    /// <summary>
    /// \if KO
    /// <para>Esc 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the esc operation.</para>
    /// \endif
    /// </summary>
    /// <param name="v">
    /// \if KO
    /// <para>v에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for v.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Esc 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the esc operation.</para>
    /// \endif
    /// </returns>
    private static string Esc(string v)
    {
        if (v.Contains(',') || v.Contains('"') || v.Contains('\n'))
            return $"\"{v.Replace("\"", "\"\"")}\"";
        return v;
    }

    /// <summary>
    /// \if KO
    /// <para>Parse Csv Line 작업을 수행합니다.</para>
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
        var sb = new StringBuilder();
        bool inQ = false;
        for (int i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (inQ) { if (ch == '"') { if (i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; } else inQ = false; } else sb.Append(ch); }
            else { if (ch == ',') { list.Add(sb.ToString()); sb.Clear(); } else if (ch == '"') inQ = true; else sb.Append(ch); }
        }
        list.Add(sb.ToString());
        return list;
    }
}
