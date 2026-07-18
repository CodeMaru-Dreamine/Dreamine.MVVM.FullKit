using System.IO;
using System.Xml.Linq;
using DreamineWeb.Models;

namespace DreamineWeb.Services;

/// <summary>
/// \if KO
/// <para>Xml Doc Parser 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates xml doc parser functionality and related state.</para>
/// \endif
/// </summary>
public static class XmlDocParser
{
    /// <summary>
    /// \if KO
    /// <para>Parse 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the parse operation.</para>
    /// \endif
    /// </summary>
    /// <param name="xmlPath">
    /// \if KO
    /// <para>xml Path에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for xml path.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Parse 작업에서 생성한 <c>List&lt;DocMember&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>List&lt;DocMember&gt;</c> result produced by the parse operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Parse Kind 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the parse kind operation.</para>
    /// \endif
    /// </summary>
    /// <param name="name">
    /// \if KO
    /// <para>name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for name.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Parse Kind 작업에서 생성한 <c>DocMemberKind</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>DocMemberKind</c> result produced by the parse kind operation.</para>
    /// \endif
    /// </returns>
    private static DocMemberKind ParseKind(string name) => name.Length > 1 ? name[0] switch
    {
        'T' => DocMemberKind.Type,
        'M' => DocMemberKind.Method,
        'P' => DocMemberKind.Property,
        'F' => DocMemberKind.Field,
        'E' => DocMemberKind.Event,
        _   => DocMemberKind.Type
    } : DocMemberKind.Type;

    /// <summary>
    /// \if KO
    /// <para>Extract Short Name 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the extract short name operation.</para>
    /// \endif
    /// </summary>
    /// <param name="name">
    /// \if KO
    /// <para>name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for name.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Extract Short Name 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the extract short name operation.</para>
    /// \endif
    /// </returns>
    private static string ExtractShortName(string name)
    {
        var dotName = name.Length > 2 ? name[2..] : name;
        var paren = dotName.IndexOf('(');
        if (paren >= 0) dotName = dotName[..paren];
        var last = dotName.LastIndexOf('.');
        return last >= 0 ? dotName[(last + 1)..] : dotName;
    }

    /// <summary>
    /// \if KO
    /// <para>Extract Type Name 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the extract type name operation.</para>
    /// \endif
    /// </summary>
    /// <param name="name">
    /// \if KO
    /// <para>name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for name.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Extract Type Name 작업에서 생성한 <c>string?</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> result produced by the extract type name operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Clean Text 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the clean text operation.</para>
    /// \endif
    /// </summary>
    /// <param name="raw">
    /// \if KO
    /// <para>raw에 사용할 <c>string?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for raw.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Clean Text 작업에서 생성한 <c>string?</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> result produced by the clean text operation.</para>
    /// \endif
    /// </returns>
    private static string? CleanText(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var lines = raw.Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.Length > 0);
        return string.Join(" ", lines);
    }
}
