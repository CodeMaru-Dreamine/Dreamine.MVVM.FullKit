using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Dreamine.MVVM.ViewModels;
using Dreamine.PLC.Abstractions.Connections;
using Dreamine.PLC.Abstractions.Devices;
using Dreamine.PLC.Wpf.Commands;
using Dreamine.PLC.Wpf.Converters;
using Dreamine.PLC.Wpf.Models;
using Dreamine.PLC.Wpf.Services;

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
    public async Task AsyncRelayCommand_CapturesExecutionException()
    {
        var failed = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        var command = new AsyncRelayCommand(() => throw new InvalidOperationException("boom"));
        command.ExecutionFailed += (_, ex) => failed.SetResult(ex);

        command.Execute(null);
        var exception = await failed.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.IsType<InvalidOperationException>(exception);
        Assert.Same(exception, command.LastException);
        Assert.True(command.CanExecute(null));
    }

    [Fact]
    public void DelegateCommand_UsesParameter()
    {
        object? received = null;
        var command = new DelegateCommand(parameter => received = parameter, parameter => Equals(parameter, "ok"));

        command.Execute("no");
        command.Execute("ok");

        Assert.Equal("ok", received);
    }

    [Fact]
    public void PlcConnectionStateBrushConverter_MapsStatesToBrushes()
    {
        var converter = new PlcConnectionStateBrushConverter();

        Assert.Same(Brushes.ForestGreen, converter.Convert(PlcConnectionState.Connected, typeof(Brush), null!, CultureInfo.InvariantCulture));
        Assert.Same(Brushes.DarkOrange, converter.Convert(PlcConnectionState.Connecting, typeof(Brush), null!, CultureInfo.InvariantCulture));
        Assert.Same(Brushes.Firebrick, converter.Convert(PlcConnectionState.Faulted, typeof(Brush), null!, CultureInfo.InvariantCulture));
        Assert.Same(Brushes.Gray, converter.Convert(PlcConnectionState.Disconnected, typeof(Brush), null!, CultureInfo.InvariantCulture));
        Assert.Equal(Binding.DoNothing, converter.ConvertBack(Brushes.Gray, typeof(PlcConnectionState), null!, CultureInfo.InvariantCulture));
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

    [Fact]
    public void PlcPlaceholderReplacements_ExposeUsefulMonitorTypes()
    {
        var address = new PlcAddressViewItem
        {
            DeviceType = PlcDeviceType.M,
            Offset = 10,
            BitOffset = 2,
            DisplayName = "Motor ready"
        };
        var channel = new PlcChannelViewItem
        {
            Name = "Line 1",
            State = PlcConnectionState.Connected,
            Description = "Main PLC"
        };
        using var service = new TestAsyncDisposable(new PlcMonitorService());

        Assert.Equal(new PlcAddress(PlcDeviceType.M, 10, 2), address.ToAddress());
        Assert.Equal("Line 1", channel.Name);
        Assert.Equal(PlcConnectionState.Connected, channel.State);
        Assert.NotNull(service.Value.ViewModel);
    }

    private sealed class TestAsyncDisposable : IDisposable
    {
        public TestAsyncDisposable(PlcMonitorService value)
        {
            Value = value;
        }

        public PlcMonitorService Value { get; }

        public void Dispose()
        {
            Value.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }
}
