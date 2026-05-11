using System.Text;
using System.Windows;
using Dreamine.Communication.Abstractions.Enums;
using Dreamine.Communication.Abstractions.Interfaces;
using Dreamine.Communication.Abstractions.Models;
using Dreamine.Communication.Core.Buses;
using Dreamine.Communication.Sockets.Clients;
using Dreamine.Communication.Sockets.Options;
using Dreamine.Communication.Sockets.Servers;
using Dreamine.Communication.Wpf.ViewModels;

namespace SampleSmart.Pages.PageSub;

/// <summary>
/// \brief Dreamine Communication 모니터 샘플 페이지 이벤트 처리 클래스입니다.
/// </summary>
public sealed class PageCommunicationMonitorEvent
{
    private const string ChannelName = "InMemory-Communication";
    private const string RouteName = "sample.communication.message";

    private const string TcpServerChannelName = "TCP-Server";
    private const string TcpClientChannelName = "TCP-Client";
    private const string TcpRouteName = "sample.communication.tcp";
    private const int TcpPort = 15001;

    private readonly IMessageBus _messageBus;
    private bool _isSubscribed;

    private TcpServerTransport? _tcpServer;
    private TcpClientTransport? _tcpClient;

    /// <summary>
    /// \brief PageCommunicationMonitorEvent 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    public PageCommunicationMonitorEvent()
        : this(new InMemoryMessageBus())
    {
    }

