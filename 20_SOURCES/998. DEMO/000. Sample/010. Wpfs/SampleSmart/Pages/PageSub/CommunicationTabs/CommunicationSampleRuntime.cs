using System.Text;
using System.Windows;
using Dreamine.Communication.Abstractions.Enums;
using Dreamine.Communication.Abstractions.Interfaces;
using Dreamine.Communication.Abstractions.Models;
using Dreamine.Communication.Core.Framing;
using Dreamine.Communication.Core.Protocols;
using Dreamine.Communication.Serial.Options;
using Dreamine.Communication.Serial.Ports;
using Dreamine.Communication.Sockets.Clients;
using Dreamine.Communication.Sockets.Options;
using Dreamine.Communication.Sockets.Servers;
using Dreamine.Communication.Wpf.ViewModels;

namespace SampleSmart.Pages.PageSub.CommunicationTabs;

/// <summary>
/// \brief Communication 샘플 전체에서 공유되는 Runtime 컨텍스트입니다.
/// </summary>
/// <remarks>
/// 이 클래스는 Communication 샘플 탭들이 공유하는 Monitor, InMemory MessageBus,
/// TCP Server, TCP Client, Serial Port 인스턴스를 관리합니다.
/// </remarks>
public sealed class CommunicationSampleRuntime
{
    private const string InMemoryChannelName = "InMemory-Communication";
    private const string InMemoryRouteName = "sample.communication.message";

    private const string DreamineEnvelopeProtocol = "DreamineEnvelope";
    private const string PlainTextProtocol = "PlainText";
    private const string RawAvailableProtocol = "RawAvailable";
    private const string RawJsonProtocol = "RawJson";

    private const int DreamineProtocolPort = 15001;
    private const int PlainTextProtocolPort = 15002;
    private const int RawAvailableProtocolPort = 15002;
    private const int RawJsonProtocolPort = 15003;

    private readonly IMessageBus _messageBus;

    private bool _isInMemorySubscribed;

    private TcpServerTransport? _tcpServer;
    private TcpClientTransport? _tcpClient;
    private SerialPortTransport? _serialTransport;

    private string _currentServerProtocol = PlainTextProtocol;
    private string _currentClientProtocol = PlainTextProtocol;

    private string _currentSerialProtocol = RawAvailableProtocol;
    private string _currentSerialPortName = string.Empty;
    private int _currentSerialBaudRate = 9600;

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
    public async Task StartTcpServerAsync(string protocol)
    {
        protocol = NormalizeProtocol(protocol);

        if (_tcpServer is not null &&
            _tcpServer.State == ConnectionState.Connected &&
            _currentServerProtocol == protocol)
        {
            return;
        }

        if (_tcpServer is not null)
        {
            await StopTcpServerAsync();
        }

        _currentServerProtocol = protocol;

        EnsureTcpServerChannel(protocol);

        var port = GetTcpPort(protocol);

        _tcpServer = new TcpServerTransport(
            new TcpServerTransportOptions
            {
                Host = "127.0.0.1",
                Port = port
            },
            CreateProtocolAdapter(protocol),
            CreateFrameCodec(protocol));

        _tcpServer.MessageReceived += OnTcpServerMessageReceived;

        await _tcpServer.ConnectAsync();

        Monitor.UpdateChannelState(
            GetTcpServerChannelName(protocol),
            ConnectionState.Connected);
    }

