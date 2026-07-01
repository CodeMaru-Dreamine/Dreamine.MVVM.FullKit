# Dreamine.Communication.Serial

`Dreamine.Communication.Serial`는 Dreamine Communication 계열 패키지의 일부입니다.

이 패키지는 RS232 및 `SerialPort` 기반 전송 구현체를 제공하며, 시리얼 장비 관련 책임을 상위 애플리케이션 계층과 분리합니다.

[➡️ English Version](./README.md)

## 설명

Dreamine Communication을 위한 시리얼 전송 패키지입니다. .NET `SerialPort`를 열고 관리하며, 포트 스트림을 Dreamine 공통 전송 추상화에 연결하고, 선택된 프로토콜 어댑터와 프레임 코덱을 통해 `MessageEnvelope`를 송수신합니다.

## 패키지 역할

```text
Dreamine.Communication.Abstractions
    ↑
Dreamine.Communication.Core
    ↑
Dreamine.Communication.Serial
```

`Serial` 패키지는 구체적인 시리얼 통신 경계만 담당합니다.

- 시리얼 포트 열기/닫기
- RS232 통신 옵션 적용
- `SerialPort.BaseStream` 기반 수신 루프 실행
- `IMessageProtocolAdapter`를 통한 송신 `MessageEnvelope` 인코딩
- `IMessageFrameCodec`을 통한 스트림 데이터 분리 및 기록

애플리케이션 명령 규칙, UI 상태, 라우팅 정책, TCP 연결 로직, RabbitMQ 연결 로직, WPF 전용 로직은 이 패키지의 책임이 아닙니다.

## 주요 기능

- `SerialPort` 기반 Transport
- RS232 통신 경계
- `MessageEnvelope` 기반 송수신 흐름
- 설정 가능한 프로토콜 어댑터
- 설정 가능한 프레임 코덱
- `Core`의 공통 JSON 직렬화 및 프로토콜 어댑터 사용
- `Core`의 공통 스트림 프레임 처리 사용
- 연결 상태 관리를 포함한 비동기 수신 루프

## 주요 구성 요소

| 타입 | 역할 |
|---|---|
| `SerialPortTransport` | RS232 / 시리얼 통신용 `IMessageTransport` 구현체입니다. |
| `SerialPortTransportOptions` | 시리얼 포트 설정 모델입니다. |
| `SerialPortStreamAdapter` | `SerialPort.BaseStream` 접근을 감싸는 작은 어댑터입니다. |
| `SerialCommunicationException` | 시리얼 통신 계층 전용 예외 타입입니다. |

## SerialPortTransport

`SerialPortTransport`는 공통 `IMessageTransport` 계약을 구현합니다.

기본 생성자 동작은 다음과 같습니다.

```text
Protocol adapter : DreamineEnvelopeProtocolAdapter
Frame codec      : LengthPrefixedMessageFrameCodec
```

이 기본값은 Dreamine 간 시리얼 통신에는 맞습니다. 외부 장비, PLC, 바코드 리더, 검사 장비, 터미널 유틸리티와 통신할 때는 프로토콜 어댑터와 프레임 코덱을 명시적으로 전달하는 편이 맞습니다.

예시:

```csharp
using System.IO.Ports;
using System.Text;
using Dreamine.Communication.Core.Framing;
using Dreamine.Communication.Core.Protocols;
using Dreamine.Communication.Serial.Options;
using Dreamine.Communication.Serial.Ports;

var transport = new SerialPortTransport(
    new SerialPortTransportOptions
    {
        PortName = "COM3",
        BaudRate = 9600,
        DataBits = 8,
        Parity = Parity.None,
        StopBits = StopBits.One,
        Handshake = Handshake.None
    },
    new PlainTextProtocolAdapter(
        Encoding.UTF8,
        "serial.raw.available",
        "Serial.RawAvailable"),
    new RawAvailableMessageFrameCodec());

transport.MessageReceived += (_, message) =>
{
    var text = Encoding.UTF8.GetString(message.Payload);
    Console.WriteLine($"RX: {text}");
};

await transport.ConnectAsync();
```

