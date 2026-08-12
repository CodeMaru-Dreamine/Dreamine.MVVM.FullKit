using System.Globalization;
using System.Text.Json.Serialization;
using Dreamine.Secs.Abstractions.Model;
using Dreamine.Secs.Abstractions.Validation;
using Dreamine.Secs.Com.Codecs;

namespace Dreamine.SecsGem.Interop.Runtime.Templates;

/// <summary>Controls how a legacy editor session with more than one root is imported.</summary>
public enum MultipleRootHandling
{
    /// <summary>Rejects multiple roots with a concrete validation error.</summary>
    Reject,
    /// <summary>Wraps cloned roots in one explicit SECS-II List root.</summary>
    WrapInList
}

/// <summary>Defines defensive limits for an editable concrete SECS-II item tree.</summary>
public sealed record MessageTemplateLimits(
    int MaximumNodeCount = 10_000,
    int MaximumTreeDepth = 64,
    int MaximumEncodedItemBytes = 16 * 1024 * 1024,
    int MaximumListItemCount = 65_535)
{
    /// <summary>Validates the configured limits.</summary>
    public void Validate()
    {
        if (MaximumNodeCount <= 0) throw new ArgumentOutOfRangeException(nameof(MaximumNodeCount));
        if (MaximumTreeDepth is < 0 or > 256)
            throw new ArgumentOutOfRangeException(nameof(MaximumTreeDepth));
        if (MaximumEncodedItemBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(MaximumEncodedItemBytes));
        if (MaximumListItemCount is < 0 or > 0x00ff_ffff)
            throw new ArgumentOutOfRangeException(nameof(MaximumListItemCount));
    }
}

/// <summary>Reports a path-specific concrete template validation error.</summary>
public sealed class TemplateValidationException : IOException
{
    /// <summary>Creates a template validation error.</summary>
    public TemplateValidationException(string message) : base(message) { }

    /// <summary>Creates a template validation error with an underlying codec or conversion error.</summary>
    public TemplateValidationException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Represents one editable concrete SECS-II item. Values use invariant strings so every numeric width,
/// raw JIS-8 byte, and exact editor error can round-trip through versioned JSON.
/// </summary>
public sealed class SecsItemTemplateNode
{
    /// <summary>Creates an empty List node for JSON deserialization.</summary>
    [JsonConstructor]
    public SecsItemTemplateNode() { }

    /// <summary>Creates a node with copied concrete value strings.</summary>
    public SecsItemTemplateNode(SecsItemFormat format, IEnumerable<string>? values = null)
    {
        Format = format;
        if (values is not null) Values.AddRange(values);
    }

    /// <summary>Gets or sets the concrete SECS-II item format.</summary>
    public SecsItemFormat Format { get; set; }

    /// <summary>Gets or sets invariant concrete values. ASCII uses exactly one string.</summary>
    public List<string> Values { get; set; } = [];

    /// <summary>Gets or sets child nodes. Only a List may contain children.</summary>
    public List<SecsItemTemplateNode> Children { get; set; } = [];

    /// <summary>Gets or sets whether this node path contains sensitive decoded data.</summary>
    public bool IsSensitive { get; set; }

    /// <summary>Adds a child to a List node.</summary>
    public void AddChild(SecsItemTemplateNode child)
    {
        EnsureListEditorOperation();
        ArgumentNullException.ThrowIfNull(child);
        if (ReferencesNode(child, this))
            throw new InvalidOperationException("Adding this child would create a cyclic item tree.");
        Children.Add(child);
    }

    /// <summary>Removes a child by identity from a List node.</summary>
    public bool RemoveChild(SecsItemTemplateNode child)
    {
        EnsureListEditorOperation();
        ArgumentNullException.ThrowIfNull(child);
        return Children.Remove(child);
    }

    /// <summary>Moves one List child toward index zero.</summary>
    public bool MoveChildUp(int index)
    {
        EnsureListEditorOperation();
        if (index <= 0 || index >= Children.Count) return false;
        (Children[index - 1], Children[index]) = (Children[index], Children[index - 1]);
        return true;
    }

    /// <summary>Moves one List child away from index zero.</summary>
    public bool MoveChildDown(int index)
    {
        EnsureListEditorOperation();
        if (index < 0 || index >= Children.Count - 1) return false;
        (Children[index + 1], Children[index]) = (Children[index], Children[index + 1]);
        return true;
    }

    /// <summary>Creates a deep editor-safe clone with no shared value or child collections.</summary>
    public SecsItemTemplateNode CloneDeep(MessageTemplateLimits? limits = null)
    {
        limits ??= new MessageTemplateLimits();
        limits.Validate();
        var path = new HashSet<SecsItemTemplateNode>(ReferenceEqualityComparer.Instance);
        var nodeCount = 0;
        return CloneDeepCore(limits, path, 0, ref nodeCount);
    }

