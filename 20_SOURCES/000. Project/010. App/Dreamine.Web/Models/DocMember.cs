namespace DreamineWeb.Models;

public enum DocMemberKind { Type, Method, Property, Field, Event }

public sealed class DocMember
{
    public string FullName { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public DocMemberKind Kind { get; set; }
    public string? Summary { get; set; }
    public string? Remarks { get; set; }
    public string? Returns { get; set; }
    public List<DocParam> Params { get; set; } = [];
    public string? TypeName { get; set; }   // 소속 타입명 (메서드/프로퍼티)
}

public sealed class DocParam
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