    /// <summary>
    /// \brief PageCommunicationMonitorEvent 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="messageBus">샘플에서 사용할 메시지 버스입니다.</param>
    public PageCommunicationMonitorEvent(IMessageBus messageBus)
    {
        _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));

        Monitor = new CommunicationMonitorViewModel();

        _ = SubscribeAsync();
    }

    /// <summary>
    /// \brief Communication Monitor ViewModel입니다.
    /// </summary>
    public CommunicationMonitorViewModel Monitor { get; }

    /// <summary>
    /// \brief InMemory 채널을 추가합니다.
    /// </summary>
    public void AddChannel()
    {
        if (Monitor.Channels.Any(x => x.Name == ChannelName))
        {
            return;
        }

        Monitor.AddChannel(
            ChannelName,
            TransportKind.InMemory,
            "SampleSmart in-memory communication channel.");
    }

    /// <summary>
    /// \brief InMemory 메시지 버스에 연결합니다.
    /// </summary>
    public void Connect()
    {
        _ = ConnectAsync();
    }

    /// <summary>
    /// \brief InMemory 메시지 버스 연결을 해제합니다.
    /// </summary>
    public void Disconnect()
    {
        _ = DisconnectAsync();
    }

    /// <summary>
    /// \brief InMemory 테스트 송신 메시지를 발행합니다.
    /// </summary>
    public void SendTest()
    {
        _ = SendTestAsync();
    }

    /// <summary>
    /// \brief InMemory 수동 수신 테스트 로그를 추가합니다.
    /// </summary>
    public void ReceiveTest()
    {
        EnsureChannel();

        var message = CreateMessage(
            "Sample.Receive",
            "sample.communication.manual-receive",
            $"Hello from SampleSmart RECEIVE - {DateTime.Now:HH:mm:ss.fff}");

        Monitor.AddReceiveLog(ChannelName, TransportKind.InMemory, message);
    }

    /// <summary>
    /// \brief TCP 서버를 시작합니다.
    /// </summary>
    public void StartTcpServer()
    {
        _ = StartTcpServerAsync();
    }

    /// <summary>
    /// \brief TCP 클라이언트를 연결합니다.
    /// </summary>
    public void ConnectTcpClient()
    {
        _ = ConnectTcpClientAsync();
    }

    /// <summary>
    /// \brief TCP 테스트 메시지를 전송합니다.
    /// </summary>
    public void SendTcp()
    {
        _ = SendTcpAsync();
    }

    /// <summary>
    /// \brief TCP 서버와 클라이언트를 중지합니다.
    /// </summary>
    public void StopTcp()
    {
        _ = StopTcpAsync();
    }

    private void EnsureChannel()
    {
        if (Monitor.Channels.Any(x => x.Name == ChannelName))
        {
            return;
        }

        AddChannel();
    }

    private async Task SubscribeAsync()
    {
        if (_isSubscribed)
        {
            return;
        }

        _isSubscribed = true;

        await _messageBus.SubscribeAsync(
            RouteName,
            (message, _) =>
            {
                RunOnUiThread(() =>
                {
                    Monitor.AddReceiveLog(ChannelName, TransportKind.InMemory, message);
                });

                return Task.CompletedTask;
            });
    }

    private async Task ConnectAsync()
    {
        EnsureChannel();

        await _messageBus.ConnectAsync();

        Monitor.UpdateChannelState(ChannelName, ConnectionState.Connected);
    }

    private async Task DisconnectAsync()
    {
        EnsureChannel();

        await _messageBus.DisconnectAsync();

        Monitor.UpdateChannelState(ChannelName, ConnectionState.Disconnected);
    }

    private async Task SendTestAsync()
    {
        EnsureChannel();

        if (_messageBus.State != ConnectionState.Connected)
        {
            await ConnectAsync();
        }

        var message = CreateMessage(
            "Sample.Send",
            RouteName,
            $"Hello from SampleSmart SEND - {DateTime.Now:HH:mm:ss.fff}");

        Monitor.AddSendLog(ChannelName, TransportKind.InMemory, message);

        await _messageBus.PublishAsync(message);
    }

    private async Task StartTcpServerAsync()
    {
        EnsureTcpServerChannel();

        if (_tcpServer is not null && _tcpServer.State == ConnectionState.Connected)
        {
            return;
        }

        _tcpServer = new TcpServerTransport(new TcpServerTransportOptions
        {
            Host = "127.0.0.1",
            Port = TcpPort
        });

        _tcpServer.MessageReceived += OnTcpServerMessageReceived;

        await _tcpServer.ConnectAsync();

        Monitor.UpdateChannelState(TcpServerChannelName, ConnectionState.Connected);
    }

    private async Task ConnectTcpClientAsync()
    {
        EnsureTcpClientChannel();

        if (_tcpServer is null || _tcpServer.State != ConnectionState.Connected)
        {
            await StartTcpServerAsync();
        }

        if (_tcpClient is not null && _tcpClient.State == ConnectionState.Connected)
        {
            return;
        }

        _tcpClient = new TcpClientTransport(new TcpClientTransportOptions
        {
            Host = "127.0.0.1",
            Port = TcpPort
        });

        _tcpClient.MessageReceived += OnTcpClientMessageReceived;

        await _tcpClient.ConnectAsync();

        Monitor.UpdateChannelState(TcpClientChannelName, ConnectionState.Connected);
    }

    private async Task SendTcpAsync()
    {
        EnsureTcpClientChannel();

        if (_tcpClient is null || _tcpClient.State != ConnectionState.Connected)
        {
            await ConnectTcpClientAsync();
        }

        if (_tcpClient is null)
        {
            return;
        }

        var message = CreateMessage(
            "Tcp.Client.Send",
            TcpRouteName,
            $"Hello TCP from SampleSmart Client - {DateTime.Now:HH:mm:ss.fff}");

        Monitor.AddSendLog(TcpClientChannelName, TransportKind.Tcp, message);

        await _tcpClient.SendAsync(message);
    }

    private async Task StopTcpAsync()
    {
        if (_tcpClient is not null)
        {
            _tcpClient.MessageReceived -= OnTcpClientMessageReceived;
            await _tcpClient.DisposeAsync();
            _tcpClient = null;
        }

        if (_tcpServer is not null)
        {
            _tcpServer.MessageReceived -= OnTcpServerMessageReceived;
            await _tcpServer.DisposeAsync();
            _tcpServer = null;
        }

        if (Monitor.Channels.Any(x => x.Name == TcpClientChannelName))
        {
            Monitor.UpdateChannelState(TcpClientChannelName, ConnectionState.Disconnected);
        }

        if (Monitor.Channels.Any(x => x.Name == TcpServerChannelName))
        {
            Monitor.UpdateChannelState(TcpServerChannelName, ConnectionState.Disconnected);
        }
    }

    private async void OnTcpServerMessageReceived(object? sender, MessageEnvelope message)
    {
        RunOnUiThread(() =>
        {
            Monitor.AddReceiveLog(TcpServerChannelName, TransportKind.Tcp, message);
        });

        if (_tcpServer is null || _tcpServer.State != ConnectionState.Connected)
        {
            return;
        }

        var echoMessage = CreateMessage(
            "Tcp.Server.Echo",
            TcpRouteName,
            $"Echo from TCP Server - {DateTime.Now:HH:mm:ss.fff}");

        RunOnUiThread(() =>
        {
            Monitor.AddSendLog(TcpServerChannelName, TransportKind.Tcp, echoMessage);
        });

        await _tcpServer.SendAsync(echoMessage);
    }

    private void OnTcpClientMessageReceived(object? sender, MessageEnvelope message)
    {
        RunOnUiThread(() =>
        {
            Monitor.AddReceiveLog(TcpClientChannelName, TransportKind.Tcp, message);
        });
    }

    private void EnsureTcpServerChannel()
    {
        if (Monitor.Channels.Any(x => x.Name == TcpServerChannelName))
        {
            return;
        }

        Monitor.AddChannel(
            TcpServerChannelName,
            TransportKind.Tcp,
            $"TCP server loopback channel on 127.0.0.1:{TcpPort}.");
    }

    private void EnsureTcpClientChannel()
    {
        if (Monitor.Channels.Any(x => x.Name == TcpClientChannelName))
        {
            return;
        }

        Monitor.AddChannel(
            TcpClientChannelName,
            TransportKind.Tcp,
            $"TCP client loopback channel to 127.0.0.1:{TcpPort}.");
    }

    private static MessageEnvelope CreateMessage(
        string name,
        string route,
        string text)
    {
        return new MessageEnvelope
        {
            Name = name,
            Route = route,
            Payload = Encoding.UTF8.GetBytes(text)
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