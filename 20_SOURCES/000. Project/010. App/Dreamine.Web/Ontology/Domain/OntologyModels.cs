namespace DreamineWeb.Ontology.Domain;

/// <summary>Represents localized ontology text.</summary>
public sealed record OntologyLocalizedText(string Language, string Text);

/// <summary>Represents provenance evidence attached to an ontology element or relation.</summary>
public sealed record OntologyEvidence(string Source, string Value, double Confidence);

/// <summary>Represents a source-verified ABox individual.</summary>
public sealed record OntologyNode
{
    public required string StableUri { get; init; }
    public string SourceGraphId { get; init; } = string.Empty;
    public required string CanonicalName { get; init; }
    public string QualifiedName { get; init; } = string.Empty;
    public IReadOnlyList<string> Types { get; init; } = [];
    public required string EffectiveType { get; init; }
    public string? RawType { get; init; }
    public string SourceVerificationStatus { get; init; } = "raw";
    public string Layer { get; init; } = string.Empty;
    public string SourcePath { get; init; } = string.Empty;
    public string? RawSourcePath { get; init; }
    public int? LineStart { get; init; }
    public int? LineEnd { get; init; }
    public string Namespace { get; init; } = string.Empty;
    public string Signature { get; init; } = string.Empty;
    public string ProjectName { get; init; } = string.Empty;
    public string PackageId { get; init; } = string.Empty;
    public string ProjectVersion { get; init; } = string.Empty;
    public bool DefaultSearchVisible { get; init; } = true;
    public bool IsStale { get; init; }
    public bool IsExcluded { get; init; }
    public IReadOnlyList<OntologyLocalizedText> Labels { get; init; } = [];
    public IReadOnlyList<OntologyLocalizedText> Summaries { get; init; } = [];
    public IReadOnlyList<string> TargetFrameworks { get; init; } = [];
    public IReadOnlyList<string> Tags { get; init; } = [];
    public IReadOnlyList<OntologyEvidence> Evidence { get; init; } = [];
}

/// <summary>Represents an original Dreamine semantic relation between ABox individuals.</summary>
public sealed record OntologyRelation
{
    public required string StableUri { get; init; }
    public required string SourceUri { get; init; }
    public required string TargetUri { get; init; }
    public required string OriginalType { get; init; }
    public IReadOnlyList<OntologyEvidence> Evidence { get; init; } = [];
}

/// <summary>Represents a TBox class generated from the LinkML single source.</summary>
public sealed record OntologyTBoxClass(
    string Name,
    string Description,
    IReadOnlyList<string> RequiredProperties,
    int PropertyCount);

/// <summary>Specifies server-side ABox search criteria.</summary>
public sealed record OntologyQuery(
    string SearchText = "",
    string Type = "",
    string Project = "",
    string FilePath = "",
    string RelationType = "",
    bool IncludeExcluded = false);

/// <summary>Represents a page of ontology results.</summary>
public sealed record OntologyPage<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling((double)TotalCount / PageSize);
}

/// <summary>Provides values used by ontology search filters.</summary>
public sealed record OntologyFacets(
    IReadOnlyList<string> Types,
    IReadOnlyList<string> Projects,
    IReadOnlyList<string> RelationTypes,
    int VisibleIndividualCount,
    int ExcludedIndividualCount);

/// <summary>Describes the availability and freshness of one generated ontology artifact.</summary>
public sealed record OntologyArtifactState(
    string Name,
    string Path,
    bool Exists,
    bool IsValid,
    bool IsStale,
    DateTimeOffset? GeneratedAt,
    string? Error);

/// <summary>Represents one architecture SHACL shape result and its fixtures.</summary>
public sealed record OntologyShapeValidation(
    string Name,
    int TargetNodeCount,
    int ViolationCount,
    bool PositiveFixtureConforms,
    bool NegativeFixtureRejected,
    bool FixtureTestPassed);

/// <summary>Contains parsed SourceAudit, SHACL, manifest, and cache health data.</summary>
public sealed record OntologyValidationData
{
    public bool IsHealthy { get; init; }
    public string HealthReason { get; init; } = string.Empty;
    public string OntologyVersion { get; init; } = string.Empty;
    public string GraphVersion { get; init; } = string.Empty;
    public string ContentHash { get; init; } = string.Empty;
    public DateTimeOffset? GeneratedAt { get; init; }
    public int ElementCount { get; init; }
    public int RelationCount { get; init; }
    public int RdfTripleCount { get; init; }
    public int SourceVsOverlayMismatches { get; init; }
    public int SourceDeclarationsMissingRawGraph { get; init; }
    public int SourceDeclarationsMissingOverlay { get; init; }
    public int PartialMethodsEnriched { get; init; }
    public int UnclassifiedPartialMethods { get; init; }
    public int StalePathRemaps { get; init; }
    public int RawDanglingRelations { get; init; }
    public int OverlayDanglingRelations { get; init; }
    public int DuplicateStableUris { get; init; }
    public int StableUriTypeConflicts { get; init; }
    public int AutoCorrectedTypes { get; init; }
    public int UncorrectableTypes { get; init; }
    public int ExcludedGeneratedFiles { get; init; }
    public int ExcludedGraphNodes { get; init; }
    public bool LinkmlShaclConforms { get; init; }
    public int LinkmlShaclViolations { get; init; }
    public bool ArchitectureShaclConforms { get; init; }
    public IReadOnlyList<OntologyShapeValidation> Shapes { get; init; } = [];
    public IReadOnlyList<OntologyArtifactState> Artifacts { get; init; } = [];
}

/// <summary>Reports server-side ontology cache load and reuse measurements.</summary>
public sealed record OntologyLoadMetrics(
    DateTimeOffset? LoadedAt,
    long SourceBytes,
    int ElementCount,
    int RelationCount,
    double LoadMilliseconds,
    double ManagedMemoryDeltaMiB,
    double WorkingSetDeltaMiB,
    long CacheHits,
    long ReloadCount);
