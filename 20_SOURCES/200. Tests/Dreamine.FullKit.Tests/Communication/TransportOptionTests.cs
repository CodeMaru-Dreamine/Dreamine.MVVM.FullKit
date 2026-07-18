using System.IO.Ports;
using System.Net;
using Dreamine.Communication.FullKit;
using Dreamine.Communication.RabbitMQ.Options;
using Dreamine.Communication.Serial.Options;
using Dreamine.Communication.Serial.Ports;
using Dreamine.Communication.Sockets.Enums;
using Dreamine.Communication.Sockets.Options;

namespace Dreamine.FullKit.Tests.Communication;

/// <summary>
/// \if KO
/// <para>Transport Option Tests 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates transport option tests functionality and related state.</para>
/// \endif
/// </summary>
public sealed class TransportOptionTests
{
    /// <summary>
    /// \if KO
    /// <para>Full Kit Marker Exposes Package Name 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the full kit marker exposes package name operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void FullKitMarker_ExposesPackageName()
    {
        Assert.Equal("Dreamine.Communication.FullKit", FullKitMarker.PackageName);
    }

    /// <summary>
    /// \if KO
    /// <para>Socket Options Expose Defaults And Create Udp Endpoints 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the socket options expose defaults and create udp endpoints operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void SocketOptions_ExposeDefaultsAndCreateUdpEndpoints()
    {
        var client = new TcpClientTransportOptions();
        var server = new TcpServerTransportOptions();
        var udp = new UdpTransportOptions();

        Assert.Equal("127.0.0.1", client.Host);
        Assert.Equal(5000, server.Port);
        Assert.Equal(TcpServerSendTargetMode.Broadcast, server.SendTargetMode);
        Assert.Equal(IPAddress.Loopback, udp.CreateLocalEndPoint().Address);
        Assert.Equal(16002, udp.CreateRemoteEndPoint().Port);
    }

    /// <summary>
    /// \if KO
    /// <para>Udp Transport Options Reject Invalid Host 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the udp transport options reject invalid host operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void UdpTransportOptions_RejectInvalidHost()
    {
        var options = new UdpTransportOptions { RemoteHost = "not a host" };

        Assert.Throws<ArgumentException>(() => options.CreateRemoteEndPoint());
    }

    /// <summary>
    /// \if KO
    /// <para>Serial And Rabbit Options Expose Defaults 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the serial and rabbit options expose defaults operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void SerialAndRabbitOptions_ExposeDefaults()
    {
        var serial = new SerialPortTransportOptions();
        var rabbit = new RabbitMqMessageBusOptions();

        Assert.Equal("COM1", serial.PortName);
        Assert.Equal(9600, serial.BaudRate);
        Assert.Equal(Parity.None, serial.Parity);
        Assert.Equal(3000, serial.ReadTimeoutMs);
        Assert.Equal("localhost", rabbit.HostName);
        Assert.Equal("guest", rabbit.UserName);
        Assert.Equal("dreamine.default.route", rabbit.RoutingKey);
    }

    /// <summary>
    /// \if KO
    /// <para>Serial Transport Allows Immediate Return Timeouts 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the serial transport allows immediate return timeouts operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void SerialTransport_AllowsImmediateReturnTimeouts()
    {
        var options = new SerialPortTransportOptions
        {
            ReadTimeoutMs = 0,
            WriteTimeoutMs = 0
        };

        var transport = new SerialPortTransport(options);

        Assert.Equal(Dreamine.Communication.Abstractions.Enums.ConnectionState.Disconnected, transport.State);
    }
}
