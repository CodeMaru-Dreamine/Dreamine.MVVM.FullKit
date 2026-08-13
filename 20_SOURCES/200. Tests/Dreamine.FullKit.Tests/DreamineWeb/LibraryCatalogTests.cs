using System.Text.Json;
using DreamineWeb.Models;
using DreamineWeb.Services;
using DreamineWeb.ViewModels;
using Xunit;

namespace Dreamine.FullKit.Tests.DreamineWeb;

public sealed class LibraryCatalogTests
{
    private static readonly string[] SecsGemPackageIds =
    [
        "Dreamine.SecsGem.FullKit",
        "Dreamine.Secs.Abstractions",
        "Dreamine.Secs.Com",
        "Dreamine.Gem.Abstractions",
        "Dreamine.Gem",
        "Dreamine.Gem300.Abstractions",
        "Dreamine.Gem300"
    ];

    [Fact]
    public async Task FreshCatalogContainsPublishedSecsGemFamilyAndIdentityVersion()
    {
        using var directory = new TemporaryDirectory();
        var store = new JsonLibraryStore(new DreamineOptions { DataPath = directory.Path });

        List<LibraryInfo> libraries = await store.GetAllAsync();
        LibraryInfo[] secsGem = libraries
            .Where(library => SecsGemPackageIds.Contains(library.NuGetId, StringComparer.Ordinal))
            .OrderBy(library => library.SortOrder)
            .ToArray();

        Assert.Equal(SecsGemPackageIds, secsGem.Select(library => library.NuGetId));
        Assert.All(secsGem, library =>
        {
            Assert.Equal("SECS/GEM", library.Category);
            Assert.Equal("1.0.0", library.Version);
            Assert.Equal("net8.0", library.TargetFramework);
            Assert.Equal("stable", library.Status);
            Assert.True(library.IsVisible);
        });

        LibraryInfo identity = Assert.Single(libraries, library => library.Id == "identity");
        Assert.Equal("Dreamine.Identity", identity.NuGetId);
        Assert.Equal("1.0.2", identity.Version);
    }

    [Fact]
    public async Task ExistingCatalogIsUpgradedWithoutOverwritingEditorialFieldsOrNewerVersions()
    {
        using var directory = new TemporaryDirectory();
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var existing = new[]
        {
            new LibraryInfo
            {
                Id = "identity",
                Name = "Dreamine.Identity",
                Category = "Identity",
                Version = "1.0.0",
                Description = "관리자가 편집한 설명",
                DescriptionEn = "Editor supplied description",
                IsVisible = false
            },
            new LibraryInfo
            {
                Id = "gem",
                Name = "Dreamine.Gem",
                Category = "SECS/GEM",
                Version = "9.0.0",
                NuGetId = "Dreamine.Gem",
                Description = "미래 버전",
                IsVisible = true
            }
        };
        await File.WriteAllTextAsync(
            System.IO.Path.Combine(directory.Path, "libraries.json"),
            JsonSerializer.Serialize(existing, options));

        var store = new JsonLibraryStore(new DreamineOptions { DataPath = directory.Path });
        List<LibraryInfo> libraries = await store.GetAllAsync();

        LibraryInfo identity = Assert.Single(libraries, library => library.Id == "identity");
        Assert.Equal("1.0.2", identity.Version);
        Assert.Equal("Dreamine.Identity", identity.NuGetId);
        Assert.Equal("관리자가 편집한 설명", identity.Description);
        Assert.Equal("Editor supplied description", identity.DescriptionEn);
        Assert.False(identity.IsVisible);

        Assert.Equal("9.0.0", Assert.Single(libraries, library => library.Id == "gem").Version);
        Assert.All(SecsGemPackageIds, id => Assert.Contains(libraries, library => library.NuGetId == id));
    }

