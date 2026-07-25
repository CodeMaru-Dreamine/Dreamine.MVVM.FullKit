using DreamineWeb.KnowledgeQa.Domain;
using System.Text.RegularExpressions;

namespace DreamineWeb.KnowledgeQa.Application;

/// <summary>Applies deterministic relevance, clarification, safety, and evidence gates.</summary>
public sealed partial class KnowledgeRequestScopePolicy : IKnowledgeRequestScopePolicy
{
    private readonly KnowledgeQaOptions _options;

    public KnowledgeRequestScopePolicy(KnowledgeQaOptions options) =>
        _options = options ?? throw new ArgumentNullException(nameof(options));

    /// <inheritdoc />
    public KnowledgeScopeDecision EvaluateQuestion(string question, string language)
    {
        string value = (question ?? string.Empty).Trim();
        bool korean = !language.Equals("en", StringComparison.OrdinalIgnoreCase);
        if (HazardousDomainRegex().IsMatch(value)
            && SafeSimulationRegex().IsMatch(value)
            && GenerationRegex().IsMatch(value))
        {
            return new KnowledgeScopeDecision(
                KnowledgeRequestDisposition.NeedsClarification,
                korean
                    ? "실제 물리 장비와 분리된 교육·가상 시뮬레이터인지 확인해야 합니다."
                    : "The request must be confirmed as an educational or virtual simulator isolated from physical equipment.",
                korean
                    ? "실제 장비를 제어하지 않고 가상 데이터만 사용하는 UI인지 명시해 주세요."
                    : "Confirm that this is a virtual-data UI with no physical equipment control.");
        }
        if (DangerousPhysicalControlRegex().IsMatch(value))
        {
            if (SafeSimulationRegex().IsMatch(value))
            {
                return new KnowledgeScopeDecision(
                    KnowledgeRequestDisposition.NeedsClarification,
                    korean
                        ? "실제 물리 장비와 분리된 교육·가상 시뮬레이터인지 확인해야 합니다."
                        : "The request must be confirmed as an educational or virtual simulator isolated from physical equipment.",
                    korean
                        ? "실제 장비를 제어하지 않고 가상 데이터만 사용하는 UI인지 명시해 주세요."
                        : "Confirm that this is a virtual-data UI with no physical equipment control.");
            }

            return new KnowledgeScopeDecision(
                KnowledgeRequestDisposition.Restricted,
                korean
                    ? "실제 무기·발사·유도·점화 또는 위험 물리 장비의 제어 코드는 지원하지 않습니다."
                    : "Control code for real weapons, launch, guidance, ignition, or hazardous physical equipment is not supported.");
        }

        if (DreamineBrandRegex().IsMatch(value)
            && SpeculativeOutcomeRegex().IsMatch(value)
            && !ExplicitIntegrationBoundaryRegex().IsMatch(value))
        {
            return new KnowledgeScopeDecision(
                KnowledgeRequestDisposition.NeedsClarification,
                korean
                    ? "Dreamine 이름만으로 현실 세계의 임의 동작이 자동 지원된다고 판단할 수 없습니다."
                    : "Mentioning Dreamine does not establish support for an arbitrary real-world outcome.",
                korean
                    ? "구현하려는 소프트웨어 화면, 입력, 외부 API와 기대 동작을 구체적으로 적어 주세요."
                    : "Specify the software screen, inputs, external API, and expected behavior to implement.");
        }

        if (AmbiguousApplicationRequestRegex().IsMatch(value) && !ConcreteApplicationScopeRegex().IsMatch(value))
        {
            return new KnowledgeScopeDecision(
                KnowledgeRequestDisposition.NeedsClarification,
                korean
                    ? "응용프로그램의 목적, 사용자, 입력 데이터와 필요한 화면 범위가 부족합니다."
                    : "The application purpose, users, input data, and required screens are not clear enough.",
                korean
                    ? "누가 어떤 데이터를 사용해 무엇을 하려는지와 필요한 화면·기능을 알려주세요."
                    : "Describe who will use it, which data it consumes, the goal, and the required screens or features.");
        }

        KnowledgeQuestionIntent intent = KnowledgeQuestionIntentClassifier.Classify(value);
        bool hasRepositoryContext = RepositoryContextRegex().IsMatch(value)
            || KnowledgeQuestionIntentClassifier.UsageProfile(value) is not null
            || (intent.IsGeneralConcept
                && intent.FocusTerms.Count > 0
                && TechnicalImplementationIntentRegex().IsMatch(value));
        if (!hasRepositoryContext)
        {
            return new KnowledgeScopeDecision(
                KnowledgeRequestDisposition.OutOfScope,
                korean
                    ? "Dreamine 코드·구조·문서 또는 Dreamine 기반 응용프로그램과 관련된 질문이 아닙니다."
                    : "The question is unrelated to Dreamine code, architecture, documentation, or Dreamine applications.");
        }

        return new KnowledgeScopeDecision(KnowledgeRequestDisposition.Supported, string.Empty);
    }

