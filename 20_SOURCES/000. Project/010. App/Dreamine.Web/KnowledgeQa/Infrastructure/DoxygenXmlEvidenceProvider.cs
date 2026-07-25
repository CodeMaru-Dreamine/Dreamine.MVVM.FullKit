using DreamineWeb.KnowledgeQa.Application;
using DreamineWeb.KnowledgeQa.Domain;
using DreamineWeb.Models;
using DreamineWeb.Ontology.Domain;
using DreamineWeb.Services;
using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;
using System.IO;
using System.Xml.Linq;

namespace DreamineWeb.KnowledgeQa.Infrastructure;

/// <summary>Reads bounded declaration evidence from generated Doxygen XML.</summary>
public sealed class DoxygenXmlEvidenceProvider : IDoxygenEvidenceProvider
{
    private readonly DocumentationCatalogService _catalog;
    private readonly string? _doxygenRoot;
    private readonly ConcurrentDictionary<string, CachedIndex> _cache = new(StringComparer.OrdinalIgnoreCase);

    public DoxygenXmlEvidenceProvider(
        DocumentationCatalogService catalog,
        IConfiguration configuration)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        ArgumentNullException.ThrowIfNull(configuration);
        _doxygenRoot = DocumentationPathResolver.ResolveDoxygenRoot(configuration);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EvidenceReference>> SearchAsync(
        IReadOnlyList<OntologyNode> nodes,
        string language,
        CancellationToken cancellationToken)
    {
        if (_doxygenRoot is null || nodes.Count == 0)
            return [];

        string normalizedLanguage = language.Equals("en", StringComparison.OrdinalIgnoreCase) ? "en" : "ko";
        IReadOnlyDictionary<string, DocumentationProjectInfo> projects = _catalog.GetProjects()
            .Where(project => project.DoxygenAvailable)
            .ToDictionary(project => project.Name, StringComparer.OrdinalIgnoreCase);
        List<EvidenceReference> result = [];

        foreach (IGrouping<string, OntologyNode> group in nodes
                     .Where(node => !string.IsNullOrWhiteSpace(node.ProjectName))
                     .GroupBy(node => node.ProjectName, StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!projects.TryGetValue(group.Key, out DocumentationProjectInfo? project))
                continue;

            string baseUrl = project.GetDoxygenUrl(normalizedLanguage);
            if (string.IsNullOrWhiteSpace(baseUrl))
                continue;

            string languageDirectory = normalizedLanguage == "en" ? "EN" : "KR";
            string xmlDirectory = GetSafeXmlDirectory(project, languageDirectory);
            string indexPath = Path.Combine(xmlDirectory, "index.xml");
            if (!File.Exists(indexPath))
                continue;

            CachedIndex index = await GetIndexAsync(indexPath, cancellationToken).ConfigureAwait(false);
            foreach (OntologyNode node in group)
            {
                DoxygenIndexEntry? entry = FindEntry(index.Entries, node);
                if (entry is null)
                    continue;

                string compoundPath = Path.Combine(xmlDirectory, entry.CompoundRefId + ".xml");
                DoxygenDetail detail = await ReadDetailAsync(compoundPath, entry, cancellationToken).ConfigureAwait(false);
                string? url = await BuildValidatedUrlAsync(
                    baseUrl, xmlDirectory, entry, cancellationToken).ConfigureAwait(false);
                result.Add(new EvidenceReference
                {
                    Id = $"doxygen:{result.Count + 1}",
                    Kind = EvidenceKind.Doxygen,
                    Origin = EvidenceOrigin.Direct,
                    Title = entry.QualifiedName,
                    Summary = detail.Summary,
                    StableUri = node.StableUri,
                    DoxygenUrl = url,
                    DoxygenUrlValidated = url is not null,
                    Declaration = detail.Declaration,
                    SourcePath = node.SourcePath,
                    LineStart = node.LineStart,
                    LineEnd = node.LineEnd,
                    Provenance = "Doxygen XML",
                    Confidence = 1d
                });
            }
        }

        return result
            .DistinctBy(item => (item.StableUri, item.Declaration, item.DoxygenUrl))
            .ToArray();
    }

