# Dreamine.Communication.FullKit

`Dreamine.Communication.FullKit` is part of the Dreamine Communication package family.

This package is a meta package for installing the main Dreamine Communication package family together.

[➡️ 한국어 문서 보기](./README_KO.md)

## Description

All-in-one meta package for Dreamine Communication core, socket, serial, and broker adapter packages.

## Features

- All-in-one package entry point
- References Abstractions, Core, Sockets, Serial, and RabbitMQ
- Keeps WPF out of the default net8.0 FullKit
- Provides a convenient package aggregation boundary

## Design Principles

- Keep concrete transport implementations isolated from upper layers.
- Depend on `Dreamine.Communication.Abstractions` contracts.
- Keep package responsibilities small and explicit.
- Preserve one-way dependency flow.
- Allow future adapters to be added without changing application logic.

## Package Role

```text
Dreamine.Communication.FullKit
 ├─ Dreamine.Communication.Abstractions
 ├─ Dreamine.Communication.Core
 ├─ Dreamine.Communication.Sockets
 ├─ Dreamine.Communication.Serial
 └─ Dreamine.Communication.RabbitMQ
```

## Dependencies

- `Dreamine.Communication.Abstractions`
- `Dreamine.Communication.Core`
- `Dreamine.Communication.Sockets`
- `Dreamine.Communication.Serial`
- `Dreamine.Communication.RabbitMQ`

## Target Framework

```text
net8.0
```

## Note

WPF components are intentionally not included in this default FullKit package because the default package targets net8.0.

## When to Use Which Package

If you do not need every transport, install only what you need. The FullKit package is a convenience entry point; it does not provide any additional types beyond the referenced packages.

| Scenario | Recommended packages |
|---|---|
| Build only against contracts (test, mock, library author) | `Dreamine.Communication.Abstractions` |
| In-process publish/subscribe only | `Dreamine.Communication.Abstractions`, `Dreamine.Communication.Core` |
| TCP or UDP socket communication | `+ Dreamine.Communication.Sockets` |
| RS232 / serial device communication | `+ Dreamine.Communication.Serial` |
| RabbitMQ broker integration | `+ Dreamine.Communication.RabbitMQ` |
| WPF monitoring/diagnostic UI | `+ Dreamine.Communication.Wpf` (not included in FullKit) |
| Want every transport in one install (no WPF) | `Dreamine.Communication.FullKit` |

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
