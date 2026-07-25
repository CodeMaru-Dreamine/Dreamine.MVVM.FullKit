using DreamineWeb.KnowledgeQa.Domain;
using DreamineWeb.Ontology.Application;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DreamineWeb.KnowledgeQa.Application;

/// <summary>Implements the evidence-first question-to-permanent-analysis-page use case.</summary>
public sealed partial class KnowledgeQaService : IKnowledgeQaService
{
    private static readonly TimeSpan SubmissionRetention = TimeSpan.FromMinutes(5);
    private const int MaximumTrackedSubmissions = 512;
    private readonly IKnowledgeQuestionRepository _repository;
    private readonly IKnowledgeEvidenceQueryService _evidenceQuery;
    private readonly IKnowledgeAnswerGenerator _answerGenerator;
    private readonly IKnowledgeRequestScopePolicy _scopePolicy;
    private readonly IKnowledgeAnswerProjectionService _projectionService;
    private readonly IKnowledgePrivacyScanner _privacyScanner;
    private readonly IOntologyValidationService _validationService;
    private readonly IKnowledgeSymbolScopeResolver? _symbolScopeResolver;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<KnowledgeQaService> _logger;
    private readonly IKnowledgeRepositoryAnswerGenerator? _repositoryAnswerGenerator;
    private readonly KnowledgeQaOptions _options;
    private readonly ConcurrentDictionary<Guid, SubmissionOperation> _submissions = [];

    public KnowledgeQaService(
        IKnowledgeQuestionRepository repository,
        IKnowledgeEvidenceQueryService evidenceQuery,
        IKnowledgeAnswerGenerator answerGenerator,
        IKnowledgeRequestScopePolicy scopePolicy,
        IKnowledgeAnswerProjectionService projectionService,
        IKnowledgePrivacyScanner privacyScanner,
        IOntologyValidationService validationService,
        IKnowledgeSymbolScopeResolver? symbolScopeResolver = null,
        TimeProvider? timeProvider = null,
        ILogger<KnowledgeQaService>? logger = null,
        IKnowledgeRepositoryAnswerGenerator? repositoryAnswerGenerator = null,
        KnowledgeQaOptions? options = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _evidenceQuery = evidenceQuery ?? throw new ArgumentNullException(nameof(evidenceQuery));
        _answerGenerator = answerGenerator ?? throw new ArgumentNullException(nameof(answerGenerator));
        _scopePolicy = scopePolicy ?? throw new ArgumentNullException(nameof(scopePolicy));
        _projectionService = projectionService ?? throw new ArgumentNullException(nameof(projectionService));
        _privacyScanner = privacyScanner ?? throw new ArgumentNullException(nameof(privacyScanner));
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _symbolScopeResolver = symbolScopeResolver;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _logger = logger ?? NullLogger<KnowledgeQaService>.Instance;
        _repositoryAnswerGenerator = repositoryAnswerGenerator;
        _options = options ?? new KnowledgeQaOptions();
    }

    /// <inheritdoc />
    public async Task<KnowledgeQuestionCreatedViewModel> AskAsync(
        KnowledgeQuestionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.SubmissionId is not Guid submissionId || submissionId == Guid.Empty)
            return await AskCoreAsync(request, cancellationToken).ConfigureAwait(false);

