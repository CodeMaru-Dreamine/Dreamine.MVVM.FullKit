# Dreamine SECS/GEM 상호운용 Workbench

이 .NET 8 WPF 애플리케이션은 공개 Dreamine SECS-II/HSMS Session Contract를 사용하는 provider-neutral Workbench입니다. 제한된 Connection Profile v1 적용, Message Template Catalog v1과 Scenario v1 로드, 설정형 Primary Responder 실행, finalize된 exact-wire JSONL 조회, privacy 경계가 있는 evidence 구성을 지원합니다. 엔지니어링·상호운용 도구이며 적합성 인증서가 아닙니다.

## 현재 evidence 경계

| Surface | 상태 | 경계 |
|---|---|---|
| 로컬 자동 Workbench 회귀 | `PASS` | 최종 Strict WPF 회귀/Build가 Green이며 정확한 Fresh 합계는 중앙 제품화 보고서에만 기록합니다. |
| 이번 실행의 WPF 화면 검증 | `BLOCKED_ENVIRONMENT` | 승인된 computer-use 경로가 app approval/elicitations 부재로 애플리케이션을 실행하지 못했습니다. 과거 fallback 캡처는 현재 화면 증거가 아닙니다. |
| SEComSimulator 상호운용 | `NOT_RUN` | Dreamine과 상대편 증거를 모두 검토하기 전에는 외부 `PASS`가 아닙니다. |
| 고정 E30 Demo 선택기/Responder Surface | `IMPLEMENTED_UNVERIFIED` | 정확한 선택기와 상호 배타적 Binding이 구현됐으며 적합성 Status가 아닙니다. |
| 고정 E30 Demo 별도 Process Smoke | `PASS` | 공개 Host/Equipment 역할이 정확한 고정 Dialogue TCP 실행을 완료했으며 로컬 Evidence로만 사용합니다. |
| E37.1 적합성 | `BLOCKED_STANDARD` | 필요한 라이선스 Revision을 사용할 수 없으므로 적합성을 추론하지 않습니다. |

UI Model에는 `InteropScenarioStatus.WaitingForUser`, `InteropScenarioStatus.Passed` 같은 운영 값이 있습니다. 이들은 Workflow/Runtime enum일 뿐 Release evidence 표의 상태 어휘로 복사하면 안 됩니다. Release evidence는 중앙 보고서의 literal status만 사용합니다.

## 실행

```powershell
dotnet run --project Dreamine.SecsGem.Interop.Wpf.csproj -c Release
```

외부 Simulator는 운영자가 **Launch Simulator — manual action**을 선택할 때만 시작됩니다.

## Workbench 흐름

1. Credential이 없는 Connection Profile v1을 적용합니다. Session 생성 설정 변경은 live session을 바꾸지 않고 recreate-required로 보고됩니다.
2. 필요하면 Message Template Catalog v1을 로드합니다. Template은 제한된 Application Data이며 Normative SxFy 정의가 아닙니다.
3. 하나의 native `ISecsMessageSession`을 Connect/Select합니다. Workbench는 경쟁 Receive Loop, Transaction Manager 또는 System Bytes Allocator를 만들지 않습니다.
4. Scenario v1을 UI에서 로드·실행하거나 같은 Runner를 Headless로 실행합니다.

```powershell
Dreamine.SecsGem.Interop.Wpf.exe --scenario scenario.json --profile profile.json --output result.json
```

`--scenario`, `--profile`은 필수이고 `--output`은 선택입니다. Runner는 제한된 Deadline, Repeat Limit, Cancellation을 적용합니다. Cleanup 실패, 비정상 Wire Log 또는 Inbound Message Drop이 있으면 성공 결과를 만들지 않습니다.

선택적 출력은 상태, 종료 코드, UTC 시각, Step 상태별 개수, Drop된 Inbound 개수, 제한된 사유 코드만 포함하는 닫힌 Public Summary입니다. Profile/Scenario 경로, Endpoint, Step 식별자, Free-form Provider Error, Message ID, System Bytes, Body는 의도적으로 제외합니다. 상세 In-process Result는 UI/Runtime 내부에서 유지되며 Public Headless Summary를 진단 Dump로 사용하면 안 됩니다.

