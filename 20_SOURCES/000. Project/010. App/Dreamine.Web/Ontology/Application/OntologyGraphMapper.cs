using DreamineWeb.Ontology.Domain;

namespace DreamineWeb.Ontology.Application;

/// <summary>Maps ontology domain records into bounded Blazor view models.</summary>
public sealed class OntologyGraphMapper : IOntologyGraphMapper
{
    /// <inheritdoc />
    public OntologyNodeItemViewModel ToNodeItem(OntologyNode node, string language = "ko") =>
        new(
            node.StableUri,
            Localized(node.Labels, language, node.CanonicalName),
            node.QualifiedName,
            node.EffectiveType,
            node.RawType,
            node.SourceVerificationStatus,
            node.ProjectName,
            node.SourcePath,
            node.LineStart,
            Localized(node.Summaries, language, node.CanonicalName),
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
        OntologyRelationMeaning meaning) =>
        new(
            relation.StableUri,
            isOutgoing,
            isOutgoing ? relation.TargetUri : relation.SourceUri,
            relatedNode?.CanonicalName ?? "(missing node)",
            meaning.OriginalType,
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
            Localized(node.Summaries, language, node.CanonicalName),
            node.Tags,
            node.Evidence.Select(item => new OntologyEvidenceViewModel(item.Source, item.Value, item.Confidence)).ToArray(),
            incoming,
            outgoing);

    /// <inheritdoc />
    public OntologyTBoxClassViewModel ToTBoxClass(OntologyTBoxClass item) =>
        new(item.Name, item.Description, item.RequiredProperties, item.PropertyCount);

    private static string Localized(IReadOnlyList<OntologyLocalizedText> values, string language, string fallback) =>
        values.FirstOrDefault(item => item.Language.Equals(language, StringComparison.OrdinalIgnoreCase))?.Text
        ?? values.FirstOrDefault(item => item.Language.Equals("en", StringComparison.OrdinalIgnoreCase))?.Text
        ?? values.FirstOrDefault()?.Text
        ?? fallback;

}
