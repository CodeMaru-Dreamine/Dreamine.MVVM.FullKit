# Dreamine.Communication.FullKit

`Dreamine.Communication.FullKit`는 Dreamine Communication 계열 패키지의 일부입니다.

이 패키지는 Dreamine Communication 주요 패키지들을 한 번에 설치하기 위한 메타 패키지입니다.

[➡️ English Version](./README.md)

## 설명

Dreamine Communication의 Core, Socket, Serial, Broker 어댑터 패키지를 한 번에 설치하기 위한 올인원 메타 패키지입니다.

## 주요 기능

- 올인원 패키지 진입점
- Abstractions, Core, Sockets, Serial, RabbitMQ 참조
- 기본 net8.0 FullKit에서 WPF 제외
- 패키지 집계 경계 제공

## 설계 원칙

- 구체 통신 구현체를 상위 레이어와 분리합니다.
- `Dreamine.Communication.Abstractions`의 계약에 의존합니다.
- 패키지 책임을 작고 명확하게 유지합니다.
- 단방향 의존성 흐름을 유지합니다.
- 향후 어댑터를 추가해도 애플리케이션 로직을 변경하지 않도록 합니다.

## 패키지 역할

```text
Dreamine.Communication.FullKit
 ├─ Dreamine.Communication.Abstractions
 ├─ Dreamine.Communication.Core
 ├─ Dreamine.Communication.Sockets
 ├─ Dreamine.Communication.Serial
 └─ Dreamine.Communication.RabbitMQ
```

## 의존성

- `Dreamine.Communication.Abstractions`
- `Dreamine.Communication.Core`
- `Dreamine.Communication.Sockets`
- `Dreamine.Communication.Serial`
- `Dreamine.Communication.RabbitMQ`

## 대상 프레임워크

```text
net8.0
```

## 참고

기본 FullKit은 net8.0을 대상으로 하므로 WPF 구성요소는 의도적으로 포함하지 않습니다.

## 어떤 패키지를 설치할지

모든 전송 방식이 필요하지 않다면 필요한 패키지만 개별 설치하세요. FullKit은 편의용 진입점일 뿐, 참조 패키지들 외에 별도의 타입을 제공하지 않습니다.

| 상황 | 권장 패키지 |
|---|---|
| 계약만 사용 (테스트, Mock, 라이브러리 작성) | `Dreamine.Communication.Abstractions` |
| 프로세스 내부 Pub/Sub만 필요 | `Dreamine.Communication.Abstractions`, `Dreamine.Communication.Core` |
| TCP 또는 UDP 소켓 통신 | `+ Dreamine.Communication.Sockets` |
| RS232 / 시리얼 장비 통신 | `+ Dreamine.Communication.Serial` |
| RabbitMQ 브로커 연동 | `+ Dreamine.Communication.RabbitMQ` |
| WPF 모니터링/진단 UI | `+ Dreamine.Communication.Wpf` (FullKit에 미포함) |
| WPF 제외, 모든 전송을 한 번에 설치 | `Dreamine.Communication.FullKit` |

## 관련 패키지

- `Dreamine.Communication.Abstractions`
- `Dreamine.Communication.Core`
- `Dreamine.Communication.Sockets`
- `Dreamine.Communication.Serial`
- `Dreamine.Communication.RabbitMQ`
- `Dreamine.Communication.FullKit`
- `Dreamine.Communication.Wpf`

## 라이선스

이 프로젝트는 MIT 라이선스를 따릅니다.
