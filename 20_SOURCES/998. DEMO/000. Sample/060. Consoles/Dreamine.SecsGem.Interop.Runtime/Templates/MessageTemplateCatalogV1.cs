using Dreamine.Secs.Abstractions.Model;
using Dreamine.SecsGem.Interop.Runtime.Persistence;

namespace Dreamine.SecsGem.Interop.Runtime.Templates;

/// <summary>Identifies whether a concrete template is a normal Primary or Secondary message.</summary>
public enum MessageTemplateKind
{
    /// <summary>No kind has been selected.</summary>
    Unspecified,
    /// <summary>A nonzero odd-function Primary.</summary>
    Primary,
    /// <summary>A nonzero even-function Secondary.</summary>
    Secondary
}

/// <summary>Identifies the intended application direction of a concrete template.</summary>
public enum MessageTemplateDirection
{
    /// <summary>No direction has been selected.</summary>
    Unspecified,
    /// <summary>The Host sends the message to Equipment.</summary>
    HostToEquipment,
    /// <summary>The Equipment sends the message to Host.</summary>
    EquipmentToHost
}

/// <summary>Defines template-level body logging metadata.</summary>
public enum TemplateBodyLogPolicy
{
    /// <summary>Records the header and body length without retaining payload bytes or decoded text.</summary>
    HeaderOnly,
    /// <summary>Excludes the body from logging and evidence capture.</summary>
    Excluded,
    /// <summary>Allows full-body capture only as an explicit non-sensitive opt-in.</summary>
    FullBodyExplicit
}

/// <summary>A concrete, single-root SECS-II message template.</summary>
public sealed record MessageTemplateV1
{
    /// <summary>Gets the stable catalog-unique display name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets an optional description.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Gets the stream number from 1 through 127.</summary>
    public byte Stream { get; init; }

    /// <summary>Gets the function number.</summary>
    public byte Function { get; init; }

    /// <summary>Gets the W-bit. It is always false for a Secondary.</summary>
    public bool WaitBit { get; init; }

    /// <summary>Gets Primary or Secondary metadata, validated against function parity.</summary>
    public MessageTemplateKind Kind { get; init; }

    /// <summary>Gets the intended Host/Equipment direction.</summary>
    public MessageTemplateDirection Direction { get; init; }

    /// <summary>Gets fail-safe body logging metadata.</summary>
    public TemplateBodyLogPolicy BodyLogPolicy { get; init; } = TemplateBodyLogPolicy.HeaderOnly;

    /// <summary>
    /// Gets the one permitted root item. Null is an empty body; a non-null empty List is a distinct wire item.
    /// </summary>
    public SecsItemTemplateNode? Root { get; init; }

    /// <summary>Validates metadata, tree shape, concrete values, and production codec limits.</summary>
    public void Validate(MessageTemplateLimits? limits = null)
    {
        limits ??= new MessageTemplateLimits();
        limits.Validate();
        ValidateMetadata();
        if (Root is not null)
        {
            _ = Root.BuildItem(limits);
            ValidateSensitiveLogging();
        }
    }

    private void ValidateMetadata()
    {
        if (string.IsNullOrWhiteSpace(Name) || Name.Length > 256)
            throw new TemplateValidationException("name: a non-whitespace value of at most 256 characters is required.");
        if (Description is null || Description.Length > 4096)
            throw new TemplateValidationException("description: must be non-null and at most 4096 characters.");
        if (Stream is 0 or > 127)
            throw new TemplateValidationException("stream: normal templates require a value from 1 through 127.");
        if (!Enum.IsDefined(Kind) || Kind == MessageTemplateKind.Unspecified)
            throw new TemplateValidationException("kind: Primary or Secondary is required.");
        if (!Enum.IsDefined(Direction) || Direction == MessageTemplateDirection.Unspecified)
            throw new TemplateValidationException("direction: HostToEquipment or EquipmentToHost is required.");
        if (!Enum.IsDefined(BodyLogPolicy))
            throw new TemplateValidationException("bodyLogPolicy: the value is not supported.");

        if (Kind == MessageTemplateKind.Primary)
        {
            if (Function == 0 || (Function & 1) == 0)
                throw new TemplateValidationException("function: a Primary must use a nonzero odd function.");
            if (Function == byte.MaxValue && WaitBit)
                throw new TemplateValidationException("waitBit: F255 cannot set the W-bit.");
        }
        else
        {
            if (Function == 0 || (Function & 1) != 0)
                throw new TemplateValidationException("function: a Secondary must use a nonzero even function.");
            if (WaitBit)
                throw new TemplateValidationException("waitBit: a Secondary must use W=false.");
        }

    }