    private string GetSafeXmlDirectory(DocumentationProjectInfo project, string languageDirectory)
    {
        string root = Path.GetFullPath(_doxygenRoot!);
        string candidate = Path.GetFullPath(Path.Combine(root, project.Category, project.Name, languageDirectory, "xml"));
        string prefix = root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Doxygen project path escaped the configured documentation root.");
        return candidate;
    }

    private async Task<CachedIndex> GetIndexAsync(string path, CancellationToken cancellationToken)
    {
        DateTime lastWrite = File.GetLastWriteTimeUtc(path);
        if (_cache.TryGetValue(path, out CachedIndex? cached) && cached.LastWriteUtc == lastWrite)
            return cached;

        await using FileStream stream = File.OpenRead(path);
        XDocument document = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken).ConfigureAwait(false);
        List<DoxygenIndexEntry> entries = [];
        foreach (XElement compound in document.Root?.Elements("compound") ?? [])
        {
            string compoundRef = (string?)compound.Attribute("refid") ?? string.Empty;
            string compoundName = NormalizeName((string?)compound.Element("name") ?? string.Empty);
            if (compoundRef.Length == 0 || compoundName.Length == 0)
                continue;

            entries.Add(new DoxygenIndexEntry(compoundRef, compoundRef, compoundName, compoundName, false));
            foreach (XElement member in compound.Elements("member"))
            {
                string memberRef = (string?)member.Attribute("refid") ?? string.Empty;
                string memberName = (string?)member.Element("name") ?? string.Empty;
                if (memberRef.Length > 0 && memberName.Length > 0)
                    entries.Add(new DoxygenIndexEntry(
                        compoundRef,
                        memberRef,
                        memberName,
                        compoundName + "." + memberName,
                        true));
            }
        }

