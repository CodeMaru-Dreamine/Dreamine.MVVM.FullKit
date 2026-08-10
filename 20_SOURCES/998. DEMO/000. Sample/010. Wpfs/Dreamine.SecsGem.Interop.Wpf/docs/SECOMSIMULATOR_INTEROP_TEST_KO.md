# SEComSimulator 상호운용 시험

## 범위와 증거 경계

Harness는 .NET 8을 대상으로 하며 Dreamine 공개 API만 사용한다. 설치 제품명과 제공된 설치 정보는 Simulator 4.0을 가리키지만 실행 파일의 File/Product Version 값은 유효하지 않아 Revision을 독립적으로 확인하지 못했다. 벤더 실행 파일, DLL, 메시지·시나리오 파일, 화면, 매뉴얼은 저장소에 포함하지 않았다.

설치 폴더는 읽기 전용으로 조사했다. 구성과 기본 예제에서 Host/Equipment, Active/Passive 필드와 XML 메시지·시나리오 데이터를 확인했으나 공식 매뉴얼은 없었다. 이 정보는 설정 참고일 뿐 Normative 근거가 아니다. 이 문서는 SEMI Revision을 확정하지 않는다.

## 자동 시험 결과

2026-08-10 실행 결과:

- 신규 프로젝트 Release Build: 통과, 경고 0, 오류 0
- 로컬 S1F1/S1F2: 1,000/1,000 통과, timeout 0
- HSMS Linktest: 100/100 통과
- 독립 Connect/Select/Dispose: 100/100 통과
- 동시 Primary 20건 상관관계: 통과
- 기존 SECS/GEM/GEM300 테스트: 203/203 통과
- UI 렌더링: 1366×768, 1920×1080 및 125%·150% 유효 DIP 레이아웃을 확인했다. 수평 스크롤에 의존하지 않으며 좁은 높이에서는 Connection/Advanced가 세로 스크롤된다.
- 외부 Simulator 시험: **Not Run**

로컬 결과만으로 외부 제품 호환성을 주장하지 않는다.

## B 모드 — Simulator Host / Dreamine Equipment

1. Harness 좌측에서 `Role = Equipment`, `Mode = Passive`, `Host / Bind = 127.0.0.1`, `Port = 7000`, 합의한 Session/Device ID(초기값 `0`)를 설정한다.
2. **Advanced Settings**에서 타이머를 확인하고 **Launch Simulator — manual action**을 누른다. 외부 프로세스를 시작하는 Harness 동작은 이 버튼뿐이다.
3. Simulator의 **Configure**(또는 **Configurate**) → **Connection**에서 HSMS, **HOST Mode**, **Active**를 선택한다. **Remote IP = 127.0.0.1**, **Remote Port = 7000**, 동일 Device ID와 T3/T5/T6/T7/T8을 입력한다.
4. Harness의 **Connect**를 먼저 눌러 Passive 수신 대기 상태로 만든다.
5. Simulator의 **Start** 또는 **Connect**를 누르고 `HSMS Connected`, `HSMS SELECTED` 표시를 확인한다.
6. Harness에서 **Enable Equipment Responder**를 누른 뒤 Simulator에서 아래의 확인된 S1 메시지를 송신한다.
7. **Protocol Log**에서 방향, SxFy, System Bytes 상관관계, Item, Raw Hex를 확인하고 **Advanced Settings**에서 마스킹 JSON/Markdown을 내보낸다.

사용자가 3–7단계를 수행하기 전까지 B 모드는 `Waiting for User` / `Not Run`이다.

## C 모드 — Dreamine Host / Simulator Equipment

설치된 Simulator가 Equipment/Local Mode를 실제로 제공할 때만 수행한다.

1. Simulator에서 **HOST Mode**를 해제하고 **Passive**, **Local Port = 7000**, 동일 Device ID와 타이머를 설정한 후 **Start**를 누른다.
2. Harness에서 `Role = Host`, `Mode = Active`, `Host / Bind = 127.0.0.1`, `Port = 7000`, 동일 Session ID를 설정한다.
3. **Connect**, **Select**, **Linktest** 순서로 실행한다.
4. **Messages** 탭에서 확인된 S1 Primary를 보내고 Secondary 상관관계를 비교한다.
5. Equipment/Passive 선택이 없으면 `Not Supported by Tested Simulator Configuration`으로 기록하고 통과로 추정하지 않는다.

C 모드는 현재 **Not Run**이다.

## 확인된 기본 메시지 형식

다음은 설치된 기본 예제와 로컬 E30 자료에서 확인한 범위이며 일반 적합성 주장이 아니다.

| 교환 | Primary Body | 예상 Secondary Body |
|---|---|---|
| S1F13/S1F14 | Host 시작은 빈 List, Equipment 시작은 식별 List | 1-byte COMMACK와 식별 List, 수락 예제 ACK `B[0]` |
| S1F1/S1F2 | Item 없음 | ASCII Model/Software Revision List |
| S1F3/S1F4 | 확인된 Status Variable ID List(예제 I2) | 대응 값 List |
| S1F11/S1F12 | 지원 범위의 빈 List 또는 요청 ID List | ID, 이름, 단위 정의 List |
| S1F15/S1F16 | Item 없음 | 1-byte OFLACK, 수락 예제 `B[0]` |
| S1F17/S1F18 | Item 없음 | 1-byte ONLACK, 수락 예제 `B[0]` |

Simulator 화면에 보인다는 이유만으로 다른 메시지 Body를 만들지 않는다.

## 판정과 제한

예상 TCP/HSMS 상태, SxFy, W-bit, Session ID, System Bytes 상관관계를 증거에서 확인한 경우에만 통과로 판정한다. 예상 동작을 실제 결과로 기록하지 않는다.

- Simulator GUI 자동화는 범위 밖이다.
- 외부 HSMS, S1F13/F14, S1F1/F2, Online/Offline, 재연결 시험은 Not Run이다.
- T3/T6/T7/T8 및 malformed frame은 라이브러리 회귀 테스트로 통과했지만 외부 제품 결과는 Not Run이다.
- 미구현 GEM300 wire handler를 통과로 표시하지 않는다.
- 이번 Harness 작업으로 라이브러리 또는 Communication public API를 변경하지 않았다.
