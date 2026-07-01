# Dreamine.PLC.Abstractions

[English documentation](./README.md)

Dreamine PLC 패키지군에서 공통으로 사용하는 PLC 통신 계약 레이어입니다.

이 패키지는 PLC Client, Simulator Client, WPF 진단 UI, 프로토콜 어댑터가 공통으로 사용하는 벤더 중립 인터페이스와 모델을 정의합니다.

## 목적

`Dreamine.PLC.Abstractions`는 PLC 계층의 최하위 계약 레이어입니다. 특정 PLC 벤더, 전송 구현, WPF UI, 시뮬레이터 런타임에 의존하면 안 됩니다.

```text
Application / Sample
        ↓
Dreamine.PLC.Wpf
        ↓
Dreamine.PLC.Abstractions
        ↑
Vendor Adapters / Core / Simulators
```

## 포함 계약

- PLC Client 추상화
- PLC 연결 상태 모델
- PLC 디바이스 주소 모델
- Bit / Word Read/Write 요청 모델
- 동작 결과 모델
- 공통 PLC Client 옵션

## 설계 규칙

- 이 패키지는 벤더 중립으로 유지합니다.
- Mitsubishi, Omron, Siemens, LS, MX Component, CX-Compolet 전용 타입을 참조하지 않습니다.
- WPF를 참조하지 않습니다.
- 시뮬레이터 전용 동작을 Abstraction 계층에 넣지 않습니다.
- 모든 PLC 어댑터가 의존하는 안정적인 경계로 사용합니다.

## 검증 상태

현재 Dreamine PLC 샘플 흐름에서 다음 항목으로 검증되었습니다.

- InMemory PLC Client
- Dreamine TCP Simulator
- Mitsubishi MC TCP Simulator
- Mitsubishi MC UDP Simulator
- Omron FINS TCP Simulator
- Omron FINS UDP Simulator
- WPF PLC Monitor 바인딩
- 1PC 및 2PC 시뮬레이터 기반 Read/Write, Handshake 테스트

실제 PLC 연동은 각 프로토콜 패키지에서 별도 실기 테스트가 필요합니다.

## 벤더 런타임 정책

이 패키지는 Mitsubishi MX Component, Omron CX-Compolet, SYSMAC Gateway 또는 기타 벤더 런타임 DLL을 포함하지 않습니다.

벤더 런타임 어댑터는 별도 패키지로 분리해야 하며, 사용자가 벤더 소프트웨어를 직접 설치하고 정식 라이선스를 보유해야 합니다.

## 라이선스

MIT License.
