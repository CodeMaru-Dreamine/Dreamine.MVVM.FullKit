using System.Text.RegularExpressions;

namespace GamePlatform.Domain;

/// <summary>마루 원정 안에서 사용하는 공개 닉네임 정책입니다.</summary>
public static partial class GameNicknameRules
{
    public const int MinimumLength = 2;
    public const int MaximumLength = 12;
    public const long PaidChangeCost = 100;

    private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin", "administrator", "codemaru", "gm", "관리자", "운영자", "운영진"
    };

    public static string Normalize(string? value) =>
        WhitespaceRegex().Replace(value?.Trim() ?? string.Empty, " ");

    public static bool TryValidate(string? value, out string nickname, out string message)
    {
        nickname = Normalize(value);
        if (nickname.Length is < MinimumLength or > MaximumLength)
        {
            message = $"닉네임은 {MinimumLength}~{MaximumLength}자로 입력해 주세요.";
            return false;
        }
        if (!AllowedCharactersRegex().IsMatch(nickname))
        {
            message = "한글, 영문, 숫자와 단어 사이 공백만 사용할 수 있습니다.";
            return false;
        }
        if (ReservedNames.Contains(nickname))
        {
            message = "운영 명칭과 혼동될 수 있는 닉네임은 사용할 수 없습니다.";
            return false;
        }
        message = string.Empty;
        return true;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"^[가-힣A-Za-z0-9]+(?: [가-힣A-Za-z0-9]+)*$")]
    private static partial Regex AllowedCharactersRegex();
}
