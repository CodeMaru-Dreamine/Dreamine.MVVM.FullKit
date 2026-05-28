# Dreamine.PLC Package Family

Dreamine PLC는 C#/.NET 기반 산업 자동화 애플리케이션을 위한 모듈형 PLC 통신 패키지군입니다.

## 패키지 구성

| Package | 목적 | 상태 |
|---|---|---|
| `Dreamine.PLC.Abstractions` | 공통 PLC 계약 | 준비됨 |
| `Dreamine.PLC.Core` | InMemory Client 및 공통 Simulator Runtime | 준비됨 |
| `Dreamine.PLC.Wpf` | WPF 진단 Monitor | 준비됨 |
| `Dreamine.PLC.Mitsubishi.MC` | Mitsubishi MC TCP/UDP 어댑터 및 Simulator | Simulator 검증됨 |
| `Dreamine.PLC.Omron.Fins` | Omron FINS TCP/UDP 어댑터 및 Simulator | Simulator 검증됨 |
| `Dreamine.PLC.Mitsubishi.MxComponent` | MX Component late-bound COM 어댑터 | 벤더 Runtime 필요 |
| `Dreamine.PLC.Omron.CxComponent` | CX-Compolet late-bound COM 어댑터 | 벤더 Runtime 필요 |

## 현재 검증 상태

검증됨:

- 1PC Simulator TCP Read/Write 및 Handshake
- 2PC Simulator TCP Read/Write 및 Handshake
- 1PC Mitsubishi MC TCP Read/Write 및 Handshake
- 2PC Mitsubishi MC TCP Read/Write 및 Handshake
- 1PC Mitsubishi MC UDP Read/Write 및 Handshake
- 2PC Mitsubishi MC UDP Read/Write 및 Handshake
- 1PC Omron FINS TCP Read/Write 및 Handshake
- 2PC Omron FINS TCP Read/Write 및 Handshake
- 1PC Omron FINS UDP Read/Write 및 Handshake
- 2PC Omron FINS UDP Read/Write 및 Handshake
- WPF Monitor 통합

대기 중:

- 실제 Mitsubishi PLC 테스트
- 실제 Omron PLC 테스트
- 현장별 Memory Map 검증
- 현장별 Polling/Write 정책 검증

## Mode 매칭 규칙

시뮬레이터 기반 테스트에서는 서버와 클라이언트 Mode가 반드시 같아야 합니다.

```text
SimulatorTcp ↔ SimulatorTcp
McTcp        ↔ McTcp
McUdp        ↔ McUdp
FinsTcp      ↔ FinsTcp
FinsUdp      ↔ FinsUdp
```

서로 다른 Mode 간 통신은 실패하는 것이 정상입니다. 각 Mode가 사용하는 프로토콜이 다르기 때문입니다.

`MxComponent`, `CxComponent`는 시뮬레이터 서버를 사용하지 않고 설치된 벤더 Runtime을 직접 호출합니다. SampleSmart PLC Monitor에서 해당 Mode를 선택한 뒤 `Use Client`와 `Connect` 순서로 실행합니다.

## Mitsubishi MX Component 테스트

SampleSmart는 기본 실행 및 `x86` Platform 빌드에서 MX Component 정석 경로를 사용합니다.

```powershell
dotnet build .\SampleSmart.csproj -c Debug -p:Platform=x86
```

실행 후 PLC Monitor에서 다음 값을 사용합니다.

- Mode: `MxComponent`
- MX ProgID: `ActUtlType.ActUtlType`
- MX LS: MX Component Communication Setup Utility에 등록한 Logical Station Number

`x64` 실행에서는 Mitsubishi `DotUtlType64` wrapper가 구형 .NET Framework WCF 타입을 요구할 수 있습니다. 이 경우 SampleSmart `net8.0-windows` 프로세스에서 직접 연결하지 말고 `x86` 실행을 사용하거나 별도 .NET Framework helper 프로세스로 중계해야 합니다.

## PC-to-PC 방화벽 요구사항

PC-to-PC 테스트에서는 서버 PC의 인바운드 포트를 열어야 합니다.

예: `55000` 포트 사용 시

```powershell
New-NetFirewallRule -DisplayName "Dreamine PLC TCP 55000" -Direction Inbound -Protocol TCP -LocalPort 55000 -Action Allow
New-NetFirewallRule -DisplayName "Dreamine PLC UDP 55000" -Direction Inbound -Protocol UDP -LocalPort 55000 -Action Allow
```

PowerShell은 관리자 권한으로 실행해야 합니다. 이 설정이 없으면 1PC 테스트는 성공하지만 2PC 테스트가 실패할 수 있습니다.

## 실제 PLC 테스트 요구사항

Simulator 검증은 실제 PLC 검증을 대체하지 않습니다.

실제 운영 전 대상 PLC 모델로 다음 항목을 검증해야 합니다.

- PLC 통신 설정
- TCP/UDP Port
- 네트워크 라우팅 및 방화벽
- Device Memory 영역
- FINS의 Node/Network/Unit Address
- 필요한 경우 MC Frame 설정
- Polling 주기
- Write 안전 정책
- 에러 복구 동작

## Polling 안전 기준

1ms Polling은 Simulator 부하 테스트 전용입니다.

실제 PLC에 1ms 주기로 통신하지 마십시오.

권장 기본값:

- 모니터링: 100ms ~ 500ms
- UI 표시 갱신: 250ms ~ 1000ms
- Write: 이벤트 기반만 권장

## 벤더 Runtime 정책

Dreamine PLC 패키지는 벤더 Runtime DLL을 재배포하지 않습니다.

포함하지 않는 항목:

- Mitsubishi MX Component DLL
- Omron CX-Compolet DLL
- Omron SYSMAC Gateway Runtime 파일
- 기타 벤더 라이선스 설치 파일 또는 Runtime 파일

벤더 Runtime 어댑터를 사용할 경우 사용자는 벤더 소프트웨어를 별도로 설치하고 정식 라이선스를 보유해야 합니다.

## 라이선스

Dreamine 소스 코드: MIT License.

벤더 제품 및 상표는 각 소유자에게 귀속됩니다.
