using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;
using Microsoft.Extensions.Logging;

namespace SampleSmart.Pages.PageSub.CommunicationTabs.RabbitMQ;

/// <summary>
/// \brief RabbitMQ 테스트 ViewModel입니다.
/// </summary>
public partial class RabbitMqTestViewModel : ViewModelBase
{
    /// <summary>
    /// \brief RabbitMQ 테스트 이벤트 처리 객체입니다.
    /// </summary>
    [DreamineEvent]
    private RabbitMqTestViewEvent _event;

    /// <summary>
    /// \brief RabbitMQ Host입니다.
    /// </summary>
    public string Host
    {
        get => Event.Host;
        set
        {
            if (Event.Host == value)
            {
                return;
            }

            Event.Host = value;
            OnPropertyChanged(nameof(Host));
            OnPropertyChanged(nameof(RabbitMqSelectionSummary));
        }
    }

    /// <summary>
    /// \brief RabbitMQ Port입니다.
    /// </summary>
    public int Port
    {
        get => Event.Port;
        set
        {
            if (Event.Port == value)
            {
                return;
            }

            Event.Port = value;
            OnPropertyChanged(nameof(Port));
            OnPropertyChanged(nameof(RabbitMqSelectionSummary));
        }
    }

    /// <summary>
    /// \brief RabbitMQ VirtualHost입니다.
    /// </summary>
    public string VirtualHost
    {
        get => Event.VirtualHost;
        set
        {
            if (Event.VirtualHost == value)
            {
                return;
            }

            Event.VirtualHost = value;
            OnPropertyChanged(nameof(VirtualHost));
            OnPropertyChanged(nameof(RabbitMqSelectionSummary));
        }
    }

    /// <summary>
    /// \brief RabbitMQ 사용자 이름입니다.
    /// </summary>
    public string UserName
    {
        get => Event.UserName;
        set
        {
            if (Event.UserName == value)
            {
                return;
            }

            Event.UserName = value;
            OnPropertyChanged(nameof(UserName));
            OnPropertyChanged(nameof(RabbitMqSelectionSummary));
        }
    }

    /// <summary>
    /// \brief RabbitMQ 비밀번호입니다.
    /// </summary>
    public string Password
    {
        get => Event.Password;
        set
        {
            if (Event.Password == value)
            {
                return;
            }

            Event.Password = value;
            OnPropertyChanged(nameof(Password));
        }
    }

    /// <summary>
    /// \brief RabbitMQ Exchange 이름입니다.
    /// </summary>
    public string ExchangeName
    {
        get => Event.ExchangeName;
        set
        {
            if (Event.ExchangeName == value)
            {
                return;
            }

            Event.ExchangeName = value;
            OnPropertyChanged(nameof(ExchangeName));
            OnPropertyChanged(nameof(RabbitMqSelectionSummary));
        }
    }

    /// <summary>
    /// \brief RabbitMQ Queue 이름입니다.
    /// </summary>
    public string QueueName
    {
        get => Event.QueueName;
        set
        {
            if (Event.QueueName == value)
            {
                return;
            }

            Event.QueueName = value;
            OnPropertyChanged(nameof(QueueName));
            OnPropertyChanged(nameof(RabbitMqSelectionSummary));
        }
    }

    /// <summary>
    /// \brief RabbitMQ RoutingKey입니다.
    /// </summary>
    public string RoutingKey
    {
        get => Event.RoutingKey;
        set
        {
            if (Event.RoutingKey == value)
            {
                return;
            }

            Event.RoutingKey = value;
            OnPropertyChanged(nameof(RoutingKey));
            OnPropertyChanged(nameof(RabbitMqSelectionSummary));
        }
    }

    /// <summary>
    /// \brief RabbitMQ 송신 메시지입니다.
    /// </summary>
    public string MessageText
    {
        get => Event.MessageText;
        set
        {
            if (Event.MessageText == value)
            {
                return;
            }

            Event.MessageText = value;
            OnPropertyChanged(nameof(MessageText));
        }
    }

    /// <summary>
    /// \brief 현재 RabbitMQ 선택 상태 요약 문자열입니다.
    /// </summary>
    public string RabbitMqSelectionSummary => Event.RabbitMqSelectionSummary;

    /// <summary>
    /// \brief RabbitMQ 연결 명령입니다.
    /// </summary>
    [DreamineCommand("Event.ConnectRabbitMq")]
    private partial void ConnectRabbitMq();

    /// <summary>
    /// \brief RabbitMQ 구독 명령입니다.
    /// </summary>
    [DreamineCommand("Event.SubscribeRabbitMq")]
    private partial void SubscribeRabbitMq();

    /// <summary>
    /// \brief RabbitMQ 발행 명령입니다.
    /// </summary>
    [DreamineCommand("Event.PublishRabbitMq")]
    private partial void PublishRabbitMq();

    /// <summary>
    /// \brief RabbitMQ 연결 해제 명령입니다.
    /// </summary>
    [DreamineCommand("Event.DisconnectRabbitMq")]
    private partial void DisconnectRabbitMq();
}