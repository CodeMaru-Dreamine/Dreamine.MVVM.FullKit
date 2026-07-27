using DreamineWeb.KnowledgeQa.Domain;
using System.Text.RegularExpressions;

namespace DreamineWeb.KnowledgeQa.Application;

/// <summary>Creates a deterministic, language-consistent public answer from persisted verified evidence.</summary>
public sealed partial class KnowledgeAnswerProjectionService : IKnowledgeAnswerProjectionService
{
    private static readonly IReadOnlyDictionary<string, int> RelationPriority =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["forwardsTo"] = 700,
            ["calls"] = 600,
            ["handles"] = 500,
            ["bindsTo"] = 400,
            ["dependsOn"] = 300,
            ["hasEventComponent"] = 250,
            ["contains"] = 10
        };

    /// <inheritdoc />
    public KnowledgeAnswerViewModel Project(KnowledgeQuestion question, AnswerRevision revision)
    {
        ArgumentNullException.ThrowIfNull(question);
        ArgumentNullException.ThrowIfNull(revision);
        bool korean = !question.Language.Equals("en", StringComparison.OrdinalIgnoreCase);
        if (question.RequestDisposition is not (
            KnowledgeRequestDisposition.Supported or KnowledgeRequestDisposition.PartiallySupported))
        {
            return new KnowledgeAnswerViewModel(
                question.RequestDisposition,
                question.ScopeReason,
                [],
                [],
                [],
                [],
                [],
                SupportExamples(question.RequestDisposition, korean),
                false);
        }

        KnowledgeQuestionIntent intent = KnowledgeQuestionIntentClassifier.Classify(question.OriginalQuestion);
        IReadOnlyList<EvidenceReference> evidence = revision.EvidenceBundle.Evidence
            .Where(item => KnowledgeEvidenceRelevancePolicy.IsRelevant(intent, item))
            .ToArray();
        string? project = FindExplicitProject(question.OriginalQuestion, revision.Version.ProjectVersions);
        string[] symbols = ExtractQuestionSymbols(question.OriginalQuestion);
        string[] requestedRelations = revision.EvidenceBundle.RetrievalDiagnostics.RequestedRelations
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        IReadOnlyList<KnowledgeRelationConstraint> relationConstraints =
            revision.EvidenceBundle.RetrievalDiagnostics.RelationConstraints;
        EvidenceReference[] relations = intent.IsGeneralConcept
            ? []
            : evidence
            .Where(item => item.Kind == EvidenceKind.OntologyRelation)
            .Where(item => requestedRelations.Length == 0 || requestedRelations.Contains(
                item.RelationType ?? string.Empty,
                StringComparer.OrdinalIgnoreCase))
            .Where(item => relationConstraints.Count == 0 || relationConstraints.Any(constraint =>
                MatchesConstraint(item, constraint)))
            .OrderByDescending(item => RelationScore(item, project, symbols))
            .ToArray();
        if (!string.IsNullOrWhiteSpace(project))
        {
            EvidenceReference[] sameProject = relations.Where(item => MatchesProject(item, project)).ToArray();
            if (sameProject.Length > 0)
                relations = sameProject;
        }

        EvidenceReference[] primaryRelations = requestedRelations.Length == 0
            ? relations.Where(item => !item.RelationType?.Equals("contains", StringComparison.OrdinalIgnoreCase) ?? false)
                .Take(1).DefaultIfEmpty(relations.FirstOrDefault()).OfType<EvidenceReference>().ToArray()
            : relations;
        EvidenceReference? directRelation = primaryRelations.FirstOrDefault();
        HashSet<string> directUris = new(StringComparer.Ordinal);
        foreach (EvidenceReference relation in primaryRelations)
        {
            if (!string.IsNullOrWhiteSpace(relation.StableUri)) directUris.Add(relation.StableUri);
            if (!string.IsNullOrWhiteSpace(relation.RelatedStableUri)) directUris.Add(relation.RelatedStableUri);
        }

        List<EvidenceReference> core = [];
        core.AddRange(primaryRelations);
        core.AddRange(evidence.Where(item =>
            item.Kind is EvidenceKind.Source or EvidenceKind.Doxygen or EvidenceKind.OntologyNode
            && !string.IsNullOrWhiteSpace(item.StableUri)
            && directUris.Contains(item.StableUri!)));
        if (core.Count == 0)
        {
            IEnumerable<EvidenceReference> fallbackNodes = evidence.Where(item =>
                item.Kind is EvidenceKind.Source or EvidenceKind.Doxygen or EvidenceKind.OntologyNode);
            if (requestedRelations.Length > 0 && symbols.Length > 0)
                fallbackNodes = fallbackNodes.Where(item => symbols.Any(symbol =>
                    EvidenceText(item).Contains(symbol, StringComparison.OrdinalIgnoreCase)));
            core.AddRange(fallbackNodes.Take(8));
        }

        KnowledgeEvidenceCardViewModel[] cards = CreateCards(core, null, korean).ToArray();
        string[] flow = directRelation is null ? [] : ParseFlow(directRelation.Title);
        string directAnswer = BuildDirectAnswer(
            question.OriginalQuestion,
            revision.Content.Summary,
            primaryRelations,
            cards.Length > 0,
            korean,
            revision.ExecutionDiagnostics.StoredDirectAnswerProducer.Equals("Codex", StringComparison.OrdinalIgnoreCase),
            requestedRelations.Length > 0 && directRelation is null);
        if (question.RequestDisposition == KnowledgeRequestDisposition.PartiallySupported)
            directAnswer = question.ScopeReason;
        HashSet<string> validIds = evidence
            .Where(item => item.Kind != EvidenceKind.OntologyRelation
                || requestedRelations.Length == 0
                || requestedRelations.Contains(item.RelationType ?? string.Empty, StringComparer.OrdinalIgnoreCase))
            .Select(item => item.Id).ToHashSet(StringComparer.Ordinal);
        KnowledgeAnswerSection[] additional = revision.Content.Sections
            .Where(section => section.EvidenceIds.Count == 0 || section.EvidenceIds.All(validIds.Contains))
            .Where(section => IsLanguageCompatible(section.Heading + " " + section.Body, korean))
            .Take(8)
            .ToArray();
        string[] unverified = cards.Length == 0 && intent.IsGeneralConcept
            ? []
            : revision.Content.UnverifiedStatements
            .Where(item => IsLanguageCompatible(item, korean))
            .Distinct(StringComparer.Ordinal)
            .Take(5)
            .ToArray();

        KnowledgeEvidenceCardViewModel[] otherProjects = intent.IsGeneralConcept || string.IsNullOrWhiteSpace(project)
            ? []
            : CreateCards(evidence.Where(item => item.Kind == EvidenceKind.OntologyRelation && !MatchesProject(item, project)), null, korean)
                .Take(8).ToArray();
        return new KnowledgeAnswerViewModel(
            question.RequestDisposition,
            directAnswer,
            flow,
            cards,
            additional,
            unverified,
            otherProjects,
            cards.Length == 0 && intent.IsGeneralConcept
                ? (korean
                    ? "직접 관련된 저장소 근거를 찾지 못했습니다."
                    : "No directly relevant repository evidence was found.")
                : string.Empty,
            cards.Length > 0 || additional.Length > 0);
    }

    private static IEnumerable<KnowledgeEvidenceCardViewModel> CreateCards(
        IEnumerable<EvidenceReference> source,
        string? requestedProject,
        bool korean)
    {
        return source
            .Where(item => string.IsNullOrWhiteSpace(requestedProject) || MatchesProject(item, requestedProject))
            .GroupBy(CardKey, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                EvidenceReference primary = group.OrderBy(item => KindPriority(item.Kind)).First();
                EvidenceReference? sourceEvidence = group.FirstOrDefault(item => item.Kind == EvidenceKind.Source);
                EvidenceReference? doxygen = group.FirstOrDefault(item =>
                    item.Kind == EvidenceKind.Doxygen
                    && item.DoxygenUrlValidated
                    && !string.IsNullOrWhiteSpace(item.DoxygenUrl));
                string symbol = NormalizeSymbol(primary.Title);
                return new KnowledgeEvidenceCardViewModel(
                    group.Key,
                    UserTitle(primary, korean),
                    UserSummary(primary, korean),
                    InferProject(primary),
                    symbol,
                    primary.RelationType,
                    primary.Origin,
                    primary.StableUri,
                    primary.RelatedStableUri,
                    sourceEvidence?.SourcePath ?? primary.SourcePath,
                    sourceEvidence?.LineStart ?? primary.LineStart,
                    sourceEvidence?.LineEnd ?? primary.LineEnd,
                    sourceEvidence?.Declaration ?? primary.Declaration,
                    sourceEvidence?.CodeExcerpt ?? primary.CodeExcerpt,
                    doxygen?.DoxygenUrl,
                    group.Select(item => item.Id).Distinct(StringComparer.Ordinal).ToArray());
            })
            .OrderBy(card => card.RelationType is null ? 1 : 0)
            .ThenBy(card => card.Title, StringComparer.OrdinalIgnoreCase);
    }

    private static string CardKey(EvidenceReference item)
    {
        string symbol = NormalizeSymbol(item.Title);
        return string.Join('|',
            InferProject(item),
            symbol,
            item.RelationType ?? string.Empty,
            item.StableUri ?? string.Empty,
            item.RelatedStableUri ?? string.Empty,
            item.SourcePath ?? string.Empty,
            item.LineStart?.ToString() ?? string.Empty,
            item.LineEnd?.ToString() ?? string.Empty);
    }

    private static int KindPriority(EvidenceKind kind) => kind switch
    {
        EvidenceKind.OntologyRelation => 0,
        EvidenceKind.Source => 1,
        EvidenceKind.Doxygen => 2,
        _ => 3
    };

    private static int RelationScore(EvidenceReference item, string? project, IReadOnlyList<string> symbols)
    {
        int score = RelationPriority.GetValueOrDefault(item.RelationType ?? string.Empty);
        if (!string.IsNullOrWhiteSpace(project) && MatchesProject(item, project)) score += 1_000;
        foreach (string symbol in symbols)
            if (EvidenceText(item).Contains(symbol, StringComparison.OrdinalIgnoreCase)) score += 180;
        return score;
    }

    private static string BuildDirectAnswer(
        string question,
        string generatedSummary,
        IReadOnlyList<EvidenceReference> relations,
        bool hasRepositoryEvidence,
        bool korean,
        bool preferGeneratedAnswer,
        bool requestedRelationMissing)
    {
        EvidenceReference? relation = relations.FirstOrDefault();
        if (requestedRelationMissing)
        {
            return korean
                ? "요청한 종류와 방향의 관계를 검증된 코드 근거에서 찾지 못했습니다. 다른 관계를 대신 근거로 사용하지 않습니다."
                : "No verified relation of the requested type and direction was found; other relations are not substituted.";
        }
        if (relations.Count > 1)
            return BuildMultipleRelationAnswer(relations, korean);
        if (!hasRepositoryEvidence)
        {
            if (WpfButtonRegex().IsMatch(question))
            {
                return korean
                    ? "일반 WPF에서는 XAML에 Button을 선언하고 Click 또는 ICommand를 연결합니다. Dreamine에서는 Command 흐름을 사용하지만, 직접 관련된 저장소 근거를 찾지 못했습니다. 파일과 줄 번호가 검증되기 전에는 구체적인 저장소 예제를 답으로 표시하지 않습니다."
                    : "In standard WPF, declare a Button in XAML and connect Click or ICommand. Dreamine uses a Command flow, but no directly relevant repository evidence was found. A concrete repository example is not shown until file and line references are verified.";
            }
            return korean
                ? "직접 관련된 저장소 근거를 찾지 못해 답변을 확정할 수 없습니다. 파일과 줄 번호가 검증되기 전에는 생성 문장을 답으로 표시하지 않습니다."
                : "No directly relevant repository evidence was found, so the answer cannot be confirmed. Generated prose is not shown as an answer until file and line references are verified.";
        }
        if (preferGeneratedAnswer
            && !string.IsNullOrWhiteSpace(generatedSummary)
            && IsLanguageCompatible(generatedSummary, korean))
        {
            return generatedSummary;
        }
        if (WpfButtonRegex().IsMatch(question))
        {
            return korean
                ? "일반 WPF에서는 XAML에 Button을 선언하고 Click 또는 ICommand를 연결합니다. 아래에는 Button·XAML·Command 개념을 실제로 포함한 저장소 근거만 표시합니다."
                : "In standard WPF, declare a Button in XAML and connect Click or ICommand. Only repository evidence that actually contains the Button, XAML, or Command concept is shown below.";
        }
        if (relation is not null)
        {
            string[] flow = ParseFlow(relation.Title);
            if (flow.Length >= 2)
            {
                return korean
                    ? $"{flow[0]}는 {flow[^1]}로 {RelationVerb(relation.RelationType, true)}."
                    : $"{flow[0]} {RelationVerb(relation.RelationType, false)} {flow[^1]}.";
            }
        }
        if (IsLanguageCompatible(generatedSummary, korean))
            return generatedSummary;
        return korean
            ? "검증된 코드 근거에서 질문과 직접 관련된 구성 요소를 확인했습니다."
            : "Verified code evidence identifies the components directly related to the question.";
    }

    private static string BuildMultipleRelationAnswer(
        IReadOnlyList<EvidenceReference> relations,
        bool korean)
    {
        (EvidenceReference Relation, string Source, string Target)[] facts = relations
            .Select(relation =>
            {
                string[] flow = ParseFlow(relation.Title);
                return (
                    Relation: relation,
                    Source: flow.ElementAtOrDefault(0) ?? string.Empty,
                    Target: flow.ElementAtOrDefault(1) ?? string.Empty);
            })
            .Where(fact => fact.Source.Length > 0 && fact.Target.Length > 0)
            .ToArray();
        if (facts.Length == 0)
            return korean ? "요청한 관계를 여러 건 확인했습니다." : "Multiple requested relations were verified.";

        bool sameSource = facts.Select(fact => fact.Source).Distinct(StringComparer.OrdinalIgnoreCase).Count() == 1;
        bool sameTarget = facts.Select(fact => fact.Target).Distinct(StringComparer.OrdinalIgnoreCase).Count() == 1;
        bool sameType = facts.Select(fact => fact.Relation.RelationType ?? string.Empty)
            .Distinct(StringComparer.OrdinalIgnoreCase).Count() == 1;
        string relationType = facts[0].Relation.RelationType ?? string.Empty;
        if (sameSource && sameType)
        {
            string targets = JoinNames(facts.Select(fact => fact.Target), korean);
            return relationType.ToLowerInvariant() switch
            {
                "implements" => korean
                    ? $"{facts[0].Source}는 {targets}를 구현합니다."
                    : $"{facts[0].Source} implements {targets}.",
                "inherits" => korean
                    ? $"{facts[0].Source}는 {targets}를 상속합니다."
                    : $"{facts[0].Source} inherits {targets}.",
                _ => string.Join(' ', facts.Select(fact => korean
                    ? $"{fact.Source}는 {fact.Target}로 {RelationVerb(fact.Relation.RelationType, true)}."
                    : $"{fact.Source} {RelationVerb(fact.Relation.RelationType, false)} {fact.Target}."))
            };
        }
        if (sameTarget && sameType && relationType.Equals("implements", StringComparison.OrdinalIgnoreCase))
        {
            string sources = JoinNames(facts.Select(fact => fact.Source), korean);
            return korean
                ? $"{sources}가 {facts[0].Target}를 구현합니다."
                : $"{sources} implement {facts[0].Target}.";
        }
        return string.Join(' ', facts.Select(fact => korean
            ? $"{fact.Source}는 {fact.Target}로 {RelationVerb(fact.Relation.RelationType, true)}."
            : $"{fact.Source} {RelationVerb(fact.Relation.RelationType, false)} {fact.Target}."));
    }

    private static string JoinNames(IEnumerable<string> names, bool korean)
    {
        string[] values = names.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (values.Length <= 1)
            return values.FirstOrDefault() ?? string.Empty;
        string conjunction = korean ? " 및 " : " and ";
        return values.Length == 2
            ? values[0] + conjunction + values[1]
            : string.Join(", ", values[..^1]) + conjunction + values[^1];
    }

    private static string RelationVerb(string? relation, bool korean) => (relation ?? string.Empty).ToLowerInvariant() switch
    {
        "forwardsto" => korean ? "전달됩니다" : "forwards to",
        "calls" => korean ? "호출합니다" : "calls",
        "handles" => korean ? "처리합니다" : "handles",
        "bindsto" => korean ? "바인딩됩니다" : "binds to",
        "dependson" => korean ? "의존합니다" : "depends on",
        "haseventcomponent" => korean ? "이벤트 구성 요소로 연결됩니다" : "uses the event component",
        "implements" => korean ? "구현합니다" : "implements",
        "inherits" => korean ? "상속합니다" : "inherits",
        _ => korean ? "연결됩니다" : "connects to"
    };

    private static string[] ParseFlow(string title)
    {
        string[] parts = title.Split('→', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 3)
            return [];
        return [NormalizeSymbol(parts[0]), NormalizeSymbol(parts[^1])];
    }

    private static bool MatchesConstraint(
        EvidenceReference relation,
        KnowledgeRelationConstraint constraint)
    {
        if (!relation.RelationType?.Equals(constraint.RelationType, StringComparison.OrdinalIgnoreCase) ?? true)
            return false;
        if (string.IsNullOrWhiteSpace(constraint.AnchorSymbol))
            return true;
        string[] parts = relation.Title.Split('→', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 3)
            return false;
        string endpoint = constraint.Direction == KnowledgeRelationDirection.Outgoing ? parts[0] : parts[^1];
        string normalized = NormalizeSymbol(endpoint);
        return normalized.Equals(constraint.AnchorSymbol, StringComparison.OrdinalIgnoreCase)
            || normalized.EndsWith('.' + constraint.AnchorSymbol, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeSymbol(string value)
    {
        string text = value.Replace(" (", "|(", StringComparison.Ordinal).Split('|')[0].Trim();
        string[] arrows = text.Split('→', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (arrows.Length >= 3) text = arrows[0];
        string[] segments = text.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length >= 2 && segments[^1].Equals(segments[^2], StringComparison.Ordinal))
            text = string.Join('.', segments[..^1]);
        return text;
    }

    private static string UserTitle(EvidenceReference item, bool korean) => item.Kind switch
    {
        EvidenceKind.OntologyRelation => item.Title,
        EvidenceKind.Source => korean ? $"{NormalizeSymbol(item.Title)} 코드 선언" : $"{NormalizeSymbol(item.Title)} source declaration",
        EvidenceKind.Doxygen => korean ? $"{NormalizeSymbol(item.Title)} API 문서" : $"{NormalizeSymbol(item.Title)} API documentation",
        _ => NormalizeSymbol(item.Title)
    };

    private static string UserSummary(EvidenceReference item, bool korean)
    {
        if (!string.IsNullOrWhiteSpace(item.Summary) && IsLanguageCompatible(item.Summary, korean))
            return item.Summary;
        if (item.Kind == EvidenceKind.OntologyRelation)
            return korean ? "소스에서 검증된 코드 관계입니다." : "This code relation is source-verified.";
        if (item.Kind == EvidenceKind.Source)
            return korean ? "stable URI로 확인한 실제 선언 일부입니다." : "This declaration excerpt was resolved by stable URI.";
        if (item.Kind == EvidenceKind.Doxygen)
            return korean ? "Doxygen XML에서 확인한 선언 정보입니다." : "Declaration information verified from Doxygen XML.";
        return korean ? "소스 검증된 코드 요소입니다." : "This is a source-verified code element.";
    }

    private static string? FindExplicitProject(string question, IReadOnlyList<string> projectVersions) =>
        projectVersions.Select(ProjectNameFromVersion)
            .FirstOrDefault(project => question.Contains(project, StringComparison.OrdinalIgnoreCase));

    private static string ProjectNameFromVersion(string value)
    {
        int marker = value.LastIndexOf(" v", StringComparison.OrdinalIgnoreCase);
        return marker > 0 ? value[..marker] : value;
    }

    private static bool MatchesProject(EvidenceReference item, string project) =>
        EvidenceText(item).Contains(project, StringComparison.OrdinalIgnoreCase);

    private static string EvidenceText(EvidenceReference item) => string.Join(' ',
        item.Title,
        item.StableUri,
        item.RelatedStableUri,
        item.SourcePath);

    private static string InferProject(EvidenceReference item)
    {
        string text = item.SourcePath ?? item.Title;
        string normalized = text.Replace('\\', '/');
        Match demo = DemoProjectRegex().Match(normalized);
        if (demo.Success) return demo.Groups[1].Value;
        string symbol = NormalizeSymbol(item.Title);
        int dot = symbol.IndexOf('.');
        return dot > 0 ? symbol[..dot] : string.Empty;
    }

    private static string[] ExtractQuestionSymbols(string question) =>
        SymbolRegex().Matches(question).Select(match => match.Value)
            .Where(value => value.Contains('.'))
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

    private static bool IsLanguageCompatible(string value, bool korean)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;
        bool hasHangul = HangulRegex().IsMatch(value);
        return korean ? hasHangul : !hasHangul;
    }

    private static string SupportExamples(KnowledgeRequestDisposition disposition, bool korean)
    {
        if (disposition == KnowledgeRequestDisposition.OutOfScope)
            return korean
                ? "Dreamine API, WPF·MVVM·XAML·Binding·Command 사용법 또는 저장소 코드 흐름을 질문해 주세요."
                : "Ask about Dreamine APIs, WPF/MVVM/XAML/Binding/Command usage, or repository code flows.";
        return korean
            ? "질문의 프로젝트·타입·멤버와 확인하려는 동작을 더 구체적으로 적어 주세요."
            : "Specify the project, type, member, and behavior that should be verified.";
    }

    [GeneratedRegex("(?i)WPF.*(button|버튼)|(button|버튼).*WPF")]
    private static partial Regex WpfButtonRegex();

    [GeneratedRegex("[가-힣]")]
    private static partial Regex HangulRegex();

    [GeneratedRegex("[A-Za-z_][A-Za-z0-9_]*(?:\\.[A-Za-z_][A-Za-z0-9_]*)+")]
    private static partial Regex SymbolRegex();

    [GeneratedRegex("(?:998\\. DEMO|998%20DEMO)/([^/]+)", RegexOptions.IgnoreCase)]
    private static partial Regex DemoProjectRegex();
}
