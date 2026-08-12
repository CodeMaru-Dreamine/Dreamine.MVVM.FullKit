using System.Text.Json.Serialization;
using Dreamine.Secs.Abstractions.Model;
using Dreamine.SecsGem.Interop.Runtime.Persistence;
using Dreamine.SecsGem.Interop.Runtime.Templates;

namespace Dreamine.SecsGem.Interop.Runtime.Scenarios;

/// <summary>Defines the defensive limits of the concrete scenario v1 format.</summary>
public static class ScenarioLimitsV1
{
    public const int MaximumFileSizeBytes = 1024 * 1024;
    public const int MaximumJsonDepth = 64;
    public const int MaximumJsonNodes = 100_000;
    public const int MaximumDefinedSteps = 1_024;
    public const int MaximumExpandedSteps = 10_000;
    public const int MaximumRepeatCount = 1_000;
    public const int MaximumRepeatDepth = 8;
    public const int MaximumItemNodes = 10_000;
    public const int MaximumItemDepth = 64;
    public const int MaximumAtomicValues = 65_535;
    public const int MaximumTextCharacters = 1024 * 1024;
    public const int MaximumMessageBodyBytes = 16 * 1024 * 1024;
    public const int MaximumStepTimeoutMilliseconds = 5 * 60 * 1_000;
    public const int MaximumRunTimeoutMilliseconds = 60 * 60 * 1_000;
    public const int MaximumInboundQueueCapacity = 4_096;
}

/// <summary>A concrete, versioned scenario document shared by WPF and headless callers.</summary>
public sealed class ScenarioDefinitionV1 : IVersionedJsonDocument
{
    public const string SchemaName = "dreamine.secs-gem.scenario";
    public const int CurrentSchemaVersion = 1;

    public string Schema { get; init; } = SchemaName;
    public int Version { get; init; } = CurrentSchemaVersion;
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public ScenarioExecutionBindingV1 Binding { get; init; } = ScenarioExecutionBindingV1.CurrentConnection();
    public int RunTimeoutMilliseconds { get; init; } = 60_000;
    public List<ScenarioStepV1> Steps { get; init; } = [];

    /// <summary>Validates schema, execution bounds, targets, dialogues, and message templates.</summary>
    public void Validate()
    {
        if (!StringComparer.Ordinal.Equals(Schema, SchemaName) || Version != CurrentSchemaVersion)
            throw new JsonSchemaVersionException(SchemaName, CurrentSchemaVersion, Schema, Version);
        ValidateIdentifier(Id, nameof(Id));
        if (string.IsNullOrWhiteSpace(Name) || Name.Length > 256)
            throw new ArgumentException("Scenario name must contain 1 through 256 characters.", nameof(Name));
        ArgumentNullException.ThrowIfNull(Binding);
        Binding.Validate();
        if (RunTimeoutMilliseconds is < 1 or > ScenarioLimitsV1.MaximumRunTimeoutMilliseconds)
            throw new ArgumentOutOfRangeException(nameof(RunTimeoutMilliseconds));
        ArgumentNullException.ThrowIfNull(Steps);
        if (Steps.Count is < 1 or > ScenarioLimitsV1.MaximumDefinedSteps)
            throw new ArgumentOutOfRangeException(nameof(Steps));

        var defined = 0;
        long expanded = 0;
        ValidateSteps(Steps, Binding.Target, depth: 0, ref defined, ref expanded);
        if (defined > ScenarioLimitsV1.MaximumDefinedSteps)
            throw new ArgumentOutOfRangeException(nameof(Steps), $"A scenario can define at most {ScenarioLimitsV1.MaximumDefinedSteps} steps.");
        if (expanded > ScenarioLimitsV1.MaximumExpandedSteps)
            throw new ArgumentOutOfRangeException(nameof(Steps), $"A scenario can execute at most {ScenarioLimitsV1.MaximumExpandedSteps} expanded steps.");
    }

