using System.Text;
using System.Windows;
using Dreamine.Communication.Abstractions.Enums;
using Dreamine.Communication.Abstractions.Interfaces;
using Dreamine.Communication.Abstractions.Models;
using Dreamine.Communication.Core.Framing;
using Dreamine.Communication.Core.Protocols;
using Dreamine.Communication.RabbitMQ.Buses;
using Dreamine.Communication.RabbitMQ.Options;
using Dreamine.Communication.Serial.Options;
using Dreamine.Communication.Serial.Ports;
using Dreamine.Communication.Sockets.Clients;
using Dreamine.Communication.Sockets.Options;
using Dreamine.Communication.Sockets.Servers;
using Dreamine.Communication.Sockets.Udp;
using Dreamine.Communication.Wpf.ViewModels;

namespace SampleSmart.Pages.PageSub.CommunicationTabs;

/// <summary>
/// \brief Communication 샘플 전체에서 공유되는 Runtime 컨텍스트입니다.
/// </summary>
/// <remarks>
/// 이 클래스는 Communication 샘플 탭들이 공유하는 Monitor, InMemory MessageBus,
/// TCP Server, TCP Client, Serial Port, RabbitMQ MessageBus 인스턴스를 관리합니다.
/// </remarks>
public sealed class CommunicationSampleRuntime
{
    private const string InMemoryChannelName = "InMemory-Communication";
    private const string InMemoryRouteName = "sample.communication.message";

    private const string RabbitMqChannelName = "RabbitMQ-MessageBus";
    private const string RabbitMqProtocol = "RabbitMQ";

    private const string DreamineEnvelopeProtocol = "DreamineEnvelope";
    private const string PlainTextProtocol = "PlainText";
    private const string RawAvailableProtocol = "RawAvailable";
    private const string RawJsonProtocol = "RawJson";

    private const int DreamineProtocolPort = 15001;
    private const int PlainTextProtocolPort = 15002;
    private const int RawAvailableProtocolPort = 15002;
    private const int RawJsonProtocolPort = 15003;

    private const int UdpPeerALocalPort = 16001;
    private const int UdpPeerBLocalPort = 16002;

    private readonly IMessageBus _messageBus;

    private bool _isInMemorySubscribed;
    private bool _isRabbitMqSubscribed;

    private TcpServerTransport? _tcpServer;
    private TcpClientTransport? _tcpClient;
    private UdpTransport? _udpPeerA;
    private UdpTransport? _udpPeerB;
    private SerialPortTransport? _serialTransport;
    private RabbitMqMessageBus? _rabbitMqBus;

    private string _currentServerProtocol = PlainTextProtocol;
    private string _currentClientProtocol = PlainTextProtocol;
    private string _currentServerEncoding = PlainTextProtocolOptions.Utf8EncodingName;
    private string _currentClientEncoding = PlainTextProtocolOptions.Utf8EncodingName;
    private string _currentUdpPeerAProtocol = PlainTextProtocol;
    private string _currentUdpPeerBProtocol = PlainTextProtocol;
    private string _currentUdpPeerAEncoding = PlainTextProtocolOptions.Utf8EncodingName;
    private string _currentUdpPeerBEncoding = PlainTextProtocolOptions.Utf8EncodingName;

    private string _currentSerialProtocol = RawAvailableProtocol;
    private string _currentSerialEncoding = PlainTextProtocolOptions.Utf8EncodingName;
    private string _currentSerialPortName = string.Empty;
    private int _currentSerialBaudRate = 9600;

    private string _currentRabbitMqHost = "localhost";
    private int _currentRabbitMqPort = 5672;
    private string _currentRabbitMqVirtualHost = "/";
    private string _currentRabbitMqExchangeName = "dreamine.sample.exchange";
    private string _currentRabbitMqQueueName = "dreamine.sample.queue";
    private string _currentRabbitMqRoutingKey = "dreamine.sample.route";

    /// <summary>
    /// \brief CommunicationSampleRuntime 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="messageBus">InMemory 샘플에 사용할 MessageBus입니다.</param>
    public CommunicationSampleRuntime(IMessageBus messageBus)
    {
        _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
        Monitor = new CommunicationMonitorViewModel();

        _ = SubscribeInMemoryAsync();
    }

    /// <summary>
    /// \brief Communication Monitor ViewModel입니다.
    /// </summary>
    public CommunicationMonitorViewModel Monitor { get; }

    /// <summary>
    /// \brief 선택 가능한 TCP 프로토콜 목록입니다.
    /// </summary>
    public IReadOnlyList<string> TcpProtocols { get; } =
    [
        DreamineEnvelopeProtocol,
        PlainTextProtocol,
        RawAvailableProtocol,
        RawJsonProtocol
    ];

    /// <summary>
    /// \brief 선택 가능한 Serial 프로토콜 목록입니다.
    /// </summary>
    public IReadOnlyList<string> SerialProtocols { get; } =
    [
        PlainTextProtocol,
        RawAvailableProtocol,
        RawJsonProtocol
    ];

    /// <summary>
    /// \brief 선택 가능한 UDP 프로토콜 목록입니다.
    /// </summary>
    public IReadOnlyList<string> UdpProtocols { get; } =
    [
        DreamineEnvelopeProtocol,
        PlainTextProtocol,
        RawAvailableProtocol,
        RawJsonProtocol
    ];

    /// <summary>
    /// \brief 선택 가능한 외부 PlainText 인코딩 목록입니다.
    /// </summary>
    public IReadOnlyList<string> TextEncodings { get; } =
    [
        PlainTextProtocolOptions.Utf8EncodingName,
        PlainTextProtocolOptions.KoreanCodePage949EncodingName
    ];

    /// <summary>
    /// \brief 선택 가능한 UDP PlainText 인코딩 목록입니다.
    /// </summary>
    public IReadOnlyList<string> UdpTextEncodings => TextEncodings;

    /// <summary>
    /// \brief InMemory 채널을 추가합니다.
    /// </summary>
    public void AddInMemoryChannel()
    {
        if (Monitor.Channels.Any(x => x.Name == InMemoryChannelName))
        {
            return;
        }

        Monitor.AddChannel(
            InMemoryChannelName,
            TransportKind.InMemory,
            "SampleSmart in-memory communication channel.");
    }

    /// <summary>
    /// \brief InMemory MessageBus에 연결합니다.
    /// </summary>
    public async Task ConnectInMemoryAsync()
    {
        AddInMemoryChannel();

        await _messageBus.ConnectAsync();

        Monitor.UpdateChannelState(
            InMemoryChannelName,
            ConnectionState.Connected);
    }

    /// <summary>
    /// \brief InMemory MessageBus 연결을 해제합니다.
    /// </summary>
    public async Task DisconnectInMemoryAsync()
    {
        AddInMemoryChannel();

        await _messageBus.DisconnectAsync();

        Monitor.UpdateChannelState(
            InMemoryChannelName,
            ConnectionState.Disconnected);
    }