## SerialPortTransportOptions

| 옵션 | 기본값 | 의미 |
|---|---:|---|
| `PortName` | `COM1` | 시리얼 포트 이름입니다. |
| `BaudRate` | `9600` | 통신 속도입니다. |
| `DataBits` | `8` | 데이터 비트 수입니다. 유효 범위는 5~8입니다. |
| `Parity` | `Parity.None` | 패리티 설정입니다. |
| `StopBits` | `StopBits.One` | Stop bit 설정입니다. |
| `Handshake` | `Handshake.None` | 하드웨어/소프트웨어 흐름 제어 설정입니다. |
| `ReadTimeoutMs` | `3000` | 읽기 타임아웃(ms)입니다. |
| `WriteTimeoutMs` | `3000` | 쓰기 타임아웃(ms)입니다. |
| `ReadBufferSize` | `4096` | 수신 버퍼 크기입니다. |
| `WriteBufferSize` | `4096` | 송신 버퍼 크기입니다. |

## 프로토콜 어댑터와 프레임 코덱

시리얼 통신도 스트림 기반입니다. Transport 자체는 애플리케이션 메시지가 어디서 시작하고 끝나는지 알 수 없습니다. 그래서 메시지 의미와 메시지 경계 처리를 의도적으로 분리합니다.

| 책임 | 타입 |
|---|---|
| 바이트와 `MessageEnvelope` 간 변환 | `IMessageProtocolAdapter` |
| 스트림에서 메시지 경계 감지 | `IMessageFrameCodec` |
| COM 포트 열기/닫기/송신/수신 | `SerialPortTransport` |

## 권장 조합

| 상황 | 프로토콜 어댑터 | 프레임 코덱 |
|---|---|---|
| Dreamine 간 시리얼 통신 | `DreamineEnvelopeProtocolAdapter` | `LengthPrefixedMessageFrameCodec` |
| 메시지 끝에 `CRLF` 또는 `LF`가 붙는 텍스트 장비 | `PlainTextProtocolAdapter` | `DelimiterMessageFrameCodec` |
| 구분자 없이 단순 문자열을 보내는 터미널 툴 또는 장비 | `PlainTextProtocolAdapter` | `RawAvailableMessageFrameCodec` |
| 줄 끝 문자를 포함한 JSON 텍스트를 보내는 장비 | `RawJsonProtocolAdapter` | `DelimiterMessageFrameCodec` |
| 고정 바이너리 프로토콜 장비 | Custom protocol adapter | Custom frame codec |

## RawAvailableMessageFrameCodec 지원

`RawAvailableMessageFrameCodec`은 이 패키지가 아니라 `Dreamine.Communication.Core`에 정의되어 있습니다. 하지만 `SerialPortTransport`는 `IMessageFrameCodec`을 받기 때문에 그대로 사용할 수 있습니다.

이 모드는 장비나 터미널 툴이 다음과 같은 단순 값을 보낼 때 유용합니다.

```text
test1
```

그리고 아래 요소가 없을 때 사용할 수 있습니다.

- Dreamine envelope
- length prefix
- `CRLF`
- `LF`

시리얼 Raw 문자열 설정 예시:

```csharp
var transport = new SerialPortTransport(
    options,
    new PlainTextProtocolAdapter(
        Encoding.UTF8,
        "serial.raw.available",
        "Serial.RawAvailable"),
    new RawAvailableMessageFrameCodec());
```

## 중요한 시리얼 스트림 주의사항

RS232 시리얼 통신은 바이트 스트림입니다. `RawAvailableMessageFrameCodec`은 한 번의 스트림 읽기에서 반환된 바이트를 하나의 메시지로 처리합니다.