    private static void ValidateSteps(
        IReadOnlyList<ScenarioStepV1> steps,
        string target,
        int depth,
        ref int defined,
        ref long expanded)
    {
        if (depth > ScenarioLimitsV1.MaximumRepeatDepth)
            throw new ArgumentOutOfRangeException(nameof(steps), $"Repeat nesting cannot exceed {ScenarioLimitsV1.MaximumRepeatDepth}.");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var step in steps)
        {
            ArgumentNullException.ThrowIfNull(step);
            defined = checked(defined + 1);
            ValidateIdentifier(step.Id, nameof(step.Id));
            if (!ids.Add(step.Id)) throw new ArgumentException($"Sibling step ID '{step.Id}' is duplicated.", nameof(steps));
            if (!StringComparer.Ordinal.Equals(step.Target, target))
                throw new ArgumentException($"Step '{step.Id}' targets '{step.Target}', but the scenario is bound to '{target}'.", nameof(steps));
            if (step.TimeoutMilliseconds is < 1 or > ScenarioLimitsV1.MaximumStepTimeoutMilliseconds)
                throw new ArgumentOutOfRangeException(nameof(step.TimeoutMilliseconds));

            switch (step)
            {
                case ConnectScenarioStepV1 or SelectScenarioStepV1 or LinktestScenarioStepV1 or
                    SeparateScenarioStepV1 or DisconnectScenarioStepV1:
                    expanded = checked(expanded + 1);
                    break;
                case WaitForStateScenarioStepV1 wait:
                    if (!Enum.IsDefined(wait.State)) throw new ArgumentOutOfRangeException(nameof(wait.State));
                    expanded = checked(expanded + 1);
                    break;
                case SendScenarioStepV1 send:
                    _ = send.CreateDialogue();
                    if (send.Body is not null) ValidateItem(send.Body);
                    expanded = checked(expanded + 1);
                    break;
                case ExpectScenarioStepV1 expect:
                    if (!Enum.IsDefined(expect.Source)) throw new ArgumentOutOfRangeException(nameof(expect.Source));
                    ArgumentNullException.ThrowIfNull(expect.Matcher);
                    expect.Matcher.Validate();
                    expanded = checked(expanded + 1);
                    break;
                case DelayScenarioStepV1 delay:
                    if (delay.DelayMilliseconds is < 0 or > ScenarioLimitsV1.MaximumStepTimeoutMilliseconds)
                        throw new ArgumentOutOfRangeException(nameof(delay.DelayMilliseconds));
                    expanded = checked(expanded + 1);
                    break;
                case RepeatScenarioStepV1 repeat:
                    if (repeat.Count is < 1 or > ScenarioLimitsV1.MaximumRepeatCount)
                        throw new ArgumentOutOfRangeException(nameof(repeat.Count));
                    ArgumentNullException.ThrowIfNull(repeat.Steps);
                    if (repeat.Steps.Count is < 1 or > ScenarioLimitsV1.MaximumDefinedSteps)
                        throw new ArgumentOutOfRangeException(nameof(repeat.Steps));
                    long nestedExpanded = 0;
                    ValidateSteps(repeat.Steps, target, depth + 1, ref defined, ref nestedExpanded);
                    // Count the repeat step itself plus every nested step invocation.
                    expanded = checked(expanded + 1 + (nestedExpanded * repeat.Count));
                    break;
                default:
                    throw new NotSupportedException($"Scenario step type '{step.GetType().Name}' is not part of scenario v1.");
            }

            if (defined > ScenarioLimitsV1.MaximumDefinedSteps || expanded > ScenarioLimitsV1.MaximumExpandedSteps)
                throw new ArgumentOutOfRangeException(nameof(steps), "Scenario step bounds were exceeded.");
        }
    }

    internal static void ValidateItem(SecsItemTemplateNode root)
    {
        var nodes = 0;
        var values = 0;
        var characters = 0;
        var path = new HashSet<SecsItemTemplateNode>(ReferenceEqualityComparer.Instance);
        ValidateItemNode(root, 0, path, ref nodes, ref values, ref characters);
        var item = root.BuildItem();
        if (item.BodyLength > ScenarioLimitsV1.MaximumMessageBodyBytes)
            throw new ArgumentOutOfRangeException(nameof(root), "The encoded message body exceeds the scenario v1 limit.");
    }

    private static void ValidateItemNode(
        SecsItemTemplateNode node,
        int depth,
        ISet<SecsItemTemplateNode> path,
        ref int nodes,
        ref int values,
        ref int characters)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (!path.Add(node)) throw new ArgumentException("The item tree contains a cycle.", nameof(node));
        try
        {
            if (depth > ScenarioLimitsV1.MaximumItemDepth)
                throw new ArgumentOutOfRangeException(nameof(node), "The item tree is too deep.");
            nodes = checked(nodes + 1);
            if (nodes > ScenarioLimitsV1.MaximumItemNodes)
                throw new ArgumentOutOfRangeException(nameof(node), "The item tree has too many nodes.");
            ArgumentNullException.ThrowIfNull(node.Values);
            ArgumentNullException.ThrowIfNull(node.Children);
            values = checked(values + node.Values.Count);
            if (values > ScenarioLimitsV1.MaximumAtomicValues)
                throw new ArgumentOutOfRangeException(nameof(node), "The item tree has too many atomic values.");
            foreach (var value in node.Values)
            {
                ArgumentNullException.ThrowIfNull(value);
                characters = checked(characters + value.Length);
                if (characters > ScenarioLimitsV1.MaximumTextCharacters)
                    throw new ArgumentOutOfRangeException(nameof(node), "The item tree contains too much text.");
            }
            foreach (var child in node.Children)
                ValidateItemNode(child, depth + 1, path, ref nodes, ref values, ref characters);
        }
        finally
        {
            path.Remove(node);
        }
    }

    private static void ValidateIdentifier(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 128 || value.IndexOfAny(['/', '[', ']']) >= 0)
            throw new ArgumentException("Identifiers must contain 1 through 128 characters and cannot contain '/', '[' or ']'.", parameterName);
    }
}