    /// <summary>
    /// \brief InMemory MessageBus로 테스트 메시지를 송신합니다.
    /// </summary>
    /// <param name="text">송신 문자열입니다.</param>
    public async Task SendInMemoryAsync(string text)
    {
        AddInMemoryChannel();

        if (_messageBus.State != ConnectionState.Connected)
        {
            await ConnectInMemoryAsync();
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            text = "Hello from SampleSmart InMemory";
        }

        var message = CreateTextMessage(
            "Sample.Send",
            InMemoryRouteName,
            text,
            "InMemory");

        Monitor.AddSendLog(
            InMemoryChannelName,
            TransportKind.InMemory,
            message);

        await _messageBus.PublishAsync(message);
    }

    /// <summary>
    /// \brief InMemory 수동 수신 테스트 로그를 추가합니다.
    /// </summary>
    /// <param name="text">수신으로 표시할 문자열입니다.</param>
    public void ReceiveInMemory(string text)
    {
        AddInMemoryChannel();

        if (string.IsNullOrWhiteSpace(text))
        {
            text = "Hello from SampleSmart manual receive";
        }

        var message = CreateTextMessage(
            "Sample.Receive",
            "sample.communication.manual-receive",
            text,
            "InMemory");

        Monitor.AddReceiveLog(
            InMemoryChannelName,
            TransportKind.InMemory,
            message);
    }

    /// <summary>
    /// \brief 선택된 프로토콜로 TCP Server를 시작합니다.
    /// </summary>
    /// <param name="protocol">프로토콜 이름입니다.</param>
    /// <param name="encodingName">PlainText 외부 송수신 인코딩 이름입니다.</param>
    public async Task StartTcpServerAsync(
        string protocol,
        string encodingName = PlainTextProtocolOptions.Utf8EncodingName)
    {
        protocol = NormalizeProtocol(protocol);
        encodingName = NormalizeTextEncodingName(encodingName);

        if (_tcpServer is not null &&
            _tcpServer.State == ConnectionState.Connected &&
            _currentServerProtocol == protocol &&
            _currentServerEncoding == encodingName)
        {
            return;
        }

        if (_tcpServer is not null)
        {
            await StopTcpServerAsync();
        }

        _currentServerProtocol = protocol;
        _currentServerEncoding = encodingName;

        EnsureTcpServerChannel(protocol, encodingName);

        var port = GetTcpPort(protocol);

        _tcpServer = new TcpServerTransport(
            new TcpServerTransportOptions
            {
                Host = "127.0.0.1",
                Port = port
            },
            CreateProtocolAdapter(
                protocol,
                "tcp",
                "Tcp",
                encodingName),
            CreateFrameCodec(protocol, encodingName));

        _tcpServer.MessageReceived += OnTcpServerMessageReceived;

        await _tcpServer.ConnectAsync();

        Monitor.UpdateChannelState(
            GetTcpServerChannelName(protocol, encodingName),
            ConnectionState.Connected);
    }

    /// <summary>
    /// \brief 현재 TCP Server를 종료합니다.
    /// </summary>
    public async Task StopTcpServerAsync()
    {
        var protocol = _currentServerProtocol;
        var encodingName = _currentServerEncoding;
        var channelName = GetTcpServerChannelName(protocol, encodingName);

        if (_tcpServer is null)
        {
            if (Monitor.Channels.Any(x => x.Name == channelName))
            {
                Monitor.UpdateChannelState(channelName, ConnectionState.Disconnected);
            }

            return;
        }

        _tcpServer.MessageReceived -= OnTcpServerMessageReceived;

        await _tcpServer.DisposeAsync();

        _tcpServer = null;

        if (Monitor.Channels.Any(x => x.Name == channelName))
        {
            Monitor.UpdateChannelState(channelName, ConnectionState.Disconnected);
        }
    }

    /// <summary>
    /// \brief 선택된 프로토콜로 TCP Server에서 연결된 클라이언트에게 메시지를 송신합니다.
    /// </summary>
    /// <param name="protocol">프로토콜 이름입니다.</param>
    /// <param name="text">송신 문자열입니다.</param>
    /// <param name="encodingName">PlainText 외부 송수신 인코딩 이름입니다.</param>
    public async Task SendTcpServerAsync(
        string protocol,
        string text,
        string encodingName = PlainTextProtocolOptions.Utf8EncodingName)
    {
        protocol = NormalizeProtocol(protocol);
        encodingName = NormalizeTextEncodingName(encodingName);

        if (string.IsNullOrWhiteSpace(text))
        {
            text = "Hello from Dreamine TCP Server";
        }

        if (_tcpServer is null ||
            _tcpServer.State != ConnectionState.Connected ||
            _currentServerProtocol != protocol ||
            _currentServerEncoding != encodingName)
        {
            await StartTcpServerAsync(protocol, encodingName);
        }

        if (_tcpServer is null)
        {
            return;
        }

        var message = CreateTcpMessageByProtocol(
            protocol,
            "Server.Send",
            text,
            encodingName);

        var channelName = GetTcpServerChannelName(protocol, encodingName);

        Monitor.AddSendLog(
            channelName,
            TransportKind.Tcp,
            message);

        await _tcpServer.SendAsync(message);
    }

    /// <summary>
    /// \brief 선택된 프로토콜로 TCP Client를 연결합니다.
    /// </summary>
    /// <param name="protocol">프로토콜 이름입니다.</param>
    /// <param name="encodingName">PlainText 외부 송수신 인코딩 이름입니다.</param>
    public async Task ConnectTcpClientAsync(
        string protocol,
        string encodingName = PlainTextProtocolOptions.Utf8EncodingName)
    {
        protocol = NormalizeProtocol(protocol);
        encodingName = NormalizeTextEncodingName(encodingName);

        if (_tcpClient is not null &&
            _tcpClient.State == ConnectionState.Connected &&
            _currentClientProtocol == protocol &&
            _currentClientEncoding == encodingName)
        {
            return;
        }

        if (_tcpClient is not null)
        {
            await DisconnectTcpClientAsync();
        }

        _currentClientProtocol = protocol;
        _currentClientEncoding = encodingName;

        EnsureTcpClientChannel(protocol, encodingName);

        var port = GetTcpPort(protocol);

        _tcpClient = new TcpClientTransport(
            new TcpClientTransportOptions
            {
                Host = "127.0.0.1",
                Port = port
            },
            CreateProtocolAdapter(
                protocol,
                "tcp",
                "Tcp",
                encodingName),
            CreateFrameCodec(protocol, encodingName));

        _tcpClient.MessageReceived += OnTcpClientMessageReceived;

        await _tcpClient.ConnectAsync();

        Monitor.UpdateChannelState(
            GetTcpClientChannelName(protocol, encodingName),
            ConnectionState.Connected);
    }

    /// <summary>
    /// \brief 현재 TCP Client 연결을 해제합니다.
    /// </summary>
    public async Task DisconnectTcpClientAsync()
    {
        var protocol = _currentClientProtocol;
        var encodingName = _currentClientEncoding;
        var channelName = GetTcpClientChannelName(protocol, encodingName);

        if (_tcpClient is null)
        {
            if (Monitor.Channels.Any(x => x.Name == channelName))
            {
                Monitor.UpdateChannelState(channelName, ConnectionState.Disconnected);
            }

            return;
        }

        _tcpClient.MessageReceived -= OnTcpClientMessageReceived;

        await _tcpClient.DisposeAsync();

        _tcpClient = null;

        if (Monitor.Channels.Any(x => x.Name == channelName))
        {
            Monitor.UpdateChannelState(channelName, ConnectionState.Disconnected);
        }
    }

