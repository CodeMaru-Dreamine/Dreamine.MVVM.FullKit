using Dreamine.Secs.Abstractions.Model;
using Dreamine.SecsGem.Interop.Runtime.Persistence;
using Dreamine.SecsGem.Interop.Runtime.Scenarios;
using Dreamine.SecsGem.Interop.Runtime.Templates;

namespace Dreamine.SecsGem.Interop.Runtime.Responders;

/// <summary>A concrete versioned set of exact S/F/W responder rules.</summary>
public sealed class ResponderConfigurationV1 : IVersionedJsonDocument
{
    public const string SchemaName = "dreamine.secs-gem.responder";
    public const int CurrentSchemaVersion = 1;
    public const int MaximumRules = 1_024;
    public const int MaximumShutdownTimeoutMilliseconds = 30_000;

    public string Schema { get; init; } = SchemaName;
    public int Version { get; init; } = CurrentSchemaVersion;
    public int ShutdownTimeoutMilliseconds { get; init; } = 5_000;
    public List<ResponderRuleV1> Rules { get; init; } = [];

    public void Validate()
    {
        if (!StringComparer.Ordinal.Equals(Schema, SchemaName) || Version != CurrentSchemaVersion)
            throw new JsonSchemaVersionException(SchemaName, CurrentSchemaVersion, Schema, Version);
        if (ShutdownTimeoutMilliseconds is < 1 or > MaximumShutdownTimeoutMilliseconds)
            throw new ArgumentOutOfRangeException(nameof(ShutdownTimeoutMilliseconds));
        ArgumentNullException.ThrowIfNull(Rules);
        if (Rules.Count is < 1 or > MaximumRules) throw new ArgumentOutOfRangeException(nameof(Rules));
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var dialogues = new HashSet<(byte Stream, byte Function)>();
        foreach (var rule in Rules)
        {
            ArgumentNullException.ThrowIfNull(rule);
            rule.Validate();
            if (!ids.Add(rule.Id)) throw new ArgumentException($"Responder rule ID '{rule.Id}' is duplicated.", nameof(Rules));
            if (!dialogues.Add((rule.Stream, rule.PrimaryFunction)))
                throw new ArgumentException(
                    $"Responder v1 allows only one exact W rule for S{rule.Stream}F{rule.PrimaryFunction}; duplicate S/F registrations are ambiguous.",
                    nameof(Rules));
        }
    }
}

public enum ResponderReplyModeV1 { Immediate, Delayed, NoReply }
public enum ResponderInvocationModeV1 { Once, Repeat }

/// <summary>Matches one exact normal Primary dialogue and applies one bounded response action.</summary>
public sealed class ResponderRuleV1
{
    public string Id { get; init; } = string.Empty;
    public bool Enabled { get; init; } = true;
    public byte Stream { get; init; }
    public byte PrimaryFunction { get; init; }
    public bool ReplyExpected { get; init; }
    public ResponderReplyModeV1 ReplyMode { get; init; }
    public int DelayMilliseconds { get; init; }
    public ResponderInvocationModeV1 InvocationMode { get; init; }
    public SecsItemTemplateNode? ReplyBody { get; init; }

    internal SecsDialogueDefinition CreateDialogue() => new(
        new SecsStream(Stream),
        new SecsFunction(PrimaryFunction),
        ReplyExpected ? new SecsFunction(checked((byte)(PrimaryFunction + 1))) : null);

    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id) || Id.Length > 128)
            throw new ArgumentException("Responder rule IDs must contain 1 through 128 characters.", nameof(Id));
        _ = CreateDialogue();
        if (!Enum.IsDefined(ReplyMode)) throw new ArgumentOutOfRangeException(nameof(ReplyMode));
        if (!Enum.IsDefined(InvocationMode)) throw new ArgumentOutOfRangeException(nameof(InvocationMode));
        switch (ReplyMode)
        {
            case ResponderReplyModeV1.Immediate:
                if (!ReplyExpected) throw new ArgumentException("A W0 rule cannot send a reply.", nameof(ReplyExpected));
                if (DelayMilliseconds != 0) throw new ArgumentOutOfRangeException(nameof(DelayMilliseconds));
                break;
            case ResponderReplyModeV1.Delayed:
                if (!ReplyExpected) throw new ArgumentException("A W0 rule cannot send a delayed reply.", nameof(ReplyExpected));
                if (DelayMilliseconds is < 1 or > ScenarioLimitsV1.MaximumStepTimeoutMilliseconds)
                    throw new ArgumentOutOfRangeException(nameof(DelayMilliseconds));
                break;
            case ResponderReplyModeV1.NoReply:
                if (DelayMilliseconds != 0) throw new ArgumentOutOfRangeException(nameof(DelayMilliseconds));
                if (ReplyBody is not null) throw new ArgumentException("A no-reply rule cannot contain a reply body.", nameof(ReplyBody));
                break;
        }
        if (ReplyBody is not null) ScenarioDefinitionV1.ValidateItem(ReplyBody);
    }
}

public sealed class ResponderFileStoreV1
{
    private readonly VersionedJsonFileStore<ResponderConfigurationV1> _store = new(
        ResponderConfigurationV1.SchemaName,
        ResponderConfigurationV1.CurrentSchemaVersion,
        static configuration => configuration.Validate(),
        new JsonPersistenceLimits(
            ScenarioLimitsV1.MaximumFileSizeBytes,
            ScenarioLimitsV1.MaximumJsonDepth,
            ScenarioLimitsV1.MaximumJsonNodes));

    public Task<ResponderConfigurationV1> LoadAsync(string path, CancellationToken cancellationToken = default) =>
        _store.LoadAsync(path, cancellationToken);

    public Task SaveAsync(
        string path,
        ResponderConfigurationV1 configuration,
        CancellationToken cancellationToken = default) =>
        _store.SaveAsync(path, configuration, cancellationToken);
}

public enum ResponderShutdownStatusV1 { Completed, AlreadyStopped, TimedOut, Cancelled }

public sealed record ResponderShutdownResultV1(
    ResponderShutdownStatusV1 Status,
    int RemainingHandlerCount,
    string? ErrorMessage = null);

public sealed class ResponderFaultEventArgs(
    string ruleId,
    Exception exception,
    DateTimeOffset observedAtUtc) : EventArgs
{
    public string RuleId { get; } = ruleId;
    public Exception Exception { get; } = exception;
    public DateTimeOffset ObservedAtUtc { get; } = observedAtUtc;
}