    /// <summary>Builds and codec-validates the concrete item under configured safety limits.</summary>
    public SecsItem BuildItem(MessageTemplateLimits? limits = null)
    {
        limits ??= new MessageTemplateLimits();
        limits.Validate();
        var nodeCount = 0;
        var item = BuildItemCore(limits, "root", 0, ref nodeCount);
        try
        {
            var codec = new SecsItemCodec(new SecsItemCodecOptions
            {
                MaximumMessageLength = limits.MaximumEncodedItemBytes,
                MaximumNestingDepth = limits.MaximumTreeDepth,
                MaximumListItemCount = limits.MaximumListItemCount
            });
            var encoded = codec.Encode(item);
            _ = codec.Decode(encoded);
        }
        catch (Exception exception) when (exception is ArgumentException or OverflowException or SecsProtocolException)
        {
            throw new TemplateValidationException("root: the item failed SECS-II codec validation.", exception);
        }
        return item;
    }

    /// <summary>Copies an immutable received SECS-II item into an editable template tree.</summary>
    public static SecsItemTemplateNode FromSecsItem(SecsItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        var limits = new MessageTemplateLimits();
        var nodeCount = 0;
        return FromSecsItemCore(item, limits, 0, ref nodeCount);
    }

    private static SecsItemTemplateNode FromSecsItemCore(
        SecsItem item,
        MessageTemplateLimits limits,
        int depth,
        ref int nodeCount)
    {
        if (++nodeCount > limits.MaximumNodeCount)
            throw new TemplateValidationException(
                $"Received item node count exceeds the maximum of {limits.MaximumNodeCount}.");
        if (depth > limits.MaximumTreeDepth)
            throw new TemplateValidationException(
                $"Received item depth exceeds the maximum of {limits.MaximumTreeDepth}.");
        var node = item switch
        {
            SecsListItem value => FromList(value, limits, depth, ref nodeCount),
            SecsBinaryItem value => new SecsItemTemplateNode(SecsItemFormat.Binary,
                value.Values.Span.ToArray().Select(static element => $"0x{element:X2}")),
            SecsBooleanItem value => new SecsItemTemplateNode(SecsItemFormat.Boolean,
                value.Values.Span.ToArray().Select(static element => element ? "true" : "false")),
            SecsAsciiItem value => new SecsItemTemplateNode(SecsItemFormat.Ascii, [value.Value]),
            SecsJis8Item value => new SecsItemTemplateNode(SecsItemFormat.Jis8,
                value.Values.Span.ToArray().Select(static element => $"0x{element:X2}")),
            SecsInt8Item value => NumericNode(SecsItemFormat.Int8, value.Values.Span.ToArray()),
            SecsInt16Item value => NumericNode(SecsItemFormat.Int16, value.Values.Span.ToArray()),
            SecsInt32Item value => NumericNode(SecsItemFormat.Int32, value.Values.Span.ToArray()),
            SecsInt64Item value => NumericNode(SecsItemFormat.Int64, value.Values.Span.ToArray()),
            SecsUInt8Item value => NumericNode(SecsItemFormat.UInt8, value.Values.Span.ToArray()),
            SecsUInt16Item value => NumericNode(SecsItemFormat.UInt16, value.Values.Span.ToArray()),
            SecsUInt32Item value => NumericNode(SecsItemFormat.UInt32, value.Values.Span.ToArray()),
            SecsUInt64Item value => NumericNode(SecsItemFormat.UInt64, value.Values.Span.ToArray()),
            SecsFloat32Item value => new SecsItemTemplateNode(SecsItemFormat.Float32,
                value.Values.Span.ToArray().Select(static element => element.ToString("R", CultureInfo.InvariantCulture))),
            SecsFloat64Item value => new SecsItemTemplateNode(SecsItemFormat.Float64,
                value.Values.Span.ToArray().Select(static element => element.ToString("R", CultureInfo.InvariantCulture))),
            _ => throw new TemplateValidationException($"Unsupported item type '{item.GetType().FullName}'.")
        };
        return node;
    }

