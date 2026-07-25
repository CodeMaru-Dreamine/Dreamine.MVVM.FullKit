using DreamineWeb.KnowledgeQa.Domain;
using DreamineWeb.Ontology.Domain;
using System.Text.RegularExpressions;

namespace DreamineWeb.KnowledgeQa.Application;

internal enum KnowledgeQuestionIntentKind
{
    Unclassified,
    GeneralConcept,
    SpecificSymbol
}

internal enum KnowledgeQuestionConcept
{
    None,
    WpfButton,
    XamlBinding,
    ICommand,
    TextInputBinding,
    ItemsSelection,
    WindowPopup,
    ChannelAction
}

internal sealed record KnowledgeUsageSearchProfile(
    string Intent,
    IReadOnlyList<string> Concepts,
    IReadOnlyList<string> Symbols,
    IReadOnlyList<string> Relations,
    IReadOnlyList<string> SourceKinds,
    string? Project);

internal sealed record KnowledgeQuestionIntent(
    KnowledgeQuestionIntentKind Kind,
    KnowledgeQuestionConcept Concept)
{
    public bool IsGeneralConcept => Kind == KnowledgeQuestionIntentKind.GeneralConcept;
    public IReadOnlyList<string> FocusTerms { get; init; } = [];
    public IReadOnlyList<string> EvidenceTerms { get; init; } = [];
    public bool RequiresXamlEvidence { get; init; }
}

/// <summary>Separates general concepts from explicit repository symbol relationship questions.</summary>
internal static partial class KnowledgeQuestionIntentClassifier
{
    private static readonly HashSet<string> FrameworkIdentifiers = new(StringComparer.OrdinalIgnoreCase)
    {
        "WPF", "XAML", "Binding", "ICommand", "Command", "Click", "MVVM",
        "ViewModel", "Model", "Event", "Dreamine", "Blazor", "WinForms", "Maui",
        "Button.Command", "SourceGenerator", "Doxygen"
    };

    internal static KnowledgeQuestionIntent Classify(string question)
    {
        string value = question ?? string.Empty;
        KnowledgeUsageSearchProfile? usage = UsageProfile(value);
        KnowledgeQuestionConcept concept = usage?.Intent switch
        {
            "button-command" => KnowledgeQuestionConcept.WpfButton,
            "input-binding" => KnowledgeQuestionConcept.TextInputBinding,
            "items-selection" => KnowledgeQuestionConcept.ItemsSelection,
            "window-popup" => KnowledgeQuestionConcept.WindowPopup,
            "channel-action" => KnowledgeQuestionConcept.ChannelAction,
            _ => WpfButtonRegex().IsMatch(value)
                ? KnowledgeQuestionConcept.WpfButton
                : XamlBindingRegex().IsMatch(value)
                ? KnowledgeQuestionConcept.XamlBinding
                : ICommandRegex().IsMatch(value)
                    ? KnowledgeQuestionConcept.ICommand
                    : KnowledgeQuestionConcept.None
        };
        string[] focusTerms = ExtractFocusTerms(value, concept);
        string[] evidenceTerms = ExtractEvidenceTerms(value, concept);
        bool relationQuestion = RelationQuestionRegex().IsMatch(value);
        bool generalQuestion = GeneralQuestionRegex().IsMatch(value)
            && TechnicalContextRegex().IsMatch(value)
            && !relationQuestion;
        bool namesSpecificSymbol = IdentifierRegex().Matches(value)
            .Select(match => match.Value)
            .Any(symbol => IsExplicitSymbol(symbol, relationQuestion));
        KnowledgeQuestionIntentKind kind = usage?.Symbols.Count > 0
            ? KnowledgeQuestionIntentKind.SpecificSymbol
            : generalQuestion
            ? KnowledgeQuestionIntentKind.GeneralConcept
            : namesSpecificSymbol
                ? KnowledgeQuestionIntentKind.SpecificSymbol
                : concept == KnowledgeQuestionConcept.None && focusTerms.Length == 0
                    ? KnowledgeQuestionIntentKind.Unclassified
                    : KnowledgeQuestionIntentKind.GeneralConcept;
        return new KnowledgeQuestionIntent(kind, concept)
        {
            FocusTerms = focusTerms,
            EvidenceTerms = evidenceTerms,
            RequiresXamlEvidence = RequiresMarkup(value, evidenceTerms)
        };
    }

