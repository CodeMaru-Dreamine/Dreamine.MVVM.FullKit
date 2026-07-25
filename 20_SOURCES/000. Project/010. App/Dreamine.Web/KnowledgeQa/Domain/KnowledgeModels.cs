namespace DreamineWeb.KnowledgeQa.Domain;

/// <summary>Controls whether a stored question may appear in public lists and search.</summary>
public enum QuestionPublicationStatus
{
    Private,
    PendingReview,
    Published,
    Rejected,
    Archived
}

/// <summary>Represents the support and safety disposition decided before model generation.</summary>
public enum KnowledgeRequestDisposition
{
    Supported,
    PartiallySupported,
    NeedsClarification,
    OutOfScope,
    InsufficientEvidence,
    Restricted
}

/// <summary>Records whether one required answer-chain step is backed by verified source evidence.</summary>
public sealed record KnowledgeEvidenceCoverageStep(
    string Key,
    string Label,
    bool Covered,
    IReadOnlyList<string> EvidenceIds,
    string Detail = "");

/// <summary>Describes the required evidence chain and any steps that repository retrieval could not connect.</summary>
public sealed record KnowledgeEvidenceCoverage
{
    public bool Required { get; init; }
    public string Chain { get; init; } = string.Empty;
    public IReadOnlyList<KnowledgeEvidenceCoverageStep> Steps { get; init; } = [];
    public bool IsComplete => Required && Steps.Count > 0 && Steps.All(item => item.Covered);
    public IReadOnlyList<string> MissingSteps => Steps
        .Where(item => !item.Covered)
        .Select(item => item.Label)
        .ToArray();
}

/// <summary>Identifies the source of a verifiable answer fact.</summary>
public enum EvidenceKind
{
    OntologyNode,
    OntologyRelation,
    Doxygen,
    Source
}

/// <summary>Distinguishes asserted facts from projections and inferred facts.</summary>
public enum EvidenceOrigin
{
    Direct,
    CompatibilityProjection,
    Inferred
}

/// <summary>Captures the project, graph, and ontology versions used by an answer revision.</summary>
public sealed record KnowledgeVersionSnapshot(
    IReadOnlyList<string> ProjectVersions,
    string GraphVersion,
    string OntologyVersion,
    string OntologyHash,
    DateTimeOffset? OntologyGeneratedAt);

/// <summary>Captures one bounded Codex CLI invocation without persisting prompts or secrets.</summary>
public sealed record CodexInvocationDiagnostics
{
    public bool Attempted { get; init; }
    public bool Succeeded { get; init; }
    public int? ExitCode { get; init; }
    public bool TimedOut { get; init; }
    public bool JsonParseSucceeded { get; init; }
    public long ElapsedMilliseconds { get; init; }
    public string FailureKind { get; init; } = string.Empty;
    public string StandardOutput { get; init; } = string.Empty;
    public string StandardError { get; init; } = string.Empty;
    public string RawOutput { get; init; } = string.Empty;
}

/// <summary>Records how many candidates one bounded ontology search returned.</summary>
public sealed record KnowledgeSearchCandidateDiagnostics(string Term, int CandidateCount, int RelevantCount);

/// <summary>Records the concrete ontology request produced from one structured plan item.</summary>
public sealed record KnowledgeServerSearchRequestDiagnostics(
    string Query,
    string Purpose,
    string? Project,
    IReadOnlyList<string> SourceKinds,
    int CandidateCount,
    int RelevantCount,
    int Priority);

/// <summary>Explains why one source-verified ontology node was selected for an answer.</summary>
public sealed record KnowledgeEvidenceSelectionDiagnostics(
    string StableUri,
    string DisplayName,
    int Score,
    string Reason);

/// <summary>Constrains a requested relation to its semantic type, direction, and exact anchor.</summary>
public sealed record KnowledgeRelationConstraint(
    string RelationType,
    KnowledgeRelationDirection Direction,
    string? AnchorSymbol);

public enum KnowledgeRelationDirection
{
    Outgoing,
    Incoming
}

/// <summary>Persists the planner decision and non-silent fallback information.</summary>
public sealed record KnowledgePlannerDiagnostics
{
    public string Provider { get; init; } = "RuleFallback";
    public string FallbackReason { get; init; } = string.Empty;
    public CodexInvocationDiagnostics Codex { get; init; } = new();
}

/// <summary>Describes the complete bounded retrieval plan and selected evidence.</summary>
public sealed record KnowledgeRetrievalDiagnostics
{
    public string Intent { get; init; } = string.Empty;
    public string Project { get; init; } = string.Empty;
    public IReadOnlyList<string> ExactSymbols { get; init; } = [];
    public IReadOnlyList<string> Concepts { get; init; } = [];
    public IReadOnlyList<string> SearchTerms { get; init; } = [];
    public IReadOnlyList<string> SourceKinds { get; init; } = [];
    public IReadOnlyList<string> RequestedRelations { get; init; } = [];
    public IReadOnlyList<KnowledgeRelationConstraint> RelationConstraints { get; init; } = [];
    public IReadOnlyList<KnowledgeSearchCandidateDiagnostics> Searches { get; init; } = [];
    public IReadOnlyList<KnowledgeServerSearchRequestDiagnostics> ServerRequests { get; init; } = [];
    public IReadOnlyList<KnowledgeEvidenceSelectionDiagnostics> Selections { get; init; } = [];
    public KnowledgePlannerDiagnostics Planner { get; init; } = new();
}

