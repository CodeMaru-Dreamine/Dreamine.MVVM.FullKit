namespace WeddingPlatform.Models;

public sealed class GuestbookEntry
{
    public string Name { get; set; } = "";
    public string Contact { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime CreatedLocal { get; set; } = DateTime.Now;
}

public sealed class GuestbookEntryView
{
    public string Name { get; init; } = "";
    public string Message { get; init; } = "";
    public DateTime CreatedLocal { get; init; }
}
