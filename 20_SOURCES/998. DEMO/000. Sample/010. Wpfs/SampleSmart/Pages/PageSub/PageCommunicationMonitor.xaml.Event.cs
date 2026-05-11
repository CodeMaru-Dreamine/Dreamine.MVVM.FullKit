using System.Text;
using Dreamine.Communication.Abstractions.Enums;
using Dreamine.Communication.Abstractions.Interfaces;
using Dreamine.Communication.Abstractions.Models;
using Dreamine.Communication.Core.Buses;
using Dreamine.Communication.Wpf.ViewModels;

namespace SampleSmart.Pages.PageSub;

/// <summary>
/// \brief Dreamine Communication 모니터 샘플 페이지 이벤트 처리 클래스입니다.
/// </summary>
public sealed class PageCommunicationMonitorEvent
{
    private const string ChannelName = "InMemory-Communication";
    private const string RouteName = "sample.communication.message";

    private readonly IMessageBus _messageBus;
    private bool _isSubscribed;

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
    /// \brief 채널을 추가합니다.
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
    /// \brief 메시지 버스에 연결합니다.
    /// </summary>
    public void Connect()
    {
        _ = ConnectAsync();
    }

    /// <summary>
    /// \brief 메시지 버스 연결을 해제합니다.
    /// </summary>
    public void Disconnect()
    {
        _ = DisconnectAsync();
    }

    /// <summary>
    /// \brief 테스트 송신 메시지를 발행합니다.
    /// </summary>
    public void SendTest()
    {
        _ = SendTestAsync();
    }

    /// <summary>
    /// \brief 수동 수신 테스트 로그를 추가합니다.
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
                Monitor.AddReceiveLog(ChannelName, TransportKind.InMemory, message);
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
}