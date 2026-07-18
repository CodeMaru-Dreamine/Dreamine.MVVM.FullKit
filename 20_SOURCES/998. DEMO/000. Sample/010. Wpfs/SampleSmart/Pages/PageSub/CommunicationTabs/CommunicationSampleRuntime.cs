using System.Text;
using System.Windows;
using Dreamine.Communication.Abstractions.Enums;
using Dreamine.Communication.Abstractions.Interfaces;
using Dreamine.Communication.Abstractions.Models;
using Dreamine.Communication.Abstractions.Options;
using Dreamine.Communication.Core.Framing;
using Dreamine.Communication.Core.Protocols;
using Dreamine.Communication.Core.Resilience;
using Dreamine.Communication.RabbitMQ.Buses;
using Dreamine.Communication.RabbitMQ.Options;
using Dreamine.Communication.Serial.Options;
using Dreamine.Communication.Serial.Ports;
using Dreamine.Communication.Sockets.Clients;
using Dreamine.Communication.Sockets.Enums;
using Dreamine.Communication.Sockets.Options;
using Dreamine.Communication.Sockets.Servers;
using Dreamine.Communication.Sockets.Udp;
using Dreamine.Communication.Wpf.ViewModels;

namespace SampleSmart.Pages.PageSub.CommunicationTabs;

/// <summary>
/// \if KO
/// <para>\brief Communication 샘플 전체에서 공유되는 Runtime 컨텍스트입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates communication sample runtime functionality and related state.</para>
/// \endif
/// </summary>
/// <remarks>
/// \if KO
/// <para>이 클래스는 Communication 샘플 탭들이 공유하는 Monitor, InMemory MessageBus, TCP Server, TCP Client, Serial Port, RabbitMQ MessageBus 인스턴스를 관리합니다.</para>
/// \endif
/// \if EN
/// <para>Describes behavior and usage considerations for this member.</para>
/// \endif
/// </remarks>
public sealed class CommunicationSampleRuntime
{
    /// <summary>
    /// \if KO
    /// <para>In Memory Channel Name 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the in memory channel name value.</para>
    /// \endif
    /// </summary>
    private const string InMemoryChannelName = "InMemory-Communication";
    /// <summary>
    /// \if KO
    /// <para>In Memory Route Name 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the in memory route name value.</para>
    /// \endif
    /// </summary>
    private const string InMemoryRouteName = "sample.communication.message";

    /// <summary>
    /// \if KO
    /// <para>Rabbit Mq Channel Name 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the rabbit mq channel name value.</para>
    /// \endif
    /// </summary>
    private const string RabbitMqChannelName = "RabbitMQ-MessageBus";
    /// <summary>
    /// \if KO
    /// <para>Rabbit Mq Protocol 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the rabbit mq protocol value.</para>
    /// \endif
    /// </summary>
    private const string RabbitMqProtocol = "RabbitMQ";

    /// <summary>
    /// \if KO
    /// <para>Dreamine Envelope Protocol 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the dreamine envelope protocol value.</para>
    /// \endif
    /// </summary>
    private const string DreamineEnvelopeProtocol = "DreamineEnvelope";
    /// <summary>
    /// \if KO
    /// <para>Plain Text Protocol 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the plain text protocol value.</para>
    /// \endif
    /// </summary>
    private const string PlainTextProtocol = "PlainText";
    /// <summary>
    /// \if KO
    /// <para>Raw Available Protocol 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the raw available protocol value.</para>
    /// \endif
    /// </summary>
    private const string RawAvailableProtocol = "RawAvailable";
    /// <summary>
    /// \if KO
    /// <para>Raw Json Protocol 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the raw json protocol value.</para>
    /// \endif
    /// </summary>
    private const string RawJsonProtocol = "RawJson";

    /// <summary>
    /// \if KO
    /// <para>Dreamine Protocol Port 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the dreamine protocol port value.</para>
    /// \endif
    /// </summary>
    private const int DreamineProtocolPort = 15001;
    /// <summary>
    /// \if KO
    /// <para>Plain Text Protocol Port 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the plain text protocol port value.</para>
    /// \endif
    /// </summary>
    private const int PlainTextProtocolPort = 15002;
    /// <summary>
    /// \if KO
    /// <para>Raw Available Protocol Port 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the raw available protocol port value.</para>
    /// \endif
    /// </summary>
    private const int RawAvailableProtocolPort = 15002;
    /// <summary>
    /// \if KO
    /// <para>Raw Json Protocol Port 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the raw json protocol port value.</para>
    /// \endif
    /// </summary>
    private const int RawJsonProtocolPort = 15003;

    /// <summary>
    /// \if KO
    /// <para>Udp Peer A Local Port 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the udp peer a local port value.</para>
    /// \endif
    /// </summary>
    private const int UdpPeerALocalPort = 16001;
    /// <summary>
    /// \if KO
    /// <para>Udp Peer B Local Port 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the udp peer b local port value.</para>
    /// \endif
    /// </summary>
    private const int UdpPeerBLocalPort = 16002;

    /// <summary>
    /// \if KO
    /// <para>message Bus 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the message bus value.</para>
    /// \endif
    /// </summary>
    private readonly IMessageBus _messageBus;

    /// <summary>
    /// \if KO
    /// <para>is In Memory Subscribed 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the is in memory subscribed value.</para>
    /// \endif
    /// </summary>
    private bool _isInMemorySubscribed;
    /// <summary>
    /// \if KO
    /// <para>is Rabbit Mq Subscribed 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the is rabbit mq subscribed value.</para>
    /// \endif
    /// </summary>
    private bool _isRabbitMqSubscribed;

    /// <summary>
    /// \if KO
    /// <para>tcp Server 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the tcp server value.</para>
    /// \endif
    /// </summary>
    private IMessageTransport? _tcpServer;
    /// <summary>
    /// \if KO
    /// <para>raw Tcp Server 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the raw tcp server value.</para>
    /// \endif
    /// </summary>
    private TcpServerTransport? _rawTcpServer;
    /// <summary>
    /// \if KO
    /// <para>tcp Client 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the tcp client value.</para>
    /// \endif
    /// </summary>
    private IMessageTransport? _tcpClient;
    /// <summary>
    /// \if KO
    /// <para>udp Peer A 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the udp peer a value.</para>
    /// \endif
    /// </summary>
    private UdpTransport? _udpPeerA;
    /// <summary>
    /// \if KO
    /// <para>udp Peer B 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the udp peer b value.</para>
    /// \endif
    /// </summary>
    private UdpTransport? _udpPeerB;
    /// <summary>
    /// \if KO
    /// <para>serial Transport 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the serial transport value.</para>
    /// \endif
    /// </summary>
    private SerialPortTransport? _serialTransport;
    /// <summary>
    /// \if KO
    /// <para>rabbit Mq Bus 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the rabbit mq bus value.</para>
    /// \endif
    /// </summary>
    private RabbitMqMessageBus? _rabbitMqBus;

