using SampleSmart.Pages.PageSub.CommunicationTabs;

namespace SampleSmart.Pages.PageSub.CommunicationTabs.RabbitMQ;

/// <summary>
/// \brief RabbitMQ 테스트 이벤트 처리 클래스입니다.
/// </summary>
public sealed class RabbitMqTestViewEvent
{
    private readonly CommunicationSampleRuntime _runtime;

    /// <summary>
    /// \brief RabbitMqTestViewEvent 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="runtime">Communication 샘플 공유 Runtime입니다.</param>
    public RabbitMqTestViewEvent(CommunicationSampleRuntime runtime)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));

        Host = "localhost";
        Port = 5672;
        VirtualHost = "/";
        UserName = "guest";
        Password = "guest";
        ExchangeName = "dreamine.sample.exchange";
        QueueName = "dreamine.sample.queue";
        RoutingKey = "dreamine.sample.route";
        MessageText = "test";
    }

    /// <summary>
    /// \brief RabbitMQ Host입니다.
    /// </summary>
    public string Host { get; set; }

    /// <summary>
    /// \brief RabbitMQ Port입니다.
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// \brief RabbitMQ VirtualHost입니다.
    /// </summary>
    public string VirtualHost { get; set; }

    /// <summary>
    /// \brief RabbitMQ 사용자 이름입니다.
    /// </summary>
    public string UserName { get; set; }

    /// <summary>
    /// \brief RabbitMQ 비밀번호입니다.
    /// </summary>
    public string Password { get; set; }

    /// <summary>
    /// \brief RabbitMQ Exchange 이름입니다.
    /// </summary>
    public string ExchangeName { get; set; }

    /// <summary>
    /// \brief RabbitMQ Queue 이름입니다.
    /// </summary>
    public string QueueName { get; set; }

    /// <summary>
    /// \brief RabbitMQ RoutingKey입니다.
    /// </summary>
    public string RoutingKey { get; set; }

    /// <summary>
    /// \brief RabbitMQ 송신 메시지입니다.
    /// </summary>
    public string MessageText { get; set; }

    /// <summary>
    /// \brief 현재 RabbitMQ 선택 상태 요약 문자열입니다.
    /// </summary>
    public string RabbitMqSelectionSummary =>
        $"Host={Host}:{Port}, VirtualHost={VirtualHost}, Exchange={ExchangeName}, Queue={QueueName}, RoutingKey={RoutingKey}";

    /// <summary>
    /// \brief RabbitMQ 연결을 시작합니다.
    /// </summary>
    public void ConnectRabbitMq()
    {
        _ = _runtime.ConnectRabbitMqAsync(
            Host,
            Port,
            VirtualHost,
            UserName,
            Password,
            ExchangeName,
            QueueName,
            RoutingKey);
    }

    /// <summary>
    /// \brief RabbitMQ 구독을 시작합니다.
    /// </summary>
    public void SubscribeRabbitMq()
    {
        _ = _runtime.SubscribeRabbitMqAsync(
            ExchangeName,
            QueueName,
            RoutingKey);
    }

    /// <summary>
    /// \brief RabbitMQ 메시지를 발행합니다.
    /// </summary>
    public void PublishRabbitMq()
    {
        if (string.IsNullOrWhiteSpace(MessageText))
        {
            MessageText = "test";
        }

        _ = _runtime.PublishRabbitMqAsync(
            ExchangeName,
            RoutingKey,
            MessageText);
    }

    /// <summary>
    /// \brief RabbitMQ 연결을 해제합니다.
    /// </summary>
    public void DisconnectRabbitMq()
    {
        _ = _runtime.DisconnectRabbitMqAsync();
    }
}