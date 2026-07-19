using DreamineWeb.Ontology.Application;
using DreamineWeb.Ontology.Domain;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DreamineWeb.Ontology.Infrastructure;

/// <summary>
/// Loads generated ontology JSON once, builds bounded server-side indices, and reloads when artifacts change.
/// </summary>
public sealed class JsonOntologyRepository : IOntologyRepository
{
    private static readonly string[] WatchedFiles =
    [
        "manifest.json",
        "instances.json",
        "dreamine.schema.json",
        "architecture-validation.json",
        "linkml-shacl-validation.json",
        "source-audit.json"
    ];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IOntologyDataPathResolver _pathResolver;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _reloadGate = new(1, 1);
    private CacheState? _cache;
    private long _cacheHits;
    private long _reloadCount;

    public JsonOntologyRepository(IOntologyDataPathResolver pathResolver, TimeProvider? timeProvider = null)
    {
        _pathResolver = pathResolver ?? throw new ArgumentNullException(nameof(pathResolver));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async Task<OntologyPage<OntologyNode>> SearchNodesAsync(
        OntologyQuery query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        CacheState cache = await GetCacheAsync(cancellationToken).ConfigureAwait(false);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        string search = query.SearchText.Trim();
        string file = NormalizePath(query.FilePath);
        cache.RelationNodes.TryGetValue(query.RelationType, out HashSet<string>? relationNodes);

        List<OntologyNode> filtered = new();
        int inspected = 0;
        foreach (OntologyNode node in cache.Nodes)
        {
            if ((++inspected & 127) == 0)
                cancellationToken.ThrowIfCancellationRequested();
            if (!query.IncludeExcluded && !node.DefaultSearchVisible)
                continue;
            if (!string.IsNullOrWhiteSpace(query.Type)
                && !node.EffectiveType.Equals(query.Type, StringComparison.OrdinalIgnoreCase))
                continue;
            if (!string.IsNullOrWhiteSpace(query.Project)
                && !node.ProjectName.Equals(query.Project, StringComparison.OrdinalIgnoreCase)
                && !node.SourcePath.Contains(query.Project, StringComparison.OrdinalIgnoreCase))
                continue;
            if (!string.IsNullOrWhiteSpace(file)
                && !NormalizePath(node.SourcePath).Contains(file, StringComparison.OrdinalIgnoreCase))
                continue;
            if (!string.IsNullOrWhiteSpace(query.RelationType)
                && (relationNodes is null || !relationNodes.Contains(node.StableUri)))
                continue;
            if (!string.IsNullOrWhiteSpace(search) && !Matches(node, search))
                continue;
            filtered.Add(node);
        }

        OntologyNode[] items = filtered
            .OrderBy(node => node.CanonicalName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(node => node.SourcePath, StringComparer.OrdinalIgnoreCase)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArray();
        return new OntologyPage<OntologyNode>(items, page, pageSize, filtered.Count);
    }

    /// <inheritdoc />
    public async Task<OntologyNode?> GetNodeAsync(string stableUri, CancellationToken cancellationToken)
    {
        CacheState cache = await GetCacheAsync(cancellationToken).ConfigureAwait(false);
        return cache.NodesByUri.GetValueOrDefault(stableUri);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, OntologyNode>> GetNodesAsync(
        IEnumerable<string> stableUris,
        CancellationToken cancellationToken)
    {
        CacheState cache = await GetCacheAsync(cancellationToken).ConfigureAwait(false);
        Dictionary<string, OntologyNode> result = new(StringComparer.Ordinal);
        foreach (string stableUri in stableUris.Distinct(StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (cache.NodesByUri.TryGetValue(stableUri, out OntologyNode? node))
                result[stableUri] = node;
        }
        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OntologyRelation>> GetRelationsAsync(string stableUri, CancellationToken cancellationToken)
    {
        CacheState cache = await GetCacheAsync(cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return [
            .. cache.Outgoing.GetValueOrDefault(stableUri, []),
            .. cache.Incoming.GetValueOrDefault(stableUri, [])
        ];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OntologyTBoxClass>> GetTBoxClassesAsync(CancellationToken cancellationToken) =>
        (await GetCacheAsync(cancellationToken).ConfigureAwait(false)).TBoxClasses;

    /// <inheritdoc />
    public async Task<OntologyFacets> GetFacetsAsync(CancellationToken cancellationToken)
    {
        CacheState cache = await GetCacheAsync(cancellationToken).ConfigureAwait(false);
        return cache.Facets;
    }

    /// <inheritdoc />
    public async Task<OntologyValidationData> GetValidationDataAsync(CancellationToken cancellationToken) =>
        (await GetCacheAsync(cancellationToken).ConfigureAwait(false)).Validation;

    /// <inheritdoc />
    public async Task<OntologyLoadMetrics> GetLoadMetricsAsync(CancellationToken cancellationToken)
    {
        CacheState cache = await GetCacheAsync(cancellationToken).ConfigureAwait(false);
        return cache.Metrics with
        {
            CacheHits = Interlocked.Read(ref _cacheHits),
            ReloadCount = Interlocked.Read(ref _reloadCount)
        };
    }

    private async Task<CacheState> GetCacheAsync(CancellationToken cancellationToken)
    {
        string directory = _pathResolver.ResolveOntologyDirectory();
        IReadOnlyDictionary<string, FileStamp> stamps = CaptureStamps(directory);
        CacheState? current = Volatile.Read(ref _cache);
        if (current is not null && current.Directory.Equals(directory, StringComparison.OrdinalIgnoreCase)
            && StampsEqual(current.Stamps, stamps))
        {
            Interlocked.Increment(ref _cacheHits);
            return current;
        }

        await _reloadGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            current = _cache;
            stamps = CaptureStamps(directory);
            if (current is not null && current.Directory.Equals(directory, StringComparison.OrdinalIgnoreCase)
                && StampsEqual(current.Stamps, stamps))
            {
                Interlocked.Increment(ref _cacheHits);
                return current;
            }

            CacheState loaded = await LoadAsync(directory, stamps, cancellationToken).ConfigureAwait(false);
            Volatile.Write(ref _cache, loaded);
            Interlocked.Increment(ref _reloadCount);
            return loaded;
        }
        finally
        {
            _reloadGate.Release();
        }
    }

    private async Task<CacheState> LoadAsync(
        string directory,
        IReadOnlyDictionary<string, FileStamp> stamps,
        CancellationToken cancellationToken)
    {
        string instancesPath = RequiredPath(directory, "instances.json");
        string manifestPath = RequiredPath(directory, "manifest.json");
        string schemaPath = RequiredPath(directory, "dreamine.schema.json");
        long managedBefore = GC.GetTotalMemory(false);
        long workingBefore = Process.GetCurrentProcess().WorkingSet64;
        Stopwatch timer = Stopwatch.StartNew();

        OntologyInstancesDocument instances;
        await using (FileStream stream = OpenAsync(instancesPath))
        {
            instances = await JsonSerializer.DeserializeAsync<OntologyInstancesDocument>(stream, JsonOptions, cancellationToken)
                .ConfigureAwait(false) ?? throw new InvalidDataException("instances.json is empty.");
        }

        OntologyManifestDocument manifest;
        await using (FileStream stream = OpenAsync(manifestPath))
        {
            manifest = await JsonSerializer.DeserializeAsync<OntologyManifestDocument>(stream, JsonOptions, cancellationToken)
                .ConfigureAwait(false) ?? throw new InvalidDataException("manifest.json is empty.");
        }

        OntologyNode[] nodes = instances.Elements.Select(MapNode).ToArray();
        Dictionary<string, OntologyNode> nodesByUri = nodes
            .GroupBy(node => node.StableUri, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        OntologyRelation[] relations = instances.Relations.Select(MapRelation).ToArray();
        Dictionary<string, List<OntologyRelation>> outgoing = new(StringComparer.Ordinal);
        Dictionary<string, List<OntologyRelation>> incoming = new(StringComparer.Ordinal);
        Dictionary<string, HashSet<string>> relationNodes = new(StringComparer.OrdinalIgnoreCase);
        foreach (OntologyRelation relation in relations)
        {
            Add(outgoing, relation.SourceUri, relation);
            Add(incoming, relation.TargetUri, relation);
            if (!relationNodes.TryGetValue(relation.OriginalType, out HashSet<string>? members))
                relationNodes[relation.OriginalType] = members = new HashSet<string>(StringComparer.Ordinal);
            members.Add(relation.SourceUri);
            members.Add(relation.TargetUri);
        }

        OntologyTBoxClass[] tbox = await ReadTBoxAsync(schemaPath, cancellationToken).ConfigureAwait(false);
        string contentHash = await ComputeHashAsync(instancesPath, cancellationToken).ConfigureAwait(false);
        OntologyValidationData validation = await ReadValidationAsync(directory, manifest, contentHash, cancellationToken)
            .ConfigureAwait(false);
        OntologyFacets facets = new(
            nodes.Where(node => node.DefaultSearchVisible).Select(node => node.EffectiveType)
                .Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToArray(),
            nodes.Where(node => node.DefaultSearchVisible && !string.IsNullOrWhiteSpace(node.ProjectName))
                .Select(node => node.ProjectName).Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase).ToArray(),
            relations.Select(relation => relation.OriginalType).Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase).ToArray(),
            nodes.Count(node => node.DefaultSearchVisible),
            nodes.Count(node => !node.DefaultSearchVisible));

        timer.Stop();
        long sourceBytes = WatchedFiles.Sum(file => new FileInfo(Path.Combine(directory, file)) is { Exists: true } info ? info.Length : 0);
        OntologyLoadMetrics metrics = new(
            _timeProvider.GetUtcNow(),
            sourceBytes,
            nodes.Length,
            relations.Length,
            timer.Elapsed.TotalMilliseconds,
            Math.Max(0, GC.GetTotalMemory(false) - managedBefore) / 1024d / 1024d,
            Math.Max(0, Process.GetCurrentProcess().WorkingSet64 - workingBefore) / 1024d / 1024d,
            0,
            0);

        return new CacheState(directory, stamps, nodes, nodesByUri, outgoing, incoming, relationNodes, tbox, facets, validation, metrics);
    }

    private async Task<OntologyValidationData> ReadValidationAsync(
        string directory,
        OntologyManifestDocument manifest,
        string contentHash,
        CancellationToken cancellationToken)
    {
        List<OntologyArtifactState> artifacts = [];
        JsonDocument? audit = await TryReadReportAsync(directory, "source-audit.json", artifacts, cancellationToken).ConfigureAwait(false);
        JsonDocument? architecture = await TryReadReportAsync(directory, "architecture-validation.json", artifacts, cancellationToken).ConfigureAwait(false);
        JsonDocument? linkml = await TryReadReportAsync(directory, "linkml-shacl-validation.json", artifacts, cancellationToken).ConfigureAwait(false);
        artifacts.Insert(0, BuildArtifactState(directory, "manifest.json", true, manifest.GeneratedAt, null));

        try
        {
            JsonElement auditRoot = audit?.RootElement ?? default;
            JsonElement architectureRoot = architecture?.RootElement ?? default;
            JsonElement linkmlRoot = linkml?.RootElement ?? default;
            OntologyShapeValidation[] shapes = GetArray(architectureRoot, "shapes")
                .Select(shape => new OntologyShapeValidation(
                    GetString(shape, "shape"),
                    GetInt(shape, "targetNodeCount"),
                    GetInt(shape, "actualViolationCount"),
                    GetBool(GetObject(shape, "positiveFixture"), "conforms"),
                    !GetBool(GetObject(shape, "negativeFixture"), "conforms"),
                    GetBool(shape, "fixtureTestPassed")))
                .ToArray();
            int declaredPartial = GetInt(auditRoot, "partialMethods", "declarationOnlyPartialMethodCount");
            int missingOverlayPartial = GetInt(auditRoot, "partialMethods", "missingFromOverlayCount");

            OntologyValidationData data = new()
            {
                OntologyVersion = manifest.OntologyVersion,
                GraphVersion = manifest.SourceGraphVersion,
                ContentHash = contentHash,
                GeneratedAt = manifest.GeneratedAt,
                ElementCount = manifest.Counts.Elements,
                RelationCount = manifest.Counts.Relations,
                RdfTripleCount = GetInt(linkmlRoot, "dataTriples"),
                SourceVsOverlayMismatches = GetInt(auditRoot, "typeAudit", "sourceVsOverlayMismatchCount"),
                SourceDeclarationsMissingRawGraph = GetInt(auditRoot, "typeAudit", "sourceDeclarationsMissingGraphNodeCount"),
                SourceDeclarationsMissingOverlay = GetInt(auditRoot, "typeAudit", "sourceDeclarationsMissingOverlayNodeCount"),
                PartialMethodsEnriched = Math.Max(0, declaredPartial - missingOverlayPartial),
                UnclassifiedPartialMethods = GetInt(auditRoot, "partialMethods", "unclassifiedCount"),
                StalePathRemaps = GetInt(auditRoot, "typeAudit", "stalePathRemapCount"),
                RawDanglingRelations = GetInt(auditRoot, "relations", "rawDanglingRelationCount"),
                OverlayDanglingRelations = GetInt(auditRoot, "relations", "overlayDanglingRelationCount"),
                DuplicateStableUris = GetInt(auditRoot, "identity", "duplicateStableUriGroupCount"),
                StableUriTypeConflicts = GetInt(auditRoot, "identity", "stableUriTypeConflictCount"),
                AutoCorrectedTypes = GetInt(auditRoot, "typeAudit", "autoCorrectedNodeCount"),
                UncorrectableTypes = GetInt(auditRoot, "typeAudit", "uncorrectableNodeCount"),
                ExcludedGeneratedFiles = GetInt(auditRoot, "generatedSourcePolicy", "excludedGeneratedFileCount"),
                ExcludedGraphNodes = GetInt(auditRoot, "generatedSourcePolicy", "excludedGraphNodeCount"),
                LinkmlShaclConforms = GetBool(linkmlRoot, "conforms"),
                LinkmlShaclViolations = GetInt(linkmlRoot, "violationCount"),
                ArchitectureShaclConforms = GetBool(architectureRoot, "fullArchitectureGraphConforms"),
                Shapes = shapes,
                Artifacts = artifacts
            };
            bool reportsReady = artifacts.All(item => item.Exists && item.IsValid && !item.IsStale);
            bool countsClean = data.SourceVsOverlayMismatches == 0
                && data.SourceDeclarationsMissingOverlay == 0
                && data.UnclassifiedPartialMethods == 0
                && data.RawDanglingRelations == 0
                && data.OverlayDanglingRelations == 0
                && data.DuplicateStableUris == 0
                && data.StableUriTypeConflicts == 0
                && data.UncorrectableTypes == 0
                && data.LinkmlShaclViolations == 0;
            bool fixturesClean = shapes.All(item => item.FixtureTestPassed
                && item.PositiveFixtureConforms && item.NegativeFixtureRejected && item.ViolationCount == 0);
            bool healthy = reportsReady && countsClean && fixturesClean
                && data.LinkmlShaclConforms && data.ArchitectureShaclConforms;
            string reason = healthy
                ? "SourceAudit, LinkML SHACL, architecture SHACL, and fixtures are current and conformant."
                : BuildHealthReason(artifacts, data, fixturesClean);
            return data with { IsHealthy = healthy, HealthReason = reason };
        }
        finally
        {
            audit?.Dispose();
            architecture?.Dispose();
            linkml?.Dispose();
        }
    }

    private async Task<JsonDocument?> TryReadReportAsync(
        string directory,
        string fileName,
        List<OntologyArtifactState> artifacts,
        CancellationToken cancellationToken)
    {
        string path = Path.Combine(directory, fileName);
        if (!File.Exists(path))
        {
            artifacts.Add(BuildArtifactState(directory, fileName, false, null, "File is missing."));
            return null;
        }

        try
        {
            await using FileStream stream = OpenAsync(path);
            JsonDocument document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
            DateTimeOffset? generatedAt = GetDate(document.RootElement, "generatedAt")
                ?? new DateTimeOffset(File.GetLastWriteTimeUtc(path), TimeSpan.Zero);
            artifacts.Add(BuildArtifactState(directory, fileName, true, generatedAt, null));
            return document;
        }
        catch (Exception exception) when (exception is JsonException or IOException)
        {
            artifacts.Add(BuildArtifactState(directory, fileName, true, null, exception.Message));
            return null;
        }
    }

    private OntologyArtifactState BuildArtifactState(
        string directory,
        string fileName,
        bool exists,
        DateTimeOffset? generatedAt,
        string? error)
    {
        bool valid = exists && string.IsNullOrWhiteSpace(error);
        bool stale = !generatedAt.HasValue || _timeProvider.GetUtcNow() - generatedAt.Value > TimeSpan.FromDays(7);
        return new OntologyArtifactState(fileName, Path.Combine(directory, fileName), exists, valid, stale, generatedAt, error);
    }

    private static async Task<OntologyTBoxClass[]> ReadTBoxAsync(string path, CancellationToken cancellationToken)
    {
        await using FileStream stream = OpenAsync(path);
        using JsonDocument document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (!document.RootElement.TryGetProperty("$defs", out JsonElement definitions))
            return [];

        List<OntologyTBoxClass> result = [];
        foreach (JsonProperty definition in definitions.EnumerateObject())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!definition.Value.TryGetProperty("type", out JsonElement type)
                || type.ValueKind != JsonValueKind.String || type.GetString() != "object")
                continue;
            string description = GetString(definition.Value, "description");
            string[] required = GetArray(definition.Value, "required")
                .Where(item => item.ValueKind == JsonValueKind.String)
                .Select(item => item.GetString() ?? string.Empty).Where(item => item.Length > 0).ToArray();
            int propertyCount = definition.Value.TryGetProperty("properties", out JsonElement properties)
                && properties.ValueKind == JsonValueKind.Object ? properties.EnumerateObject().Count() : 0;
            result.Add(new OntologyTBoxClass(definition.Name, description, required, propertyCount));
        }
        return result.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static OntologyNode MapNode(OntologyElementDocument item)
    {
        string effectiveType = string.IsNullOrWhiteSpace(item.ElementType) ? item.RawElementType ?? "SoftwareElement" : item.ElementType;
        string sourcePath = NormalizePath(item.SourcePath);
        string qualified = !string.IsNullOrWhiteSpace(item.Namespace)
            ? $"{item.Namespace}.{item.CanonicalName}"
            : item.CanonicalName;
        bool excluded = item.DefaultSearchVisible == false;
        bool stale = item.SourceVerificationStatus.Contains("stale", StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrWhiteSpace(item.RawSourcePath)
                && !NormalizePath(item.RawSourcePath).Equals(sourcePath, StringComparison.OrdinalIgnoreCase));
        return new OntologyNode
        {
            StableUri = item.StableId,
            SourceGraphId = item.SourceGraphId ?? string.Empty,
            CanonicalName = item.CanonicalName,
            QualifiedName = qualified,
            Types = item.Types ?? [],
            EffectiveType = effectiveType,
            RawType = item.RawElementType,
            SourceVerificationStatus = item.SourceVerificationStatus,
            Layer = item.ElementLayer ?? string.Empty,
            SourcePath = sourcePath,
            RawSourcePath = item.RawSourcePath,
            LineStart = item.LineStart,
            LineEnd = item.LineEnd,
            Namespace = item.Namespace ?? string.Empty,
            Signature = item.Signature ?? string.Empty,
            ProjectName = item.ProjectName ?? string.Empty,
            PackageId = item.PackageId ?? string.Empty,
            ProjectVersion = item.ProjectVersion ?? string.Empty,
            DefaultSearchVisible = !excluded,
            IsStale = stale,
            IsExcluded = excluded,
            Labels = MapLocalized(item.Labels),
            Summaries = MapLocalized(item.Summaries),
            TargetFrameworks = item.TargetFrameworks ?? [],
            Tags = item.Tags ?? [],
            Evidence = MapEvidence(item.Evidence)
        };
    }

    private static OntologyRelation MapRelation(OntologyRelationDocument item) => new()
    {
        StableUri = item.StableId,
        SourceUri = item.Source,
        TargetUri = item.Target,
        OriginalType = item.RelationType,
        Evidence = MapEvidence(item.Evidence)
    };

    private static OntologyLocalizedText[] MapLocalized(List<OntologyLocalizedTextDocument>? values) =>
        values?.Select(item => new OntologyLocalizedText(item.Language, item.Text)).ToArray() ?? [];

    private static OntologyEvidence[] MapEvidence(List<OntologyEvidenceDocument>? values) =>
        values?.Select(item => new OntologyEvidence(item.Source, item.Value, item.Confidence)).ToArray() ?? [];

    private static bool Matches(OntologyNode node, string search) =>
        node.CanonicalName.Contains(search, StringComparison.OrdinalIgnoreCase)
        || node.QualifiedName.Contains(search, StringComparison.OrdinalIgnoreCase)
        || node.SourcePath.Contains(search, StringComparison.OrdinalIgnoreCase)
        || node.StableUri.Contains(search, StringComparison.OrdinalIgnoreCase)
        || node.Signature.Contains(search, StringComparison.OrdinalIgnoreCase)
        || node.Labels.Any(item => item.Text.Contains(search, StringComparison.OrdinalIgnoreCase))
        || node.Summaries.Any(item => item.Text.Contains(search, StringComparison.OrdinalIgnoreCase));

    private static void Add(Dictionary<string, List<OntologyRelation>> index, string key, OntologyRelation relation)
    {
        if (!index.TryGetValue(key, out List<OntologyRelation>? list))
            index[key] = list = [];
        list.Add(relation);
    }

    private static string RequiredPath(string directory, string fileName)
    {
        string path = Path.Combine(directory, fileName);
        if (!File.Exists(path))
            throw new FileNotFoundException($"Ontology artifact '{fileName}' was not found. Run Generate-DreamineOntology.ps1 and Publish-UnderstandDashboard.ps1.", path);
        return path;
    }

    private static FileStream OpenAsync(string path) => new(
        path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete,
        bufferSize: 128 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);

    private static async Task<string> ComputeHashAsync(string path, CancellationToken cancellationToken)
    {
        await using FileStream stream = OpenAsync(path);
        byte[] hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static IReadOnlyDictionary<string, FileStamp> CaptureStamps(string directory) =>
        WatchedFiles.ToDictionary(
            file => file,
            file =>
            {
                FileInfo info = new(Path.Combine(directory, file));
                return new FileStamp(info.Exists, info.Exists ? info.Length : 0, info.Exists ? info.LastWriteTimeUtc.Ticks : 0);
            },
            StringComparer.OrdinalIgnoreCase);

    private static bool StampsEqual(IReadOnlyDictionary<string, FileStamp> left, IReadOnlyDictionary<string, FileStamp> right) =>
        left.Count == right.Count && left.All(pair => right.TryGetValue(pair.Key, out FileStamp? value)
            && value is not null && value == pair.Value);

    private static string NormalizePath(string? value) => (value ?? string.Empty).Replace('\\', '/');

    private static string BuildHealthReason(
        IReadOnlyList<OntologyArtifactState> artifacts,
        OntologyValidationData data,
        bool fixturesClean)
    {
        OntologyArtifactState? broken = artifacts.FirstOrDefault(item => !item.Exists || !item.IsValid || item.IsStale);
        if (broken is not null)
            return $"{broken.Name} is missing, invalid, or older than seven days.";
        if (!data.LinkmlShaclConforms || !data.ArchitectureShaclConforms || data.LinkmlShaclViolations > 0)
            return "One or more SHACL reports do not conform.";
        if (!fixturesClean)
            return "One or more SHACL positive/negative fixtures failed.";
        return "SourceAudit contains unresolved mismatches, dangling relations, partial methods, or stable URI conflicts.";
    }

    private static JsonElement GetObject(JsonElement root, string property) =>
        root.ValueKind == JsonValueKind.Object && root.TryGetProperty(property, out JsonElement value) ? value : default;

    private static IEnumerable<JsonElement> GetArray(JsonElement root, string property) =>
        root.ValueKind == JsonValueKind.Object && root.TryGetProperty(property, out JsonElement value)
        && value.ValueKind == JsonValueKind.Array ? value.EnumerateArray() : [];

    private static string GetString(JsonElement root, string property) =>
        root.ValueKind == JsonValueKind.Object && root.TryGetProperty(property, out JsonElement value)
        && value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : string.Empty;

    private static int GetInt(JsonElement root, params string[] path)
    {
        JsonElement current = root;
        foreach (string segment in path)
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out current))
                return 0;
        }
        return current.ValueKind == JsonValueKind.Number && current.TryGetInt32(out int value) ? value : 0;
    }

    private static bool GetBool(JsonElement root, string property) =>
        root.ValueKind == JsonValueKind.Object && root.TryGetProperty(property, out JsonElement value)
        && value.ValueKind is JsonValueKind.True or JsonValueKind.False && value.GetBoolean();

    private static DateTimeOffset? GetDate(JsonElement root, string property) =>
        DateTimeOffset.TryParse(GetString(root, property), out DateTimeOffset value) ? value : null;

    private sealed record FileStamp(bool Exists, long Length, long ModifiedTicks);

    private sealed record CacheState(
        string Directory,
        IReadOnlyDictionary<string, FileStamp> Stamps,
        IReadOnlyList<OntologyNode> Nodes,
        IReadOnlyDictionary<string, OntologyNode> NodesByUri,
        IReadOnlyDictionary<string, List<OntologyRelation>> Outgoing,
        IReadOnlyDictionary<string, List<OntologyRelation>> Incoming,
        IReadOnlyDictionary<string, HashSet<string>> RelationNodes,
        IReadOnlyList<OntologyTBoxClass> TBoxClasses,
        OntologyFacets Facets,
        OntologyValidationData Validation,
        OntologyLoadMetrics Metrics);
}

internal sealed class OntologyInstancesDocument
{
    [JsonPropertyName("ontology_version")]
    public string OntologyVersion { get; set; } = string.Empty;

    [JsonPropertyName("generated_at")]
    public DateTimeOffset? GeneratedAt { get; set; }

    [JsonPropertyName("elements")]
    public List<OntologyElementDocument> Elements { get; set; } = [];

    [JsonPropertyName("relations")]
    public List<OntologyRelationDocument> Relations { get; set; } = [];
}

internal sealed class OntologyElementDocument
{
    [JsonPropertyName("@type")]
    public List<string>? Types { get; set; }

    [JsonPropertyName("stable_id")]
    public string StableId { get; set; } = string.Empty;

    [JsonPropertyName("source_graph_id")]
    public string? SourceGraphId { get; set; }

    [JsonPropertyName("canonical_name")]
    public string CanonicalName { get; set; } = string.Empty;

    [JsonPropertyName("element_type")]
    public string ElementType { get; set; } = string.Empty;

    [JsonPropertyName("element_layer")]
    public string? ElementLayer { get; set; }

    [JsonPropertyName("raw_element_type")]
    public string? RawElementType { get; set; }

    [JsonPropertyName("source_verification_status")]
    public string SourceVerificationStatus { get; set; } = "raw";

    [JsonPropertyName("source_path")]
    public string? SourcePath { get; set; }

    [JsonPropertyName("raw_source_path")]
    public string? RawSourcePath { get; set; }

    [JsonPropertyName("line_start")]
    public int? LineStart { get; set; }

    [JsonPropertyName("line_end")]
    public int? LineEnd { get; set; }

    [JsonPropertyName("namespace")]
    public string? Namespace { get; set; }

    [JsonPropertyName("signature")]
    public string? Signature { get; set; }

    [JsonPropertyName("project_name")]
    public string? ProjectName { get; set; }

    [JsonPropertyName("package_id")]
    public string? PackageId { get; set; }

    [JsonPropertyName("project_version")]
    public string? ProjectVersion { get; set; }

    [JsonPropertyName("default_search_visible")]
    public bool? DefaultSearchVisible { get; set; }

    [JsonPropertyName("labels")]
    public List<OntologyLocalizedTextDocument>? Labels { get; set; }

    [JsonPropertyName("summaries")]
    public List<OntologyLocalizedTextDocument>? Summaries { get; set; }

    [JsonPropertyName("target_frameworks")]
    public List<string>? TargetFrameworks { get; set; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }

    [JsonPropertyName("evidence")]
    public List<OntologyEvidenceDocument>? Evidence { get; set; }
}

internal sealed class OntologyRelationDocument
{
    [JsonPropertyName("stable_id")]
    public string StableId { get; set; } = string.Empty;

    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("target")]
    public string Target { get; set; } = string.Empty;

    [JsonPropertyName("relation_type")]
    public string RelationType { get; set; } = string.Empty;

    [JsonPropertyName("evidence")]
    public List<OntologyEvidenceDocument>? Evidence { get; set; }
}

internal sealed class OntologyLocalizedTextDocument
{
    [JsonPropertyName("language")]
    public string Language { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

internal sealed class OntologyEvidenceDocument
{
    [JsonPropertyName("evidence_source")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("evidence_value")]
    public string Value { get; set; } = string.Empty;

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }
}

internal sealed class OntologyManifestDocument
{
    [JsonPropertyName("ontologyVersion")]
    public string OntologyVersion { get; set; } = string.Empty;

    [JsonPropertyName("sourceGraphVersion")]
    public string SourceGraphVersion { get; set; } = string.Empty;

    [JsonPropertyName("generatedAt")]
    public DateTimeOffset? GeneratedAt { get; set; }

    [JsonPropertyName("counts")]
    public OntologyManifestCounts Counts { get; set; } = new();
}

internal sealed class OntologyManifestCounts
{
    [JsonPropertyName("elements")]
    public int Elements { get; set; }

    [JsonPropertyName("relations")]
    public int Relations { get; set; }
}
