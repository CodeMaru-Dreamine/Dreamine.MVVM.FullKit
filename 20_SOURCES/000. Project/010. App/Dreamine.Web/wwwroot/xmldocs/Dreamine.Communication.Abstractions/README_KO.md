# Dreamine.Communication.Abstractions

`Dreamine.Communication.Abstractions`는 Dreamine Communication 계열 패키지에서 사용하는 최하위 계약, 모델, 옵션, 연결 생명주기 인터페이스, enum, 공통 예외를 제공하는 패키지입니다.

이 패키지는 실제 통신 프로토콜을 구현하지 않습니다. TCP, UDP, Serial, RabbitMQ, HTTP, InMemory 메시징 및 향후 추가될 통신 어댑터들이 공통으로 사용할 기반 계약만 정의합니다.

[➡️ English Version](./README.md)

## 목적

이 패키지의 목적은 애플리케이션 로직이 특정 통신 기술에 직접 의존하지 않도록 만드는 것입니다.

상위 애플리케이션 코드는 TCP, `SerialPort`, RabbitMQ, HTTP Client, 제조사 전용 통신 라이브러리에 직접 의존하지 않고 `IMessageBus`, `IMessageTransport`, `IMessageProtocolAdapter`, `MessageEnvelope` 같은 추상 계약에 의존해야 합니다.

## 패키지 역할

이 패키지는 Dreamine Communication 아키텍처의 계약 계층입니다.

```text
Dreamine.Communication.Abstractions
    ↑
Dreamine.Communication.Core
    ↑
Sockets / Serial / RabbitMQ / WPF / FullKit
```

`Abstractions`는 `Core`, `Sockets`, `Serial`, `RabbitMQ`, `WPF` 또는 UI/런타임 전용 패키지를 참조하면 안 됩니다.

## 포함 구성

### Interfaces

| 인터페이스 | 역할 |
|---|---|
| `IConnectionLifecycle` | `State`, `ConnectAsync`, `DisconnectAsync`를 가지는 공통 연결 생명주기 계약입니다. |
| `IMessageBus` | 발행/구독 기반 메시징 계약입니다. |
| `IMessageTransport` | 연결 기반 송수신 전송 계층 계약입니다. |
| `IMessageProtocolAdapter` | 외부 프로토콜 payload와 `MessageEnvelope` 간 변환 계약입니다. |
| `IMessageRouter` | 수신된 `MessageEnvelope`를 등록된 핸들러로 라우팅하는 계약입니다. |
| `IMessageSerializer` | `MessageEnvelope` 직렬화/역직렬화 계약입니다. |

### Models

| 모델 | 역할 |
|---|---|
| `MessageEnvelope` | Dreamine Communication 전체에서 사용하는 표준 내부 메시지 단위입니다. |
| `CommunicationError` | 통신 계층 공통 오류 정보 모델입니다. |

### Options

| 옵션 | 역할 |
|---|---|
| `CommunicationOptions` | 통신 이름, 자동 연결, 재연결 정책 같은 공통 통신 설정입니다. |
| `MessageBusOptions` | 기본 라우트, 핸들러 실행 정책 같은 메시지 버스 설정입니다. |
| `TransportOptions` | Host, Port, Serial Port, BaudRate, Timeout 같은 공통 전송 설정입니다. |

### Enums

| Enum | 역할 |
|---|---|
| `ConnectionState` | Disconnected, Connecting, Connected, Disconnecting, Faulted 상태를 표현합니다. |
| `TransportKind` | InMemory, Serial, TCP, UDP, HTTP, RabbitMQ 전송 계열을 표현합니다. |

### Exceptions

| 예외 | 역할 |
|---|---|
| `CommunicationException` | Dreamine Communication 계층의 기본 예외입니다. |
| `CommunicationConnectionException` | 연결 실패 전용 예외입니다. |

## 핵심 개념

### MessageEnvelope