        PruneSubmissionOperations();
        SubmissionOperation candidate = new(
            new Lazy<Task<KnowledgeQuestionCreatedViewModel>>(
                () => AskCoreAsync(request, cancellationToken),
                LazyThreadSafetyMode.ExecutionAndPublication),
            _timeProvider.GetUtcNow());
        SubmissionOperation operation = _submissions.GetOrAdd(submissionId, candidate);
        try
        {
            return await operation.Task.Value.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            if (_submissions.TryGetValue(submissionId, out SubmissionOperation? current)
                && ReferenceEquals(current, operation))
            {
                _submissions.TryRemove(submissionId, out _);
            }
            throw;
        }
    }

    private async Task<KnowledgeQuestionCreatedViewModel> AskCoreAsync(
        KnowledgeQuestionRequest request,
        CancellationToken cancellationToken)
    {
        string question = request.Question.Trim();
        if (question.Length is < 5 or > 1000)
            throw new ArgumentException("Question must contain between 5 and 1,000 characters.", nameof(request));
        string language = request.Language.Equals("en", StringComparison.OrdinalIgnoreCase) ? "en" : "ko";
        KnowledgeScopeDecision scope = _scopePolicy.EvaluateQuestion(question, language);
        bool hasPlannedSymbol = DeterministicKnowledgeSearchPlan.Create(question).ExactSymbols.Count > 0;
        if (_repositoryAnswerGenerator is null
            && _symbolScopeResolver is not null
            && (scope.Disposition == KnowledgeRequestDisposition.OutOfScope || hasPlannedSymbol))
        {
            KnowledgeSymbolScopeResolution symbolScope = await _symbolScopeResolver.ResolveAsync(
                question, cancellationToken).ConfigureAwait(false);
            if (symbolScope.Kind == KnowledgeSymbolScopeResolutionKind.Exact
                && scope.Disposition == KnowledgeRequestDisposition.OutOfScope)
            {
                scope = new KnowledgeScopeDecision(KnowledgeRequestDisposition.Supported, string.Empty);
            }
            else if (symbolScope.Kind == KnowledgeSymbolScopeResolutionKind.Ambiguous)
            {
                bool koreanScope = !language.Equals("en", StringComparison.OrdinalIgnoreCase);
                scope = new KnowledgeScopeDecision(
                    KnowledgeRequestDisposition.NeedsClarification,
                    koreanScope
                        ? $"'{symbolScope.Symbol}' 심볼이 여러 후보와 일치하여 프로젝트나 타입 범위가 필요합니다."
                        : $"The symbol '{symbolScope.Symbol}' matches multiple candidates and needs a project or type scope.",
                    koreanScope
                        ? "프로젝트명 또는 전체 타입·멤버 이름을 함께 입력해 주세요."
                        : "Include the project name or the fully qualified type/member name.");
            }
        }
        EvidenceBundle bundle;
        KnowledgeAnswerGenerationResult generated;
        if (scope.Disposition == KnowledgeRequestDisposition.Supported)
        {
            if (_repositoryAnswerGenerator is not null)
            {
                RepositoryKnowledgeAnswerResult repositoryAnswer = await _repositoryAnswerGenerator.GenerateAsync(
                    question, language, cancellationToken).ConfigureAwait(false);
                bundle = repositoryAnswer.EvidenceBundle;
                generated = repositoryAnswer.Answer;
                if (generated.ModelId.Equals("repository-search-gate", StringComparison.Ordinal))
                {
                    int timeoutSeconds = Math.Clamp(_options.RequestTimeoutSeconds, 15, 600);
                    string failure = generated.Diagnostics.FallbackReason;
                    bool korean = !language.Equals("en", StringComparison.OrdinalIgnoreCase);
                    string message = failure switch
                    {
                        "timeout" => korean
                            ? $"저장소 답변을 {timeoutSeconds}초 안에 만들지 못했습니다. 질문은 저장하지 않았습니다."
                            : $"The repository answer was not completed within {timeoutSeconds} seconds. Nothing was saved.",
                        "start-failed" => korean
                            ? "서버에서 Codex CLI를 시작하지 못했습니다. 실행 파일 설정을 확인해 주세요. 질문은 저장하지 않았습니다."
                            : "The server could not start Codex CLI. Check the executable configuration. Nothing was saved.",
                        "nonzero-exit" => korean
                            ? "Codex CLI가 오류 코드로 종료되었습니다. 서버 진단 로그를 확인해 주세요. 질문은 저장하지 않았습니다."
                            : "Codex CLI exited with an error. Check the server diagnostics. Nothing was saved.",
                        "invalid-answer" or "JsonException" => korean
                            ? "Codex 응답 형식이 올바르지 않아 답변을 저장하지 않았습니다."
                            : "The Codex response format was invalid, so the answer was not saved.",
                        "no-valid-sources" => korean
                            ? "생성된 답변에 포함된 파일 경로 또는 줄 번호가 현재 저장소와 일치하지 않아 저장하지 않았습니다. 잠시 후 다시 시도해주세요."
                            : "The file paths or line numbers in the generated answer did not match the current repository, so it was not saved. Please try again shortly.",
                        _ => korean
                            ? $"저장소 답변 생성이 즉시 실패했습니다 ({failure}). 질문은 저장하지 않았습니다."
                            : $"Repository answer generation failed immediately ({failure}). Nothing was saved."
                    };
                    if (failure.Equals("no-valid-sources", StringComparison.Ordinal)
                        && repositoryAnswer.UnverifiedDraft is { } draft)
                    {
                        throw new KnowledgeUnverifiedAnswerException(message, draft);
                    }
                    throw new InvalidOperationException(message);
                }
                if (bundle.Coverage.Required && !bundle.Coverage.IsComplete)
                {
                    bool korean = !language.Equals("en", StringComparison.OrdinalIgnoreCase);
                    string missing = string.Join(", ", bundle.Coverage.MissingSteps);
                    scope = new KnowledgeScopeDecision(
                        KnowledgeRequestDisposition.PartiallySupported,
                        korean
                            ? $"실행 경로의 일부만 검증되었습니다. 누락 단계: {missing}"
                            : $"Only part of the execution path was verified. Missing steps: {missing}");
                }
            }
            else
            {
                bundle = await _evidenceQuery.QueryAsync(
                    new KnowledgeEvidenceQuery(question, language), cancellationToken).ConfigureAwait(false);
                scope = _scopePolicy.EvaluateEvidence(bundle, language);
                generated = scope.Disposition == KnowledgeRequestDisposition.Supported
                    ? await _answerGenerator.GenerateAsync(bundle, cancellationToken).ConfigureAwait(false)
                    : CreatePolicyAnswer(scope, language);
            }
        }
        else
        {
            OntologyValidationSummaryViewModel validation = await _validationService.GetSummaryAsync(cancellationToken)
                .ConfigureAwait(false);
            bundle = new EvidenceBundle(
                question,
                KnowledgeEvidenceBundleBuilder.NormalizeQuestion(question),
                [],
                new KnowledgeVersionSnapshot(
                    [], validation.GraphVersion, validation.OntologyVersion, validation.ContentHash, validation.GeneratedAt),
                _timeProvider.GetUtcNow());
            generated = CreatePolicyAnswer(scope, language);
        }
        IReadOnlyList<string> privacyFindings = _privacyScanner.Scan(question, bundle, generated.Content);
        DateTimeOffset now = _timeProvider.GetUtcNow();
        string accessKey = CreateAccessKey();
        KnowledgeExecutionDiagnostics executionDiagnostics = BuildExecutionDiagnostics(
            scope,
            bundle.RetrievalDiagnostics,
            generated.Diagnostics);
        LogExecutionDiagnostics(executionDiagnostics);
        AnswerRevision revision = new()
        {
            Revision = 1,
            Content = generated.Content,
            EvidenceBundle = bundle,
            Version = bundle.Version,
            PromptPolicyVersion = generated.PromptPolicyVersion,
            ModelId = generated.ModelId,
            ExecutionDiagnostics = executionDiagnostics,
            CreatedAt = now,
            LastValidatedAt = now
        };
        string category = Classify(question, bundle);
        KnowledgeQuestion pending = new()
        {
            Id = 0,
            Slug = CreateSlug(question),
            OriginalQuestion = question,
            NormalizedQuestion = bundle.NormalizedQuestion,
            Summary = scope.Disposition == KnowledgeRequestDisposition.PartiallySupported
                ? scope.Reason
                : generated.Content.Summary,
            Category = category,
            Language = language,
            RequestDisposition = scope.Disposition,
            ScopeReason = scope.Reason,
            PublicationStatus = IsPublicationEligible(scope.Disposition, privacyFindings, revision)
                ? QuestionPublicationStatus.PendingReview
                : QuestionPublicationStatus.Private,
            AccessKeyHash = HashAccessKey(accessKey),
            Tags = BuildTags(question, category),
            Answer = new KnowledgeAnswer
            {
                CurrentRevision = 1,
                Revisions = [revision]
            },
            PrivacyFindings = privacyFindings,
            CreatedAt = now,
            UpdatedAt = now
        };
        KnowledgeQuestion created = await _repository.CreateAsync(pending, cancellationToken).ConfigureAwait(false);
        string url = $"/questions/{created.Id}/{created.Slug}?access={accessKey}";
        return new KnowledgeQuestionCreatedViewModel(
            created.Id,
            created.Slug,
            accessKey,
            url,
            created.PublicationStatus,
            created.RequestDisposition);
    }

    private void PruneSubmissionOperations()
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();
        foreach ((Guid key, SubmissionOperation operation) in _submissions)
        {
            bool expired = now - operation.CreatedAt >= SubmissionRetention;
            bool overCapacity = _submissions.Count > MaximumTrackedSubmissions;
            if ((expired || overCapacity)
                && operation.Task.IsValueCreated
                && operation.Task.Value.IsCompleted)
            {
                _submissions.TryRemove(key, out _);
            }
        }
    }

    /// <inheritdoc />
    public async Task<KnowledgeQuestionDetailsViewModel?> GetAsync(
        long id,
        string? accessKey,
        bool includeNonPublic,
        CancellationToken cancellationToken)
    {
        KnowledgeQuestion? question = await _repository.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (question is null)
            return null;
        bool authorized = question.PublicationStatus == QuestionPublicationStatus.Published
            || includeNonPublic
            || VerifyAccessKey(accessKey, question.AccessKeyHash);
        if (!authorized)
            return null;
        AnswerRevision? revision = question.Answer.Revisions.FirstOrDefault(item =>
            item.Revision == question.Answer.CurrentRevision) ?? question.Answer.Revisions.LastOrDefault();
        if (revision is null)
            return null;
        OntologyValidationSummaryViewModel validation = await _validationService.GetSummaryAsync(cancellationToken)
            .ConfigureAwait(false);
        KnowledgeAnswerViewModel answer = _projectionService.Project(question, revision);
        return new KnowledgeQuestionDetailsViewModel(
            question.Id,
            question.Slug,
            question.OriginalQuestion,
            question.NormalizedQuestion,
            question.Category,
            question.PublicationStatus,
            question.RequestDisposition,
            question.ScopeReason,
            question.Language,
            revision,
            answer,
            question.Metric,
            question.Tags,
            question.PrivacyFindings,
            question.CreatedAt,
            question.UpdatedAt,
            NeedsRevalidation(revision, validation),
            question.Answer.Revisions.Count);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<KnowledgeAccessibleQuestionViewModel>> GetAccessibleAsync(
        IReadOnlyList<KnowledgeQuestionAccessReference> references,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(references);
        List<KnowledgeAccessibleQuestionViewModel> result = [];
        foreach (KnowledgeQuestionAccessReference reference in references
                     .DistinctBy(item => item.Id)
                     .Take(100))
        {
            cancellationToken.ThrowIfCancellationRequested();
            KnowledgeQuestion? question = await _repository.GetAsync(reference.Id, cancellationToken).ConfigureAwait(false);
            if (question is null || !VerifyAccessKey(reference.AccessKey, question.AccessKeyHash))
                continue;
            AnswerRevision? revision = question.Answer.Revisions.FirstOrDefault(item =>
                item.Revision == question.Answer.CurrentRevision) ?? question.Answer.Revisions.LastOrDefault();
            if (revision is null
                || revision.ModelId.Equals("repository-search-gate", StringComparison.Ordinal))
                continue;
            KnowledgeAnswerViewModel answer = _projectionService.Project(question, revision);
            result.Add(new KnowledgeAccessibleQuestionViewModel(
                question.Id,
                question.Slug,
                reference.AccessKey,
                question.OriginalQuestion,
                answer.DirectAnswer,
                question.Category,
                question.PublicationStatus,
                question.RequestDisposition,
                question.CreatedAt));
        }
        return result.OrderByDescending(item => item.CreatedAt).ToArray();
    }

    /// <inheritdoc />
    public async Task<KnowledgeQuestionSearchViewModel> SearchPublishedAsync(
        string query,
        string category,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        KnowledgeQuestionPage result = await _repository.SearchAsync(
            query,
            category,
            QuestionPublicationStatus.Published,
            page,
            pageSize,
            cancellationToken).ConfigureAwait(false);
        return await MapPageAsync(result, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<KnowledgeQuestionSearchViewModel> SearchForReviewAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        KnowledgeQuestionPage result = await _repository.SearchAsync(
            string.Empty,
            string.Empty,
            QuestionPublicationStatus.PendingReview,
            page,
            pageSize,
            cancellationToken).ConfigureAwait(false);
        return await MapPageAsync(result, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<KnowledgeQuestionSearchViewModel> SearchForAdministrationAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        KnowledgeQuestionPage result = await _repository.SearchAsync(
            string.Empty,
            string.Empty,
            null,
            page,
            pageSize,
            cancellationToken).ConfigureAwait(false);
        return await MapPageAsync(result, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> SetPublicationStatusAsync(
        long id,
        QuestionPublicationStatus status,
        CancellationToken cancellationToken)
    {
        KnowledgeQuestion? question = await _repository.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (question is null)
            return false;
        if (status == QuestionPublicationStatus.Published)
        {
            AnswerRevision? revision = question.Answer.Revisions.FirstOrDefault(item =>
                item.Revision == question.Answer.CurrentRevision) ?? question.Answer.Revisions.LastOrDefault();
            if (revision is null
                || !IsPublicationEligible(question.RequestDisposition, question.PrivacyFindings, revision))
            {
                return false;
            }
        }
        await _repository.UpdateAsync(
            question with { PublicationStatus = status, UpdatedAt = _timeProvider.GetUtcNow() },
            cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(long id, CancellationToken cancellationToken) =>
        _repository.DeleteAsync(id, cancellationToken);

    /// <inheritdoc />
    public async Task RecordViewAsync(long id, CancellationToken cancellationToken)
    {
        KnowledgeQuestion? question = await _repository.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (question is null)
            return;
        await _repository.UpdateAsync(question with
        {
            Metric = question.Metric with { ViewCount = question.Metric.ViewCount + 1 }
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RecordHelpfulAsync(long id, bool helpful, CancellationToken cancellationToken)
    {
        KnowledgeQuestion? question = await _repository.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (question is null)
            return;
        QuestionMetric metric = helpful
            ? question.Metric with { HelpfulCount = question.Metric.HelpfulCount + 1 }
            : question.Metric with { NotHelpfulCount = question.Metric.NotHelpfulCount + 1 };
        await _repository.UpdateAsync(question with { Metric = metric }, cancellationToken).ConfigureAwait(false);
    }

    private async Task<KnowledgeQuestionSearchViewModel> MapPageAsync(
        KnowledgeQuestionPage page,
        CancellationToken cancellationToken)
    {
        OntologyValidationSummaryViewModel validation = await _validationService.GetSummaryAsync(cancellationToken)
            .ConfigureAwait(false);
        KnowledgeQuestionListItemViewModel[] items = page.Items.Select(question =>
        {
            AnswerRevision? revision = question.Answer.Revisions.FirstOrDefault(item =>
                item.Revision == question.Answer.CurrentRevision) ?? question.Answer.Revisions.LastOrDefault();
            int relatedCode = revision?.EvidenceBundle.Evidence
                .Where(item => !string.IsNullOrWhiteSpace(item.StableUri))
                .Select(item => item.StableUri).Distinct(StringComparer.Ordinal).Count() ?? 0;
            return new KnowledgeQuestionListItemViewModel(
                question.Id,
                question.Slug,
                question.OriginalQuestion,
                revision is null ? question.Summary : _projectionService.Project(question, revision).DirectAnswer,
                question.Category,
                question.RequestDisposition,
                revision?.EvidenceBundle.Evidence.Count ?? 0,
                relatedCode,
                question.Metric.ViewCount,
                question.Metric.HelpfulCount,
                question.PublicationStatus,
                question.ScopeReason,
                question.CreatedAt,
                revision is not null && NeedsRevalidation(revision, validation));
        }).ToArray();
        return new KnowledgeQuestionSearchViewModel(
            items, page.Page, page.PageSize, page.TotalCount, page.TotalPages);
    }

    private static KnowledgeAnswerGenerationResult CreatePolicyAnswer(
        KnowledgeScopeDecision decision,
        string language)
    {
        bool korean = !language.Equals("en", StringComparison.OrdinalIgnoreCase);
        string heading = decision.Disposition switch
        {
            KnowledgeRequestDisposition.PartiallySupported => korean ? "일부 근거만 확인되었습니다" : "Partially supported",
            KnowledgeRequestDisposition.NeedsClarification => korean ? "확인이 필요합니다" : "Clarification required",
            KnowledgeRequestDisposition.OutOfScope => korean ? "지원 범위 밖의 질문입니다" : "Out of scope",
            KnowledgeRequestDisposition.InsufficientEvidence => korean ? "근거가 부족합니다" : "Insufficient evidence",
            KnowledgeRequestDisposition.Restricted => korean ? "지원할 수 없는 요청입니다" : "Restricted request",
            _ => korean ? "요청 상태" : "Request status"
        };
        string body = string.IsNullOrWhiteSpace(decision.ClarificationPrompt)
            ? decision.Reason
            : $"{decision.Reason}\n{decision.ClarificationPrompt}";
        KnowledgeAnswerContent content = new(
            decision.Reason,
            [new KnowledgeAnswerSection(heading, body, [])],
            [],
            [decision.Reason],
            []);
        return new KnowledgeAnswerGenerationResult(content, "scope-policy", "dreamine-request-scope-v1")
        {
            Diagnostics = new KnowledgeAnswerGeneratorDiagnostics
            {
                Provider = "RulePolicy",
                FallbackReason = decision.Disposition.ToString()
            }
        };
    }

    private static KnowledgeExecutionDiagnostics BuildExecutionDiagnostics(
        KnowledgeScopeDecision scope,
        KnowledgeRetrievalDiagnostics retrieval,
        KnowledgeAnswerGeneratorDiagnostics answer)
    {
        List<string> fallbackReasons = [];
        if (retrieval.Planner.Provider.Equals("RuleFallback", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(retrieval.Planner.FallbackReason))
        {
            fallbackReasons.Add($"planner:{retrieval.Planner.FallbackReason}");
        }
        if (answer.Provider.Equals("RuleFallback", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(answer.FallbackReason))
        {
            fallbackReasons.Add($"answer:{answer.FallbackReason}");
        }
        return new KnowledgeExecutionDiagnostics
        {
            ScopeEvaluator = "RulePolicy",
            ScopeDisposition = scope.Disposition,
            ScopeReason = scope.Reason,
            Retrieval = retrieval,
            AnswerGenerator = answer,
            FallbackUsed = fallbackReasons.Count > 0,
            FallbackReason = string.Join("; ", fallbackReasons),
            StoredDirectAnswerProducer = scope.Disposition != KnowledgeRequestDisposition.Supported
                ? "RulePolicy"
                : answer.Provider
        };
    }

    private static bool IsPublicationEligible(
        KnowledgeRequestDisposition disposition,
        IReadOnlyList<string> privacyFindings,
        AnswerRevision revision)
    {
        KnowledgeExecutionDiagnostics diagnostics = revision.ExecutionDiagnostics;
        string answerProvider = diagnostics.AnswerGenerator.Provider;
        string directAnswerProducer = diagnostics.StoredDirectAnswerProducer;
        return disposition == KnowledgeRequestDisposition.Supported
            && privacyFindings.Count == 0
            && !diagnostics.FallbackUsed
            && !answerProvider.Equals("RuleFallback", StringComparison.OrdinalIgnoreCase)
            && !answerProvider.Equals("RulePolicy", StringComparison.OrdinalIgnoreCase)
            && !directAnswerProducer.Equals("RuleFallback", StringComparison.OrdinalIgnoreCase)
            && !directAnswerProducer.Equals("RulePolicy", StringComparison.OrdinalIgnoreCase);
    }

    private void LogExecutionDiagnostics(KnowledgeExecutionDiagnostics diagnostics)
    {
        string searches = string.Join(", ", diagnostics.Retrieval.Searches.Select(item =>
            $"{item.Term}:{item.CandidateCount}/{item.RelevantCount}"));
        string serverRequests = string.Join(", ", diagnostics.Retrieval.ServerRequests.Select(item =>
            $"{item.Purpose}:{item.Query}:{item.Project}:{string.Join('+', item.SourceKinds)}:{item.CandidateCount}/{item.RelevantCount}"));
        string selections = string.Join(", ", diagnostics.Retrieval.Selections.Select(item =>
            $"{item.DisplayName}:{item.Score}:{item.Reason}"));
        _logger.LogInformation(
            new EventId(4203, "KnowledgeQaExecution"),
            "Knowledge Q&A execution. ScopeEvaluator={ScopeEvaluator} Scope={Scope} " +
            "Planner={Planner} AnswerGenerator={AnswerGenerator} StoredAnswerProducer={StoredAnswerProducer} " +
            "Fallback={Fallback} FallbackReason={FallbackReason} Intent={Intent} ExactSymbols={ExactSymbols} " +
            "Concepts={Concepts} Project={Project} SourceKinds={SourceKinds} SearchTerms={SearchTerms} RequestedRelations={RequestedRelations} " +
            "SearchCandidateCounts={SearchCandidateCounts} ServerRequests={ServerRequests} EvidenceSelections={EvidenceSelections} " +
            "PlannerExitCode={PlannerExitCode} PlannerTimeout={PlannerTimeout} PlannerJsonParsed={PlannerJsonParsed} " +
            "AnswerExitCode={AnswerExitCode} AnswerTimeout={AnswerTimeout} AnswerJsonParsed={AnswerJsonParsed} " +
            "CodexElapsedMs={CodexElapsedMs}",
            diagnostics.ScopeEvaluator,
            diagnostics.ScopeDisposition,
            diagnostics.Retrieval.Planner.Provider,
            diagnostics.AnswerGenerator.Provider,
            diagnostics.StoredDirectAnswerProducer,
            diagnostics.FallbackUsed,
            diagnostics.FallbackReason,
            diagnostics.Retrieval.Intent,
            string.Join(",", diagnostics.Retrieval.ExactSymbols),
            string.Join(",", diagnostics.Retrieval.Concepts),
            diagnostics.Retrieval.Project,
            string.Join(",", diagnostics.Retrieval.SourceKinds),
            string.Join(",", diagnostics.Retrieval.SearchTerms),
            string.Join(",", diagnostics.Retrieval.RequestedRelations),
            searches,
            serverRequests,
            selections,
            diagnostics.Retrieval.Planner.Codex.ExitCode,
            diagnostics.Retrieval.Planner.Codex.TimedOut,
            diagnostics.Retrieval.Planner.Codex.JsonParseSucceeded,
            diagnostics.AnswerGenerator.Codex.ExitCode,
            diagnostics.AnswerGenerator.Codex.TimedOut,
            diagnostics.AnswerGenerator.Codex.JsonParseSucceeded,
            diagnostics.Retrieval.Planner.Codex.ElapsedMilliseconds
                + diagnostics.AnswerGenerator.Codex.ElapsedMilliseconds);
    }

    private static bool NeedsRevalidation(
        AnswerRevision revision,
        OntologyValidationSummaryViewModel current)
    {
        if (string.IsNullOrWhiteSpace(revision.Version.OntologyVersion)
            && string.IsNullOrWhiteSpace(revision.Version.GraphVersion)
            && string.IsNullOrWhiteSpace(revision.Version.OntologyHash))
        {
            return false;
        }
        return !revision.Version.OntologyVersion.Equals(current.OntologyVersion, StringComparison.Ordinal)
            || !revision.Version.GraphVersion.Equals(current.GraphVersion, StringComparison.Ordinal)
            || !revision.Version.OntologyHash.Equals(current.ContentHash, StringComparison.Ordinal);
    }

    private static string Classify(string question, EvidenceBundle bundle)
    {
        if (ContainsAny(question, "event", "이벤트")) return "Event";
        if (ContainsAny(question, "command", "커맨드", "명령")) return "Command";
        if (ContainsAny(question, "전달", "호출", "forward", "call")) return "호출 및 전달 흐름";
        if (ContainsAny(question, "상속", "인터페이스", "inherit", "implement")) return "상속과 인터페이스 구현";
        if (ContainsAny(question, "의존", "depend")) return "의존성";
        if (ContainsAny(question, "오류", "문제", "exception", "error")) return "오류 및 문제 해결";
        if (ContainsAny(question, "viewmodel")) return "ViewModel";
        if (ContainsAny(question, "model")) return "Model";
        return bundle.Evidence.Count == 0 ? "아직 충분히 답변되지 않은 질문" : "Dreamine 입문";
    }

    private static IReadOnlyList<QuestionTag> BuildTags(string question, string category)
    {
        string[] terms = KnowledgeEvidenceBundleBuilder.ExtractTerms(question);
        return new[] { category }.Concat(terms.Take(7))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(item => new QuestionTag(item)).ToArray();
    }

    private static string CreateSlug(string question)
    {
        string[] terms = KnowledgeEvidenceBundleBuilder.ExtractTerms(question)
            .Where(term => AsciiIdentifierRegex().IsMatch(term)).Take(6).ToArray();
        string value = terms.Length == 0 ? "code-question" : string.Join('-', terms);
        value = SlugInvalidRegex().Replace(value.ToLowerInvariant().Replace('.', '-'), "-").Trim('-');
        return value.Length == 0 ? "code-question" : value[..Math.Min(value.Length, 80)];
    }

    private static string CreateAccessKey()
    {
        string value = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        return value.TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string HashAccessKey(string accessKey) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(accessKey)));

    private static bool VerifyAccessKey(string? accessKey, string expectedHash)
    {
        if (string.IsNullOrWhiteSpace(accessKey) || expectedHash.Length != 64)
            return false;
        byte[] actual = SHA256.HashData(Encoding.UTF8.GetBytes(accessKey));
        try
        {
            return CryptographicOperations.FixedTimeEquals(actual, Convert.FromHexString(expectedHash));
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool ContainsAny(string value, params string[] candidates) =>
        candidates.Any(candidate => value.Contains(candidate, StringComparison.OrdinalIgnoreCase));

    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_.]*$")]
    private static partial Regex AsciiIdentifierRegex();

    [GeneratedRegex(@"[^a-z0-9-]+")]
    private static partial Regex SlugInvalidRegex();

    private sealed record SubmissionOperation(
        Lazy<Task<KnowledgeQuestionCreatedViewModel>> Task,
        DateTimeOffset CreatedAt);
}
