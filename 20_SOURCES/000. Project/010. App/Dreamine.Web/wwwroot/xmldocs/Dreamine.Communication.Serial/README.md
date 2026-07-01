# Dreamine.Communication.Serial

`Dreamine.Communication.Serial` is part of the Dreamine Communication package family.

This package provides RS232 and `SerialPort` based transport implementations while keeping serial-device specific logic isolated from the upper application layer.

[➡️ 한국어 문서 보기](./README_KO.md)

## Description

Serial transport package for Dreamine Communication. It opens and manages a .NET `SerialPort`, adapts the port stream to the shared Dreamine transport abstraction, and sends or receives `MessageEnvelope` instances through a selected protocol adapter and frame codec.

## Package Role

```text
Dreamine.Communication.Abstractions
    ↑
Dreamine.Communication.Core
    ↑
Dreamine.Communication.Serial
```

`Serial` owns only the concrete serial communication boundary:

- Opening and closing a serial port.
- Applying RS232 communication options.
- Running the receive loop over `SerialPort.BaseStream`.
- Encoding outgoing `MessageEnvelope` instances through an `IMessageProtocolAdapter`.
- Splitting or writing stream data through an `IMessageFrameCodec`.

It must not own application command rules, UI state, routing policies, TCP connection logic, RabbitMQ connection logic, or WPF-specific logic.

## Features

- `SerialPort` based transport
- RS232 communication boundary
- `MessageEnvelope` based send and receive flow
- Configurable protocol adapter
- Configurable frame codec
- Shared JSON serialization and protocol adapters from `Core`
- Shared stream framing from `Core`
- Async receive loop with connection state management

## Main Components

| Type | Role |
|---|---|
| `SerialPortTransport` | `IMessageTransport` implementation for RS232 / serial communication. |
| `SerialPortTransportOptions` | Serial port configuration model. |
| `SerialPortStreamAdapter` | Small wrapper around `SerialPort.BaseStream`. |
| `SerialCommunicationException` | Serial communication specific exception type. |

## SerialPortTransport

`SerialPortTransport` implements the shared `IMessageTransport` contract.

Default constructor behavior:

```text
Protocol adapter : DreamineEnvelopeProtocolAdapter
Frame codec      : LengthPrefixedMessageFrameCodec
```

This default is correct for Dreamine-to-Dreamine communication over a serial line. For external devices, PLCs, barcode readers, inspection tools, or terminal utilities, pass the protocol adapter and frame codec explicitly.

Example:

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

| Option | Default | Meaning |
|---|---:|---|
| `PortName` | `COM1` | Serial port name. |
| `BaudRate` | `9600` | Communication speed. |
| `DataBits` | `8` | Data bit count. Valid range is 5 to 8. |
| `Parity` | `Parity.None` | Parity mode. |
| `StopBits` | `StopBits.One` | Stop bit mode. |
| `Handshake` | `Handshake.None` | Hardware/software flow control. |
| `ReadTimeoutMs` | `3000` | Serial read timeout in milliseconds. |
| `WriteTimeoutMs` | `3000` | Serial write timeout in milliseconds. |
| `ReadBufferSize` | `4096` | Serial receive buffer size. |
| `WriteBufferSize` | `4096` | Serial transmit buffer size. |

## Protocol Adapter and Frame Codec

Serial communication is also stream-based. The transport does not know where an application message begins or ends by itself. Message meaning and message boundary are intentionally separated:

| Responsibility | Type |
|---|---|
| Convert bytes to/from `MessageEnvelope` | `IMessageProtocolAdapter` |
| Detect message boundaries on a stream | `IMessageFrameCodec` |
| Open, close, send, receive through COM port | `SerialPortTransport` |

## Recommended Combinations

| Scenario | Protocol adapter | Frame codec |
|---|---|---|
| Dreamine-to-Dreamine serial communication | `DreamineEnvelopeProtocolAdapter` | `LengthPrefixedMessageFrameCodec` |
| Text device ending each message with `CRLF` or `LF` | `PlainTextProtocolAdapter` | `DelimiterMessageFrameCodec` |
| Simple terminal tool or device sends raw text without delimiter | `PlainTextProtocolAdapter` | `RawAvailableMessageFrameCodec` |
| Device sends JSON text with line ending | `RawJsonProtocolAdapter` | `DelimiterMessageFrameCodec` |
| Device sends fixed binary protocol | Custom protocol adapter | Custom frame codec |

## RawAvailableMessageFrameCodec Support

`RawAvailableMessageFrameCodec` is defined in `Dreamine.Communication.Core`, not in this package. `SerialPortTransport` can still use it because the transport accepts any `IMessageFrameCodec`.

This mode is useful when a device or terminal tool sends a plain value such as:

```text
test1
```

without:

- a Dreamine envelope
- a length prefix
- `CRLF`
- `LF`

Example serial raw-text setup:

```csharp
var transport = new SerialPortTransport(
    options,
    new PlainTextProtocolAdapter(
        Encoding.UTF8,
        "serial.raw.available",
        "Serial.RawAvailable"),
    new RawAvailableMessageFrameCodec());
```

## Important Serial Stream Note

RS232 serial communication is a byte stream. `RawAvailableMessageFrameCodec` reads the bytes returned by one stream read operation and treats them as one message.

That is convenient for manual testing, but it is not a strict message boundary rule. Depending on device timing, buffer behavior, and baud rate, multiple sends can be merged or a single send can be split.

For production device protocols, prefer one of these designs:

- Use a delimiter such as `CR`, `LF`, or `CRLF`.
- Use a length-prefixed frame.
- Use a fixed-length binary frame.
- Use a device-specific custom `IMessageFrameCodec`.

Use `RawAvailableMessageFrameCodec` for compatibility, simple tools, debugging, or devices that have no explicit framing rule.

## Connection State

`SerialPortTransport` reports its state through the shared `ConnectionState` enum.

| State | Meaning |
|---|---|
| `Disconnected` | The COM port is closed. |
| `Connecting` | The transport is opening the COM port. |
| `Connected` | The COM port is open and the receive loop is active. |
| `Disconnecting` | The transport is closing the COM port. |
| `Faulted` | An error occurred in the serial connection or receive loop. |

## Error Handling

The transport validates options before opening the port. Invalid settings fail early.

Examples:

- Empty `PortName`
- `BaudRate <= 0`
- `DataBits` outside 5 to 8
- Non-positive timeout values
- Non-positive buffer sizes

Runtime port errors may move the transport to `Faulted` state. Application-level recovery should close the transport, correct the port condition, and reconnect.

## Usage Example: Line-Based Text Device

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

## Design Principles

- Keep concrete serial implementation isolated from upper layers.
- Depend on `Dreamine.Communication.Abstractions` contracts.
- Reuse `Core` protocol adapters and frame codecs.
- Separate serial port control from payload interpretation.
- Keep package responsibilities small and explicit.
- Preserve one-way dependency flow.
- Allow future serial devices and custom codecs to be added without changing application logic.

## Dependencies

- `Dreamine.Communication.Abstractions`
- `Dreamine.Communication.Core`
- `System.IO.Ports`

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

## License

This project is licensed under the MIT License.
