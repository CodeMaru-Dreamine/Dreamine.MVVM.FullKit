using System.Collections.ObjectModel;
using System.Globalization;
using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;
using Dreamine.Secs.Abstractions.Model;
using Dreamine.SecsGem.Interop.Wpf.Models;

namespace Dreamine.SecsGem.Interop.Wpf.ViewModels;

public sealed partial class SecsItemNodeViewModel : ViewModelBase
{
    [DreamineProperty]
    private SecsItemEditorFormat _format;
    [DreamineProperty]
    private string _value = string.Empty;
    public SecsItemNodeViewModel(SecsItemEditorFormat format = SecsItemEditorFormat.List) => _format = format;
    public IReadOnlyList<SecsItemEditorFormat> Formats { get; } = Enum.GetValues<SecsItemEditorFormat>();
    public ObservableCollection<SecsItemNodeViewModel> Children { get; } = new();

    public SecsItem ToSecsItem()
    {
        var values = Value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return Format switch
        {
            SecsItemEditorFormat.List => new SecsListItem(Children.Select(child => child.ToSecsItem()).ToArray()),
            SecsItemEditorFormat.Ascii => new SecsAsciiItem(Value),
            SecsItemEditorFormat.Binary => new SecsBinaryItem(values.Select(ParseByte).ToArray()),
            SecsItemEditorFormat.Boolean => new SecsBooleanItem(values.Select(bool.Parse).ToArray()),
            SecsItemEditorFormat.Int8 => new SecsInt8Item(values.Select(value => sbyte.Parse(value, CultureInfo.InvariantCulture)).ToArray()),
            SecsItemEditorFormat.Int16 => new SecsInt16Item(values.Select(value => short.Parse(value, CultureInfo.InvariantCulture)).ToArray()),
            SecsItemEditorFormat.Int32 => new SecsInt32Item(values.Select(value => int.Parse(value, CultureInfo.InvariantCulture)).ToArray()),
            SecsItemEditorFormat.Int64 => new SecsInt64Item(values.Select(value => long.Parse(value, CultureInfo.InvariantCulture)).ToArray()),
            SecsItemEditorFormat.UInt8 => new SecsUInt8Item(values.Select(value => byte.Parse(value, CultureInfo.InvariantCulture)).ToArray()),
            SecsItemEditorFormat.UInt16 => new SecsUInt16Item(values.Select(value => ushort.Parse(value, CultureInfo.InvariantCulture)).ToArray()),
            SecsItemEditorFormat.UInt32 => new SecsUInt32Item(values.Select(value => uint.Parse(value, CultureInfo.InvariantCulture)).ToArray()),
            SecsItemEditorFormat.UInt64 => new SecsUInt64Item(values.Select(value => ulong.Parse(value, CultureInfo.InvariantCulture)).ToArray()),
            SecsItemEditorFormat.Float32 => new SecsFloat32Item(values.Select(value => float.Parse(value, CultureInfo.InvariantCulture)).ToArray()),
            SecsItemEditorFormat.Float64 => new SecsFloat64Item(values.Select(value => double.Parse(value, CultureInfo.InvariantCulture)).ToArray()),
            _ => throw new NotSupportedException($"Unsupported editor format: {Format}")
        };
    }

    private static byte ParseByte(string value) => value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
        ? byte.Parse(value.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture)
        : byte.Parse(value, CultureInfo.InvariantCulture);
}
