using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using DreamineWeb.Models;

namespace DreamineWeb.Services;

public sealed class LibraryCatalogSyncService
{
    private readonly ILibraryStore _store;
    private readonly string _libRoot;
    private readonly string _xmlDocRoot;

    private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);

    public LibraryCatalogSyncService(ILibraryStore store, DreamineOptions opts)
    {
        _store = store;
        _libRoot = opts.LibrarySourceRoot;
        _xmlDocRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot", "xmldocs");
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_libRoot) && Directory.Exists(_libRoot);

    public async Task<LibraryCatalogSyncResult> SyncAsync()
    {
        if (!IsConfigured) return new LibraryCatalogSyncResult(-1, 0, 0);

        var projects = Directory.EnumerateFiles(_libRoot, "*.csproj", SearchOption.AllDirectories)
            .Select(ReadProject)
            .Where(p => p is not null)
            .Cast<ProjectMetadata>()
            .Where(p => !p.PackageId.EndsWith(".Tests", StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => CategoryRank(p.Category))
            .ThenBy(p => p.PackageId, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var libraries = await _store.GetAllAsync();
        var byName = libraries.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
        var nextSortByCategory = libraries
            .GroupBy(x => string.IsNullOrWhiteSpace(x.Category) ? "Other" : x.Category)
            .ToDictionary(g => g.Key, g => g.Max(x => x.SortOrder), StringComparer.OrdinalIgnoreCase);

        var added = 0;
        var updated = 0;

        foreach (var project in projects)
        {
            var isNew = !byName.TryGetValue(project.PackageId, out var lib);
            if (isNew)
            {
                var sortBase = nextSortByCategory.TryGetValue(project.Category, out var current)
                    ? current
                    : CategoryRank(project.Category) * 10;

                lib = new LibraryInfo
                {
                    Id = ToId(project.PackageId),
                    Name = project.PackageId,
                    Category = project.Category,
                    SortOrder = sortBase + 1,
                    IsVisible = true,
                    Status = "stable",
                    Version = "1.0.0"
                };

                nextSortByCategory[project.Category] = lib.SortOrder;
                added++;
            }

            if (ApplyProjectMetadata(lib!, project) || isNew)
            {
                await _store.SaveAsync(lib!);
                if (!isNew) updated++;
            }

            byName[project.PackageId] = lib!;
        }

        return new LibraryCatalogSyncResult(added, updated, projects.Count);
    }

    private bool ApplyProjectMetadata(LibraryInfo lib, ProjectMetadata project)
    {
        var dirty = false;

        if (SetString(lib.Name, project.PackageId, v => lib.Name = v ?? string.Empty)) dirty = true;
        if (SetString(lib.Category, project.Category, v => lib.Category = v ?? string.Empty)) dirty = true;
        if (SetString(lib.Version, project.Version, v => lib.Version = v ?? string.Empty)) dirty = true;
        if (SetString(lib.NuGetId, project.PackageId, v => lib.NuGetId = v)) dirty = true;
        if (SetString(lib.SourceProjectPath, project.ProjectPath, v => lib.SourceProjectPath = v)) dirty = true;
        if (SetString(lib.TargetFramework, project.TargetFramework, v => lib.TargetFramework = v)) dirty = true;

        if (!string.IsNullOrWhiteSpace(project.Description))
        {
            if (SetString(lib.DescriptionEn, project.Description, v => lib.DescriptionEn = v)) dirty = true;
            if (string.IsNullOrWhiteSpace(lib.Description))
            {
                lib.Description = project.Description;
                dirty = true;
            }
        }

        if (!string.IsNullOrWhiteSpace(project.RepositoryUrl))
        {
            if (SetString(lib.RepoUrl, project.RepositoryUrl, v => lib.RepoUrl = v)) dirty = true;
        }

        if (project.Tags.Length > 0 && !SameSet(lib.Tags, project.Tags))
        {
            lib.Tags = project.Tags;
            dirty = true;
        }

        if (!SameSet(lib.Dependencies, project.Dependencies))
        {
            lib.Dependencies = project.Dependencies;
            dirty = true;
        }

        var xmlPath = Path.Combine(_xmlDocRoot, project.PackageId, $"{project.PackageId}.xml");
        if (File.Exists(xmlPath))
        {
            if (SetString(lib.XmlDocPath, xmlPath, v => lib.XmlDocPath = v)) dirty = true;
        }

        if (dirty) lib.UpdatedAt = DateTime.UtcNow;
        return dirty;
    }

    private static ProjectMetadata? ReadProject(string path)
    {
        try
        {
            var doc = XDocument.Load(path);
            var root = doc.Root;
            if (root is null) return null;

            string? Property(string name) => root.Elements("PropertyGroup")
                .Elements(name)
                .Select(x => x.Value.Trim())
                .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

            var assemblyName = Path.GetFileNameWithoutExtension(path);
            var packageId = Property("PackageId") ?? Property("AssemblyName") ?? assemblyName;
            var version = Property("Version");
            if (string.IsNullOrWhiteSpace(version) || version.Contains("$(", StringComparison.Ordinal))
                version = "1.0.0";

            var targetFramework = Property("TargetFramework") ?? Property("TargetFrameworks") ?? string.Empty;
            var description = Normalize(Property("Description"));
            var tags = SplitTags(Property("PackageTags"));
            var dependencies = root.Elements("ItemGroup")
                .Elements("ProjectReference")
                .Select(x => x.Attribute("Include")?.Value)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => Path.GetFileNameWithoutExtension(x!))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return new ProjectMetadata(
                path,
                packageId,
                version,
                targetFramework,
                description,
                Property("RepositoryUrl") ?? Property("PackageProjectUrl"),
                tags,
                dependencies,
                InferCategory(packageId));
        }
        catch
        {
            return null;
        }
    }

    private static string InferCategory(string packageId)
    {
        if (packageId.StartsWith("Dreamine.MVVM.", StringComparison.OrdinalIgnoreCase)) return "MVVM";
        if (packageId.StartsWith("Dreamine.UI.", StringComparison.OrdinalIgnoreCase)) return "UI";
        if (packageId.StartsWith("Dreamine.Communication.", StringComparison.OrdinalIgnoreCase)) return "Communication";
        if (packageId.StartsWith("Dreamine.Database.", StringComparison.OrdinalIgnoreCase)) return "Database";
        if (packageId.StartsWith("Dreamine.PLC.", StringComparison.OrdinalIgnoreCase)) return "PLC";
        if (packageId.StartsWith("Dreamine.IO.", StringComparison.OrdinalIgnoreCase)) return "IO";
        if (packageId.StartsWith("Dreamine.Threading", StringComparison.OrdinalIgnoreCase)) return "Infrastructure";
        if (packageId.StartsWith("Dreamine.Logging", StringComparison.OrdinalIgnoreCase)) return "Infrastructure";
        if (packageId.StartsWith("Dreamine.Hybrid", StringComparison.OrdinalIgnoreCase)) return "Hybrid";
        if (packageId.StartsWith("Dreamine.Identity", StringComparison.OrdinalIgnoreCase)) return "Identity";
        return "Other";
    }

    private static int CategoryRank(string category) => category switch
    {
        "MVVM" => 1,
        "Hybrid" => 2,
        "UI" => 3,
        "Identity" => 4,
        "Infrastructure" => 5,
        "Database" => 6,
        "Communication" => 7,
        "IO" => 8,
        "PLC" => 9,
        _ => 99
    };

    private static string ToId(string packageId)
    {
        var id = packageId.StartsWith("Dreamine.", StringComparison.OrdinalIgnoreCase)
            ? packageId["Dreamine.".Length..]
            : packageId;

        return Regex.Replace(id.ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
    }

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return Whitespace.Replace(value.Trim(), " ");
    }

    private static string[] SplitTags(string? tags) =>
        string.IsNullOrWhiteSpace(tags)
            ? []
            : tags.Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                  .Distinct(StringComparer.OrdinalIgnoreCase)
                  .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                  .ToArray();

    private static bool SameSet(string[] left, string[] right) =>
        left.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .SequenceEqual(right.OrderBy(x => x, StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);

    private static bool SetString(string? current, string? value, Action<string?> assign)
    {
        value = string.IsNullOrWhiteSpace(value) ? null : value;
        if (string.Equals(current, value, StringComparison.Ordinal)) return false;
        assign(value);
        return true;
    }

    private sealed record ProjectMetadata(
        string ProjectPath,
        string PackageId,
        string Version,
        string TargetFramework,
        string? Description,
        string? RepositoryUrl,
        string[] Tags,
        string[] Dependencies,
        string Category);
}

public sealed record LibraryCatalogSyncResult(int Added, int Updated, int ScannedProjects);
