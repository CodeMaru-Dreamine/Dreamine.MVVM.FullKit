using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FamiliesAutoWriter.Services;

/// <summary>
/// \if KO
/// <para>이미 전송한 프롬프트 해시를 기록해 중복 전송을 방지합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates prompt history service functionality and related state.</para>
/// \endif
/// </summary>
public sealed class PromptHistoryService
{
    /// <summary>
    /// \if KO
    /// <para>file Gate 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the file gate value.</para>
    /// \endif
    /// </summary>
    private static readonly object _fileGate = new();
    /// <summary>
    /// \if KO
    /// <para>history File 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the history file value.</para>
    /// \endif
    /// </summary>
    private readonly string _historyFile;
    /// <summary>
    /// \if KO
    /// <para>sent Hashes 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the sent hashes value.</para>
    /// \endif
    /// </summary>
    private readonly HashSet<string> _sentHashes;

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="PromptHistoryService"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="PromptHistoryService"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    public PromptHistoryService()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FamiliesAutoWriter");
        Directory.CreateDirectory(dir);
        _historyFile = Path.Combine(dir, "prompt_history.json");
        _sentHashes = Load();
    }

    /// <summary>
    /// \if KO
    /// <para>Is Duplicate 조건을 확인합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Determines whether is duplicate.</para>
    /// \endif
    /// </summary>
    /// <param name="prompt">
    /// \if KO
    /// <para>prompt에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for prompt.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Is Duplicate 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the is duplicate condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    public bool IsDuplicate(string prompt)
    {
        lock (_fileGate)
        {
            foreach (var hash in Load())
                _sentHashes.Add(hash);
            return _sentHashes.Contains(Hash(prompt));
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Mark Sent 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the mark sent operation.</para>
    /// \endif
    /// </summary>
    /// <param name="prompt">
    /// \if KO
    /// <para>prompt에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for prompt.</para>
    /// \endif
    /// </param>
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

    /// <summary>
    /// \if KO
    /// <para>Clear 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the clear operation.</para>
    /// \endif
    /// </summary>
    public void Clear()
    {
        lock (_fileGate)
        {
            _sentHashes.Clear();
            Save();
        }
    }

    /// <summary>
    /// \if KO
    /// <para>데이터를 불러옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Loads data.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Load 작업에서 생성한 <c>HashSet&lt;string&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>HashSet&lt;string&gt;</c> result produced by the load operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>데이터를 저장합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Saves data.</para>
    /// \endif
    /// </summary>
    private void Save() =>
        File.WriteAllText(_historyFile, JsonSerializer.Serialize(_sentHashes));

    /// <summary>
    /// \if KO
    /// <para>Hash 조건을 확인합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Determines whether hash.</para>
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
    /// <para>Hash 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the hash operation.</para>
    /// \endif
    /// </returns>
    private static string Hash(string s) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s.Trim()))).ToLower()[..16];
}
