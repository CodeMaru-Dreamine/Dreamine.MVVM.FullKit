# Dreamine.Communication.Core

`Dreamine.Communication.Core`는 Dreamine Communication 계열 패키지의 일부입니다.

이 패키지는 구체 통신 어댑터들이 공통으로 사용하는 런타임 계층을 제공합니다. TCP, Serial, RabbitMQ, WPF를 직접 구현하지 않습니다. 구체 Transport는 이 패키지에 의존하고, Protocol Adapter와 Frame Codec을 조합해서 사용합니다.

[➡️ English Version](./README.md)

## 설명

Dreamine Communication을 위한 Core MessageBus, Routing, Serialization, Protocol Adapter, Message Framing, Transport-to-MessageBus Adapter 유틸리티를 제공합니다.

## 패키지 역할

```text
Dreamine.Communication.Abstractions
    ↑
Dreamine.Communication.Core
    ↑
Sockets / Serial / RabbitMQ / WPF
```

`Core`는 Transport와 독립적인 규칙을 담당합니다.

- `MessageEnvelope`를 직렬화하는 방법
- 외부 Payload를 `MessageEnvelope`로 변환하는 방법
- 바이트 스트림에서 메시지 경계를 나누는 방법
- 애플리케이션 내부에서 메시지를 라우팅하거나 발행하는 방법

Socket 연결, Serial Port 연결, RabbitMQ 연결, WPF UI 로직은 `Core`의 책임이 아닙니다.

## 주요 기능

- 메모리 기반 메시지 버스
- 메시지 Route Dispatcher
- `MessageEnvelope` JSON 직렬화
- 내부/외부 Payload 형식 변환을 위한 Protocol Adapter
- TCP 같은 바이트 스트림을 위한 Frame Codec
- Transport-to-MessageBus 어댑터

## 주요 구성 요소

### Message Bus

| 타입 | 역할 |
|---|---|
| `InMemoryMessageBus` | 프로세스 내부 Publish/Subscribe 메시지 버스입니다. |
| `TransportMessageBusAdapter` | `IMessageTransport`를 `IMessageBus`처럼 사용할 수 있게 감싸는 어댑터입니다. |

### Routing

| 타입 | 역할 |
|---|---|
| `MessageRouter` | Route 기준으로 Handler를 등록하고 `MessageEnvelope`를 Dispatch합니다. |
| `MessageHandlerRegistration` | Handler 등록 메타데이터를 표현합니다. |

### Serialization

| 타입 | 역할 |
|---|---|
| `JsonMessageSerializer` | `System.Text.Json` 기반으로 `MessageEnvelope`를 직렬화/역직렬화합니다. |

### Protocol Adapters

Protocol Adapter는 원시 Payload 바이트와 Dreamine `MessageEnvelope` 사이의 변환을 담당합니다.

| 타입 | 사용 목적 |
|---|---|
| `DreamineEnvelopeProtocolAdapter` | Dreamine 내부 표준 형식입니다. 전체 `MessageEnvelope` JSON을 Encode/Decode합니다. |
| `PlainTextProtocolAdapter` | 외부 일반 문자열 통신용입니다. 문자열 바이트를 `MessageEnvelope`로 감쌉니다. |
| `RawJsonProtocolAdapter` | Dreamine 고정 스키마가 없는 외부 JSON 통신용입니다. Raw JSON을 `MessageEnvelope`로 감쌉니다. |

### Frame Codecs

Frame Codec은 바이트 스트림에서 메시지 경계를 판단하는 규칙입니다.

| 타입 | 경계 판단 방식 | 권장 용도 |
|---|---|---|
| `LengthPrefixedMessageFrameCodec` | 4바이트 Big-Endian 길이 Prefix를 사용합니다. | Dreamine 내부 통신, 안정적인 Binary-safe 프로토콜 |
| `DelimiterMessageFrameCodec` | `\r\n` 같은 Delimiter가 나올 때까지 읽습니다. | Line Ending을 보내는 외부 장비/툴 문자열 통신 |
| `RawAvailableMessageFrameCodec` | 현재 수신된 바이트를 즉시 하나의 메시지로 처리합니다. | Hercules, 임시 테스트, `CRLF`나 길이 Prefix 없이 단순 문자열을 보내는 외부 툴 |

## Frame Codec 선택 기준

| 상황 | 권장 Codec | 이유 |
|---|---|---|
| Dreamine ↔ Dreamine 통신 | `LengthPrefixedMessageFrameCodec` | 메시지 경계를 정확히 보존하고 임의 Payload를 안전하게 처리합니다. |
| `CRLF` 또는 `LF`로 끝나는 문자열 프로토콜 | `DelimiterMessageFrameCodec` | Line 기반 프로토콜과 맞습니다. |
| Hercules에서 `test1`만 보내고 `CRLF`가 없음 | `RawAvailableMessageFrameCodec` | Delimiter를 기다리지 않고 즉시 Receive 처리할 수 있습니다. |
| `CRLF`로 끝나는 Raw JSON | `DelimiterMessageFrameCodec` + `RawJsonProtocolAdapter` | JSON Payload를 그대로 유지하면서 메시지 경계도 보존합니다. |

