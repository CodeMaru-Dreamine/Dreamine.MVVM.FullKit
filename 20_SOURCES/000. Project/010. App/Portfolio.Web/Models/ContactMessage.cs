namespace PortfolioApp.Models;

public class ContactMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string SenderName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime SentAt { get; set; } = DateTime.Now;
    public bool IsRead { get; set; } = false;
}
