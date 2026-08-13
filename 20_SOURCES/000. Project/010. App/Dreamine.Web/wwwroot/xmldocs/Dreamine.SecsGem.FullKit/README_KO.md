# Dreamine.SecsGem.FullKit

> Dreamine SECS-II, HSMS, GEM, GEM300 스택을 버전이 정렬된 하나의 패키지 참조로 설치합니다.

`Dreamine.SecsGem.FullKit`은 의존성 전용 메타 패키지입니다. 별도의 런타임 어셈블리를 추가하지 않고 Dreamine SECS/GEM 스택의 공용 계약과 런타임 패키지를 한 번에 참조합니다.

## 포함 패키지

| 패키지 | 역할 |
|---|---|
| `Dreamine.Secs.Abstractions` | SECS-II/HSMS 계약, 메시지, 아이템 및 Provider 경계 |
| `Dreamine.Secs.Com` | SECS-II 코덱 및 HSMS 통신 런타임 |
| `Dreamine.Gem.Abstractions` | GEM 기능 계약 |
| `Dreamine.Gem` | GEM 동작 및 장비 서비스 런타임 |
| `Dreamine.Gem300.Abstractions` | GEM300 기능 계약 |
| `Dreamine.Gem300` | GEM300 기능 런타임 |

## 빠른 시작

깨끗한 .NET 8 프로젝트에 메타 패키지를 설치합니다.

```bash
dotnet add package Dreamine.SecsGem.FullKit --version 1.0.0
dotnet restore
dotnet build -c Release
```

HSMS에는 `Dreamine.Secs.Com.Hsms.HsmsSession`을 사용합니다. 애플리케이션에 필요한 기능만 `Dreamine.Gem.GemRuntime` 및 `Dreamine.Gem300.Gem300Runtime`으로 조립하십시오. 메타 패키지는 의존성 버전을 정렬하지만 Endpoint, Responder 또는 Workflow를 자동으로 구성하지 않습니다.

## 범위와 검증

이 패키지는 구현 도구이며 적합성 인증서가 아닙니다. 실제 장비 연동에 적용되는 표준과 요구사항에 따라 선택한 구성 및 시나리오를 별도로 검증해야 합니다.

## 알려진 제한

- 의존성 전용 메타 패키지이므로 런타임 어셈블리를 포함하지 않습니다.
- 구성 패키지의 도메인 서비스가 완전한 SECS/GEM/GEM300 Wire Mapping을 의미하지는 않습니다.
- 패키지 설치와 자체 Loopback 테스트만으로 외부 Simulator 상호운용성 또는 최신 표준 개정판 적합성이 입증되지는 않습니다.
- NuGet 인덱싱에는 시간이 걸릴 수 있습니다. 릴리스 직후에는 모든 구성 패키지가 표시된 다음 Restore를 다시 시도해야 할 수 있습니다.

## 링크

- 웹사이트: <https://dreamine.kr>
- GitHub: <https://github.com/CodeMaru-Dreamine>
- NuGet: <https://www.nuget.org/packages/Dreamine.SecsGem.FullKit>

## 라이선스

MIT
