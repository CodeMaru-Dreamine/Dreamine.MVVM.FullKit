using DreamineWeb.Ontology.Domain;

namespace DreamineWeb.Ontology.Application;

/// <summary>Maps current SourceAudit and SHACL reports into a single health view.</summary>
public sealed class OntologyValidationService : IOntologyValidationService
{
    private readonly IOntologyRepository _repository;

    public OntologyValidationService(IOntologyRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <inheritdoc />
    public async Task<OntologyValidationSummaryViewModel> GetSummaryAsync(CancellationToken cancellationToken)
    {
        OntologyValidationData data = await _repository.GetValidationDataAsync(cancellationToken).ConfigureAwait(false);
        OntologyLoadMetrics metrics = await _repository.GetLoadMetricsAsync(cancellationToken).ConfigureAwait(false);
        return new OntologyValidationSummaryViewModel
        {
            IsHealthy = data.IsHealthy,
            HealthReason = data.HealthReason,
            OntologyVersion = data.OntologyVersion,
            GraphVersion = data.GraphVersion,
            ContentHash = data.ContentHash,
            GeneratedAt = data.GeneratedAt,
            ElementCount = data.ElementCount,
            RelationCount = data.RelationCount,
            RdfTripleCount = data.RdfTripleCount,
            SourceVsOverlayMismatches = data.SourceVsOverlayMismatches,
            SourceDeclarationsMissingRawGraph = data.SourceDeclarationsMissingRawGraph,
            SourceDeclarationsMissingOverlay = data.SourceDeclarationsMissingOverlay,
            PartialMethodsEnriched = data.PartialMethodsEnriched,
            UnclassifiedPartialMethods = data.UnclassifiedPartialMethods,
            StalePathRemaps = data.StalePathRemaps,
            RawDanglingRelations = data.RawDanglingRelations,
            OverlayDanglingRelations = data.OverlayDanglingRelations,
            DuplicateStableUris = data.DuplicateStableUris,
            StableUriTypeConflicts = data.StableUriTypeConflicts,
            AutoCorrectedTypes = data.AutoCorrectedTypes,
            UncorrectableTypes = data.UncorrectableTypes,
            ExcludedGeneratedFiles = data.ExcludedGeneratedFiles,
            ExcludedGraphNodes = data.ExcludedGraphNodes,
            LinkmlShaclConforms = data.LinkmlShaclConforms,
            LinkmlShaclViolations = data.LinkmlShaclViolations,
            ArchitectureShaclConforms = data.ArchitectureShaclConforms,
            Shapes = data.Shapes.Select(shape => new OntologyShapeValidationViewModel(
                shape.Name,
                shape.TargetNodeCount,
                shape.ViolationCount,
                shape.PositiveFixtureConforms,
                shape.NegativeFixtureRejected,
                shape.FixtureTestPassed)).ToArray(),
            Artifacts = data.Artifacts.Select(artifact => new OntologyArtifactViewModel(
                artifact.Name,
                artifact.Exists,
                artifact.IsValid,
                artifact.IsStale,
                artifact.GeneratedAt,
                artifact.Error)).ToArray(),
            LoadMetrics = new OntologyLoadMetricsViewModel
            {
                LoadedAt = metrics.LoadedAt,
                SourceBytes = metrics.SourceBytes,
                LoadMilliseconds = metrics.LoadMilliseconds,
                ManagedMemoryDeltaMiB = metrics.ManagedMemoryDeltaMiB,
                WorkingSetDeltaMiB = metrics.WorkingSetDeltaMiB,
                CacheHits = metrics.CacheHits,
                ReloadCount = metrics.ReloadCount
            }
        };
    }
}
