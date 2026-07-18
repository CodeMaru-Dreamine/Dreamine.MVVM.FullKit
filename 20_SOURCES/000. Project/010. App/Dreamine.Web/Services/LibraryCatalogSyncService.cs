using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using DreamineWeb.Models;

namespace DreamineWeb.Services;

/// <summary>
/// \if KO
/// <para>Library Catalog Sync Service 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates library catalog sync service functionality and related state.</para>
/// \endif
/// </summary>
public sealed class LibraryCatalogSyncService
{
    /// <summary>
    /// \if KO
    /// <para>store 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the store value.</para>
    /// \endif
    /// </summary>
    private readonly ILibraryStore _store;
    /// <summary>
    /// \if KO
    /// <para>lib Root 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the lib root value.</para>
    /// \endif
    /// </summary>
    private readonly string _libRoot;
    /// <summary>
    /// \if KO
    /// <para>xml Doc Root 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the xml doc root value.</para>
    /// \endif
    /// </summary>
    private readonly string _xmlDocRoot;

    /// <summary>
    /// \if KO
    /// <para>Whitespace 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the whitespace value.</para>
    /// \endif
    /// </summary>
    private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="LibraryCatalogSyncService"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="LibraryCatalogSyncService"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="store">
    /// \if KO
    /// <para>store에 사용할 <c>ILibraryStore</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ILibraryStore</c> value used for store.</para>
    /// \endif
    /// </param>
    /// <param name="opts">
    /// \if KO
    /// <para>opts에 사용할 <c>DreamineOptions</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>DreamineOptions</c> value used for opts.</para>
    /// \endif
    /// </param>
    public LibraryCatalogSyncService(ILibraryStore store, DreamineOptions opts)
    {
        _store = store;
        _libRoot = opts.LibrarySourceRoot;
        _xmlDocRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot", "xmldocs");
    }

    /// <summary>
    /// \if KO
    /// <para>Is Configured 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the is configured value.</para>
    /// \endif
    /// </summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_libRoot) && Directory.Exists(_libRoot);

