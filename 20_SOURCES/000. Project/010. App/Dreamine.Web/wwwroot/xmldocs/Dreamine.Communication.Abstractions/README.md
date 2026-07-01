# Dreamine.Communication.Abstractions

`Dreamine.Communication.Abstractions` provides the lowest-level contracts, models, options, lifecycle interfaces, enums, and common exceptions used by the Dreamine Communication package family.

This package does **not** implement any concrete communication protocol. It defines the shared foundation used by TCP, UDP, Serial, RabbitMQ, HTTP, in-memory messaging, and future communication adapters.

[➡️ 한국어 문서 보기](./README_KO.md)

## Purpose

The purpose of this package is to keep application logic independent from concrete communication technologies.

Application code should depend on abstractions such as `IMessageBus`, `IMessageTransport`, `IMessageProtocolAdapter`, and `MessageEnvelope` instead of depending directly on TCP, `SerialPort`, RabbitMQ, HTTP clients, or any vendor-specific communication library.

## Package Role

This package is the contract layer of the Dreamine Communication architecture.

```text
Dreamine.Communication.Abstractions
    ↑
Dreamine.Communication.Core
    ↑
Sockets / Serial / RabbitMQ / WPF / FullKit
```

`Abstractions` must not reference `Core`, `Sockets`, `Serial`, `RabbitMQ`, `WPF`, or any UI/runtime-specific package.

## Included Components

### Interfaces

| Interface | Role |
|---|---|
| `IConnectionLifecycle` | Common connection lifecycle contract with `State`, `ConnectAsync`, and `DisconnectAsync`. |
| `IMessageBus` | Publish/subscribe messaging contract. |
| `IMessageTransport` | Connection-based send/receive transport contract. |
| `IMessageProtocolAdapter` | Converts external protocol payloads to and from `MessageEnvelope`. |
| `IMessageRouter` | Routes received `MessageEnvelope` instances to registered handlers. |
| `IMessageSerializer` | Serializes and deserializes `MessageEnvelope` instances. |

### Models

| Model | Role |
|---|---|
| `MessageEnvelope` | Standard internal message unit used across Dreamine Communication. |
| `CommunicationError` | Common communication error information model. |

### Options

| Option | Role |
|---|---|
| `CommunicationOptions` | Shared communication configuration such as name, auto-connect, and reconnect policy. |
| `MessageBusOptions` | Message bus behavior options such as default route and handler execution policy. |
| `TransportOptions` | Common transport settings such as host, port, serial port, baud rate, and timeouts. |

### Enums

| Enum | Role |
|---|---|
| `ConnectionState` | Represents disconnected, connecting, connected, disconnecting, and faulted states. |
| `TransportKind` | Represents in-memory, serial, TCP, UDP, HTTP, and RabbitMQ transport families. |

### Exceptions

| Exception | Role |
|---|---|
| `CommunicationException` | Base exception for the Dreamine Communication layer. |
| `CommunicationConnectionException` | Specialized exception for connection failures. |

## Core Concepts

### MessageEnvelope

`MessageEnvelope` is the standard message unit used inside Dreamine Communication.

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

Recommended usage:

| Property | Recommended usage |
|---|---|
| `MessageId` | Unique message identifier for tracing and diagnostics. |
| `Name` | Human-readable message name, such as `Machine.Start` or `Tcp.RawAvailable.Receive`. |
| `Route` | Routing key used by message buses and routers. |
| `Payload` | Raw binary payload. Text, JSON, protocol packets, or device data can all be stored here. |
| `Headers` | Metadata such as protocol name, content type, encoding, source, or correlation id. |
| `CreatedAt` | Message creation timestamp in UTC. |

### IMessageProtocolAdapter

`IMessageProtocolAdapter` is the contract that separates external protocol formats from the Dreamine internal message model.

```text
External bytes
    ↓
Frame codec in Core package
    ↓
IMessageProtocolAdapter.Decode(...)
    ↓
MessageEnvelope
```

In the opposite direction:

```text
MessageEnvelope
    ↓
IMessageProtocolAdapter.Encode(...)
    ↓
Frame codec in Core package
    ↓
External bytes
```

This interface is intentionally defined in `Abstractions` because protocol conversion is needed by multiple implementation packages.

Examples of protocol adapters in higher packages:

| Adapter | Typical purpose |
|---|---|
| `DreamineEnvelopeProtocolAdapter` | Dreamine-native envelope serialization. |
| `PlainTextProtocolAdapter` | Plain string payloads. |
| `RawJsonProtocolAdapter` | JSON payloads without forcing upper layers to know the transport. |

### Relation to Core Frame Codecs

Frame codecs are implemented in `Dreamine.Communication.Core`, not in this package.

The responsibility split is:

