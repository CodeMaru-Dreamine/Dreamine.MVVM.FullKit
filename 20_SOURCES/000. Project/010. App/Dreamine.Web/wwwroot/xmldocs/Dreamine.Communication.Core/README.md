# Dreamine.Communication.Core

`Dreamine.Communication.Core` is part of the Dreamine Communication package family.

This package provides the common runtime layer used by concrete communication adapters. It does not implement TCP, Serial, RabbitMQ, or WPF directly. Concrete transports should depend on this package and combine a protocol adapter with a frame codec.

[➡️ 한국어 문서 보기](./README_KO.md)

## Description

Core message bus, routing, serialization, protocol adaptation, message framing, and transport-to-message-bus adapter utilities for Dreamine Communication.

## Package Role

```text
Dreamine.Communication.Abstractions
    ↑
Dreamine.Communication.Core
    ↑
Sockets / Serial / RabbitMQ / WPF
```

`Core` owns the transport-independent rules:

- How a `MessageEnvelope` is serialized.
- How an external payload is converted to a `MessageEnvelope`.
- How a byte stream is split into message frames.
- How messages are routed or published inside the application.

It must not own socket connection logic, serial port logic, RabbitMQ connection logic, or WPF UI logic.

## Features

- In-memory message bus
- Message route dispatcher
- `MessageEnvelope` JSON serialization
- Protocol adapters for internal and external payload formats
- Stream frame codecs for TCP-like byte streams
- Transport-to-message-bus adapter

## Main Components

### Message Bus

| Type | Role |
|---|---|
| `InMemoryMessageBus` | In-process publish/subscribe message bus. |
| `TransportMessageBusAdapter` | Wraps an `IMessageTransport` and exposes it as an `IMessageBus`. |

### Routing

| Type | Role |
|---|---|
| `MessageRouter` | Registers handlers by route and dispatches `MessageEnvelope` instances. |
| `MessageHandlerRegistration` | Represents handler registration metadata. |

### Serialization

| Type | Role |
|---|---|
| `JsonMessageSerializer` | Serializes and deserializes `MessageEnvelope` using `System.Text.Json`. |

### Protocol Adapters

Protocol adapters convert between raw payload bytes and Dreamine `MessageEnvelope` instances.

| Type | Use case |
|---|---|
| `DreamineEnvelopeProtocolAdapter` | Dreamine internal standard format. Encodes/decodes full `MessageEnvelope` JSON. |
| `PlainTextProtocolAdapter` | External plain text communication. Wraps text bytes as a `MessageEnvelope`. |
| `RawJsonProtocolAdapter` | External JSON communication without a fixed Dreamine schema. Wraps raw JSON as a `MessageEnvelope`. |

### Frame Codecs

Frame codecs define how message boundaries are detected on a byte stream.

| Type | Boundary rule | Recommended use |
|---|---|---|
| `LengthPrefixedMessageFrameCodec` | 4-byte big-endian length prefix. | Dreamine internal communication and stable binary-safe protocols. |
| `DelimiterMessageFrameCodec` | Reads until a delimiter such as `\r\n`. | Text-based external devices and tools that send line endings. |
| `RawAvailableMessageFrameCodec` | Treats currently received bytes as one message. | Hercules, temporary tests, or simple external tools that send plain strings without `CRLF` or a length prefix. |

## Frame Codec Selection Guide

| Scenario | Recommended codec | Reason |
|---|---|---|
| Dreamine-to-Dreamine communication | `LengthPrefixedMessageFrameCodec` | Preserves exact message boundaries and supports arbitrary payloads. |
| Text protocol ending with `CRLF` or `LF` | `DelimiterMessageFrameCodec` | Matches line-based protocols. |
| Hercules sends `test1` without `CRLF` | `RawAvailableMessageFrameCodec` | Allows immediate receive processing without waiting for a delimiter. |
| Raw JSON ending with `CRLF` | `DelimiterMessageFrameCodec` + `RawJsonProtocolAdapter` | Keeps JSON payload readable while preserving message boundaries. |

## RawAvailableMessageFrameCodec

`RawAvailableMessageFrameCodec` is a compatibility codec for simple raw byte receive scenarios. It reads the bytes returned by the underlying stream read operation and immediately returns them as a single frame.

This is useful when an external tool sends a simple string such as:

```text
test1
```

without:

- a length prefix
- `CRLF`
- `LF`
- a fixed JSON envelope

Example usage:

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

## Important TCP Note

TCP is a stream protocol, not a message protocol. `RawAvailableMessageFrameCodec` is intentionally permissive, but it does not guarantee application-level message boundaries.

For example, two quick sends may arrive as:

```text
test1test2
```

or one send may be split as:

```text
te
st1
```

Therefore, use `RawAvailableMessageFrameCodec` only for compatibility, debugging, temporary tests, or tools that do not provide framing. For production protocols, prefer `LengthPrefixedMessageFrameCodec` or `DelimiterMessageFrameCodec`.

## Recommended Protocol Combinations

| Mode | Protocol adapter | Frame codec |
|---|---|---|
| Dreamine envelope | `DreamineEnvelopeProtocolAdapter` | `LengthPrefixedMessageFrameCodec` |
| Plain text line protocol | `PlainTextProtocolAdapter` | `DelimiterMessageFrameCodec` |
| Plain text raw receive | `PlainTextProtocolAdapter` | `RawAvailableMessageFrameCodec` |
| Raw JSON line protocol | `RawJsonProtocolAdapter` | `DelimiterMessageFrameCodec` |

## Design Principles

- Keep concrete transport implementations isolated from upper layers.
- Depend on `Dreamine.Communication.Abstractions` contracts.
- Keep package responsibilities small and explicit.
- Preserve one-way dependency flow.
- Separate payload protocol conversion from stream frame detection.
- Allow future adapters and codecs to be added without changing application logic.

## Dependencies

- `Dreamine.Communication.Abstractions`

## Target Framework

```text
net8.0
```

## Related Packages

- `Dreamine.Communication.Abstractions`
- `Dreamine.Communication.Core`
- `Dreamine.Communication.Sockets`
- `Dreamine.Communication.Serial`
- `Dreamine.Communication.RabbitMQ`
- `Dreamine.Communication.FullKit`
- `Dreamine.Communication.Wpf`

## Text Encoding Policy

Text encoding is handled at the protocol adapter boundary when raw external bytes are converted to a `MessageEnvelope`, or when a `MessageEnvelope` payload is converted back to external bytes.

Encoding options are intended for external byte-based communication:

- TCP
- UDP
- Serial

`PlainTextProtocolAdapter` and `RawJsonProtocolAdapter` can be configured with an external text encoding such as UTF-8 or CP949. UTF-8 is recommended for modern systems. CP949 is useful when interoperating with legacy Windows tools, Korean industrial equipment, or test tools that do not send UTF-8 text.

`InMemoryMessageBus` does not need text encoding because it passes `MessageEnvelope` objects inside the same process. RabbitMQ also does not expose this option in the default Dreamine flow because the adapter serializes `MessageEnvelope` data as UTF-8 JSON.

## License

This project is licensed under the MIT License.