public enum ScenarioBindingKindV1 { CurrentConnection, NamedEquipment }

/// <summary>Identifies the one session to which a scenario v1 run is bound.</summary>
public sealed class ScenarioExecutionBindingV1
{
    public const string CurrentConnectionTarget = "$current";
    public ScenarioBindingKindV1 Kind { get; init; } = ScenarioBindingKindV1.CurrentConnection;
    public string Target { get; init; } = CurrentConnectionTarget;

    public static ScenarioExecutionBindingV1 CurrentConnection() => new();
    public static ScenarioExecutionBindingV1 NamedEquipment(string equipmentName) =>
        new() { Kind = ScenarioBindingKindV1.NamedEquipment, Target = equipmentName };

    internal void Validate()
    {
        if (!Enum.IsDefined(Kind)) throw new ArgumentOutOfRangeException(nameof(Kind));
        if (Kind == ScenarioBindingKindV1.CurrentConnection)
        {
            if (!StringComparer.Ordinal.Equals(Target, CurrentConnectionTarget))
                throw new ArgumentException($"The current-connection target must be '{CurrentConnectionTarget}'.", nameof(Target));
            return;
        }
        if (string.IsNullOrWhiteSpace(Target) || Target.Length > 128 || Target == CurrentConnectionTarget)
            throw new ArgumentException("A named-equipment binding requires a distinct name of at most 128 characters.", nameof(Target));
    }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(ConnectScenarioStepV1), "connect")]
[JsonDerivedType(typeof(WaitForStateScenarioStepV1), "waitForState")]
[JsonDerivedType(typeof(SelectScenarioStepV1), "select")]
[JsonDerivedType(typeof(LinktestScenarioStepV1), "linktest")]
[JsonDerivedType(typeof(SendScenarioStepV1), "send")]
[JsonDerivedType(typeof(ExpectScenarioStepV1), "expect")]
[JsonDerivedType(typeof(DelayScenarioStepV1), "delay")]
[JsonDerivedType(typeof(SeparateScenarioStepV1), "separate")]
[JsonDerivedType(typeof(DisconnectScenarioStepV1), "disconnect")]
[JsonDerivedType(typeof(RepeatScenarioStepV1), "repeat")]
public abstract class ScenarioStepV1
{
    public string Id { get; init; } = string.Empty;
    public string Target { get; init; } = string.Empty;
    public int TimeoutMilliseconds { get; init; } = 10_000;
}

public sealed class ConnectScenarioStepV1 : ScenarioStepV1;
public sealed class SelectScenarioStepV1 : ScenarioStepV1;
public sealed class LinktestScenarioStepV1 : ScenarioStepV1;
public sealed class SeparateScenarioStepV1 : ScenarioStepV1;
public sealed class DisconnectScenarioStepV1 : ScenarioStepV1;

public enum ScenarioWaitStateV1 { Connected, Selected }

public sealed class WaitForStateScenarioStepV1 : ScenarioStepV1
{
    public ScenarioWaitStateV1 State { get; init; }
}

