# Dreamine.PLC.Omron.CxComponent

[Korean documentation](./README_KO.md)

Omron CX-Compolet adapter boundary for Dreamine PLC communication.

## Important vendor runtime notice

This package must not redistribute Omron CX-Compolet, SYSMAC Gateway, DLLs, installers, samples, or licensed runtime files.

Users must install and license the required Omron software separately according to Omron's license terms.

This package may only contain adapter code that integrates with a user-installed vendor runtime.

## Current status

This package provides a late-bound COM adapter without redistributing or directly referencing the vendor runtime.

Main types:

- `OmronCxComponentPlcClient`
- `OmronCxComponentOptions`
- `OmronCxAddressNameFormatter`

The default ProgID is `OMRON.Compolet.CJ2Compolet`. The adapter opens the component through the `Active` property and calls `ReadVariable` and `WriteVariable`. Depending on the installed CX-Compolet control, `ProgId`, `PeerAddressPropertyName`, `ReadVariableMethodName`, and `WriteVariableMethodName` can be adjusted through options.

Sample:

- Open the `SampleSmart` PLC Monitor page and select `CxComponent`.
- Confirm `CX ProgID` and `CX Peer`, then run `Use Client` -> `Connect`.

Recommended production path:

- Use `Dreamine.PLC.Omron.Fins` for direct FINS TCP/UDP protocol communication.
- Use this package only when a project explicitly requires CX-Compolet or SYSMAC Gateway integration.

## License

Dreamine adapter code: MIT License.

Omron CX-Compolet and SYSMAC Gateway: not included and not licensed by this package.
