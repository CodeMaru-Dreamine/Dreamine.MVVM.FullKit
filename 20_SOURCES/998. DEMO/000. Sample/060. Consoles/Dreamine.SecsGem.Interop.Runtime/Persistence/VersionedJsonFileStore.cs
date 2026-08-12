using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dreamine.SecsGem.Interop.Runtime.Persistence;

/// <summary>
/// Loads and atomically replaces bounded, versioned JSON documents without a WPF dependency.
/// </summary>
/// <typeparam name="TDocument">The concrete persisted document type.</typeparam>
public sealed class VersionedJsonFileStore<TDocument>
    where TDocument : class, IVersionedJsonDocument
{
    private readonly string _schema;
    private readonly int _version;
    private readonly Action<TDocument> _validate;
    private readonly JsonPersistenceLimits _limits;
    private readonly JsonSerializerOptions _serializerOptions;

    /// <summary>Creates a store for one exact schema version.</summary>
    public VersionedJsonFileStore(
        string schema,
        int version,
        Action<TDocument> validate,
        JsonPersistenceLimits? limits = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schema);
        if (version <= 0) throw new ArgumentOutOfRangeException(nameof(version));
        ArgumentNullException.ThrowIfNull(validate);
        _limits = limits ?? new JsonPersistenceLimits();
        _limits.Validate();
        _schema = schema;
        _version = version;
        _validate = validate;
        _serializerOptions = CreateSerializerOptions(_limits.MaximumJsonDepth);
    }

    /// <summary>Loads, bounds, version-checks, deserializes, and validates one document.</summary>
    public async Task<TDocument> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(path);
        cancellationToken.ThrowIfCancellationRequested();
        var bytes = await ReadBoundedAsync(fullPath, cancellationToken).ConfigureAwait(false);
        ValidateEnvelope(bytes);

        TDocument document;
        try
        {
            document = JsonSerializer.Deserialize<TDocument>(bytes, _serializerOptions)
                ?? throw new JsonPersistenceException("The JSON document deserialized to null.");
        }
        catch (JsonPersistenceException)
        {
            throw;
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            throw new JsonPersistenceException("The JSON document does not match the supported schema.", exception);
        }

        EnsureCurrentVersion(document);
        _validate(document);
        return document;
    }

    /// <summary>
    /// Validates and writes one document to a same-directory temporary file, then atomically publishes it.
    /// The prior destination remains intact if validation, serialization, writing, or cancellation fails.
    /// </summary>
    public async Task SaveAsync(string path, TDocument document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        var fullPath = GetFullPath(path);
        cancellationToken.ThrowIfCancellationRequested();
        EnsureCurrentVersion(document);
        _validate(document);

        byte[] bytes;
        try
        {
            bytes = JsonSerializer.SerializeToUtf8Bytes(document, _serializerOptions);
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            throw new JsonPersistenceException("The document could not be serialized.", exception);
        }

        ValidateEnvelope(bytes);
        var directory = Path.GetDirectoryName(fullPath)
            ?? throw new ArgumentException("The path must have a parent directory.", nameof(path));
        if (!Directory.Exists(directory)) throw new DirectoryNotFoundException(directory);

        var temporaryPath = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            await using (var stream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                64 * 1024,
                FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                stream.Flush(flushToDisk: true);
            }

            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }

    private async Task<byte[]> ReadBoundedAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            64 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        if (stream.Length > _limits.MaximumFileSizeBytes)
            throw new JsonInputLimitException(
                $"Input is {stream.Length} bytes; the maximum is {_limits.MaximumFileSizeBytes} bytes.");

        using var output = new MemoryStream((int)Math.Min(stream.Length, _limits.MaximumFileSizeBytes));
        var buffer = new byte[Math.Min(64 * 1024, _limits.MaximumFileSizeBytes + 1)];
        while (true)
        {
            var read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (read == 0) break;
            if (output.Length + read > _limits.MaximumFileSizeBytes)
                throw new JsonInputLimitException(
                    $"Input grew beyond the maximum of {_limits.MaximumFileSizeBytes} bytes while being read.");
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
        }

        return output.ToArray();
    }

    private void ValidateEnvelope(ReadOnlyMemory<byte> bytes)
    {
        if (bytes.Length > _limits.MaximumFileSizeBytes)
            throw new JsonInputLimitException(
                $"Input is {bytes.Length} bytes; the maximum is {_limits.MaximumFileSizeBytes} bytes.");

        try
        {
            var reader = new Utf8JsonReader(bytes.Span, new JsonReaderOptions
            {
                CommentHandling = JsonCommentHandling.Disallow,
                AllowTrailingCommas = false,
                MaxDepth = _limits.MaximumJsonDepth
            });
            var nodeCount = 0;
            while (reader.Read())
            {
                if (reader.TokenType is JsonTokenType.EndArray or JsonTokenType.EndObject or JsonTokenType.PropertyName)
                    continue;
                if (++nodeCount > _limits.MaximumNodeCount)
                    throw new JsonInputLimitException(
                        $"JSON node count exceeds the maximum of {_limits.MaximumNodeCount}.");
            }

            using var json = JsonDocument.Parse(bytes, new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Disallow,
                AllowTrailingCommas = false,
                MaxDepth = _limits.MaximumJsonDepth
            });
            if (json.RootElement.ValueKind != JsonValueKind.Object)
                throw new JsonPersistenceException("The JSON root must be an object.");
            var actualSchema = json.RootElement.TryGetProperty("schema", out var schemaProperty) &&
                               schemaProperty.ValueKind == JsonValueKind.String
                ? schemaProperty.GetString()
                : null;
            var actualVersion = json.RootElement.TryGetProperty("version", out var versionProperty) &&
                                versionProperty.TryGetInt32(out var parsedVersion)
                ? (int?)parsedVersion
                : null;
            if (!StringComparer.Ordinal.Equals(actualSchema, _schema) || actualVersion != _version)
                throw new JsonSchemaVersionException(_schema, _version, actualSchema, actualVersion);
        }
        catch (JsonPersistenceException)
        {
            throw;
        }
        catch (JsonException exception)
        {
            throw new JsonInputLimitException(
                $"JSON is malformed or exceeds the configured depth of {_limits.MaximumJsonDepth}.", exception);
        }
    }

    private void EnsureCurrentVersion(TDocument document)
    {
        if (!StringComparer.Ordinal.Equals(document.Schema, _schema) || document.Version != _version)
            throw new JsonSchemaVersionException(_schema, _version, document.Schema, document.Version);
    }

    private static string GetFullPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Path.GetFullPath(path);
    }

    private static JsonSerializerOptions CreateSerializerOptions(int maximumDepth)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = false,
            WriteIndented = true,
            MaxDepth = maximumDepth,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
        return options;
    }
}
