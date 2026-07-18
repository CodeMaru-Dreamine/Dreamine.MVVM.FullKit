using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;
using Microsoft.Extensions.Logging;

namespace SampleSmart.Pages.PageSub.CommunicationTabs.RabbitMQ;

/// <summary>
/// \if KO
/// <para>\brief RabbitMQ 테스트 ViewModel입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates rabbit mq test view model functionality and related state.</para>
/// \endif
/// </summary>
public partial class RabbitMqTestViewModel : ViewModelBase
{
    /// <summary>
    /// \if KO
    /// <para>\brief RabbitMQ 테스트 이벤트 처리 객체입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the event value.</para>
    /// \endif
    /// </summary>
    [DreamineEvent]
    private RabbitMqTestViewEvent _event;

    /// <summary>
    /// \if KO
    /// <para>\brief RabbitMQ Host입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the host value.</para>
    /// \endif
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
    /// \if KO
    /// <para>\brief RabbitMQ Port입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the port value.</para>
    /// \endif
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
    /// \if KO
    /// <para>\brief RabbitMQ VirtualHost입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the virtual host value.</para>
    /// \endif
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
    /// \if KO
    /// <para>\brief RabbitMQ 사용자 이름입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the user name value.</para>
    /// \endif
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
    /// \if KO
    /// <para>\brief RabbitMQ 비밀번호입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the password value.</para>
    /// \endif
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
    /// \if KO
    /// <para>\brief RabbitMQ Exchange 이름입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the exchange name value.</para>
    /// \endif
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
    /// \if KO
    /// <para>\brief RabbitMQ Queue 이름입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the queue name value.</para>
    /// \endif
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
    /// \if KO
    /// <para>\brief RabbitMQ RoutingKey입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the routing key value.</para>
    /// \endif
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
    /// \if KO
    /// <para>\brief RabbitMQ 송신 메시지입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the message text value.</para>
    /// \endif
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
    /// \if KO
    /// <para>\brief 현재 RabbitMQ 선택 상태 요약 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the rabbit mq selection summary value.</para>
    /// \endif
    /// </summary>
    public string RabbitMqSelectionSummary => Event.RabbitMqSelectionSummary;

    /// <summary>
    /// \if KO
    /// <para>\brief RabbitMQ 연결 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the connect rabbit mq operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.ConnectRabbitMq")]
    private partial void ConnectRabbitMq();

    /// <summary>
    /// \if KO
    /// <para>\brief RabbitMQ 구독 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the subscribe rabbit mq operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.SubscribeRabbitMq")]
    private partial void SubscribeRabbitMq();

    /// <summary>
    /// \if KO
    /// <para>\brief RabbitMQ 발행 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the publish rabbit mq operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.PublishRabbitMq")]
    private partial void PublishRabbitMq();

    /// <summary>
    /// \if KO
    /// <para>\brief RabbitMQ 연결 해제 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the disconnect rabbit mq operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.DisconnectRabbitMq")]
    private partial void DisconnectRabbitMq();

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="RabbitMqTestViewModel"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="RabbitMqTestViewModel"/> class.</para>
    /// \endif
    /// </summary>
    /// <param name="event">
    /// \if KO
    /// <para>event에 사용할 <c>RabbitMqTestViewEvent</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The event handler used by the RabbitMQ test view.</para>
    /// \endif
    /// </param>
    public RabbitMqTestViewModel(RabbitMqTestViewEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        _event = @event;
    }
}