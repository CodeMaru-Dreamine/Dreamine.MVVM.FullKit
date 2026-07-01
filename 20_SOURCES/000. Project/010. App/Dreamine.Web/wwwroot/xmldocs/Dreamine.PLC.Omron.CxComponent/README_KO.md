# Dreamine.PLC.Omron.CxComponent

[English documentation](./README.md)

Dreamine PLC 통신을 위한 Omron CX-Compolet 어댑터 경계 패키지입니다.

## 중요 벤더 런타임 안내

이 패키지는 Omron CX-Compolet, SYSMAC Gateway, DLL, 설치 파일, 샘플, 라이선스가 필요한 Runtime 파일을 재배포하면 안 됩니다.

사용자는 Omron의 라이선스 조건에 따라 필요한 Omron 소프트웨어를 별도로 설치하고 정식 라이선스를 보유해야 합니다.

이 패키지는 사용자 PC에 설치된 벤더 Runtime과 연동하기 위한 어댑터 코드만 포함할 수 있습니다.

## 현재 상태

이 패키지는 벤더 Runtime을 직접 참조하지 않는 late-bound COM 어댑터를 제공합니다.

주요 클래스:

- `OmronCxComponentPlcClient`
- `OmronCxComponentOptions`
- `OmronCxAddressNameFormatter`

기본 ProgID는 `OMRON.Compolet.CJ2Compolet`이며, `Active` 속성을 통해 연결 상태를 열고 `ReadVariable`, `WriteVariable`을 호출합니다. CX-Compolet 구성에 따라 `ProgId`, `PeerAddressPropertyName`, `ReadVariableMethodName`, `WriteVariableMethodName`을 옵션으로 조정할 수 있습니다.

샘플:

- `SampleSmart`의 PLC Monitor 화면에서 `CxComponent` 모드를 선택합니다.
- `CX ProgID`, `CX Peer` 값을 확인한 뒤 `Use Client` -> `Connect` 순서로 실행합니다.

권장 운영 경로:

- 직접 FINS TCP/UDP 통신은 `Dreamine.PLC.Omron.Fins`를 사용합니다.
- 프로젝트에서 명시적으로 CX-Compolet 또는 SYSMAC Gateway 연동이 필요한 경우에만 이 패키지를 사용합니다.

## 라이선스

Dreamine 어댑터 코드: MIT License.

Omron CX-Compolet 및 SYSMAC Gateway: 이 패키지에 포함되지 않으며, 이 패키지의 라이선스 대상도 아닙니다.
