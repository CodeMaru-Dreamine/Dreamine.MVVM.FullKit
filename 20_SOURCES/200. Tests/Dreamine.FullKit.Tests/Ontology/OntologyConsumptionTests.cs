using DreamineWeb.Ontology.Application;
using DreamineWeb.Ontology.Domain;
using DreamineWeb.Ontology.Infrastructure;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace Dreamine.FullKit.Tests.Ontology;

/// <summary>Verifies the bounded Dreamine.Web ontology consumer against generated and synthetic data.</summary>
public sealed class OntologyConsumptionTests
{
    private static readonly Lazy<Task<ActualContext>> Actual = new(CreateActualContextAsync);
    private readonly ITestOutputHelper _output;

    public OntologyConsumptionTests(ITestOutputHelper output) => _output = output;

    [Theory]
    [InlineData("forwardsTo", "calls")]
    [InlineData("hasEventComponent", "depends_on")]
    public void RelationResolver_RestoresOriginalDreamineMeaning(string original, string projection)
    {
        OntologyRelationMeaning meaning = new OntologyRelationResolver().Resolve(original);

        Assert.Equal(original, meaning.OriginalType);
        Assert.Equal(projection, meaning.ProjectionType);
        Assert.True(meaning.WasProjected);
    }

    [Fact]
    public async Task Repository_PrefersSourceVerifiedType_AndFallsBackToRawType()
    {
        await using TemporaryOntologyFixture fixture = await TemporaryOntologyFixture.CreateAsync();
        JsonOntologyRepository repository = fixture.CreateRepository();

        OntologyNode corrected = Assert.IsType<OntologyNode>(
            await repository.GetNodeAsync("urn:test:corrected", CancellationToken.None));
        OntologyNode fallback = Assert.IsType<OntologyNode>(
            await repository.GetNodeAsync("urn:test:fallback", CancellationToken.None));

        Assert.Equal("CodeInterface", corrected.EffectiveType);
        Assert.Equal("class", corrected.RawType);
        Assert.Equal("enum", fallback.EffectiveType);
    }

    [Fact]
    public async Task Repository_SearchesFiltersAndPagesWithoutSendingWholeDataset()
    {
        ActualContext context = await Actual.Value;

        OntologyPage<OntologyNode> page = await context.Repository.SearchNodesAsync(
            new OntologyQuery("MainWindow", "ViewModel", "SampleSmart"), 1, 5, CancellationToken.None);
        OntologyPage<OntologyNode> relationPage = await context.Repository.SearchNodesAsync(
            new OntologyQuery(RelationType: "forwardsTo", IncludeExcluded: true), 1, 3, CancellationToken.None);

        Assert.InRange(page.Items.Count, 1, 5);
        Assert.All(page.Items, item => Assert.Equal("ViewModel", item.EffectiveType));
        Assert.All(page.Items, item => Assert.Contains("SampleSmart", item.SourcePath, StringComparison.OrdinalIgnoreCase));
        Assert.InRange(relationPage.Items.Count, 1, 3);
        Assert.True(relationPage.TotalCount > relationPage.Items.Count);
    }

    [Fact]
    public async Task Repository_ResolvesStableUri_AndHandlesMissingNode()
    {
        ActualContext context = await Actual.Value;
        OntologyPage<OntologyNode> page = await context.Repository.SearchNodesAsync(
            new OntologyQuery("MainWindowViewModel", "ViewModel", FilePath: "SampleSmart/Pages/MainWindow.xaml.ViewModel.cs"),
            1, 10, CancellationToken.None);
        OntologyNode expected = Assert.Single(page.Items, item => item.CanonicalName == "MainWindowViewModel");

        OntologyNode? resolved = await context.Repository.GetNodeAsync(expected.StableUri, CancellationToken.None);
        OntologyNode? missing = await context.Repository.GetNodeAsync("urn:dreamine:missing", CancellationToken.None);

        Assert.Equal(expected.StableUri, resolved?.StableUri);
        Assert.Null(missing);
        Assert.Null(await context.Search.GetNodeAsync("urn:dreamine:missing", CancellationToken.None));
    }

