namespace DreamineWeb.Models;

/// <summary>
/// One control demo on the playground page.
/// The live interaction is fixed in code by Id; the rest of the content
/// (title, description, code snippets, and media) can be edited in the admin page.
/// </summary>
public sealed class PlaygroundDemo
{
    /// <summary>Key matched with the live render fragment, such as "button" or "checkbox".</summary>
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string? TitleEn { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? DescriptionEn { get; set; }

    public int SortOrder { get; set; }
    public bool IsVisible { get; set; } = true;

    /// <summary>Short label shown in the side navigation. Falls back to Title when empty.</summary>
    public string? NavLabel { get; set; }

    // -- Platform code snippets ----------------------------
    public string BlazorCode { get; set; } = string.Empty;
    public string WpfCode { get; set; } = string.Empty;
    public string WinFormsCode { get; set; } = string.Empty;
    public string MauiCode { get; set; } = string.Empty;
    public string VmCode { get; set; } = string.Empty;

    // -- Running screen media paths ------------------------
    public string WpfShot { get; set; } = string.Empty;
    public string WinFormsShot { get; set; } = string.Empty;
    public string MauiShot { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
