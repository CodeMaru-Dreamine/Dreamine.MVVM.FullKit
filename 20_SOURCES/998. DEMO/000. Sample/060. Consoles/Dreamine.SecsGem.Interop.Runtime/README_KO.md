# Dreamine SECS/GEM Interop Runtime

`Dreamine.SecsGem.Interop.Runtime`은 공개 콘솔 샘플과 WPF Workbench가 함께 사용하는 provider-neutral 재사용 계층입니다. 크기·버전이 제한된 연결 Profile, 메시지 Template Catalog, Scenario 실행, 설정형 Primary Responder, Evidence Manifest, 지속형 exact-wire 로그를 제공합니다.

## Capability 경계

- Connection Profile v1은 credential이 없는 endpoint, role, mode, live 변경이 불가능한 session 설정, timer, safety limit, reconnect policy 및 등록된 log-policy ID를 검증합니다. Session 생성 설정은 live session을 바꾸지 않고 validate, diff, stop, recreate 순서로 적용합니다.
- Message Template Catalog v1은 제한된 불변 SECS Item Tree, direction, Primary/Secondary 역할, W-bit 및 body-log policy를 저장합니다. Catalog entry는 application data이며 Normative SxFy 정의가 아닙니다.
- Scenario v1은 제한된 run/step deadline, cancellation, repeat 제한, state wait, send, expect 및 structured exit status를 제공합니다. Inbound message가 drop되면 run을 성공 evidence로 사용할 수 없습니다.
- Configurable Responder v1은 exact dispatcher registration을 소유하며 즉시 응답, 지연 응답, 의도적 no-reply 및 bounded shutdown을 지원합니다.
- Evidence 및 JSON persistence는 명시적 schema version, input limit, stable snapshot 및 atomic file replacement 경계를 사용합니다.
- 지속형 wire logging은 session의 실제 wire-observation stream을 소비하며 경쟁 receive loop를 만들거나 canonical 재인코딩을 captured wire로 표시하지 않습니다.

이 package는 `ISecsMessageSession`과 Dispatcher를 소비합니다. 별도 HSMS Session, Transaction Manager, System Bytes Generator 또는 Receive Loop를 만들지 않습니다. Session 소유권은 application에 남습니다.

## Package 및 source-build 경계

이 project는 `Dreamine.SecsGem.Interop.Runtime` package candidate입니다. 서로 일치하는 `Dreamine.Communication.Abstractions`, `Dreamine.Secs.Abstractions`, `Dreamine.Secs.Com`, `Dreamine.Gem.Abstractions`, `Dreamine.Gem` candidate와 함께 사용합니다. Isolated local feed에는 `Dreamine.Secs.Com`이 transitively 선언한 Communication dependency도 있어야 합니다.

Canonical full workspace 안에서는 공개 Host/Equipment sample project가 이 source project를 의도적으로 `ProjectReference`합니다. 별도 application은 대신 `PackageReference`를 사용합니다. 같은 version을 가진 과거 cache binary와 새 Runtime 또는 SECS/GEM `1.0.0` candidate를 혼합하지 마십시오. Package 검증은 local-only feed와 isolated package cache를 사용합니다. 이 검증 과정은 package를 게시하지 않습니다.

소스 예시는 [공개 sample fixture](../../../../100.%20Library/Secs.Com/samples/fixtures/README.md)와 [WPF Workbench](../../010.%20Wpfs/Dreamine.SecsGem.Interop.Wpf/README.md)를 참고하십시오.

## 안전한 지속형 로그

Wire capture는 opt-in이며 민감한 application data를 포함할 수 있습니다. 안전한 기본값은 `HeaderOnly`이고 `Excluded`는 body나 raw frame을 보존하지 않습니다. 안전한 sample facade는 global `FullBodyExplicit` 선택을 거부합니다. Full-body 저장에는 별도로 승인한 S/F별 규칙, 제한된 retention 및 application 소유 저장 위치가 필요합니다.

`InteropWireLogSessionOptions`을 만들고 underlying session을 생성할 때 `CreateObservationOptions()`을 적용한 다음 session이 만들어진 후 `InteropWireLogSession`을 시작합니다. Terminal producer와 queue completion이 경쟁하지 않도록 application이 session을 먼저 Stop 또는 Dispose한 다음 recorder를 종료해야 합니다. `StopAsync`는 idempotent하며 bounded JSONL sink를 finalize합니다. 두 drop counter가 0이고 flush가 완료됐으며 writer failure가 없음을 뜻하는 `Health.IsEvidenceEligible`이 true일 때만 run을 evidence로 취급하십시오.

안전한 facade는 endpoint를 `redacted`로 저장하고 동적 diagnostic text를 저장하지 않습니다. Header field, timestamp, SxFy, Session ID, System Bytes, local segment path 및 운영 timing도 민감할 수 있습니다. Product-owned log root를 보호하고 Simulator, 고객 또는 private-sidecar artifact를 public package나 repository에 복사하지 마십시오.

## Evidence 상태

이 README는 코드 존재를 시험 결과로 승격하지 않습니다. Fresh build, test, pack 및 isolated-consumer evidence가 중앙 검증 보고서에 기록되기 전 package candidate는 `IMPLEMENTED_UNVERIFIED`입니다. E37.1 적합성 주장은 `BLOCKED_STANDARD`, 외부 Simulator 및 현장 검증은 `NOT_RUN`, Legacy SECS-I는 `INTENTIONALLY_EXCLUDED`입니다. 로컬 loopback이나 건강한 JSONL 파일은 인증, 최신 Revision 적합성, 외부 상호운용 또는 현장 검증이 아닙니다.

호환성, 소유권, persistence 및 package pairing 결정은 [API_REVIEW.md](docs/API_REVIEW.md)를 참고하십시오. 최종 exported-member inventory는 Release assembly에서 별도로 생성하여 `docs/PUBLIC_API.md`에 기록합니다.
