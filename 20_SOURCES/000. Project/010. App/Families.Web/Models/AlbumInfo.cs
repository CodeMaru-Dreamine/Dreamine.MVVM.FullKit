namespace FamiliesApp.Models;

public sealed class AlbumInfo
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string CoverPhotoFileName { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int SortOrder { get; set; } = 0;
}
