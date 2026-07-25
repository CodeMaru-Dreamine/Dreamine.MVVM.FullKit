using DreamineWeb.Ontology.Application;
using DreamineWeb.Ontology.Domain;

namespace DreamineWeb.KnowledgeQa.Application;

/// <summary>Uses exact indexed ontology symbols to recover scope without invoking an LLM.</summary>
public sealed class OntologySymbolScopeResolver(IOntologyRepository repository) : IKnowledgeSymbolScopeResolver
{
    private readonly IOntologyRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<KnowledgeSymbolScopeResolution> ResolveAsync(
        string question,
        CancellationToken cancellationToken)
    {
        string[] symbols = KnowledgeQuestionIntentClassifier.SpecificSymbols(question).Take(6).ToArray();
        if (symbols.Length == 0)
            return new KnowledgeSymbolScopeResolution(KnowledgeSymbolScopeResolutionKind.None, string.Empty, 0, 0);

        bool asksForForwarding = ContainsAny(question, "전달", "forward");
        foreach (string symbol in symbols)
        {
            cancellationToken.ThrowIfCancellationRequested();
            OntologyPage<OntologyNode> page = await _repository.SearchNodesAsync(
                new OntologyQuery(symbol), 1, 200, cancellationToken).ConfigureAwait(false);
            OntologyNode[] exactNodes = page.Items
                .Where(node => !node.IsExcluded && !node.IsStale && node.DefaultSearchVisible)
                .Where(node => IsExactMatch(node, symbol))
                .DistinctBy(node => node.StableUri, StringComparer.Ordinal)
                .ToArray();
            if (exactNodes.Length == 0)
                continue;

            List<OntologyRelation> forwardings = [];
            if (asksForForwarding)
            {
                foreach (OntologyNode node in exactNodes)
                {
                    IReadOnlyList<OntologyRelation> relations = await _repository.GetRelationsAsync(
                        node.StableUri, cancellationToken).ConfigureAwait(false);
                    forwardings.AddRange(relations.Where(relation =>
                        relation.OriginalType.Equals("forwardsTo", StringComparison.OrdinalIgnoreCase)));
                }
            }
            int forwardingCount = forwardings
                .DistinctBy(relation => relation.StableUri, StringComparer.Ordinal)
                .Count();
            if (asksForForwarding && forwardingCount == 1)
            {
                return new KnowledgeSymbolScopeResolution(
                    KnowledgeSymbolScopeResolutionKind.Exact, symbol, exactNodes.Length, forwardingCount);
            }
            if (asksForForwarding && forwardingCount > 1)
            {
                return new KnowledgeSymbolScopeResolution(
                    KnowledgeSymbolScopeResolutionKind.Ambiguous, symbol, exactNodes.Length, forwardingCount);
            }
            return new KnowledgeSymbolScopeResolution(
                exactNodes.Length == 1
                    ? KnowledgeSymbolScopeResolutionKind.Exact
                    : KnowledgeSymbolScopeResolutionKind.Ambiguous,
                symbol,
                exactNodes.Length,
                forwardingCount);
        }

        return new KnowledgeSymbolScopeResolution(KnowledgeSymbolScopeResolutionKind.None, string.Empty, 0, 0);
    }

    internal static bool IsExactMatch(OntologyNode node, string symbol)
    {
        if (!symbol.Contains('.'))
            return node.CanonicalName.Equals(symbol, StringComparison.OrdinalIgnoreCase);
        return node.QualifiedName.Equals(symbol, StringComparison.OrdinalIgnoreCase)
            || node.QualifiedName.EndsWith('.' + symbol, StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsAny(string value, params string[] candidates) =>
        candidates.Any(candidate => value.Contains(candidate, StringComparison.OrdinalIgnoreCase));
}