    /// <summary>
    /// Imports roots from the former multi-root editor. Zero roots remain an empty body, one is cloned,
    /// and multiple roots require an explicit reject-or-wrap decision.
    /// </summary>
    public static SecsItemTemplateNode? ImportLegacyRoots(
        IEnumerable<SecsItemTemplateNode> roots,
        MultipleRootHandling handling)
    {
        ArgumentNullException.ThrowIfNull(roots);
        if (!Enum.IsDefined(handling)) throw new ArgumentOutOfRangeException(nameof(handling));
        var materialized = new List<SecsItemTemplateNode>();
        foreach (var root in roots)
        {
            if (root is null) throw new TemplateValidationException("Legacy roots cannot contain null.");
            if (materialized.Count == 10_000)
                throw new TemplateValidationException("Legacy root count exceeds the maximum of 10000.");
            materialized.Add(root);
        }
        if (materialized.Count == 0) return null;
        var limits = new MessageTemplateLimits();
        var path = new HashSet<SecsItemTemplateNode>(ReferenceEqualityComparer.Instance);
        var nodeCount = 0;
        if (materialized.Count == 1)
            return materialized[0].CloneDeepCore(limits, path, 0, ref nodeCount);
        if (handling == MultipleRootHandling.Reject)
            throw new TemplateValidationException(
                $"The legacy editor contains {materialized.Count} roots. Choose WrapInList explicitly to preserve wire order.");
        var list = new SecsItemTemplateNode(SecsItemFormat.List);
        foreach (var root in materialized)
            list.Children.Add(root.CloneDeepCore(limits, path, 1, ref nodeCount));
        return list;
    }

    internal bool ContainsSensitiveNode() => IsSensitive || Children.Any(static child => child.ContainsSensitiveNode());

    private SecsItem BuildItemCore(MessageTemplateLimits limits, string path, int depth, ref int nodeCount)
    {
        if (++nodeCount > limits.MaximumNodeCount)
            throw new TemplateValidationException(
                $"{path}: node count exceeds the maximum of {limits.MaximumNodeCount}.");
        if (depth > limits.MaximumTreeDepth)
            throw new TemplateValidationException(
                $"{path}: tree depth exceeds the maximum of {limits.MaximumTreeDepth}.");
        if (!Enum.IsDefined(Format))
            throw new TemplateValidationException($"{path}.format: unsupported item format value {(byte)Format}.");
        if (Values is null) throw new TemplateValidationException($"{path}.values: collection is required.");
        if (Children is null) throw new TemplateValidationException($"{path}.children: collection is required.");
        if (Values.Any(static value => value is null))
            throw new TemplateValidationException($"{path}.values: null values are not allowed.");
        if (Children.Any(static child => child is null))
            throw new TemplateValidationException($"{path}.children: null nodes are not allowed.");

        if (Format == SecsItemFormat.List)
        {
            if (Values.Count != 0)
                throw new TemplateValidationException($"{path}.values: a List cannot contain atomic values.");
            if (Children.Count > limits.MaximumListItemCount)
                throw new TemplateValidationException(
                    $"{path}.children: count exceeds the maximum of {limits.MaximumListItemCount}.");
            var items = new SecsItem[Children.Count];
            for (var index = 0; index < items.Length; index++)
                items[index] = Children[index].BuildItemCore(limits, $"{path}.children[{index}]", depth + 1,
                    ref nodeCount);
            return new SecsListItem(items);
        }

        if (Children.Count != 0)
            throw new TemplateValidationException($"{path}.children: only a List may contain children.");
        try
        {
            return Format switch
            {
                SecsItemFormat.Binary => new SecsBinaryItem(ParseBytes(path)),
                SecsItemFormat.Boolean => new SecsBooleanItem(ParseBooleans(path)),
                SecsItemFormat.Ascii => BuildAscii(path),
                SecsItemFormat.Jis8 => new SecsJis8Item(ParseBytes(path)),
                SecsItemFormat.Int8 => new SecsInt8Item(ParseValues(path, sbyte.Parse)),
                SecsItemFormat.Int16 => new SecsInt16Item(ParseValues(path, short.Parse)),
                SecsItemFormat.Int32 => new SecsInt32Item(ParseValues(path, int.Parse)),
                SecsItemFormat.Int64 => new SecsInt64Item(ParseValues(path, long.Parse)),
                SecsItemFormat.UInt8 => new SecsUInt8Item(ParseValues(path, byte.Parse)),
                SecsItemFormat.UInt16 => new SecsUInt16Item(ParseValues(path, ushort.Parse)),
                SecsItemFormat.UInt32 => new SecsUInt32Item(ParseValues(path, uint.Parse)),
                SecsItemFormat.UInt64 => new SecsUInt64Item(ParseValues(path, ulong.Parse)),
                SecsItemFormat.Float32 => new SecsFloat32Item(ParseValues(path, float.Parse, NumberStyles.Float)),
                SecsItemFormat.Float64 => new SecsFloat64Item(ParseValues(path, double.Parse, NumberStyles.Float)),
                _ => throw new TemplateValidationException($"{path}.format: unsupported item format '{Format}'.")
            };
        }
        catch (TemplateValidationException)
        {
            throw;
        }
        catch (Exception exception) when (exception is ArgumentException or OverflowException or FormatException)
        {
            throw new TemplateValidationException($"{path}: item construction failed.", exception);
        }
    }

