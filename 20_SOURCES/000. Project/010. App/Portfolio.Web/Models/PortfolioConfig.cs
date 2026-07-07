namespace PortfolioApp.Models;

public class PortfolioConfig
{
    public string Slug { get; set; } = "";
    public string OwnerName { get; set; } = "";
    public string Title { get; set; } = "";
    public string Bio { get; set; } = "";
    public string ProfileImageFileName { get; set; } = "";
    public string ThemeName { get; set; } = "dark";
    public string PasswordHash { get; set; } = "";
    public string OwnerUserId { get; set; } = "";
    public string OwnerProvider { get; set; } = "";
    public string OwnerEmail { get; set; } = "";
    public string OwnerDisplayName { get; set; } = "";
    public DateTime? OwnerLinkedAt { get; set; }
    public List<PortfolioAdminUser> AdminUsers { get; set; } = new();
    public bool ShowOnHome { get; set; } = true;
    public int PinOrder { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // 소셜/연락처
    public string GithubUrl { get; set; } = "";
    public string LinkedInUrl { get; set; } = "";
    public string ContactEmail { get; set; } = "";
    public bool AllowContact { get; set; } = true;

    // OG
    public string OgTitle { get; set; } = "";
    public string OgDescription { get; set; } = "";
}

public sealed class PortfolioAdminUser
{
    public string UserId { get; set; } = "";
    public string Provider { get; set; } = "";
    public string Email { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Role { get; set; } = "Admin";
    public DateTime AddedAt { get; set; } = DateTime.Now;
}
