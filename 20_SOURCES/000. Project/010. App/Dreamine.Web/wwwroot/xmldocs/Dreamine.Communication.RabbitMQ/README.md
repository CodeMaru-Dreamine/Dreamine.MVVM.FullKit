# Dreamine.Communication.RabbitMQ

`Dreamine.Communication.RabbitMQ` is part of the Dreamine Communication package family.

This package provides a RabbitMQ-based `IMessageBus` implementation for Dreamine Communication. It connects the common Dreamine message contract, `MessageEnvelope`, to RabbitMQ exchange, queue, and routing key based publish/subscribe messaging.

[➡️ 한국어 문서 보기](./README_KO.md)

## Description

RabbitMQ message bus adapter for Dreamine Communication.

This package is responsible for integrating RabbitMQ as an optional broker-backed message bus while keeping upper application layers dependent only on `Dreamine.Communication.Abstractions`.

## Features

- RabbitMQ-based `IMessageBus` implementation
- RabbitMQ connection lifecycle management
- Publish / Subscribe support through RabbitMQ exchange, queue, and routing key
- `MessageEnvelope` JSON serialization and deserialization
- RabbitMQ topology declaration
  - Exchange
  - Queue
  - Queue binding
- RabbitMQ options model
- RabbitMQ-specific communication exception type
- Communication Monitor sample compatibility
- Docker RabbitMQ server validation scenario

## Package Role

```text
Dreamine.Communication.Abstractions
    ↑
Dreamine.Communication.RabbitMQ
```

`Dreamine.Communication.RabbitMQ` depends on the abstraction contracts and provides a concrete RabbitMQ adapter.

Application code should depend on `IMessageBus`, `MessageEnvelope`, and shared communication contracts instead of depending directly on RabbitMQ-specific implementation types.

## Main Components

### RabbitMqMessageBus

`RabbitMqMessageBus` implements `IMessageBus`.

Responsibilities:

- Connect to a RabbitMQ server
- Declare exchange, queue, and queue binding topology
- Publish `MessageEnvelope` messages
- Subscribe to a route and dispatch received messages to registered handlers
- Manage RabbitMQ connection and channel lifecycle
- Update connection state through the `ConnectionState` contract

### RabbitMqMessageBusOptions

Defines RabbitMQ connection and routing settings.

Typical fields:

- `HostName`
- `Port`
- `VirtualHost`
- `UserName`
- `Password`
- `ExchangeName`
- `QueueName`
- `RoutingKey`

### RabbitMqCommunicationException

RabbitMQ-specific exception type for communication-layer failures.

## Basic Usage

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

## RabbitMQ Test Server

For local validation, RabbitMQ can be started through Docker.

```bash
docker run -d --name dreamine-rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
```

RabbitMQ Management UI:

```text
http://localhost:15672
```

Default credentials:

```text
guest / guest
```

Sample configuration:

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

## Validated Scenario

The RabbitMQ adapter has been validated with the Dreamine Communication sample.

Validated flow:

```text
Connect
→ Subscribe
→ Publish
→ Receive
→ Monitor SEND / RECV log confirmation
```

Validated items:

- RabbitMQ Docker container startup
- RabbitMQ Management UI access
- RabbitMQ connection
- Exchange / queue / routing key topology declaration
- Message publish
- Message receive through subscription handler
- `MessageEnvelope` JSON serialization and deserialization
- Communication Monitor channel state update
- Communication Monitor SEND / RECV message logs

## Design Principles

- Keep broker-specific implementation isolated from upper layers.
- Depend on `Dreamine.Communication.Abstractions` contracts.
- Keep package responsibilities small and explicit.
- Preserve one-way dependency flow.
- Keep RabbitMQ optional and replaceable.
- Allow application logic to use `IMessageBus` without knowing RabbitMQ details.
- Avoid leaking RabbitMQ.Client types into application-level code.

## Dependencies

- `Dreamine.Communication.Abstractions`
- `RabbitMQ.Client`

## Third-party License Notice

This package uses `RabbitMQ.Client`.

`RabbitMQ.Client` is dual-licensed under:

- Apache License 2.0
- Mozilla Public License 2.0

This package uses `RabbitMQ.Client` under the Apache License 2.0 option.

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