    /// <summary>
    /// \if KO
    /// <para>Sync Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the sync async operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Sync Async 작업에서 생성한 <c>Task&lt;LibraryCatalogSyncResult&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;LibraryCatalogSyncResult&gt;</c> result produced by the sync async operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Apply Project Metadata 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the apply project metadata operation.</para>
    /// \endif
    /// </summary>
    /// <param name="lib">
    /// \if KO
    /// <para>lib에 사용할 <c>LibraryInfo</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>LibraryInfo</c> value used for lib.</para>
    /// \endif
    /// </param>
    /// <param name="project">
    /// \if KO
    /// <para>project에 사용할 <c>ProjectMetadata</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ProjectMetadata</c> value used for project.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Apply Project Metadata 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the apply project metadata condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Project 데이터를 읽습니다.</para>
    /// \endif
    /// \if EN
    /// <para>Reads project data.</para>
    /// \endif
    /// </summary>
    /// <param name="path">
    /// \if KO
    /// <para>path에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for path.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Read Project 작업에서 생성한 <c>ProjectMetadata?</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ProjectMetadata?</c> result produced by the read project operation.</para>
    /// \endif
    /// </returns>
    private static ProjectMetadata? ReadProject(string path)
    {
        try
        {
            var doc = XDocument.Load(path);
            var root = doc.Root;
            if (root is null) return null;

            #pragma warning disable CS1587
            /// \cond LOCAL_FUNCTION_DOCUMENTATION
            /// <summary>
            /// \if KO
            /// <para>프로젝트 파일의 속성 그룹에서 지정한 속성 값을 읽습니다.</para>
            /// \endif
            /// \if EN
            /// <para>Reads a named value from the project file's property groups.</para>
            /// \endif
            /// </summary>
            /// <param name="name">
            /// \if KO
            /// <para>읽을 MSBuild 속성 이름입니다.</para>
            /// \endif
            /// \if EN
            /// <para>The MSBuild property name to read.</para>
            /// \endif
            /// </param>
            /// <returns>
            /// \if KO
            /// <para>공백을 제거한 속성 값이며, 없으면 null입니다.</para>
            /// \endif
            /// \if EN
            /// <para>The trimmed property value, or null when absent.</para>
            /// \endif
            /// </returns>
            /// \endcond
            string? Property(string name) => root.Elements("PropertyGroup")
                .Elements(name)
                .Select(x => x.Value.Trim())
                .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
            #pragma warning restore CS1587

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

    /// <summary>
    /// \if KO
    /// <para>Infer Category 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the infer category operation.</para>
    /// \endif
    /// </summary>
    /// <param name="packageId">
    /// \if KO
    /// <para>package Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for package id.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Infer Category 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the infer category operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Category Rank 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the category rank operation.</para>
    /// \endif
    /// </summary>
    /// <param name="category">
    /// \if KO
    /// <para>category에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for category.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Category Rank 작업에서 생성한 <c>int</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> result produced by the category rank operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>To Id 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the to id operation.</para>
    /// \endif
    /// </summary>
    /// <param name="packageId">
    /// \if KO
    /// <para>package Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for package id.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>To Id 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the to id operation.</para>
    /// \endif
    /// </returns>
    private static string ToId(string packageId)
    {
        var id = packageId.StartsWith("Dreamine.", StringComparison.OrdinalIgnoreCase)
            ? packageId["Dreamine.".Length..]
            : packageId;

        return Regex.Replace(id.ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
    }

    /// <summary>
    /// \if KO
    /// <para>Normalize 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the normalize operation.</para>
    /// \endif
    /// </summary>
    /// <param name="value">
    /// \if KO
    /// <para>적용할 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The value to apply.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Normalize 작업에서 생성한 <c>string?</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> result produced by the normalize operation.</para>
    /// \endif
    /// </returns>
    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return Whitespace.Replace(value.Trim(), " ");
    }

    /// <summary>
    /// \if KO
    /// <para>Split Tags 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the split tags operation.</para>
    /// \endif
    /// </summary>
    /// <param name="tags">
    /// \if KO
    /// <para>tags에 사용할 <c>string?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for tags.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Split Tags 작업에서 생성한 <c>string[]</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string[]</c> result produced by the split tags operation.</para>
    /// \endif
    /// </returns>
    private static string[] SplitTags(string? tags) =>
        string.IsNullOrWhiteSpace(tags)
            ? []
            : tags.Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                  .Distinct(StringComparer.OrdinalIgnoreCase)
                  .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                  .ToArray();

    /// <summary>
    /// \if KO
    /// <para>Same Set 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the same set operation.</para>
    /// \endif
    /// </summary>
    /// <param name="left">
    /// \if KO
    /// <para>left에 사용할 <c>string[]</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string[]</c> value used for left.</para>
    /// \endif
    /// </param>
    /// <param name="right">
    /// \if KO
    /// <para>right에 사용할 <c>string[]</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string[]</c> value used for right.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Same Set 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the same set condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    private static bool SameSet(string[] left, string[] right) =>
        left.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .SequenceEqual(right.OrderBy(x => x, StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// \if KO
    /// <para>String 값을 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Sets the string value.</para>
    /// \endif
    /// </summary>
    /// <param name="current">
    /// \if KO
    /// <para>current에 사용할 <c>string?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for current.</para>
    /// \endif
    /// </param>
    /// <param name="value">
    /// \if KO
    /// <para>적용할 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The value to apply.</para>
    /// \endif
    /// </param>
    /// <param name="assign">
    /// \if KO
    /// <para>assign에 사용할 <c>Action&lt;string?&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Action&lt;string?&gt;</c> value used for assign.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Set String 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the set string condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    private static bool SetString(string? current, string? value, Action<string?> assign)
    {
        value = string.IsNullOrWhiteSpace(value) ? null : value;
        if (string.Equals(current, value, StringComparison.Ordinal)) return false;
        assign(value);
        return true;
    }

    /// <summary>
    /// \if KO
    /// <para>Project Metadata 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates project metadata functionality and related state.</para>
    /// \endif
    /// </summary>
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

/// <summary>
/// \if KO
/// <para>Library Catalog Sync Result 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates library catalog sync result functionality and related state.</para>
/// \endif
/// </summary>
public sealed record LibraryCatalogSyncResult(int Added, int Updated, int ScannedProjects);