    private SecsAsciiItem BuildAscii(string path)
    {
        if (Values.Count != 1)
            throw new TemplateValidationException($"{path}.values: ASCII requires exactly one string value.");
        try
        {
            return new SecsAsciiItem(Values[0]);
        }
        catch (ArgumentException exception)
        {
            throw new TemplateValidationException($"{path}.values[0]: value must contain ASCII characters only.", exception);
        }
    }

    private byte[] ParseBytes(string path)
    {
        var result = new byte[Values.Count];
        for (var index = 0; index < result.Length; index++)
        {
            var value = Values[index];
            var hexadecimal = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase);
            if (!byte.TryParse(hexadecimal ? value[2..] : value,
                    hexadecimal ? NumberStyles.AllowHexSpecifier : NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out result[index]))
                throw new TemplateValidationException(
                    $"{path}.values[{index}]: '{value}' is not a byte in decimal or 0x00..0xFF form.");
        }
        return result;
    }

    private bool[] ParseBooleans(string path)
    {
        var result = new bool[Values.Count];
        for (var index = 0; index < result.Length; index++)
        {
            if (!bool.TryParse(Values[index], out result[index]))
                throw new TemplateValidationException(
                    $"{path}.values[{index}]: '{Values[index]}' is not true or false.");
        }
        return result;
    }

    private T[] ParseValues<T>(string path, Func<string, NumberStyles, IFormatProvider, T> parse,
        NumberStyles styles = NumberStyles.Integer)
    {
        var result = new T[Values.Count];
        for (var index = 0; index < result.Length; index++)
        {
            try
            {
                result[index] = parse(Values[index], styles, CultureInfo.InvariantCulture);
            }
            catch (Exception exception) when (exception is FormatException or OverflowException)
            {
                throw new TemplateValidationException(
                    $"{path}.values[{index}]: '{Values[index]}' is invalid or outside the range for {Format}.",
                    exception);
            }
        }
        return result;
    }

    private void EnsureListEditorOperation()
    {
        if (Format != SecsItemFormat.List)
            throw new InvalidOperationException("Child editing operations are available only on a List node.");
        if (Children is null) throw new TemplateValidationException("children: collection is required.");
    }

    private SecsItemTemplateNode CloneDeepCore(
        MessageTemplateLimits limits,
        ISet<SecsItemTemplateNode> path,
        int depth,
        ref int nodeCount)
    {
        if (++nodeCount > limits.MaximumNodeCount)
            throw new TemplateValidationException(
                $"Clone node count exceeds the maximum of {limits.MaximumNodeCount}.");
        if (depth > limits.MaximumTreeDepth)
            throw new TemplateValidationException(
                $"Clone depth exceeds the maximum of {limits.MaximumTreeDepth}.");
        if (Values is null) throw new TemplateValidationException("Clone source values collection is required.");
        if (Children is null) throw new TemplateValidationException("Clone source children collection is required.");
        if (!path.Add(this)) throw new TemplateValidationException("The item tree contains a cycle.");
        try
        {
            var clone = new SecsItemTemplateNode(Format, Values) { IsSensitive = IsSensitive };
            foreach (var child in Children)
            {
                if (child is null) throw new TemplateValidationException("Clone source contains a null child.");
                clone.Children.Add(child.CloneDeepCore(limits, path, depth + 1, ref nodeCount));
            }
            return clone;
        }
        finally
        {
            path.Remove(this);
        }
    }

    private static bool ReferencesNode(SecsItemTemplateNode candidate, SecsItemTemplateNode target)
    {
        var pending = new Stack<SecsItemTemplateNode>();
        var visited = new HashSet<SecsItemTemplateNode>(ReferenceEqualityComparer.Instance);
        pending.Push(candidate);
        while (pending.TryPop(out var node))
        {
            if (ReferenceEquals(node, target)) return true;
            if (!visited.Add(node) || node.Children is null) continue;
            foreach (var child in node.Children)
                if (child is not null) pending.Push(child);
        }
        return false;
    }

    private static SecsItemTemplateNode FromList(
        SecsListItem value,
        MessageTemplateLimits limits,
        int depth,
        ref int nodeCount)
    {
        var node = new SecsItemTemplateNode(SecsItemFormat.List);
        foreach (var item in value.Items)
            node.Children.Add(FromSecsItemCore(item, limits, depth + 1, ref nodeCount));
        return node;
    }

    private static SecsItemTemplateNode NumericNode<T>(SecsItemFormat format, IEnumerable<T> values)
        where T : IFormattable => new(format,
        values.Select(static value => value.ToString(null, CultureInfo.InvariantCulture)));
}
