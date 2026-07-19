namespace DreamineWeb.Ontology.Application;

/// <summary>Maps Dreamine semantic relations to and from Understand-compatible projections.</summary>
public sealed class OntologyRelationResolver : IOntologyRelationResolver
{
    private static readonly IReadOnlyDictionary<string, string> ProjectionByOriginal =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["forwardsTo"] = "calls",
            ["hasEventComponent"] = "depends_on"
        };

    /// <inheritdoc />
    public OntologyRelationMeaning Resolve(string? originalType, string? projectionType = null)
    {
        string original = string.IsNullOrWhiteSpace(originalType)
            ? projectionType?.Trim() ?? "related"
            : originalType.Trim();
        string projected = string.IsNullOrWhiteSpace(projectionType)
            ? ProjectionByOriginal.GetValueOrDefault(original, original)
            : projectionType.Trim();
        bool wasProjected = !original.Equals(projected, StringComparison.OrdinalIgnoreCase);
        return new OntologyRelationMeaning(original, projected, wasProjected, original);
    }
}
