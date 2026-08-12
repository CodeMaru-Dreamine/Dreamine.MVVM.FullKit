using Dreamine.SecsGem.Interop.Runtime.Logging;
using Dreamine.SecsGem.Interop.Runtime.Persistence;

namespace Dreamine.SecsGem.Interop.Runtime.Evidence;

internal enum EvidenceReviewState
{
    EvidenceRecorded,
    ManualReview,
    Verified
}

internal enum EvidenceArtifactKind
{
    DreamineLog,
    CounterpartLog,
    Screenshot,
    Configuration,
    Other
}

internal sealed record EvidenceArtifact(
    string Label,
    EvidenceArtifactKind Kind,
    string Sha256)
{
    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(Label)) throw new ArgumentException("An artifact label is required.", nameof(Label));
        if (!Enum.IsDefined(Kind)) throw new ArgumentOutOfRangeException(nameof(Kind));
        if (Sha256.Length != 64 || Sha256.Any(value => !Uri.IsHexDigit(value)))
            throw new ArgumentException("Artifact SHA-256 must contain 64 hexadecimal characters.", nameof(Sha256));
    }
}

internal sealed record EvidenceChecklistItem(string Id, string Description, bool Confirmed)
{
    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id)) throw new ArgumentException("A checklist ID is required.", nameof(Id));
        if (string.IsNullOrWhiteSpace(Description)) throw new ArgumentException("A checklist description is required.", nameof(Description));
    }
}

internal sealed record InteropEvidenceManifest(
    int SchemaVersion,
    string RunId,
    string Operator,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    string ToolName,
    string ToolVersion,
    string ConfigurationSha256,
    EvidenceReviewState ReviewState,
    WireLogHealth WireLogHealth,
    IReadOnlyList<EvidenceArtifact> Artifacts,
    IReadOnlyList<EvidenceChecklistItem> Checklist,
    string? ReviewNote = null)
{
    internal const int CurrentSchemaVersion = 1;

    internal EvidenceEligibility EvaluateExternalEligibility()
    {
        var reasons = new List<string>();
        if (SchemaVersion != CurrentSchemaVersion) reasons.Add($"Unsupported manifest schema version {SchemaVersion}.");
        if (string.IsNullOrWhiteSpace(RunId)) reasons.Add("Run ID is missing.");
        if (string.IsNullOrWhiteSpace(Operator)) reasons.Add("Operator is missing.");
        if (StartedAtUtc.Offset != TimeSpan.Zero || CompletedAtUtc.Offset != TimeSpan.Zero || CompletedAtUtc < StartedAtUtc)
            reasons.Add("Run timestamps must be ordered UTC values.");
        if (string.IsNullOrWhiteSpace(ToolName) || string.IsNullOrWhiteSpace(ToolVersion)) reasons.Add("Tool identity is incomplete.");
        if (ConfigurationSha256.Length != 64 || ConfigurationSha256.Any(value => !Uri.IsHexDigit(value)))
            reasons.Add("Configuration SHA-256 is invalid.");
        if (ReviewState != EvidenceReviewState.Verified) reasons.Add("Manual verification is not complete.");
        if (WireLogHealth is null)
            reasons.Add("Wire-log health is missing.");
        else if (!WireLogHealth.IsEvidenceEligible)
            reasons.Add("Wire logging reported a drop, writer failure, or incomplete flush.");

        if (Artifacts is null) reasons.Add("Evidence artifacts are missing.");
        if (Checklist is null) reasons.Add("The evidence checklist is missing.");
        var artifacts = Artifacts ?? [];
        var checklist = Checklist ?? [];
        foreach (var artifact in artifacts)
        {
            try
            {
                if (artifact is null) throw new ArgumentException("An evidence artifact is null.");
                artifact.Validate();
            }
            catch (Exception exception) { reasons.Add(exception.Message); }
        }
        foreach (var item in checklist)
        {
            try
            {
                if (item is null) throw new ArgumentException("An evidence checklist item is null.");
                item.Validate();
            }
            catch (Exception exception) { reasons.Add(exception.Message); }
        }
        if (!artifacts.Any(value => value?.Kind == EvidenceArtifactKind.DreamineLog)) reasons.Add("Dreamine evidence is missing.");
        if (!artifacts.Any(value => value?.Kind is EvidenceArtifactKind.CounterpartLog or EvidenceArtifactKind.Screenshot))
            reasons.Add("Counterpart evidence is missing.");
        if (checklist.Count == 0 || checklist.Any(value => value is null || !value.Confirmed))
            reasons.Add("The evidence checklist is incomplete.");

        return new(reasons.Count == 0, reasons);
    }
}

