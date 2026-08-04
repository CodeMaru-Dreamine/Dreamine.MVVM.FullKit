using System.Net.Mail;

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
    public const int MaxSenderNameLength = 100;
    public const int MaxEmailLength = 254;
    public const int MaxMessageLength = 4000;

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

    /// <summary>Normalizes and validates untrusted public contact input before persistence.</summary>
    public bool TryNormalizeForStorage(out string validationError)
    {
        SenderName = (SenderName ?? string.Empty).Trim();
        Email = (Email ?? string.Empty).Trim();
        Message = (Message ?? string.Empty).Trim();

        if (SenderName.Length == 0)
        {
            validationError = "name.required";
            return false;
        }

        if (Message.Length == 0)
        {
            validationError = "message.required";
            return false;
        }

        if (SenderName.Length > MaxSenderNameLength ||
            Email.Length > MaxEmailLength ||
            Message.Length > MaxMessageLength)
        {
            validationError = "input.tooLong";
            return false;
        }

        if (SenderName.Any(char.IsControl) ||
            Email.Contains('\r') ||
            Email.Contains('\n') ||
            (!string.IsNullOrEmpty(Email) && !MailAddress.TryCreate(Email, out _)))
        {
            validationError = "input.invalid";
            return false;
        }

        validationError = string.Empty;
        return true;
    }
}
