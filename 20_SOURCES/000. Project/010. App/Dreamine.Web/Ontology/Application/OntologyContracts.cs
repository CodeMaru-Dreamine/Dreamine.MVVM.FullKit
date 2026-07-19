using DreamineWeb.Ontology.Domain;

namespace DreamineWeb.Ontology.Application;

/// <summary>Provides indexed access to generated TBox, ABox, relations, and validation data.</summary>
public interface IOntologyRepository
{
    Task<OntologyPage<OntologyNode>> SearchNodesAsync(OntologyQuery query, int page, int pageSize, CancellationToken cancellationToken);
    Task<OntologyNode?> GetNodeAsync(string stableUri, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<string, OntologyNode>> GetNodesAsync(IEnumerable<string> stableUris, CancellationToken cancellationToken);
    Task<IReadOnlyList<OntologyRelation>> GetRelationsAsync(string stableUri, CancellationToken cancellationToken);
    Task<IReadOnlyList<OntologyTBoxClass>> GetTBoxClassesAsync(CancellationToken cancellationToken);
    Task<OntologyFacets> GetFacetsAsync(CancellationToken cancellationToken);
    Task<OntologyValidationData> GetValidationDataAsync(CancellationToken cancellationToken);
    Task<OntologyLoadMetrics> GetLoadMetricsAsync(CancellationToken cancellationToken);
}

/// <summary>Provides paged ontology search and navigation use cases to Blazor.</summary>
public interface IOntologySearchService
{
    Task<OntologySearchResultViewModel> SearchAsync(OntologySearchRequest request, int page, int pageSize, CancellationToken cancellationToken);
    Task<OntologyNodeDetailsViewModel?> GetNodeAsync(string stableUri, CancellationToken cancellationToken);
    Task<IReadOnlyList<OntologyTBoxClassViewModel>> GetTBoxClassesAsync(CancellationToken cancellationToken);
    Task<OntologyFacetsViewModel> GetFacetsAsync(CancellationToken cancellationToken);
    Task<OntologyEventFlowViewModel?> GetDreamineEventSampleAsync(CancellationToken cancellationToken);
}

/// <summary>Restores original Dreamine relation meaning from dashboard projections.</summary>
public interface IOntologyRelationResolver
{
    OntologyRelationMeaning Resolve(string? originalType, string? projectionType = null);
}

/// <summary>Provides SourceAudit and SHACL validation results to the UI.</summary>
public interface IOntologyValidationService
{
    Task<OntologyValidationSummaryViewModel> GetSummaryAsync(CancellationToken cancellationToken);
}

/// <summary>Provides safe, stable-URI-based access to generated source preview artifacts.</summary>
public interface IOntologySourceService
{
    Task<OntologySourceAvailabilityViewModel> GetAvailabilityAsync(string stableUri, CancellationToken cancellationToken);
    Task<OntologySourceDocumentViewModel> GetSourceAsync(string stableUri, CancellationToken cancellationToken);
}

/// <summary>Maps repository domain records into UI-safe view models.</summary>
public interface IOntologyGraphMapper
{
    OntologyNodeItemViewModel ToNodeItem(OntologyNode node, string language = "ko");
    OntologyRelationViewModel ToRelation(
        OntologyRelation relation,
        bool isOutgoing,
        OntologyNode? relatedNode,
        OntologyRelationMeaning meaning);
    OntologyNodeDetailsViewModel ToNodeDetails(
        OntologyNode node,
        IReadOnlyList<OntologyRelationViewModel> incoming,
        IReadOnlyList<OntologyRelationViewModel> outgoing,
        string language = "ko");
    OntologyTBoxClassViewModel ToTBoxClass(OntologyTBoxClass item);
}
