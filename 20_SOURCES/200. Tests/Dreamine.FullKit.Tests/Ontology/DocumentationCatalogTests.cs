using DreamineWeb.Models;
using DreamineWeb.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using System.Text.Json;
using Xunit;

namespace Dreamine.FullKit.Tests.Ontology;

/// <summary>Verifies that documentation hub links are backed by non-empty generated entry artifacts.</summary>
public sealed class DocumentationCatalogTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task Catalog_ContainsExplicitAvailabilityForAllProjects()
    {
        string root = FindRepositoryRoot();
        DocumentationProjectCatalog catalog = await ReadCatalogAsync(root);
        ProjectManifest manifest = await ReadManifestAsync(root);

        int expectedProjects = manifest.SourceProjects.Count + manifest.SyntheticProjects.Count;
        Assert.Equal(expectedProjects, catalog.Projects.Count);
        Assert.True(catalog.Projects.Count(project => project.DocumentationAvailable) >= 50);
        Assert.Equal(manifest.SourceProjects.Count, catalog.Projects.Count(project => project.DoxygenAvailable));
        Assert.Equal(expectedProjects, catalog.Projects.Count(project => project.KnowledgeGraphAvailable));
        Assert.All(catalog.Projects, project =>
        {
            Assert.Equal(project.DocumentationAvailable, !string.IsNullOrWhiteSpace(project.DocumentPageUrl));
            Assert.Equal(project.DoxygenAvailable, project.DoxygenUrls.Count > 0);
            Assert.Equal(project.KnowledgeGraphAvailable, project.GraphUrls.Count > 0);
        });
    }

    [Fact]
    public async Task Catalog_ContainsSecsGemProductsAndSyntheticFullKit()
    {
        string root = FindRepositoryRoot();
        DocumentationProjectCatalog catalog = await ReadCatalogAsync(root);
        ProjectManifest manifest = await ReadManifestAsync(root);

        Assert.Equal(6, manifest.RequiredProductProjects.Count);
        foreach (RequiredProductProject expected in manifest.RequiredProductProjects)
        {
            DocumentationProjectInfo project = Assert.Single(catalog.Projects, item => item.Name == expected.Name);
            Assert.Equal(expected.Slug, project.Slug);
            Assert.Equal(expected.DocumentPageUrl, project.DocumentPageUrl);
            Assert.True(project.DocumentationAvailable);
            Assert.True(project.DoxygenAvailable);
            Assert.True(project.KnowledgeGraphAvailable);
        }

        SyntheticProject synthetic = Assert.Single(manifest.SyntheticProjects);
        DocumentationProjectInfo fullKit = Assert.Single(catalog.Projects, item => item.Name == synthetic.Name);
        Assert.Equal("meta-package", synthetic.Kind);
        Assert.Equal(synthetic.Slug, fullKit.Slug);
        Assert.Equal(synthetic.DocumentPageUrl, fullKit.DocumentPageUrl);
        Assert.True(fullKit.DocumentationAvailable);
        Assert.False(fullKit.DoxygenAvailable);
        Assert.Empty(fullKit.DoxygenUrls);
        Assert.True(fullKit.KnowledgeGraphAvailable);

        string understandRoot = ResolveUnderstandRoot(root);
        foreach (string language in new[] { "ko", "en" })
        {
            string graphPath = Path.Combine(understandRoot, "projects", synthetic.Slug, language, "knowledge-graph.json");
            using JsonDocument graph = JsonDocument.Parse(await File.ReadAllTextAsync(graphPath));
            string[] dependencyNodes = graph.RootElement.GetProperty("nodes")
                .EnumerateArray()
                .Where(node => node.GetProperty("id").GetString()?.StartsWith("module:external:", StringComparison.Ordinal) == true)
                .Select(node => node.GetProperty("name").GetString() ?? string.Empty)
                .ToArray();
            Assert.Equal(synthetic.Dependencies.Order(), dependencyNodes.Order());
            Assert.Equal(
                synthetic.Dependencies.Count,
                graph.RootElement.GetProperty("edges").EnumerateArray().Count(edge => edge.GetProperty("type").GetString() == "depends_on"));
        }
    }

    [Fact]
    public async Task CatalogService_ResolvesKnowledgeGraphFromEveryDreamineDocumentRoute()
    {
        string root = FindRepositoryRoot();
        string webRoot = Path.Combine(
            root,
            "20_SOURCES", "000. Project", "010. App", "Dreamine.Web", "wwwroot");
        DocumentationCatalogService service = new(new TestWebHostEnvironment(webRoot));
        DocumentationProjectCatalog catalog = await ReadCatalogAsync(root);

        foreach (DocumentationProjectInfo expected in catalog.Projects.Where(project =>
                     project.DocumentationAvailable
                     && project.KnowledgeGraphAvailable
                     && !string.IsNullOrWhiteSpace(project.DocumentPageUrl)))
        {
            DocumentationProjectInfo? actual = service.FindByDocumentPageUrl(expected.DocumentPageUrl!);

            Assert.NotNull(actual);
            Assert.Equal(expected.Slug, actual.Slug);
            Assert.Equal(expected.GetGraphUrl("ko"), actual.GetGraphUrl("ko"));
            Assert.Equal(expected.GetGraphUrl("en"), actual.GetGraphUrl("en"));
        }

        DocumentationProjectInfo fullKit = Assert.Single(
            catalog.Projects,
            project => project.Name == "Dreamine.SecsGem.FullKit");
        DocumentationProjectInfo? resolved = service.FindByDocumentPageUrl("/DOCS/SECSGEM-FULLKIT/");
        Assert.Equal(fullKit.Slug, resolved?.Slug);
        Assert.Contains("project=", resolved?.GetGraphUrl("ko"));
        Assert.EndsWith("&lang=ko", resolved?.GetGraphUrl("ko"));
        Assert.EndsWith("&lang=en", resolved?.GetGraphUrl("en"));
    }

    [Fact]
    public async Task Catalog_UsesActualDreamineDocumentIdsInsteadOfGuessedSlugs()
    {
        DocumentationProjectCatalog catalog = await ReadCatalogAsync(FindRepositoryRoot());

        Assert.Equal("/docs/plc-mitsubishi-mx", catalog.Projects.Single(project => project.Name == "Dreamine.PLC.Mitsubishi.MxComponent").DocumentPageUrl);
        Assert.Equal("/docs/plc-omron-cx", catalog.Projects.Single(project => project.Name == "Dreamine.PLC.Omron.CxComponent").DocumentPageUrl);
    }

    [Fact]
    public async Task Catalog_AllActiveStaticEntriesExistAndAreNonEmpty()
    {
        string root = FindRepositoryRoot();
        DocumentationProjectCatalog catalog = await ReadCatalogAsync(root);
        string understandRoot = ResolveUnderstandRoot(root);
        string doxygenRoot = Path.Combine(root, "10_DOCUMENTS", "Doxygen");

        foreach (DocumentationProjectInfo project in catalog.Projects)
        {
            foreach ((string language, string url) in project.DoxygenUrls)
            {
                string locale = language == "ko" ? "KR" : "EN";
                string entry = Path.Combine(doxygenRoot, project.Category, project.Name, locale, "html", "index.html");
                Assert.True(new FileInfo(entry) is { Exists: true, Length: >= 100 }, $"Missing Doxygen entry: {url}");
            }

            foreach ((string language, string url) in project.GraphUrls)
            {
                string directory = Path.Combine(understandRoot, "projects", project.Slug, language);
                Assert.True(new FileInfo(Path.Combine(directory, "knowledge-graph.json")) is { Exists: true, Length: >= 100 }, url);
                Assert.True(new FileInfo(Path.Combine(directory, "config.json")) is { Exists: true, Length: > 1 }, url);
                Assert.True(new FileInfo(Path.Combine(directory, "meta.json")) is { Exists: true, Length: >= 20 }, url);
            }
        }
    }

    [Fact]
    public async Task CatalogValidation_HasNoRemainingInvalidLinks()
    {
        string root = FindRepositoryRoot();
        string path = Path.Combine(ResolveUnderstandRoot(root), "project-catalog-validation.json");
        using JsonDocument document = JsonDocument.Parse(await File.ReadAllTextAsync(path));

        Assert.Equal(0, document.RootElement.GetProperty("remainingInvalidLinkCount").GetInt32());
        Assert.True(document.RootElement.GetProperty("activeLinkCount").GetInt32() > 0);
    }

    private static async Task<DocumentationProjectCatalog> ReadCatalogAsync(string root)
    {
        string path = Path.Combine(ResolveUnderstandRoot(root), "project-catalog.json");
        await using FileStream stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<DocumentationProjectCatalog>(stream, JsonOptions)
            ?? throw new InvalidDataException("project-catalog.json is empty.");
    }

    private static async Task<ProjectManifest> ReadManifestAsync(string root)
    {
        string path = Path.Combine(root, "50_SETUP", "UnderstandAnything", "project-manifest.json");
        await using FileStream stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<ProjectManifest>(stream, JsonOptions)
            ?? throw new InvalidDataException("project-manifest.json is empty.");
    }

    private static string ResolveUnderstandRoot(string root)
    {
        string? overridePath = Environment.GetEnvironmentVariable("DREAMINE_TEST_UNDERSTAND_ROOT");
        return string.IsNullOrWhiteSpace(overridePath)
            ? Path.Combine(root, "20_SOURCES", "000. Project", "010. App", "Dreamine.Web", "wwwroot", "understand")
            : Path.GetFullPath(overridePath);
    }

    private static string FindRepositoryRoot()
    {
        foreach (string start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            DirectoryInfo? current = new(start);
            while (current is not null)
            {
                if (File.Exists(Path.Combine(current.FullName, ".ua", "ontology", "instances.json")))
                    return current.FullName;
                current = current.Parent;
            }
        }

        throw new DirectoryNotFoundException("Could not find the repository root.");
    }

    public sealed class ProjectManifest
    {
        public int SchemaVersion { get; set; }
        public List<SourceProject> SourceProjects { get; set; } = [];
        public List<RequiredProductProject> RequiredProductProjects { get; set; } = [];
        public List<SyntheticProject> SyntheticProjects { get; set; } = [];
    }

    public class SourceProject
    {
        public string ProjectFile { get; set; } = string.Empty;
    }

    public sealed class RequiredProductProject : SourceProject
    {
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string DocumentPageUrl { get; set; } = string.Empty;
    }

    public sealed class SyntheticProject
    {
        public string Kind { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string DocumentPageUrl { get; set; } = string.Empty;
        public List<string> Dependencies { get; set; } = [];
    }

    private sealed class TestWebHostEnvironment(string webRootPath) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Dreamine.FullKit.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = webRootPath;
        public string EnvironmentName { get; set; } = "Test";
        public string ContentRootPath { get; set; } = webRootPath;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
