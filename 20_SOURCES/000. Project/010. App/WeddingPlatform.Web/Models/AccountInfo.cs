namespace WeddingPlatform.Models;

/// <summary>
/// \if KO
/// <para>Account Info 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates account info functionality and related state.</para>
/// \endif
/// </summary>
public sealed class AccountInfo
{
    /// <summary>
    /// 공개 청첩장에 실제로 표시할 수 있는 계좌 또는 연락 수단이 있는지 확인합니다.
    /// 레이블/이름만 입력된 편집 중 행은 빈 카드로 노출하지 않습니다.
    /// </summary>
    public static bool HasDisplayableContent(AccountInfo? account) =>
        account is not null
        && (!string.IsNullOrWhiteSpace(account.Account)
            || !string.IsNullOrWhiteSpace(account.Phone)
            || NormalizePaymentUrl(account.KakaoPayUrl).Length > 0);

    /// <summary>
    /// 관리자 iframe 미리보기에 전달할 때 원본 편집 객체와 참조를 공유하지 않도록
    /// 계좌 정보를 길이 제한 및 URL 검증 후 값 복사합니다.
    /// </summary>
    public static AccountInfo CloneForPreview(AccountInfo source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return new AccountInfo
        {
            Label = Limit(source.Label, 80),
            Name = Limit(source.Name, 120),
            Phone = Limit(source.Phone, 32),
            BankName = Limit(source.BankName, 80),
            Account = Limit(source.Account, 100),
            AccountHolder = Limit(source.AccountHolder, 120),
            KakaoPayUrl = NormalizePaymentUrl(source.KakaoPayUrl),
        };
    }

    /// <summary>
    /// 결제 링크로 안전한 HTTP(S) 절대 URL만 반환합니다.
    /// </summary>
    public static string NormalizePaymentUrl(string? value)
    {
        var candidate = Limit(value, 2048).Trim();
        return Uri.TryCreate(candidate, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp
                || uri.Scheme == Uri.UriSchemeHttps)
            ? uri.AbsoluteUri
            : "";
    }

    private static string Limit(string? value, int maxLength)
    {
        var normalized = value ?? "";
        return normalized.Length <= maxLength
            ? normalized
            : normalized[..maxLength];
    }

    /// <summary>
    /// \if KO
    /// <para>표시 레이블 — 예: 신랑, 신부, 신랑 아버지, 신부 어머니</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the label value.</para>
    /// \endif
    /// </summary>
    public string Label { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Name 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the name value.</para>
    /// \endif
    /// </summary>
    public string Name { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Phone 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the phone value.</para>
    /// \endif
    /// </summary>
    public string Phone { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Bank Name 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the bank name value.</para>
    /// \endif
    /// </summary>
    public string BankName { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Account 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the account value.</para>
    /// \endif
    /// </summary>
    public string Account { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Account Holder 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the account holder value.</para>
    /// \endif
    /// </summary>
    public string AccountHolder { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Kakao Pay Url 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the kakao pay url value.</para>
    /// \endif
    /// </summary>
    public string KakaoPayUrl { get; set; } = "";
}
