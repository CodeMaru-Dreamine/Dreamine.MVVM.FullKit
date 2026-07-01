# Dreamine.PLC.Abstractions

[Korean documentation](./README_KO.md)

Common PLC communication contracts for the Dreamine PLC package family.

This package defines vendor-neutral interfaces and models used by PLC clients, simulator clients, WPF diagnostics, and protocol adapters.

## Purpose

`Dreamine.PLC.Abstractions` is the lowest PLC layer. It must not depend on a specific PLC vendor, transport implementation, WPF UI, or simulator runtime.

```text
Application / Sample
        ↓
Dreamine.PLC.Wpf
        ↓
Dreamine.PLC.Abstractions
        ↑
Vendor Adapters / Core / Simulators
```

## Included contracts

- PLC client abstraction
- PLC connection state model
- PLC device address model
- Bit and word read/write request models
- Operation result models
- Common PLC client options

## Design rules

- Keep this package vendor-neutral.
- Do not reference Mitsubishi, Omron, Siemens, LS, MX Component, or CX-Compolet specific types.
- Do not reference WPF.
- Do not include simulator-only behavior in the abstraction layer.
- Use this package as the stable boundary for every PLC adapter.

## Validation status

This package is validated through the current Dreamine PLC sample flow:

- InMemory PLC client
- Dreamine TCP simulator
- Mitsubishi MC TCP simulator
- Mitsubishi MC UDP simulator
- Omron FINS TCP simulator
- Omron FINS UDP simulator
- WPF PLC monitor binding
- 1PC and 2PC simulator-based read/write and handshake tests

Physical PLC integration still requires vendor-specific testing in each protocol package.

## Vendor runtime policy

This package does not include Mitsubishi MX Component, Omron CX-Compolet, SYSMAC Gateway, or any vendor runtime DLL.

Vendor runtime adapters must be implemented in separate packages and must require users to install and license the vendor software separately.

## License

MIT License.
