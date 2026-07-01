# Dreamine.PLC.Mitsubishi.MxComponent

[Korean documentation](./README_KO.md)

Mitsubishi MX Component adapter boundary for Dreamine PLC communication.

## Important vendor runtime notice

This package must not redistribute Mitsubishi MX Component DLLs, installers, samples, or licensed runtime files.

Users must install and license Mitsubishi MX Component separately according to Mitsubishi Electric's license terms.

This package may only contain adapter code that integrates with a user-installed vendor runtime.

## Current status

This package provides a late-bound COM adapter without redistributing or directly referencing the vendor runtime.

Main types:

- `MitsubishiMxComponentPlcClient`
- `MitsubishiMxComponentOptions`
- `MitsubishiMxDeviceNameFormatter`

The default ProgID follows the current process bitness:

- `x86`: `ActUtlType.ActUtlType`
- `x64`: `ActUtlType64.ActUtlWrap`

The adapter uses the MX Component `LogicalStationNumber` and calls `Open`/`Close`. Word block access first tries `ReadDeviceBlock2`/`WriteDeviceBlock2`; when COM late binding rejects block array arguments, it falls back to repeated `GetDevice`/`SetDevice` calls.

Sample:

- Open the `SampleSmart` PLC Monitor page and select `MxComponent`.
- Confirm `MX ProgID` and `MX LS`, then run `Use Client` -> `Connect`.
- For the standard MX Component path, run SampleSmart as `x86` and use `ActUtlType.ActUtlType`.
- The Mitsubishi `DotUtlType64` wrapper can require legacy .NET Framework WCF types; if it fails under `net8.0-windows x64`, use the `x86` path or a separate .NET Framework bridge process.

Recommended production path:

- Use `Dreamine.PLC.Mitsubishi.MC` for direct MC TCP/UDP protocol communication.
- Use this package only when a project explicitly requires MX Component integration.

## License

Dreamine adapter code: MIT License.

Mitsubishi MX Component: not included and not licensed by this package.
