# Dreamine SECS/GEM 상호운용 Harness

이 .NET 8 WPF 도구는 Dreamine의 공개 SECS-II, HSMS 및 기본 GEM API를 검증한다. TCP/HSMS 상태, 구조화 Item 편집기, 시나리오 결과, wire 지향 로그, 마스킹 결과 내보내기와 로컬 자체 루프백을 제공한다.

이 도구는 상호운용 증거 수집용이며 적합성 인증서가 아니다. 외부 Simulator 시나리오는 사용자가 문서의 절차를 완료하기 전까지 `Not Run` 또는 `Waiting for User`이다.

실행 및 수동 시험 절차는 [docs/SECOMSIMULATOR_INTEROP_TEST_KO.md](docs/SECOMSIMULATOR_INTEROP_TEST_KO.md), 결과 상태는 [docs/INTEROP_TEST_MATRIX.md](docs/INTEROP_TEST_MATRIX.md)를 참고한다.

## Multi-Equipment Host

구성된 endpoint는 **Multi Equipment** 탭에서 관리한다. Release 출력 폴더에서 1/2/10/50대 격리 로컬 loopback을 Headless로 실행할 수 있다.

```powershell
Dreamine.SecsGem.Interop.Wpf.exe --multi-self-test --output multi-equipment-self-test.json
```

구조와 사용법은 [Multi-Equipment Host 한국어 문서](docs/MULTI_EQUIPMENT_HOST_KO.md), 검증 범위는 [시험 Matrix](docs/MULTI_EQUIPMENT_TEST_MATRIX.md), 계측값은 [성능 Snapshot](docs/MULTI_EQUIPMENT_PERFORMANCE.md)을 참고한다. 외부 Simulator 결과는 사용자가 직접 기록하기 전까지 `Not Run / Waiting for User`이다.

## 기본 Responder 확장 예제

`Managers/MessageManager.cs`에는 의도적으로 작게 유지한 내장 Equipment Responder가 있다. 다음 구현 방법을 한·영 주석으로 보여준다.

- `SecsListItem.Items`와 `Values.Span`으로 요청의 형식 지정 자식 Item 읽기
- 임시 `List<SecsItem>`을 이용한 동적 평면 `L[n]` 생성
- 부모 `SecsListItem`에 자식 `SecsListItem`을 전달하는 중첩 목록 생성
- 1바이트 Binary ACK 반환
- 불필요한 바깥 List 없이 ASCII 단일 값 반환

`SecsListItem`은 불변이므로 동적 자식은 임시 컬렉션에 넣고 `children.ToArray()`를 생성자에 전달한다. 교육용 메시지 쌍을 추가할 때는 `BuildBasicResponseItem`에 `(Stream, Function)` tuple을 하나 추가한다. S1F1/S1F2와 S1F13/S1F14는 `GemProtocolEngine`이 먼저 처리하므로 fallback switch에 중복 구현하지 않는다.

이 Handler들은 작성법을 보여주는 예제이며 Normative 메시지 정의나 운영 Equipment 동작이 아니다. 상태 규칙, 데이터 식별자, 명령 의미 등 Application 전용 동작은 별도 Equipment Profile에 둔다. 또한 내장 Responder와 외부 sidecar Responder는 서로 다른 경로이므로 sidecar 트래픽은 `MessageManager`를 통과하지 않는다.