        CachedIndex loaded = new(lastWrite, entries);
        _cache[path] = loaded;
        return loaded;
    }

    private static DoxygenIndexEntry? FindEntry(IReadOnlyList<DoxygenIndexEntry> entries, OntologyNode node)
    {
        string qualifiedName = NormalizeName(node.QualifiedName);
        DoxygenIndexEntry? exact = entries.FirstOrDefault(entry =>
            entry.QualifiedName.Equals(qualifiedName, StringComparison.OrdinalIgnoreCase));
        if (exact is not null)
            return exact;

        string ownerName = InferOwnerName(node.SourcePath);
        if (ownerName.Length > 0)
        {
            DoxygenIndexEntry? sourceOwner = entries.FirstOrDefault(entry =>
                entry.Name.Equals(node.CanonicalName, StringComparison.OrdinalIgnoreCase)
                && entry.QualifiedName.EndsWith(
                    $".{ownerName}.{node.CanonicalName}", StringComparison.OrdinalIgnoreCase));
            if (sourceOwner is not null)
                return sourceOwner;
        }

        return entries.FirstOrDefault(entry =>
            entry.Name.Equals(node.CanonicalName, StringComparison.OrdinalIgnoreCase)
            && (qualifiedName.Length == 0
                || qualifiedName.EndsWith("." + entry.Name, StringComparison.OrdinalIgnoreCase)));
    }

    private static string InferOwnerName(string sourcePath)
    {
        string fileName = Path.GetFileName(sourcePath);
        string[] suffixes = [".xaml.ViewModel.cs", ".xaml.Event.cs", ".xaml.Model.cs"];
        string[] ownerSuffixes = ["ViewModel", "Event", "Model"];
        for (int index = 0; index < suffixes.Length; index += 1)
        {
            if (fileName.EndsWith(suffixes[index], StringComparison.OrdinalIgnoreCase))
                return fileName[..^suffixes[index].Length] + ownerSuffixes[index];
        }
        return string.Empty;
    }

    private static async Task<DoxygenDetail> ReadDetailAsync(
        string path,
        DoxygenIndexEntry entry,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
            return new DoxygenDetail(string.Empty, string.Empty);

        await using FileStream stream = File.OpenRead(path);
        XDocument document = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken).ConfigureAwait(false);
        XElement? element = entry.IsMember
            ? document.Descendants("memberdef").FirstOrDefault(item =>
                string.Equals((string?)item.Attribute("id"), entry.RefId, StringComparison.Ordinal))
            : document.Descendants("compounddef").FirstOrDefault();
        if (element is null)
            return new DoxygenDetail(string.Empty, string.Empty);

        string summary = Flatten(element.Element("briefdescription"));
        if (summary.Length == 0)
            summary = Flatten(element.Element("detaileddescription"));
        string definition = Flatten(element.Element("definition"));
        string arguments = Flatten(element.Element("argsstring"));
        string declaration = string.Concat(definition, arguments);
        if (declaration.Length == 0)
            declaration = entry.QualifiedName;
        return new DoxygenDetail(Trim(summary, 500), Trim(declaration, 500));
    }

    private static string BuildUrl(string baseUrl, DoxygenIndexEntry entry)
    {
        int slash = baseUrl.LastIndexOf('/');
        string directory = slash >= 0 ? baseUrl[..(slash + 1)] : baseUrl;
        string url = directory + Uri.EscapeDataString(entry.CompoundRefId) + ".html";
        if (!entry.IsMember)
            return url;

        string prefix = entry.CompoundRefId + "_1";
        string anchor = entry.RefId.StartsWith(prefix, StringComparison.Ordinal)
            ? entry.RefId[prefix.Length..]
            : entry.RefId;
        return url + "#" + Uri.EscapeDataString(anchor);
    }

    private static async Task<string?> BuildValidatedUrlAsync(
        string baseUrl,
        string xmlDirectory,
        DoxygenIndexEntry entry,
        CancellationToken cancellationToken)
    {
        DirectoryInfo? languageDirectory = Directory.GetParent(xmlDirectory);
        if (languageDirectory is null)
            return null;
        string htmlPath = Path.Combine(languageDirectory.FullName, "html", entry.CompoundRefId + ".html");
        if (!File.Exists(htmlPath))
            return null;
        string url = BuildUrl(baseUrl, entry);
        if (!entry.IsMember)
            return url;

        int hash = url.IndexOf('#');
        if (hash < 0 || hash == url.Length - 1)
            return null;
        string anchor = Uri.UnescapeDataString(url[(hash + 1)..]);
        string html = await File.ReadAllTextAsync(htmlPath, cancellationToken).ConfigureAwait(false);
        bool exists = html.Contains($"id=\"{anchor}\"", StringComparison.Ordinal)
            || html.Contains($"name=\"{anchor}\"", StringComparison.Ordinal)
            || html.Contains($"id='{anchor}'", StringComparison.Ordinal)
            || html.Contains($"name='{anchor}'", StringComparison.Ordinal);
        return exists ? url : null;
    }

    private static string NormalizeName(string value) =>
        value.Replace("::", ".", StringComparison.Ordinal).Trim();

    private static string Flatten(XElement? element) =>
        element is null
            ? string.Empty
            : string.Join(' ', element.DescendantNodesAndSelf().OfType<XText>().Select(text => text.Value))
                .Replace("\r", " ", StringComparison.Ordinal)
                .Replace("\n", " ", StringComparison.Ordinal)
                .Trim();

    private static string Trim(string value, int length) =>
        value.Length <= length ? value : value[..length].TrimEnd() + "…";

    private sealed record CachedIndex(DateTime LastWriteUtc, IReadOnlyList<DoxygenIndexEntry> Entries);
    private sealed record DoxygenIndexEntry(
        string CompoundRefId,
        string RefId,
        string Name,
        string QualifiedName,
        bool IsMember);
    private sealed record DoxygenDetail(string Summary, string Declaration);
}
