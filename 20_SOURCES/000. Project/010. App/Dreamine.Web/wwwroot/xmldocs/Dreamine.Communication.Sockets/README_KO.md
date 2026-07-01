# Dreamine.Communication.Sockets

`Dreamine.Communication.Sockets`는 Dreamine Communication 계열 패키지의 일부입니다.

이 패키지는 TCP 및 UDP 소켓 기반 전송 구현체를 제공하며, 소켓 연결 관련 책임을 애플리케이션 계층 및 다른 전송 패키지와 분리합니다.

[➡️ English Version](./README.md)

## 설명

Dreamine Communication을 위한 TCP/UDP 소켓 전송 패키지입니다. `TcpClientTransport`, `TcpServerTransport`, `UdpTransport` 구현체를 제공하며, 선택된 프로토콜 어댑터와 (TCP의 경우) 프레임 코덱을 통해 `MessageEnvelope`를 송수신합니다.

## 패키지 역할

```text
Dreamine.Communication.Abstractions
    ↑
Dreamine.Communication.Core
    ↑
Dreamine.Communication.Sockets
```

`Sockets`는 구체적인 TCP 통신 경계만 담당합니다.

- TCP Client 연결 열기 및 닫기.
- TCP Server Listener 시작 및 중지.
- TCP Client Accept 처리.
- `NetworkStream` 기반 수신 루프 실행.
- 연결된 Client 대상 Server 메시지 Broadcast 송신.
- `IMessageProtocolAdapter`를 통한 송신 `MessageEnvelope` 인코딩.
- `IMessageFrameCodec`을 통한 스트림 데이터 프레임 처리.

이 패키지는 UI 상태, 애플리케이션 명령 규칙, 비즈니스 라우팅 정책, Serial Port 로직, RabbitMQ 연결 로직, WPF 전용 로직을 소유하면 안 됩니다.

## 주요 기능

- TCP Client Transport
- TCP Server Transport
- UDP Datagram Peer-to-Peer Transport
- `MessageEnvelope` 기반 송수신 흐름
- 프로토콜 어댑터 설정 가능
- 프레임 코덱 설정 가능 (TCP 전용. UDP는 Datagram 기반이므로 사용하지 않음)
- `Core`의 공통 JSON 직렬화 및 프로토콜 어댑터 사용
- `Core`의 공통 스트림 프레임 처리 사용
- 연결 상태 관리를 포함한 비동기 수신 루프
- Server에서 연결된 Client 전체 대상 Broadcast 송신

## 주요 구성 요소

| 타입 | 역할 |
|---|---|
| `TcpClientTransport` | TCP Client 연결을 위한 `IMessageTransport` 구현체입니다. |
| `TcpServerTransport` | TCP Server Listener 및 연결된 Client 처리를 위한 `IMessageTransport` 구현체입니다. |
| `UdpTransport` | UDP Datagram Peer-to-Peer 통신을 위한 `IMessageTransport` 구현체입니다. |
| `TcpClientTransportOptions` | TCP Client 연결 설정 모델입니다. |
| `TcpServerTransportOptions` | TCP Server Listener 설정 모델입니다. |
| `UdpTransportOptions` | UDP Local/Remote 엔드포인트 설정 모델입니다. |
| `SocketCommunicationException` | 소켓 통신 전용 예외 타입입니다. |

## TcpClientTransport

`TcpClientTransport`는 공통 `IMessageTransport` 계약을 구현합니다.

기본 생성자 동작:

```text
Protocol adapter : DreamineEnvelopeProtocolAdapter
Frame codec      : LengthPrefixedMessageFrameCodec
```

이 기본값은 Dreamine 간 TCP 통신에 적합합니다. 외부 툴, TCP 터미널, PLC Gateway 소프트웨어, 검사 장비, Raw 문자열 테스트에는 프로토콜 어댑터와 프레임 코덱을 명시적으로 전달해야 합니다.

Raw Text Client 예시:

```csharp
using System.Text;
using Dreamine.Communication.Core.Framing;
using Dreamine.Communication.Core.Protocols;
using Dreamine.Communication.Sockets.Clients;
using Dreamine.Communication.Sockets.Options;

var client = new TcpClientTransport(
    new TcpClientTransportOptions
    {
        Host = "127.0.0.1",
        Port = 15002
    },
    new PlainTextProtocolAdapter(
        Encoding.UTF8,
        "tcp.raw.available",
        "Tcp.RawAvailable"),
    new RawAvailableMessageFrameCodec());

client.MessageReceived += (_, message) =>
{
    var text = Encoding.UTF8.GetString(message.Payload);
    Console.WriteLine($"RX: {text}");
};

await client.ConnectAsync();
```

## TcpServerTransport

