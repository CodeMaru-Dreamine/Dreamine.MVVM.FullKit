# Dreamine.PLC.Wpf

[English documentation](./README.md)

Dreamine PLC 통신을 위한 WPF 모니터링 및 진단 UI 컴포넌트입니다.

이 패키지는 Dreamine PLC 패키지군의 모든 `IPlcClient` 구현체와 바인딩 가능한 재사용 PLC Monitor View를 제공합니다.

## 주요 기능

- PLC 연결 상태 표시
- 선택 가능한 Client 바인딩 흐름
- Bit Read/Write 진단
- Word Read/Write 진단
- 동작 로그 표시
- Faulted / Disconnected 상태 처리
- 재사용 가능한 WPF Monitor Control

## 샘플에서 검증된 Client 타입

Monitor는 모든 `IPlcClient`와 사용할 수 있습니다. 현재 SampleSmart에서는 다음 Client 흐름이 검증되었습니다.

- InMemory PLC Client
- Dreamine Simulator TCP Client
- Mitsubishi MC TCP Client
- Mitsubishi MC UDP Client
- Omron FINS TCP Client
- Omron FINS UDP Client

## 샘플 Mode 매칭 규칙

샘플은 하나의 통합 PLC Protocol 페이지를 사용합니다. 시뮬레이터 기반 테스트에서는 서버와 클라이언트 Mode가 반드시 같아야 합니다.

```text
SimulatorTcp ↔ SimulatorTcp
McTcp        ↔ McTcp
McUdp        ↔ McUdp
FinsTcp      ↔ FinsTcp
FinsUdp      ↔ FinsUdp
```

Mode가 다르면 통신 실패가 정상입니다.

## PC-to-PC 테스트 요구사항

두 PC 간 테스트를 진행할 때는 서버 PC의 해당 프로토콜 포트를 열어야 합니다.

예: `55000` 포트 사용 시

```powershell
New-NetFirewallRule -DisplayName "Dreamine PLC TCP 55000" -Direction Inbound -Protocol TCP -LocalPort 55000 -Action Allow
New-NetFirewallRule -DisplayName "Dreamine PLC UDP 55000" -Direction Inbound -Protocol UDP -LocalPort 55000 -Action Allow
```

PowerShell은 관리자 권한으로 실행해야 합니다. 클라이언트 PC는 일반적으로 서버 역할을 하지 않으면 인바운드 규칙이 필요 없습니다.

## 실제 PLC 테스트 안내

WPF Monitor는 실제 PLC Client와 연결할 수 있지만, 실제 PLC 검증은 별도로 진행해야 합니다.

실제 PLC 연결 전 확인 항목:

- PLC IP 주소와 Port
- 프로토콜 Mode
- TCP/UDP 방화벽 경로
- PLC Ethernet Module 설정
- PLC Memory 영역 매핑
- 안전한 Polling 주기
- Write 동작 안전성

실제 PLC에는 1ms Polling을 사용하지 마십시오. 모니터링은 100ms ~ 500ms, 제어 신호 Write는 이벤트 기반을 권장합니다.

## 패키지 범위

이 패키지는 벤더 프로토콜을 직접 구현하지 않습니다. UI 컴포넌트와 ViewModel만 제공합니다.

벤더 프로토콜 구현은 다음 패키지에 둡니다.

- `Dreamine.PLC.Mitsubishi.MC`
- `Dreamine.PLC.Omron.Fins`
- 향후 벤더 패키지

## 라이선스

MIT License.