    /// <summary>
    /// \brief 선택된 프로토콜로 TCP Client에서 서버로 메시지를 송신합니다.
    /// </summary>
    /// <param name="protocol">프로토콜 이름입니다.</param>
    /// <param name="text">송신 문자열입니다.</param>
    /// <param name="encodingName">PlainText 외부 송수신 인코딩 이름입니다.</param>
    public async Task SendTcpClientAsync(
        string protocol,
        string text,
        string encodingName = PlainTextProtocolOptions.Utf8EncodingName)
    {
        protocol = NormalizeProtocol(protocol);
        encodingName = NormalizeTextEncodingName(encodingName);

        if (string.IsNullOrWhiteSpace(text))
        {
            text = "Hello from Dreamine TCP Client";
        }

        if (_tcpClient is null ||
            _tcpClient.State != ConnectionState.Connected ||
            _currentClientProtocol != protocol ||
            _currentClientEncoding != encodingName)
        {
            await ConnectTcpClientAsync(protocol, encodingName);
        }

        if (_tcpClient is null)
        {
            return;
        }

        var message = CreateTcpMessageByProtocol(
            protocol,
            "Client.Send",
            text,
            encodingName);

        var channelName = GetTcpClientChannelName(protocol, encodingName);

        Monitor.AddSendLog(
            channelName,
            TransportKind.Tcp,
            message);

        await _tcpClient.SendAsync(message);
    }

    /// <summary>
    /// \brief TCP Server와 TCP Client를 모두 종료합니다.
    /// </summary>
    public async Task StopAllTcpAsync()
    {
        await DisconnectTcpClientAsync();
        await StopTcpServerAsync();
    }

    /// <summary>
    /// \brief UDP Peer A와 Peer B를 모두 시작합니다.
    /// </summary>
    /// <param name="protocol">프로토콜 이름입니다.</param>
    /// <param name="encodingName">PlainText 외부 송수신 인코딩 이름입니다.</param>
    public async Task StartUdpLoopbackAsync(
        string protocol,
        string encodingName = PlainTextProtocolOptions.Utf8EncodingName)
    {
        await ConnectUdpPeerAAsync(protocol, encodingName);
        await ConnectUdpPeerBAsync(protocol, encodingName);
    }

    /// <summary>
    /// \brief 선택된 프로토콜로 UDP Peer A를 시작합니다.
    /// </summary>
    /// <param name="protocol">프로토콜 이름입니다.</param>
    /// <param name="encodingName">PlainText 외부 송수신 인코딩 이름입니다.</param>
    public async Task ConnectUdpPeerAAsync(
        string protocol,
        string encodingName = PlainTextProtocolOptions.Utf8EncodingName)
    {
        protocol = NormalizeProtocol(protocol);
        encodingName = NormalizeTextEncodingName(encodingName);

        if (_udpPeerA is not null &&
            _udpPeerA.State == ConnectionState.Connected &&
            _currentUdpPeerAProtocol == protocol &&
            _currentUdpPeerAEncoding == encodingName)
        {
            return;
        }

        if (_udpPeerA is not null)
        {
            await DisconnectUdpPeerAAsync();
        }

        _currentUdpPeerAProtocol = protocol;
        _currentUdpPeerAEncoding = encodingName;

        EnsureUdpPeerChannel("A", protocol, encodingName);

        _udpPeerA = new UdpTransport(
            new UdpTransportOptions
            {
                LocalHost = "127.0.0.1",
                LocalPort = UdpPeerALocalPort,
                RemoteHost = "127.0.0.1",
                RemotePort = UdpPeerBLocalPort,
                ReuseAddress = true
            },
            CreateProtocolAdapter(
                protocol,
                "udp",
                "Udp",
                encodingName));

        _udpPeerA.MessageReceived += OnUdpPeerAMessageReceived;

        await _udpPeerA.ConnectAsync();

        Monitor.UpdateChannelState(
            GetUdpPeerChannelName("A", protocol, encodingName),
            ConnectionState.Connected);
    }

    /// <summary>
    /// \brief 선택된 프로토콜로 UDP Peer B를 시작합니다.
    /// </summary>
    /// <param name="protocol">프로토콜 이름입니다.</param>
    /// <param name="encodingName">PlainText 외부 송수신 인코딩 이름입니다.</param>
    public async Task ConnectUdpPeerBAsync(
        string protocol,
        string encodingName = PlainTextProtocolOptions.Utf8EncodingName)
    {
        protocol = NormalizeProtocol(protocol);
        encodingName = NormalizeTextEncodingName(encodingName);

        if (_udpPeerB is not null &&
            _udpPeerB.State == ConnectionState.Connected &&
            _currentUdpPeerBProtocol == protocol &&
            _currentUdpPeerBEncoding == encodingName)
        {
            return;
        }

        if (_udpPeerB is not null)
        {
            await DisconnectUdpPeerBAsync();
        }

        _currentUdpPeerBProtocol = protocol;
        _currentUdpPeerBEncoding = encodingName;

        EnsureUdpPeerChannel("B", protocol, encodingName);

        _udpPeerB = new UdpTransport(
            new UdpTransportOptions
            {
                LocalHost = "127.0.0.1",
                LocalPort = UdpPeerBLocalPort,
                RemoteHost = "127.0.0.1",
                RemotePort = UdpPeerALocalPort,
                ReuseAddress = true
            },
            CreateProtocolAdapter(
                protocol,
                "udp",
                "Udp",
                encodingName));

        _udpPeerB.MessageReceived += OnUdpPeerBMessageReceived;

        await _udpPeerB.ConnectAsync();

        Monitor.UpdateChannelState(
            GetUdpPeerChannelName("B", protocol, encodingName),
            ConnectionState.Connected);
    }

    /// <summary>
    /// \brief UDP Peer A 연결을 해제합니다.
    /// </summary>
    public async Task DisconnectUdpPeerAAsync()
    {
        var protocol = _currentUdpPeerAProtocol;
        var encodingName = _currentUdpPeerAEncoding;
        var channelName = GetUdpPeerChannelName("A", protocol, encodingName);

        if (_udpPeerA is null)
        {
            if (Monitor.Channels.Any(x => x.Name == channelName))
            {
                Monitor.UpdateChannelState(channelName, ConnectionState.Disconnected);
            }

            return;
        }

        _udpPeerA.MessageReceived -= OnUdpPeerAMessageReceived;

        await _udpPeerA.DisposeAsync();

        _udpPeerA = null;

        if (Monitor.Channels.Any(x => x.Name == channelName))
        {
            Monitor.UpdateChannelState(channelName, ConnectionState.Disconnected);
        }
    }

    /// <summary>
    /// \brief UDP Peer B 연결을 해제합니다.
    /// </summary>
    public async Task DisconnectUdpPeerBAsync()
    {
        var protocol = _currentUdpPeerBProtocol;
        var encodingName = _currentUdpPeerBEncoding;
        var channelName = GetUdpPeerChannelName("B", protocol, encodingName);

        if (_udpPeerB is null)
        {
            if (Monitor.Channels.Any(x => x.Name == channelName))
            {
                Monitor.UpdateChannelState(channelName, ConnectionState.Disconnected);
            }

            return;
        }

        _udpPeerB.MessageReceived -= OnUdpPeerBMessageReceived;

        await _udpPeerB.DisposeAsync();

        _udpPeerB = null;

        if (Monitor.Channels.Any(x => x.Name == channelName))
        {
            Monitor.UpdateChannelState(channelName, ConnectionState.Disconnected);
        }
    }