`TcpServerTransport`는 공통 `IMessageTransport` 계약을 구현합니다.

기본 생성자 동작:

```text
Protocol adapter : DreamineEnvelopeProtocolAdapter
Frame codec      : LengthPrefixedMessageFrameCodec
```

Server는 `TcpListener`를 시작하고, 여러 TCP Client를 Accept하며, Client별 수신 루프를 생성합니다. 송신 메시지는 현재 연결된 모든 Client에게 Broadcast됩니다.

Raw Text Server 예시:

```csharp
using System.Text;
using Dreamine.Communication.Core.Framing;
using Dreamine.Communication.Core.Protocols;
using Dreamine.Communication.Sockets.Options;
using Dreamine.Communication.Sockets.Servers;

var server = new TcpServerTransport(
    new TcpServerTransportOptions
    {
        Host = "0.0.0.0",
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

## TcpClientTransportOptions

| 옵션 | 기본값 | 의미 |
|---|---:|---|
| `Host` | `127.0.0.1` | 연결할 TCP Server Host입니다. |
| `Port` | `0` | 연결할 TCP Server Port입니다. 연결 전 1~65535 범위여야 합니다. |
| `ReceiveBufferSize` | `8192` | TCP 수신 버퍼 크기입니다. |
| `SendBufferSize` | `8192` | TCP 송신 버퍼 크기입니다. |
| `ConnectTimeoutMs` | `5000` | TCP 연결 타임아웃(ms)입니다. |

## TcpServerTransportOptions

| 옵션 | 기본값 | 의미 |
|---|---:|---|
| `Host` | `0.0.0.0` | 바인딩할 IP 주소입니다. `0.0.0.0`은 모든 네트워크 인터페이스를 의미합니다. |
| `Port` | `5000` | TCP 수신 대기 Port입니다. 1~65535 범위여야 합니다. |
| `Backlog` | `100` | TCP Listener backlog 값입니다. |
| `ReceiveBufferSize` | `8192` | Accept된 Client에 적용할 수신 버퍼 크기입니다. |
| `SendBufferSize` | `8192` | Accept된 Client에 적용할 송신 버퍼 크기입니다. |

Server Host 파싱은 현재 다음 값을 지원합니다.

- `0.0.0.0`
- `127.0.0.1`
- `localhost`
- 직접 입력한 IP 주소 문자열

외부 공개 서비스는 `0.0.0.0` 또는 특정 로컬 인터페이스 IP에 바인딩합니다. 로컬 테스트는 `127.0.0.1`을 사용합니다.

## Protocol Adapter와 Frame Codec

TCP는 스트림 기반입니다. 애플리케이션 메시지 경계를 보존하지 않습니다. 그래서 메시지 의미와 메시지 경계 처리를 의도적으로 분리합니다.

| 책임 | 타입 |
|---|---|
| Byte와 `MessageEnvelope` 상호 변환 | `IMessageProtocolAdapter` |
| 스트림에서 메시지 경계 판단 | `IMessageFrameCodec` |
| TCP Socket 열기, 닫기, 송신, 수신 | `TcpClientTransport` / `TcpServerTransport` |

## 권장 조합

| 시나리오 | Protocol Adapter | Frame Codec |
|---|---|---|
| Dreamine 간 TCP 통신 | `DreamineEnvelopeProtocolAdapter` | `LengthPrefixedMessageFrameCodec` |
| 각 메시지가 `CRLF` 또는 `LF`로 끝나는 Text Protocol | `PlainTextProtocolAdapter` | `DelimiterMessageFrameCodec` |
| 단순 TCP 테스트 툴이 구분자 없이 Raw Text 전송 | `PlainTextProtocolAdapter` | `RawAvailableMessageFrameCodec` |
| 줄 끝이 있는 JSON Text Protocol | `RawJsonProtocolAdapter` | `DelimiterMessageFrameCodec` |
| Custom Binary TCP Protocol | Custom Protocol Adapter | Custom Frame Codec |

## RawAvailableMessageFrameCodec 지원

`RawAvailableMessageFrameCodec`은 이 패키지가 아니라 `Dreamine.Communication.Core`에 정의됩니다. 그래도 `TcpClientTransport`와 `TcpServerTransport`는 모든 `IMessageFrameCodec`을 받을 수 있으므로 그대로 사용할 수 있습니다.

이 모드는 TCP 툴 또는 외부 장비가 다음처럼 단순 값을 보낼 때 유용합니다.

```text
test1
```

위 값에 다음 항목이 없어도 수신 처리가 가능합니다.

- Dreamine Envelope
- Length Prefix
- `CRLF`
- `LF`

설정 예시:

```csharp
var transport = new TcpServerTransport(
    options,
    new PlainTextProtocolAdapter(
        Encoding.UTF8,
        "tcp.raw.available",
        "Tcp.RawAvailable"),
    new RawAvailableMessageFrameCodec());
