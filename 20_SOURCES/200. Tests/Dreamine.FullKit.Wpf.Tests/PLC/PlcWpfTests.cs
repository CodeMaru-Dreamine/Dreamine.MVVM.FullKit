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

/// <summary>
/// \if KO
/// <para>Plc Wpf Tests 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates plc wpf tests functionality and related state.</para>
/// \endif
/// </summary>
public sealed class PlcWpfTests
{
    /// <summary>
    /// \if KO
    /// <para>Relay Command Executes Only When Allowed 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the relay command executes only when allowed operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void RelayCommand_ExecutesOnlyWhenAllowed()
    {
        var count = 0;
        var command = new RelayCommand(() => count++, () => false);

        command.Execute(null);

        Assert.False(command.CanExecute(null));
        Assert.Equal(0, count);
    }

    /// <summary>
    /// \if KO
    /// <para>Async Relay Command Prevents Reentry While Executing 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the async relay command prevents reentry while executing operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Async Relay Command Prevents Reentry While Executing 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the async relay command prevents reentry while executing operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Async Relay Command Captures Execution Exception 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the async relay command captures execution exception operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Async Relay Command Captures Execution Exception 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the async relay command captures execution exception operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Delegate Command Uses Parameter 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the delegate command uses parameter operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void DelegateCommand_UsesParameter()
    {
        object? received = null;
        var command = new DelegateCommand(parameter => received = parameter, parameter => Equals(parameter, "ok"));

        command.Execute("no");
        command.Execute("ok");

        Assert.Equal("ok", received);
    }

    /// <summary>
    /// \if KO
    /// <para>Plc Connection State Brush Converter Maps States To Brushes 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the plc connection state brush converter maps states to brushes operation.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>Plc Operation Log Item Stores Values 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the plc operation log item stores values operation.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>Plc Placeholder Replacements Expose Useful Monitor Types 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the plc placeholder replacements expose useful monitor types operation.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>Test Async Disposable 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates test async disposable functionality and related state.</para>
    /// \endif
    /// </summary>
    private sealed class TestAsyncDisposable : IDisposable
    {
        /// <summary>
        /// \if KO
        /// <para>지정한 설정으로 <see cref="TestAsyncDisposable"/> 클래스의 새 인스턴스를 초기화합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Initializes a new instance of the <see cref="TestAsyncDisposable"/> class with the specified settings.</para>
        /// \endif
        /// </summary>
        /// <param name="value">
        /// \if KO
        /// <para>적용할 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The value to apply.</para>
        /// \endif
        /// </param>
        public TestAsyncDisposable(PlcMonitorService value)
        {
            Value = value;
        }

        /// <summary>
        /// \if KO
        /// <para>Value 값을 가져옵니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets the value value.</para>
        /// \endif
        /// </summary>
        public PlcMonitorService Value { get; }

        /// <summary>
        /// \if KO
        /// <para>이 인스턴스가 소유한 리소스를 해제합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Releases resources owned by this instance.</para>
        /// \endif
        /// </summary>
        public void Dispose()
        {
            Value.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }
}