    /// <summary>
    /// \if KO
    /// <para>current Server Protocol 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the current server protocol value.</para>
    /// \endif
    /// </summary>
    private string _currentServerProtocol = PlainTextProtocol;
    /// <summary>
    /// \if KO
    /// <para>current Client Protocol 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the current client protocol value.</para>
    /// \endif
    /// </summary>
    private string _currentClientProtocol = PlainTextProtocol;
    /// <summary>
    /// \if KO
    /// <para>current Server Encoding 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the current server encoding value.</para>
    /// \endif
    /// </summary>
    private string _currentServerEncoding = PlainTextProtocolOptions.Utf8EncodingName;
    /// <summary>
    /// \if KO
    /// <para>current Client Encoding 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the current client encoding value.</para>
    /// \endif
    /// </summary>
    private string _currentClientEncoding = PlainTextProtocolOptions.Utf8EncodingName;
    /// <summary>
    /// \if KO
    /// <para>current Server Send Target Mode 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the current server send target mode value.</para>
    /// \endif
    /// </summary>
    private TcpServerSendTargetMode _currentServerSendTargetMode = TcpServerSendTargetMode.Broadcast;
    /// <summary>
    /// \if KO
    /// <para>current Server Echo Enabled 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the current server echo enabled value.</para>
    /// \endif
    /// </summary>
    private bool _currentServerEchoEnabled;
    /// <summary>
    /// \if KO
    /// <para>current Udp Peer A Protocol 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the current udp peer a protocol value.</para>
    /// \endif
    /// </summary>
    private string _currentUdpPeerAProtocol = PlainTextProtocol;
    /// <summary>
    /// \if KO
    /// <para>current Udp Peer B Protocol 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the current udp peer b protocol value.</para>
    /// \endif
    /// </summary>
    private string _currentUdpPeerBProtocol = PlainTextProtocol;
    /// <summary>
    /// \if KO
    /// <para>current Udp Peer A Encoding 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the current udp peer a encoding value.</para>
    /// \endif
    /// </summary>
    private string _currentUdpPeerAEncoding = PlainTextProtocolOptions.Utf8EncodingName;
    /// <summary>
    /// \if KO
    /// <para>current Udp Peer B Encoding 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the current udp peer b encoding value.</para>
    /// \endif
    /// </summary>
    private string _currentUdpPeerBEncoding = PlainTextProtocolOptions.Utf8EncodingName;

    /// <summary>
    /// \if KO
    /// <para>current Serial Protocol 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the current serial protocol value.</para>
    /// \endif
    /// </summary>
    private string _currentSerialProtocol = RawAvailableProtocol;
    /// <summary>
    /// \if KO
    /// <para>current Serial Encoding 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the current serial encoding value.</para>
    /// \endif
    /// </summary>
    private string _currentSerialEncoding = PlainTextProtocolOptions.Utf8EncodingName;
    /// <summary>
    /// \if KO
    /// <para>current Serial Port Name 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the current serial port name value.</para>
    /// \endif
    /// </summary>
    private string _currentSerialPortName = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>current Serial Baud Rate 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the current serial baud rate value.</para>
    /// \endif
    /// </summary>
    private int _currentSerialBaudRate = 9600;

    /// <summary>
    /// \if KO
    /// <para>current Rabbit Mq Host 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the current rabbit mq host value.</para>
    /// \endif
    /// </summary>
    private string _currentRabbitMqHost = "localhost";
    /// <summary>
    /// \if KO
    /// <para>current Rabbit Mq Port 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the current rabbit mq port value.</para>
    /// \endif
    /// </summary>
    private int _currentRabbitMqPort = 5672;
    /// <summary>
    /// \if KO
    /// <para>current Rabbit Mq Virtual Host 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the current rabbit mq virtual host value.</para>
    /// \endif
    /// </summary>
    private string _currentRabbitMqVirtualHost = "/";
    /// <summary>
    /// \if KO
    /// <para>current Rabbit Mq Exchange Name 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the current rabbit mq exchange name value.</para>
    /// \endif
    /// </summary>
    private string _currentRabbitMqExchangeName = "dreamine.sample.exchange";
    /// <summary>
    /// \if KO
    /// <para>current Rabbit Mq Queue Name 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the current rabbit mq queue name value.</para>
    /// \endif
    /// </summary>
    private string _currentRabbitMqQueueName = "dreamine.sample.queue";
    /// <summary>
    /// \if KO
    /// <para>current Rabbit Mq Routing Key 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the current rabbit mq routing key value.</para>
    /// \endif
    /// </summary>
    private string _currentRabbitMqRoutingKey = "dreamine.sample.route";

    /// <summary>
    /// \if KO
    /// <para>\brief CommunicationSampleRuntime 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="CommunicationSampleRuntime"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="messageBus">
    /// \if KO
    /// <para>InMemory 샘플에 사용할 MessageBus입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IMessageBus</c> value used for message bus.</para>
    /// \endif
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// \if KO
    /// <para>필수 입력 인자 중 하나가 <see langword="null"/>인 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when a required input argument is <see langword="null"/>.</para>
    /// \endif
    /// </exception>
    public CommunicationSampleRuntime(IMessageBus messageBus)
    {
        _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
        Monitor = new CommunicationMonitorViewModel();

        _ = SubscribeInMemoryAsync();
    }

    /// <summary>
    /// \if KO
    /// <para>\brief Communication Monitor ViewModel입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the monitor value.</para>
    /// \endif
    /// </summary>
    public CommunicationMonitorViewModel Monitor { get; }

    /// <summary>
    /// \if KO
    /// <para>\brief 선택 가능한 TCP 프로토콜 목록입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the tcp protocols value.</para>
    /// \endif
    /// </summary>
    public IReadOnlyList<string> TcpProtocols { get; } =
    [
        DreamineEnvelopeProtocol,
        PlainTextProtocol,
        RawAvailableProtocol,
        RawJsonProtocol
    ];

    /// <summary>
    /// \if KO
    /// <para>\brief 선택 가능한 Serial 프로토콜 목록입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the serial protocols value.</para>
    /// \endif
    /// </summary>
    public IReadOnlyList<string> SerialProtocols { get; } =
    [
        PlainTextProtocol,
        RawAvailableProtocol,
        RawJsonProtocol
    ];

    /// <summary>
    /// \if KO
    /// <para>\brief 선택 가능한 UDP 프로토콜 목록입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the udp protocols value.</para>
    /// \endif
    /// </summary>
    public IReadOnlyList<string> UdpProtocols { get; } =
    [
        DreamineEnvelopeProtocol,
        PlainTextProtocol,
        RawAvailableProtocol,
        RawJsonProtocol
    ];

    /// <summary>
    /// \if KO
    /// <para>\brief 선택 가능한 외부 PlainText 인코딩 목록입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the text encodings value.</para>
    /// \endif
    /// </summary>
    public IReadOnlyList<string> TextEncodings { get; } =
    [
        PlainTextProtocolOptions.Utf8EncodingName,
        PlainTextProtocolOptions.KoreanCodePage949EncodingName
    ];

    /// <summary>
    /// \if KO
    /// <para>\brief 선택 가능한 TCP Server 송신 대상 정책 목록입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the tcp server send target modes value.</para>
    /// \endif
    /// </summary>
    public IReadOnlyList<string> TcpServerSendTargetModes { get; } =
    [
        nameof(TcpServerSendTargetMode.Broadcast),
        nameof(TcpServerSendTargetMode.FirstClient),
        nameof(TcpServerSendTargetMode.LastClient)
    ];

    /// <summary>
    /// \if KO
    /// <para>\brief 선택 가능한 UDP PlainText 인코딩 목록입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the udp text encodings value.</para>
    /// \endif
    /// </summary>
    public IReadOnlyList<string> UdpTextEncodings => TextEncodings;

    /// <summary>
    /// \if KO
    /// <para>\brief InMemory 채널을 추가합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Adds the in memory channel item.</para>
    /// \endif
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
    /// \if KO
    /// <para>\brief InMemory MessageBus에 연결합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the connect in memory async operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Connect In Memory Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the connect in memory async operation.</para>
    /// \endif
    /// </returns>
    public async Task ConnectInMemoryAsync()
    {
        AddInMemoryChannel();

        await _messageBus.ConnectAsync();

        Monitor.UpdateChannelState(
            InMemoryChannelName,
            ConnectionState.Connected);
    }

    /// <summary>
    /// \if KO
    /// <para>\brief InMemory MessageBus 연결을 해제합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the disconnect in memory async operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Disconnect In Memory Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the disconnect in memory async operation.</para>
    /// \endif
    /// </returns>
    public async Task DisconnectInMemoryAsync()
    {
        AddInMemoryChannel();

        await _messageBus.DisconnectAsync();

        Monitor.UpdateChannelState(
            InMemoryChannelName,
            ConnectionState.Disconnected);
    }

