using System.IO;
using System.Xml.Linq;
using DreamineWeb.Models;

namespace DreamineWeb.Services;

public static class XmlDocParser
{
    public static List<DocMember> Parse(string xmlPath)
    {
        if (!File.Exists(xmlPath)) return [];

        try
        {
            var doc = XDocument.Load(xmlPath);
            var members = doc.Descendants("member");
            var result = new List<DocMember>();

            foreach (var m in members)
            {
                var name = m.Attribute("name")?.Value ?? string.Empty;
                var member = new DocMember
                {
                    FullName = name,
                    ShortName = ExtractShortName(name),
                    Kind = ParseKind(name),
                    Summary = CleanText(m.Element("summary")?.Value),
                    Remarks = CleanText(m.Element("remarks")?.Value),
                    Returns = CleanText(m.Element("returns")?.Value),
                    Params = m.Elements("param")
                        .Select(p => new DocParam
                        {
                            Name = p.Attribute("name")?.Value ?? string.Empty,
                            Description = CleanText(p.Value)
                        }).ToList(),
                    TypeName = ExtractTypeName(name)
                };
                result.Add(member);
            }

            return result;
        }
        catch
        {
            return [];
        }
    }

    private static DocMemberKind ParseKind(string name) => name.Length > 1 ? name[0] switch
    {
        'T' => DocMemberKind.Type,
        'M' => DocMemberKind.Method,
        'P' => DocMemberKind.Property,
        'F' => DocMemberKind.Field,
        'E' => DocMemberKind.Event,
        _   => DocMemberKind.Type
    } : DocMemberKind.Type;

    private static string ExtractShortName(string name)
    {
        var dotName = name.Length > 2 ? name[2..] : name;
        var paren = dotName.IndexOf('(');
        if (paren >= 0) dotName = dotName[..paren];
        var last = dotName.LastIndexOf('.');
        return last >= 0 ? dotName[(last + 1)..] : dotName;
    }

    private static string? ExtractTypeName(string name)
    {
        if (name.Length < 3) return null;
        var dotName = name[2..];
        var paren = dotName.IndexOf('(');
        if (paren >= 0) dotName = dotName[..paren];
        var last = dotName.LastIndexOf('.');
        if (last < 0) return null;
        var typePart = dotName[..last];
        var typeLast = typePart.LastIndexOf('.');
        return typeLast >= 0 ? typePart[(typeLast + 1)..] : typePart;
    }

    private static string? CleanText(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var lines = raw.Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.Length > 0);
        return string.Join(" ", lines);
    }
}
