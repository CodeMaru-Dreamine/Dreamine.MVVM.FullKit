# SEComSimulator 상호운용 시험

## 범위와 현재 상태

| Evidence surface | 상태 | 경계 |
|---|---|---|
| 외부 SEComSimulator 상호운용 | `NOT_RUN` | 이번 실행에서 Dreamine/상대편 Evidence Pair를 Review하지 않았습니다. |
| WPF 화면 검증 | `BLOCKED_ENVIRONMENT` | Computer-use가 app approval/elicitations 부재로 애플리케이션을 실행하지 못했습니다. |
| 고정 E30 Demo Responder Surface | `IMPLEMENTED_UNVERIFIED` | `E30-0611 derived subset profile v1`; 구현 범위가 고정돼 있으며 적합성 Evidence가 아닙니다. |
| 고정 E30 별도 Process TCP Evidence | `PASS` | 공개 Host/Equipment Process가 실제 로컬 고정 Dialogue 실행을 완료했습니다. |
| E37.1 적합성 | `BLOCKED_STANDARD` | 필요한 라이선스 Revision을 사용할 수 없습니다. |

설치 제품명과 제공된 설치 정보는 Simulator 4.0을 가리키지만 실행 파일에서 유효한 File/Product Version을 확인하지 못했습니다. Vendor 실행 파일, DLL, Message/Scenario 파일, Screenshot, Manual은 저장소에 복사하지 않았습니다. Host/Equipment 및 Active/Passive 필드를 읽기 전용으로 관찰한 결과는 설정 참고일 뿐 Normative Evidence가 아닙니다.

UI의 `WaitingForUser`, `Passed` 값은 운영용 `InteropScenarioStatus` enum member입니다. 위 Evidence Status를 대체하지 않습니다. Root 실행은 자동 작업이 끝난 뒤 화면과 Simulator 수동 동작을 한 번에 요청합니다.

## 외부 실행 전 확인

1. 별도 환경 소유자가 다른 Endpoint를 승인하지 않았다면 Loopback Endpoint와 합의한 Session/Device ID만 사용합니다.
2. Responder를 활성화하기 전에 정확한 Profile을 선택합니다. 기본값은 **E30-0611 derived subset profile v1 (Demo)**입니다. 대안 **Educational basic responder (Demo-only, not GEM)**은 GEM이 아닙니다.
3. 별도로 승인한 S/F별 Full-body Policy가 없다면 지속형 Logging은 `HeaderOnly`를 유지합니다. 고객/Private-sidecar Data를 저장하지 않습니다.
4. Evidence Manifest/Checklist와 Product 소유 Output Folder를 준비합니다. 정상 Dreamine Log와 상대편 Log 또는 Screenshot이 모두 필요하며 단방향 Evidence로 외부 `PASS`를 만들 수 없습니다.
5. 설치된 Simulator가 실제로 지원한다고 확인된 Message Row만 실행합니다. Menu Entry만으로 호환 Body Shape를 증명할 수 없습니다.

## B 모드 — Simulator Host Active / Dreamine Equipment Passive

1. Workbench에서 `Role = Equipment`, `Mode = Passive`, `Host / Bind = 127.0.0.1`, `Port = 7000`, 합의한 Session/Device ID를 설정합니다.
2. Responder가 꺼진 상태에서 Profile을 선택한 뒤 **Enable Equipment Responder**를 누릅니다. 활성 중에는 선택기가 잠기며 Reconnect 후 교체 Session에 Binding이 복원됩니다.
3. **Advanced Settings**에서 Timer를 확인하고 필요하면 **Launch Simulator — manual action**을 사용합니다. 외부 Process를 시작하는 Workbench 동작은 이 버튼뿐입니다.
4. Simulator의 **Configure**(또는 **Configurate**) → **Connection**에서 HSMS, **HOST Mode**, **Active**를 선택합니다. `Remote IP = 127.0.0.1`, `Remote Port = 7000`, 동일 Device ID와 합의한 T3/T5/T6/T7/T8을 입력합니다.
5. Workbench의 **Connect**를 먼저 누릅니다. Passive Listening/Disconnected-not-selected 자체는 실패가 아닙니다.
6. Simulator의 **Start** 또는 **Connect**를 누르고 양쪽 애플리케이션에서 TCP Connected와 HSMS Selected를 확인합니다.
7. 선택 Profile에서 Simulator가 지원하는 Primary만 전송합니다. 양쪽에서 Direction, SxFy, W-bit, Session ID, System Bytes Correlation, Typed Body/ACK를 확인합니다.
8. Disconnect/Reconnect를 수행하고 Responder가 한 번만 다시 Bind됐는지 확인한 뒤 Correlated Exchange를 반복합니다.
9. Exact-wire Recorder를 finalize하기 전에 Session을 Disconnect/Stop합니다. Observation Drop 0, Recorder Drop 0, Flush 완료, Writer Failure 없음인지 확인합니다.
10. Finalize된 Dreamine/상대편 Artifact를 Hash하고 Checklist를 완료한 뒤 Manual Review합니다. 모든 단계에 Evidence가 생기기 전까지 B 모드 행은 `NOT_RUN`입니다.