    /// <summary>
    /// \brief UDP Peer A와 Peer B를 모두 종료합니다.
    /// </summary>
    public async Task StopAllUdpAsync()
    {
        await DisconnectUdpPeerBAsync();
        await DisconnectUdpPeerAAsync();
    }

    /// <summary>
    /// \brief UDP Peer A에서 Peer B로 메시지를 송신합니다.
    /// </summary>
    /// <param name="protocol">프로토콜 이름입니다.</param>
    /// <param name="text">송신 문자열입니다.</param>
    /// <param name="encodingName">PlainText 외부 송수신 인코딩 이름입니다.</param>
    public async Task SendUdpPeerAAsync(
        string protocol,
        string text,
        string encodingName = PlainTextProtocolOptions.Utf8EncodingName)
    {
        protocol = NormalizeProtocol(protocol);
        encodingName = NormalizeTextEncodingName(encodingName);

        if (string.IsNullOrWhiteSpace(text))
        {
            text = "Hello from Dreamine UDP Peer A";
        }

        if (_udpPeerA is null ||
            _udpPeerA.State != ConnectionState.Connected ||
            _currentUdpPeerAProtocol != protocol ||
            _currentUdpPeerAEncoding != encodingName)
        {
            await ConnectUdpPeerAAsync(protocol, encodingName);
        }

        if (_udpPeerA is null)
        {
            return;
        }

        var message = CreateUdpMessageByProtocol(
            protocol,
            "PeerA.Send",
            text,
            encodingName);

        var channelName = GetUdpPeerChannelName("A", protocol, encodingName);

        Monitor.AddSendLog(
            channelName,
            TransportKind.Udp,
            message);

        await _udpPeerA.SendAsync(message);
    }

    /// <summary>
    /// \brief UDP Peer B에서 Peer A로 메시지를 송신합니다.
    /// </summary>
    /// <param name="protocol">프로토콜 이름입니다.</param>
    /// <param name="text">송신 문자열입니다.</param>
    /// <param name="encodingName">PlainText 외부 송수신 인코딩 이름입니다.</param>
    public async Task SendUdpPeerBAsync(
        string protocol,
        string text,
        string encodingName = PlainTextProtocolOptions.Utf8EncodingName)
    {
        protocol = NormalizeProtocol(protocol);
        encodingName = NormalizeTextEncodingName(encodingName);

        if (string.IsNullOrWhiteSpace(text))
        {
            text = "Hello from Dreamine UDP Peer B";
        }

        if (_udpPeerB is null ||
            _udpPeerB.State != ConnectionState.Connected ||
            _currentUdpPeerBProtocol != protocol ||
            _currentUdpPeerBEncoding != encodingName)
        {
            await ConnectUdpPeerBAsync(protocol, encodingName);
        }

        if (_udpPeerB is null)
        {
            return;
        }

        var message = CreateUdpMessageByProtocol(
            protocol,
            "PeerB.Send",
            text,
            encodingName);

        var channelName = GetUdpPeerChannelName("B", protocol, encodingName);

        Monitor.AddSendLog(
            channelName,
            TransportKind.Udp,
            message);

        await _udpPeerB.SendAsync(message);
    }

    /// <summary>
    /// \brief 선택된 설정으로 Serial Port를 연결합니다.
    /// </summary>
    /// <param name="portName">Serial Port 이름입니다.</param>
    /// <param name="baudRate">BaudRate입니다.</param>
    /// <param name="protocol">프로토콜 이름입니다.</param>
    /// <param name="encodingName">PlainText 외부 송수신 인코딩 이름입니다.</param>
    public async Task ConnectSerialAsync(
        string portName,
        int baudRate,
        string protocol,
        string encodingName = PlainTextProtocolOptions.Utf8EncodingName)
    {
        if (string.IsNullOrWhiteSpace(portName))
        {
            return;
        }

        protocol = NormalizeProtocol(protocol);
        encodingName = NormalizeTextEncodingName(encodingName);

        if (_serialTransport is not null &&
            _serialTransport.State == ConnectionState.Connected &&
            _currentSerialPortName == portName &&
            _currentSerialBaudRate == baudRate &&
            _currentSerialProtocol == protocol &&
            _currentSerialEncoding == encodingName)
        {
            return;
        }

        if (_serialTransport is not null)
        {
            await DisconnectSerialAsync();
        }

        _currentSerialPortName = portName;
        _currentSerialBaudRate = baudRate;
        _currentSerialProtocol = protocol;
        _currentSerialEncoding = encodingName;

        EnsureSerialChannel();

        _serialTransport = new SerialPortTransport(
            new SerialPortTransportOptions
            {
                PortName = portName,
                BaudRate = baudRate
            },
            CreateProtocolAdapter(
                protocol,
                "serial",
                "Serial",
                encodingName),
            CreateFrameCodec(protocol, encodingName));

        _serialTransport.MessageReceived += OnSerialMessageReceived;

        await _serialTransport.ConnectAsync();

        Monitor.UpdateChannelState(
            GetSerialChannelName(),
            ConnectionState.Connected);
    }

    /// <summary>
    /// \brief 현재 Serial Port 연결을 해제합니다.
    /// </summary>
    public async Task DisconnectSerialAsync()
    {
        var channelName = GetSerialChannelName();

        if (_serialTransport is null)
        {
            if (Monitor.Channels.Any(x => x.Name == channelName))
            {
                Monitor.UpdateChannelState(channelName, ConnectionState.Disconnected);
            }

            return;
        }

        _serialTransport.MessageReceived -= OnSerialMessageReceived;

        await _serialTransport.DisposeAsync();

        _serialTransport = null;

        if (Monitor.Channels.Any(x => x.Name == channelName))
        {
            Monitor.UpdateChannelState(channelName, ConnectionState.Disconnected);
        }
    }

    /// <summary>
    /// \brief 현재 Serial Port로 메시지를 송신합니다.
    /// </summary>
    /// <param name="protocol">프로토콜 이름입니다.</param>
    /// <param name="text">송신 문자열입니다.</param>
    /// <param name="encodingName">PlainText 외부 송수신 인코딩 이름입니다.</param>
    public async Task SendSerialAsync(
        string protocol,
        string text,
        string encodingName = PlainTextProtocolOptions.Utf8EncodingName)
    {
        protocol = NormalizeProtocol(protocol);
        encodingName = NormalizeTextEncodingName(encodingName);

        if (string.IsNullOrWhiteSpace(text))
        {
            text = "test";
        }

        if (_serialTransport is null ||
            _serialTransport.State != ConnectionState.Connected)
        {
            return;
        }

        var message = CreateSerialMessageByProtocol(
            protocol,
            "Send",
            text,
            encodingName);

        var channelName = GetSerialChannelName();

        Monitor.AddSendLog(
            channelName,
            TransportKind.Serial,
            message);

        await _serialTransport.SendAsync(message);
    }