## Equipment responder profile

**Equipment responder profile** 선택기는 Responder 활성 중 비활성화되므로 한 Profile만 Dispatcher Registration을 소유합니다.

- 기본값 **E30-0611 derived subset profile v1 (Demo)**는 공개 `E30DemoEquipmentProfile.Create()`와 `E30EquipmentRouter` 경로를 사용합니다. 범위는 고정된 20개 Dialogue뿐이며 최신 Revision 적합성이나 외부 검증이 아닙니다.
- **Educational basic responder (Demo-only, not GEM)**은 의도적으로 작은 fallback입니다. S1F1/F2, S1F3/F4, S1F11/F12, S1F13/F14, S1F15/F16, S1F17/F18, S2F17/F18 일곱 쌍의 작성법만 보여 줍니다. Application 의미는 별도 Equipment Profile에 두고 이 fallback을 GEM으로 표현하지 마십시오.

Reconnect 후 Responder 소유권은 교체 Session에 다시 Bind되고 이전 Binding과 함께 Dispose됩니다. E30과 교육용 경로는 상호 배타적입니다.

고정 E30 Dialogue 전체를 별도 Host/Equipment Process로 실행하는 예제는 [`Dreamine.Gem.QuickStart`](../../../../100.%20Library/Gem/samples/Dreamine.Gem.QuickStart)에 있습니다. 그 로컬 결과는 외부 Simulator evidence와 별개입니다.

## Exact-wire logging, privacy, evidence health

지속형 Wire Log는 Session의 실제 Wire Observation Stream을 소비합니다. 안전한 기본값은 `HeaderOnly`이며 `Excluded`는 Body/Raw Frame을 보존하지 않습니다. Safe Facade는 Global Full-body Capture를 거부합니다. Terminal Observation과 Queue Completion이 경쟁하지 않도록 Session을 먼저 Stop/Dispose한 뒤 Recorder를 finalize합니다.

두 Drop Counter가 0이고 Flush가 완료됐으며 Writer Failure가 없을 때만 Evidence Review 대상이 됩니다. Public Export는 닫힌 Allow-list를 사용하므로 Endpoint, Free-form Text, Raw Frame, Decoded Body, 임의 Check, Private-profile Traffic을 생략하거나 세부 내용을 숨깁니다. Header/Timing 식별자도 운영상 민감할 수 있으므로 Log Root 접근을 통제해야 합니다.

Evidence Manifest를 첨부하는 것만으로 외부 `PASS`가 되지 않습니다. Manual Verification, 정상 Dreamine Log, 상대편 Log 또는 Screenshot, Artifact Hash, 완료된 Checklist를 모두 갖춰야 Review할 수 있습니다.

## 로컬 self-test와 multi-equipment mode

```powershell
Dreamine.SecsGem.Interop.Wpf.exe --self-test --output self-test.json
Dreamine.SecsGem.Interop.Wpf.exe --multi-self-test --output multi-equipment-self-test.json
```

두 명령은 로컬 합성 Peer를 사용합니다. 로컬 자동화 Evidence만 만들 수 있고 외부 행을 승격할 수 없습니다. [Multi-Equipment Host](docs/MULTI_EQUIPMENT_HOST_KO.md), [시험 Matrix](docs/MULTI_EQUIPMENT_TEST_MATRIX.md), [과거 성능 Snapshot](docs/MULTI_EQUIPMENT_PERFORMANCE.md)을 참고하십시오.

일회성 외부 절차와 Evidence Checklist는 [SEComSimulator 상호운용 시험](docs/SECOMSIMULATOR_INTEROP_TEST_KO.md), 상태는 [상호운용 시험 Matrix](docs/INTEROP_TEST_MATRIX.md)를 참고하십시오.
