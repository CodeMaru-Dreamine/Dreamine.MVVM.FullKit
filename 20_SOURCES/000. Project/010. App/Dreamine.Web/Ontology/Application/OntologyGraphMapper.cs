using DreamineWeb.Ontology.Domain;

namespace DreamineWeb.Ontology.Application;

/// <summary>Maps ontology domain records into bounded Blazor view models.</summary>
public sealed class OntologyGraphMapper : IOntologyGraphMapper
{
    /// <inheritdoc />
    public OntologyNodeItemViewModel ToNodeItem(OntologyNode node, string language = "ko") =>
        new(
            node.StableUri,
            Localized(node.Labels, language, node.CanonicalName, node.CanonicalName),
            node.QualifiedName,
            node.EffectiveType,
            node.RawType,
            node.SourceVerificationStatus,
            node.ProjectName,
            node.SourcePath,
            node.LineStart,
            Localized(node.Summaries, language, node.CanonicalName, EnglishSummaryFallback(node)),
            !string.IsNullOrWhiteSpace(node.RawType)
                && !node.RawType.Equals(node.EffectiveType, StringComparison.OrdinalIgnoreCase),
            node.IsStale,
            node.IsExcluded,
            node.EffectiveType.Equals("DreamineEventComponent", StringComparison.OrdinalIgnoreCase));

    /// <inheritdoc />
    public OntologyRelationViewModel ToRelation(
        OntologyRelation relation,
        bool isOutgoing,
        OntologyNode? relatedNode,
        OntologyRelationMeaning meaning,
        string language = "ko") =>
        new(
            relation.StableUri,
            isOutgoing,
            isOutgoing ? relation.TargetUri : relation.SourceUri,
            relatedNode is null
                ? (IsKorean(language) ? "(누락된 노드)" : "(missing node)")
                : Localized(relatedNode.Labels, language, relatedNode.CanonicalName, relatedNode.CanonicalName),
            meaning.OriginalType,
            RelationLabel(meaning.OriginalType, language),
            meaning.ProjectionType,
            meaning.WasProjected,
            relation.Evidence.Select(item => new OntologyEvidenceViewModel(item.Source, item.Value, item.Confidence)).ToArray());

    /// <inheritdoc />
    public OntologyNodeDetailsViewModel ToNodeDetails(
        OntologyNode node,
        IReadOnlyList<OntologyRelationViewModel> incoming,
        IReadOnlyList<OntologyRelationViewModel> outgoing,
        string language = "ko") =>
        new(
            ToNodeItem(node, language),
            node.SourceGraphId,
            node.Types,
            node.Namespace,
            node.Signature,
            node.RawSourcePath,
            node.LineEnd,
            Localized(node.Summaries, language, node.CanonicalName, EnglishSummaryFallback(node)),
            node.Tags,
            node.Evidence.Select(item => new OntologyEvidenceViewModel(item.Source, item.Value, item.Confidence)).ToArray(),
            incoming,
            outgoing);

    /// <inheritdoc />
    public OntologyTBoxClassViewModel ToTBoxClass(OntologyTBoxClass item, string language = "ko") =>
        new(
            item.Name,
            IsKorean(language) || IsUsableEnglish(item.Description)
                ? item.Description
                : $"{item.Name} is a LinkML ontology class.",
            item.RequiredProperties,
            item.PropertyCount);

    // English pages never consume a Korean sentence that was incorrectly tagged as "en".
    // English fallback: valid en text -> generated neutral English -> canonical name.
    // Korean fallback: ko text -> valid en text -> first available text -> canonical name.
    private static string Localized(
        IReadOnlyList<OntologyLocalizedText> values,
        string language,
        string koreanFallback,
        string englishFallback)
    {
        if (!IsKorean(language))
        {
            string? english = values.FirstOrDefault(item =>
                item.Language.Equals("en", StringComparison.OrdinalIgnoreCase)
                && IsUsableEnglish(item.Text))?.Text;
            return !string.IsNullOrWhiteSpace(english) ? english : englishFallback;
        }

        return values.FirstOrDefault(item => item.Language.Equals("ko", StringComparison.OrdinalIgnoreCase))?.Text
            ?? values.FirstOrDefault(item => item.Language.Equals("en", StringComparison.OrdinalIgnoreCase)
                && IsUsableEnglish(item.Text))?.Text
            ?? values.FirstOrDefault()?.Text
            ?? koreanFallback;
    }

    private static bool IsKorean(string language) =>
        language.StartsWith("ko", StringComparison.OrdinalIgnoreCase);

    private static bool IsUsableEnglish(string? value) =>
        !string.IsNullOrWhiteSpace(value) && !value.Any(character => character is >= '\uac00' and <= '\ud7a3');

    private static string EnglishSummaryFallback(OntologyNode node)
    {
        string project = string.IsNullOrWhiteSpace(node.ProjectName) ? "the Dreamine solution" : node.ProjectName;
        string kind = node.EffectiveType switch
        {
            "SourceFile" or "EventComponentFile" => "source file",
            "ViewModel" => "ViewModel",
            "View" => "view",
            "Service" => "service",
            "DreamineEventComponent" => "Dreamine Event component",
            "CodeInterface" => "C# interface",
            "CodeRecord" => "C# record",
            "CodeStruct" => "C# struct",
            "CodeEnum" => "C# enum",
            "CodeAttribute" => "C# attribute",
            "CodeClass" => "C# class",
            "Method" => "method",
            "Constructor" => "constructor",
            _ => "ontology element"
        };
        return $"{node.CanonicalName} is a source-verified {kind} in {project}.";
    }

    private static string RelationLabel(string relationType, string language)
    {
        if (!IsKorean(language))
            return relationType switch
            {
                "hasEventComponent" => "Has event component",
                "forwardsTo" => "Forwards to",
                "declaredIn" => "Declared in",
                "companionOf" => "Companion of",
                "usesModel" => "Uses model",
                "dependsOn" or "depends_on" => "Depends on",
                "invokesNavigation" => "Invokes navigation",
                "controlsView" => "Controls view",
                "implements" => "Implements",
                "inherits" => "Inherits",
                "calls" => "Calls",
                "contains" => "Contains",
                _ => relationType
            };

        return relationType switch
        {
            "hasEventComponent" => "이벤트 구성 요소 연결",
            "forwardsTo" => "동작 전달",
            "declaredIn" => "선언 파일",
            "companionOf" => "컴패니언 연결",
            "usesModel" => "모델 사용",
            "dependsOn" or "depends_on" => "의존",
            "invokesNavigation" => "화면 이동 호출",
            "controlsView" => "View 제어",
            "implements" => "구현",
            "inherits" => "상속",
            "calls" => "호출",
            "contains" => "포함",
            _ => relationType
        };
    }

}
