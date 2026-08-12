# Multi-Equipment Host

## 현재 경계

| Surface | 상태 | 경계 |
|---|---|---|
| Provider-neutral Orchestration 로컬 회귀 | `PASS` | 최종 Strict WPF 회귀/Build가 Green이며 정확한 Fresh 합계는 중앙 보고서에만 기록합니다. |
| 이번 실행의 독립 `--multi-self-test` 명령 | `NOT_RUN` | 2026-08-10 과거 명령 실행은 Matrix에 보존하며 회귀 Coverage는 로컬/합성 범위입니다. |
| 외부 SEComSimulator Multi-Equipment 상호운용 | `NOT_RUN` | 동시 Instance에 대한 Vendor 지원/License를 확인하지 못했습니다. |
| 운영 Fleet SDK | `NOT_APPLICABLE` | Orchestration은 내부 Workbench/Runtime 조합이며 새로운 Public Fleet API가 아닙니다. |

Multi-Equipment Host는 여러 Equipment Endpoint에 대한 독립 Host-role HSMS/GEM 연결을 조정합니다. 상호운용·격리 Surface이며 적합성 인증서, Capacity SLA, 운영 Fleet-management SDK, 외부 Simulator/실장비 Evidence가 아닙니다.

`Passed`, `Failed`, `WaitingForUser` 같은 UI/Runtime 문자열은 운영 값일 뿐 위 Evidence Status를 대체하지 않습니다.

## 경계와 소유권

Orchestration의 WPF 전용 Session 생성은 공유 provider-neutral Runtime 계층으로 이동했습니다. `ISecsMessageSession`을 소비하고 Provider의 `SecsConnectionIdentity`를 검증하며, Orchestration Type을 Public Fleet API로 승격하지 않습니다.

| 계층 | 책임 |
|---|---|
| Registry | 대소문자를 구분하지 않는 고유 `EquipmentId`를 보관하고 안정된 Snapshot을 제공하며 삭제·교체 Context를 Dispose합니다. |
| Provider Factory | Equipment Definition마다 Host-role Message-session Provider를 만들고 Role, Mode, Session ID를 검증합니다. |
| Context | Message Session 하나, Connection-scoped `GemRuntime`, Cancellation, Reconnect State, Diagnostic, Log Identity를 소유합니다. |
| Host | Selected/All 명령, 제한된 Fan-out, Credential-free Configuration, Cancellation과 최종 Dispose를 조정합니다. |

연결된 Context마다 독립 `ISecsMessageSession`을 소유하므로 Transaction Manager와 System Bytes Allocator가 Connection 범위에 머뭅니다. Connection-scoped `GemRuntime`은 연결이 닫히면 버리고 교체 Session용으로 다시 만듭니다. 한 Context의 Disconnect, Timeout, Reconnect, Dispose가 Peer Context를 의도적으로 변경하지 않습니다.

참조 방향은 WPF에서 공유 Runtime과 공개 SECS/GEM Abstraction으로만 향하며 Product Library는 WPF를 참조하지 않습니다.

## 식별과 상관관계

| 필드 | 범위 |
|---|---|
| `EquipmentId` | 운영자 지정 Registry Key이며 대소문자 구분 없이 고유합니다. |
| `ConnectionId` | Transport 연결 하나의 Identity이며 Reconnect 시 새 값이 생깁니다. |
| `SessionId` | Definition의 HSMS/SECS ID이며 서로 다른 연결이 같은 값을 사용할 수 있습니다. |
| `SystemBytes` | 소유 Session이 할당·상관 처리하며 다른 연결에서 같은 숫자를 독립적으로 사용할 수 있습니다. |

`EquipmentId`, `ConnectionId`는 Workbench Metadata이며 SECS Wire Message에 추가되지 않습니다. Protocol Correlation은 Session 내부에 머뭅니다. Log에는 독립 Context를 구분할 Connection/Header Metadata가 있지만 Endpoint/Header/Timing Data도 운영상 민감할 수 있습니다.

## 수명주기와 동시성

- Registry Add/Remove/Replace가 Context Lifetime을 소유합니다. Import는 모든 교체 Definition을 검증한 뒤 Registry를 전환하고 이전 Context를 Dispose합니다.
- Context의 Connect/Disconnect/Reconnect 전이는 직렬화됩니다. Dispose는 대기 중인 Passive Accept와 Lifetime-linked Operation을 취소합니다.
- Active Automatic Reconnect는 제한된 Coordinator를 사용하고 새 Connection Identity/Session/GEM Runtime을 만든 뒤 Select를 복원합니다. Passive Automatic Reconnect는 거부됩니다.
- 한 번에 하나의 Aggregate Host Command만 실행하며 내부 작업은 제한된 Connect/Message/Reconnect 동시성을 사용합니다.
- Observable Collection 알림은 설정한 Synchronization Context를 통하며 Network 작업을 View Code-behind로 옮기지 않습니다.
- Host Dispose는 새 작업을 거부하고 Lifetime Operation을 취소하며 추적 Aggregate Work를 기다린 다음 모든 Context와 소유 Reconnect Coordinator를 Dispose합니다. Cleanup 실패는 숨기지 않습니다.

