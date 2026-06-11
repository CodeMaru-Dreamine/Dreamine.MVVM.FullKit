using System.IO.Ports;
using System.Net;
using Dreamine.Communication.FullKit;
using Dreamine.Communication.RabbitMQ.Options;
using Dreamine.Communication.Serial.Options;
using Dreamine.Communication.Serial.Ports;
using Dreamine.Communication.Sockets.Enums;
using Dreamine.Communication.Sockets.Options;

namespace Dreamine.FullKit.Tests.Communication;

public sealed class TransportOptionTests
{
    [Fact]
    public void FullKitMarker_ExposesPackageName()
    {
        Assert.Equal("Dreamine.Communication.FullKit", FullKitMarker.PackageName);
    }

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

    [Fact]
    public void UdpTransportOptions_RejectInvalidHost()
    {
        var options = new UdpTransportOptions { RemoteHost = "not a host" };

        Assert.Throws<ArgumentException>(() => options.CreateRemoteEndPoint());
    }

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
