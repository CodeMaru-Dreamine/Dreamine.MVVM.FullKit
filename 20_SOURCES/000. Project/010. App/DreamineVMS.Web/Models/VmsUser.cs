namespace DreamineVMS.Web.Models;

public sealed class VmsUser
{
    public string Id { get; init; } = "";
    public string Email { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string PublicSlug { get; init; } = "";
    public string PasswordHash { get; init; } = "";
    public string PasswordSalt { get; init; } = "";
    public DateTimeOffset CreatedAt { get; init; }
    public string LiveLayout { get; init; } = "auto";
    public string OgTitle { get; init; } = "";
    public string OgDescription { get; init; } = "";
    public string OgImage { get; init; } = "";
}
