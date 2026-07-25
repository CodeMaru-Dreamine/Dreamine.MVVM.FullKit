using DreamineWeb.KnowledgeQa.Domain;
using DreamineWeb.Ontology.Application;
using DreamineWeb.Ontology.Domain;
using System.IO;
using System.Text.RegularExpressions;

namespace DreamineWeb.KnowledgeQa.Application;

/// <summary>Retrieves a small evidence subgraph, Doxygen records, and source excerpts for one question.</summary>
public sealed partial class KnowledgeEvidenceBundleBuilder : IEvidenceBundleBuilder, IKnowledgeEvidenceQueryService
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "and", "for", "from", "with", "what", "where", "how", "does", "into",
        "this", "that", "code", "class", "method", "project", "어디로", "어떻게", "무엇", "인가요",
        "되나요", "하나요", "코드", "클래스", "메서드", "프로젝트", "에서", "으로", "관련"
    };
    private static readonly HashSet<string> GenericActionWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "add", "adds", "added", "adding", "use", "uses", "used", "using",
        "connect", "connects", "connected", "connecting", "execute", "executes", "executed", "executing"
    };

    private readonly IOntologyRepository _repository;
    private readonly IOntologyRelationResolver _relationResolver;
    private readonly IOntologySourceService _sourceService;
    private readonly IDoxygenEvidenceProvider _doxygen;
    private readonly KnowledgeQaOptions _options;
    private readonly IKnowledgeQuestionPlanner? _planner;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _markupIndexGate = new(1, 1);
    private readonly SemaphoreSlim _codeIndexGate = new(1, 1);
    private IReadOnlyList<IndexedSourceDocument>? _markupIndex;
    private readonly Dictionary<string, IReadOnlyList<IndexedSourceDocument>> _codeIndexes = new(StringComparer.OrdinalIgnoreCase);

    public KnowledgeEvidenceBundleBuilder(
        IOntologyRepository repository,
        IOntologyRelationResolver relationResolver,
        IOntologySourceService sourceService,
        IDoxygenEvidenceProvider doxygen,
        KnowledgeQaOptions options,
        IKnowledgeQuestionPlanner? planner = null,
        TimeProvider? timeProvider = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _relationResolver = relationResolver ?? throw new ArgumentNullException(nameof(relationResolver));
        _sourceService = sourceService ?? throw new ArgumentNullException(nameof(sourceService));
        _doxygen = doxygen ?? throw new ArgumentNullException(nameof(doxygen));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _planner = planner;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async Task<EvidenceBundle> BuildAsync(
        string question,
        string language,
        CancellationToken cancellationToken)
    {
        string normalized = NormalizeQuestion(question);
        KnowledgeQuestionIntent intent = KnowledgeQuestionIntentClassifier.Classify(normalized);
        KnowledgeSearchPlan deterministicPlan = DeterministicKnowledgeSearchPlan.Create(normalized);
        KnowledgeSearchPlan plan = _planner is null
            ? deterministicPlan
            : await _planner.PlanAsync(normalized, language, cancellationToken).ConfigureAwait(false);
        string[] effectiveExactSymbols = plan.ExactSymbols
            .Concat(deterministicPlan.ExactSymbols)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        string[] effectiveConcepts = plan.Concepts
            .Concat(deterministicPlan.Concepts)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        string[] effectiveSourceKinds = plan.SourceKinds
            .Concat(deterministicPlan.SourceKinds)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        string? effectiveProject = plan.Project ?? deterministicPlan.Project;
        SearchRequest[] requests = effectiveExactSymbols.Select(term => new SearchRequest(term, "symbol", 300))
            .Concat(effectiveConcepts.Select(term => new SearchRequest(term, "concept", 200)))
            .Concat(plan.SearchTerms.Select(term => new SearchRequest(
                term,
                "searchTerm",
                IsGenericUiTerm(term) ? 10 : 100)))
            .Concat(effectiveExactSymbols.Length == 0 && effectiveConcepts.Length == 0
                ? ExtractTerms(normalized).Select(term => new SearchRequest(term, "raw-fallback", 10))
                : [])
            .Where(request => !string.IsNullOrWhiteSpace(request.Term))
            .DistinctBy(request => request.Term, StringComparer.OrdinalIgnoreCase)
            .Take(16)
            .ToArray();
        string[] terms = requests.Select(request => request.Term)
            .Where(term => !string.IsNullOrWhiteSpace(term))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (terms.Length == 0)
            return await EmptyBundleAsync(question, normalized, cancellationToken).ConfigureAwait(false);

        Dictionary<string, RankedNode> ranked = new(StringComparer.Ordinal);
        List<KnowledgeSearchCandidateDiagnostics> searchDiagnostics = [];
        List<KnowledgeServerSearchRequestDiagnostics> serverRequests = [];
        HashSet<string> exactSymbolUris = new(StringComparer.Ordinal);
        HashSet<string> exactMemberUris = new(StringComparer.Ordinal);
        Dictionary<string, HashSet<string>> exactUrisBySymbol = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> verifiedConceptExampleUris = new(StringComparer.Ordinal);
        Dictionary<string, VerifiedSourceMatch> verifiedSourceMatches = new(StringComparer.Ordinal);
        foreach (SearchRequest request in requests.Take(12))
        {
            cancellationToken.ThrowIfCancellationRequested();
            string term = request.Term;
            OntologyPage<OntologyNode> page = await _repository.SearchNodesAsync(
                new OntologyQuery(term, Project: effectiveProject ?? string.Empty), 1, 100, cancellationToken).ConfigureAwait(false);
            int relevantCount = 0;
            foreach (OntologyNode node in page.Items)
            {
                if (!MatchesSourceKinds(node, effectiveSourceKinds)
                    || !KnowledgeEvidenceRelevancePolicy.IsRelevant(intent, node))
                    continue;
                relevantCount += 1;
                if (!intent.IsGeneralConcept
                    && (OntologySymbolScopeResolver.IsExactMatch(node, term)
                        || effectiveExactSymbols.Any(symbol => OntologySymbolScopeResolver.IsExactMatch(node, symbol))))
                {
                    exactSymbolUris.Add(node.StableUri);
                    exactMemberUris.Add(node.StableUri);
                    foreach (string symbol in effectiveExactSymbols.Where(symbol =>
                        OntologySymbolScopeResolver.IsExactMatch(node, symbol)))
                        AddExactSymbolUri(exactUrisBySymbol, symbol, node.StableUri);
                }
                int score = ScoreNode(node, term, normalized) + request.Priority;
                bool exact = exactSymbolUris.Contains(node.StableUri);
                if (!intent.IsGeneralConcept && !exact && score < 30)
                    continue;
                string reason = exact
                    ? $"exact symbol match: {term}"
                    : $"relevance score {score} for search term: {term}";
                if (ranked.TryGetValue(node.StableUri, out RankedNode? existing) && existing is not null)
                    ranked[node.StableUri] = existing with
                    {
                        Score = existing.Score + score,
                        Reason = existing.Reason.Contains(term, StringComparison.OrdinalIgnoreCase)
                            ? existing.Reason
                            : existing.Reason + "; " + reason
                    };
                else
                    ranked[node.StableUri] = new RankedNode(node, score, [], reason);
            }
            searchDiagnostics.Add(new KnowledgeSearchCandidateDiagnostics(term, page.TotalCount, relevantCount));
            serverRequests.Add(new KnowledgeServerSearchRequestDiagnostics(
                term,
                request.Purpose,
                effectiveProject,
                effectiveSourceKinds,
                page.TotalCount,
                relevantCount,
                request.Priority));
        }

        if (intent.IsGeneralConcept)
        {
            await AddVerifiedConceptExampleAsync(
                intent,
                ranked,
                verifiedConceptExampleUris,
                verifiedSourceMatches,
                searchDiagnostics,
                cancellationToken).ConfigureAwait(false);
            if (verifiedConceptExampleUris.Count > 0)
            {
                foreach (string uri in ranked.Keys.Where(uri => !verifiedConceptExampleUris.Contains(uri)).ToArray())
                    ranked.Remove(uri);
            }
        }

        KnowledgeRelationConstraint[] relationConstraints = (deterministicPlan.RelationConstraints.Count > 0
                ? deterministicPlan.RelationConstraints
                : plan.RelationConstraints)
            .Distinct()
            .ToArray();
        string[] relationHints = (relationConstraints.Length > 0
                ? relationConstraints.Select(item => item.RelationType)
                : GetRelationHints(normalized).Concat(plan.RelationTypes))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        foreach (RankedNode item in ranked.Values.OrderByDescending(item => item.Score).Take(40).ToArray())
        {
            if (intent.IsGeneralConcept && !verifiedConceptExampleUris.Contains(item.Node.StableUri))
                continue;
            IReadOnlyList<OntologyRelation> relations = await _repository.GetRelationsAsync(
                item.Node.StableUri, cancellationToken).ConfigureAwait(false);
            int relationScore = relations.Count(relation => relationHints.Contains(
                relation.OriginalType, StringComparer.OrdinalIgnoreCase)) * 80;
            if (relations.Any(relation => relation.OriginalType.Equals("forwardsTo", StringComparison.OrdinalIgnoreCase)))
                relationScore += 25;
            ranked[item.Node.StableUri] = item with { Score = item.Score + relationScore, Relations = relations };
        }

        foreach (string dottedSymbol in terms.Where(term => !intent.IsGeneralConcept && term.Contains('.')).Take(8))
        {
            int separator = dottedSymbol.LastIndexOf('.');
            string ownerName = dottedSymbol[..separator];
            string memberName = dottedSymbol[(separator + 1)..];
            RankedNode[] owners = ranked.Values
                .Where(item => item.Node.CanonicalName.Equals(ownerName, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(item => item.Score)
                .ThenBy(item => item.Node.ProjectName, StringComparer.OrdinalIgnoreCase)
                .Take(8)
                .ToArray();
            foreach (RankedNode owner in owners)
            {
                exactSymbolUris.Add(owner.Node.StableUri);
                IReadOnlyList<OntologyRelation> ownerRelations = owner.Relations.Count > 0
                    ? owner.Relations
                    : await _repository.GetRelationsAsync(owner.Node.StableUri, cancellationToken).ConfigureAwait(false);
                if (owner.Relations.Count == 0)
                    ranked[owner.Node.StableUri] = owner with { Relations = ownerRelations };
                string[] memberUris = ownerRelations
                    .Where(relation => relation.SourceUri == owner.Node.StableUri
                        && relation.OriginalType.Equals("contains", StringComparison.OrdinalIgnoreCase))
                    .Select(relation => relation.TargetUri)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();
                IReadOnlyDictionary<string, OntologyNode> members = await _repository.GetNodesAsync(
                    memberUris, cancellationToken).ConfigureAwait(false);
                OntologyPage<OntologyNode> sourceMembers = await _repository.SearchNodesAsync(
                    new OntologyQuery(
                        memberName,
                        Project: owner.Node.ProjectName,
                        FilePath: owner.Node.SourcePath,
                        IncludeExcluded: true),
                    1,
                    100,
                    cancellationToken).ConfigureAwait(false);
                IEnumerable<OntologyNode> matchingMembers = members.Values.Concat(sourceMembers.Items)
                    .Where(node => node.CanonicalName.Equals(memberName, StringComparison.OrdinalIgnoreCase))
                    .DistinctBy(node => node.StableUri, StringComparer.Ordinal);
                foreach (OntologyNode member in matchingMembers)
                {
                    IReadOnlyList<OntologyRelation> memberRelations = await _repository.GetRelationsAsync(
                        member.StableUri, cancellationToken).ConfigureAwait(false);
                    ranked[member.StableUri] = new RankedNode(
                        member,
                        owner.Score + 5_000,
                        memberRelations,
                        $"exact owner/member resolution: {dottedSymbol}");
                    exactSymbolUris.Add(member.StableUri);
                    exactMemberUris.Add(member.StableUri);
                    AddExactSymbolUri(exactUrisBySymbol, dottedSymbol, member.StableUri);
                }
            }
        }

        OntologyNode[] selectedNodes = ranked.Values
            .OrderByDescending(item => exactSymbolUris.Contains(item.Node.StableUri))
            .ThenByDescending(item => item.Score)
            .ThenBy(item => item.Node.SourcePath, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Clamp(_options.MaximumOntologyNodes, 1, 24))
            .Select(item => item.Node)
            .ToArray();

        HashSet<string> fixedAnchorUris = ResolveFixedAnchorUris(relationConstraints, exactUrisBySymbol);
        if (fixedAnchorUris.Count > 0)
            selectedNodes = selectedNodes.Where(node => fixedAnchorUris.Contains(node.StableUri)).ToArray();

        List<EvidenceReference> evidence = [];
        int nodeIndex = 0;
        foreach (OntologyNode node in selectedNodes)
        {
            EvidenceReference nodeEvidence = ToNodeEvidence(node, language, ++nodeIndex);
            evidence.Add(verifiedConceptExampleUris.Contains(node.StableUri)
                ? nodeEvidence with { Provenance = "source-verified-concept-example" }
                : nodeEvidence);
        }

        List<OntologyRelation> relationCandidates = ranked.Values
            .Where(item => selectedNodes.Any(node => node.StableUri == item.Node.StableUri))
            .SelectMany(item => item.Relations)
            .DistinctBy(relation => relation.StableUri, StringComparer.Ordinal)
            .ToList();
        if (relationConstraints.Length > 0)
        {
            relationCandidates = relationCandidates
                .Where(relation => relationConstraints.Any(constraint =>
                    MatchesConstraint(relation, constraint, exactUrisBySymbol)))
                .ToList();
        }

        IReadOnlyDictionary<string, OntologyNode> relationCandidateNodes = await _repository.GetNodesAsync(
            relationCandidates.SelectMany(relation => new[] { relation.SourceUri, relation.TargetUri }),
            cancellationToken).ConfigureAwait(false);
        relationCandidates = relationCandidates
            .Where(relation => !ConnectsTypeAndFileFlow(relation, relationCandidateNodes))
            .ToList();
        if (intent.IsGeneralConcept)
        {
            relationCandidates = relationCandidates.Where(relation =>
                verifiedConceptExampleUris.Contains(relation.SourceUri)
                    && verifiedConceptExampleUris.Contains(relation.TargetUri)
                && VerifiedExampleRelationTypes.Contains(relation.OriginalType)).ToList();
        }

        // A planner's suppression flag means "do not invent a flow". It must not erase a
        // source-verified edge that was explicitly requested and whose endpoint resolved to
        // an exact symbol. The previous all-or-nothing branch discarded the real
        // MainWindowViewModel.Ok -> MainWindowEvent.Ok forwarding edge before the answer
        // generator ever saw it.
        if (plan.SuppressUnverifiedFlows && verifiedConceptExampleUris.Count == 0)
        {
            relationCandidates = relationCandidates.Where(relation =>
                relationHints.Contains(relation.OriginalType, StringComparer.OrdinalIgnoreCase)
                && (exactMemberUris.Contains(relation.SourceUri)
                    || exactMemberUris.Contains(relation.TargetUri)
                    || exactSymbolUris.Contains(relation.SourceUri)
                    || exactSymbolUris.Contains(relation.TargetUri)))
                .ToList();
        }

        // When a repeated symbol such as MainWindowViewModel.Ok exists in several samples,
        // an exact source-verified forwardsTo edge identifies the coherent feature folder.
        // Keep companion requested relations in that folder instead of mixing SampleCore,
        // SampleEnterprise and SampleSmart into one answer.
        OntologyRelation[] exactForwarding = relationCandidates.Where(relation =>
            relation.OriginalType.Equals("forwardsTo", StringComparison.OrdinalIgnoreCase)
            && exactMemberUris.Contains(relation.SourceUri))
            .ToArray();
        HashSet<string> coherentDirectories = exactForwarding
            .Select(relation => ranked.GetValueOrDefault(relation.SourceUri)?.Node.SourcePath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => NormalizeSourceDirectory(path!))
            .Where(path => path.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (coherentDirectories.Count > 0)
        {
            relationCandidates = relationCandidates.Where(relation =>
                exactForwarding.Any(item => item.StableUri.Equals(
                    relation.StableUri,
                    StringComparison.Ordinal))
                || IsRelationSourceInDirectory(relation, ranked, coherentDirectories))
                .ToList();

            selectedNodes = selectedNodes.Where(node =>
                !string.IsNullOrWhiteSpace(node.SourcePath)
                && coherentDirectories.Contains(NormalizeSourceDirectory(node.SourcePath)))
                .ToArray();
            HashSet<string> coherentNodeUris = selectedNodes
                .Select(node => node.StableUri)
                .ToHashSet(StringComparer.Ordinal);
            evidence.RemoveAll(item =>
                item.Kind == EvidenceKind.OntologyNode
                && (string.IsNullOrWhiteSpace(item.StableUri)
                    || !coherentNodeUris.Contains(item.StableUri)));
        }

        List<OntologyRelation> selectedRelations = relationCandidates
            .OrderByDescending(relation => exactMemberUris.Contains(relation.SourceUri))
            .ThenByDescending(relation => relationHints.Contains(relation.OriginalType, StringComparer.OrdinalIgnoreCase))
            .ThenByDescending(relation => exactSymbolUris.Contains(relation.SourceUri)
                || exactSymbolUris.Contains(relation.TargetUri))
            .ThenByDescending(relation => ranked.TryGetValue(relation.SourceUri, out RankedNode? sourceRank)
                ? sourceRank.Score
                : 0)
            .ThenBy(relation => relation.OriginalType, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Clamp(_options.MaximumRelations, 1, 80))
            .ToList();
        IReadOnlyDictionary<string, OntologyNode> relatedNodes = await _repository.GetNodesAsync(
            selectedRelations.SelectMany(relation => new[] { relation.SourceUri, relation.TargetUri }),
            cancellationToken).ConfigureAwait(false);
        OntologyNode[] expandedNodes = selectedNodes
            .Concat(relatedNodes.Values)
            .DistinctBy(node => node.StableUri, StringComparer.Ordinal)
            .ToArray();
        foreach (OntologyNode node in expandedNodes.Where(node => selectedNodes.All(item => item.StableUri != node.StableUri)))
            evidence.Add(ToNodeEvidence(node, language, ++nodeIndex));
        int relationIndex = 0;
        foreach (OntologyRelation relation in selectedRelations)
        {
            EvidenceReference relationEvidence = ToRelationEvidence(relation, relatedNodes, ++relationIndex);
            evidence.Add(intent.IsGeneralConcept
                ? relationEvidence with { Provenance = "source-verified-concept-example" }
                : relationEvidence);
        }

        IReadOnlyList<EvidenceReference> doxygen = await _doxygen.SearchAsync(
            expandedNodes, language, cancellationToken).ConfigureAwait(false);
        evidence.AddRange(doxygen.Take(Math.Clamp(_options.MaximumDoxygenReferences, 0, 20)).Select(item =>
            !string.IsNullOrWhiteSpace(item.StableUri) && verifiedConceptExampleUris.Contains(item.StableUri)
                ? item with { Provenance = "source-verified-concept-example" }
                : item));

        int sourceIndex = 0;
        foreach (OntologyNode node in expandedNodes.Take(Math.Clamp(_options.MaximumSourceReferences, 0, 20)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            OntologySourceDocumentViewModel source = verifiedSourceMatches.TryGetValue(node.StableUri, out VerifiedSourceMatch? match)
                ? match.Source
                : await _sourceService.GetSourceAsync(node.StableUri, cancellationToken).ConfigureAwait(false);
            if (!source.Availability.IsAvailable || source.Lines.Count == 0)
                continue;
            EvidenceReference sourceEvidence = match is null
                ? ToSourceEvidence(node, source, ++sourceIndex)
                : ToSourceEvidence(node, source, ++sourceIndex, match.StartLine, match.EndLine);
            evidence.Add(verifiedConceptExampleUris.Contains(node.StableUri)
                ? sourceEvidence with { Provenance = "source-verified-concept-example" }
                : sourceEvidence);
        }

        OntologyValidationData validation = await _repository.GetValidationDataAsync(cancellationToken).ConfigureAwait(false);
        KnowledgeVersionSnapshot version = BuildVersion(expandedNodes, validation);
        KnowledgeEvidenceSelectionDiagnostics[] selections = expandedNodes
            .Select(node => ranked.TryGetValue(node.StableUri, out RankedNode? rank)
                ? new KnowledgeEvidenceSelectionDiagnostics(
                    node.StableUri,
                    string.IsNullOrWhiteSpace(node.QualifiedName) ? node.CanonicalName : node.QualifiedName,
                    rank.Score,
                    rank.Reason)
                : new KnowledgeEvidenceSelectionDiagnostics(
                    node.StableUri,
                    string.IsNullOrWhiteSpace(node.QualifiedName) ? node.CanonicalName : node.QualifiedName,
                    0,
                    "requested relation endpoint"))
            .ToArray();
        return new EvidenceBundle(question.Trim(), normalized, evidence, version, _timeProvider.GetUtcNow())
        {
            RetrievalDiagnostics = new KnowledgeRetrievalDiagnostics
            {
                Intent = string.IsNullOrWhiteSpace(plan.Intent) ? plan.QueryKind : plan.Intent,
                Project = effectiveProject ?? string.Empty,
                ExactSymbols = effectiveExactSymbols,
                Concepts = effectiveConcepts,
                SearchTerms = terms,
                SourceKinds = effectiveSourceKinds,
                RequestedRelations = relationHints,
                RelationConstraints = relationConstraints,
                Searches = searchDiagnostics,
                ServerRequests = serverRequests,
                Selections = selections,
                Planner = plan.Diagnostics
            }
        };
    }

    private static bool MatchesSourceKinds(OntologyNode node, IReadOnlyList<string> sourceKinds)
    {
        if (sourceKinds.Count == 0 || sourceKinds.Contains("Code", StringComparer.OrdinalIgnoreCase))
            return true;
        string path = node.SourcePath ?? string.Empty;
        string type = node.EffectiveType ?? string.Empty;
        return sourceKinds.Any(kind => kind.ToLowerInvariant() switch
        {
            "xaml" => path.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase),
            "viewmodel" => type.Contains("ViewModel", StringComparison.OrdinalIgnoreCase)
                || path.Contains("ViewModel", StringComparison.OrdinalIgnoreCase),
            "event" => type.Contains("Event", StringComparison.OrdinalIgnoreCase)
                || path.Contains("Event", StringComparison.OrdinalIgnoreCase),
            "model" => type.Contains("Model", StringComparison.OrdinalIgnoreCase)
                || path.Contains("Model", StringComparison.OrdinalIgnoreCase),
            "service" => type.Contains("Service", StringComparison.OrdinalIgnoreCase)
                || path.Contains("Service", StringComparison.OrdinalIgnoreCase),
            "view" => type.Contains("View", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase),
            _ => false
        });
    }

    private static bool IsGenericUiTerm(string term) => term.Trim().ToLowerInvariant() is
        "button" or "radiobutton" or "keybutton" or "input" or "text" or "list" or "window"
        or "버튼" or "입력" or "글자" or "목록" or "창";

    private async Task AddVerifiedConceptExampleAsync(
        KnowledgeQuestionIntent intent,
        IDictionary<string, RankedNode> ranked,
        ISet<string> verifiedUris,
        IDictionary<string, VerifiedSourceMatch> verifiedSources,
        ICollection<KnowledgeSearchCandidateDiagnostics> diagnostics,
        CancellationToken cancellationToken)
    {
        if (!intent.IsGeneralConcept || !intent.RequiresXamlEvidence)
            return;

        IReadOnlyList<IndexedSourceDocument> markup = await GetMarkupIndexAsync(cancellationToken)
            .ConfigureAwait(false);
        List<VerifiedSourceMatch> candidates = [];
        foreach (IndexedSourceDocument document in markup)
        {
            cancellationToken.ThrowIfCancellationRequested();
            VerifiedSourceMatch? match = FindBestSourceRange(intent, document.Node, document.Source);
            if (match is not null)
                candidates.Add(match);
        }

        VerifiedSourceMatch? anchor = candidates
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.EndLine - item.StartLine)
            .ThenBy(item => item.Node.SourcePath, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
        diagnostics.Add(new KnowledgeSearchCandidateDiagnostics(
            $"source-verified-xaml:{string.Join('+', intent.FocusTerms)}",
            markup.Count,
            candidates.Count));
        if (anchor is null)
            return;

        AddVerifiedMatch(anchor, ranked, verifiedUris, verifiedSources);
        string featureStem = Path.GetFileNameWithoutExtension(anchor.Node.SourcePath);
        string family = ProjectFamily(anchor.Node);
        string[] boundSymbols = ExtractBindingSymbols(anchor.Source, anchor.StartLine, anchor.EndLine);
        IReadOnlyList<IndexedSourceDocument> codeDocuments = await GetCodeIndexAsync(family, cancellationToken)
            .ConfigureAwait(false);
        VerifiedSourceMatch[] verifiedCompanions = codeDocuments
            .Select(document => VerifyCompanion(document, boundSymbols, anchor.Score))
            .OfType<VerifiedSourceMatch>()
            .OrderByDescending(item => CompanionTypePriority(item.Node))
            .ThenByDescending(item => item.Score)
            .Take(3)
            .ToArray();
        foreach (VerifiedSourceMatch companion in verifiedCompanions)
        {
            AddVerifiedMatch(companion, ranked, verifiedUris, verifiedSources);
        }
        diagnostics.Add(new KnowledgeSearchCandidateDiagnostics(
            $"source-verified-companion:{featureStem}:{string.Join('+', boundSymbols)}",
            codeDocuments.Count,
            verifiedCompanions.Length));
    }

    private async Task<IReadOnlyList<IndexedSourceDocument>> GetMarkupIndexAsync(
        CancellationToken cancellationToken)
    {
        if (_markupIndex is not null)
            return _markupIndex;
        await _markupIndexGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_markupIndex is not null)
                return _markupIndex;
            List<IndexedSourceDocument> documents = [];
            int page = 1;
            int totalPages;
            do
            {
                OntologyPage<OntologyNode> nodes = await _repository.SearchNodesAsync(
                    new OntologyQuery(FilePath: ".xaml"), page, 100, cancellationToken).ConfigureAwait(false);
                totalPages = nodes.TotalPages;
                foreach (OntologyNode node in nodes.Items.Where(node =>
                    node.SourcePath.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase)
                    && !node.IsExcluded
                    && !node.IsStale))
                {
                    OntologySourceDocumentViewModel source = await _sourceService.GetSourceAsync(
                        node.StableUri, cancellationToken).ConfigureAwait(false);
                    if (source.Availability.IsAvailable && source.Lines.Count > 0)
                        documents.Add(new IndexedSourceDocument(node, source));
                }
                page += 1;
            }
            while (page <= totalPages);
            _markupIndex = documents;
            return _markupIndex;
        }
        finally
        {
            _markupIndexGate.Release();
        }
    }

    private async Task<IReadOnlyList<IndexedSourceDocument>> GetCodeIndexAsync(
        string projectFamily,
        CancellationToken cancellationToken)
    {
        if (_codeIndexes.TryGetValue(projectFamily, out IReadOnlyList<IndexedSourceDocument>? cached))
            return cached;
        await _codeIndexGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_codeIndexes.TryGetValue(projectFamily, out cached))
                return cached;
            List<OntologyNode> nodes = [];
            int page = 1;
            int totalPages;
            do
            {
                OntologyPage<OntologyNode> result = await _repository.SearchNodesAsync(
                    new OntologyQuery(Project: projectFamily), page, 100, cancellationToken).ConfigureAwait(false);
                totalPages = result.TotalPages;
                nodes.AddRange(result.Items.Where(node =>
                    node.SourcePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                    && !node.IsExcluded
                    && !node.IsStale
                    && ProjectFamily(node).Equals(projectFamily, StringComparison.OrdinalIgnoreCase)));
                page += 1;
            }
            while (page <= totalPages);

            List<IndexedSourceDocument> documents = [];
            foreach (IGrouping<string, OntologyNode> group in nodes.GroupBy(
                node => node.SourcePath, StringComparer.OrdinalIgnoreCase))
            {
                OntologyNode representative = group
                    .OrderByDescending(CompanionTypePriority)
                    .ThenBy(node => node.LineStart ?? int.MaxValue)
                    .First();
                OntologySourceDocumentViewModel source = await _sourceService.GetSourceAsync(
                    representative.StableUri, cancellationToken).ConfigureAwait(false);
                if (source.Availability.IsAvailable && source.Lines.Count > 0)
                    documents.Add(new IndexedSourceDocument(representative, source));
            }
            _codeIndexes[projectFamily] = documents;
            return documents;
        }
        finally
        {
            _codeIndexGate.Release();
        }
    }

    private static VerifiedSourceMatch? FindBestSourceRange(
        KnowledgeQuestionIntent intent,
        OntologyNode node,
        OntologySourceDocumentViewModel source)
    {
        VerifiedSourceMatch? best = null;
        foreach (OntologySourceLineViewModel line in source.Lines)
        {
            if (!intent.FocusTerms.Any(term => line.Text.Contains(term, StringComparison.OrdinalIgnoreCase)))
                continue;
            (int start, int end) = ExpandMarkupRange(source.Lines, line.Number);
            string excerpt = string.Join('\n', source.Lines
                .Where(item => item.Number >= start && item.Number <= end)
                .Select(item => item.Text));
            KnowledgeEvidenceRelevanceScore assessment = KnowledgeEvidenceRelevancePolicy.Evaluate(
                intent, excerpt, node.SourcePath);
            if (!assessment.IsRelevant)
                continue;
            int elementBonus = excerpt.Contains('<') ? 140 : 0;
            int score = assessment.Score + elementBonus
                + (node.SourceVerificationStatus.Contains("source", StringComparison.OrdinalIgnoreCase) ? 15 : 0);
            VerifiedSourceMatch candidate = new(
                node,
                source,
                score,
                start,
                end,
                $"verified source relevance {score}: {assessment.Reason}; coherent markup block");
            if (best is null || candidate.Score > best.Score)
                best = candidate;
        }
        return best;
    }

    private static VerifiedSourceMatch? VerifyCompanion(
        IndexedSourceDocument document,
        IReadOnlyList<string> bindingSymbols,
        int anchorScore)
    {
        OntologyNode node = document.Node;
        OntologySourceDocumentViewModel source = document.Source;
        if (!source.Availability.IsAvailable || source.Lines.Count == 0 || bindingSymbols.Count == 0)
            return null;
        OntologySourceLineViewModel[] matchingLines = source.Lines
            .Where(line => LooksLikeDeclaration(line.Text, bindingSymbols))
            .ToArray();
        if (matchingLines.Length == 0)
            return null;
        OntologySourceLineViewModel focus = matchingLines
            .OrderBy(line => Math.Abs(line.Number - (node.LineStart ?? line.Number)))
            .First();
        int start = Math.Max(1, focus.Number - 6);
        int end = Math.Min(source.Lines[^1].Number, focus.Number + 8);
        int score = anchorScore - 100 + Math.Min(200, matchingLines.Length * 40);
        return new VerifiedSourceMatch(
            node,
            source,
            score,
            start,
            end,
            $"verified binding companion {score}: {matchingLines.Length} bound symbol declaration(s)");
    }

    private static bool LooksLikeDeclaration(string text, IReadOnlyList<string> symbols) =>
        symbols.Any(symbol => ContainsIdentifier(text, symbol))
        && (text.Contains("public ", StringComparison.Ordinal)
            || text.Contains("private ", StringComparison.Ordinal)
            || text.Contains("protected ", StringComparison.Ordinal)
            || text.Contains("internal ", StringComparison.Ordinal)
            || text.TrimStart().StartsWith("[", StringComparison.Ordinal));

    private static void AddVerifiedMatch(
        VerifiedSourceMatch match,
        IDictionary<string, RankedNode> ranked,
        ISet<string> verifiedUris,
        IDictionary<string, VerifiedSourceMatch> verifiedSources)
    {
        ranked[match.Node.StableUri] = new RankedNode(match.Node, match.Score, [], match.Reason);
        verifiedUris.Add(match.Node.StableUri);
        verifiedSources[match.Node.StableUri] = match;
    }

    private static (int Start, int End) ExpandMarkupRange(
        IReadOnlyList<OntologySourceLineViewModel> lines,
        int focusLine)
    {
        int start = focusLine;
        for (int number = focusLine; number >= Math.Max(1, focusLine - 24); number -= 1)
        {
            string text = lines[number - 1].Text;
            if (number < focusLine && (text.Contains("/>", StringComparison.Ordinal)
                || text.Contains("</", StringComparison.Ordinal)))
                break;
            if (text.Contains('<', StringComparison.Ordinal))
            {
                start = number;
                break;
            }
        }
        int end = focusLine;
        for (int number = focusLine; number <= Math.Min(lines[^1].Number, focusLine + 24); number += 1)
        {
            string text = lines[number - 1].Text;
            end = number;
            if (text.Contains("/>", StringComparison.Ordinal)
                || text.Contains("</", StringComparison.Ordinal))
                break;
        }
        return (start, end);
    }

    private static string[] ExtractBindingSymbols(
        OntologySourceDocumentViewModel source,
        int start,
        int end)
    {
        string text = string.Join('\n', source.Lines
            .Where(line => line.Number >= start && line.Number <= end)
            .Select(line => line.Text));
        return BindingPathRegex().Matches(text)
            .SelectMany(match =>
            {
                string path = match.Groups[1].Value;
                return new[] { path }.Concat(path.Split('.', StringSplitOptions.RemoveEmptyEntries));
            })
            .Where(value => value.Length > 1)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static int CompanionTypePriority(OntologyNode node) =>
        node.EffectiveType.Contains("ViewModel", StringComparison.OrdinalIgnoreCase) ? 3
        : node.EffectiveType.Contains("Class", StringComparison.OrdinalIgnoreCase) ? 2
        : node.EffectiveType.Contains("SourceFile", StringComparison.OrdinalIgnoreCase) ? 1
        : 0;

    private static string ProjectFamily(OntologyNode node)
    {
        string project = string.IsNullOrWhiteSpace(node.ProjectName)
            ? Path.GetFileName(Path.GetDirectoryName(node.SourcePath) ?? string.Empty)
            : node.ProjectName;
        string[] segments = project.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length > 1 && PlatformProjectSuffixes.Contains(segments[^1]))
            return string.Join('.', segments[..^1]);
        return project;
    }

    private static bool ContainsIdentifier(string text, string identifier)
    {
        int index = text.IndexOf(identifier, StringComparison.OrdinalIgnoreCase);
        while (index >= 0)
        {
            bool left = index == 0 || !IsIdentifierCharacter(text[index - 1]);
            int rightIndex = index + identifier.Length;
            bool right = rightIndex == text.Length || !IsIdentifierCharacter(text[rightIndex]);
            if (left && right)
                return true;
            index = text.IndexOf(identifier, index + 1, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    /// <inheritdoc />
    public Task<EvidenceBundle> QueryAsync(
        KnowledgeEvidenceQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        string language = query.Language.Equals("en", StringComparison.OrdinalIgnoreCase) ? "en" : "ko";
        return BuildAsync(query.Query, language, cancellationToken);
    }

    private async Task<EvidenceBundle> EmptyBundleAsync(
        string question,
        string normalized,
        CancellationToken cancellationToken)
    {
        OntologyValidationData validation = await _repository.GetValidationDataAsync(cancellationToken).ConfigureAwait(false);
        return new EvidenceBundle(
            question.Trim(),
            normalized,
            [],
            BuildVersion([], validation),
            _timeProvider.GetUtcNow());
    }

    internal static string NormalizeQuestion(string question) =>
        WhitespaceRegex().Replace((question ?? string.Empty).Trim(), " ");

    internal static string[] ExtractTerms(string question)
    {
        HashSet<string> terms = new(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in IdentifierRegex().Matches(question))
        {
            string token = match.Value.Trim();
            if (token.Length < 2 || ShouldIgnoreTerm(token))
                continue;
            terms.Add(token);
            foreach (string segment in token.Split('.', StringSplitOptions.RemoveEmptyEntries))
                if (segment.Length >= 2 && !ShouldIgnoreTerm(segment))
                    terms.Add(segment);
        }
        foreach (Match match in KoreanWordRegex().Matches(question))
        {
            string token = match.Value.Trim();
            if (token.Length >= 2 && !ShouldIgnoreTerm(token))
                terms.Add(token);
        }
        return terms.OrderByDescending(term => term.Contains('.')).ThenByDescending(term => term.Length).ToArray();
    }

    private static bool ShouldIgnoreTerm(string value) =>
        StopWords.Contains(value)
        || GenericActionWords.Contains(value)
        || value.StartsWith("추가", StringComparison.Ordinal)
        || value.StartsWith("사용", StringComparison.Ordinal)
        || value.StartsWith("연결", StringComparison.Ordinal)
        || value.StartsWith("실행", StringComparison.Ordinal);

    private static int ScoreNode(OntologyNode node, string term, string question)
    {
        int score = node.CanonicalName.Equals(term, StringComparison.OrdinalIgnoreCase) ? 100 : 0;
        if (node.QualifiedName.EndsWith(term, StringComparison.OrdinalIgnoreCase)) score += 90;
        if (node.SourcePath.Contains(term, StringComparison.OrdinalIgnoreCase)) score += 35;
        if (node.Signature.Contains(term, StringComparison.OrdinalIgnoreCase)) score += 30;
        if (question.Contains(node.ProjectName, StringComparison.OrdinalIgnoreCase)) score += 120;
        if (node.SourceVerificationStatus.Contains("source", StringComparison.OrdinalIgnoreCase)) score += 15;
        if (node.IsExcluded || node.IsStale) score -= 200;
        return score;
    }

    private static string[] GetRelationHints(string question)
    {
        List<string> hints = [];
        if (ContainsAny(question, "전달", "forward")) hints.Add("forwardsTo");
        if (ContainsAny(question, "event", "이벤트")) hints.Add("hasEventComponent");
        if (ContainsAny(question, "호출", "call")) hints.Add("calls");
        if (ContainsAny(question, "의존", "depend")) hints.Add("dependsOn");
        if (ContainsAny(question, "상속", "inherit")) hints.Add("inherits");
        if (ContainsAny(question, "구현", "implement")) hints.Add("implements");
        return hints.ToArray();
    }

    private static bool ContainsAny(string value, params string[] candidates) =>
        candidates.Any(candidate => value.Contains(candidate, StringComparison.OrdinalIgnoreCase));

    private static HashSet<string> ResolveFixedAnchorUris(
        IReadOnlyList<KnowledgeRelationConstraint> constraints,
        IReadOnlyDictionary<string, HashSet<string>> exactUrisBySymbol) => constraints
        .Where(constraint => constraint.AnchorSymbol?.Contains('.') == true)
        .SelectMany(constraint => exactUrisBySymbol.GetValueOrDefault(constraint.AnchorSymbol!, []))
        .ToHashSet(StringComparer.Ordinal);

    private static bool MatchesConstraint(
        OntologyRelation relation,
        KnowledgeRelationConstraint constraint,
        IReadOnlyDictionary<string, HashSet<string>> exactUrisBySymbol)
    {
        if (!relation.OriginalType.Equals(constraint.RelationType, StringComparison.OrdinalIgnoreCase))
            return false;
        if (string.IsNullOrWhiteSpace(constraint.AnchorSymbol))
            return true;
        HashSet<string> anchors = exactUrisBySymbol.GetValueOrDefault(constraint.AnchorSymbol, []);
        if (anchors.Count == 0)
            return false;
        return constraint.Direction == KnowledgeRelationDirection.Outgoing
            ? anchors.Contains(relation.SourceUri)
            : anchors.Contains(relation.TargetUri);
    }

    private static void AddExactSymbolUri(
        IDictionary<string, HashSet<string>> exactUrisBySymbol,
        string symbol,
        string stableUri)
    {
        if (!exactUrisBySymbol.TryGetValue(symbol, out HashSet<string>? uris))
            exactUrisBySymbol[symbol] = uris = new HashSet<string>(StringComparer.Ordinal);
        uris.Add(stableUri);
    }

    private static bool ConnectsTypeAndFileFlow(
        OntologyRelation relation,
        IReadOnlyDictionary<string, OntologyNode> nodes)
    {
        if (!relation.OriginalType.Equals("calls", StringComparison.OrdinalIgnoreCase)
            && !relation.OriginalType.Equals("forwardsTo", StringComparison.OrdinalIgnoreCase))
            return false;
        if (!nodes.TryGetValue(relation.SourceUri, out OntologyNode? source)
            || !nodes.TryGetValue(relation.TargetUri, out OntologyNode? target))
            return false;
        return IsFileNode(source) && IsTypeNode(target)
            || IsTypeNode(source) && IsFileNode(target);
    }

    private static bool IsFileNode(OntologyNode node) =>
        node.RawType?.Equals("file", StringComparison.OrdinalIgnoreCase) == true
        || node.EffectiveType.Contains("File", StringComparison.OrdinalIgnoreCase)
        || node.Types.Any(type => type.Contains("File", StringComparison.OrdinalIgnoreCase));

    private static bool IsTypeNode(OntologyNode node) =>
        node.RawType is not null && TypeRawKinds.Contains(node.RawType)
        || node.Types.Any(type => type.Equals("CodeType", StringComparison.OrdinalIgnoreCase));

    private static bool IsRelationSourceInDirectory(
        OntologyRelation relation,
        IReadOnlyDictionary<string, RankedNode> ranked,
        IReadOnlySet<string> directories)
    {
        if (!ranked.TryGetValue(relation.SourceUri, out RankedNode? source)
            || string.IsNullOrWhiteSpace(source.Node.SourcePath))
        {
            return false;
        }

        return directories.Contains(NormalizeSourceDirectory(source.Node.SourcePath));
    }

    private static string NormalizeSourceDirectory(string sourcePath)
    {
        string normalized = sourcePath.Replace('\\', '/');
        int separator = normalized.LastIndexOf('/');
        return separator <= 0 ? string.Empty : normalized[..separator];
    }

    private static EvidenceReference ToNodeEvidence(OntologyNode node, string language, int index)
    {
        string summary = node.Summaries.FirstOrDefault(item => item.Language.Equals(language, StringComparison.OrdinalIgnoreCase))?.Text
            ?? node.Summaries.FirstOrDefault()?.Text
            ?? node.Signature;
        return new EvidenceReference
        {
            Id = $"ontology-node-{index}",
            Kind = EvidenceKind.OntologyNode,
            Origin = EvidenceOrigin.Direct,
            Title = $"{node.QualifiedName} ({node.EffectiveType})",
            Summary = summary,
            StableUri = node.StableUri,
            SourcePath = node.SourcePath,
            LineStart = node.LineStart,
            LineEnd = node.LineEnd,
            Declaration = node.Signature,
            Provenance = node.Evidence.FirstOrDefault()?.Source ?? node.SourceVerificationStatus,
            Confidence = node.Evidence.Count == 0 ? 1d : node.Evidence.Max(item => item.Confidence)
        };
    }

    private EvidenceReference ToRelationEvidence(
        OntologyRelation relation,
        IReadOnlyDictionary<string, OntologyNode> nodes,
        int index)
    {
        OntologyRelationMeaning meaning = _relationResolver.Resolve(relation.OriginalType);
        string source = RelationDisplayName(nodes.GetValueOrDefault(relation.SourceUri), relation.SourceUri);
        string target = RelationDisplayName(nodes.GetValueOrDefault(relation.TargetUri), relation.TargetUri);
        bool inferred = relation.Evidence.Any(item => item.Source.Contains("infer", StringComparison.OrdinalIgnoreCase));
        return new EvidenceReference
        {
            Id = $"ontology-relation-{index}",
            Kind = EvidenceKind.OntologyRelation,
            Origin = inferred ? EvidenceOrigin.Inferred : EvidenceOrigin.Direct,
            Title = $"{source} → {relation.OriginalType} → {target}",
            Summary = meaning.WasProjected
                ? $"Original relation {relation.OriginalType}; compatibility projection {meaning.ProjectionType}."
                : $"Direct ontology relation {relation.OriginalType}.",
            StableUri = relation.SourceUri,
            RelatedStableUri = relation.TargetUri,
            RelationType = relation.OriginalType,
            ProjectionType = meaning.WasProjected ? meaning.ProjectionType : null,
            Provenance = relation.Evidence.FirstOrDefault()?.Source ?? "ontology",
            Confidence = relation.Evidence.Count == 0 ? 1d : relation.Evidence.Max(item => item.Confidence)
        };
    }

    private static string RelationDisplayName(OntologyNode? node, string fallback)
    {
        if (node is null)
            return fallback;
        string fileName = Path.GetFileName(node.SourcePath);
        (string Suffix, string OwnerSuffix)[] patterns =
        [
            (".xaml.ViewModel.cs", "ViewModel"),
            (".xaml.Event.cs", "Event"),
            (".xaml.Model.cs", "Model")
        ];
        foreach ((string suffix, string ownerSuffix) in patterns)
        {
            if (!fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                continue;
            string owner = fileName[..^suffix.Length] + ownerSuffix;
            string prefix = string.IsNullOrWhiteSpace(node.Namespace) ? string.Empty : node.Namespace + ".";
            return prefix + owner + "." + node.CanonicalName;
        }
        return string.IsNullOrWhiteSpace(node.QualifiedName) ? node.CanonicalName : node.QualifiedName;
    }

    private static EvidenceReference ToSourceEvidence(
        OntologyNode node,
        OntologySourceDocumentViewModel source,
        int index)
    {
        int focusStart = source.HighlightStart ?? node.LineStart ?? 1;
        int focusEnd = source.HighlightEnd ?? node.LineEnd ?? focusStart;
        (int start, int end) = ResolveDeclarationRange(node, source, focusStart, focusEnd);
        string excerpt = string.Join('\n', source.Lines.Where(line => line.Number >= start && line.Number <= end)
            .Select(line => $"{line.Number,4}: {line.Text}"));
        return new EvidenceReference
        {
            Id = $"source-{index}",
            Kind = EvidenceKind.Source,
            Origin = EvidenceOrigin.Direct,
            Title = $"{node.CanonicalName} source declaration",
            Summary = "Bounded source excerpt resolved through the stable URI.",
            StableUri = node.StableUri,
            SourcePath = source.Availability.DisplayPath,
            LineStart = start,
            LineEnd = end,
            Declaration = node.Signature,
            CodeExcerpt = excerpt,
            Provenance = "source-mirror",
            Confidence = 1d
        };
    }

    private static EvidenceReference ToSourceEvidence(
        OntologyNode node,
        OntologySourceDocumentViewModel source,
        int index,
        int start,
        int end)
    {
        int lastLine = source.Lines.Count == 0 ? 1 : source.Lines[^1].Number;
        int boundedStart = Math.Clamp(start, 1, lastLine);
        int boundedEnd = Math.Clamp(Math.Max(end, boundedStart), boundedStart, lastLine);
        string excerpt = string.Join('\n', source.Lines
            .Where(line => line.Number >= boundedStart && line.Number <= boundedEnd)
            .Select(line => $"{line.Number,4}: {line.Text}"));
        return new EvidenceReference
        {
            Id = $"source-{index}",
            Kind = EvidenceKind.Source,
            Origin = EvidenceOrigin.Direct,
            Title = $"{node.CanonicalName} source declaration",
            Summary = "Question-relevant source excerpt resolved through the stable URI.",
            StableUri = node.StableUri,
            SourcePath = source.Availability.DisplayPath,
            LineStart = boundedStart,
            LineEnd = boundedEnd,
            Declaration = node.Signature,
            CodeExcerpt = excerpt,
            Provenance = "source-mirror",
            Confidence = 1d
        };
    }

    private static (int Start, int End) ResolveDeclarationRange(
        OntologyNode node,
        OntologySourceDocumentViewModel source,
        int focusStart,
        int focusEnd)
    {
        if (source.Lines.Count == 0)
            return (focusStart, focusEnd);

        Dictionary<int, string> lines = source.Lines.ToDictionary(line => line.Number, line => line.Text);
        int lastLine = source.Lines[^1].Number;
        int start = Math.Clamp(focusStart, 1, lastLine);
        int end = Math.Clamp(Math.Max(focusEnd, start), start, lastLine);
        int limit = Math.Min(lastLine, start + 47);
        bool declarationFound = false;
        bool blockStarted = false;
        int braceDepth = 0;

        for (int number = start; number <= limit; number += 1)
        {
            string text = lines.GetValueOrDefault(number, string.Empty);
            string trimmed = text.TrimStart();
            if (number > end && trimmed.StartsWith("///", StringComparison.Ordinal))
                break;

            if (!declarationFound && IsDeclarationLine(trimmed, node.CanonicalName))
                declarationFound = true;

            if (declarationFound)
            {
                end = number;
                int opens = text.Count(character => character == '{');
                int closes = text.Count(character => character == '}');
                if (opens > 0)
                    blockStarted = true;
                braceDepth += opens - closes;

                if ((!blockStarted && (trimmed.EndsWith(';') || trimmed.Contains("=>", StringComparison.Ordinal)))
                    || (blockStarted && braceDepth <= 0))
                {
                    break;
                }
            }
            else
            {
                end = Math.Max(end, number);
            }
        }

        return (start, end);
    }

    private static bool IsDeclarationLine(string trimmedLine, string symbol)
    {
        if (trimmedLine.Length == 0
            || trimmedLine.StartsWith("//", StringComparison.Ordinal)
            || trimmedLine.StartsWith("[", StringComparison.Ordinal))
        {
            return false;
        }

        int index = trimmedLine.IndexOf(symbol, StringComparison.Ordinal);
        while (index >= 0)
        {
            bool leftBoundary = index == 0 || !IsIdentifierCharacter(trimmedLine[index - 1]);
            int right = index + symbol.Length;
            bool rightBoundary = right == trimmedLine.Length || !IsIdentifierCharacter(trimmedLine[right]);
            if (leftBoundary && rightBoundary)
                return true;
            index = trimmedLine.IndexOf(symbol, index + 1, StringComparison.Ordinal);
        }

        return false;
    }

    private static bool IsIdentifierCharacter(char character) =>
        char.IsLetterOrDigit(character) || character == '_';

    private static KnowledgeVersionSnapshot BuildVersion(
        IReadOnlyList<OntologyNode> nodes,
        OntologyValidationData validation)
    {
        string[] projectVersions = nodes
            .Where(node => !string.IsNullOrWhiteSpace(node.ProjectName))
            .Select(node => string.IsNullOrWhiteSpace(node.ProjectVersion)
                ? node.ProjectName
                : $"{node.ProjectName} v{node.ProjectVersion}")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return new KnowledgeVersionSnapshot(
            projectVersions,
            validation.GraphVersion,
            validation.OntologyVersion,
            validation.ContentHash,
            validation.GeneratedAt);
    }

    private sealed record RankedNode(
        OntologyNode Node,
        int Score,
        IReadOnlyList<OntologyRelation> Relations,
        string Reason);

    private sealed record SearchRequest(string Term, string Purpose, int Priority);

    private sealed record IndexedSourceDocument(
        OntologyNode Node,
        OntologySourceDocumentViewModel Source);

    private sealed record VerifiedSourceMatch(
        OntologyNode Node,
        OntologySourceDocumentViewModel Source,
        int Score,
        int StartLine,
        int EndLine,
        string Reason);

    private static readonly HashSet<string> VerifiedExampleRelationTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "hasEventComponent", "forwardsTo", "usesModel", "declaredIn", "companionOf", "controlsView"
    };

    private static readonly HashSet<string> PlatformProjectSuffixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Wpf", "WinForms", "Blazor", "Maui", "Shared", "Web"
    };

    private static readonly HashSet<string> TypeRawKinds = new(StringComparer.OrdinalIgnoreCase)
    {
        "class", "interface", "struct", "record", "enum"
    };

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"[A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z_][A-Za-z0-9_]*)*")]
    private static partial Regex IdentifierRegex();

    [GeneratedRegex(@"[가-힣]{2,}")]
    private static partial Regex KoreanWordRegex();

    [GeneratedRegex(@"\{Binding\s+([A-Za-z_][A-Za-z0-9_.]*)", RegexOptions.IgnoreCase)]
    private static partial Regex BindingPathRegex();
}