    /// <summary>
    /// \brief RabbitMQ에 연결합니다.
    /// </summary>
    /// <param name="host">RabbitMQ Host입니다.</param>
    /// <param name="port">RabbitMQ Port입니다.</param>
    /// <param name="virtualHost">RabbitMQ VirtualHost입니다.</param>
    /// <param name="userName">RabbitMQ 사용자 이름입니다.</param>
    /// <param name="password">RabbitMQ 비밀번호입니다.</param>
    /// <param name="exchangeName">Exchange 이름입니다.</param>
    /// <param name="queueName">Queue 이름입니다.</param>
    /// <param name="routingKey">RoutingKey입니다.</param>
    public async Task ConnectRabbitMqAsync(
        string host,
        int port,
        string virtualHost,
        string userName,
        string password,
        string exchangeName,
        string queueName,
        string routingKey)
    {
        host = NormalizeText(host, "localhost");
        virtualHost = NormalizeText(virtualHost, "/");
        userName = NormalizeText(userName, "guest");
        password = NormalizeText(password, "guest");
        exchangeName = NormalizeText(exchangeName, "dreamine.sample.exchange");
        queueName = NormalizeText(queueName, "dreamine.sample.queue");
        routingKey = NormalizeText(routingKey, "dreamine.sample.route");

        if (port <= 0 || port > 65535)
        {
            port = 5672;
        }

        if (_rabbitMqBus is not null &&
            _rabbitMqBus.State == ConnectionState.Connected &&
            _currentRabbitMqHost == host &&
            _currentRabbitMqPort == port &&
            _currentRabbitMqVirtualHost == virtualHost &&
            _currentRabbitMqExchangeName == exchangeName &&
            _currentRabbitMqQueueName == queueName &&
            _currentRabbitMqRoutingKey == routingKey)
        {
            return;
        }

        if (_rabbitMqBus is not null)
        {
            await DisconnectRabbitMqAsync();
        }

        _currentRabbitMqHost = host;
        _currentRabbitMqPort = port;
        _currentRabbitMqVirtualHost = virtualHost;
        _currentRabbitMqExchangeName = exchangeName;
        _currentRabbitMqQueueName = queueName;
        _currentRabbitMqRoutingKey = routingKey;
        _isRabbitMqSubscribed = false;

        EnsureRabbitMqChannel();

        _rabbitMqBus = new RabbitMqMessageBus(
            new RabbitMqMessageBusOptions
            {
                HostName = host,
                Port = port,
                VirtualHost = virtualHost,
                UserName = userName,
                Password = password,
                ExchangeName = exchangeName,
                QueueName = queueName,
                RoutingKey = routingKey
            });

        try
        {
            await _rabbitMqBus.ConnectAsync();

            Monitor.UpdateChannelState(
                RabbitMqChannelName,
                ConnectionState.Connected);
        }
        catch (Exception ex)
        {
            Monitor.UpdateChannelState(
                RabbitMqChannelName,
                ConnectionState.Faulted);

            AddRabbitMqErrorLog("RabbitMQ.Connect.Failed", ex.Message);
        }
    }

    /// <summary>
    /// \brief RabbitMQ 메시지 구독을 시작합니다.
    /// </summary>
    /// <param name="exchangeName">Exchange 이름입니다.</param>
    /// <param name="queueName">Queue 이름입니다.</param>
    /// <param name="routingKey">RoutingKey입니다.</param>
    public async Task SubscribeRabbitMqAsync(
        string exchangeName,
        string queueName,
        string routingKey)
    {
        exchangeName = NormalizeText(exchangeName, _currentRabbitMqExchangeName);
        queueName = NormalizeText(queueName, _currentRabbitMqQueueName);
        routingKey = NormalizeText(routingKey, _currentRabbitMqRoutingKey);

        if (_rabbitMqBus is null)
        {
            AddRabbitMqErrorLog(
                "RabbitMQ.Subscribe.Skipped",
                "RabbitMQ is not connected.");
            return;
        }

        if (_rabbitMqBus.State != ConnectionState.Connected)
        {
            AddRabbitMqErrorLog(
                "RabbitMQ.Subscribe.Skipped",
                $"RabbitMQ state is {_rabbitMqBus.State}.");
            return;
        }

        if (_isRabbitMqSubscribed)
        {
            return;
        }

        _currentRabbitMqExchangeName = exchangeName;
        _currentRabbitMqQueueName = queueName;
        _currentRabbitMqRoutingKey = routingKey;

        try
        {
            await _rabbitMqBus.SubscribeAsync(
                routingKey,
                (message, _) =>
                {
                    RunOnUiThread(() =>
                    {
                        Monitor.AddReceiveLog(
                            RabbitMqChannelName,
                            TransportKind.RabbitMq,
                            message);
                    });

                    return Task.CompletedTask;
                });

            _isRabbitMqSubscribed = true;
        }
        catch (Exception ex)
        {
            AddRabbitMqErrorLog("RabbitMQ.Subscribe.Failed", ex.Message);
        }
    }

    /// <summary>
    /// \brief RabbitMQ 메시지를 발행합니다.
    /// </summary>
    /// <param name="exchangeName">Exchange 이름입니다.</param>
    /// <param name="routingKey">RoutingKey입니다.</param>
    /// <param name="text">송신 문자열입니다.</param>
    public async Task PublishRabbitMqAsync(
        string exchangeName,
        string routingKey,
        string text)
    {
        exchangeName = NormalizeText(exchangeName, _currentRabbitMqExchangeName);
        routingKey = NormalizeText(routingKey, _currentRabbitMqRoutingKey);

        if (string.IsNullOrWhiteSpace(text))
        {
            text = "test";
        }

        if (_rabbitMqBus is null)
        {
            AddRabbitMqErrorLog(
                "RabbitMQ.Publish.Skipped",
                "RabbitMQ is not connected.");
            return;
        }

        if (_rabbitMqBus.State != ConnectionState.Connected)
        {
            AddRabbitMqErrorLog(
                "RabbitMQ.Publish.Skipped",
                $"RabbitMQ state is {_rabbitMqBus.State}.");
            return;
        }

        _currentRabbitMqExchangeName = exchangeName;
        _currentRabbitMqRoutingKey = routingKey;

        var message = CreateRabbitMqMessage(
            "Publish",
            routingKey,
            text);

        Monitor.AddSendLog(
            RabbitMqChannelName,
            TransportKind.RabbitMq,
            message);

        try
        {
            await _rabbitMqBus.PublishAsync(message);
        }
        catch (Exception ex)
        {
            AddRabbitMqErrorLog("RabbitMQ.Publish.Failed", ex.Message);
        }
    }

    /// <summary>
    /// \brief RabbitMQ 연결을 해제합니다.
    /// </summary>
    public async Task DisconnectRabbitMqAsync()
    {
        if (_rabbitMqBus is null)
        {
            if (Monitor.Channels.Any(x => x.Name == RabbitMqChannelName))
            {
                Monitor.UpdateChannelState(
                    RabbitMqChannelName,
                    ConnectionState.Disconnected);
            }

            return;
        }

        await _rabbitMqBus.DisposeAsync();

        _rabbitMqBus = null;
        _isRabbitMqSubscribed = false;

        if (Monitor.Channels.Any(x => x.Name == RabbitMqChannelName))
        {
            Monitor.UpdateChannelState(
                RabbitMqChannelName,
                ConnectionState.Disconnected);
        }
    }