    [Fact]
    public async Task DreamineEventSample_ConnectsViewModelAndForwardedOkMethod()
    {
        ActualContext context = await Actual.Value;

        OntologyEventFlowViewModel flow = Assert.IsType<OntologyEventFlowViewModel>(
            await context.Search.GetDreamineEventSampleAsync(CancellationToken.None));

        Assert.Equal("MainWindowViewModel", flow.ViewModel.Name);
        Assert.Equal("MainWindowEvent", flow.EventComponent.Name);
        Assert.Equal("Ok", flow.Command.Name);
        Assert.Equal("Ok", flow.TargetMethod.Name);
        Assert.Equal("hasEventComponent", flow.ComponentRelation.OriginalType);
        Assert.Equal("depends_on", flow.ComponentRelation.ProjectionType);
        Assert.Equal("forwardsTo", flow.ForwardingRelation.OriginalType);
        Assert.Equal("calls", flow.ForwardingRelation.ProjectionType);
        Assert.True(flow.EventComponent.IsDreamineEventComponent);
    }

    [Fact]
    public async Task SourceViewer_DeserializesCodeAndPreservesLinesAndXmlComments()
    {
        ActualContext context = await Actual.Value;
        OntologyNode file = await FindActualNodeAsync(
            context.Repository,
            "OmronFinsTcpSimulatorServer.cs",
            "SourceFile",
            "PLC.Omron.Fins/Simulation/OmronFinsTcpSimulatorServer.cs");

        OntologySourceDocumentViewModel source = await context.Source.GetSourceAsync(file.StableUri, CancellationToken.None);

        Assert.True(source.Availability.IsAvailable, source.Availability.ReasonCode);
        Assert.True(source.Lines.Count > 100);
        Assert.Contains(source.Lines, line => line.Text.Contains("/// <summary>", StringComparison.Ordinal));
        Assert.DoesNotContain(source.Lines, line => line.Text.Contains("\\r\\n", StringComparison.Ordinal));
        Assert.DoesNotContain(".json", source.Availability.DisplayPath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SourceViewer_UsesCurrentStaleOverrideForRemappedTestSource()
    {
        ActualContext context = await Actual.Value;
        OntologyNode file = await FindActualNodeAsync(
            context.Repository,
            "DreamineThemeTests.cs",
            "SourceFile",
            "200. Tests/UI.WinForms.Tests/DreamineThemeTests.cs");

        OntologySourceDocumentViewModel source = await context.Source.GetSourceAsync(file.StableUri, CancellationToken.None);

        Assert.True(source.Availability.IsAvailable, source.Availability.ReasonCode);
        Assert.Equal("20_SOURCES/200. Tests/UI.WinForms.Tests/DreamineThemeTests.cs", source.Availability.DisplayPath);
        Assert.NotEmpty(source.Lines);
    }

    [Fact]
    public async Task SourceViewer_ReportsGeneratedCodeAsExcluded()
    {
        ActualContext context = await Actual.Value;
        OntologyPage<OntologyNode> page = await context.Repository.SearchNodesAsync(
            new OntologyQuery("Resources.Designer.cs", IncludeExcluded: true), 1, 100, CancellationToken.None);
        OntologyNode generated = Assert.Single(page.Items, node => node.SourceVerificationStatus == "excluded_generated"
            && node.EffectiveType == "SourceFile"
            && node.ProjectName == "SampleSmart");

        OntologySourceAvailabilityViewModel source = await context.Source.GetAvailabilityAsync(
            generated.StableUri,
            CancellationToken.None);

        Assert.False(source.IsAvailable);
        Assert.True(source.IsExcluded);
        Assert.Equal("generated-excluded", source.ReasonCode);
    }

    [Fact]
    public async Task SourceViewer_BlocksTraversalAndDoesNotExposeAbsolutePaths()
    {
        await using TemporaryOntologyFixture fixture = await TemporaryOntologyFixture.CreateAsync("../secret.cs");
        JsonOntologyRepository repository = fixture.CreateRepository();
        OntologySourceService service = new(repository, new FixedOntologySourcePathResolver(fixture.SourceDirectory));

        OntologySourceAvailabilityViewModel source = await service.GetAvailabilityAsync(
            "urn:test:corrected",
            CancellationToken.None);

        Assert.False(source.IsAvailable);
        Assert.Equal("invalid-path", source.ReasonCode);
        Assert.DoesNotContain(Path.GetTempPath(), source.DisplayPath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SourceViewer_HonorsCancellation()
    {
        ActualContext context = await Actual.Value;
        OntologyNode file = await FindActualNodeAsync(
            context.Repository,
            "OmronFinsTcpSimulatorServer.cs",
            "SourceFile",
            "PLC.Omron.Fins/Simulation/OmronFinsTcpSimulatorServer.cs");
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            context.Source.GetSourceAsync(file.StableUri, cancellation.Token));
    }

    [Fact]
    public async Task SourceViewer_AllActiveStableUriEntriesResolveWithoutMissingArtifacts()
    {
        ActualContext context = await Actual.Value;
        HashSet<string> openedPaths = new(StringComparer.OrdinalIgnoreCase);
        int activeSourceNodes = 0;
        int inspectedNodes = 0;
        int pageNumber = 1;

        while (true)
        {
            OntologyPage<OntologyNode> page = await context.Repository.SearchNodesAsync(
                new OntologyQuery(),
                pageNumber,
                250,
                CancellationToken.None);

            foreach (OntologyNode node in page.Items)
            {
                inspectedNodes += 1;
                OntologySourceAvailabilityViewModel availability = await context.Source.GetAvailabilityAsync(
                    node.StableUri,
                    CancellationToken.None);
                if (!availability.IsAvailable)
                    continue;

                activeSourceNodes += 1;
                Assert.False(string.IsNullOrWhiteSpace(availability.DisplayPath));
                Assert.False(Path.IsPathRooted(availability.DisplayPath));
                Assert.False(
                    availability.DisplayPath.StartsWith("/understand/source/", StringComparison.OrdinalIgnoreCase),
                    availability.DisplayPath);

                if (!openedPaths.Add(availability.DisplayPath))
                    continue;

                OntologySourceDocumentViewModel source = await context.Source.GetSourceAsync(
                    node.StableUri,
                    CancellationToken.None);
                Assert.True(source.Availability.IsAvailable, $"{node.StableUri}: {source.Availability.ReasonCode}");
                Assert.NotEmpty(source.Lines);
            }

            if (pageNumber >= page.TotalPages)
                break;
            pageNumber += 1;
        }

        _output.WriteLine(
            "Source links: inspected nodes={0:N0}; active stable URIs={1:N0}; unique artifacts opened={2:N0}; failures=0",
            inspectedNodes,
            activeSourceNodes,
            openedPaths.Count);
        Assert.True(inspectedNodes > 6_000);
        Assert.True(activeSourceNodes > 5_000);
        Assert.True(openedPaths.Count > 1_000);
    }

    [Fact]
    public async Task Repository_HonorsCancellationDuringPagedSearch()
    {
        ActualContext context = await Actual.Value;
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.Repository.SearchNodesAsync(
            new OntologyQuery(IncludeExcluded: true), 1, 25, cancellation.Token));
    }

    [Fact]
    public async Task Validation_DoesNotReportHealthyWhenReportsAreMissing()
    {
        await using TemporaryOntologyFixture fixture = await TemporaryOntologyFixture.CreateAsync();
        JsonOntologyRepository repository = fixture.CreateRepository();

        OntologyValidationData validation = await repository.GetValidationDataAsync(CancellationToken.None);

        Assert.False(validation.IsHealthy);
        Assert.Contains(validation.Artifacts, item => item.Name == "source-audit.json" && !item.Exists);
        Assert.Contains(validation.Artifacts, item => item.Name == "architecture-validation.json" && !item.Exists);
        Assert.Contains(validation.Artifacts, item => item.Name == "linkml-shacl-validation.json" && !item.Exists);
    }

    [Fact]
    public async Task Validation_DoesNotReportHealthyWhenReportIsCorrupt()
    {
        await using TemporaryOntologyFixture fixture = await TemporaryOntologyFixture.CreateAsync();
        await File.WriteAllTextAsync(Path.Combine(fixture.DirectoryPath, "source-audit.json"), "{ not-json");
        JsonOntologyRepository repository = fixture.CreateRepository();

        OntologyValidationData validation = await repository.GetValidationDataAsync(CancellationToken.None);

        Assert.False(validation.IsHealthy);
        Assert.Contains(validation.Artifacts, item => item.Name == "source-audit.json" && item.Exists && !item.IsValid);
    }

    [Fact]
    public async Task ActualReports_RemainConformantAndCurrent()
    {
        ActualContext context = await Actual.Value;

        OntologyValidationData validation = await context.Repository.GetValidationDataAsync(CancellationToken.None);

        Assert.True(validation.IsHealthy, validation.HealthReason);
        Assert.Equal(0, validation.SourceVsOverlayMismatches);
        Assert.Equal(0, validation.SourceDeclarationsMissingOverlay);
        Assert.Equal(0, validation.UnclassifiedPartialMethods);
        Assert.Equal(0, validation.RawDanglingRelations);
        Assert.Equal(0, validation.OverlayDanglingRelations);
        Assert.Equal(0, validation.DuplicateStableUris);
        Assert.Equal(0, validation.StableUriTypeConflicts);
        Assert.Equal(0, validation.LinkmlShaclViolations);
        Assert.All(validation.Shapes, shape =>
        {
            Assert.Equal(0, shape.ViolationCount);
            Assert.True(shape.PositiveFixtureConforms);
            Assert.True(shape.NegativeFixtureRejected);
            Assert.True(shape.FixtureTestPassed);
        });
    }

    [Fact]
    public async Task LoadMetrics_AreBoundedAndCacheIsReused()
    {
        ActualContext context = await Actual.Value;
        _ = await context.Repository.GetFacetsAsync(CancellationToken.None);

        OntologyLoadMetrics metrics = await context.Repository.GetLoadMetricsAsync(CancellationToken.None);

        _output.WriteLine(
            "Ontology load: {0:N1} ms; managed delta: {1:N1} MiB; working-set delta: {2:N1} MiB; source: {3:N0} bytes; cache hits: {4}; reloads: {5}",
            metrics.LoadMilliseconds,
            metrics.ManagedMemoryDeltaMiB,
            metrics.WorkingSetDeltaMiB,
            metrics.SourceBytes,
            metrics.CacheHits,
            metrics.ReloadCount);
        Assert.Equal(6549, metrics.ElementCount);
        Assert.Equal(21280, metrics.RelationCount);
        Assert.True(metrics.SourceBytes < 40_000_000);
        Assert.True(metrics.ManagedMemoryDeltaMiB < 250);
        Assert.True(metrics.LoadMilliseconds < 15_000);
        Assert.True(metrics.CacheHits > 0);
        Assert.Equal(1, metrics.ReloadCount);
    }

    private static Task<ActualContext> CreateActualContextAsync()
    {
        string repositoryRoot = FindRepositoryRoot();
        JsonOntologyRepository repository = new(
            new FixedOntologyDataPathResolver(Path.Combine(repositoryRoot, ".ua", "ontology")));
        OntologySearchService search = new(repository, new OntologyRelationResolver(), new OntologyGraphMapper());
        OntologySourceService source = new(
            repository,
            new FixedOntologySourcePathResolver(Path.Combine(
                repositoryRoot,
                "20_SOURCES",
                "000. Project",
                "010. App",
                "Dreamine.Web",
                "wwwroot",
                "understand",
                "source")));
        return Task.FromResult(new ActualContext(repository, search, source));
    }

    private static async Task<OntologyNode> FindActualNodeAsync(
        IOntologyRepository repository,
        string name,
        string type,
        string pathFragment)
    {
        OntologyPage<OntologyNode> page = await repository.SearchNodesAsync(
            new OntologyQuery(name, type, FilePath: pathFragment, IncludeExcluded: true),
            1,
            100,
            CancellationToken.None);
        return Assert.Single(page.Items, node => node.CanonicalName.Equals(name, StringComparison.Ordinal)
            && node.SourcePath.Contains(pathFragment, StringComparison.OrdinalIgnoreCase));
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

        throw new DirectoryNotFoundException("Could not find .ua/ontology from the test host.");
    }

    private sealed record ActualContext(
        JsonOntologyRepository Repository,
        OntologySearchService Search,
        OntologySourceService Source);

    private sealed class TemporaryOntologyFixture : IAsyncDisposable
    {
        private TemporaryOntologyFixture(string directoryPath)
        {
            DirectoryPath = directoryPath;
            SourceDirectory = Path.Combine(directoryPath, "source");
            Directory.CreateDirectory(SourceDirectory);
        }

        public string DirectoryPath { get; }
        public string SourceDirectory { get; }

        public static async Task<TemporaryOntologyFixture> CreateAsync(string correctedSourcePath = "Tests/CorrectedContract.cs")
        {
            string directory = Path.Combine(Path.GetTempPath(), $"dreamine-ontology-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            DateTimeOffset now = DateTimeOffset.UtcNow;
            var manifest = new
            {
                ontologyVersion = "test",
                sourceGraphVersion = "test",
                generatedAt = now,
                counts = new { elements = 2, relations = 1 }
            };
            var instances = new
            {
                ontology_version = "test",
                generated_at = now,
                elements = new object[]
                {
                    new
                    {
                        stable_id = "urn:test:corrected",
                        canonical_name = "CorrectedContract",
                        element_type = "CodeInterface",
                        raw_element_type = "class",
                        source_verification_status = "source-verified",
                        source_path = correctedSourcePath,
                        project_name = "Tests",
                        default_search_visible = true,
                        labels = new[] { new { language = "ko", text = "보정 계약" }, new { language = "en", text = "Corrected contract" } }
                    },
                    new
                    {
                        stable_id = "urn:test:fallback",
                        canonical_name = "FallbackKind",
                        element_type = "",
                        raw_element_type = "enum",
                        source_verification_status = "raw",
                        source_path = "Tests/FallbackKind.cs",
                        project_name = "Tests",
                        default_search_visible = true
                    }
                },
                relations = new[]
                {
                    new { stable_id = "urn:test:relation", source = "urn:test:corrected", target = "urn:test:fallback", relation_type = "dependsOn" }
                }
            };
            await WriteJsonAsync(directory, "manifest.json", manifest);
            await WriteJsonAsync(directory, "instances.json", instances);
            await File.WriteAllTextAsync(Path.Combine(directory, "dreamine.schema.json"), "{\"$defs\":{}}");
            return new TemporaryOntologyFixture(directory);
        }

        public JsonOntologyRepository CreateRepository() =>
            new(new FixedOntologyDataPathResolver(DirectoryPath));

        public ValueTask DisposeAsync()
        {
            if (Directory.Exists(DirectoryPath))
                Directory.Delete(DirectoryPath, true);
            return ValueTask.CompletedTask;
        }

        private static Task WriteJsonAsync(string directory, string fileName, object value) =>
            File.WriteAllTextAsync(
                Path.Combine(directory, fileName),
                JsonSerializer.Serialize(value));
    }
}