    internal static string[] SearchTerms(KnowledgeQuestionIntent intent) => intent.FocusTerms
        .Concat(intent.EvidenceTerms)
        .Concat(intent.Concept switch
        {
            KnowledgeQuestionConcept.WpfButton => ["Button", "Command", "XAML", "WPF"],
            KnowledgeQuestionConcept.XamlBinding => ["Binding", "XAML", "Command"],
            KnowledgeQuestionConcept.ICommand => ["ICommand", "Command", "XAML", "WPF"],
            KnowledgeQuestionConcept.TextInputBinding => ["Binding", "INotifyPropertyChanged", "SetProperty"],
            KnowledgeQuestionConcept.ItemsSelection => ["ComboBox", "ItemsSource", "SelectedItem"],
            KnowledgeQuestionConcept.WindowPopup => ["Window", "Popup"],
            KnowledgeQuestionConcept.ChannelAction => ["AddChannel"],
            _ => []
        })
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    internal static string[] SpecificSymbols(string question)
    {
        string value = question ?? string.Empty;
        bool relationQuestion = RelationQuestionRegex().IsMatch(value);
        if (!relationQuestion && GeneralQuestionRegex().IsMatch(value))
            return [];
        return IdentifierRegex().Matches(value)
            .Select(match => match.Value)
            .Where(symbol => IsExplicitSymbol(symbol, relationQuestion))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(symbol => symbol.Contains('.'))
            .ThenByDescending(symbol => symbol.Length)
            .ToArray();
    }

