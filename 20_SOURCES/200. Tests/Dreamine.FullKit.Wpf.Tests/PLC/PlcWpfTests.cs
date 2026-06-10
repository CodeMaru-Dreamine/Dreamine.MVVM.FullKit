using System.Globalization;
using System.Windows.Media;
using Dreamine.PLC.Abstractions.Connections;
using Dreamine.PLC.Wpf.Commands;
using Dreamine.PLC.Wpf.Converters;
using Dreamine.PLC.Wpf.Models;

namespace Dreamine.FullKit.Wpf.Tests.PLC;

public sealed class PlcWpfTests
{
    [Fact]
    public void RelayCommand_ExecutesOnlyWhenAllowed()
    {
        var count = 0;
        var command = new RelayCommand(() => count++, () => false);

        command.Execute(null);

        Assert.False(command.CanExecute(null));
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task AsyncRelayCommand_PreventsReentryWhileExecuting()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var command = new AsyncRelayCommand(async () =>
        {
            entered.SetResult();
            await release.Task;
        });

        command.Execute(null);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.False(command.CanExecute(null));

        release.SetResult();
        await Task.Delay(50);

        Assert.True(command.CanExecute(null));
    }

    [Fact]
    public void PlcConnectionStateBrushConverter_MapsStatesToBrushes()
    {
        var converter = new PlcConnectionStateBrushConverter();

        Assert.Same(Brushes.ForestGreen, converter.Convert(PlcConnectionState.Connected, typeof(Brush), null!, CultureInfo.InvariantCulture));
        Assert.Same(Brushes.DarkOrange, converter.Convert(PlcConnectionState.Connecting, typeof(Brush), null!, CultureInfo.InvariantCulture));
        Assert.Same(Brushes.Firebrick, converter.Convert(PlcConnectionState.Faulted, typeof(Brush), null!, CultureInfo.InvariantCulture));
        Assert.Same(Brushes.Gray, converter.Convert(PlcConnectionState.Disconnected, typeof(Brush), null!, CultureInfo.InvariantCulture));
        Assert.Throws<NotSupportedException>(() => converter.ConvertBack(Brushes.Gray, typeof(PlcConnectionState), null!, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void PlcOperationLogItem_StoresValues()
    {
        var item = new PlcOperationLogItem
        {
            Operation = "Read",
            Address = "D100",
            Values = "1,2",
            IsSuccess = true,
            Message = "OK"
        };

        Assert.Equal("Read", item.Operation);
        Assert.Equal("D100", item.Address);
        Assert.True(item.IsSuccess);
    }
}