## C 모드 — Dreamine Host Active / Simulator Equipment Passive

설치된 Simulator가 Equipment/Local Mode를 제공할 때만 진행합니다.

1. Simulator에서 **HOST Mode**를 해제하고 **Passive**, `Local Port = 7000`, 합의한 Device ID와 Timer를 설정한 후 **Start**를 누릅니다.
2. Workbench에서 `Role = Host`, `Mode = Active`, `Host / Bind = 127.0.0.1`, `Port = 7000`, 동일 Session ID를 설정합니다.
3. **Connect**, **Select**, **Linktest** 순서로 실행합니다.
4. 제한된 Message Template Catalog v1/Scenario v1을 로드하거나 Structured Editor에서 확인된 Message Shape만 사용합니다.
5. B 모드와 동일하게 양쪽 Correlation과 Evidence-health 조건을 검증합니다.
6. Equipment/Passive 선택이 없으면 관찰 결과만 보존하고 C 모드는 `NOT_RUN`으로 둡니다. 호환성을 추론하지 않습니다.

C 모드는 `NOT_RUN`입니다.

## 고정 E30 Demo Dialogue 경계

정확한 Profile 이름은 `E30-0611 derived subset profile v1`입니다. 공개 Demo Profile은 다음 20개 Dialogue Definition을 고정합니다.

| Direction family | 포함 Dialogue |
|---|---|
| Host-request/Equipment-response | S1F1/F2, S1F3/F4, S1F11/F12, S1F13/F14, S1F15/F16, S1F17/F18; S2F13/F14, S2F15/F16, S2F17/F18, S2F29/F30, S2F31/F32, S2F33/F34, S2F35/F36, S2F37/F38, S2F41/F42; S5F3/F4, S5F5/F6; S6F15/F16 |
| Equipment-primary/Host-response | S5F1/F2, S6F11/F12 |

Public Host Client가 해당 Direction을 명시적으로 등록한 일부 Communication/Time Exchange는 Equipment가 시작할 수도 있습니다. Direction은 반드시 관찰하며 추론하지 않습니다. S2F35 Empty-list Unlink/Delete Variant와 S6F19/F20은 `BLOCKED_STANDARD`이고, Multi-block, Trace, Limit, Spooling 및 고정 Profile 밖 Capability Family는 `INTENTIONALLY_EXCLUDED`입니다.

교육용 Fallback은 S1F1/F2, S1F3/F4, S1F11/F12, S1F13/F14, S1F15/F16, S1F17/F18, S2F17/F18만 포함합니다. Demo-only이며 GEM으로 보고하면 안 됩니다.

## 외부 `PASS` 기준

다음 조건을 모두 만족한 행만 `PASS`가 될 수 있습니다.

- 예상 TCP/HSMS State와 Reconnect 동작이 보입니다.
- Direction, SxFy, W-bit, Session ID, System Bytes Correlation이 일치합니다.
- Body와 Typed ACK가 선택 Profile에 맞고 Reject 시 State를 변경하지 않습니다.
- Exact-wire Health Gate가 Evidence 대상입니다.
- Dreamine/상대편 Artifact가 Finalize·Hash되고 Manual Review됐습니다.
- Profile 범위를 넓히지 않은 결과가 중앙 보고서에 기록됐습니다.

Timeout, Malformed Body, 예상 밖 ACK, Disconnect, 미지원 설정, Observation Drop, 불완전 Flush는 Evidence로 보존하며 `PASS`가 아닙니다. 로컬 Loopback, 별도 Process QuickStart 또는 UI Workflow 값은 외부 행을 승격할 수 없습니다.

## Historical Evidence — 2026-08-10 only

과거 문서에는 2026-08-10 로컬 합성 Loopback Count와 UIAutomation/PrintWindow Fallback Capture가 기록돼 있었습니다. 해당 항목은 날짜가 고정된 과거 비교 Evidence이며 현재 화면 또는 외부 Evidence가 아닙니다. 당시 외부 Simulator 상태도 `NOT_RUN`이었습니다.