internal sealed record InteropEvidenceDocument(
    string Schema,
    int Version,
    InteropEvidenceManifest Manifest) : IVersionedJsonDocument
{
    internal const string SchemaName = "dreamine.interop-evidence";
    internal const int CurrentVersion = 1;

    internal static InteropEvidenceDocument Create(InteropEvidenceManifest manifest) =>
        new(SchemaName, CurrentVersion, manifest ?? throw new ArgumentNullException(nameof(manifest)));

    internal void Validate()
    {
        if (!StringComparer.Ordinal.Equals(Schema, SchemaName) || Version != CurrentVersion)
            throw new JsonSchemaVersionException(SchemaName, CurrentVersion, Schema, Version);
        if (Manifest is null) throw new JsonPersistenceException("The evidence manifest is missing.");
        if (Manifest.SchemaVersion != InteropEvidenceManifest.CurrentSchemaVersion)
            throw new JsonPersistenceException(
                $"Unsupported evidence manifest schema version {Manifest.SchemaVersion}.");
    }
}

internal sealed class EvidenceManifestStore
{
    private readonly VersionedJsonFileStore<InteropEvidenceDocument> _store;

    internal EvidenceManifestStore(JsonPersistenceLimits? limits = null)
    {
        _store = new VersionedJsonFileStore<InteropEvidenceDocument>(
            InteropEvidenceDocument.SchemaName,
            InteropEvidenceDocument.CurrentVersion,
            document => document.Validate(),
            limits);
    }

    internal async Task<InteropEvidenceManifest> LoadAsync(
        string path,
        CancellationToken cancellationToken = default) =>
        (await _store.LoadAsync(path, cancellationToken).ConfigureAwait(false)).Manifest;

    internal Task SaveAsync(
        string path,
        InteropEvidenceManifest manifest,
        CancellationToken cancellationToken = default) =>
        _store.SaveAsync(path, InteropEvidenceDocument.Create(manifest), cancellationToken);
}

internal sealed record EvidenceEligibility(bool EligibleForExternalPassReview, IReadOnlyList<string> Reasons);

internal sealed record SafeWireLogExportRecord(
    int SchemaVersion,
    long Sequence,
    long ConnectionEpoch,
    DateTimeOffset TimestampUtc,
    string Direction,
    string ConnectionReference,
    ushort? SessionId,
    byte? Stream,
    byte? Function,
    bool? ReplyExpected,
    byte? PType,
    byte? SType,
    uint? SystemBytes,
    int ActualFrameBytes,
    WireBodyCaptureMode CaptureMode,
    int CapturedBodyBytes,
    bool SourceCaptureTruncated,
    string? DecodeStatus,
    string? TransactionStatus,
    string? Error);

internal static class EvidencePrivacySanitizer
{
    internal static SafeWireLogExportRecord Sanitize(WireLogRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        return new(
            record.SchemaVersion,
            record.Sequence,
            record.ConnectionEpoch,
            record.TimestampUtc,
            record.Direction?.ToString() ?? "None",
            "CONNECTION-REDACTED",
            record.HeaderSessionId,
            record.Stream,
            record.Function,
            record.ReplyExpected,
            record.PType,
            record.SType,
            record.SystemBytes,
            record.ActualFrameBytes,
            record.CaptureMode,
            record.CapturedBodyBytes,
            record.SourceCaptureTruncated,
            record.DecodeError is not null ? "Decode error recorded; detail withheld" :
                record.DecodedItem is not null ? "Decoded body withheld" : null,
            record.TransactionStatus is null ? null : "Transaction status recorded; detail withheld",
            record.Error is null ? null : "Error recorded; detail withheld");
    }
}
