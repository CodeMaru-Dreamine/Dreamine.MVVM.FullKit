# Multi-Equipment Host

상태: 2026-08-10 기준 Harness 구현 및 로컬 loopback 증거.

Multi-Equipment Host는 하나의 Harness 프로세스에서 여러 장비 endpoint의 HSMS/GEM 연결을 각각 독립적으로 관리한다. 상호운용 및 격리 시험을 위한 기능이며 적합성 인증서, 운영용 장비군 관리 SDK, 외부 Simulator 또는 실제 장비 시험 증거가 아니다.

## 경계와 소유권

이 구현은 `Dreamine.Secs.Abstractions`, `Dreamine.Secs.Com`, `Dreamine.Gem.Abstractions`, `Dreamine.Gem`의 public API를 변경하지 않는다. 오케스트레이션 타입은 모두 WPF Harness 내부 `internal` 타입이다.

| 계층 | 책임 |
|---|---|
| Registry | 고유한 `EquipmentId` 항목을 보관하고 fan-out용 snapshot을 만들며, 삭제·교체된 Context를 Dispose한다. |
| Factory | 장비 정의마다 Connection Context 하나를 생성한다. |
| Context | `HsmsSession` 하나, 연결 상태, 연결 단위 `GemRuntime` 하나, 취소·진단·로그 식별자를 소유한다. |
| Host | Selected/All 명령, 제한된 병렬 실행, 설정 Import/Export, 결과 집계, 취소와 최종 Dispose를 조정한다. |

연결된 Context마다 별도 `HsmsSession`을 소유하므로 Transaction Manager와 `SystemBytes` Generator를 다른 장비와 공유하지 않는다. `GemRuntime`도 Context/연결 단위이며 새 연결이 성립하면 새 인스턴스로 교체된다. 따라서 한 장비의 Disconnect, Timeout, Reconnect, Dispose가 다른 장비 Context를 의도적으로 변경하지 않는다.

참조 방향은 Harness에서 SECS/GEM 라이브러리 쪽으로만 향한다. 라이브러리가 Harness 오케스트레이션을 참조하지 않는다.

## 식별과 상관관계

| 필드 | 범위와 용도 |
|---|---|
| `EquipmentId` | Registry에서 사용하는 운영자 지정 논리 키이다. 대소문자를 구분하지 않고 고유해야 한다. |
| `ConnectionId` | 특정 transport 연결을 나타내는 Harness 생성 값이다. Reconnect하면 새 값이 부여된다. |
| `SessionId` | 장비 정의에 포함된 HSMS/SECS protocol 식별자이다. 서로 다른 연결에서 같은 값을 사용할 수 있다. |
| `SystemBytes` | 해당 `HsmsSession` 안에서 할당하고 Transaction을 연결하는 값이다. 다른 연결에서는 같은 값이 다시 나타나도 Transaction을 공유하지 않는다. |

Protocol 상관관계는 Session 내부에서 처리한다. `EquipmentId`와 `ConnectionId`는 Context 선택과 로그 구분을 위한 Harness metadata이며 wire의 SECS Message에 추가되지 않는다. Protocol Log에는 `EquipmentId`, `ConnectionId`, endpoint, `SessionId`, 방향, SxFy, `SystemBytes`가 기록된다. 로컬 격리 시험은 서로 다른 연결에 `SessionId=0`, `SystemBytes=1`을 의도적으로 동시에 사용하고 각 응답이 올바른 장비 Context로 돌아오는지 검증한다.

## 수명주기와 Thread Safety

- Registry가 Add/Get/Remove/Replace를 소유한다. 설정 Import는 기존 Context를 Dispose한 뒤 구성 목록을 교체한다.
- Context는 Connect/Disconnect/Reconnect 전이를 직렬화하고 호출자 취소를 자체 수명주기에 연결한다. Dispose 시 대기 중인 Passive accept도 취소한다.
- Active 자동 재연결은 새 `ConnectionId`와 `GemRuntime`을 만들고 HSMS Select를 복원한다. Passive 모드의 자동 재연결 설정은 거부된다.
- Host는 한 번에 하나의 집계 명령을 수행하고, 그 명령 내부 장비 작업만 제한된 동시성으로 실행한다. 기본값은 Connect 10, Message 20, Reconnect 5이다.
- Observable Collection 변경은 필요한 경우 WPF Dispatcher로 전달한다. Network와 서비스 책임을 View 또는 code-behind로 이동하지 않는다.
- Host Dispose는 새 작업과 진행 중인 집계 작업을 취소하고 추적 중인 Host 작업이 끝나기를 기다린 다음 모든 Context와 Session을 비동기로 Dispose한다.

Self-test의 자원 Counter는 Harness가 예약한 장비 delegate, Harness/loopback live session, selection recovery, loopback peer background operation 범위이다. 모든 CLR `Task`나 runtime 내부 continuation의 총 개수를 뜻하지 않는다.

## Credential 없는 JSON 설정