    /// <summary>
    /// \brief 모든 통신 샘플 연결을 종료합니다.
    /// </summary>
    public async Task StopAllAsync()
    {
        await DisconnectRabbitMqAsync();
        await DisconnectSerialAsync();
        await StopAllUdpAsync();
        await StopAllTcpAsync();
        await DisconnectInMemoryAsync();
    }

    private async Task SubscribeInMemoryAsync()
    {
        if (_isInMemorySubscribed)
        {
            return;
        }

        _isInMemorySubscribed = true;

        await _messageBus.SubscribeAsync(
            InMemoryRouteName,
            (message, _) =>
            {
                RunOnUiThread(() =>
                {
                    Monitor.AddReceiveLog(
                        InMemoryChannelName,
                        TransportKind.InMemory,
                        message);
                });

                return Task.CompletedTask;
            });
    }

    private async void OnTcpServerMessageReceived(object? sender, MessageEnvelope message)
    {
        var protocol = _currentServerProtocol;
        var encodingName = _currentServerEncoding;
        var channelName = GetTcpServerChannelName(protocol, encodingName);

        RunOnUiThread(() =>
        {
            Monitor.AddReceiveLog(
                channelName,
                TransportKind.Tcp,
                message);
        });

        if (_tcpServer is null ||
            _tcpServer.State != ConnectionState.Connected)
        {
            return;
        }

        var receiveText = Encoding.UTF8.GetString(message.Payload);

        var echoMessage = CreateTcpMessageByProtocol(
            protocol,
            "Server.Echo",
            $"Echo from Dreamine TCP Server - {receiveText}",
            encodingName);

        RunOnUiThread(() =>
        {
            Monitor.AddSendLog(
                channelName,
                TransportKind.Tcp,
                echoMessage);
        });

        await _tcpServer.SendAsync(echoMessage);
    }

    private void OnTcpClientMessageReceived(object? sender, MessageEnvelope message)
    {
        var protocol = _currentClientProtocol;
        var encodingName = _currentClientEncoding;
        var channelName = GetTcpClientChannelName(protocol, encodingName);

        RunOnUiThread(() =>
        {
            Monitor.AddReceiveLog(
                channelName,
                TransportKind.Tcp,
                message);
        });
    }

    private void OnUdpPeerAMessageReceived(object? sender, MessageEnvelope message)
    {
        var protocol = _currentUdpPeerAProtocol;
        var encodingName = _currentUdpPeerAEncoding;
        var channelName = GetUdpPeerChannelName("A", protocol, encodingName);

        RunOnUiThread(() =>
        {
            Monitor.AddReceiveLog(
                channelName,
                TransportKind.Udp,
                message);
        });
    }

    private void OnUdpPeerBMessageReceived(object? sender, MessageEnvelope message)
    {
        var protocol = _currentUdpPeerBProtocol;
        var encodingName = _currentUdpPeerBEncoding;
        var channelName = GetUdpPeerChannelName("B", protocol, encodingName);

        RunOnUiThread(() =>
        {
            Monitor.AddReceiveLog(
                channelName,
                TransportKind.Udp,
                message);
        });
    }

    private void OnSerialMessageReceived(object? sender, MessageEnvelope message)
    {
        var channelName = GetSerialChannelName();

        RunOnUiThread(() =>
        {
            Monitor.AddReceiveLog(
                channelName,
                TransportKind.Serial,
                message);
        });
    }

    private void EnsureTcpServerChannel(
        string protocol,
        string encodingName = PlainTextProtocolOptions.Utf8EncodingName)
    {
        var channelName = GetTcpServerChannelName(protocol, encodingName);
        var port = GetTcpPort(protocol);

        if (Monitor.Channels.Any(x => x.Name == channelName))
        {
            return;
        }

        Monitor.AddChannel(
            channelName,
            TransportKind.Tcp,
            $"TCP server [{protocol}/{NormalizeTextEncodingName(encodingName)}] on 127.0.0.1:{port}.");
    }

    private void EnsureTcpClientChannel(
        string protocol,
        string encodingName = PlainTextProtocolOptions.Utf8EncodingName)
    {
        var channelName = GetTcpClientChannelName(protocol, encodingName);
        var port = GetTcpPort(protocol);

        if (Monitor.Channels.Any(x => x.Name == channelName))
        {
            return;
        }

        Monitor.AddChannel(
            channelName,
            TransportKind.Tcp,
            $"TCP client [{protocol}/{NormalizeTextEncodingName(encodingName)}] to 127.0.0.1:{port}.");
    }

    private void EnsureUdpPeerChannel(
        string peerName,
        string protocol,
        string encodingName)
    {
        encodingName = NormalizeTextEncodingName(encodingName);

        var channelName = GetUdpPeerChannelName(peerName, protocol, encodingName);
        var localPort = GetUdpLocalPort(peerName);
        var remotePort = GetUdpRemotePort(peerName);

        if (Monitor.Channels.Any(x => x.Name == channelName))
        {
            return;
        }

        Monitor.AddChannel(
            channelName,
            TransportKind.Udp,
            $"UDP peer {peerName} [{protocol}/{encodingName}] 127.0.0.1:{localPort} -> 127.0.0.1:{remotePort}.");
    }

    private void EnsureSerialChannel()
    {
        var channelName = GetSerialChannelName();

        if (Monitor.Channels.Any(x => x.Name == channelName))
        {
            return;
        }

        Monitor.AddChannel(
            channelName,
            TransportKind.Serial,
            $"Serial [{_currentSerialProtocol}/{_currentSerialEncoding}] on {_currentSerialPortName}:{_currentSerialBaudRate}.");
    }

    private void EnsureRabbitMqChannel()
    {
        if (Monitor.Channels.Any(x => x.Name == RabbitMqChannelName))
        {
            return;
        }

        Monitor.AddChannel(
            RabbitMqChannelName,
            TransportKind.RabbitMq,
            $"RabbitMQ on {_currentRabbitMqHost}:{_currentRabbitMqPort}, vhost={_currentRabbitMqVirtualHost}, exchange={_currentRabbitMqExchangeName}, queue={_currentRabbitMqQueueName}, route={_currentRabbitMqRoutingKey}.");
    }

    private string GetSerialChannelName()
    {
        if (string.IsNullOrWhiteSpace(_currentSerialPortName))
        {
            return "Serial-Port";
        }

        return $"Serial-{_currentSerialPortName}";
    }

    private static IMessageProtocolAdapter CreateProtocolAdapter(
        string protocol,
        string routePrefix = "tcp",
        string namePrefix = "Tcp",
        string encodingName = PlainTextProtocolOptions.Utf8EncodingName)
    {
        var normalizedProtocol = NormalizeProtocol(protocol);
        var textEncoding = PlainTextProtocolOptions.CreateEncoding(encodingName);

        return normalizedProtocol switch
        {
            DreamineEnvelopeProtocol => new DreamineEnvelopeProtocolAdapter(),

            PlainTextProtocol => new PlainTextProtocolAdapter(
                textEncoding,
                $"{routePrefix}.plaintext",
                $"{namePrefix}.PlainText"),

            RawAvailableProtocol => new PlainTextProtocolAdapter(
                textEncoding,
                $"{routePrefix}.raw.available",
                $"{namePrefix}.RawAvailable"),

            RawJsonProtocol => new RawJsonProtocolAdapter(
                textEncoding,
                $"{routePrefix}.rawjson",
                $"{namePrefix}.RawJson"),

            _ => new PlainTextProtocolAdapter(
                textEncoding,
                $"{routePrefix}.plaintext",
                $"{namePrefix}.PlainText")
        };
    }

