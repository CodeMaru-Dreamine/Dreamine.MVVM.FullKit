# Dreamine.PLC.Core

[English documentation](./README.md)

Dreamine PLC 통신을 위한 공통 Runtime 패키지입니다.

이 패키지는 InMemory PLC 시뮬레이션, 공통 주소 처리 지원, Simulator Server/Client 기반 구조, 프로토콜 어댑터와 WPF 진단 UI에서 사용하는 공통 Runtime 로직을 제공합니다.

## 주요 기능

- InMemory PLC Client
- 공통 PLC Memory 모델
- Dreamine TCP Simulator Server/Client
- 기본 Read/Write 명령 흐름
- PC-to-PC 시뮬레이터 테스트 지원
- PLC 패키지군에서 공유하는 Runtime 유틸리티

## 아키텍처

```text
Dreamine.PLC.Wpf / SampleSmart
        ↓
Dreamine.PLC.Abstractions
        ↓
Dreamine.PLC.Core
        ↓
Protocol Adapters / Simulators
```

`Dreamine.PLC.Core`는 벤더 중립으로 유지합니다. Mitsubishi MC와 Omron FINS 세부 프로토콜은 각각의 전용 패키지에 둡니다.

## Simulator 프로토콜

내장 Dreamine Simulator는 진단 및 프레임워크 검증용 텍스트 기반 프로토콜입니다.

예시 명령 흐름:

```text
WRITE_WORDS D100 100,200,300,400
READ_WORDS D100 4
WRITE_BITS M10 1,0,1,0
READ_BITS M10 4
```

이 Simulator는 Mitsubishi MC 프로토콜도 아니고 Omron FINS 프로토콜도 아닙니다.

## Mode 매칭 규칙

PC-to-PC 테스트에서는 서버 Mode와 클라이언트 Mode가 반드시 같아야 합니다.

```text
SimulatorTcp ↔ SimulatorTcp
McTcp        ↔ McTcp
McUdp        ↔ McUdp
FinsTcp      ↔ FinsTcp
FinsUdp      ↔ FinsUdp
```

`SimulatorTcp` 서버는 `McTcp`, `McUdp`, `FinsTcp`, `FinsUdp` 클라이언트와 통신할 수 없습니다.

## PC-to-PC 방화벽 요구사항

PC-to-PC 테스트에서는 서버 PC의 인바운드 포트가 열려 있어야 합니다.

예: `55000` 포트 사용 시

```powershell
New-NetFirewallRule -DisplayName "Dreamine PLC TCP 55000" -Direction Inbound -Protocol TCP -LocalPort 55000 -Action Allow
New-NetFirewallRule -DisplayName "Dreamine PLC UDP 55000" -Direction Inbound -Protocol UDP -LocalPort 55000 -Action Allow
```

PowerShell은 관리자 권한으로 실행해야 합니다. 이 설정을 하지 않으면 1PC 테스트는 되지만 2PC 테스트가 실패할 수 있습니다.

## 실제 PLC 주의사항

내장 Simulator는 개발 및 부하 테스트용입니다.

- 1ms Polling은 Simulator 부하 테스트 전용입니다.
- 실제 PLC에 1ms 주기로 통신하지 마십시오.
- 실제 PLC 모니터링 권장 주기: 100ms ~ 500ms
- UI 표시 갱신 권장 주기: 250ms ~ 1000ms
- PLC Write는 상시 주기 전송이 아니라 이벤트 기반으로 처리하는 것을 권장합니다.

## 검증 상태

다음 항목이 검증되었습니다.

- 1PC Simulator TCP Read/Write 및 Handshake
- 2PC Simulator TCP Read/Write 및 Handshake
- WPF Monitor 공통 흐름

실제 PLC 테스트는 각 프로토콜 패키지에서 별도 진행해야 합니다.

## 라이선스

MIT License.