    /// <summary>
    /// \if KO
    /// <para>\brief InMemory MessageBus로 테스트 메시지를 송신합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the send in memory async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="text">
    /// \if KO
    /// <para>송신 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for text.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Send In Memory Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the send in memory async operation.</para>
    /// \endif
    /// </returns>
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
    /// \if KO
    /// <para>\brief InMemory 수동 수신 테스트 로그를 추가합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the receive in memory operation.</para>
    /// \endif
    /// </summary>
    /// <param name="text">
    /// \if KO
    /// <para>수신으로 표시할 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for text.</para>
    /// \endif
    /// </param>
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
    /// \if KO
    /// <para>\brief 선택된 프로토콜로 TCP Server를 시작합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the start tcp server async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="protocol">
    /// \if KO
    /// <para>프로토콜 이름입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for protocol.</para>
    /// \endif
    /// </param>
    /// <param name="encodingName">
    /// \if KO
    /// <para>PlainText 외부 송수신 인코딩 이름입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for encoding name.</para>
    /// \endif
    /// </param>
    /// <param name="sendTargetMode">
    /// \if KO
    /// <para>send Target Mode에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for send target mode.</para>
    /// \endif
    /// </param>
    /// <param name="echoEnabled">
    /// \if KO
    /// <para>echo Enabled에 사용할 <c>bool</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>bool</c> value used for echo enabled.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Start Tcp Server Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the start tcp server async operation.</para>
    /// \endif
    /// </returns>
    public async Task StartTcpServerAsync(
        string protocol,
        string encodingName = PlainTextProtocolOptions.Utf8EncodingName,
        string sendTargetMode = nameof(TcpServerSendTargetMode.Broadcast),
        bool echoEnabled = false)
    {
        protocol = NormalizeProtocol(protocol);
        encodingName = NormalizeTextEncodingName(encodingName);
        var targetMode = NormalizeTcpServerSendTargetMode(sendTargetMode);

        if (_tcpServer is not null &&
            _tcpServer.State == ConnectionState.Listening &&
            _currentServerProtocol == protocol &&
            _currentServerEncoding == encodingName)
        {
            _currentServerSendTargetMode = targetMode;
            _currentServerEchoEnabled = echoEnabled;

            if (_rawTcpServer is not null)
            {
                _rawTcpServer.SendTargetMode = targetMode;
                UpdateTcpServerDescription(
                    protocol,
                    encodingName,
                    _rawTcpServer.ConnectedClientCount,
                    targetMode,
                    echoEnabled);
            }

            return;
        }

        if (_tcpServer is not null)
        {
            await StopTcpServerAsync();
        }

        _currentServerProtocol = protocol;
        _currentServerEncoding = encodingName;
        _currentServerSendTargetMode = targetMode;
        _currentServerEchoEnabled = echoEnabled;

        EnsureTcpServerChannel(protocol, encodingName);

        var port = GetTcpPort(protocol);

        _rawTcpServer = new TcpServerTransport(
            new TcpServerTransportOptions
            {
                Host = "127.0.0.1",
                Port = port,
                SendTargetMode = targetMode
            },
            CreateProtocolAdapter(
                protocol,
                "tcp",
                "Tcp",
                encodingName),
            CreateFrameCodec(protocol, encodingName));

        _tcpServer = new ResilientMessageTransport(
            _rawTcpServer,
            CreateSampleReconnectPolicy(),
            CreateSampleServerOutboundQueueOptions());

        var channelName = GetTcpServerChannelName(protocol, encodingName);

        AttachTcpResilientStateMonitor(
            _tcpServer,
            channelName);

        AttachTcpServerClientCountMonitor(
            _rawTcpServer,
            protocol,
            encodingName);

        UpdateTcpServerDescription(
            protocol,
            encodingName,
            _rawTcpServer.ConnectedClientCount,
            targetMode,
            echoEnabled);

        _tcpServer.MessageReceived += OnTcpServerMessageReceived;

        Monitor.UpdateChannelState(channelName, ConnectionState.Connecting);

        await _tcpServer.ConnectAsync();

        Monitor.UpdateChannelState(
            channelName,
            _tcpServer.State);
    }

    /// <summary>
    /// \if KO
    /// <para>\brief 실행 중인 TCP Server의 송신 대상과 Echo 옵션을 갱신합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the update tcp server options operation.</para>
    /// \endif
    /// </summary>
    /// <param name="protocol">
    /// \if KO
    /// <para>프로토콜 이름입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for protocol.</para>
    /// \endif
    /// </param>
    /// <param name="encodingName">
    /// \if KO
    /// <para>PlainText 외부 송수신 인코딩 이름입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for encoding name.</para>
    /// \endif
    /// </param>
    /// <param name="sendTargetMode">
    /// \if KO
    /// <para>서버 송신 대상 정책입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for send target mode.</para>
    /// \endif
    /// </param>
    /// <param name="echoEnabled">
    /// \if KO
    /// <para>Echo 응답 사용 여부입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>bool</c> value used for echo enabled.</para>
    /// \endif
    /// </param>
    public void UpdateTcpServerOptions(
        string protocol,
        string encodingName = PlainTextProtocolOptions.Utf8EncodingName,
        string sendTargetMode = nameof(TcpServerSendTargetMode.Broadcast),
        bool echoEnabled = false)
    {
        protocol = NormalizeProtocol(protocol);
        encodingName = NormalizeTextEncodingName(encodingName);
        var targetMode = NormalizeTcpServerSendTargetMode(sendTargetMode);

        _currentServerSendTargetMode = targetMode;
        _currentServerEchoEnabled = echoEnabled;

        if (_rawTcpServer is null ||
            _currentServerProtocol != protocol ||
            _currentServerEncoding != encodingName)
        {
            return;
        }

        _rawTcpServer.SendTargetMode = targetMode;

        UpdateTcpServerDescription(
            protocol,
            encodingName,
            _rawTcpServer.ConnectedClientCount,
            targetMode,
            echoEnabled);
    }

    /// <summary>
    /// \if KO
    /// <para>\brief 현재 TCP Server를 종료합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the stop tcp server async operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Stop Tcp Server Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the stop tcp server async operation.</para>
    /// \endif
    /// </returns>
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
        _rawTcpServer = null;

