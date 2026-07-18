using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Dreamine.Communication.Abstractions.Enums;
using Dreamine.Communication.Wpf.Commands;
using Dreamine.Communication.Wpf.Converters;
using Dreamine.Communication.Wpf.Models;

namespace Dreamine.FullKit.Wpf.Tests.Communication;

/// <summary>
/// \if KO
/// <para>Communication Wpf Tests 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates communication wpf tests functionality and related state.</para>
/// \endif
/// </summary>
public sealed class CommunicationWpfTests
{
    /// <summary>
    /// \if KO
    /// <para>Delegate Command Executes And Raises Can Execute Changed 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the delegate command executes and raises can execute changed operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void DelegateCommand_ExecutesAndRaisesCanExecuteChanged()
    {
        var executedParameter = "";
        var raised = false;
        var command = new DelegateCommand(
            parameter => executedParameter = (string)parameter!,
            parameter => (string)parameter! == "ok");
        command.CanExecuteChanged += (_, _) => raised = true;

        command.Execute("ok");
        command.RaiseCanExecuteChanged();

        Assert.True(command.CanExecute("ok"));
        Assert.False(command.CanExecute("no"));
        Assert.Equal("ok", executedParameter);
        Assert.True(raised);
    }

    /// <summary>
    /// \if KO
    /// <para>Connection State Brush Converter Maps States To Brushes 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the connection state brush converter maps states to brushes operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void ConnectionStateBrushConverter_MapsStatesToBrushes()
    {
        var converter = new ConnectionStateBrushConverter();

        Assert.Same(Brushes.ForestGreen, converter.Convert(ConnectionState.Connected, typeof(Brush), null!, CultureInfo.InvariantCulture));
        Assert.Same(Brushes.DarkOrange, converter.Convert(ConnectionState.Connecting, typeof(Brush), null!, CultureInfo.InvariantCulture));
        Assert.Same(Brushes.Firebrick, converter.Convert(ConnectionState.Faulted, typeof(Brush), null!, CultureInfo.InvariantCulture));
        Assert.Same(Brushes.Gray, converter.Convert("bad", typeof(Brush), null!, CultureInfo.InvariantCulture));
        Assert.Same(Binding.DoNothing, converter.ConvertBack(Brushes.Gray, typeof(ConnectionState), null!, CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// \if KO
    /// <para>Communication Channel View Item Raises Property Changed For Changed Values Only 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the communication channel view item raises property changed for changed values only operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void CommunicationChannelViewItem_RaisesPropertyChangedForChangedValuesOnly()
    {
        var item = new CommunicationChannelViewItem();
        var changed = new List<string?>();
        item.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        item.Name = "Main";
        item.Name = "Main";
        item.State = ConnectionState.Connected;

        Assert.Equal(new[] { "Name", "State" }, changed);
    }

    /// <summary>
    /// \if KO
    /// <para>Communication Message Log Item Stores Display Values 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the communication message log item stores display values operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void CommunicationMessageLogItem_StoresDisplayValues()
    {
        var item = new CommunicationMessageLogItem
        {
            ChannelName = "Socket",
            Kind = TransportKind.Tcp,
            Direction = "TX",
            MessageName = "Ping",
            Route = "route",
            PayloadLength = 3,
            PayloadPreview = "abc"
        };

        Assert.Equal("Socket", item.ChannelName);
        Assert.Equal(TransportKind.Tcp, item.Kind);
        Assert.Equal("abc", item.PayloadPreview);
    }
}
