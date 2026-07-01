# Dreamine.IO.Fastech.Ethernet

[English documentation](./README.md)

Dreamine IO 스택용 Fastech Ethernet I/O 어댑터 패키지입니다.

이 패키지는 `Dreamine.IO.Abstractions`와 .NET 네트워킹 API만 참조합니다. Fastech SDK DLL, 벤더 런타임 DLL, 벤더 소스 코드는 재배포하지 않습니다.

## 현재 범위

- Fastech Ethernet 연결 옵션
- Fastech Ezi-IO Plus-E 통신용 UDP transport
- 향후 프로토콜 변형을 위한 TCP transport scaffold
- `IIoController` 구현
- Digital Input / Output 채널 래퍼
- 16점 DIO 프로토콜에서 not-supported를 반환하는 Analog Input / Output 채널 래퍼
- `IFastechEthernetIoProtocol` 기반 프로토콜 주입 지점
- Ezi-IO Plus-E 16 DI / 16 DO용 내장 `FastechPlusE16PointProtocol`

## 구현된 하드웨어 프로토콜

현재 실물 검증이 끝난 구현 대상은 UDP 기반 Fastech Ezi-IO Plus-E 16점 Digital I/O입니다.

- Transport: UDP
- 기본 포트: `3001`
- Header: `0xAA`
- 구현 명령:
  - `0x01` GetSlaveInfo probe
  - `0xC0` Digital Input 읽기
  - `0xC5` Digital Output 읽기
  - `0xC6` Digital Output 쓰기
- 채널 번호는 0-base입니다. `DI00`/`DO00`은 channel `0`입니다.

이 구현은 `SampleSmart`에서 실물 장비를 연결해 검증했습니다. 장비 프레임의 바이트 매핑은 `FastechPlusE16PointProtocol` 안에서 처리하므로, 애플리케이션 코드는 `IoPoint.Channel` 값 `0`부터 `15`를 사용하면 됩니다.

## SampleSmart 검증

`SampleSmart`에는 Fastech 16 DI / 16 DO 장비 테스트용 Dreamine I/O 샘플 페이지가 포함되어 있습니다.

권장 테스트 흐름:

1. PC 유선 NIC와 Fastech 장비를 같은 subnet에 둡니다.
2. 장비 IP와 port `3001`을 입력합니다.
3. `Use Real UDP`를 선택합니다.
4. `Connect`를 누릅니다.
5. `Probe`로 raw UDP 응답을 확인합니다.
6. `Read DI`, `Write DO`, `Read DO`로 채널 매핑을 검증합니다.

샘플 상태줄에는 raw TX/RX 프레임이 표시됩니다. 다른 Fastech 모델을 추가할 때는 모델별 바이트 매핑을 실물 장비로 확인해야 하므로 이 로그를 유지하는 것이 좋습니다.

## 현재 제한 사항

- 내장 프로토콜은 현재 실물 검증된 Ezi-IO Plus-E 16 DI / 16 DO 모델만 대상으로 합니다.
- 이 프로토콜에서는 Analog I/O를 구현하지 않았습니다.
- A접/B접 반전, debounce, ON delay, OFF delay, pulse output, interlock 로직은 아직 구현하지 않았습니다.
- 신호 보정 기능은 Fastech 프로토콜 클래스 안이 아니라 raw I/O 채널 위에 얹는 벤더 중립 레이어로 추가하는 방향이 적합합니다.
- 다른 Fastech 모델은 별도 프로토콜 구현으로 추가하고 실물 장비로 검증해야 합니다.

## 사용 예

```csharp
var options = new FastechEthernetIoOptions
{
    Host = "192.168.0.2",
    Port = 3001,
    TransportType = FastechEthernetIoTransportType.Udp,
    ReceiveTimeoutMs = 1000
};

await using var controller = new FastechEthernetIoController(options);
await controller.ConnectAsync();

var inputs = Enumerable
    .Range(0, 16)
    .Select(channel => new IoPoint(0, channel, $"DI{channel:00}"))
    .ToArray();

var readResult = await controller.DigitalInputs.ReadAsync(inputs);
```

## 벤더 런타임 정책

이 패키지는 Fastech 런타임 DLL을 포함하지 않습니다. 현재 Ezi-IO Plus-E 구현은 벤더 DLL 없이 UDP 프레임만 직접 사용합니다. 향후 벤더 제공 런타임 파일이 필요한 어댑터는 별도 패키지로 분리하고, 사용자가 벤더 소프트웨어를 직접 설치하고 정식 라이선스를 보유하도록 해야 합니다.

## 라이선스

MIT License.
