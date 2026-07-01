# Dreamine.Communication.RabbitMQ

`Dreamine.Communication.RabbitMQ`는 Dreamine Communication 패키지 제품군의 RabbitMQ 어댑터 패키지입니다.

이 패키지는 Dreamine Communication의 공통 메시지 계약인 `MessageEnvelope`를 RabbitMQ의 Exchange, Queue, RoutingKey 기반 Publish/Subscribe 메시징 구조와 연결하는 `IMessageBus` 구현체를 제공합니다.

[➡️ English Version](./README.md)

## 설명

Dreamine Communication을 위한 RabbitMQ 메시지 버스 어댑터입니다.

이 패키지는 RabbitMQ를 선택 가능한 Broker 기반 메시지 버스로 통합하되, 상위 애플리케이션 레이어는 `Dreamine.Communication.Abstractions`에만 의존하도록 유지합니다.

## 주요 기능

- RabbitMQ 기반 `IMessageBus` 구현
- RabbitMQ 연결 생명주기 관리
- RabbitMQ Exchange, Queue, RoutingKey 기반 Publish / Subscribe 지원
- `MessageEnvelope` JSON 직렬화 및 역직렬화
- RabbitMQ 토폴로지 선언
  - Exchange
  - Queue
  - Queue Binding
- RabbitMQ 옵션 모델
- RabbitMQ 전용 통신 예외 타입
- Communication Monitor 샘플 연동
- Docker RabbitMQ 서버 기반 검증 시나리오 지원

## 패키지 역할

```text
Dreamine.Communication.Abstractions
    ↑
Dreamine.Communication.RabbitMQ
```

`Dreamine.Communication.RabbitMQ`는 추상화 계약에 의존하며, RabbitMQ에 대한 구체 구현을 제공합니다.

애플리케이션 코드는 RabbitMQ 구현체에 직접 의존하지 말고, `IMessageBus`, `MessageEnvelope`, 공통 통신 계약에 의존하는 구조를 유지해야 합니다.

## 주요 구성 요소

### RabbitMqMessageBus

`RabbitMqMessageBus`는 `IMessageBus`를 구현합니다.

책임:

- RabbitMQ 서버 연결
- Exchange, Queue, Queue Binding 토폴로지 선언
- `MessageEnvelope` 메시지 발행
- 지정 Route 구독 및 수신 메시지 Handler 전달
- RabbitMQ Connection / Channel 생명주기 관리
- `ConnectionState` 계약 기반 연결 상태 관리

### RabbitMqMessageBusOptions

RabbitMQ 연결 및 라우팅 설정을 정의합니다.

대표 필드:

- `HostName`
- `Port`
- `VirtualHost`
- `UserName`
- `Password`
- `ExchangeName`
- `QueueName`
- `RoutingKey`

### RabbitMqCommunicationException

RabbitMQ 통신 레이어에서 발생하는 오류를 표현하기 위한 전용 예외 타입입니다.

## 기본 사용 예시

```csharp
using Dreamine.Communication.Abstractions.Models;
using Dreamine.Communication.RabbitMQ.Buses;
using Dreamine.Communication.RabbitMQ.Options;

var bus = new RabbitMqMessageBus(
    new RabbitMqMessageBusOptions
    {
        HostName = "localhost",
        Port = 5672,
        VirtualHost = "/",
        UserName = "guest",
        Password = "guest",
        ExchangeName = "dreamine.default.exchange",
        QueueName = "dreamine.default.queue",
        RoutingKey = "dreamine.default.route"
    });

await bus.ConnectAsync();

await bus.SubscribeAsync(
    "dreamine.default.route",
    async (message, cancellationToken) =>
    {
        var text = System.Text.Encoding.UTF8.GetString(message.Payload);
        Console.WriteLine(text);
        await Task.CompletedTask;
    });

await bus.PublishAsync(
    new MessageEnvelope
    {
        Name = "RabbitMQ.Publish",
        Route = "dreamine.default.route",
        Payload = System.Text.Encoding.UTF8.GetBytes("test"),
        Headers = new Dictionary<string, string>
        {
            ["ContentType"] = "text/plain",
            ["Protocol"] = "RabbitMQ"
        }
    });

await bus.DisconnectAsync();
```

## RabbitMQ 테스트 서버

로컬 검증은 Docker로 RabbitMQ를 실행하는 방식이 가장 간단합니다.

```bash
docker run -d --name dreamine-rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
```

RabbitMQ Management UI:

```text
http://localhost:15672
```

기본 계정:

```text
guest / guest
```

샘플 설정:

```text
Host        localhost
Port        5672
VirtualHost /
User        guest
Password    guest
Exchange    dreamine.default.exchange
Queue       dreamine.default.queue
RoutingKey  dreamine.default.route
```

## 검증된 시나리오

RabbitMQ 어댑터는 Dreamine Communication 샘플에서 검증되었습니다.

검증 흐름:

```text
Connect
→ Subscribe
→ Publish
→ Receive
→ Monitor SEND / RECV 로그 확인
```

검증 항목:

- RabbitMQ Docker 컨테이너 실행
- RabbitMQ Management UI 접속
- RabbitMQ 연결
- Exchange / Queue / RoutingKey 토폴로지 선언
- 메시지 발행
- 구독 Handler를 통한 메시지 수신
- `MessageEnvelope` JSON 직렬화 및 역직렬화
- Communication Monitor 채널 상태 갱신
- Communication Monitor SEND / RECV 메시지 로그 기록

## 설계 원칙

- Broker 전용 구현은 상위 레이어로 누출하지 않습니다.
- `Dreamine.Communication.Abstractions` 계약에 의존합니다.
- 패키지 책임을 작고 명확하게 유지합니다.
- 단방향 의존성 흐름을 유지합니다.
- RabbitMQ는 선택 가능하고 교체 가능한 어댑터로 유지합니다.
- 애플리케이션 로직은 RabbitMQ 세부 구현을 몰라도 `IMessageBus`만으로 동작해야 합니다.
- RabbitMQ.Client 타입이 애플리케이션 레벨 코드로 누출되지 않게 합니다.

## 의존성

- `Dreamine.Communication.Abstractions`
- `RabbitMQ.Client`

## 외부 라이선스 안내

이 패키지는 `RabbitMQ.Client`를 사용합니다.

`RabbitMQ.Client`는 아래 라이선스의 Dual License 구조입니다.

- Apache License 2.0
- Mozilla Public License 2.0

이 패키지는 `RabbitMQ.Client`를 Apache License 2.0 조건으로 사용합니다.

## 대상 프레임워크

```text
net8.0
```

## 관련 패키지

- `Dreamine.Communication.Abstractions`
- `Dreamine.Communication.Core`
- `Dreamine.Communication.Sockets`
- `Dreamine.Communication.Serial`
- `Dreamine.Communication.RabbitMQ`
- `Dreamine.Communication.FullKit`
- `Dreamine.Communication.Wpf`

## 라이선스

이 프로젝트는 MIT License를 따릅니다.
