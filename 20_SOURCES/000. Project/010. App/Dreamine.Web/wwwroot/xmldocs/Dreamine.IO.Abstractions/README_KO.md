# Dreamine.IO.Abstractions

[English documentation](./README.md)

Dreamine IO 패키지군에서 공통으로 사용하는 벤더 중립 산업용 I/O 계약 레이어입니다.

이 패키지는 Digital Input, Digital Output, Analog Input, Analog Output 흐름에서 공통으로 사용할 인터페이스와 모델만 정의합니다. Ajin, Comizoa, Fastech 또는 기타 벤더 런타임 DLL은 참조하지 않습니다.

## 목적

`Dreamine.IO.Abstractions`는 I/O 계층의 최하위 계약 레이어입니다. 애플리케이션, 샘플, 시뮬레이터, 실제 벤더 어댑터가 이 계약을 공유하되 MIT 패키지 안에 벤더 라이선스 바이너리가 들어오지 않도록 분리합니다.

```text
Application / Sample
        ↓
Dreamine.IO.Abstractions
        ↑
Vendor Adapters / Simulators
```

## 포함 계약

- I/O Controller 추상화
- Digital Input / Output 채널 추상화
- Digital Input / Output 다점 읽기 계약
- Analog Input / Output 채널 추상화
- 공통 I/O 연결 상태
- Digital / Analog Point 모델
- 공통 I/O Result 모델
- 벤더 중립 연결 옵션

## 현재 메모

- Digital Input은 단점 읽기와 다점 읽기를 지원합니다.
- Digital Output은 단점 읽기, 다점 읽기, 단점 쓰기, 다점 쓰기를 지원합니다.
- 포인트 번호는 0-base입니다. 예를 들어 channel `0`은 `DI00` 또는 `DO00`입니다.
- A접/B접, debounce, ON delay, OFF delay 같은 FA 현장용 신호 보정 레이어는 아직 구현하지 않았습니다. 이 기능은 특정 벤더 프로토콜 안이 아니라 raw I/O 채널 위에 얹는 벤더 중립 wrapper로 추가하는 방향이 적합합니다.

## 설계 규칙

- 이 패키지는 벤더 중립으로 유지합니다.
- Ajin AXT, Comizoa, Fastech 또는 기타 벤더 런타임 어셈블리를 참조하지 않습니다.
- 벤더 SDK 소스 파일이나 바이너리를 이 패키지에 복사하지 않습니다.
- 실제 벤더 어댑터는 `Dreamine.IO.Ajin`, `Dreamine.IO.Comizoa`, `Dreamine.IO.Fastech` 같은 별도 패키지로 분리합니다.
- 런타임 어댑터 사용자는 벤더 소프트웨어를 직접 설치하고 정식 라이선스를 보유해야 합니다.

## 벤더 런타임 정책

이 패키지는 벤더 런타임 DLL을 포함하지 않습니다. `IoProvider` enum은 공급사 계열을 식별하기 위한 값일 뿐, Dreamine이 해당 벤더 런타임을 재배포한다는 의미가 아닙니다.

## 라이선스

MIT License.