| Layer | Responsibility |
|---|---|
| Frame codec | Splits or writes message boundaries in a byte stream. |
| Protocol adapter | Converts one complete payload to or from `MessageEnvelope`. |
| Transport | Sends and receives through TCP, Serial, RabbitMQ, etc. |
| Message bus/router | Dispatches messages by route. |

For example, `RawAvailableMessageFrameCodec` belongs in `Dreamine.Communication.Core`. It can receive a simple TCP payload such as `test1` without requiring `CRLF` or a length prefix. No new abstraction is required for that feature because the existing `IMessageProtocolAdapter` contract already handles payload-to-envelope conversion.

Recommended combinations:

| Scenario | Frame codec | Protocol adapter |
|---|---|---|
| Dreamine internal communication | `LengthPrefixedMessageFrameCodec` | `DreamineEnvelopeProtocolAdapter` |
| Text protocol with delimiter | `DelimiterMessageFrameCodec` | `PlainTextProtocolAdapter` |
| Hercules or simple raw TCP string test | `RawAvailableMessageFrameCodec` | `PlainTextProtocolAdapter` |
| Raw JSON line protocol | `DelimiterMessageFrameCodec` | `RawJsonProtocolAdapter` |

## Interface Responsibility Guide

### `IConnectionLifecycle`

Use this interface for objects that have a connection state and explicit connect/disconnect behavior.

```csharp
public interface IConnectionLifecycle
{
    ConnectionState State { get; }
    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
}
```

### `IMessageTransport`

Use this interface for connection-based communication components such as TCP clients, TCP servers, serial transports, or other physical/logical transports.

```csharp
public interface IMessageTransport : IConnectionLifecycle, IAsyncDisposable
{
    TransportKind Kind { get; }
    event EventHandler<MessageEnvelope>? MessageReceived;
    Task SendAsync(MessageEnvelope message, CancellationToken cancellationToken = default);
}
```

### `IMessageBus`

Use this interface for publish/subscribe messaging.

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

Use this interface when message dispatching must be separated from the bus or transport implementation.

### `IMessageSerializer`

Use this interface when `MessageEnvelope` serialization must be replaceable. For example, JSON, binary, compressed, or encrypted serializers can implement this contract in a higher package.

## TransportKind

`TransportKind` identifies the communication family, not a specific implementation class.

```text
InMemory
Serial
Tcp
Udp
Http
RabbitMq
```

Examples:

| TransportKind | Possible implementation |
|---|---|
| `InMemory` | `InMemoryMessageBus` |
| `Tcp` | TCP client/server transport |
| `Serial` | RS232/SerialPort transport |
| `RabbitMq` | RabbitMQ message bus |
| `Udp` | UDP transport |
| `Http` | HTTP request/response or streaming adapter |

## ConnectionState

`ConnectionState` provides a common state model for both transports and message buses.

```text
Disconnected → Connecting → Connected → Disconnecting → Disconnected
                                      ↓
                                   Faulted
```

Recommended usage:

| State | Meaning |
|---|---|
| `Disconnected` | Not connected. |
| `Connecting` | Connection attempt is in progress. |
| `Connected` | Communication is active. |
| `Disconnecting` | Disconnection is in progress. |
| `Faulted` | Communication failed or entered an invalid state. |

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

For serial communication:

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

## Example

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

End-to-end flow using only the abstractions plus a concrete `IMessageBus` implementation:

```csharp
using System.Text;
using Dreamine.Communication.Abstractions.Interfaces;
using Dreamine.Communication.Abstractions.Models;

// IMessageBus is provided by a concrete package, e.g. InMemoryMessageBus
// from Dreamine.Communication.Core or RabbitMqMessageBus from
// Dreamine.Communication.RabbitMQ.
IMessageBus bus = /* concrete implementation */;

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

Application code depends only on `IMessageBus` and `MessageEnvelope`. The concrete transport (in-memory, RabbitMQ, or a transport-backed bus through `TransportMessageBusAdapter`) can be replaced without changing this code.

## Design Principles

- Keep abstraction contracts independent from concrete implementations.
- Prevent the application layer from referencing transport-specific libraries directly.
- Allow TCP, Serial, RabbitMQ, UDP, HTTP, MQTT, and future adapters to be added without changing upper layers.
- Keep message format conversion separate from frame boundary detection.
- Follow dependency inversion and interface-based design.
- Keep the package lightweight and UI-framework independent.
- Keep the dependency direction one-way: implementations depend on abstractions, not the other way around.

## Target Framework

```text
net8.0
```

This package does not require WPF and does not depend on any Windows-specific UI framework.

## Related Packages

- `Dreamine.Communication.Core`
- `Dreamine.Communication.Sockets`
- `Dreamine.Communication.Serial`
- `Dreamine.Communication.RabbitMQ`
- `Dreamine.Communication.Wpf`
- `Dreamine.Communication.FullKit`

## License

This project is licensed under the MIT License.