    private static IMessageFrameCodec CreateFrameCodec(
        string protocol,
        string encodingName = PlainTextProtocolOptions.Utf8EncodingName)
    {
        var delimiterEncoding = PlainTextProtocolOptions.CreateEncoding(encodingName);

        return NormalizeProtocol(protocol) switch
        {
            DreamineEnvelopeProtocol => new LengthPrefixedMessageFrameCodec(),

            PlainTextProtocol => new DelimiterMessageFrameCodec(
                "\r\n",
                delimiterEncoding,
                1024 * 1024),

            RawAvailableProtocol => new RawAvailableMessageFrameCodec(),

            RawJsonProtocol => new DelimiterMessageFrameCodec(
                "\r\n",
                Encoding.UTF8,
                1024 * 1024),

            _ => new DelimiterMessageFrameCodec(
                "\r\n",
                delimiterEncoding,
                1024 * 1024)
        };
    }

    private static int GetTcpPort(string protocol)
    {
        return NormalizeProtocol(protocol) switch
        {
            DreamineEnvelopeProtocol => DreamineProtocolPort,
            PlainTextProtocol => PlainTextProtocolPort,
            RawAvailableProtocol => RawAvailableProtocolPort,
            RawJsonProtocol => RawJsonProtocolPort,
            _ => PlainTextProtocolPort
        };
    }

    private static string GetTcpServerChannelName(
        string protocol,
        string encodingName = PlainTextProtocolOptions.Utf8EncodingName)
    {
        var normalizedEncodingName = NormalizeTextEncodingName(encodingName);

        return NormalizeProtocol(protocol) switch
        {
            DreamineEnvelopeProtocol => "TCP-Server-Dreamine",
            PlainTextProtocol => $"TCP-Server-PlainText-{normalizedEncodingName}",
            RawAvailableProtocol => $"TCP-Server-RawAvailable-{normalizedEncodingName}",
            RawJsonProtocol => "TCP-Server-RawJson",
            _ => $"TCP-Server-PlainText-{normalizedEncodingName}"
        };
    }

    private static string GetTcpClientChannelName(
        string protocol,
        string encodingName = PlainTextProtocolOptions.Utf8EncodingName)
    {
        var normalizedEncodingName = NormalizeTextEncodingName(encodingName);

        return NormalizeProtocol(protocol) switch
        {
            DreamineEnvelopeProtocol => "TCP-Client-Dreamine",
            PlainTextProtocol => $"TCP-Client-PlainText-{normalizedEncodingName}",
            RawAvailableProtocol => $"TCP-Client-RawAvailable-{normalizedEncodingName}",
            RawJsonProtocol => "TCP-Client-RawJson",
            _ => $"TCP-Client-PlainText-{normalizedEncodingName}"
        };
    }

