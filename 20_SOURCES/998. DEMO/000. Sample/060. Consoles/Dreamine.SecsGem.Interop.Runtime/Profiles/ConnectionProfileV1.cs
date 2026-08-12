using Dreamine.Secs.Abstractions.Enums;
using Dreamine.Secs.Abstractions.Hsms;
using Dreamine.Secs.Abstractions.Model;
using Dreamine.SecsGem.Interop.Runtime.Persistence;

namespace Dreamine.SecsGem.Interop.Runtime.Profiles;

/// <summary>Stable identifiers for built-in, versioned connection log policies.</summary>
public static class ConnectionLogPolicyIds
{
    /// <summary>Captures headers only and does not retain a message body.</summary>
    public const string HeaderOnlyV1 = "header-only-v1";

    /// <summary>Excludes the connection from body and raw-frame logging.</summary>
    public const string ExcludedV1 = "excluded-v1";

    /// <summary>Allows a separately confirmed, explicit full-body policy.</summary>
    public const string FullBodyExplicitV1 = "full-body-explicit-v1";

    internal static readonly IReadOnlySet<string> BuiltIn = new HashSet<string>(StringComparer.Ordinal)
    {
        HeaderOnlyV1,
        ExcludedV1,
        FullBodyExplicitV1
    };
}

/// <summary>Stores whole-second HSMS timer values used by Connection Profile v1.</summary>
public sealed record ConnectionTimerProfileV1(
    int T3Seconds = 45,
    int T5Seconds = 10,
    int T6Seconds = 5,
    int T7Seconds = 10,
    int T8Seconds = 5)
{
    internal HsmsTimerOptions ToOptions() => new()
    {
        T3 = TimeSpan.FromSeconds(T3Seconds),
        T5 = TimeSpan.FromSeconds(T5Seconds),
        T6 = TimeSpan.FromSeconds(T6Seconds),
        T7 = TimeSpan.FromSeconds(T7Seconds),
        T8 = TimeSpan.FromSeconds(T8Seconds)
    };
}

/// <summary>
/// Stores an operational reconnect-backoff policy separately from the protocol T5 timer.
/// The policy is consumed by an orchestration layer rather than changing T5 semantics.
/// </summary>
public sealed record OperationalReconnectPolicyV1(
    int InitialDelaySeconds = 1,
    int MaximumDelaySeconds = 30,
    double BackoffMultiplier = 2);

/// <summary>Stores frame and SECS-II item safety limits for Connection Profile v1.</summary>
public sealed record ConnectionSafetyLimitsV1(
    int MaximumFrameLength = 16 * 1024 * 1024,
    int MaximumMessageLength = 16 * 1024 * 1024 - 10,
    int MaximumNestingDepth = 64,
    int MaximumListItemCount = 65_535);

/// <summary>
/// A versioned, credential-free profile for one Host or Equipment HSMS connection.
/// Session construction settings are immutable snapshots; changing them requires recreation.
/// </summary>
public sealed record SingleConnectionProfileV1 : IVersionedJsonDocument
{
    /// <summary>The exact schema identifier for Connection Profile v1.</summary>
    public const string SchemaId = "dreamine.secs.connection-profile";

    /// <summary>The current supported schema version.</summary>
    public const int CurrentVersion = 1;

    /// <inheritdoc />
    public string Schema { get; init; } = SchemaId;

    /// <inheritdoc />
    public int Version { get; init; } = CurrentVersion;

    /// <summary>Gets the SECS application role.</summary>
    public SecsRole Role { get; init; } = SecsRole.Host;

    /// <summary>Gets whether TCP is initiated or accepted.</summary>
    public SecsConnectionMode Mode { get; init; } = SecsConnectionMode.Active;

    /// <summary>Gets the remote host or passive bind address. URI credentials are not accepted.</summary>
    public string Host { get; init; } = "127.0.0.1";

    /// <summary>Gets the TCP port.</summary>
    public int Port { get; init; } = 5000;

    /// <summary>Gets the SECS data-message Session ID.</summary>
    public ushort SessionId { get; init; }

    /// <summary>Gets whether an Active session reconnects after an unexpected disconnect.</summary>
    public bool AutoReconnect { get; init; }

    /// <summary>Gets protocol timer values.</summary>
    public ConnectionTimerProfileV1 Timers { get; init; } = new();

    /// <summary>Gets operational backoff separately from T5.</summary>
    public OperationalReconnectPolicyV1 ReconnectPolicy { get; init; } = new();

    /// <summary>Gets frame and item codec safety limits.</summary>
    public ConnectionSafetyLimitsV1 SafetyLimits { get; init; } = new();

