using DreamineWeb.Ontology.Application;
using DreamineWeb.Ontology.Domain;
using System.IO;
using System.Text.Json;

namespace DreamineWeb.Ontology.Infrastructure;

/// <summary>Reads generated source mirrors through source-verified stable URIs.</summary>
public sealed class OntologySourceService : IOntologySourceService
{
    private const long MaximumArtifactBytes = 8 * 1024 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly IOntologyRepository _repository;
    private readonly IOntologySourcePathResolver _pathResolver;

    public OntologySourceService(IOntologyRepository repository, IOntologySourcePathResolver pathResolver)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _pathResolver = pathResolver ?? throw new ArgumentNullException(nameof(pathResolver));
    }

    /// <inheritdoc />
    public async Task<OntologySourceAvailabilityViewModel> GetAvailabilityAsync(
        string stableUri,
        CancellationToken cancellationToken)
    {
        OntologyNode? node = await _repository.GetNodeAsync(stableUri, cancellationToken).ConfigureAwait(false);
        return ResolveAvailability(node, out _);
    }

    /// <inheritdoc />
    public async Task<OntologySourceDocumentViewModel> GetSourceAsync(
        string stableUri,
        CancellationToken cancellationToken)
    {
        OntologyNode? node = await _repository.GetNodeAsync(stableUri, cancellationToken).ConfigureAwait(false);
        OntologySourceAvailabilityViewModel availability = ResolveAvailability(node, out string? artifactPath);
        if (!availability.IsAvailable || node is null || artifactPath is null)
            return new OntologySourceDocumentViewModel(availability, string.Empty, [], null, null);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            FileInfo info = new(artifactPath);
            if (!info.Exists || info.Length is <= 0 or > MaximumArtifactBytes)
                return Unavailable(node.SourcePath, "invalid-artifact");

            await using FileStream stream = new(
                artifactPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite,
                64 * 1024,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            SourceMirrorDocument? source = await JsonSerializer.DeserializeAsync<SourceMirrorDocument>(
                stream,
                JsonOptions,
                cancellationToken).ConfigureAwait(false);
            string normalizedPayloadPath = NormalizeRelativePath(source?.Path);
            if (source is null || source.Content is null
                || !normalizedPayloadPath.Equals(NormalizeRelativePath(node.SourcePath), StringComparison.OrdinalIgnoreCase))
                return Unavailable(node.SourcePath, "invalid-artifact");

            string[] textLines = source.Content.Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace('\r', '\n').Split('\n');
            int? highlightStart = NormalizeLine(node.LineStart, textLines.Length);
            int? highlightEnd = NormalizeLine(node.LineEnd ?? node.LineStart, textLines.Length);
            if (highlightStart.HasValue && highlightEnd.HasValue && highlightEnd < highlightStart)
                highlightEnd = highlightStart;
            OntologySourceLineViewModel[] lines = new OntologySourceLineViewModel[textLines.Length];
            for (int index = 0; index < textLines.Length; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                int number = index + 1;
                bool highlighted = highlightStart.HasValue && highlightEnd.HasValue
                    && number >= highlightStart && number <= highlightEnd;
                lines[index] = new OntologySourceLineViewModel(number, textLines[index], highlighted);
            }

            return new OntologySourceDocumentViewModel(
                availability,
                source.Language ?? string.Empty,
                lines,
                highlightStart,
                highlightEnd);
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
        {
            return Unavailable(node.SourcePath, "invalid-artifact");
        }
    }

    private OntologySourceAvailabilityViewModel ResolveAvailability(OntologyNode? node, out string? artifactPath)
    {
        artifactPath = null;
        if (node is null)
            return new OntologySourceAvailabilityViewModel(false, false, string.Empty, "node-not-found");
        if (node.IsExcluded || node.SourceVerificationStatus.Equals("excluded_generated", StringComparison.OrdinalIgnoreCase))
            return new OntologySourceAvailabilityViewModel(false, true, NormalizeRelativePath(node.SourcePath), "generated-excluded");
        if (node.SourceVerificationStatus.Equals("stale_quarantined", StringComparison.OrdinalIgnoreCase))
            return new OntologySourceAvailabilityViewModel(false, true, NormalizeRelativePath(node.SourcePath), "stale-excluded");

        string relativePath = NormalizeRelativePath(node.SourcePath);
        if (string.IsNullOrWhiteSpace(relativePath) || !TryResolveArtifactPath(relativePath, out artifactPath))
            return new OntologySourceAvailabilityViewModel(false, false, relativePath, "invalid-path");
        if (!File.Exists(artifactPath) || new FileInfo(artifactPath).Length <= 0)
            return new OntologySourceAvailabilityViewModel(false, false, relativePath, "source-unavailable");
        return new OntologySourceAvailabilityViewModel(true, false, relativePath, string.Empty);
    }

    private bool TryResolveArtifactPath(string relativePath, out string? artifactPath)
    {
        artifactPath = null;
        if (Path.IsPathRooted(relativePath) || relativePath.Contains(':', StringComparison.Ordinal))
            return false;
        string[] segments = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0 || segments.Any(segment => segment is "." or ".."))
            return false;

        string root = Path.GetFullPath(_pathResolver.ResolveSourceDirectory());
        string rootBoundary = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
        string candidate = Path.GetFullPath(Path.Combine([root, .. segments]) + ".json");
        if (!candidate.StartsWith(rootBoundary, StringComparison.OrdinalIgnoreCase))
            return false;
        artifactPath = candidate;
        return true;
    }

    private static string NormalizeRelativePath(string? value) =>
        string.Join('/', (value ?? string.Empty).Replace('\\', '/').TrimStart('/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries));

    private static int? NormalizeLine(int? line, int lineCount) =>
        line.HasValue && line.Value > 0 && line.Value <= lineCount ? line.Value : null;

    private static OntologySourceDocumentViewModel Unavailable(string displayPath, string reason) =>
        new(new OntologySourceAvailabilityViewModel(false, false, NormalizeRelativePath(displayPath), reason), string.Empty, [], null, null);

    private sealed record SourceMirrorDocument(string? Path, string? Language, string? Content);
}
