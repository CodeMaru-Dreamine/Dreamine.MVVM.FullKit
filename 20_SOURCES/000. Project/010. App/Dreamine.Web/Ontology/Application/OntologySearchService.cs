using DreamineWeb.Ontology.Domain;

namespace DreamineWeb.Ontology.Application;

/// <summary>Implements paged ABox, TBox, relation, and Dreamine Event navigation use cases.</summary>
public sealed class OntologySearchService : IOntologySearchService
{
    private const string SampleSmartMainViewModelPath = "SampleSmart/Pages/MainWindow.xaml.ViewModel.cs";
    private const string SampleSmartMainEventPath = "SampleSmart/Pages/MainWindow.xaml.Event.cs";

    private readonly IOntologyRepository _repository;
    private readonly IOntologyRelationResolver _relationResolver;
    private readonly IOntologyGraphMapper _mapper;

    public OntologySearchService(
        IOntologyRepository repository,
        IOntologyRelationResolver relationResolver,
        IOntologyGraphMapper mapper)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _relationResolver = relationResolver ?? throw new ArgumentNullException(nameof(relationResolver));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    /// <inheritdoc />
    public async Task<OntologySearchResultViewModel> SearchAsync(
        OntologySearchRequest request,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        OntologyPage<OntologyNode> result = await _repository.SearchNodesAsync(
            new OntologyQuery(
                request.SearchText,
                request.Type,
                request.Project,
                request.FilePath,
                request.RelationType,
                request.IncludeExcluded),
            page,
            pageSize,
            cancellationToken).ConfigureAwait(false);
        OntologyNodeItemViewModel[] items = result.Items
            .Select(node => _mapper.ToNodeItem(node, request.Language)).ToArray();
        return new OntologySearchResultViewModel(items, result.Page, result.PageSize, result.TotalCount, result.TotalPages);
    }

    /// <inheritdoc />
    public async Task<OntologyNodeDetailsViewModel?> GetNodeAsync(string stableUri, CancellationToken cancellationToken)
    {
        OntologyNode? node = await _repository.GetNodeAsync(stableUri, cancellationToken).ConfigureAwait(false);
        if (node is null)
            return null;

        IReadOnlyList<OntologyRelation> relations = await _repository.GetRelationsAsync(stableUri, cancellationToken).ConfigureAwait(false);
        IReadOnlyDictionary<string, OntologyNode> related = await _repository.GetNodesAsync(
            relations.SelectMany(relation => new[] { relation.SourceUri, relation.TargetUri }), cancellationToken).ConfigureAwait(false);
        List<OntologyRelationViewModel> incoming = [];
        List<OntologyRelationViewModel> outgoing = [];
        foreach (OntologyRelation relation in relations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            bool isOutgoing = relation.SourceUri.Equals(stableUri, StringComparison.Ordinal);
            string relatedUri = isOutgoing ? relation.TargetUri : relation.SourceUri;
            OntologyRelationMeaning meaning = _relationResolver.Resolve(relation.OriginalType);
            OntologyRelationViewModel mapped = _mapper.ToRelation(
                relation,
                isOutgoing,
                related.GetValueOrDefault(relatedUri),
                meaning);
            (isOutgoing ? outgoing : incoming).Add(mapped);
        }

        return _mapper.ToNodeDetails(
            node,
            incoming.OrderBy(item => item.OriginalType).ThenBy(item => item.RelatedName).ToArray(),
            outgoing.OrderBy(item => item.OriginalType).ThenBy(item => item.RelatedName).ToArray());
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OntologyTBoxClassViewModel>> GetTBoxClassesAsync(CancellationToken cancellationToken) =>
        (await _repository.GetTBoxClassesAsync(cancellationToken).ConfigureAwait(false))
            .Select(_mapper.ToTBoxClass).ToArray();

    /// <inheritdoc />
    public async Task<OntologyFacetsViewModel> GetFacetsAsync(CancellationToken cancellationToken)
    {
        OntologyFacets facets = await _repository.GetFacetsAsync(cancellationToken).ConfigureAwait(false);
        return new OntologyFacetsViewModel(
            facets.Types,
            facets.Projects,
            facets.RelationTypes,
            facets.VisibleIndividualCount,
            facets.ExcludedIndividualCount);
    }

    /// <inheritdoc />
    public async Task<OntologyEventFlowViewModel?> GetDreamineEventSampleAsync(CancellationToken cancellationToken)
    {
        OntologyNode? viewModel = await FindNodeAsync("MainWindowViewModel", "ViewModel", SampleSmartMainViewModelPath, cancellationToken)
            .ConfigureAwait(false);
        OntologyNode? component = await FindNodeAsync("MainWindowEvent", "DreamineEventComponent", SampleSmartMainEventPath, cancellationToken)
            .ConfigureAwait(false);
        OntologyNode? command = await FindNodeAsync("Ok", "Method", SampleSmartMainViewModelPath, cancellationToken)
            .ConfigureAwait(false);
        OntologyNode? target = await FindNodeAsync("Ok", "Method", SampleSmartMainEventPath, cancellationToken)
            .ConfigureAwait(false);
        if (viewModel is null || component is null || command is null || target is null)
            return null;

        OntologyRelation? componentRelation = (await _repository.GetRelationsAsync(viewModel.StableUri, cancellationToken).ConfigureAwait(false))
            .FirstOrDefault(relation => relation.SourceUri == viewModel.StableUri
                && relation.TargetUri == component.StableUri
                && relation.OriginalType.Equals("hasEventComponent", StringComparison.OrdinalIgnoreCase));
        OntologyRelation? forwardingRelation = (await _repository.GetRelationsAsync(command.StableUri, cancellationToken).ConfigureAwait(false))
            .FirstOrDefault(relation => relation.SourceUri == command.StableUri
                && relation.TargetUri == target.StableUri
                && relation.OriginalType.Equals("forwardsTo", StringComparison.OrdinalIgnoreCase));
        if (componentRelation is null || forwardingRelation is null)
            return null;

        return new OntologyEventFlowViewModel(
            _mapper.ToNodeItem(viewModel),
            _mapper.ToNodeItem(component),
            _mapper.ToNodeItem(command),
            _mapper.ToNodeItem(target),
            _mapper.ToRelation(componentRelation, true, component, _relationResolver.Resolve(componentRelation.OriginalType)),
            _mapper.ToRelation(forwardingRelation, true, target, _relationResolver.Resolve(forwardingRelation.OriginalType)));
    }

    private async Task<OntologyNode?> FindNodeAsync(
        string name,
        string type,
        string sourcePathFragment,
        CancellationToken cancellationToken)
    {
        OntologyPage<OntologyNode> page = await _repository.SearchNodesAsync(
            new OntologyQuery(name, type, FilePath: sourcePathFragment, IncludeExcluded: true),
            1,
            100,
            cancellationToken).ConfigureAwait(false);
        return page.Items.FirstOrDefault(node => node.CanonicalName.Equals(name, StringComparison.Ordinal)
            && node.SourcePath.Contains(sourcePathFragment, StringComparison.OrdinalIgnoreCase));
    }
}