/// <summary>Persists which answer generator produced the stored structured answer.</summary>
public sealed record KnowledgeAnswerGeneratorDiagnostics
{
    public string Provider { get; init; } = "RuleFallback";
    public string FallbackReason { get; init; } = string.Empty;
    public CodexInvocationDiagnostics Codex { get; init; } = new();
}

/// <summary>Joins scope, retrieval, and answer diagnostics for one immutable answer revision.</summary>
public sealed record KnowledgeExecutionDiagnostics
{
    public string ScopeEvaluator { get; init; } = "RulePolicy";
    public KnowledgeRequestDisposition ScopeDisposition { get; init; }
    public string ScopeReason { get; init; } = string.Empty;
    public KnowledgeRetrievalDiagnostics Retrieval { get; init; } = new();
    public KnowledgeAnswerGeneratorDiagnostics AnswerGenerator { get; init; } = new();
    public bool FallbackUsed { get; init; }
    public string FallbackReason { get; init; } = string.Empty;
    public string StoredDirectAnswerProducer { get; init; } = "RulePolicy";
}

/// <summary>Represents one server-verified fact that may be cited by the language model.</summary>
public sealed record EvidenceReference
{
    public required string Id { get; init; }
    public required EvidenceKind Kind { get; init; }
    public required EvidenceOrigin Origin { get; init; }
    public required string Title { get; init; }
    public string Summary { get; init; } = string.Empty;
    public string? StableUri { get; init; }
    public string? RelatedStableUri { get; init; }
    public string? RelationType { get; init; }
    public string? ProjectionType { get; init; }
    public string? DoxygenUrl { get; init; }
    public bool DoxygenUrlValidated { get; init; }
    public string? SourcePath { get; init; }
    public int? LineStart { get; init; }
    public int? LineEnd { get; init; }
    public string? Declaration { get; init; }
    public string? CodeExcerpt { get; init; }
    public string Provenance { get; init; } = string.Empty;
    public double Confidence { get; init; } = 1d;
}

/// <summary>The only code knowledge supplied to a language model for one question.</summary>
public sealed record EvidenceBundle(
    string Question,
    string NormalizedQuestion,
    IReadOnlyList<EvidenceReference> Evidence,
    KnowledgeVersionSnapshot Version,
    DateTimeOffset CreatedAt)
{
    public KnowledgeRetrievalDiagnostics RetrievalDiagnostics { get; init; } = new();
    public KnowledgeEvidenceCoverage Coverage { get; init; } = new();

    public int OntologyEvidenceCount => Evidence.Count(item =>
        item.Kind is EvidenceKind.OntologyNode or EvidenceKind.OntologyRelation);

    public int DoxygenReferenceCount => Evidence.Count(item => item.Kind is EvidenceKind.Doxygen);

    public int SourceReferenceCount => Evidence.Count(item => item.Kind is EvidenceKind.Source);
}

/// <summary>Represents one structured answer section and its verified citations.</summary>
public sealed record KnowledgeAnswerSection(
    string Heading,
    string Body,
    IReadOnlyList<string> EvidenceIds);

/// <summary>Represents structured, HTML-independent answer content.</summary>
public sealed record KnowledgeAnswerContent(
    string Summary,
    IReadOnlyList<KnowledgeAnswerSection> Sections,
    IReadOnlyList<string> RelatedComponents,
    IReadOnlyList<string> UnverifiedStatements,
    IReadOnlyList<string> EvidenceIds);

/// <summary>Preserves one immutable answer generation result.</summary>
public sealed record AnswerRevision
{
    public int Revision { get; init; }
    public required KnowledgeAnswerContent Content { get; init; }
    public required EvidenceBundle EvidenceBundle { get; init; }
    public required KnowledgeVersionSnapshot Version { get; init; }
    public required string PromptPolicyVersion { get; init; }
    public required string ModelId { get; init; }
    public KnowledgeExecutionDiagnostics ExecutionDiagnostics { get; init; } = new();
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset LastValidatedAt { get; init; }
}

/// <summary>Groups the revision history for one question answer.</summary>
public sealed record KnowledgeAnswer
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public int CurrentRevision { get; init; }
    public IReadOnlyList<AnswerRevision> Revisions { get; init; } = [];
}

/// <summary>Stores one searchable question category or topic label.</summary>
public sealed record QuestionTag(string Value);

/// <summary>Stores durable counters without exposing question author information.</summary>
public sealed record QuestionMetric
{
    public long ViewCount { get; init; }
    public long HelpfulCount { get; init; }
    public long NotHelpfulCount { get; init; }
}

/// <summary>Represents one permanently stored code-knowledge question.</summary>
public sealed record KnowledgeQuestion
{
    public long Id { get; init; }
    public required string Slug { get; init; }
    public required string OriginalQuestion { get; init; }
    public required string NormalizedQuestion { get; init; }
    public required string Summary { get; init; }
    public required string Category { get; init; }
    public string Language { get; init; } = "ko";
    public KnowledgeRequestDisposition RequestDisposition { get; init; } = KnowledgeRequestDisposition.Supported;
    public string ScopeReason { get; init; } = string.Empty;
    public QuestionPublicationStatus PublicationStatus { get; init; }
    public required string AccessKeyHash { get; init; }
    public IReadOnlyList<QuestionTag> Tags { get; init; } = [];
    public required KnowledgeAnswer Answer { get; init; }
    public QuestionMetric Metric { get; init; } = new();
    public IReadOnlyList<string> PrivacyFindings { get; init; } = [];
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

/// <summary>Represents a page of persisted questions.</summary>
public sealed record KnowledgeQuestionPage(
    IReadOnlyList<KnowledgeQuestion> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling((double)TotalCount / PageSize);
}
