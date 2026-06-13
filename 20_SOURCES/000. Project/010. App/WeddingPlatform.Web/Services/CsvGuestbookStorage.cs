using System.Globalization;
using System.IO;
using System.Text;
using WeddingPlatform.Models;

namespace WeddingPlatform.Services;

public sealed class CsvGuestbookStorage : IGuestbookStorage
{
    private readonly ITenantStore _tenants;
    private static readonly SemaphoreSlim _gate = new(1, 1);

    public CsvGuestbookStorage(ITenantStore tenants) => _tenants = tenants;

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

    private string CsvPath(string slug) =>
        Path.Combine(_tenants.GetTenantDataPath(slug), "guestbook.csv");

    private static string Esc(string v)
    {
        if (v.Contains(',') || v.Contains('"') || v.Contains('\n'))
            return $"\"{v.Replace("\"", "\"\"")}\"";
        return v;
    }

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
