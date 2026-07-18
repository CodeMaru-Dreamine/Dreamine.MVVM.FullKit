namespace PortfolioApp.Models;

/// <summary>
/// \if KO
/// <para>Contact Message 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates contact message functionality and related state.</para>
/// \endif
/// </summary>
public class ContactMessage
{
    /// <summary>
    /// \if KO
    /// <para>Id 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the id value.</para>
    /// \endif
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    /// <summary>
    /// \if KO
    /// <para>Sender Name 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the sender name value.</para>
    /// \endif
    /// </summary>
    public string SenderName { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Email 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the email value.</para>
    /// \endif
    /// </summary>
    public string Email { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Message 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the message value.</para>
    /// \endif
    /// </summary>
    public string Message { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Sent At 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the sent at value.</para>
    /// \endif
    /// </summary>
    public DateTime SentAt { get; set; } = DateTime.Now;
    /// <summary>
    /// \if KO
    /// <para>Is Read 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the is read value.</para>
    /// \endif
    /// </summary>
    public bool IsRead { get; set; } = false;
}