수동 테스트에는 편하지만, 엄격한 메시지 경계 규칙은 아닙니다. 장비 타이밍, 버퍼 동작, Baud rate에 따라 여러 번 보낸 데이터가 합쳐지거나, 한 번 보낸 데이터가 나누어질 수 있습니다.

실제 양산 장비 프로토콜에서는 아래 방식 중 하나를 권장합니다.

- `CR`, `LF`, `CRLF` 같은 구분자 사용
- 길이 prefix 기반 프레임 사용
- 고정 길이 바이너리 프레임 사용
- 장비 전용 Custom `IMessageFrameCodec` 사용

`RawAvailableMessageFrameCodec`은 호환성, 단순 툴, 디버깅, 명시적 프레이밍 규칙이 없는 장비 대응 용도로 사용하는 것이 맞습니다.

## 연결 상태

`SerialPortTransport`는 공통 `ConnectionState` enum으로 상태를 보고합니다.

| 상태 | 의미 |
|---|---|
| `Disconnected` | COM 포트가 닫혀 있습니다. |
| `Connecting` | Transport가 COM 포트를 여는 중입니다. |
| `Connected` | COM 포트가 열려 있고 수신 루프가 동작 중입니다. |
| `Disconnecting` | Transport가 COM 포트를 닫는 중입니다. |
| `Faulted` | 시리얼 연결 또는 수신 루프에서 오류가 발생했습니다. |

## 오류 처리

Transport는 포트를 열기 전에 옵션을 검증합니다. 잘못된 설정은 초기에 실패합니다.

예시:

- 비어 있는 `PortName`
- `BaudRate <= 0`
- `DataBits`가 5~8 범위를 벗어남
- 0 이하의 timeout 값
- 0 이하의 buffer size

런타임 포트 오류가 발생하면 Transport 상태가 `Faulted`로 변경될 수 있습니다. 애플리케이션 수준 복구는 Transport를 닫고, 포트 상태를 수정한 뒤 다시 연결하는 방식이 맞습니다.

## 사용 예시: 라인 기반 텍스트 장비

```csharp
using System.IO.Ports;
using System.Text;
using Dreamine.Communication.Core.Framing;
using Dreamine.Communication.Core.Protocols;
using Dreamine.Communication.Serial.Options;
using Dreamine.Communication.Serial.Ports;

var options = new SerialPortTransportOptions
{
    PortName = "COM3",
    BaudRate = 9600,
    DataBits = 8,
    Parity = Parity.None,
    StopBits = StopBits.One,
    Handshake = Handshake.None
};

var transport = new SerialPortTransport(
    options,
    new PlainTextProtocolAdapter(
        Encoding.UTF8,
        "serial.plaintext",
        "Serial.PlainText"),
    new DelimiterMessageFrameCodec(
        "\r\n",
        Encoding.UTF8,
        1024 * 1024));

transport.MessageReceived += (_, message) =>
{
    var text = Encoding.UTF8.GetString(message.Payload);
    Console.WriteLine($"RX: {text}");
};

await transport.ConnectAsync();
```

## 설계 원칙

- 구체 시리얼 구현체를 상위 레이어와 분리합니다.
- `Dreamine.Communication.Abstractions`의 계약에 의존합니다.
- `Core`의 프로토콜 어댑터와 프레임 코덱을 재사용합니다.
- 시리얼 포트 제어와 Payload 해석을 분리합니다.
- 패키지 책임을 작고 명확하게 유지합니다.
- 단방향 의존성 흐름을 유지합니다.
- 향후 시리얼 장비 및 Custom codec을 추가해도 애플리케이션 로직을 변경하지 않도록 합니다.

## 의존성

- `Dreamine.Communication.Abstractions`
- `Dreamine.Communication.Core`
- `System.IO.Ports`

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

이 프로젝트는 MIT 라이선스를 따릅니다.