`MessageEnvelope`는 Dreamine Communication 내부에서 사용하는 표준 메시지 단위입니다.

```csharp
public sealed class MessageEnvelope
{
    public string MessageId { get; init; }
    public string Name { get; init; }
    public string Route { get; init; }
    public byte[] Payload { get; init; }
    public IReadOnlyDictionary<string, string> Headers { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
```

권장 사용 기준:

| 속성 | 권장 사용 방식 |
|---|---|
| `MessageId` | 추적과 진단을 위한 고유 메시지 ID입니다. |
| `Name` | `Machine.Start`, `Tcp.RawAvailable.Receive` 같은 사람이 읽기 쉬운 메시지 이름입니다. |
| `Route` | 메시지 버스와 라우터에서 사용하는 라우팅 키입니다. |
| `Payload` | 실제 바이너리 데이터입니다. Text, JSON, 프로토콜 패킷, 장비 데이터 등을 모두 담을 수 있습니다. |
| `Headers` | 프로토콜 이름, ContentType, Encoding, Source, CorrelationId 같은 메타데이터입니다. |
| `CreatedAt` | UTC 기준 메시지 생성 시각입니다. |

### IMessageProtocolAdapter

`IMessageProtocolAdapter`는 외부 프로토콜 형식과 Dreamine 내부 메시지 모델을 분리하는 계약입니다.

```text
외부 바이트
    ↓
Core 패키지의 Frame Codec
    ↓
IMessageProtocolAdapter.Decode(...)
    ↓
MessageEnvelope
```

반대 방향은 다음과 같습니다.

```text
MessageEnvelope
    ↓
IMessageProtocolAdapter.Encode(...)
    ↓
Core 패키지의 Frame Codec
    ↓
외부 바이트
```

이 인터페이스는 여러 구현 패키지에서 필요하기 때문에 `Abstractions`에 위치합니다.

상위 패키지의 프로토콜 어댑터 예시는 다음과 같습니다.

| Adapter | 일반적인 용도 |
|---|---|
| `DreamineEnvelopeProtocolAdapter` | Dreamine 표준 Envelope 직렬화입니다. |
| `PlainTextProtocolAdapter` | 단순 문자열 payload 처리입니다. |
| `RawJsonProtocolAdapter` | 상위 계층이 전송 방식에 의존하지 않도록 JSON payload를 처리합니다. |

### Core Frame Codec과의 관계

Frame Codec은 이 패키지가 아니라 `Dreamine.Communication.Core`에 구현됩니다.

책임 분리는 다음과 같습니다.

| 계층 | 책임 |
|---|---|
| Frame Codec | 바이트 스트림에서 메시지 경계를 분리하거나 기록합니다. |
| Protocol Adapter | 하나의 완성된 payload를 `MessageEnvelope`로 변환하거나 반대로 변환합니다. |
| Transport | TCP, Serial, RabbitMQ 같은 실제 송수신을 담당합니다. |
| Message Bus / Router | 메시지를 Route 기준으로 분배합니다. |

예를 들어 `RawAvailableMessageFrameCodec`은 `Dreamine.Communication.Core`에 위치하는 것이 맞습니다. 이 코덱은 `CRLF`나 길이 Prefix 없이 TCP로 들어온 `test1` 같은 단순 문자열도 수신 가능한 하나의 메시지로 처리할 수 있습니다. 이 기능을 위해 새로운 Abstraction을 추가할 필요는 없습니다. 기존 `IMessageProtocolAdapter` 계약이 payload와 `MessageEnvelope` 간 변환을 이미 담당하기 때문입니다.

권장 조합은 다음과 같습니다.