```

이 조합을 사용하면 Hercules 같은 수동 TCP 테스트 툴에서 단순 문자열을 보내도 `MessageReceived`가 발생합니다.

## 중요한 TCP Stream 주의사항

TCP는 Byte Stream입니다. `RawAvailableMessageFrameCodec`은 한 번의 Stream Read에서 반환된 Byte를 하나의 메시지로 처리합니다.

수동 테스트에는 편하지만, 엄격한 메시지 경계 규칙은 아닙니다. 타이밍과 버퍼 상태에 따라 여러 번의 송신이 합쳐지거나, 한 번의 송신이 쪼개질 수 있습니다.

가능한 수신 결과 예시:

```text
test1test2
```

또는:

```text
te
st1
```

운영용 TCP Protocol에는 아래 방식 중 하나를 권장합니다.

- Dreamine 내부 통신에는 `LengthPrefixedMessageFrameCodec` 사용.
- `CRLF`, `LF` 또는 명확한 Delimiter 기반 `DelimiterMessageFrameCodec` 사용.
- 고정 길이 Binary Frame 사용.
- Protocol 전용 Custom `IMessageFrameCodec` 사용.

`RawAvailableMessageFrameCodec`은 호환성, 디버깅, 수동 테스트, 명확한 프레이밍 규칙이 없는 툴 대응용으로 사용하는 것이 맞습니다.

## 연결 상태

두 Socket Transport는 공통 `ConnectionState` enum을 통해 상태를 제공합니다.

| 상태 | TcpClientTransport | TcpServerTransport |
|---|---|---|
| `Disconnected` | Client 연결이 끊긴 상태입니다. | Listener가 중지된 상태입니다. |
| `Connecting` | Client 연결 중입니다. | Server Listener 시작 중입니다. |
| `Connected` | Client가 연결되었고 수신 루프가 동작 중입니다. | Listener와 Accept Loop가 동작 중입니다. |
| `Disconnecting` | Client 연결 종료 중입니다. | Server 중지 및 Client 정리 중입니다. |
| `Faulted` | 연결 또는 수신 루프 오류가 발생했습니다. | Listener, Accept Loop 또는 Socket 작업 오류가 발생했습니다. |

## 송신 동작

`TcpClientTransport.SendAsync`는 연결된 Server로 인코딩된 Frame 하나를 전송합니다.

`TcpServerTransport.SendAsync`는 현재 연결된 모든 Client에게 인코딩된 Frame 하나를 Broadcast합니다. 송신 중 Client 연결이 끊겼거나 오류가 발생하면 해당 Client는 Server 연결 목록에서 제거됩니다.

## 오류 처리

Transport는 Socket을 열기 전에 Options를 검증합니다. 잘못된 설정은 조기에 실패합니다.

예시:

- 비어 있는 `Host`
- `Port <= 0`
- `Port > 65535`
- 0 이하의 버퍼 크기
- 0 이하의 Client 연결 타임아웃
- 0 이하의 Server backlog

런타임 Socket 오류는 Transport 상태를 `Faulted`로 변경할 수 있습니다. 애플리케이션 레벨 복구는 Transport를 Dispose 또는 Disconnect한 뒤, Socket 조건을 수정하고 다시 연결하거나 Server를 재시작하는 방식으로 처리합니다.

## 사용 예시: Dreamine 간 TCP 통신

```csharp
using Dreamine.Communication.Abstractions.Models;
using Dreamine.Communication.Sockets.Clients;
using Dreamine.Communication.Sockets.Options;
using Dreamine.Communication.Sockets.Servers;

var server = new TcpServerTransport(
    new TcpServerTransportOptions
    {
        Host = "127.0.0.1",
        Port = 15001
    });

server.MessageReceived += (_, message) =>
{
    Console.WriteLine($"Server RX: {message.Name}");
};

await server.ConnectAsync();

var client = new TcpClientTransport(
    new TcpClientTransportOptions
    {
        Host = "127.0.0.1",
        Port = 15001
    });

await client.ConnectAsync();

await client.SendAsync(new MessageEnvelope
{
    Name = "Sample.Ping",
    Route = "sample.tcp.ping",
    Payload = []
});
```

## 사용 예시: CRLF Text TCP

```csharp
using System.Text;
using Dreamine.Communication.Core.Framing;
using Dreamine.Communication.Core.Protocols;
using Dreamine.Communication.Sockets.Clients;
using Dreamine.Communication.Sockets.Options;

