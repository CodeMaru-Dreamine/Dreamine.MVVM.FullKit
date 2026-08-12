namespace Dreamine.SecsGem.Interop.Runtime.Persistence;

/// <summary>Identifies a persisted document with a stable schema name and version.</summary>
public interface IVersionedJsonDocument
{
    /// <summary>Gets the stable schema identifier.</summary>
    string Schema { get; }

    /// <summary>Gets the document schema version.</summary>
    int Version { get; }
}

/// <summary>Defines defensive limits applied before a JSON document is materialized.</summary>
public sealed record JsonPersistenceLimits(
    int MaximumFileSizeBytes = 4 * 1024 * 1024,
    int MaximumJsonDepth = 64,
    int MaximumNodeCount = 100_000)
{
    /// <summary>Validates the configured limits.</summary>
    public void Validate()
    {
        if (MaximumFileSizeBytes is <= 0 or > 256 * 1024 * 1024)
            throw new ArgumentOutOfRangeException(nameof(MaximumFileSizeBytes));
        if (MaximumJsonDepth is <= 0 or > 256)
            throw new ArgumentOutOfRangeException(nameof(MaximumJsonDepth));
        if (MaximumNodeCount is <= 0 or > 10_000_000)
            throw new ArgumentOutOfRangeException(nameof(MaximumNodeCount));
    }
}

/// <summary>Represents a bounded JSON persistence failure.</summary>
public class JsonPersistenceException : IOException
{
    /// <summary>Creates a persistence failure.</summary>
    public JsonPersistenceException(string message) : base(message) { }

    /// <summary>Creates a persistence failure with an underlying error.</summary>
    public JsonPersistenceException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>Reports a file-size, JSON-depth, or JSON-node limit violation.</summary>
public sealed class JsonInputLimitException : JsonPersistenceException
{
    /// <summary>Creates an input-limit failure.</summary>
    public JsonInputLimitException(string message) : base(message) { }

    /// <summary>Creates an input-limit failure with an underlying parser error.</summary>
    public JsonInputLimitException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>Reports a missing, unknown, or unsupported JSON schema/version.</summary>
public sealed class JsonSchemaVersionException : JsonPersistenceException
{
    /// <summary>Creates a schema/version failure.</summary>
    public JsonSchemaVersionException(
        string expectedSchema,
        int expectedVersion,
        string? actualSchema,
        int? actualVersion)
        : base(CreateMessage(expectedSchema, expectedVersion, actualSchema, actualVersion))
    {
        ExpectedSchema = expectedSchema;
        ExpectedVersion = expectedVersion;
        ActualSchema = actualSchema;
        ActualVersion = actualVersion;
    }

    /// <summary>Gets the expected schema identifier.</summary>
    public string ExpectedSchema { get; }

    /// <summary>Gets the supported schema version.</summary>
    public int ExpectedVersion { get; }

    /// <summary>Gets the schema identifier found in the input, if any.</summary>
    public string? ActualSchema { get; }

    /// <summary>Gets the schema version found in the input, if any.</summary>
    public int? ActualVersion { get; }

    private static string CreateMessage(string expectedSchema, int expectedVersion, string? actualSchema,
        int? actualVersion) =>
        $"Expected JSON schema '{expectedSchema}' version {expectedVersion}, but found " +
        $"'{actualSchema ?? "<missing>"}' version {(actualVersion?.ToString() ?? "<missing>")}. " +
        "Newer, older, and unknown schemas require an explicit migration.";
}