    [Fact]
    public async Task ProjectSyncClassifiesSecsGemAndExcludesNonPackableSamples()
    {
        using var directory = new TemporaryDirectory();
        string sourceRoot = System.IO.Path.Combine(directory.Path, "sources");
        string dataRoot = System.IO.Path.Combine(directory.Path, "data");
        Directory.CreateDirectory(sourceRoot);

        await WriteProjectAsync(sourceRoot, "Dreamine.Secs.Custom", isPackable: null);
        await WriteProjectAsync(sourceRoot, "Dreamine.Gem.Custom", isPackable: null);
        await WriteProjectAsync(sourceRoot, "Dreamine.Gem.QuickStart", isPackable: false);

        var options = new DreamineOptions { DataPath = dataRoot, LibrarySourceRoot = sourceRoot };
        var store = new JsonLibraryStore(options);
        var service = new LibraryCatalogSyncService(store, options);

        LibraryCatalogSyncResult result = await service.SyncAsync();
        List<LibraryInfo> libraries = await store.GetAllAsync();

        Assert.Equal(2, result.Added);
        Assert.Equal("SECS/GEM", Assert.Single(libraries, library => library.Name == "Dreamine.Secs.Custom").Category);
        Assert.Equal("SECS/GEM", Assert.Single(libraries, library => library.Name == "Dreamine.Gem.Custom").Category);
        Assert.DoesNotContain(libraries, library => library.Name == "Dreamine.Gem.QuickStart");
    }

    [Fact]
    public async Task FullKitDocumentationIsBundledForBothLanguageTabs()
    {
        using var directory = new TemporaryDirectory();
        var store = new JsonLibraryStore(new DreamineOptions { DataPath = directory.Path });
        string documentRoot = Path.Combine(
            FindRepositoryRoot(),
            "20_SOURCES", "000. Project", "010. App", "Dreamine.Web", "wwwroot", "xmldocs");
        var viewModel = new DocViewModel(store, documentRoot);

        viewModel.IsKorean = true;
        await viewModel.LoadAsync("secsgem-fullkit");

        Assert.NotNull(viewModel.ReadmeHtml);
        Assert.Contains("포함 패키지", viewModel.ReadmeHtml, StringComparison.Ordinal);
        Assert.Contains("빠른 시작", viewModel.ReadmeHtml, StringComparison.Ordinal);
        Assert.Contains("알려진 제한", viewModel.ReadmeHtml, StringComparison.Ordinal);

        viewModel.IsKorean = false;
        await viewModel.RefreshReadme();

        Assert.NotNull(viewModel.ReadmeHtml);
        Assert.Contains("Included packages", viewModel.ReadmeHtml, StringComparison.Ordinal);
        Assert.Contains("Quick start", viewModel.ReadmeHtml, StringComparison.Ordinal);
        Assert.Contains("Known limitations", viewModel.ReadmeHtml, StringComparison.Ordinal);
    }

    private static Task WriteProjectAsync(string root, string packageId, bool? isPackable)
    {
        string directory = System.IO.Path.Combine(root, packageId);
        Directory.CreateDirectory(directory);
        string packable = isPackable.HasValue ? $"<IsPackable>{isPackable.Value.ToString().ToLowerInvariant()}</IsPackable>" : string.Empty;
        string project = $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <PackageId>{packageId}</PackageId>
                <Version>1.0.0</Version>
                <Description>{packageId} test package.</Description>
                {packable}
              </PropertyGroup>
            </Project>
            """;
        return File.WriteAllTextAsync(System.IO.Path.Combine(directory, $"{packageId}.csproj"), project);
    }

    private static string FindRepositoryRoot()
    {
        foreach (string start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            var directory = new DirectoryInfo(start);
            while (directory is not null)
            {
                if (Directory.Exists(Path.Combine(directory.FullName, "20_SOURCES")))
                    return directory.FullName;

                directory = directory.Parent;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the Dreamine.MVVM.FullKit repository root.");
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"dreamine-web-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path)) Directory.Delete(Path, recursive: true);
        }
    }
}
