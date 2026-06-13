namespace WeddingPlatform.Models;

public sealed class AccountInfo
{
    /// <summary>표시 레이블 — 예: 신랑, 신부, 신랑 아버지, 신부 어머니</summary>
    public string Label { get; set; } = "";
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public string BankName { get; set; } = "";
    public string Account { get; set; } = "";
    public string AccountHolder { get; set; } = "";
    public string KakaoPayUrl { get; set; } = "";
}
