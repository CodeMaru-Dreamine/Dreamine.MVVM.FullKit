using DreamineWeb.KnowledgeQa.Domain;
using DreamineWeb.Ontology.Domain;

namespace DreamineWeb.KnowledgeQa.Application;

/// <summary>Persists questions and complete answer revision history.</summary>
public interface IKnowledgeQuestionRepository
{
    Task<KnowledgeQuestion> CreateAsync(KnowledgeQuestion question, CancellationToken cancellationToken);
    Task<KnowledgeQuestion?> GetAsync(long id, CancellationToken cancellationToken);
    Task<KnowledgeQuestionPage> SearchAsync(
        string query,
        string category,
        QuestionPublicationStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
    Task UpdateAsync(KnowledgeQuestion question, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(long id, CancellationToken cancellationToken);
}

/// <summary>Builds a bounded, source-verified evidence bundle before any LLM call.</summary>
public interface IEvidenceBundleBuilder
{
    Task<EvidenceBundle> BuildAsync(string question, string language, CancellationToken cancellationToken);
}

/// <summary>
/// Public application use case for retrieving structured Dreamine evidence without invoking an LLM.
/// Web, HTTP API, MCP, Codex skills, and CLI adapters may all consume this contract.
/// </summary>
public interface IKnowledgeEvidenceQueryService
{
    Task<EvidenceBundle> QueryAsync(KnowledgeEvidenceQuery query, CancellationToken cancellationToken);
}

/// <summary>Turns a natural-language question into a bounded repository search plan.</summary>
public interface IKnowledgeQuestionPlanner
{
    Task<KnowledgeSearchPlan> PlanAsync(string question, string language, CancellationToken cancellationToken);
}

public sealed record KnowledgeSearchPlan(
    string QueryKind,
    IReadOnlyList<string> SearchTerms,
    IReadOnlyList<string> ExactSymbols,
    IReadOnlyList<string> RelationTypes,
    string? Project,
    bool SuppressUnverifiedFlows)
{
    public string Intent { get; init; } = string.Empty;
    public IReadOnlyList<string> Concepts { get; init; } = [];
    public IReadOnlyList<string> SourceKinds { get; init; } = [];
    public IReadOnlyList<KnowledgeRelationConstraint> RelationConstraints { get; init; } = [];
    public KnowledgePlannerDiagnostics Diagnostics { get; init; } = new();
}

/// <summary>Represents a language-neutral evidence query from any delivery adapter.</summary>
public sealed record KnowledgeEvidenceQuery(string Query, string Language = "ko");

/// <summary>Resolves exact repository symbols before an otherwise out-of-scope request is rejected.</summary>
public interface IKnowledgeSymbolScopeResolver
{
    Task<KnowledgeSymbolScopeResolution> ResolveAsync(string question, CancellationToken cancellationToken);
}

public enum KnowledgeSymbolScopeResolutionKind
{
    None,
    Exact,
    Ambiguous
}

public sealed record KnowledgeSymbolScopeResolution(
    KnowledgeSymbolScopeResolutionKind Kind,
    string Symbol,
    int ExactNodeCount,
    int ForwardingCandidateCount);

/// <summary>Finds Doxygen declarations and summaries without copying whole HTML documents.</summary>
public interface IDoxygenEvidenceProvider
{
    Task<IReadOnlyList<EvidenceReference>> SearchAsync(
        IReadOnlyList<OntologyNode> nodes,
        string language,
        CancellationToken cancellationToken);
}

/// <summary>Generates structured answer content from an EvidenceBundle and nothing else.</summary>
public interface IKnowledgeAnswerGenerator
{
    Task<KnowledgeAnswerGenerationResult> GenerateAsync(EvidenceBundle bundle, CancellationToken cancellationToken);
}

/// <summary>Lets Codex search the read-only repository and return one structured answer with source locations.</summary>
public interface IKnowledgeRepositoryAnswerGenerator
{
    Task<RepositoryKnowledgeAnswerResult> GenerateAsync(
        string question,
        string language,
        CancellationToken cancellationToken);
}

public sealed record RepositoryKnowledgeAnswerResult(
    EvidenceBundle EvidenceBundle,
    KnowledgeAnswerGenerationResult Answer)
{
    /// <summary>Model prose retained only for an explicit user preview when source verification fails.</summary>
    public KnowledgeAnswerContent? UnverifiedDraft { get; init; }
}

public sealed class KnowledgeUnverifiedAnswerException(
    string message,
    KnowledgeAnswerContent draft) : InvalidOperationException(message)
{
    public KnowledgeAnswerContent Draft { get; } = draft;
}

/// <summary>Detects local paths, secrets, and other content that blocks automatic review submission.</summary>
public interface IKnowledgePrivacyScanner
{
    IReadOnlyList<string> Scan(string question, EvidenceBundle bundle, KnowledgeAnswerContent answer);
}

/// <summary>Rejects unrelated or unsafe requests before retrieval and enforces evidence sufficiency before LLM use.</summary>
public interface IKnowledgeRequestScopePolicy
{
    KnowledgeScopeDecision EvaluateQuestion(string question, string language);
    KnowledgeScopeDecision EvaluateEvidence(EvidenceBundle bundle, string language);
}

/// <summary>
/// Projects the complete audit-oriented evidence model into a small, human-readable answer without
/// mutating or discarding the persisted EvidenceBundle.
/// </summary>
public interface IKnowledgeAnswerProjectionService
{
    KnowledgeAnswerViewModel Project(KnowledgeQuestion question, AnswerRevision revision);
}

/// <summary>Contains a machine-readable request disposition and a user-facing explanation.</summary>
public sealed record KnowledgeScopeDecision(
    KnowledgeRequestDisposition Disposition,
    string Reason,
    string? ClarificationPrompt = null);

/// <summary>Coordinates question analysis, persistence, retrieval, and publication state.</summary>
public interface IKnowledgeQaService
{
    Task<KnowledgeQuestionCreatedViewModel> AskAsync(
        KnowledgeQuestionRequest request,
        CancellationToken cancellationToken);
    Task<KnowledgeQuestionDetailsViewModel?> GetAsync(
        long id,
        string? accessKey,
        bool includeNonPublic,
        CancellationToken cancellationToken);
    Task<KnowledgeQuestionSearchViewModel> SearchPublishedAsync(
        string query,
        string category,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
    Task<KnowledgeQuestionSearchViewModel> SearchForReviewAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken);
    Task<KnowledgeQuestionSearchViewModel> SearchForAdministrationAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<KnowledgeAccessibleQuestionViewModel>> GetAccessibleAsync(
        IReadOnlyList<KnowledgeQuestionAccessReference> references,
        CancellationToken cancellationToken);
    Task<bool> SetPublicationStatusAsync(
        long id,
        QuestionPublicationStatus status,
        CancellationToken cancellationToken);
    Task<bool> DeleteAsync(long id, CancellationToken cancellationToken);
    Task RecordViewAsync(long id, CancellationToken cancellationToken);
    Task RecordHelpfulAsync(long id, bool helpful, CancellationToken cancellationToken);
}

/// <summary>Owns long-running repository Q&amp;A work independently from a Blazor page or circuit.</summary>
public interface IKnowledgeQuestionJobService
{
    KnowledgeQuestionJobViewModel Enqueue(KnowledgeQuestionRequest request);
    KnowledgeQuestionJobViewModel? Get(Guid submissionId);
}

public enum KnowledgeQuestionJobStatus
{
    Queued,
    Running,
    Completed,
    Failed
}

public sealed record KnowledgeQuestionJobViewModel(
    Guid SubmissionId,
    string Question,
    string Language,
    KnowledgeQuestionJobStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    KnowledgeQuestionCreatedViewModel? Result = null,
    string ErrorMessage = "",
    KnowledgeAnswerContent? UnverifiedDraft = null);

public sealed record KnowledgeQuestionRequest(
    string Question,
    string Language = "ko",
    Guid? SubmissionId = null);

public sealed record KnowledgeQuestionAccessReference(long Id, string Slug, string AccessKey);

public sealed record KnowledgeAccessibleQuestionViewModel(
    long Id,
    string Slug,
    string AccessKey,
    string Question,
    string Summary,
    string Category,
    QuestionPublicationStatus PublicationStatus,
    KnowledgeRequestDisposition RequestDisposition,
    DateTimeOffset CreatedAt);

public sealed record KnowledgeAnswerGenerationResult(
    KnowledgeAnswerContent Content,
    string ModelId,
    string PromptPolicyVersion)
{
    public KnowledgeAnswerGeneratorDiagnostics Diagnostics { get; init; } = new();
}

public sealed record KnowledgeQuestionCreatedViewModel(
    long Id,
    string Slug,
    string AccessKey,
    string Url,
    QuestionPublicationStatus PublicationStatus,
    KnowledgeRequestDisposition RequestDisposition);

public sealed record KnowledgeQuestionListItemViewModel(
    long Id,
    string Slug,
    string Question,
    string Summary,
    string Category,
    KnowledgeRequestDisposition RequestDisposition,
    int EvidenceCount,
    int RelatedCodeCount,
    long ViewCount,
    long HelpfulCount,
    QuestionPublicationStatus PublicationStatus,
    string ScopeReason,
    DateTimeOffset CreatedAt,
    bool NeedsRevalidation);

public sealed record KnowledgeQuestionSearchViewModel(
    IReadOnlyList<KnowledgeQuestionListItemViewModel> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record KnowledgeQuestionDetailsViewModel(
    long Id,
    string Slug,
    string OriginalQuestion,
    string NormalizedQuestion,
    string Category,
    QuestionPublicationStatus PublicationStatus,
    KnowledgeRequestDisposition RequestDisposition,
    string ScopeReason,
    string Language,
    AnswerRevision Revision,
    KnowledgeAnswerViewModel Answer,
    QuestionMetric Metric,
    IReadOnlyList<QuestionTag> Tags,
    IReadOnlyList<string> PrivacyFindings,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    bool NeedsRevalidation,
    int RevisionCount);

/// <summary>A compact answer projection intended for people rather than validators.</summary>
public sealed record KnowledgeAnswerViewModel(
    KnowledgeRequestDisposition Disposition,
    string DirectAnswer,
    IReadOnlyList<string> Flow,
    IReadOnlyList<KnowledgeEvidenceCardViewModel> CoreEvidence,
    IReadOnlyList<KnowledgeAnswerSection> AdditionalSections,
    IReadOnlyList<string> UnverifiedStatements,
    IReadOnlyList<KnowledgeEvidenceCardViewModel> OtherProjectCandidates,
    string ScopeGuidance,
    bool ShowEvidenceSections);

/// <summary>Combines source, ontology, and Doxygen provenance for one user-facing code fact.</summary>
public sealed record KnowledgeEvidenceCardViewModel(
    string Key,
    string Title,
    string Summary,
    string Project,
    string Symbol,
    string? RelationType,
    EvidenceOrigin Origin,
    string? StableUri,
    string? RelatedStableUri,
    string? SourcePath,
    int? LineStart,
    int? LineEnd,
    string? Declaration,
    string? CodeExcerpt,
    string? DoxygenUrl,
    IReadOnlyList<string> EvidenceIds);

/// <summary>Configures Codex-backed question planning, evidence-only answers, and bounded retrieval.</summary>
public sealed class KnowledgeQaOptions
{
    public bool Enabled { get; set; } = true;
    public string Provider { get; set; } = "CodexCli";
    public string CodexExecutable { get; set; } = "codex";
    public string CodexModel { get; set; } = string.Empty;
    public string RepositoryRoot { get; set; } = string.Empty;
    public int CodexMaxConcurrency { get; set; } = 1;
    public string Endpoint { get; set; } = "http://192.168.0.100:1234/v1/";
    public string Model { get; set; } = "gemma-3-4b-it";
    public string ApiKey { get; set; } = string.Empty;
    public int RequestTimeoutSeconds { get; set; } = 600;
    public int MaximumOntologyNodes { get; set; } = 10;
    public int MaximumRelations { get; set; } = 32;
    public int MaximumDoxygenReferences { get; set; } = 10;
    public int MaximumSourceReferences { get; set; } = 6;
    public int MinimumOntologyEvidence { get; set; } = 1;
    public int MinimumDoxygenEvidence { get; set; } = 1;
    public bool IncludeDevelopmentDiagnostics { get; set; }
}