| 시나리오 | Frame Codec | Protocol Adapter |
|---|---|---|
| Dreamine 내부 표준 통신 | `LengthPrefixedMessageFrameCodec` | `DreamineEnvelopeProtocolAdapter` |
| 구분자 기반 문자열 통신 | `DelimiterMessageFrameCodec` | `PlainTextProtocolAdapter` |
| Hercules 또는 단순 Raw TCP 문자열 테스트 | `RawAvailableMessageFrameCodec` | `PlainTextProtocolAdapter` |
| Raw JSON Line 통신 | `DelimiterMessageFrameCodec` | `RawJsonProtocolAdapter` |

## 인터페이스 책임 기준

### `IConnectionLifecycle`

연결 상태와 명시적인 연결/해제 동작을 가지는 객체에 사용합니다.

```csharp
public interface IConnectionLifecycle
{
    ConnectionState State { get; }
    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
}
```

### `IMessageTransport`

TCP Client, TCP Server, Serial Transport처럼 연결 기반 송수신을 담당하는 컴포넌트에 사용합니다.

```csharp
public interface IMessageTransport : IConnectionLifecycle, IAsyncDisposable
{
    TransportKind Kind { get; }
    event EventHandler<MessageEnvelope>? MessageReceived;
    Task SendAsync(MessageEnvelope message, CancellationToken cancellationToken = default);
}
```

### `IMessageBus`

발행/구독 기반 메시징에 사용합니다.

```csharp
public interface IMessageBus : IConnectionLifecycle, IAsyncDisposable
{
    TransportKind Kind { get; }
    Task PublishAsync(MessageEnvelope message, CancellationToken cancellationToken = default);
    Task SubscribeAsync(
        string route,
        Func<MessageEnvelope, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default);
}
```

### `IMessageRouter`

메시지 분배 책임을 MessageBus 또는 Transport 구현체에서 분리해야 할 때 사용합니다.

### `IMessageSerializer`

`MessageEnvelope` 직렬화 방식을 교체 가능하게 만들 때 사용합니다. 예를 들어 JSON, Binary, 압축, 암호화 직렬화 구현체를 상위 패키지에서 제공할 수 있습니다.

## TransportKind

`TransportKind`는 특정 구현 클래스가 아니라 통신 계열을 나타냅니다.

```text
InMemory
Serial
Tcp
Udp
Http
RabbitMq
```

예시는 다음과 같습니다.

| TransportKind | 가능한 구현체 |
|---|---|
| `InMemory` | `InMemoryMessageBus` |
| `Tcp` | TCP Client/Server Transport |
| `Serial` | RS232/SerialPort Transport |
| `RabbitMq` | RabbitMQ Message Bus |
| `Udp` | UDP Transport |
| `Http` | HTTP 요청/응답 또는 스트리밍 어댑터 |

## ConnectionState

`ConnectionState`는 Transport와 MessageBus 모두에서 사용할 수 있는 공통 상태 모델입니다.

```text
Disconnected → Connecting → Connected → Disconnecting → Disconnected
                                      ↓
                                   Faulted
```

권장 의미는 다음과 같습니다.

| 상태 | 의미 |
|---|---|
| `Disconnected` | 연결되지 않은 상태입니다. |
| `Connecting` | 연결을 시도 중입니다. |
| `Connected` | 통신이 활성화된 상태입니다. |
| `Disconnecting` | 연결 해제 중입니다. |
| `Faulted` | 통신 실패 또는 비정상 상태입니다. |

## Options

### CommunicationOptions

```csharp
var options = new CommunicationOptions
{
    Name = "MachineTcp",
    AutoConnect = true,
    EnableAutoReconnect = true,
    ReconnectIntervalMs = 3000
};
```

### MessageBusOptions

```csharp
var options = new MessageBusOptions
{
    Kind = TransportKind.InMemory,
    DefaultRoute = "machine.event",
    ThrowOnHandlerError = true,
    EnableParallelHandlers = false
};
```

### TransportOptions

```csharp
var options = new TransportOptions
{
    Kind = TransportKind.Tcp,
    Host = "127.0.0.1",
    Port = 15002,
    ReadTimeoutMs = 3000,
    WriteTimeoutMs = 3000
};
```

