# Dreamine.SecsGem.FullKit

> Install the version-aligned Dreamine SECS-II, HSMS, GEM, and GEM300 stack with one package reference.

`Dreamine.SecsGem.FullKit` is a dependency-only meta package. It adds the public contracts and runtimes of the Dreamine SECS/GEM stack without adding another runtime assembly.

## Included packages

| Package | Role |
|---|---|
| `Dreamine.Secs.Abstractions` | SECS-II and HSMS contracts, messages, items, and provider boundaries |
| `Dreamine.Secs.Com` | SECS-II codec and HSMS communication runtime |
| `Dreamine.Gem.Abstractions` | GEM capability contracts |
| `Dreamine.Gem` | GEM behavior and equipment service runtime |
| `Dreamine.Gem300.Abstractions` | GEM300 capability contracts |
| `Dreamine.Gem300` | GEM300 capability runtime |

## Quick start

Install the meta package into a clean .NET 8 project:

```bash
dotnet add package Dreamine.SecsGem.FullKit --version 1.0.0
dotnet restore
dotnet build -c Release
```

Use `Dreamine.Secs.Com.Hsms.HsmsSession` for HSMS. Compose `Dreamine.Gem.GemRuntime` and `Dreamine.Gem300.Gem300Runtime` only for the capabilities required by the application. The meta package aligns dependencies; it does not configure an endpoint, responder, or workflow automatically.

## Scope and verification

This package provides an implementation toolkit, not a compliance certificate. Validate the selected configuration and scenarios against the standards and requirements applicable to the equipment integration.

## Known limitations

- This is a dependency-only meta package and intentionally contains no runtime assembly.
- Component domain services do not imply complete SECS/GEM/GEM300 wire mappings.
- Package installation and self-loopback tests do not establish external-simulator interoperability or current-revision standards conformance.
- NuGet indexing is eventually consistent. Immediately after a release, restore may need to be retried after all component packages become visible.

## Links

- Website: <https://dreamine.kr>
- GitHub: <https://github.com/CodeMaru-Dreamine>
- NuGet: <https://www.nuget.org/packages/Dreamine.SecsGem.FullKit>

## License

MIT