## RawAvailableMessageFrameCodec

`RawAvailableMessageFrameCodec`은 단순 Raw Byte 수신 상황을 위한 호환용 Codec입니다. 내부 Stream Read에서 반환된 바이트를 즉시 하나의 Frame으로 반환합니다.

다음처럼 외부 툴이 단순 문자열만 보내는 경우에 유용합니다.

```text
test1
```

이때 아래 요소가 없어도 Receive 처리가 가능합니다.

- Length Prefix
- `CRLF`
- `LF`
- 고정 JSON Envelope

사용 예시:

```csharp
using System.Text;
using Dreamine.Communication.Core.Framing;
using Dreamine.Communication.Core.Protocols;
using Dreamine.Communication.Sockets.Options;
using Dreamine.Communication.Sockets.Servers;

var server = new TcpServerTransport(
    new TcpServerTransportOptions
    {
        Host = "127.0.0.1",
        Port = 15002
    },
    new PlainTextProtocolAdapter(
        Encoding.UTF8,
        "tcp.raw.available",
        "Tcp.RawAvailable"),
    new RawAvailableMessageFrameCodec());

server.MessageReceived += (_, message) =>
{
    var text = Encoding.UTF8.GetString(message.Payload);
    Console.WriteLine($"RX: {text}");
};

await server.ConnectAsync();
```

## TCP 주의 사항

TCP는 Message Protocol이 아니라 Stream Protocol입니다. `RawAvailableMessageFrameCodec`은 의도적으로 허용적인 방식이지만, 애플리케이션 레벨의 메시지 경계를 보장하지 않습니다.

예를 들어 빠르게 두 번 보낸 데이터가 다음처럼 합쳐질 수 있습니다.

```text
test1test2
```

또는 한 번 보낸 데이터가 다음처럼 쪼개질 수도 있습니다.

```text
te
st1
```

따라서 `RawAvailableMessageFrameCodec`은 호환성, 디버깅, 임시 테스트, Framing을 제공하지 않는 외부 툴 대응용으로만 사용하는 것이 좋습니다. 운영 프로토콜에는 `LengthPrefixedMessageFrameCodec` 또는 `DelimiterMessageFrameCodec`을 우선 권장합니다.

## 권장 Protocol 조합

| Mode | Protocol Adapter | Frame Codec |
|---|---|---|
| Dreamine Envelope | `DreamineEnvelopeProtocolAdapter` | `LengthPrefixedMessageFrameCodec` |
| Plain Text Line Protocol | `PlainTextProtocolAdapter` | `DelimiterMessageFrameCodec` |
| Plain Text Raw Receive | `PlainTextProtocolAdapter` | `RawAvailableMessageFrameCodec` |
| Raw JSON Line Protocol | `RawJsonProtocolAdapter` | `DelimiterMessageFrameCodec` |

## 설계 원칙

- 구체 Transport 구현체를 상위 레이어와 분리합니다.
- `Dreamine.Communication.Abstractions`의 계약에 의존합니다.
- 패키지 책임을 작고 명확하게 유지합니다.
- 단방향 의존성 흐름을 유지합니다.
- Payload Protocol 변환과 Stream Frame 검출을 분리합니다.
- 향후 Adapter와 Codec을 추가해도 애플리케이션 로직을 변경하지 않도록 합니다.

## 의존성

- `Dreamine.Communication.Abstractions`

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

## Text Encoding 정책

Text Encoding은 외부 Raw Byte를 `MessageEnvelope`로 변환하거나, `MessageEnvelope`의 Payload를 다시 외부 Byte로 변환하는 Protocol Adapter 경계에서 처리합니다.

Encoding 옵션은 외부 바이트 기반 통신에 적용됩니다.

- TCP
- UDP
- Serial

`PlainTextProtocolAdapter`와 `RawJsonProtocolAdapter`는 UTF-8 또는 CP949 같은 외부 Text Encoding을 설정할 수 있습니다. 현대 시스템 간 통신에는 UTF-8을 권장합니다. CP949는 레거시 Windows 툴, 한글 장비, UTF-8을 보내지 않는 테스트 툴과 연동할 때 사용할 수 있습니다.

`InMemoryMessageBus`는 같은 프로세스 안에서 `MessageEnvelope` 객체를 직접 전달하므로 Text Encoding이 필요하지 않습니다. RabbitMQ도 기본 Dreamine 흐름에서는 `MessageEnvelope`를 UTF-8 JSON으로 직렬화하므로 별도 Encoding 옵션을 노출하지 않습니다.

## 라이선스

이 프로젝트는 MIT 라이선스를 따릅니다.
