using DreamineWeb.Models;
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

        Assert.Equal(80, catalog.Projects.Count);
        Assert.Equal(50, catalog.Projects.Count(project => project.DocumentationAvailable));
        Assert.Equal(80, catalog.Projects.Count(project => project.DoxygenAvailable));
        Assert.Equal(80, catalog.Projects.Count(project => project.KnowledgeGraphAvailable));
        Assert.All(catalog.Projects, project =>
        {
            Assert.Equal(project.DocumentationAvailable, !string.IsNullOrWhiteSpace(project.DocumentPageUrl));
            Assert.Equal(project.DoxygenAvailable, project.DoxygenUrls.Count > 0);
            Assert.Equal(project.KnowledgeGraphAvailable, project.GraphUrls.Count > 0);
        });
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
        string understandRoot = Path.Combine(root, "20_SOURCES", "000. Project", "010. App", "Dreamine.Web", "wwwroot", "understand");
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
        string path = Path.Combine(root, "20_SOURCES", "000. Project", "010. App", "Dreamine.Web", "wwwroot", "understand", "project-catalog-validation.json");
        using JsonDocument document = JsonDocument.Parse(await File.ReadAllTextAsync(path));

        Assert.Equal(0, document.RootElement.GetProperty("remainingInvalidLinkCount").GetInt32());
        Assert.True(document.RootElement.GetProperty("activeLinkCount").GetInt32() > 0);
    }

    private static async Task<DocumentationProjectCatalog> ReadCatalogAsync(string root)
    {
        string path = Path.Combine(root, "20_SOURCES", "000. Project", "010. App", "Dreamine.Web", "wwwroot", "understand", "project-catalog.json");
        await using FileStream stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<DocumentationProjectCatalog>(stream, JsonOptions)
            ?? throw new InvalidDataException("project-catalog.json is empty.");
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
}