    /// <summary>Builds a codec-validated body, preserving null body versus an empty List.</summary>
    public SecsItem? BuildItem(MessageTemplateLimits? limits = null)
    {
        limits ??= new MessageTemplateLimits();
        limits.Validate();
        ValidateMetadata();
        if (Root is null) return null;
        var item = Root.BuildItem(limits);
        ValidateSensitiveLogging();
        return item;
    }

    /// <summary>
    /// Builds a message with a caller-provided configured Session ID and System Bytes; neither value is hardcoded.
    /// </summary>
    public SecsMessage BuildMessage(
        SecsSessionId sessionId,
        SecsSystemBytes systemBytes,
        MessageTemplateLimits? limits = null)
    {
        var item = BuildItem(limits);
        return new SecsMessage(sessionId, new SecsStream(Stream), new SecsFunction(Function), WaitBit,
            systemBytes, item);
    }

    /// <summary>Creates a deep clone suitable for independent editing.</summary>
    public MessageTemplateV1 CloneDeep(MessageTemplateLimits? limits = null) =>
        this with { Root = Root?.CloneDeep(limits) };

    /// <summary>Copies a received message into a concrete editor template without retaining its System Bytes.</summary>
    public static MessageTemplateV1 FromReceivedMessage(
        string name,
        MessageTemplateDirection direction,
        SecsMessage message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(message);
        var kind = message.Function.IsPrimary
            ? MessageTemplateKind.Primary
            : message.Function.IsSecondary
                ? MessageTemplateKind.Secondary
                : throw new TemplateValidationException(
                    $"Function {message.Function.Value} is not a normal Primary or Secondary template function.");
        var template = new MessageTemplateV1
        {
            Name = name,
            Description = $"Copied from received S{message.Stream.Value}F{message.Function.Value}.",
            Stream = message.Stream.Value,
            Function = message.Function.Value,
            WaitBit = message.ReplyExpected,
            Kind = kind,
            Direction = direction,
            BodyLogPolicy = TemplateBodyLogPolicy.HeaderOnly,
            Root = message.Item is null ? null : SecsItemTemplateNode.FromSecsItem(message.Item)
        };
        template.Validate();
        return template;
    }

    private void ValidateSensitiveLogging()
    {
        if (Root is not null && Root.ContainsSensitiveNode() &&
            BodyLogPolicy == TemplateBodyLogPolicy.FullBodyExplicit)
            throw new TemplateValidationException(
                "bodyLogPolicy: a template containing a sensitive node must be HeaderOnly or Excluded.");
    }
}

/// <summary>A versioned catalog of concrete, single-root SECS-II message templates.</summary>
public sealed class MessageTemplateCatalogV1 : IVersionedJsonDocument
{
    /// <summary>The exact schema identifier for Message Template Catalog v1.</summary>
    public const string SchemaId = "dreamine.secs.message-template-catalog";

    /// <summary>The current supported schema version.</summary>
    public const int CurrentVersion = 1;

    /// <inheritdoc />
    public string Schema { get; init; } = SchemaId;

    /// <inheritdoc />
    public int Version { get; init; } = CurrentVersion;