    internal static string[] Concepts(string question, KnowledgeQuestionIntent intent)
    {
        KnowledgeUsageSearchProfile? usage = UsageProfile(question);
        if (usage is not null)
            return usage.Concepts.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        List<string> concepts = [.. intent.FocusTerms, .. intent.EvidenceTerms];
        if (intent.Concept == KnowledgeQuestionConcept.WpfButton) concepts.AddRange(["WPF", "XAML", "Button", "Command"]);
        if (intent.Concept == KnowledgeQuestionConcept.XamlBinding) concepts.AddRange(["XAML", "Binding"]);
        if (intent.Concept == KnowledgeQuestionConcept.ICommand) concepts.AddRange(["ICommand", "Command"]);
        if (ContainsAny(question, "DreamineCommand")) concepts.Add("DreamineCommand");
        if (ContainsAny(question, "DreamineEvent", "hasEventComponent", "forwardsTo", "이벤트")) concepts.Add("DreamineEventComponent");
        if (ContainsAny(question, "생성 코드", "source generator", "generated code")) concepts.Add("SourceGenerator");
        return concepts.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static string[] ExtractFocusTerms(string value, KnowledgeQuestionConcept concept)
    {
        KnowledgeUsageSearchProfile? usage = UsageProfile(value);
        if (usage is not null)
            return usage.Concepts.ToArray();
        List<string> terms = IdentifierRegex().Matches(value)
            .Select(match => match.Value)
            .Where(identifier => !FrameworkIdentifiers.Contains(identifier))
            .Where(identifier => identifier.Length > 1)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (concept == KnowledgeQuestionConcept.WpfButton || ContainsAny(value, "버튼")) terms.Add("Button");
        if (concept == KnowledgeQuestionConcept.ICommand) terms.Add("ICommand");
        return terms.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static string[] ExtractEvidenceTerms(string value, KnowledgeQuestionConcept concept)
    {
        KnowledgeUsageSearchProfile? usage = UsageProfile(value);
        if (usage is not null)
            return [];
        List<string> terms = [];
        if (ContainsAny(value, "XAML")) terms.Add("XAML");
        if (concept == KnowledgeQuestionConcept.WpfButton) terms.AddRange(["Button", "Command"]);
        if (concept == KnowledgeQuestionConcept.XamlBinding || ContainsAny(value, "바인딩", "Binding")) terms.Add("Binding");
        if (concept == KnowledgeQuestionConcept.ICommand) terms.Add("ICommand");
        if (ContainsAny(value, "항목 목록", "목록 바인딩", "item source", "ItemsSource")) terms.Add("ItemsSource");
        if (ContainsAny(value, "선택값", "선택 값", "현재 선택", "SelectedItem")) terms.Add("SelectedItem");
        if (ContainsAny(value, "SelectedValue")) terms.Add("SelectedValue");
        if (ContainsAny(value, "입력값", "입력 값", "텍스트 바인딩")) terms.Add("Text");
        if (ContainsAny(value, "명령", "커맨드", "Command")) terms.Add("Command");
        return terms.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static bool IsExplicitSymbol(string value, bool relationQuestion)
    {
        if (FrameworkIdentifiers.Contains(value))
            return false;
        if (value.Contains('.'))
            return true;
        return relationQuestion
            && value.Length > 2
            && char.IsUpper(value[0])
            && value.Skip(1).Any(char.IsUpper);
    }

    private static bool RequiresMarkup(string value, IReadOnlyList<string> evidenceTerms) =>
        ContainsAny(value, "XAML", "WPF", "바인딩", "Binding")
        || UsageProfile(value)?.SourceKinds.Contains("Xaml", StringComparer.OrdinalIgnoreCase) == true
        || evidenceTerms.Any(term => term is "ItemsSource" or "SelectedItem" or "SelectedValue");

    internal static KnowledgeUsageSearchProfile? UsageProfile(string value)
    {
        string project = ProjectRegex().Match(value) is { Success: true } match
            ? match.Value
            : string.Empty;
        if (ChannelRegex().IsMatch(value) && AddActionRegex().IsMatch(value))
            return new("channel-action", ["AddChannel", "Command"], ["AddChannel"],
                ["forwardsTo", "hasEventComponent"], ["Xaml", "ViewModel", "Event"],
                string.IsNullOrWhiteSpace(project) ? null : project);
        if (ButtonSignalRegex().IsMatch(value) && ExecuteActionRegex().IsMatch(value))
            return new("button-command", ["DreamineCommand", "Command", "Button.Command"], [],
                ["forwardsTo", "hasEventComponent"], ["Xaml", "ViewModel", "Event"], null);
        if (InputSignalRegex().IsMatch(value) && ChangeOrSyncRegex().IsMatch(value))
            return new("input-binding", ["Binding", "TextBox", "Text", "INotifyPropertyChanged", "SetProperty"], [],
                [], ["Xaml", "ViewModel"], null);
        if (ListControlRegex().IsMatch(value) && ListSelectionRegex().IsMatch(value))
            return new("items-selection", ["ComboBox", "ItemsSource", "SelectedItem"], [],
                [], ["Xaml", "ViewModel"], null);
        if (WindowSignalRegex().IsMatch(value) && OpenActionRegex().IsMatch(value))
            return new("window-popup", ["Window", "Popup", "ShowDialog"], [],
                ["invokesNavigation"], ["Xaml", "View", "ViewModel", "Service"], null);
        return null;
    }

    private static bool ContainsAny(string value, params string[] candidates) =>
        candidates.Any(candidate => value.Contains(candidate, StringComparison.OrdinalIgnoreCase));

    [GeneratedRegex("(?i)(?<![A-Za-z0-9_])WPF(?![A-Za-z0-9_]).*(button|버튼)|(button|버튼).*(?<![A-Za-z0-9_])WPF(?![A-Za-z0-9_])")]
    private static partial Regex WpfButtonRegex();

    [GeneratedRegex("(?i)(?<![A-Za-z0-9_])XAML(?![A-Za-z0-9_]).*(binding|바인딩)|(binding|바인딩).*(?<![A-Za-z0-9_])XAML(?![A-Za-z0-9_])")]
    private static partial Regex XamlBindingRegex();

    [GeneratedRegex("(?i)(?<![A-Za-z0-9_])ICommand(?![A-Za-z0-9_])")]
    private static partial Regex ICommandRegex();

    [GeneratedRegex("(?i)(어떻게|방법|예제|사용|추가|연결|바인딩|설명|찾아|하려면|반영|가져오|띄우|처리|how|example|use|add|connect|bind|explain|find|show|open|handle)")]
    private static partial Regex GeneralQuestionRegex();

    [GeneratedRegex("(?i)(Dreamine|WPF|XAML|MVVM|ViewModel|ICommand|Command|Binding|ComboBox|ItemsSource|SelectedItem|Window|Popup|Blazor|WinForms|Maui|드리마인|뷰모델|명령|커맨드|바인딩|버튼|컨트롤|입력|글자|선택|목록|콤보박스|창|팝업|채널)")]
    private static partial Regex TechnicalContextRegex();

    [GeneratedRegex(@"(?i)(어디로\s*전달|전달되|호출되|누가\s*호출|상속|구현하|어떤\s*(?:클래스|타입)(?:에서|이)?\s*사용|forwards?To|hasEventComponent|dependsOn|invokesNavigation|controlsView|who calls|where.*forward|which (?:classes|types) use|inherits|implements)")]
    private static partial Regex RelationQuestionRegex();

    [GeneratedRegex("[A-Za-z_][A-Za-z0-9_]*(?:\\.[A-Za-z_][A-Za-z0-9_]*)*")]
    private static partial Regex IdentifierRegex();

    [GeneratedRegex("(?i)(SampleSmart|SampleCore|SampleEnterprise|CodeMaru)")]
    private static partial Regex ProjectRegex();
    [GeneratedRegex("(?i)(button|버튼|클릭)")]
    private static partial Regex ButtonSignalRegex();
    [GeneratedRegex("(?i)(execute|run|invoke|command|실행|처리|동작|코드)")]
    private static partial Regex ExecuteActionRegex();
    [GeneratedRegex("(?i)(input|text|textbox|입력|글자|문자|텍스트)")]
    private static partial Regex InputSignalRegex();
    [GeneratedRegex("(?i)(change|update|sync|reflect|binding|바뀌|변경|갱신|동기|반영|바인딩|연결)")]
    private static partial Regex ChangeOrSyncRegex();
    [GeneratedRegex("(?i)(combobox|listbox|combo|list|콤보박스|리스트|목록)")]
    private static partial Regex ListControlRegex();
    [GeneratedRegex("(?i)(items|source|select|value|항목|넣|선택|값|가져오)")]
    private static partial Regex ListSelectionRegex();
    [GeneratedRegex("(?i)(window|popup|dialog|창|팝업|대화상자)")]
    private static partial Regex WindowSignalRegex();
    [GeneratedRegex("(?i)(open|show|launch|띄우|열|표시)")]
    private static partial Regex OpenActionRegex();
    [GeneratedRegex("(?i)(channel|채널)")]
    private static partial Regex ChannelRegex();
    [GeneratedRegex("(?i)(add|create|추가|생성)")]
    private static partial Regex AddActionRegex();
}

internal sealed record KnowledgeEvidenceRelevanceScore(
    bool IsRelevant,
    int Score,
    int FocusMatches,
    int EvidenceMatches,
    string Reason);

/// <summary>Rejects evidence that does not contain the question's independently extracted core concepts.</summary>
internal static class KnowledgeEvidenceRelevancePolicy
{
    internal static bool IsRelevant(KnowledgeQuestionIntent intent, OntologyNode node) =>
        Evaluate(intent, NodeText(node), node.SourcePath).IsRelevant;

    internal static bool IsRelevant(KnowledgeQuestionIntent intent, EvidenceReference evidence)
    {
        if (!intent.IsGeneralConcept)
            return true;
        if (evidence.Provenance.Equals("source-verified-concept-example", StringComparison.OrdinalIgnoreCase)
            || evidence.Provenance.Equals("repository-command-trace", StringComparison.OrdinalIgnoreCase))
            return true;
        string text = string.Join(' ',
            evidence.Title,
            evidence.Summary,
            evidence.SourcePath,
            evidence.Declaration,
            evidence.CodeExcerpt,
            evidence.RelationType);
        KnowledgeQuestionIntent evidenceIntent = evidence.Kind == EvidenceKind.Doxygen
            ? intent with { RequiresXamlEvidence = false }
            : intent;
        return Evaluate(evidenceIntent, text, evidence.SourcePath).IsRelevant;
    }

    internal static KnowledgeEvidenceRelevanceScore Evaluate(
        KnowledgeQuestionIntent intent,
        string text,
        string? sourcePath)
    {
        if (!intent.IsGeneralConcept)
            return new KnowledgeEvidenceRelevanceScore(true, 0, 0, 0, "specific-symbol evidence");
        string[] focus = intent.FocusTerms.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        string[] evidence = intent.EvidenceTerms
            .Where(term => !term.Equals("XAML", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        int focusMatches = focus.Count(term => ContainsTerm(text, term));
        int evidenceMatches = evidence.Count(term => ContainsTerm(text, term));
        bool isMarkup = (sourcePath ?? string.Empty).EndsWith(".xaml", StringComparison.OrdinalIgnoreCase);
        bool hasCore = focus.Length == 0 ? evidenceMatches > 0 : focusMatches > 0;
        int requiredEvidence = evidence.Length == 0 ? 0 : Math.Min(2, evidence.Length);
        bool enoughStructure = evidenceMatches >= requiredEvidence;
        bool relevant = hasCore && enoughStructure && (!intent.RequiresXamlEvidence || isMarkup);
        int score = focusMatches * 180 + evidenceMatches * 70 + (isMarkup ? 120 : 0);
        string reason = $"focus {focusMatches}/{focus.Length}; structure {evidenceMatches}/{evidence.Length}; xaml={isMarkup}";
        return new KnowledgeEvidenceRelevanceScore(relevant, score, focusMatches, evidenceMatches, reason);
    }

    private static string NodeText(OntologyNode node) => string.Join(' ',
        node.CanonicalName,
        node.QualifiedName,
        node.SourcePath,
        node.ProjectName,
        node.Namespace,
        node.Signature,
        string.Join(' ', node.Types),
        string.Join(' ', node.Tags),
        string.Join(' ', node.Labels.Select(item => item.Text)),
        string.Join(' ', node.Summaries.Select(item => item.Text)));

    private static bool ContainsTerm(string value, string term) =>
        value.Contains(term, StringComparison.OrdinalIgnoreCase)
        || (term.Equals("Binding", StringComparison.OrdinalIgnoreCase)
            && value.Contains("{Binding", StringComparison.OrdinalIgnoreCase));
}

/// <summary>Provides a no-model query plan and remains the fallback for every external planner.</summary>
internal static partial class DeterministicKnowledgeSearchPlan
{
    internal static KnowledgeSearchPlan Create(string question)
    {
        KnowledgeQuestionIntent intent = KnowledgeQuestionIntentClassifier.Classify(question);
        KnowledgeUsageSearchProfile? usage = KnowledgeQuestionIntentClassifier.UsageProfile(question);
        string[] symbols = KnowledgeQuestionIntentClassifier.SpecificSymbols(question)
            .Concat(usage?.Symbols ?? [])
            .Concat(DerivePossessiveMemberSymbols(question))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(value => value.Contains('.'))
            .ThenByDescending(value => value.Length)
            .ToArray();
        string[] terms = usage is not null
            ? usage.Concepts.ToArray()
            : intent.IsGeneralConcept
                ? KnowledgeQuestionIntentClassifier.SearchTerms(intent)
            : KnowledgeEvidenceBundleBuilder.ExtractTerms(question);
        List<string> relations = [];
        relations.AddRange(usage?.Relations ?? []);
        if (ContainsAny(question, "전달", "forward", "forwardsTo")) relations.Add("forwardsTo");
        if (ContainsAny(question, "hasEventComponent", "이벤트 구성", "event component")) relations.Add("hasEventComponent");
        if (ContainsAny(question, "호출", "call")) relations.Add("calls");
        if (ContainsAny(question, "의존", "depend")) relations.Add("dependsOn");
        if (ContainsAny(question, "상속", "inherit")) relations.Add("inherits");
        if (ContainsAny(question, "구현", "implement") || InterfaceUsageRegex().IsMatch(question))
            relations.Add("implements");
        KnowledgeRelationConstraint[] relationConstraints = CreateRelationConstraints(question, symbols, relations);
        KnowledgeSearchPlan plan = new(
            usage is not null && symbols.Length > 0 ? "mixed" : intent.IsGeneralConcept ? "general-concept" : symbols.Length > 0 ? "specific-symbol" : "mixed",
            terms,
            symbols,
            relations.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            usage?.Project,
            intent.IsGeneralConcept && symbols.Length == 0);
        return plan with
        {
            Intent = usage?.Intent ?? (intent.IsGeneralConcept ? "general-concept" : "symbol-relation"),
            Concepts = usage?.Concepts ?? KnowledgeQuestionIntentClassifier.Concepts(question, intent),
            SourceKinds = usage?.SourceKinds ?? [],
            RelationConstraints = relationConstraints,
            Diagnostics = new KnowledgePlannerDiagnostics
            {
                Provider = "RuleFallback",
                FallbackReason = "deterministic-plan"
            }
        };
    }

    private static bool ContainsAny(string value, params string[] terms) =>
        terms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));

    private static KnowledgeRelationConstraint[] CreateRelationConstraints(
        string question,
        IReadOnlyList<string> symbols,
        IReadOnlyList<string> relations)
    {
        string? anchor = symbols.FirstOrDefault(symbol => symbol.Contains('.'))
            ?? symbols.FirstOrDefault();
        return relations.Select(relation => new KnowledgeRelationConstraint(
                relation,
                IsIncomingRequest(question, relation)
                    ? KnowledgeRelationDirection.Incoming
                    : KnowledgeRelationDirection.Outgoing,
                anchor))
            .ToArray();
    }

    private static bool IsIncomingRequest(string question, string relation) => relation.ToLowerInvariant() switch
    {
        "inherits" => IncomingInheritsRegex().IsMatch(question)
            || ContainsAny(question, "who inherits", "types inheriting", "derived from"),
        "calls" => ContainsAny(question, "누가 호출", "who calls"),
        "forwardsto" => ContainsAny(question, "어디에서 전달", "who forwards"),
        "implements" => IncomingImplementsRegex().IsMatch(question)
            || ContainsAny(question, "who implements", "which classes implement", "which types implement"),
        _ => false
    };

    private static IEnumerable<string> DerivePossessiveMemberSymbols(string question)
    {
        foreach (Match match in PossessiveMemberRegex().Matches(question ?? string.Empty))
            yield return $"{match.Groups[1].Value}.{match.Groups[2].Value}";
    }

    [GeneratedRegex(@"\b([A-Za-z_][A-Za-z0-9_]*(?:ViewModel|Event|Model|Service|Controller))\s*(?:의|\.|::)\s*([A-Z][A-Za-z0-9_]*)")]
    private static partial Regex PossessiveMemberRegex();

    [GeneratedRegex(@"(?:[A-Za-z_][A-Za-z0-9_.]*|[가-힣]+)(?:을|를)\s*상속(?:하|받)")]
    private static partial Regex IncomingInheritsRegex();

    [GeneratedRegex(@"(?i)(?:INotifyPropertyChanged|INotifyPropertyChanging).*(?:어떤\s*(?:클래스|타입)|누가).*(?:사용|구현)|(?:어떤\s*(?:클래스|타입)|누가).*(?:사용|구현).*(?:INotifyPropertyChanged|INotifyPropertyChanging)")]
    private static partial Regex InterfaceUsageRegex();

    [GeneratedRegex(@"(?i)(?:어떤\s*(?:클래스|타입)(?:에서|이)?\s*(?:사용|구현)|누가\s*(?:사용|구현)|who implements|which (?:classes|types) (?:implement|use))")]
    private static partial Regex IncomingImplementsRegex();
}
