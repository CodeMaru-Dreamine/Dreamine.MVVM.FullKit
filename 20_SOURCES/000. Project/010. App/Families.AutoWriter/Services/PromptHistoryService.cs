using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FamiliesAutoWriter.Services;

/// <summary>
/// 이미 전송한 프롬프트 해시를 기록해 중복 전송을 방지합니다.
/// </summary>
public sealed class PromptHistoryService
{
    private static readonly object _fileGate = new();
    private readonly string _historyFile;
    private readonly HashSet<string> _sentHashes;

    public PromptHistoryService()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FamiliesAutoWriter");
        Directory.CreateDirectory(dir);
        _historyFile = Path.Combine(dir, "prompt_history.json");
        _sentHashes = Load();
    }

    public bool IsDuplicate(string prompt)
    {
        lock (_fileGate)
        {
            foreach (var hash in Load())
                _sentHashes.Add(hash);
            return _sentHashes.Contains(Hash(prompt));
        }
    }

    public void MarkSent(string prompt)
    {
        lock (_fileGate)
        {
            foreach (var hash in Load())
                _sentHashes.Add(hash);
            _sentHashes.Add(Hash(prompt));
            Save();
        }
    }

    public void Clear()
    {
        lock (_fileGate)
        {
            _sentHashes.Clear();
            Save();
        }
    }

    private HashSet<string> Load()
    {
        if (!File.Exists(_historyFile)) return [];
        try
        {
            var json = File.ReadAllText(_historyFile);
            return JsonSerializer.Deserialize<HashSet<string>>(json) ?? [];
        }
        catch { return []; }
    }

    private void Save() =>
        File.WriteAllText(_historyFile, JsonSerializer.Serialize(_sentHashes));

    private static string Hash(string s) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s.Trim()))).ToLower()[..16];
}
