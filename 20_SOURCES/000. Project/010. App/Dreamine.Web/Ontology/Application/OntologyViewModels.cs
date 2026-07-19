namespace DreamineWeb.Ontology.Application;

/// <summary>Represents the search form submitted by the Ontology page.</summary>
public sealed record OntologySearchRequest(
    string SearchText = "",
    string Type = "",
    string Project = "",
    string FilePath = "",
    string RelationType = "",
    bool IncludeExcluded = false,
    string Language = "ko");

/// <summary>Represents one paged ABox result.</summary>
public sealed record OntologyNodeItemViewModel(
    string StableUri,
    string Name,
    string QualifiedName,
    string EffectiveType,
    string? RawType,
    string SourceVerificationStatus,
    string ProjectName,
    string SourcePath,
    int? LineStart,
    string Summary,
    bool HasTypeCorrection,
    bool IsStale,
    bool IsExcluded,
    bool IsDreamineEventComponent);

/// <summary>Represents a page of UI-safe ABox results.</summary>
public sealed record OntologySearchResultViewModel(
    IReadOnlyList<OntologyNodeItemViewModel> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

/// <summary>Describes the original and projected meaning of an ontology relation.</summary>
public sealed record OntologyRelationMeaning(
    string OriginalType,
    string ProjectionType,
    bool WasProjected,
    string DisplayLabel);

/// <summary>Represents one incoming or outgoing relation in node details.</summary>
public sealed record OntologyRelationViewModel(
    string StableUri,
    bool IsOutgoing,
    string RelatedStableUri,
    string RelatedName,
    string OriginalType,
    string ProjectionType,
    bool WasProjected,
    IReadOnlyList<OntologyEvidenceViewModel> Evidence);

/// <summary>Represents provenance evidence rendered by the UI.</summary>
public sealed record OntologyEvidenceViewModel(string Source, string Value, double Confidence);

/// <summary>Represents a fully resolved ontology node and its lazy-loaded relations.</summary>
public sealed record OntologyNodeDetailsViewModel(
    OntologyNodeItemViewModel Node,
    string SourceGraphId,
    IReadOnlyList<string> Types,
    string Namespace,
    string Signature,
    string? RawSourcePath,
    int? LineEnd,
    string Summary,
    IReadOnlyList<string> Tags,
    IReadOnlyList<OntologyEvidenceViewModel> Evidence,
    IReadOnlyList<OntologyRelationViewModel> Incoming,
    IReadOnlyList<OntologyRelationViewModel> Outgoing);

/// <summary>Describes whether a source preview may be opened without exposing a local path.</summary>
public sealed record OntologySourceAvailabilityViewModel(
    bool IsAvailable,
    bool IsExcluded,
    string DisplayPath,
    string ReasonCode);

/// <summary>Represents one HTML-safe source line. Razor encodes <see cref="Text"/> when rendering.</summary>
public sealed record OntologySourceLineViewModel(int Number, string Text, bool IsHighlighted);

/// <summary>Contains a bounded source preview resolved through a stable ontology URI.</summary>
public sealed record OntologySourceDocumentViewModel(
    OntologySourceAvailabilityViewModel Availability,
    string Language,
    IReadOnlyList<OntologySourceLineViewModel> Lines,
    int? HighlightStart,
    int? HighlightEnd);

/// <summary>Represents one LinkML-generated TBox class.</summary>
public sealed record OntologyTBoxClassViewModel(
    string Name,
    string Description,
    IReadOnlyList<string> RequiredProperties,
    int PropertyCount);

/// <summary>Provides filter values without exposing repository state to Blazor.</summary>
public sealed record OntologyFacetsViewModel(
    IReadOnlyList<string> Types,
    IReadOnlyList<string> Projects,
    IReadOnlyList<string> RelationTypes,
    int VisibleIndividualCount,
    int ExcludedIndividualCount);

/// <summary>Displays the representative Dreamine Event forwarding chain.</summary>
public sealed record OntologyEventFlowViewModel(
    OntologyNodeItemViewModel ViewModel,
    OntologyNodeItemViewModel EventComponent,
    OntologyNodeItemViewModel Command,
    OntologyNodeItemViewModel TargetMethod,
    OntologyRelationViewModel ComponentRelation,
    OntologyRelationViewModel ForwardingRelation);

/// <summary>Represents one validation artifact shown by the dashboard.</summary>
public sealed record OntologyArtifactViewModel(
    string Name,
    bool Exists,
    bool IsValid,
    bool IsStale,
    DateTimeOffset? GeneratedAt,
    string? Error);

/// <summary>Represents one SHACL shape result shown by the dashboard.</summary>
public sealed record OntologyShapeValidationViewModel(
    string Name,
    int TargetNodeCount,
    int ViolationCount,
    bool PositiveFixturePassed,
    bool NegativeFixturePassed,
    bool FixtureTestPassed);

/// <summary>Represents the SourceAudit and SHACL health summary.</summary>
public sealed record OntologyValidationSummaryViewModel
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
    public IReadOnlyList<OntologyShapeValidationViewModel> Shapes { get; init; } = [];
    public IReadOnlyList<OntologyArtifactViewModel> Artifacts { get; init; } = [];
    public OntologyLoadMetricsViewModel LoadMetrics { get; init; } = new();
}

/// <summary>Reports measured server-side ontology cache cost.</summary>
public sealed record OntologyLoadMetricsViewModel
{
    public DateTimeOffset? LoadedAt { get; init; }
    public long SourceBytes { get; init; }
    public double LoadMilliseconds { get; init; }
    public double ManagedMemoryDeltaMiB { get; init; }
    public double WorkingSetDeltaMiB { get; init; }
    public long CacheHits { get; init; }
    public long ReloadCount { get; init; }
}