Serial 통신에서는 다음처럼 사용할 수 있습니다.

```csharp
var options = new TransportOptions
{
    Kind = TransportKind.Serial,
    PortName = "COM1",
    BaudRate = 9600,
    ReadTimeoutMs = 3000,
    WriteTimeoutMs = 3000
};
```

## 예시

```csharp
using Dreamine.Communication.Abstractions.Interfaces;
using Dreamine.Communication.Abstractions.Models;

public sealed class MachineMessageService
{
    private readonly IMessageBus _messageBus;

    public MachineMessageService(IMessageBus messageBus)
    {
        _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
    }

    public Task PublishStartAsync(CancellationToken cancellationToken = default)
    {
        var message = new MessageEnvelope
        {
            Name = "Machine.Start",
            Route = "machine.command.start",
            Payload = Array.Empty<byte>(),
            Headers = new Dictionary<string, string>
            {
                ["ContentType"] = "application/octet-stream",
                ["Source"] = "MachineMessageService"
            }
        };

        return _messageBus.PublishAsync(message, cancellationToken);
    }
}
```

추상화와 구체 `IMessageBus` 구현체만으로 동작하는 엔드투엔드 흐름은 다음과 같습니다.

```csharp
using System.Text;
using Dreamine.Communication.Abstractions.Interfaces;
using Dreamine.Communication.Abstractions.Models;

// IMessageBus는 구체 패키지에서 제공합니다.
// 예: Dreamine.Communication.Core의 InMemoryMessageBus,
//     Dreamine.Communication.RabbitMQ의 RabbitMqMessageBus 등.
IMessageBus bus = /* 구체 구현체 */;

await bus.ConnectAsync();

await bus.SubscribeAsync(
    "machine.command.start",
    (message, _) =>
    {
        var text = Encoding.UTF8.GetString(message.Payload);
        Console.WriteLine($"RX {message.Name}: {text}");
        return Task.CompletedTask;
    });

await bus.PublishAsync(new MessageEnvelope
{
    Name = "Machine.Start",
    Route = "machine.command.start",
    Payload = Encoding.UTF8.GetBytes("go"),
    Headers = new Dictionary<string, string>
    {
        ["ContentType"] = "text/plain"
    }
});

await bus.DisconnectAsync();
```

애플리케이션 코드는 `IMessageBus`와 `MessageEnvelope`에만 의존합니다. InMemory, RabbitMQ, `TransportMessageBusAdapter` 기반 Transport 어느 쪽으로 바꾸어도 이 코드는 변경되지 않습니다.

## 설계 원칙

- 추상 계약은 구체 구현체에 의존하지 않습니다.
- 애플리케이션 레이어가 특정 통신 라이브러리를 직접 참조하지 않게 합니다.
- TCP, Serial, RabbitMQ, UDP, HTTP, MQTT 및 향후 어댑터를 상위 레이어 변경 없이 추가할 수 있어야 합니다.
- 메시지 형식 변환과 프레임 경계 감지는 분리합니다.
- 의존성 역전 원칙과 인터페이스 기반 설계를 따릅니다.
- 패키지는 가볍고 UI 프레임워크에 독립적으로 유지합니다.
- 의존성 방향은 단방향입니다. 구현체가 추상화에 의존해야 하며, 추상화가 구현체에 의존하면 안 됩니다.

## 대상 프레임워크

```text
net8.0
```

이 패키지는 WPF를 필요로 하지 않으며, Windows 전용 UI 프레임워크에 의존하지 않습니다.

## 관련 패키지

- `Dreamine.Communication.Core`
- `Dreamine.Communication.Sockets`
- `Dreamine.Communication.Serial`
- `Dreamine.Communication.RabbitMQ`
- `Dreamine.Communication.Wpf`
- `Dreamine.Communication.FullKit`

## 라이선스

이 프로젝트는 MIT 라이선스를 따릅니다.