public sealed class SendScenarioStepV1 : ScenarioStepV1
{
    public byte Stream { get; init; }
    public byte PrimaryFunction { get; init; }
    public byte? SecondaryFunction { get; init; }
    public SecsItemTemplateNode? Body { get; init; }

    internal SecsDialogueDefinition CreateDialogue() => new(
        new SecsStream(Stream),
        new SecsFunction(PrimaryFunction),
        SecondaryFunction is { } secondary ? new SecsFunction(secondary) : null);
}

public enum ScenarioMessageSourceV1 { LastReply, NextMessage }
public enum ScenarioCorrelationV1 { Ignore, LastSent, Exact }
public enum ScenarioBodyMatchV1 { Ignore, Exact, Structural, Absent }

public sealed class ScenarioMessageMatcherV1
{
    public ushort? SessionId { get; init; }
    public byte? Stream { get; init; }
    public byte? Function { get; init; }
    public bool? ReplyExpected { get; init; }
    public ScenarioCorrelationV1 Correlation { get; init; }
    public uint? SystemBytes { get; init; }
    public ScenarioBodyMatchV1 BodyMatch { get; init; }
    public SecsItemTemplateNode? Body { get; init; }

    internal void Validate()
    {
        if (SessionId is > SecsSessionId.MaximumValue) throw new ArgumentOutOfRangeException(nameof(SessionId));
        if (Stream is > 127) throw new ArgumentOutOfRangeException(nameof(Stream));
        if (!Enum.IsDefined(Correlation)) throw new ArgumentOutOfRangeException(nameof(Correlation));
        if (!Enum.IsDefined(BodyMatch)) throw new ArgumentOutOfRangeException(nameof(BodyMatch));
        if (Correlation == ScenarioCorrelationV1.Exact && SystemBytes is null)
            throw new ArgumentException("Exact correlation requires SystemBytes.", nameof(SystemBytes));
        if (Correlation != ScenarioCorrelationV1.Exact && SystemBytes is not null)
            throw new ArgumentException("SystemBytes is used only by exact correlation.", nameof(SystemBytes));
        if (BodyMatch is ScenarioBodyMatchV1.Exact or ScenarioBodyMatchV1.Structural)
        {
            ArgumentNullException.ThrowIfNull(Body);
            ScenarioDefinitionV1.ValidateItem(Body);
        }
        else if (Body is not null)
        {
            throw new ArgumentException("Ignored or absent body matching cannot contain a body template.", nameof(Body));
        }
        if (SessionId is null && Stream is null && Function is null && ReplyExpected is null &&
            Correlation == ScenarioCorrelationV1.Ignore && BodyMatch == ScenarioBodyMatchV1.Ignore)
            throw new ArgumentException("A message matcher must assert at least one field.");
    }
}

public sealed class ExpectScenarioStepV1 : ScenarioStepV1
{
    public ScenarioMessageSourceV1 Source { get; init; }
    public ScenarioMessageMatcherV1 Matcher { get; init; } = new();
}

public sealed class DelayScenarioStepV1 : ScenarioStepV1
{
    public int DelayMilliseconds { get; init; }
}

public sealed class RepeatScenarioStepV1 : ScenarioStepV1
{
    public int Count { get; init; }
    public List<ScenarioStepV1> Steps { get; init; } = [];
}

public enum ScenarioRunStatusV1 { Passed, Invalid, Failed, TimedOut, Cancelled }
public enum ScenarioStepStatusV1 { Passed, Failed, TimedOut, Cancelled }

/// <summary>Maps a structured scenario outcome to a deterministic headless process exit code.</summary>
public static class ScenarioExitCodesV1
{
    public const int Passed = 0;
    public const int Failed = 1;
    public const int Invalid = 2;
    public const int TimedOut = 3;
    public const int Cancelled = 130;
}

public sealed record ScenarioStepResultV1(
    string Path,
    ScenarioStepStatusV1 Status,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    string? ErrorCode = null,
    string? ErrorMessage = null);

public sealed record ScenarioRunResultV1(
    ScenarioRunStatusV1 Status,
    int ExitCode,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    IReadOnlyList<ScenarioStepResultV1> Steps,
    string? ErrorCode = null,
    string? ErrorMessage = null,
    long DroppedInboundMessageCount = 0);
