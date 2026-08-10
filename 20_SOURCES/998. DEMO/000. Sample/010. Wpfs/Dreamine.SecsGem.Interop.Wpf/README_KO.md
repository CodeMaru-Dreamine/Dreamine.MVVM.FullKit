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