    /// <summary>Gets the stable ID of a separately resolved log policy.</summary>
    public string LogPolicyId { get; init; } = ConnectionLogPolicyIds.HeaderOnlyV1;

    /// <summary>Validates the profile against built-in log policy identifiers.</summary>
    public void Validate() => Validate(ConnectionLogPolicyIds.BuiltIn);

    /// <summary>
    /// Validates the profile against a caller-owned policy registry. Unknown policy IDs fail closed;
    /// they are never treated as full-body logging.
    /// </summary>
    public void Validate(IReadOnlySet<string> knownLogPolicyIds)
    {
        ArgumentNullException.ThrowIfNull(knownLogPolicyIds);
        try
        {
            if (!StringComparer.Ordinal.Equals(Schema, SchemaId) || Version != CurrentVersion)
                throw new ConnectionProfileValidationException(
                    $"Only schema '{SchemaId}' version {CurrentVersion} is supported.");
            if (Role is not (SecsRole.Host or SecsRole.Equipment))
                throw new ConnectionProfileValidationException("Role must be Host or Equipment.");
            if (Mode is not (SecsConnectionMode.Active or SecsConnectionMode.Passive))
                throw new ConnectionProfileValidationException("Mode must be Active or Passive.");
            ValidateHost(Host);
            if (SessionId > SecsSessionId.MaximumValue)
                throw new ConnectionProfileValidationException(
                    $"SessionId must be between 0 and {SecsSessionId.MaximumValue}.");
            if (Timers is null) throw new ConnectionProfileValidationException("Timers are required.");
            if (ReconnectPolicy is null)
                throw new ConnectionProfileValidationException("ReconnectPolicy is required.");
            if (SafetyLimits is null)
                throw new ConnectionProfileValidationException("SafetyLimits are required.");
            ValidateReconnectPolicy(ReconnectPolicy);
            if (string.IsNullOrWhiteSpace(LogPolicyId) || !knownLogPolicyIds.Contains(LogPolicyId))
                throw new ConnectionProfileValidationException(
                    $"LogPolicyId '{LogPolicyId}' is not registered; loading fails closed.");

            _ = CreateOptions(validateProfile: false);
        }
        catch (ConnectionProfileValidationException)
        {
            throw;
        }
        catch (Exception exception) when (exception is ArgumentException or OverflowException)
        {
            throw new ConnectionProfileValidationException("The connection profile is invalid.", exception);
        }
    }

    /// <summary>Creates validated immutable session options from the profile.</summary>
    public HsmsSessionOptions ToHsmsSessionOptions()
    {
        Validate();
        return CreateOptions(validateProfile: false);
    }

    private HsmsSessionOptions CreateOptions(bool validateProfile)
    {
        if (validateProfile) Validate();
        var options = new HsmsSessionOptions
        {
            Host = Host,
            Port = Port,
            Mode = Mode,
            Role = Role,
            SessionId = new SecsSessionId(SessionId),
            AutoReconnect = AutoReconnect,
            Timers = Timers.ToOptions(),
            MaximumFrameLength = SafetyLimits.MaximumFrameLength,
            MaximumMessageLength = SafetyLimits.MaximumMessageLength,
            MaximumNestingDepth = SafetyLimits.MaximumNestingDepth,
            MaximumListItemCount = SafetyLimits.MaximumListItemCount
        };
        options.Validate();
        return options;
    }

    private static void ValidateHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host) || host.Length > 255)
            throw new ConnectionProfileValidationException("Host is required and must be at most 255 characters.");
        if (!StringComparer.Ordinal.Equals(host, host.Trim()) ||
            host.Any(char.IsControl) ||
            host.Any(char.IsWhiteSpace) ||
            host.Contains('@') ||
            host.Contains('/') ||
            host.Contains('\\') ||
            host.Contains("://", StringComparison.Ordinal))
            throw new ConnectionProfileValidationException(
                "Host must be a hostname or IP/bind address without credentials, a URI scheme, a path, or whitespace.");
    }

    private static void ValidateReconnectPolicy(OperationalReconnectPolicyV1 policy)
    {
        if (policy.InitialDelaySeconds is < 0 or > 3600)
            throw new ConnectionProfileValidationException("InitialDelaySeconds must be between 0 and 3600.");
        if (policy.MaximumDelaySeconds < policy.InitialDelaySeconds || policy.MaximumDelaySeconds > 86_400)
            throw new ConnectionProfileValidationException(
                "MaximumDelaySeconds must be at least InitialDelaySeconds and no greater than 86400.");
        if (!double.IsFinite(policy.BackoffMultiplier) || policy.BackoffMultiplier is < 1 or > 10)
            throw new ConnectionProfileValidationException("BackoffMultiplier must be finite and between 1 and 10.");
    }
}