    /// <summary>Gets or initializes concrete templates in stable editor order.</summary>
    public List<MessageTemplateV1> Templates { get; init; } = [];

    /// <summary>Validates catalog identity, unique names, metadata, concrete values, and codec limits.</summary>
    public void Validate(MessageTemplateLimits? limits = null)
    {
        if (!StringComparer.Ordinal.Equals(Schema, SchemaId) || Version != CurrentVersion)
            throw new TemplateValidationException(
                $"Only schema '{SchemaId}' version {CurrentVersion} is supported.");
        if (Templates is null) throw new TemplateValidationException("templates: collection is required.");
        if (Templates.Count > 10_000)
            throw new TemplateValidationException("templates: count exceeds the maximum of 10000.");
        if (Templates.Any(static template => template is null))
            throw new TemplateValidationException("templates: null entries are not allowed.");

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < Templates.Count; index++)
        {
            try
            {
                Templates[index].Validate(limits);
            }
            catch (TemplateValidationException exception)
            {
                throw new TemplateValidationException($"templates[{index}].{exception.Message}", exception);
            }
            if (!names.Add(Templates[index].Name))
                throw new TemplateValidationException(
                    $"templates[{index}].name: duplicate name '{Templates[index].Name}'.");
        }
    }

    /// <summary>Runs the same full validation required immediately before sending.</summary>
    public void ValidateForSend(MessageTemplateLimits? limits = null) => Validate(limits);

    /// <summary>Adds a validated, independently cloned template.</summary>
    public void AddTemplate(MessageTemplateV1 template, MessageTemplateLimits? limits = null)
    {
        ArgumentNullException.ThrowIfNull(template);
        template.Validate(limits);
        Templates.Add(template.CloneDeep(limits));
        try
        {
            Validate(limits);
        }
        catch
        {
            Templates.RemoveAt(Templates.Count - 1);
            throw;
        }
    }

    /// <summary>Removes a template at an editor index.</summary>
    public void RemoveTemplateAt(int index) => Templates.RemoveAt(index);

    /// <summary>Moves a template toward index zero.</summary>
    public bool MoveTemplateUp(int index)
    {
        if (index <= 0 || index >= Templates.Count) return false;
        (Templates[index - 1], Templates[index]) = (Templates[index], Templates[index - 1]);
        return true;
    }

    /// <summary>Moves a template away from index zero.</summary>
    public bool MoveTemplateDown(int index)
    {
        if (index < 0 || index >= Templates.Count - 1) return false;
        (Templates[index + 1], Templates[index]) = (Templates[index], Templates[index + 1]);
        return true;
    }

    /// <summary>Deep-clones and inserts one template immediately after its source.</summary>
    public MessageTemplateV1 CloneTemplateAt(int index, string newName, MessageTemplateLimits? limits = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newName);
        var clone = Templates[index].CloneDeep(limits) with { Name = newName };
        clone.Validate(limits);
        Templates.Insert(index + 1, clone);
        try
        {
            Validate(limits);
            return clone;
        }
        catch
        {
            Templates.RemoveAt(index + 1);
            throw;
        }
    }
}

/// <summary>Creates a bounded Message Template Catalog v1 JSON store.</summary>
public static class MessageTemplateCatalogStore
{
    /// <summary>Creates a store with independent JSON and SECS-II item-tree limits.</summary>
    public static VersionedJsonFileStore<MessageTemplateCatalogV1> Create(
        MessageTemplateLimits? templateLimits = null,
        JsonPersistenceLimits? persistenceLimits = null)
    {
        templateLimits ??= new MessageTemplateLimits();
        templateLimits.Validate();
        return new VersionedJsonFileStore<MessageTemplateCatalogV1>(
            MessageTemplateCatalogV1.SchemaId,
            MessageTemplateCatalogV1.CurrentVersion,
            catalog => catalog.Validate(templateLimits),
            persistenceLimits);
    }
}
