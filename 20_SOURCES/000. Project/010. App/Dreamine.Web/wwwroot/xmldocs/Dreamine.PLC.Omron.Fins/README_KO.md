# Dreamine.PLC.Omron.Fins

[English documentation](./README.md)

Dreamine PLC 패키지군을 위한 Omron FINS TCP/UDP PLC 어댑터입니다.

이 패키지는 FINS TCP/UDP Client와 로컬 및 PC-to-PC 검증을 위한 내장 FINS Simulator Server를 제공합니다.

## 주요 기능

- Omron FINS TCP Client
- Omron FINS UDP Client
- FINS TCP Simulator Server
- FINS UDP Simulator Server
- Memory Area Read/Write 지원 경계
- Word Read/Write 진단
- 반복 Handshake 검증 흐름
- UDP Timeout 및 Retry 지원
- `IPlcClient` 통합

## 지원되는 시뮬레이터 테스트 Mode

SampleSmart의 PLC Protocol 페이지는 다음 조합을 지원합니다.

```text
FinsTcp ↔ FinsTcp
FinsUdp ↔ FinsUdp
```

서버와 클라이언트 Mode는 반드시 같아야 합니다. `SimulatorTcp`, `McTcp`, `McUdp` 서버는 `FinsTcp`, `FinsUdp` 클라이언트와 통신할 수 없습니다.

## 1PC 테스트

로컬 검증은 다음 흐름으로 진행합니다.

```text
Mode: FinsTcp 또는 FinsUdp
Host: 127.0.0.1
Port: 55000
Start Server
Use Client
Connect
Write Words
Read Words
Run Handshake
```

## 2PC 테스트

서버 PC:

```text
Mode: FinsTcp 또는 FinsUdp
Host: 0.0.0.0
Port: 55000
Start Server
```

클라이언트 PC:

```text
Mode: 서버와 동일
Host: 서버 PC IP
Port: 55000
Use Client
Connect
Read/Write 또는 Handshake
```

## PC-to-PC 테스트 방화벽 요구사항

서버 PC의 인바운드 포트를 열어야 합니다.

TCP:

```powershell
New-NetFirewallRule -DisplayName "Dreamine PLC FINS TCP 55000" -Direction Inbound -Protocol TCP -LocalPort 55000 -Action Allow
```

UDP:

```powershell
New-NetFirewallRule -DisplayName "Dreamine PLC FINS UDP 55000" -Direction Inbound -Protocol UDP -LocalPort 55000 -Action Allow
```

PowerShell은 관리자 권한으로 실행해야 합니다. 이 규칙이 없으면 1PC 테스트는 통과하지만 2PC 테스트는 실패할 수 있습니다.

## 실제 PLC 테스트 안내

FINS 지원은 현재 내장 Simulator 기준으로 검증되었습니다. 실제 Omron PLC 연동 테스트는 별도로 진행해야 합니다.

실제 Omron PLC 연결 전 확인 항목:

- PLC 모델 및 Ethernet Module 지원 여부
- FINS TCP/UDP 설정
- Port 번호. 많은 FINS 환경에서는 9600 포트를 사용하도록 설정됩니다.
- Source / Destination Node 설정
- Network 번호
- Unit Address
- Memory Area 매핑
- PLC Ethernet Module 라우팅 설정
- 안전한 Polling 주기

FINS/TCP는 장비별 Handshake 또는 Node 설정이 필요할 수 있습니다. Simulator에서 성공했다고 해서 실제 PLC 호환성이 보장되는 것은 아니며, 현장 실기 테스트가 필요합니다.

## Polling 및 Write 안전성

실제 PLC에는 1ms Polling을 사용하지 마십시오.

실제 PLC 권장값:

- 모니터링: 100ms ~ 500ms
- UI 표시 갱신: 250ms ~ 1000ms
- Write: 이벤트 기반만 권장
- Handshake 부하 테스트: 명시적으로 승인된 실제 장비가 아니라면 Simulator 전용

## 벤더 런타임 정책

이 패키지는 Omron CX-Compolet, SYSMAC Gateway 또는 Omron 런타임 DLL을 포함하지 않습니다.

이 패키지는 FINS TCP/UDP 통신을 직접 구현합니다. CX-Compolet 연동이 필요하면 별도 어댑터 패키지로 분리하고, 벤더 DLL은 재배포하지 않아야 합니다.

## 검증 상태

검증됨:

- 1PC FINS TCP Read/Write 및 Handshake
- 1PC FINS UDP Read/Write 및 Handshake
- 2PC FINS TCP Read/Write 및 Handshake
- 2PC FINS UDP Read/Write 및 Handshake
- WPF Monitor 통합

대기 중:

- 실제 Omron PLC 검증

## 라이선스

MIT License.