    /// <summary>
    /// \brief 현재 TCP Server를 종료합니다.
    /// </summary>
    public async Task StopTcpServerAsync()
    {
        var protocol = _currentServerProtocol;
        var channelName = GetTcpServerChannelName(protocol);

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
    public async Task SendTcpServerAsync(string protocol, string text)
    {
        protocol = NormalizeProtocol(protocol);

        if (string.IsNullOrWhiteSpace(text))
        {
            text = "Hello from Dreamine TCP Server";
        }

        if (_tcpServer is null ||
            _tcpServer.State != ConnectionState.Connected ||
            _currentServerProtocol != protocol)
        {
            await StartTcpServerAsync(protocol);
        }

        if (_tcpServer is null)
        {
            return;
        }

        var message = CreateTcpMessageByProtocol(
            protocol,
            "Server.Send",
            text);

        var channelName = GetTcpServerChannelName(protocol);

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
    public async Task ConnectTcpClientAsync(string protocol)
    {
        protocol = NormalizeProtocol(protocol);

        if (_tcpClient is not null &&
            _tcpClient.State == ConnectionState.Connected &&
            _currentClientProtocol == protocol)
        {
            return;
        }

        if (_tcpClient is not null)
        {
            await DisconnectTcpClientAsync();
        }

        _currentClientProtocol = protocol;

        EnsureTcpClientChannel(protocol);

        var port = GetTcpPort(protocol);

        _tcpClient = new TcpClientTransport(
            new TcpClientTransportOptions
            {
                Host = "127.0.0.1",
                Port = port
            },
            CreateProtocolAdapter(protocol),
            CreateFrameCodec(protocol));

        _tcpClient.MessageReceived += OnTcpClientMessageReceived;

        await _tcpClient.ConnectAsync();

        Monitor.UpdateChannelState(
            GetTcpClientChannelName(protocol),
            ConnectionState.Connected);
    }

    /// <summary>
    /// \brief 현재 TCP Client 연결을 해제합니다.
    /// </summary>
    public async Task DisconnectTcpClientAsync()
    {
        var protocol = _currentClientProtocol;
        var channelName = GetTcpClientChannelName(protocol);

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
    public async Task SendTcpClientAsync(string protocol, string text)
    {
        protocol = NormalizeProtocol(protocol);

        if (string.IsNullOrWhiteSpace(text))
        {
            text = "Hello from Dreamine TCP Client";
        }

        if (_tcpClient is null ||
            _tcpClient.State != ConnectionState.Connected ||
            _currentClientProtocol != protocol)
        {
            await ConnectTcpClientAsync(protocol);
        }

        if (_tcpClient is null)
        {
            return;
        }

        var message = CreateTcpMessageByProtocol(
            protocol,
            "Client.Send",
            text);

        var channelName = GetTcpClientChannelName(protocol);

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
    /// \brief 선택된 설정으로 Serial Port를 연결합니다.
    /// </summary>
    /// <param name="portName">Serial Port 이름입니다.</param>
    /// <param name="baudRate">BaudRate입니다.</param>
    /// <param name="protocol">프로토콜 이름입니다.</param>
    public async Task ConnectSerialAsync(
        string portName,
        int baudRate,
        string protocol)
    {
        if (string.IsNullOrWhiteSpace(portName))
        {
            return;
        }

        protocol = NormalizeProtocol(protocol);

        if (_serialTransport is not null &&
            _serialTransport.State == ConnectionState.Connected &&
            _currentSerialPortName == portName &&
            _currentSerialBaudRate == baudRate &&
            _currentSerialProtocol == protocol)
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
                "Serial"),
            CreateFrameCodec(protocol));

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
    public async Task SendSerialAsync(string protocol, string text)
    {
        protocol = NormalizeProtocol(protocol);

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
            text);

        var channelName = GetSerialChannelName();

        Monitor.AddSendLog(
            channelName,
            TransportKind.Serial,
            message);

        await _serialTransport.SendAsync(message);
    }

    /// <summary>
    /// \brief 모든 통신 샘플 연결을 종료합니다.
    /// </summary>
    public async Task StopAllAsync()
    {
        await DisconnectSerialAsync();
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
        var channelName = GetTcpServerChannelName(protocol);

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
            $"Echo from Dreamine TCP Server - {receiveText}");

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
        var channelName = GetTcpClientChannelName(protocol);

        RunOnUiThread(() =>
        {
            Monitor.AddReceiveLog(
                channelName,
                TransportKind.Tcp,
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

    private void EnsureTcpServerChannel(string protocol)
    {
        var channelName = GetTcpServerChannelName(protocol);
        var port = GetTcpPort(protocol);

        if (Monitor.Channels.Any(x => x.Name == channelName))
        {
            return;
        }

        Monitor.AddChannel(
            channelName,
            TransportKind.Tcp,
            $"TCP server [{protocol}] on 127.0.0.1:{port}.");
    }

    private void EnsureTcpClientChannel(string protocol)
    {
        var channelName = GetTcpClientChannelName(protocol);
        var port = GetTcpPort(protocol);

        if (Monitor.Channels.Any(x => x.Name == channelName))
        {
            return;
        }

        Monitor.AddChannel(
            channelName,
            TransportKind.Tcp,
            $"TCP client [{protocol}] to 127.0.0.1:{port}.");
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
            $"Serial [{_currentSerialProtocol}] on {_currentSerialPortName}:{_currentSerialBaudRate}.");
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
        string namePrefix = "Tcp")
    {
        var normalizedProtocol = NormalizeProtocol(protocol);

        return normalizedProtocol switch
        {
            DreamineEnvelopeProtocol => new DreamineEnvelopeProtocolAdapter(),

            PlainTextProtocol => new PlainTextProtocolAdapter(
                Encoding.UTF8,
                $"{routePrefix}.plaintext",
                $"{namePrefix}.PlainText"),

            RawAvailableProtocol => new PlainTextProtocolAdapter(
                Encoding.UTF8,
                $"{routePrefix}.raw.available",
                $"{namePrefix}.RawAvailable"),

            RawJsonProtocol => new RawJsonProtocolAdapter(
                $"{routePrefix}.rawjson",
                $"{namePrefix}.RawJson"),

            _ => new PlainTextProtocolAdapter(
                Encoding.UTF8,
                $"{routePrefix}.plaintext",
                $"{namePrefix}.PlainText")
        };
    }

    private static IMessageFrameCodec CreateFrameCodec(string protocol)
    {
        return NormalizeProtocol(protocol) switch
        {
            DreamineEnvelopeProtocol => new LengthPrefixedMessageFrameCodec(),

            PlainTextProtocol => new DelimiterMessageFrameCodec(
                "\r\n",
                Encoding.UTF8,
                1024 * 1024),

            RawAvailableProtocol => new RawAvailableMessageFrameCodec(),

            RawJsonProtocol => new DelimiterMessageFrameCodec(
                "\r\n",
                Encoding.UTF8,
                1024 * 1024),

            _ => new DelimiterMessageFrameCodec(
                "\r\n",
                Encoding.UTF8,
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

    private static string GetTcpServerChannelName(string protocol)
    {
        return NormalizeProtocol(protocol) switch
        {
            DreamineEnvelopeProtocol => "TCP-Server-Dreamine",
            PlainTextProtocol => "TCP-Server-PlainText",
            RawAvailableProtocol => "TCP-Server-RawAvailable",
            RawJsonProtocol => "TCP-Server-RawJson",
            _ => "TCP-Server-PlainText"
        };
    }

    private static string GetTcpClientChannelName(string protocol)
    {
        return NormalizeProtocol(protocol) switch
        {
            DreamineEnvelopeProtocol => "TCP-Client-Dreamine",
            PlainTextProtocol => "TCP-Client-PlainText",
            RawAvailableProtocol => "TCP-Client-RawAvailable",
            RawJsonProtocol => "TCP-Client-RawJson",
            _ => "TCP-Client-PlainText"
        };
    }

    private static MessageEnvelope CreateTcpMessageByProtocol(
        string protocol,
        string direction,
        string text)
    {
        var normalizedProtocol = NormalizeProtocol(protocol);
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
                    ["Protocol"] = PlainTextProtocol
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
                    ["Protocol"] = RawAvailableProtocol
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
                    ["Protocol"] = PlainTextProtocol
                }
            }
        };
    }

    private static MessageEnvelope CreateSerialMessageByProtocol(
        string protocol,
        string direction,
        string text)
    {
        var normalizedProtocol = NormalizeProtocol(protocol);
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
                    ["Protocol"] = PlainTextProtocol
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
                    ["Protocol"] = RawAvailableProtocol
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
                    ["Protocol"] = PlainTextProtocol
                }
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