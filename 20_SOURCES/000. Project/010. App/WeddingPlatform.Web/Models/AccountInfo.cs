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