Self-test Counter는 명시적으로 계측한 Workbench Context, 합성 Peer, Reconnect Work, Host Delegate만 셉니다. 모든 CLR Task, Socket State, Allocation의 총합이 아닙니다.

## Credential-free 설정

Schema Version 1 JSON에는 Fan-out Limit, `EquipmentId`, Host/Address, Port, Active/Passive Mode, Session ID, Automatic-reconnect 선택, T3/T5/T6/T7/T8만 들어갑니다. Username, Password, Token, Certificate, Secret Field는 없습니다.

Import는 Schema Version, 고유 ID, Endpoint, Mode, Session 값, Timer 범위, Reconnect/Mode 조합을 검증한 뒤 Registry를 교체합니다. Endpoint/Configuration Data도 민감할 수 있으므로 Deployment Access Control 아래 저장하고 고객/Private-sidecar Configuration을 Public Package나 Repository에 넣지 마십시오.

## UI 사용 순서

1. **Multi Equipment Host**를 선택하고 **Multi Equipment**를 엽니다.
2. Definition을 추가하거나 검증된 Configuration을 Import합니다.
3. 한 Context에는 Selected 명령, 전체에는 제한된 All/Broadcast 명령을 사용합니다.
4. Context별 TCP, HSMS, GEM, Responder/Activity, Last Error를 확인합니다.
5. Protocol Log를 Equipment/자유 검색어로 Filter하고 Equipment, Connection, Session, System Bytes 열로 연결합니다.
6. Credential-free Configuration은 승인된 위치에만 Export합니다.

**Run 1/2/10/50 Self-test**는 로컬 합성 Peer를 만듭니다. 구성된 외부 Endpoint를 사용하지 않으며 외부 행을 승격할 수 없습니다.

## Headless self-test

Release 출력 폴더:

```powershell
Dreamine.SecsGem.Interop.Wpf.exe --multi-self-test --output multi-equipment-self-test.json
```

Project 폴더:

```powershell
dotnet run --project Dreamine.SecsGem.Interop.Wpf.csproj -c Release -- --multi-self-test --output multi-equipment-self-test.json
```

종료 코드 `0`은 Runner 자체 Assertion과 계측 Cleanup 조건 성공, `2`는 Assertion/Cleanup 조건 실패, `1`은 처리되지 않은 Runner 오류를 뜻합니다. Export의 `Passed`/`Failed` Text는 운영 Result Field이며 외부 상호운용 Status가 아닙니다.

범위는 [MULTI_EQUIPMENT_TEST_MATRIX.md](MULTI_EQUIPMENT_TEST_MATRIX.md), 날짜가 고정된 과거 측정은 [MULTI_EQUIPMENT_PERFORMANCE.md](MULTI_EQUIPMENT_PERFORMANCE.md)를 참고하십시오.

## 외부 사전 확인

읽기 전용 조사로는 Vendor가 지원하는 SEComSimulator 동시 Instance, 해당 License, GUI 하나의 복수 Active Equipment 연결을 확인하지 못했습니다. 따라서 승인된 외부 Multi-Equipment Result가 없으며 상태는 `NOT_RUN`입니다.

추후 Vendor 문서와 License가 동시 Instance를 허용할 때 운영자는 격리된 Configuration/Log 위치와 고유 Loopback Port를 사용하고, 서로 다른 두 Process가 Selected임을 증명한 뒤 한쪽만 Reconnect하여 영향받지 않은 Peer가 Selected와 Correlated Traffic을 유지하는지 확인해야 합니다. Process Identity, Endpoint, Timestamp, 양쪽의 Finalize된 Evidence를 보존합니다. Root 실행이 승인된 Manual Request를 통합하므로 이 문서는 별도 요청을 만들지 않습니다.

## 제한

- 로컬 합성 Peer는 계측 구현만 검증하며 표준 적합성, 운영 준비, Network Capacity를 확립하지 않습니다.
- Configuration에는 Credential/TLS-material Model이 없습니다.
- Context는 현재 기본 Connection-scoped `GemRuntime`을 조합합니다. 고정 E30 Equipment Profile 선택은 별도 Single-equipment Responder Surface입니다.
- Throughput/Memory 값은 진단 비교점이며 SLA가 아닙니다.
- 외부 Simulator와 실장비 행은 양쪽 Evidence를 Review할 때까지 `NOT_RUN`입니다.