var client = new TcpClientTransport(
    new TcpClientTransportOptions
    {
        Host = "127.0.0.1",
        Port = 15002
    },
    new PlainTextProtocolAdapter(
        Encoding.UTF8,
        "tcp.plaintext",
        "Tcp.PlainText"),
    new DelimiterMessageFrameCodec(
        "\r\n",
        Encoding.UTF8,
        1024 * 1024));

await client.ConnectAsync();
```

## 설계 원칙

- 구체 Socket 구현체를 상위 레이어와 분리합니다.
- `Dreamine.Communication.Abstractions` 계약에 의존합니다.
- `Core`의 프로토콜 어댑터와 프레임 코덱을 재사용합니다.
- Socket 연결 제어와 Payload 해석을 분리합니다.
- 패키지 책임을 작고 명확하게 유지합니다.
- 단방향 의존성 흐름을 유지합니다.
- 향후 TCP Protocol 및 Custom Codec을 추가해도 애플리케이션 로직을 변경하지 않도록 합니다.

## 의존성

- `Dreamine.Communication.Abstractions`
- `Dreamine.Communication.Core`

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

## UDP Transport

`Dreamine.Communication.Sockets`는 UDP Peer 방식 Transport도 제공합니다.
UDP는 Datagram 기반이므로 `LengthPrefixedMessageFrameCodec`, `DelimiterMessageFrameCodec`, `RawAvailableMessageFrameCodec` 같은 Stream Frame Codec을 사용하지 않습니다.

권장 UDP 구조는 다음과 같습니다.

```text
UDP Datagram -> IMessageProtocolAdapter -> MessageEnvelope
MessageEnvelope -> IMessageProtocolAdapter -> UDP Datagram
```

샘플은 로컬 Loopback 테스트를 위해 두 개의 UDP Peer를 사용합니다.

```text
Peer A: 127.0.0.1:16001 -> 127.0.0.1:16002
Peer B: 127.0.0.1:16002 -> 127.0.0.1:16001
```

Dreamine 내부 Peer 간 테스트는 `Start Loopback`을 사용합니다. Hercules 같은 외부 UDP 툴과 테스트할 때는 로컬 포트 충돌을 피하기 위해 Peer 하나만 연결합니다.

Hercules 테스트 예시는 다음과 같습니다.

```text
Dreamine Peer A Local : 16001
Dreamine Peer A Remote: 16002
Hercules Local port   : 16002
Hercules Target port  : 16001
```

이 경우 Dreamine 샘플에서는 `Connect A` 후 `Send Peer A`를 누릅니다.

### UDP 사용 예시

```csharp
using System.Text;
using Dreamine.Communication.Abstractions.Models;
using Dreamine.Communication.Core.Protocols;
using Dreamine.Communication.Sockets.Options;
using Dreamine.Communication.Sockets.Udp;

var peerA = new UdpTransport(
    new UdpTransportOptions
    {
        LocalHost = "127.0.0.1",
        LocalPort = 16001,
        RemoteHost = "127.0.0.1",
        RemotePort = 16002,
        ReuseAddress = true
    },
    new PlainTextProtocolAdapter(
        Encoding.UTF8,
        "udp.plaintext",
        "Udp.PlainText"));

peerA.MessageReceived += (_, message) =>
{
    var text = Encoding.UTF8.GetString(message.Payload);
    Console.WriteLine($"Peer A RX: {text}");
};

await peerA.ConnectAsync();

await peerA.SendAsync(new MessageEnvelope
{
    Name = "Udp.PeerA.Send",
    Route = "udp.plaintext",
    Payload = Encoding.UTF8.GetBytes("hello"),
    Headers = new Dictionary<string, string>
    {
        ["ContentType"] = "text/plain",
        ["Protocol"] = "PlainText"
    }
});
```

Loopback 로컬 테스트는 LocalPort와 RemotePort를 서로 맞바꾼 두 번째 `UdpTransport`(Peer B)를 함께 생성하여 사용합니다.

## 외부 Text Encoding

TCP와 UDP는 UTF-8을 사용하지 않는 외부 툴 또는 장비와 통신할 수 있습니다. 따라서 샘플에서는 외부 Text 계열 모드에 대해 Encoding 선택 기능을 제공합니다.

| Encoding | 권장 용도 |
|---|---|
| `UTF-8` | 현대 시스템 및 Dreamine ↔ Dreamine 통신 기본값 |
| `CP949` | 레거시 한글 Windows 툴, Hercules 한글 테스트, 오래된 장비 프로토콜 |

Encoding 선택은 Protocol Adapter 경계에서 적용됩니다. `TcpClientTransport`, `TcpServerTransport`, `UdpTransport`의 책임이 아닙니다.

## 라이선스

이 프로젝트는 MIT 라이선스를 따릅니다.
