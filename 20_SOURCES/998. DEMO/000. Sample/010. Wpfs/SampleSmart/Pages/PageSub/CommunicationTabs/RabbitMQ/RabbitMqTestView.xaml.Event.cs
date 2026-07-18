using SampleSmart.Pages.PageSub.CommunicationTabs;

namespace SampleSmart.Pages.PageSub.CommunicationTabs.RabbitMQ;

/// <summary>
/// \if KO
/// <para>\brief RabbitMQ 테스트 이벤트 처리 클래스입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates rabbit mq test view event functionality and related state.</para>
/// \endif
/// </summary>
public sealed class RabbitMqTestViewEvent
{
    /// <summary>
    /// \if KO
    /// <para>runtime 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the runtime value.</para>
    /// \endif
    /// </summary>
    private readonly CommunicationSampleRuntime _runtime;

    /// <summary>
    /// \if KO
    /// <para>\brief RabbitMqTestViewEvent 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="RabbitMqTestViewEvent"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="runtime">
    /// \if KO
    /// <para>Communication 샘플 공유 Runtime입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CommunicationSampleRuntime</c> value used for runtime.</para>
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
    /// \if KO
    /// <para>\brief RabbitMQ Host입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the host value.</para>
    /// \endif
    /// </summary>
    public string Host { get; set; }

    /// <summary>
    /// \if KO
    /// <para>\brief RabbitMQ Port입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the port value.</para>
    /// \endif
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// \if KO
    /// <para>\brief RabbitMQ VirtualHost입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the virtual host value.</para>
    /// \endif
    /// </summary>
    public string VirtualHost { get; set; }

    /// <summary>
    /// \if KO
    /// <para>\brief RabbitMQ 사용자 이름입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the user name value.</para>
    /// \endif
    /// </summary>
    public string UserName { get; set; }

    /// <summary>
    /// \if KO
    /// <para>\brief RabbitMQ 비밀번호입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the password value.</para>
    /// \endif
    /// </summary>
    public string Password { get; set; }

    /// <summary>
    /// \if KO
    /// <para>\brief RabbitMQ Exchange 이름입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the exchange name value.</para>
    /// \endif
    /// </summary>
    public string ExchangeName { get; set; }

    /// <summary>
    /// \if KO
    /// <para>\brief RabbitMQ Queue 이름입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the queue name value.</para>
    /// \endif
    /// </summary>
    public string QueueName { get; set; }

    /// <summary>
    /// \if KO
    /// <para>\brief RabbitMQ RoutingKey입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the routing key value.</para>
    /// \endif
    /// </summary>
    public string RoutingKey { get; set; }

    /// <summary>
    /// \if KO
    /// <para>\brief RabbitMQ 송신 메시지입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the message text value.</para>
    /// \endif
    /// </summary>
    public string MessageText { get; set; }

    /// <summary>
    /// \if KO
    /// <para>\brief 현재 RabbitMQ 선택 상태 요약 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the rabbit mq selection summary value.</para>
    /// \endif
    /// </summary>
    public string RabbitMqSelectionSummary =>
        $"Host={Host}:{Port}, VirtualHost={VirtualHost}, Exchange={ExchangeName}, Queue={QueueName}, RoutingKey={RoutingKey}";

    /// <summary>
    /// \if KO
    /// <para>\brief RabbitMQ 연결을 시작합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the connect rabbit mq operation.</para>
    /// \endif
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
    /// \if KO
    /// <para>\brief RabbitMQ 구독을 시작합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the subscribe rabbit mq operation.</para>
    /// \endif
    /// </summary>
    public void SubscribeRabbitMq()
    {
        _ = _runtime.SubscribeRabbitMqAsync(
            ExchangeName,
            QueueName,
            RoutingKey);
    }

    /// <summary>
    /// \if KO
    /// <para>\brief RabbitMQ 메시지를 발행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the publish rabbit mq operation.</para>
    /// \endif
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
    /// \if KO
    /// <para>\brief RabbitMQ 연결을 해제합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the disconnect rabbit mq operation.</para>
    /// \endif
    /// </summary>
    public void DisconnectRabbitMq()
    {
        _ = _runtime.DisconnectRabbitMqAsync();
    }
}