UI에서 schema version 1 JSON을 Export/Import할 수 있다. 포함 필드는 다음으로 한정된다.

- Host fan-out 제한값: `ConnectConcurrency`, `MessageConcurrency`, `ReconnectConcurrency`
- `EquipmentId`, host/address, port, Active/Passive mode, `SessionId`
- 자동 재연결 여부와 T3/T5/T6/T7/T8 값

Username, password, token, certificate 등 credential 필드는 없다. Import 시 schema version, 중복 Equipment ID, endpoint, mode, session ID, timer 범위, reconnect/mode 조합을 검증한 다음 Registry를 교체한다. 다만 endpoint 정보도 운영 환경에서는 민감할 수 있으므로 배포 환경의 일반적인 파일 접근 정책을 적용해야 한다.

## UI 사용 순서

1. Host Mode에서 **Multi Equipment Host**를 선택하고 **Multi Equipment** 탭을 연다.
2. 장비 정의를 직접 추가하거나 **Import Configuration**을 사용한다.
3. 한 행에는 Selected 명령을, 전체 구성에는 Connect/Disconnect/Linktest/Scenario/Broadcast 명령을 사용한다.
4. 행별 TCP, HSMS, GEM, Activity, Last Error를 확인한다.
5. **Protocol Log**에서 장비 또는 자유 검색어로 필터링하고 Equipment, Connection, Session, System Bytes 열로 로그를 연결한다.
6. **Export Configuration**으로 credential 없는 연결 설정을 저장한다.

**Run 1/2/10/50 Self-test**는 로컬 합성 loopback peer를 생성한다. 구성된 외부 endpoint를 시험하지 않으며 외부 상호운용 증거로 기록하면 안 된다.

## Headless self-test

Release 출력 폴더에서 다음을 실행한다.

```powershell
Dreamine.SecsGem.Interop.Wpf.exe --multi-self-test --output multi-equipment-self-test.json
```

또는 프로젝트 폴더에서 실행한다.

```powershell
dotnet run --project Dreamine.SecsGem.Interop.Wpf.csproj -c Release -- --multi-self-test --output multi-equipment-self-test.json
```

이 명령은 10대 중 한 대를 대상으로 100회 Reconnect한다. 종료 코드 `0`은 Scenario가 `Passed`이고 추적 Session/Background Operation이 남지 않았음을, `2`는 Assertion 또는 정리 결과 실패를, `1`은 Headless Runner의 처리되지 않은 오류를 뜻한다.

검증 항목은 [MULTI_EQUIPMENT_TEST_MATRIX.md](MULTI_EQUIPMENT_TEST_MATRIX.md), 최신 계측 snapshot은 [MULTI_EQUIPMENT_PERFORMANCE.md](MULTI_EQUIPMENT_PERFORMANCE.md)를 참고한다.

## 외부 Simulator 사전 확인

로컬 읽기 전용 조사만으로는 Simulator 동시 인스턴스에 대한 Vendor 지원 또는 GUI 하나에서 둘 이상의 Equipment 연결 지원을 확인하지 못했다. 따라서 승인된 외부 Multi-Equipment 절차나 Passed 결과는 아직 없다.

추후 Vendor 문서와 License 조건에서 동시 인스턴스를 명확히 허용하는 경우에만 사용자가 **조건부** 수동 시험을 준비한다. Application/Configuration/Log 위치를 인스턴스별로 격리하고 7201·7202 같은 서로 다른 loopback port를 사용한 뒤, 서로 다른 두 Process가 모두 Selected인지 확인한다. 그 다음 한 인스턴스만 Reconnect하고 다른 인스턴스가 Selected를 유지하면서 S1F1/F2와 Linktest를 계속 처리하는지 확인한다. Process 식별자, endpoint, 시각, 양쪽 Log, 영향받지 않은 Peer 결과를 증거로 남긴다. 이 선행조건과 관찰 결과를 기록하기 전까지 외부 항목은 모두 **Not Run / Waiting for User**로 유지한다. 기존 단일 연결 증거 절차는 [SECOMSIMULATOR_INTEROP_TEST_KO.md](SECOMSIMULATOR_INTEROP_TEST_KO.md)를 참고한다.

## 현재 제한

- 로컬 loopback은 이 구현과 한 프로세스 안의 동작을 검증한다. 표준 적합성, 운영 준비 완료, 실제 Network 수용량을 증명하지 않는다.
- 오케스트레이션은 Harness 내부 기능이며 이번 변경에서 Multi-Equipment public library API를 추가하지 않았다.
- 설정 모델에는 credential 또는 TLS material이 없다.
- 처리량과 Memory 값은 비교용 진단 수치이며 SLA가 아니다. Runtime warm-up, 장비 부하, loopback scheduling에 따라 달라진다.
- 외부 SEComSimulator Scenario는 사용자가 외부 절차를 수행하고 결과를 기록할 때까지 반드시 **Not Run / Waiting for User**이다. 로컬 loopback 결과로 대체하면 안 된다.