/// <summary>Reports invalid Connection Profile v1 content.</summary>
public sealed class ConnectionProfileValidationException : IOException
{
    /// <summary>Creates a profile validation error.</summary>
    public ConnectionProfileValidationException(string message) : base(message) { }

    /// <summary>Creates a profile validation error with an underlying error.</summary>
    public ConnectionProfileValidationException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>Classifies the operational effect of applying one profile over another.</summary>
public enum ConnectionProfileApplyDisposition
{
    /// <summary>The profiles are equivalent.</summary>
    NoChanges,
    /// <summary>Only settings owned outside the current session can be applied immediately.</summary>
    ImmediateOnly,
    /// <summary>At least one immutable session-construction setting changed.</summary>
    RecreateRequired
}

/// <summary>Describes immediate and session-recreation changes without mutating a live connection.</summary>
public sealed record ConnectionProfileApplyDiff(
    ConnectionProfileApplyDisposition Disposition,
    IReadOnlyList<string> ImmediateChanges,
    IReadOnlyList<string> RecreateRequiredChanges)
{
    /// <summary>Gets whether the live session must be stopped and recreated.</summary>
    public bool RequiresSessionRecreation => RecreateRequiredChanges.Count != 0;

    /// <summary>Compares two validated profiles.</summary>
    public static ConnectionProfileApplyDiff Compare(
        SingleConnectionProfileV1 current,
        SingleConnectionProfileV1 next)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(next);
        current.Validate();
        next.Validate();

        var immediate = new List<string>();
        var recreate = new List<string>();
        AddIfChanged(current.LogPolicyId, next.LogPolicyId, nameof(SingleConnectionProfileV1.LogPolicyId), immediate);
        AddIfChanged(current.Role, next.Role, nameof(SingleConnectionProfileV1.Role), recreate);
        AddIfChanged(current.Mode, next.Mode, nameof(SingleConnectionProfileV1.Mode), recreate);
        AddIfChanged(current.Host, next.Host, nameof(SingleConnectionProfileV1.Host), recreate);
        AddIfChanged(current.Port, next.Port, nameof(SingleConnectionProfileV1.Port), recreate);
        AddIfChanged(current.SessionId, next.SessionId, nameof(SingleConnectionProfileV1.SessionId), recreate);
        AddIfChanged(current.AutoReconnect, next.AutoReconnect,
            nameof(SingleConnectionProfileV1.AutoReconnect), recreate);
        AddIfChanged(current.Timers, next.Timers, nameof(SingleConnectionProfileV1.Timers), recreate);
        AddIfChanged(current.ReconnectPolicy, next.ReconnectPolicy,
            nameof(SingleConnectionProfileV1.ReconnectPolicy), recreate);
        AddIfChanged(current.SafetyLimits, next.SafetyLimits,
            nameof(SingleConnectionProfileV1.SafetyLimits), recreate);

        var disposition = recreate.Count != 0
            ? ConnectionProfileApplyDisposition.RecreateRequired
            : immediate.Count != 0
                ? ConnectionProfileApplyDisposition.ImmediateOnly
                : ConnectionProfileApplyDisposition.NoChanges;
        return new ConnectionProfileApplyDiff(disposition, immediate.AsReadOnly(), recreate.AsReadOnly());
    }

    private static void AddIfChanged<T>(T current, T next, string name, ICollection<string> destination)
    {
        if (!EqualityComparer<T>.Default.Equals(current, next)) destination.Add(name);
    }
}

/// <summary>Creates a bounded Connection Profile v1 JSON store.</summary>
public static class ConnectionProfileStore
{
    /// <summary>Creates a store with built-in log policy identifiers.</summary>
    public static VersionedJsonFileStore<SingleConnectionProfileV1> Create(
        JsonPersistenceLimits? persistenceLimits = null,
        IEnumerable<string>? additionalLogPolicyIds = null)
    {
        var knownPolicies = new HashSet<string>(ConnectionLogPolicyIds.BuiltIn, StringComparer.Ordinal);
        if (additionalLogPolicyIds is not null)
        {
            foreach (var policyId in additionalLogPolicyIds)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(policyId);
                knownPolicies.Add(policyId);
            }
        }

        return new VersionedJsonFileStore<SingleConnectionProfileV1>(
            SingleConnectionProfileV1.SchemaId,
            SingleConnectionProfileV1.CurrentVersion,
            profile => profile.Validate(knownPolicies),
            persistenceLimits);
    }
}