        if (Monitor.Channels.Any(x => x.Name == channelName))
        {
            Monitor.UpdateChannelState(channelName, ConnectionState.Disconnected);
        }
    }

    /// <summary>
    /// \if KO
    /// <para>\brief 선택된 프로토콜로 TCP Server에서 연결된 클라이언트에게 메시지를 송신합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the send tcp server async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="protocol">
    /// \if KO
    /// <para>프로토콜 이름입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for protocol.</para>
    /// \endif
    /// </param>
    /// <param name="text">
    /// \if KO
    /// <para>송신 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for text.</para>
    /// \endif
    /// </param>
    /// <param name="encodingName">
    /// \if KO
    /// <para>PlainText 외부 송수신 인코딩 이름입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for encoding name.</para>
    /// \endif
    /// </param>
    /// <param name="sendTargetMode">
    /// \if KO
    /// <para>send Target Mode에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for send target mode.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Send Tcp Server Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the send tcp server async operation.</para>
    /// \endif
    /// </returns>
    public async Task SendTcpServerAsync(
        string protocol,
        string text,
        string encodingName = PlainTextProtocolOptions.Utf8EncodingName,
        string sendTargetMode = nameof(TcpServerSendTargetMode.Broadcast))
    {
        protocol = NormalizeProtocol(protocol);
        encodingName = NormalizeTextEncodingName(encodingName);
        var targetMode = NormalizeTcpServerSendTargetMode(sendTargetMode);
        _currentServerSendTargetMode = targetMode;

        if (_rawTcpServer is not null)
        {
            _rawTcpServer.SendTargetMode = targetMode;
            UpdateTcpServerDescription(
                protocol,
                encodingName,
                _rawTcpServer.ConnectedClientCount,
                targetMode,
                _currentServerEchoEnabled);
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            text = "Hello from Dreamine TCP Server";
        }

        if (_tcpServer is null ||
            _tcpServer.State != ConnectionState.Listening ||
            _currentServerProtocol != protocol ||
            _currentServerEncoding != encodingName)
        {
            await StartTcpServerAsync(protocol, encodingName, sendTargetMode, _currentServerEchoEnabled);
        }

        if (_tcpServer is null)
        {
            return;
        }

        var message = CreateTcpMessageByProtocol(
            protocol,
            $"Server.Send.{targetMode}",
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
    /// \if KO
    /// <para>\brief 선택된 프로토콜로 TCP Client를 연결합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the connect tcp client async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="protocol">
    /// \if KO
    /// <para>프로토콜 이름입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for protocol.</para>
    /// \endif
    /// </param>
    /// <param name="encodingName">
    /// \if KO
    /// <para>PlainText 외부 송수신 인코딩 이름입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for encoding name.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Connect Tcp Client Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the connect tcp client async operation.</para>
    /// \endif
    /// </returns>
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

        await PrepareTcpClientTransportAsync(protocol, encodingName);

        if (_tcpClient is null)
        {
            return;
        }

        var channelName = GetTcpClientChannelName(protocol, encodingName);

        Monitor.UpdateChannelState(channelName, ConnectionState.Connecting);

        try
        {
            await _tcpClient.ConnectAsync();

            Monitor.UpdateChannelState(
                channelName,
                _tcpClient.State);
        }
        catch
        {
            // ResilientMessageTransport의 내부 WatchLoop가 계속 재연결을 시도합니다.
            // 수동 Connect 실패는 UI 상태만 Connecting으로 유지하고 예외를 샘플 밖으로 전파하지 않습니다.
            Monitor.UpdateChannelState(channelName, ConnectionState.Connecting);
        }
    }

    /// <summary>
    /// \if KO
    /// <para>\brief TCP Client Transport를 준비합니다. 연결은 수행하지 않고, 송신 큐와 재연결 Decorator를 구성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the prepare tcp client transport async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="protocol">
    /// \if KO
    /// <para>프로토콜 이름입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for protocol.</para>
    /// \endif
    /// </param>
    /// <param name="encodingName">
    /// \if KO
    /// <para>PlainText 외부 송수신 인코딩 이름입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for encoding name.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Prepare Tcp Client Transport Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the prepare tcp client transport async operation.</para>
    /// \endif
    /// </returns>
    private async Task PrepareTcpClientTransportAsync(
        string protocol,
        string encodingName)
    {
        protocol = NormalizeProtocol(protocol);
        encodingName = NormalizeTextEncodingName(encodingName);

        if (_tcpClient is not null &&
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

        var rawTcpClient = new TcpClientTransport(
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

        _tcpClient = new ResilientMessageTransport(
            rawTcpClient,
            CreateSampleReconnectPolicy(),
            CreateSampleOutboundQueueOptions());

        var channelName = GetTcpClientChannelName(protocol, encodingName);

        AttachTcpResilientStateMonitor(
            _tcpClient,
            channelName);

        AttachTcpClientQueueMonitor(
            _tcpClient,
            protocol,
            encodingName,
            channelName);

        _tcpClient.MessageReceived += OnTcpClientMessageReceived;

        Monitor.UpdateChannelState(
            channelName,
            _tcpClient.State);
    }

    /// <summary>
    /// \if KO
    /// <para>\brief 현재 TCP Client 연결을 해제합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the disconnect tcp client async operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Disconnect Tcp Client Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the disconnect tcp client async operation.</para>
    /// \endif
    /// </returns>
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
    /// \if KO
    /// <para>\brief 선택된 프로토콜로 TCP Client에서 서버로 메시지를 송신합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the send tcp client async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="protocol">
    /// \if KO
    /// <para>프로토콜 이름입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for protocol.</para>
    /// \endif
    /// </param>
    /// <param name="text">
    /// \if KO
    /// <para>송신 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for text.</para>
    /// \endif
    /// </param>
    /// <param name="encodingName">
    /// \if KO
    /// <para>PlainText 외부 송수신 인코딩 이름입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for encoding name.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Send Tcp Client Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the send tcp client async operation.</para>
    /// \endif
    /// </returns>
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

        await PrepareTcpClientTransportAsync(protocol, encodingName);

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

        if (_tcpClient is ResilientMessageTransport resilientTransport)
        {
            var queueStatusMessage = CreateTcpMessageByProtocol(
                protocol,
                "Client.QueueStatus",
                $"QueueCount={resilientTransport.QueuedMessageCount}, State={_tcpClient.State}",
                encodingName);

            Monitor.AddReceiveLog(
                channelName,
                TransportKind.Tcp,
                queueStatusMessage);
        }

        Monitor.UpdateChannelState(
            channelName,
            _tcpClient.State);
    }

    /// <summary>
    /// \if KO
    /// <para>\brief TCP Server와 TCP Client를 모두 종료합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the stop all tcp async operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Stop All Tcp Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the stop all tcp async operation.</para>
    /// \endif
    /// </returns>
    public async Task StopAllTcpAsync()
    {
        await DisconnectTcpClientAsync();
        await StopTcpServerAsync();
    }

    /// <summary>
    /// \if KO
    /// <para>\brief UDP Peer A와 Peer B를 모두 시작합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the start udp loopback async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="protocol">
    /// \if KO
    /// <para>프로토콜 이름입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for protocol.</para>
    /// \endif
    /// </param>
    /// <param name="encodingName">
    /// \if KO
    /// <para>PlainText 외부 송수신 인코딩 이름입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for encoding name.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Start Udp Loopback Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the start udp loopback async operation.</para>
    /// \endif
    /// </returns>
    public async Task StartUdpLoopbackAsync(
        string protocol,
        string encodingName = PlainTextProtocolOptions.Utf8EncodingName)
    {
        await ConnectUdpPeerAAsync(protocol, encodingName);
        await ConnectUdpPeerBAsync(protocol, encodingName);
    }

    /// <summary>
    /// \if KO
    /// <para>\brief 선택된 프로토콜로 UDP Peer A를 시작합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the connect udp peer a async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="protocol">
    /// \if KO
    /// <para>프로토콜 이름입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for protocol.</para>
    /// \endif
    /// </param>
    /// <param name="encodingName">
    /// \if KO
    /// <para>PlainText 외부 송수신 인코딩 이름입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for encoding name.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Connect Udp Peer A Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the connect udp peer a async operation.</para>
    /// \endif
    /// </returns>
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
    /// \if KO
    /// <para>\brief 선택된 프로토콜로 UDP Peer B를 시작합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the connect udp peer b async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="protocol">
    /// \if KO
    /// <para>프로토콜 이름입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for protocol.</para>
    /// \endif
    /// </param>
    /// <param name="encodingName">
    /// \if KO
    /// <para>PlainText 외부 송수신 인코딩 이름입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for encoding name.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Connect Udp Peer B Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the connect udp peer b async operation.</para>
    /// \endif
    /// </returns>
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
    /// \if KO
    /// <para>\brief UDP Peer A 연결을 해제합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the disconnect udp peer a async operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Disconnect Udp Peer A Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the disconnect udp peer a async operation.</para>
    /// \endif
    /// </returns>
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
    /// \if KO
    /// <para>\brief UDP Peer B 연결을 해제합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the disconnect udp peer b async operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Disconnect Udp Peer B Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the disconnect udp peer b async operation.</para>
    /// \endif
    /// </returns>
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
    /// \if KO
    /// <para>\brief UDP Peer A와 Peer B를 모두 종료합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the stop all udp async operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Stop All Udp Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the stop all udp async operation.</para>
    /// \endif
    /// </returns>
    public async Task StopAllUdpAsync()
    {
        await DisconnectUdpPeerBAsync();
        await DisconnectUdpPeerAAsync();
    }

    /// <summary>
    /// \if KO
    /// <para>\brief UDP Peer A에서 Peer B로 메시지를 송신합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the send udp peer a async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="protocol">
    /// \if KO
    /// <para>프로토콜 이름입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for protocol.</para>
    /// \endif
    /// </param>
    /// <param name="text">
    /// \if KO
    /// <para>송신 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for text.</para>
    /// \endif
    /// </param>
    /// <param name="encodingName">
    /// \if KO
    /// <para>PlainText 외부 송수신 인코딩 이름입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for encoding name.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Send Udp Peer A Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the send udp peer a async operation.</para>
    /// \endif
    /// </returns>
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
    /// \if KO
    /// <para>\brief UDP Peer B에서 Peer A로 메시지를 송신합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the send udp peer b async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="protocol">
    /// \if KO
    /// <para>프로토콜 이름입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for protocol.</para>
    /// \endif
    /// </param>
    /// <param name="text">
    /// \if KO
    /// <para>송신 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for text.</para>
    /// \endif
    /// </param>
    /// <param name="encodingName">
    /// \if KO
    /// <para>PlainText 외부 송수신 인코딩 이름입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for encoding name.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Send Udp Peer B Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the send udp peer b async operation.</para>
    /// \endif
    /// </returns>
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
    /// \if KO
    /// <para>\brief 선택된 설정으로 Serial Port를 연결합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the connect serial async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="portName">
    /// \if KO
    /// <para>Serial Port 이름입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for port name.</para>
    /// \endif
    /// </param>
    /// <param name="baudRate">
    /// \if KO
    /// <para>BaudRate입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for baud rate.</para>
    /// \endif
    /// </param>
    /// <param name="protocol">
    /// \if KO
    /// <para>프로토콜 이름입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for protocol.</para>
    /// \endif
    /// </param>
    /// <param name="encodingName">
    /// \if KO
    /// <para>PlainText 외부 송수신 인코딩 이름입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for encoding name.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Connect Serial Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the connect serial async operation.</para>
    /// \endif
    /// </returns>
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
    /// \if KO
    /// <para>\brief 현재 Serial Port 연결을 해제합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the disconnect serial async operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Disconnect Serial Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the disconnect serial async operation.</para>
    /// \endif
    /// </returns>
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
    /// \if KO
    /// <para>\brief 현재 Serial Port로 메시지를 송신합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the send serial async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="protocol">
    /// \if KO
    /// <para>프로토콜 이름입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for protocol.</para>
    /// \endif
    /// </param>
    /// <param name="text">
    /// \if KO
    /// <para>송신 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for text.</para>
    /// \endif
    /// </param>
    /// <param name="encodingName">
    /// \if KO
    /// <para>PlainText 외부 송수신 인코딩 이름입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for encoding name.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Send Serial Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the send serial async operation.</para>
    /// \endif
    /// </returns>
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
    /// \if KO
    /// <para>\brief RabbitMQ에 연결합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the connect rabbit mq async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="host">
    /// \if KO
    /// <para>RabbitMQ Host입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for host.</para>
    /// \endif
    /// </param>
    /// <param name="port">
    /// \if KO
    /// <para>RabbitMQ Port입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for port.</para>
    /// \endif
    /// </param>
    /// <param name="virtualHost">
    /// \if KO
    /// <para>RabbitMQ VirtualHost입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for virtual host.</para>
    /// \endif
    /// </param>
    /// <param name="userName">
    /// \if KO
    /// <para>RabbitMQ 사용자 이름입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for user name.</para>
    /// \endif
    /// </param>
    /// <param name="password">
    /// \if KO
    /// <para>RabbitMQ 비밀번호입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for password.</para>
    /// \endif
    /// </param>
    /// <param name="exchangeName">
    /// \if KO
    /// <para>Exchange 이름입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for exchange name.</para>
    /// \endif
    /// </param>
    /// <param name="queueName">
    /// \if KO
    /// <para>Queue 이름입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for queue name.</para>
    /// \endif
    /// </param>
    /// <param name="routingKey">
    /// \if KO
    /// <para>RoutingKey입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for routing key.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Connect Rabbit Mq Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the connect rabbit mq async operation.</para>
    /// \endif
    /// </returns>
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
    /// \if KO
    /// <para>\brief RabbitMQ 메시지 구독을 시작합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the subscribe rabbit mq async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="exchangeName">
    /// \if KO
    /// <para>Exchange 이름입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for exchange name.</para>
    /// \endif
    /// </param>
    /// <param name="queueName">
    /// \if KO
    /// <para>Queue 이름입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for queue name.</para>
    /// \endif
    /// </param>
    /// <param name="routingKey">
    /// \if KO
    /// <para>RoutingKey입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for routing key.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Subscribe Rabbit Mq Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the subscribe rabbit mq async operation.</para>
    /// \endif
    /// </returns>
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
    /// \if KO
    /// <para>\brief RabbitMQ 메시지를 발행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the publish rabbit mq async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="exchangeName">
    /// \if KO
    /// <para>Exchange 이름입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for exchange name.</para>
    /// \endif
    /// </param>
    /// <param name="routingKey">
    /// \if KO
    /// <para>RoutingKey입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for routing key.</para>
    /// \endif
    /// </param>
    /// <param name="text">
    /// \if KO
    /// <para>송신 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for text.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Publish Rabbit Mq Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the publish rabbit mq async operation.</para>
    /// \endif
    /// </returns>
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
    /// \if KO
    /// <para>\brief RabbitMQ 연결을 해제합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the disconnect rabbit mq async operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Disconnect Rabbit Mq Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the disconnect rabbit mq async operation.</para>
    /// \endif
    /// </returns>
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
    /// \if KO
    /// <para>\brief 모든 통신 샘플 연결을 종료합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the stop all async operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Stop All Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the stop all async operation.</para>
    /// \endif
    /// </returns>
    public async Task StopAllAsync()
    {
        await DisconnectRabbitMqAsync();
        await DisconnectSerialAsync();
        await StopAllUdpAsync();
        await StopAllTcpAsync();
        await DisconnectInMemoryAsync();
    }

    /// <summary>
    /// \if KO
    /// <para>Subscribe In Memory Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the subscribe in memory async operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Subscribe In Memory Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the subscribe in memory async operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Tcp Server Message Received 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the tcp server message received event or state change.</para>
    /// \endif
    /// </summary>
    /// <param name="sender">
    /// \if KO
    /// <para>이벤트를 발생시킨 객체입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The object that raised the event.</para>
    /// \endif
    /// </param>
    /// <param name="message">
    /// \if KO
    /// <para>처리할 메시지입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The message to process.</para>
    /// \endif
    /// </param>
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
            _tcpServer.State != ConnectionState.Listening ||
            !_currentServerEchoEnabled)
        {
            return;
        }

        var receiveText = Encoding.UTF8.GetString(message.Payload);

        var echoMessage = CreateTcpMessageByProtocol(
            protocol,
            $"Server.Echo.{_currentServerSendTargetMode}",
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

    /// <summary>
    /// \if KO
    /// <para>\brief SampleSmart TCP Client 예제에서 사용할 재연결 정책을 생성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Creates the sample reconnect policy value.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>재연결 정책입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ReconnectPolicy</c> result produced by the create sample reconnect policy operation.</para>
    /// \endif
    /// </returns>
    private static ReconnectPolicy CreateSampleReconnectPolicy()
    {
        return new ReconnectPolicy
        {
            Enabled = true,
            InitialDelay = TimeSpan.FromSeconds(1),
            MaxDelay = TimeSpan.FromSeconds(5),
            BackoffFactor = 1.5,
            MaxRetryCount = null,
            WatchInterval = TimeSpan.FromMilliseconds(500)
        };
    }

    /// <summary>
    /// \if KO
    /// <para>\brief SampleSmart TCP Client 예제에서 사용할 송신 큐 정책을 생성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Creates the sample outbound queue options value.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>송신 큐 정책입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>OutboundQueueOptions</c> result produced by the create sample outbound queue options operation.</para>
    /// \endif
    /// </returns>
    private static OutboundQueueOptions CreateSampleOutboundQueueOptions()
    {
        return new OutboundQueueOptions
        {
            DisconnectedSendPolicy = DisconnectedSendPolicy.Queue,
            MaxQueueSize = 10_000,
            DropOldestWhenFull = true,
            MaxMessageAge = null,
            FlushOnReconnect = true
        };
    }

    /// <summary>
    /// \if KO
    /// <para>\brief TCP Server 예제에서 사용할 송신 큐 정책을 생성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Creates the sample server outbound queue options value.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>송신 큐 정책입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>OutboundQueueOptions</c> result produced by the create sample server outbound queue options operation.</para>
    /// \endif
    /// </returns>
    private static OutboundQueueOptions CreateSampleServerOutboundQueueOptions()
    {
        return new OutboundQueueOptions
        {
            DisconnectedSendPolicy = DisconnectedSendPolicy.Fail,
            MaxQueueSize = 1_000,
            DropOldestWhenFull = true,
            MaxMessageAge = null,
            FlushOnReconnect = false
        };
    }

    /// <summary>
    /// \if KO
    /// <para>대상 객체에 동작을 연결합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Attaches the behavior to a target object.</para>
    /// \endif
    /// </summary>
    /// <param name="transport">
    /// \if KO
    /// <para>transport에 사용할 <c>IMessageTransport</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IMessageTransport</c> value used for transport.</para>
    /// \endif
    /// </param>
    /// <param name="channelName">
    /// \if KO
    /// <para>channel Name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for channel name.</para>
    /// \endif
    /// </param>
    private void AttachTcpResilientStateMonitor(
        IMessageTransport transport,
        string channelName)
    {
        if (transport is not ResilientMessageTransport resilientTransport)
        {
            return;
        }

        resilientTransport.StateChanged += (_, state) =>
        {
            RunOnUiThread(() =>
            {
                Monitor.UpdateChannelState(
                    channelName,
                    state);
            });
        };
    }

    /// <summary>
    /// \if KO
    /// <para>대상 객체에 동작을 연결합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Attaches the behavior to a target object.</para>
    /// \endif
    /// </summary>
    /// <param name="tcpServerTransport">
    /// \if KO
    /// <para>tcp Server Transport에 사용할 <c>TcpServerTransport</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>TcpServerTransport</c> value used for tcp server transport.</para>
    /// \endif
    /// </param>
    /// <param name="protocol">
    /// \if KO
    /// <para>protocol에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for protocol.</para>
    /// \endif
    /// </param>
    /// <param name="encodingName">
    /// \if KO
    /// <para>encoding Name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for encoding name.</para>
    /// \endif
    /// </param>
    private void AttachTcpServerClientCountMonitor(
        TcpServerTransport tcpServerTransport,
        string protocol,
        string encodingName)
    {
        tcpServerTransport.ConnectedClientCountChanged += (_, clientCount) =>
        {
            RunOnUiThread(() =>
            {
                UpdateTcpServerDescription(
                    protocol,
                    encodingName,
                    clientCount,
                    _currentServerSendTargetMode,
                    _currentServerEchoEnabled);
            });
        };
    }

    /// <summary>
    /// \if KO
    /// <para>Update Tcp Server Description 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the update tcp server description operation.</para>
    /// \endif
    /// </summary>
    /// <param name="protocol">
    /// \if KO
    /// <para>protocol에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for protocol.</para>
    /// \endif
    /// </param>
    /// <param name="encodingName">
    /// \if KO
    /// <para>encoding Name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for encoding name.</para>
    /// \endif
    /// </param>
    /// <param name="clientCount">
    /// \if KO
    /// <para>client Count에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for client count.</para>
    /// \endif
    /// </param>
    /// <param name="targetMode">
    /// \if KO
    /// <para>target Mode에 사용할 <c>TcpServerSendTargetMode</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>TcpServerSendTargetMode</c> value used for target mode.</para>
    /// \endif
    /// </param>
    /// <param name="echoEnabled">
    /// \if KO
    /// <para>echo Enabled에 사용할 <c>bool</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>bool</c> value used for echo enabled.</para>
    /// \endif
    /// </param>
    private void UpdateTcpServerDescription(
        string protocol,
        string encodingName,
        int clientCount,
        TcpServerSendTargetMode targetMode,
        bool echoEnabled)
    {
        var channelName = GetTcpServerChannelName(protocol, encodingName);
        var port = GetTcpPort(protocol);

        Monitor.UpdateChannelDescription(
            channelName,
            CreateTcpServerDescription(protocol, encodingName, port, clientCount, targetMode, echoEnabled));
    }

    /// <summary>
    /// \if KO
    /// <para>Tcp Server Description 값을 생성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Creates the tcp server description value.</para>
    /// \endif
    /// </summary>
    /// <param name="protocol">
    /// \if KO
    /// <para>protocol에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for protocol.</para>
    /// \endif
    /// </param>
    /// <param name="encodingName">
    /// \if KO
    /// <para>encoding Name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for encoding name.</para>
    /// \endif
    /// </param>
    /// <param name="port">
    /// \if KO
    /// <para>port에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for port.</para>
    /// \endif
    /// </param>
    /// <param name="clientCount">
    /// \if KO
    /// <para>client Count에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for client count.</para>
    /// \endif
    /// </param>
    /// <param name="targetMode">
    /// \if KO
    /// <para>target Mode에 사용할 <c>TcpServerSendTargetMode</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>TcpServerSendTargetMode</c> value used for target mode.</para>
    /// \endif
    /// </param>
    /// <param name="echoEnabled">
    /// \if KO
    /// <para>echo Enabled에 사용할 <c>bool</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>bool</c> value used for echo enabled.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Create Tcp Server Description 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the create tcp server description operation.</para>
    /// \endif
    /// </returns>
    private static string CreateTcpServerDescription(
        string protocol,
        string encodingName,
        int port,
        int clientCount,
        TcpServerSendTargetMode targetMode,
        bool echoEnabled)
    {
        var echoText = echoEnabled ? "On" : "Off";

        return $"TCP server [{protocol}/{NormalizeTextEncodingName(encodingName)}] on 127.0.0.1:{port}. Clients={clientCount}. Target={targetMode}. Echo={echoText}.";
    }

    /// <summary>
    /// \if KO
    /// <para>대상 객체에 동작을 연결합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Attaches the behavior to a target object.</para>
    /// \endif
    /// </summary>
    /// <param name="transport">
    /// \if KO
    /// <para>transport에 사용할 <c>IMessageTransport</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IMessageTransport</c> value used for transport.</para>
    /// \endif
    /// </param>
    /// <param name="protocol">
    /// \if KO
    /// <para>protocol에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for protocol.</para>
    /// \endif
    /// </param>
    /// <param name="encodingName">
    /// \if KO
    /// <para>encoding Name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for encoding name.</para>
    /// \endif
    /// </param>
    /// <param name="channelName">
    /// \if KO
    /// <para>channel Name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for channel name.</para>
    /// \endif
    /// </param>
    private void AttachTcpClientQueueMonitor(
        IMessageTransport transport,
        string protocol,
        string encodingName,
        string channelName)
    {
        if (transport is not ResilientMessageTransport resilientTransport)
        {
            return;
        }

        resilientTransport.QueueCountChanged += (_, queueCount) =>
        {
            var queueStatusMessage = CreateTcpMessageByProtocol(
                protocol,
                "Client.QueueStatus",
                $"QueueCount={queueCount}, State={resilientTransport.State}",
                encodingName);

            RunOnUiThread(() =>
            {
                Monitor.AddReceiveLog(
                    channelName,
                    TransportKind.Tcp,
                    queueStatusMessage);

                Monitor.UpdateChannelState(
                    channelName,
                    resilientTransport.State);
            });
        };
    }

    /// <summary>
    /// \if KO
    /// <para>Tcp Client Message Received 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the tcp client message received event or state change.</para>
    /// \endif
    /// </summary>
    /// <param name="sender">
    /// \if KO
    /// <para>이벤트를 발생시킨 객체입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The object that raised the event.</para>
    /// \endif
    /// </param>
    /// <param name="message">
    /// \if KO
    /// <para>처리할 메시지입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The message to process.</para>
    /// \endif
    /// </param>
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

    /// <summary>
    /// \if KO
    /// <para>Udp Peer A Message Received 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the udp peer a message received event or state change.</para>
    /// \endif
    /// </summary>
    /// <param name="sender">
    /// \if KO
    /// <para>이벤트를 발생시킨 객체입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The object that raised the event.</para>
    /// \endif
    /// </param>
    /// <param name="message">
    /// \if KO
    /// <para>처리할 메시지입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The message to process.</para>
    /// \endif
    /// </param>
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

    /// <summary>
    /// \if KO
    /// <para>Udp Peer B Message Received 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the udp peer b message received event or state change.</para>
    /// \endif
    /// </summary>
    /// <param name="sender">
    /// \if KO
    /// <para>이벤트를 발생시킨 객체입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The object that raised the event.</para>
    /// \endif
    /// </param>
    /// <param name="message">
    /// \if KO
    /// <para>처리할 메시지입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The message to process.</para>
    /// \endif
    /// </param>
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

    /// <summary>
    /// \if KO
    /// <para>Serial Message Received 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the serial message received event or state change.</para>
    /// \endif
    /// </summary>
    /// <param name="sender">
    /// \if KO
    /// <para>이벤트를 발생시킨 객체입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The object that raised the event.</para>
    /// \endif
    /// </param>
    /// <param name="message">
    /// \if KO
    /// <para>처리할 메시지입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The message to process.</para>
    /// \endif
    /// </param>
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

    /// <summary>
    /// \if KO
    /// <para>Ensure Tcp Server Channel 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the ensure tcp server channel operation.</para>
    /// \endif
    /// </summary>
    /// <param name="protocol">
    /// \if KO
    /// <para>protocol에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for protocol.</para>
    /// \endif
    /// </param>
    /// <param name="encodingName">
    /// \if KO
    /// <para>encoding Name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for encoding name.</para>
    /// \endif
    /// </param>
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
            CreateTcpServerDescription(
                protocol,
                encodingName,
                port,
                0,
                _currentServerSendTargetMode,
                _currentServerEchoEnabled));
    }

    /// <summary>
    /// \if KO
    /// <para>Ensure Tcp Client Channel 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the ensure tcp client channel operation.</para>
    /// \endif
    /// </summary>
    /// <param name="protocol">
    /// \if KO
    /// <para>protocol에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for protocol.</para>
    /// \endif
    /// </param>
    /// <param name="encodingName">
    /// \if KO
    /// <para>encoding Name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for encoding name.</para>
    /// \endif
    /// </param>
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

    /// <summary>
    /// \if KO
    /// <para>Ensure Udp Peer Channel 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the ensure udp peer channel operation.</para>
    /// \endif
    /// </summary>
    /// <param name="peerName">
    /// \if KO
    /// <para>peer Name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for peer name.</para>
    /// \endif
    /// </param>
    /// <param name="protocol">
    /// \if KO
    /// <para>protocol에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for protocol.</para>
    /// \endif
    /// </param>
    /// <param name="encodingName">
    /// \if KO
    /// <para>encoding Name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for encoding name.</para>
    /// \endif
    /// </param>
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

    /// <summary>
    /// \if KO
    /// <para>Ensure Serial Channel 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the ensure serial channel operation.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>Ensure Rabbit Mq Channel 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the ensure rabbit mq channel operation.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>Serial Channel Name 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the serial channel name value.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Get Serial Channel Name 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the get serial channel name operation.</para>
    /// \endif
    /// </returns>
    private string GetSerialChannelName()
    {
        if (string.IsNullOrWhiteSpace(_currentSerialPortName))
        {
            return "Serial-Port";
        }

        return $"Serial-{_currentSerialPortName}";
    }

    /// <summary>
    /// \if KO
    /// <para>Protocol Adapter 값을 생성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Creates the protocol adapter value.</para>
    /// \endif
    /// </summary>
    /// <param name="protocol">
    /// \if KO
    /// <para>protocol에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for protocol.</para>
    /// \endif
    /// </param>
    /// <param name="routePrefix">
    /// \if KO
    /// <para>route Prefix에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for route prefix.</para>
    /// \endif
    /// </param>
    /// <param name="namePrefix">
    /// \if KO
    /// <para>name Prefix에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for name prefix.</para>
    /// \endif
    /// </param>
    /// <param name="encodingName">
    /// \if KO
    /// <para>encoding Name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for encoding name.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Create Protocol Adapter 작업에서 생성한 <c>IMessageProtocolAdapter</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IMessageProtocolAdapter</c> result produced by the create protocol adapter operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Frame Codec 값을 생성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Creates the frame codec value.</para>
    /// \endif
    /// </summary>
    /// <param name="protocol">
    /// \if KO
    /// <para>protocol에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for protocol.</para>
    /// \endif
    /// </param>
    /// <param name="encodingName">
    /// \if KO
    /// <para>encoding Name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for encoding name.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Create Frame Codec 작업에서 생성한 <c>IMessageFrameCodec</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IMessageFrameCodec</c> result produced by the create frame codec operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Tcp Port 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the tcp port value.</para>
    /// \endif
    /// </summary>
    /// <param name="protocol">
    /// \if KO
    /// <para>protocol에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for protocol.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get Tcp Port 작업에서 생성한 <c>int</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> result produced by the get tcp port operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Tcp Server Channel Name 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the tcp server channel name value.</para>
    /// \endif
    /// </summary>
    /// <param name="protocol">
    /// \if KO
    /// <para>protocol에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for protocol.</para>
    /// \endif
    /// </param>
    /// <param name="encodingName">
    /// \if KO
    /// <para>encoding Name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for encoding name.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get Tcp Server Channel Name 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the get tcp server channel name operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Tcp Client Channel Name 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the tcp client channel name value.</para>
    /// \endif
    /// </summary>
    /// <param name="protocol">
    /// \if KO
    /// <para>protocol에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for protocol.</para>
    /// \endif
    /// </param>
    /// <param name="encodingName">
    /// \if KO
    /// <para>encoding Name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for encoding name.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get Tcp Client Channel Name 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the get tcp client channel name operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Udp Peer Channel Name 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the udp peer channel name value.</para>
    /// \endif
    /// </summary>
    /// <param name="peerName">
    /// \if KO
    /// <para>peer Name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for peer name.</para>
    /// \endif
    /// </param>
    /// <param name="protocol">
    /// \if KO
    /// <para>protocol에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for protocol.</para>
    /// \endif
    /// </param>
    /// <param name="encodingName">
    /// \if KO
    /// <para>encoding Name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for encoding name.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get Udp Peer Channel Name 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the get udp peer channel name operation.</para>
    /// \endif
    /// </returns>
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
    /// <summary>
    /// \if KO
    /// <para>Normalize Text Encoding Name 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the normalize text encoding name operation.</para>
    /// \endif
    /// </summary>
    /// <param name="encodingName">
    /// \if KO
    /// <para>encoding Name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for encoding name.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Normalize Text Encoding Name 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the normalize text encoding name operation.</para>
    /// \endif
    /// </returns>
    private static string NormalizeTextEncodingName(string encodingName)
    {
        if (string.Equals(encodingName, PlainTextProtocolOptions.KoreanCodePage949EncodingName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(encodingName, "949", StringComparison.OrdinalIgnoreCase))
        {
            return PlainTextProtocolOptions.KoreanCodePage949EncodingName;
        }

        return PlainTextProtocolOptions.Utf8EncodingName;
    }


    /// <summary>
    /// \if KO
    /// <para>Udp Local Port 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the udp local port value.</para>
    /// \endif
    /// </summary>
    /// <param name="peerName">
    /// \if KO
    /// <para>peer Name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for peer name.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get Udp Local Port 작업에서 생성한 <c>int</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> result produced by the get udp local port operation.</para>
    /// \endif
    /// </returns>
    private static int GetUdpLocalPort(string peerName)
    {
        return NormalizeUdpPeerName(peerName) == "B"
            ? UdpPeerBLocalPort
            : UdpPeerALocalPort;
    }

    /// <summary>
    /// \if KO
    /// <para>Udp Remote Port 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the udp remote port value.</para>
    /// \endif
    /// </summary>
    /// <param name="peerName">
    /// \if KO
    /// <para>peer Name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for peer name.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get Udp Remote Port 작업에서 생성한 <c>int</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> result produced by the get udp remote port operation.</para>
    /// \endif
    /// </returns>
    private static int GetUdpRemotePort(string peerName)
    {
        return NormalizeUdpPeerName(peerName) == "B"
            ? UdpPeerALocalPort
            : UdpPeerBLocalPort;
    }

    /// <summary>
    /// \if KO
    /// <para>Normalize Udp Peer Name 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the normalize udp peer name operation.</para>
    /// \endif
    /// </summary>
    /// <param name="peerName">
    /// \if KO
    /// <para>peer Name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for peer name.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Normalize Udp Peer Name 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the normalize udp peer name operation.</para>
    /// \endif
    /// </returns>
    private static string NormalizeUdpPeerName(string peerName)
    {
        return string.Equals(peerName, "B", StringComparison.OrdinalIgnoreCase)
            ? "B"
            : "A";
    }

    /// <summary>
    /// \if KO
    /// <para>Tcp Message By Protocol 값을 생성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Creates the tcp message by protocol value.</para>
    /// \endif
    /// </summary>
    /// <param name="protocol">
    /// \if KO
    /// <para>protocol에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for protocol.</para>
    /// \endif
    /// </param>
    /// <param name="direction">
    /// \if KO
    /// <para>direction에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for direction.</para>
    /// \endif
    /// </param>
    /// <param name="text">
    /// \if KO
    /// <para>text에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for text.</para>
    /// \endif
    /// </param>
    /// <param name="encodingName">
    /// \if KO
    /// <para>encoding Name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for encoding name.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Create Tcp Message By Protocol 작업에서 생성한 <c>MessageEnvelope</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>MessageEnvelope</c> result produced by the create tcp message by protocol operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Udp Message By Protocol 값을 생성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Creates the udp message by protocol value.</para>
    /// \endif
    /// </summary>
    /// <param name="protocol">
    /// \if KO
    /// <para>protocol에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for protocol.</para>
    /// \endif
    /// </param>
    /// <param name="direction">
    /// \if KO
    /// <para>direction에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for direction.</para>
    /// \endif
    /// </param>
    /// <param name="text">
    /// \if KO
    /// <para>text에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for text.</para>
    /// \endif
    /// </param>
    /// <param name="encodingName">
    /// \if KO
    /// <para>encoding Name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for encoding name.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Create Udp Message By Protocol 작업에서 생성한 <c>MessageEnvelope</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>MessageEnvelope</c> result produced by the create udp message by protocol operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Serial Message By Protocol 값을 생성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Creates the serial message by protocol value.</para>
    /// \endif
    /// </summary>
    /// <param name="protocol">
    /// \if KO
    /// <para>protocol에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for protocol.</para>
    /// \endif
    /// </param>
    /// <param name="direction">
    /// \if KO
    /// <para>direction에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for direction.</para>
    /// \endif
    /// </param>
    /// <param name="text">
    /// \if KO
    /// <para>text에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for text.</para>
    /// \endif
    /// </param>
    /// <param name="encodingName">
    /// \if KO
    /// <para>encoding Name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for encoding name.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Create Serial Message By Protocol 작업에서 생성한 <c>MessageEnvelope</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>MessageEnvelope</c> result produced by the create serial message by protocol operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Rabbit Mq Message 값을 생성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Creates the rabbit mq message value.</para>
    /// \endif
    /// </summary>
    /// <param name="direction">
    /// \if KO
    /// <para>direction에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for direction.</para>
    /// \endif
    /// </param>
    /// <param name="route">
    /// \if KO
    /// <para>route에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for route.</para>
    /// \endif
    /// </param>
    /// <param name="text">
    /// \if KO
    /// <para>text에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for text.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Create Rabbit Mq Message 작업에서 생성한 <c>MessageEnvelope</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>MessageEnvelope</c> result produced by the create rabbit mq message operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Text Message 값을 생성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Creates the text message value.</para>
    /// \endif
    /// </summary>
    /// <param name="name">
    /// \if KO
    /// <para>name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for name.</para>
    /// \endif
    /// </param>
    /// <param name="route">
    /// \if KO
    /// <para>route에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for route.</para>
    /// \endif
    /// </param>
    /// <param name="text">
    /// \if KO
    /// <para>text에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for text.</para>
    /// \endif
    /// </param>
    /// <param name="protocol">
    /// \if KO
    /// <para>protocol에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for protocol.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Create Text Message 작업에서 생성한 <c>MessageEnvelope</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>MessageEnvelope</c> result produced by the create text message operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Rabbit Mq Error Log 항목을 추가합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Adds the rabbit mq error log item.</para>
    /// \endif
    /// </summary>
    /// <param name="name">
    /// \if KO
    /// <para>name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for name.</para>
    /// \endif
    /// </param>
    /// <param name="text">
    /// \if KO
    /// <para>text에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for text.</para>
    /// \endif
    /// </param>
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

    /// <summary>
    /// \if KO
    /// <para>Ensure Json Payload 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the ensure json payload operation.</para>
    /// \endif
    /// </summary>
    /// <param name="text">
    /// \if KO
    /// <para>text에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for text.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Ensure Json Payload 작업에서 생성한 <c>byte[]</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>byte[]</c> result produced by the ensure json payload operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Escape Json String 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the escape json string operation.</para>
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
    /// <returns>
    /// \if KO
    /// <para>Escape Json String 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the escape json string operation.</para>
    /// \endif
    /// </returns>
    private static string EscapeJsonString(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
    }

    /// <summary>
    /// \if KO
    /// <para>Normalize Tcp Server Send Target Mode 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the normalize tcp server send target mode operation.</para>
    /// \endif
    /// </summary>
    /// <param name="sendTargetMode">
    /// \if KO
    /// <para>send Target Mode에 사용할 <c>string?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for send target mode.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Normalize Tcp Server Send Target Mode 작업에서 생성한 <c>TcpServerSendTargetMode</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>TcpServerSendTargetMode</c> result produced by the normalize tcp server send target mode operation.</para>
    /// \endif
    /// </returns>
    private static TcpServerSendTargetMode NormalizeTcpServerSendTargetMode(string? sendTargetMode)
    {
        if (Enum.TryParse<TcpServerSendTargetMode>(
                sendTargetMode,
                ignoreCase: true,
                out var parsed))
        {
            return parsed;
        }

        return TcpServerSendTargetMode.Broadcast;
    }

    /// <summary>
    /// \if KO
    /// <para>Normalize Protocol 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the normalize protocol operation.</para>
    /// \endif
    /// </summary>
    /// <param name="protocol">
    /// \if KO
    /// <para>protocol에 사용할 <c>string?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for protocol.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Normalize Protocol 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the normalize protocol operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Normalize Text 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the normalize text operation.</para>
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
    /// <param name="defaultValue">
    /// \if KO
    /// <para>default Value에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for default value.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Normalize Text 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the normalize text operation.</para>
    /// \endif
    /// </returns>
    private static string NormalizeText(
        string? value,
        string defaultValue)
    {
        return string.IsNullOrWhiteSpace(value)
            ? defaultValue
            : value.Trim();
    }

    /// <summary>
    /// \if KO
    /// <para>Run On Ui Thread 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the run on ui thread operation.</para>
    /// \endif
    /// </summary>
    /// <param name="action">
    /// \if KO
    /// <para>action에 사용할 <c>Action</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Action</c> value used for action.</para>
    /// \endif
    /// </param>
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