    /// <inheritdoc />
    public KnowledgeScopeDecision EvaluateEvidence(EvidenceBundle bundle, string language)
    {
        ArgumentNullException.ThrowIfNull(bundle);
        KnowledgeQuestionIntent intent = KnowledgeQuestionIntentClassifier.Classify(bundle.Question);
        if (intent.IsGeneralConcept)
        {
            int relevantSources = bundle.Evidence.Count(item =>
                item.Kind == EvidenceKind.Source
                && KnowledgeEvidenceRelevancePolicy.IsRelevant(intent, item));
            int relevantOntology = bundle.Evidence.Count(item =>
                item.Kind == EvidenceKind.OntologyNode
                && KnowledgeEvidenceRelevancePolicy.IsRelevant(intent, item));
            if (relevantSources > 0 && relevantOntology > 0)
                return new KnowledgeScopeDecision(KnowledgeRequestDisposition.Supported, string.Empty);

            bool koreanGeneral = !language.Equals("en", StringComparison.OrdinalIgnoreCase);
            return new KnowledgeScopeDecision(
                KnowledgeRequestDisposition.InsufficientEvidence,
                koreanGeneral
                    ? $"질문의 핵심 개념을 실제로 포함한 저장소 근거가 부족합니다(소스 {relevantSources}, 온톨로지 {relevantOntology})."
                    : $"Repository evidence containing the question's core concept is insufficient (source {relevantSources}, ontology {relevantOntology}).");
        }
        bool sufficient = bundle.OntologyEvidenceCount >= Math.Max(1, _options.MinimumOntologyEvidence)
            && bundle.DoxygenReferenceCount >= Math.Max(1, _options.MinimumDoxygenEvidence);
        if (sufficient)
            return new KnowledgeScopeDecision(KnowledgeRequestDisposition.Supported, string.Empty);

        bool korean = !language.Equals("en", StringComparison.OrdinalIgnoreCase);
        return new KnowledgeScopeDecision(
            KnowledgeRequestDisposition.InsufficientEvidence,
            korean
                ? $"검증 기준을 충족하지 못했습니다(온톨로지 {bundle.OntologyEvidenceCount}, Doxygen {bundle.DoxygenReferenceCount})."
                : $"The evidence threshold was not met (ontology {bundle.OntologyEvidenceCount}, Doxygen {bundle.DoxygenReferenceCount}).");
    }

    [GeneratedRegex("(?i)(weapon|missile|firearm|detonat|ignition|guidance|launch|무기|미사일|총기|폭발|점화|유도|발사).{0,40}(control|code|program|제어|코드|프로그램)|(?:control|code|program|제어|코드|프로그램).{0,40}(weapon|missile|firearm|detonat|ignition|guidance|launch|무기|미사일|총기|폭발|점화|유도|발사)")]
    private static partial Regex DangerousPhysicalControlRegex();

    [GeneratedRegex("(?i)(weapon|missile|firearm|detonat|ignition|guidance|launch|무기|미사일|총기|폭발|점화|유도|발사)")]
    private static partial Regex HazardousDomainRegex();

    [GeneratedRegex("(?i)(create|build|generate|make|만들|생성|작성)")]
    private static partial Regex GenerationRegex();

    [GeneratedRegex("(?i)(educational|training|simulat|virtual data|mock data|dashboard|status UI|교육|훈련|시뮬레이터|모의|가상 데이터|대시보드|상태 UI)")]
    private static partial Regex SafeSimulationRegex();

    [GeneratedRegex("(?i)(codemaru|samplesmart|wpf|winforms|blazor|maui|mvvm|viewmodel|xaml|binding|icommand|command|dependency injection|\\bDI\\b|doxygen|ontology|knowledge graph|stable uri|forwardsto|haseventcomponent|relaycommand|dreamineevent|코드마루|뷰모델|독시젠|온톨로지|지식 ?그래프|코드|클래스|메서드|커맨드|명령|바인딩|이벤트|라이브러리|프로젝트|의존성|인터페이스|생성 코드)|(?:[A-Z][A-Za-z0-9_]*(?:View|Control|Panel|Grid|Picker|List|Item|Window|Page|Model|Service|Command|Event|Attribute|Generator))")]
    private static partial Regex RepositoryContextRegex();

    [GeneratedRegex("(?i)(dreamine|드리마인)")]
    private static partial Regex DreamineBrandRegex();

    [GeneratedRegex("(?i)(자동|automatically|버튼|선택|클릭|입력|체크|click|select|input|check).{0,80}(배달|배송|주문|결제|도착|이동|발사|점화|운전|요리|잠그|잠기|문을? ?열|문을? ?닫|날씨|온도|deliver|ship|order|payment|arrive|move|launch|ignite|drive|cook|lock|unlock|open the door|close the door|weather|temperature)|(?:배달|배송|주문|결제|도착|이동|발사|점화|운전|요리|잠그|잠기|문을? ?열|문을? ?닫|날씨|온도|deliver|ship|order|payment|arrive|move|launch|ignite|drive|cook|lock|unlock|open the door|close the door|weather|temperature).{0,80}(자동|automatically|버튼|선택|클릭|입력|체크|click|select|input|check)")]
    private static partial Regex SpeculativeOutcomeRegex();

    [GeneratedRegex("(?i)(external api|webhook|http api|integration|device protocol|mock|simulat|외부 ?API|웹훅|연동 ?API|장비 프로토콜|모의|시뮬레이터)")]
    private static partial Regex ExplicitIntegrationBoundaryRegex();

    [GeneratedRegex("(?i)(어떻게|방법|예제|구현|작성|코드|연결|바인딩|호출|전달|API|how|example|implement|code|bind|connect|call|forward|xaml|viewmodel|icommand|command)")]
    private static partial Regex TechnicalImplementationIntentRegex();

    [GeneratedRegex("(?i)(dreamine|드리마인).{0,30}(app|application|program|앱|응용 ?프로그램|프로그램).{0,30}(create|build|generate|make|만들|생성|작성)")]
    private static partial Regex AmbiguousApplicationRequestRegex();

    [GeneratedRegex("(?i)(user|screen|page|data|input|output|workflow|database|api|dashboard|viewmodel|사용자|화면|페이지|데이터|입력|출력|흐름|DB|데이터베이스|대시보드|기능|목적)")]
    private static partial Regex ConcreteApplicationScopeRegex();
}