    private static string GetUdpPeerChannelName(
        string peerName,
        string protocol,
        string encodingName = PlainTextProtocolOptions.Utf8EncodingName)
    {
        var normalizedPeerName = NormalizeUdpPeerName(peerName);
        var normalizedEncodingName = NormalizeTextEncodingName(encodingName);

        return NormalizeProtocol(protocol) switch
        {
            DreamineEnvelopeProtocol => $"UDP-Peer{normalizedPeerName}-Dreamine",
            PlainTextProtocol => $"UDP-Peer{normalizedPeerName}-PlainText-{normalizedEncodingName}",
            RawAvailableProtocol => $"UDP-Peer{normalizedPeerName}-RawAvailable-{normalizedEncodingName}",
            RawJsonProtocol => $"UDP-Peer{normalizedPeerName}-RawJson",
            _ => $"UDP-Peer{normalizedPeerName}-PlainText-{normalizedEncodingName}"
        };
    }
    private static string NormalizeTextEncodingName(string encodingName)
    {
        if (string.Equals(encodingName, PlainTextProtocolOptions.KoreanCodePage949EncodingName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(encodingName, "949", StringComparison.OrdinalIgnoreCase))
        {
            return PlainTextProtocolOptions.KoreanCodePage949EncodingName;
        }

        return PlainTextProtocolOptions.Utf8EncodingName;
    }


    private static int GetUdpLocalPort(string peerName)
    {
        return NormalizeUdpPeerName(peerName) == "B"
            ? UdpPeerBLocalPort
            : UdpPeerALocalPort;
    }

    private static int GetUdpRemotePort(string peerName)
    {
        return NormalizeUdpPeerName(peerName) == "B"
            ? UdpPeerALocalPort
            : UdpPeerBLocalPort;
    }

    private static string NormalizeUdpPeerName(string peerName)
    {
        return string.Equals(peerName, "B", StringComparison.OrdinalIgnoreCase)
            ? "B"
            : "A";
    }

    private static MessageEnvelope CreateTcpMessageByProtocol(
        string protocol,
        string direction,
        string text,
        string encodingName = PlainTextProtocolOptions.Utf8EncodingName)
    {
        var normalizedProtocol = NormalizeProtocol(protocol);
        var normalizedEncodingName = NormalizeTextEncodingName(encodingName);
        var payload = Encoding.UTF8.GetBytes(text);

        return normalizedProtocol switch
        {
            DreamineEnvelopeProtocol => new MessageEnvelope
            {
                Name = $"Tcp.{direction}.DreamineEnvelope",
                Route = "sample.communication.tcp",
                Payload = payload,
                Headers = new Dictionary<string, string>
                {
                    ["Protocol"] = DreamineEnvelopeProtocol
                }
            },

            PlainTextProtocol => new MessageEnvelope
            {
                Name = $"Tcp.{direction}.PlainText",
                Route = "tcp.plaintext",
                Payload = payload,
                Headers = new Dictionary<string, string>
                {
                    ["ContentType"] = "text/plain",
                    ["Protocol"] = PlainTextProtocol,
                    ["ExternalEncoding"] = normalizedEncodingName
                }
            },

            RawAvailableProtocol => new MessageEnvelope
            {
                Name = $"Tcp.{direction}.RawAvailable",
                Route = "tcp.raw.available",
                Payload = payload,
                Headers = new Dictionary<string, string>
                {
                    ["ContentType"] = "text/plain",
                    ["Protocol"] = RawAvailableProtocol,
                    ["ExternalEncoding"] = normalizedEncodingName
                }
            },

            RawJsonProtocol => new MessageEnvelope
            {
                Name = $"Tcp.{direction}.RawJson",
                Route = "tcp.rawjson",
                Payload = EnsureJsonPayload(text),
                Headers = new Dictionary<string, string>
                {
                    ["ContentType"] = "application/json",
                    ["Protocol"] = RawJsonProtocol
                }
            },

            _ => new MessageEnvelope
            {
                Name = $"Tcp.{direction}.PlainText",
                Route = "tcp.plaintext",
                Payload = payload,
                Headers = new Dictionary<string, string>
                {
                    ["ContentType"] = "text/plain",
                    ["Protocol"] = PlainTextProtocol,
                    ["ExternalEncoding"] = normalizedEncodingName
                }
            }
        };
    }

    private static MessageEnvelope CreateUdpMessageByProtocol(
        string protocol,
        string direction,
        string text,
        string encodingName = PlainTextProtocolOptions.Utf8EncodingName)
    {
        var normalizedProtocol = NormalizeProtocol(protocol);
        var normalizedEncodingName = NormalizeTextEncodingName(encodingName);
        var payload = Encoding.UTF8.GetBytes(text);

        return normalizedProtocol switch
        {
            DreamineEnvelopeProtocol => new MessageEnvelope
            {
                Name = $"Udp.{direction}.DreamineEnvelope",
                Route = "sample.communication.udp",
                Payload = payload,
                Headers = new Dictionary<string, string>
                {
                    ["Protocol"] = DreamineEnvelopeProtocol
                }
            },

            PlainTextProtocol => new MessageEnvelope
            {
                Name = $"Udp.{direction}.PlainText",
                Route = "udp.plaintext",
                Payload = payload,
                Headers = new Dictionary<string, string>
                {
                    ["ContentType"] = "text/plain",
                    ["Protocol"] = PlainTextProtocol,
                    ["ExternalEncoding"] = normalizedEncodingName
                }
            },

            RawAvailableProtocol => new MessageEnvelope
            {
                Name = $"Udp.{direction}.RawAvailable",
                Route = "udp.raw.available",
                Payload = payload,
                Headers = new Dictionary<string, string>
                {
                    ["ContentType"] = "text/plain",
                    ["Protocol"] = RawAvailableProtocol,
                    ["ExternalEncoding"] = normalizedEncodingName
                }
            },

            RawJsonProtocol => new MessageEnvelope
            {
                Name = $"Udp.{direction}.RawJson",
                Route = "udp.rawjson",
                Payload = EnsureJsonPayload(text),
                Headers = new Dictionary<string, string>
                {
                    ["ContentType"] = "application/json",
                    ["Protocol"] = RawJsonProtocol
                }
            },

            _ => new MessageEnvelope
            {
                Name = $"Udp.{direction}.PlainText",
                Route = "udp.plaintext",
                Payload = payload,
                Headers = new Dictionary<string, string>
                {
                    ["ContentType"] = "text/plain",
                    ["Protocol"] = PlainTextProtocol,
                    ["ExternalEncoding"] = normalizedEncodingName
                }
            }
        };
    }

    private static MessageEnvelope CreateSerialMessageByProtocol(
        string protocol,
        string direction,
        string text,
        string encodingName = PlainTextProtocolOptions.Utf8EncodingName)
    {
        var normalizedProtocol = NormalizeProtocol(protocol);
        var normalizedEncodingName = NormalizeTextEncodingName(encodingName);
        var payload = Encoding.UTF8.GetBytes(text);

        return normalizedProtocol switch
        {
            DreamineEnvelopeProtocol => new MessageEnvelope
            {
                Name = $"Serial.{direction}.DreamineEnvelope",
                Route = "sample.communication.serial",
                Payload = payload,
                Headers = new Dictionary<string, string>
                {
                    ["Protocol"] = DreamineEnvelopeProtocol
                }
            },

            PlainTextProtocol => new MessageEnvelope
            {
                Name = $"Serial.{direction}.PlainText",
                Route = "serial.plaintext",
                Payload = payload,
                Headers = new Dictionary<string, string>
                {
                    ["ContentType"] = "text/plain",
                    ["Protocol"] = PlainTextProtocol,
                    ["ExternalEncoding"] = normalizedEncodingName
                }
            },

            RawAvailableProtocol => new MessageEnvelope
            {
                Name = $"Serial.{direction}.RawAvailable",
                Route = "serial.raw.available",
                Payload = payload,
                Headers = new Dictionary<string, string>
                {
                    ["ContentType"] = "text/plain",
                    ["Protocol"] = RawAvailableProtocol,
                    ["ExternalEncoding"] = normalizedEncodingName
                }
            },

            RawJsonProtocol => new MessageEnvelope
            {
                Name = $"Serial.{direction}.RawJson",
                Route = "serial.rawjson",
                Payload = EnsureJsonPayload(text),
                Headers = new Dictionary<string, string>
                {
                    ["ContentType"] = "application/json",
                    ["Protocol"] = RawJsonProtocol
                }
            },

            _ => new MessageEnvelope
            {
                Name = $"Serial.{direction}.PlainText",
                Route = "serial.plaintext",
                Payload = payload,
                Headers = new Dictionary<string, string>
                {
                    ["ContentType"] = "text/plain",
                    ["Protocol"] = PlainTextProtocol,
                    ["ExternalEncoding"] = normalizedEncodingName
                }
            }
        };
    }

    private static MessageEnvelope CreateRabbitMqMessage(
        string direction,
        string route,
        string text)
    {
        return new MessageEnvelope
        {
            Name = $"RabbitMQ.{direction}",
            Route = route,
            Payload = Encoding.UTF8.GetBytes(text),
            Headers = new Dictionary<string, string>
            {
                ["ContentType"] = "text/plain",
                ["Protocol"] = RabbitMqProtocol
            }
        };
    }

    private static MessageEnvelope CreateTextMessage(
        string name,
        string route,
        string text,
        string protocol)
    {
        return new MessageEnvelope
        {
            Name = name,
            Route = route,
            Payload = Encoding.UTF8.GetBytes(text),
            Headers = new Dictionary<string, string>
            {
                ["ContentType"] = "text/plain",
                ["Protocol"] = protocol
            }
        };
    }

    private void AddRabbitMqErrorLog(
        string name,
        string text)
    {
        var message = CreateTextMessage(
            name,
            _currentRabbitMqRoutingKey,
            text,
            RabbitMqProtocol);

        Monitor.AddReceiveLog(
            RabbitMqChannelName,
            TransportKind.RabbitMq,
            message);
    }

    private static byte[] EnsureJsonPayload(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            text = "{\"cmd\":\"PING\"}";
        }

        var trimmed = text.Trim();

        if (trimmed.StartsWith("{", StringComparison.Ordinal) ||
            trimmed.StartsWith("[", StringComparison.Ordinal))
        {
            return Encoding.UTF8.GetBytes(trimmed);
        }

        var json = $$"""
        {"message":"{{EscapeJsonString(text)}}"}
        """;

        return Encoding.UTF8.GetBytes(json);
    }

    private static string EscapeJsonString(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
    }

    private static string NormalizeProtocol(string? protocol)
    {
        if (string.IsNullOrWhiteSpace(protocol))
        {
            return PlainTextProtocol;
        }

        return protocol.Trim() switch
        {
            DreamineEnvelopeProtocol => DreamineEnvelopeProtocol,
            PlainTextProtocol => PlainTextProtocol,
            RawAvailableProtocol => RawAvailableProtocol,
            RawJsonProtocol => RawJsonProtocol,
            _ => PlainTextProtocol
        };
    }

    private static string NormalizeText(
        string? value,
        string defaultValue)
    {
        return string.IsNullOrWhiteSpace(value)
            ? defaultValue
            : value.Trim();
    }

    private static void RunOnUiThread(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var dispatcher = Application.Current?.Dispatcher;

        if (dispatcher is null || dispatcher.CheckAccess())
        {
            action();
            return;
        }

        dispatcher.Invoke(action);
    }
}