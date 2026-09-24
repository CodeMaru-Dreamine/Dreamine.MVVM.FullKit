# 아기하루 · BabyCare.Web

수유(분유·모유 직수·유축 모유), 이유식, 트림, 수면, 기저귀, 체온과 유축을 가족끼리 기록하는 모바일 우선 Blazor Server 앱입니다. 사진·생년월일 대시보드·접이식 예제 및 10개 언어를 지원합니다.

[![CI](https://github.com/CodeMaru-Dreamine/Dreamine.MVVM.FullKit/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/CodeMaru-Dreamine/Dreamine.MVVM.FullKit/actions/workflows/ci.yml)

## 실행

솔루션의 `000. Project > 010. App > BabyCare.Web`을 시작 프로젝트로 선택합니다.

```powershell
dotnet run --project "20_SOURCES/000. Project/010. App/BabyCare.Web/BabyCare.Web.csproj" --launch-profile BabyCare
```

로컬 주소는 `http://localhost:5188`입니다. 기본 설정에서는 CodeMaru 중앙 Identity 포털로 로그인합니다. 가족 공간을 만들고, 공간 소유자가 발급한 24시간·1회용 초대 코드로 다른 보호자가 참여합니다. MVP는 가족 공간 하나에 아기 한 명입니다. 여러 아기는 공간을 각각 만들 수 있습니다.

## 재사용한 라이브러리

- `Dreamine.Identity`: 사용자 계정, 로그인/회원가입, 인증 쿠키, Data Protection.
- `Dreamine.Database.Sqlite` → `Database.Core` / `Database.Abstractions`: DB 생성 및 매개변수 SQL 실행·조회. 별도 SQLite 접근 계층을 다시 만들지 않습니다.
- `Dreamine.UI.Blazor`: 삭제 확인 다이얼로그와 접기/펼치기 패널.

가족 구성원 권한과 육아 기록은 앱 도메인에 둡니다. Identity 라이브러리가 서비스별 테넌트 정책을 담당하지 않는 기존 경계를 따릅니다. 기존 공통 라이브러리 소스는 변경하지 않습니다.

## 사용 흐름

1. 마이크를 누르고 말하거나 텍스트를 입력합니다. 브라우저 음성 인식이 최종 텍스트를 반환하면 AI 해석이 시작됩니다.
2. Codex가 새 기록/기존 기록 수정/확인 질문 중 하나를 반환합니다.
3. 초안에서 종류, 시작·종료, 수유량, 기저귀 종류, 메모를 직접 수정한 뒤 **확인하고 저장**합니다.
4. 기존 기록의 수정·삭제, 진행 중인 수유/트림시키기/수면의 종료도 가능합니다.
5. 다른 보호자의 변경은 새로고침 버튼으로 불러옵니다. 동시 수정은 버전 검사로 덮어쓰기를 막습니다.

한 번에 한 기록을 처리합니다. 불분명한 오전/오후나 여러 기록을 한 문장에 담으면 AI가 확인 질문을 하도록 설계했습니다. 질문 아래에 답변을 입력하거나 다시 마이크로 답할 수 있습니다. **AI는 저장하지 않으며**, 서버 검증과 사용자 저장 동작을 거칩니다.

브라우저 내장 음성 인식의 지원·품질은 기기에 따라 다릅니다. HTTPS 및 마이크 권한이 필요하며, 미지원 시 텍스트 또는 스마트폰 키보드 음성 입력을 사용합니다. Codex는 이 MVP에서 **음성 전사 대신 전사된 텍스트 해석**을 담당합니다. 원본 음성을 앱 서버에 저장하지 않습니다. 브라우저의 음성 인식 서비스가 음성을 처리하고, Codex 공급자가 텍스트 및 해당 가족의 최근 기록을 처리합니다.

## 서버 Codex 연결

서비스를 실행하는 **동일한 OS 계정**에서 Codex CLI가 로그인되어 있어야 합니다. Windows에서 npm의 `codex.cmd`가 아닌 실제 `codex.exe` 경로를 지정하세요.

```powershell
$env:Codex__Executable = 'C:\Tools\Codex\codex.exe' # 서버에 설치된 실제 경로
$env:Codex__Home = 'C:\Services\BabyCare\CodexHome' # 전용 로그인 프로필, 이 경로에서 로그인 필요
$env:Codex__TimeoutSeconds = '60'
$env:BabyCare__DataPath = 'C:\Services\BabyCare\App_Data'
```

`Codex:Enabled` 기본값은 true입니다. AI를 사용하지 않는 테스트에서는 `Codex__Enabled=false`로 설정할 수 있습니다. AI 실패/시간 초과에도 직접 기록과 기존 기록 수정은 계속 가능합니다.

호출 방식은 `codex exec --ignore-user-config --ephemeral --skip-git-repo-check --sandbox read-only --disable shell_tool --disable unified_exec` + `web_search="disabled"`, `approval_policy="never"`, `--output-schema`, `-o`, 표준입력입니다. 개인 설정/MCP 구성을 불러오지 않고 임시 작업 디렉터리를 사용합니다. 웹앱 환경 변수의 비밀값은 자식 프로세스에 전달하지 않습니다. 작업 디렉터리와 결과 파일은 호출 후 정리합니다. 요청마다 새 실행이며 `resume --last` 등 가족 간 공유 대화는 사용하지 않습니다. 실행은 전역 1개로 제한하고 혼잡 시 재시도 메시지를 표시합니다.

사용하는 CLI 버전은 위 플래그를 지원해야 합니다. 설치되었다는 사실만으로 서비스 계정의 인증과 사용 한도가 보장되지는 않으므로, 배포 계정에서 실제 입력을 한 번 검증하세요. 전용 저권한 서비스 계정/프로필을 권장합니다.

공식 참고: [Codex 비대화형 실행](https://learn.chatgpt.com/docs/non-interactive-mode).

## 기존 서비스와 로그인 공유

기본 설정은 CodeMaru 중앙 통합로그인을 사용합니다. BabyCare에 별도 Google/Naver/Kakao OAuth 자격증명을 등록하지 않습니다. 기존 서버와 다음 값을 맞춥니다.

- `Authentication__CookieName` / `Authentication__CookieDomain`
- `Authentication__DataProtectionApplicationName` / `Authentication__DataProtectionKeysPath`
- `Authentication__UsersDbPath`
- 중앙 포털 사용 시 `Authentication__UseCentralPortal=true`

`Authentication__DataProtectionKeysPath`가 없으면 통합로그인 모드의 시작을 중단합니다. 기존 키를 새로 생성하거나 덮어쓰지 말고 기존 CodeMaru 키 경로와 실행 계정의 읽기 권한을 확인하세요. `/auth/login`, 예전 `/_identity/login` 및 `/signin/{provider}` 주소는 중앙 포털로 이동하고, 로그인 후 `BabyCare:PublicUrl`(기본 `https://babycare.codemaru.co.kr/`)로 돌아옵니다. 격리 검증에서만 `Authentication__UseCentralPortal=false`와 별도 쿠키/키/DB 설정을 사용하세요. `Start-BabyCareValidation.ps1`은 이 격리 설정을 명시합니다.

공유 사용자 DB 및 키는 기존 서비스와 같은 영구 경로를 지정해야 합니다. 앱 데이터 `babycare.db`는 사용자 DB와 별도로 유지합니다. 프록시 뒤에서 운영할 때는 HTTPS와 실제 호스트를 `AllowedHosts`에 설정하고, 신뢰하는 프록시에 한해서 forwarded headers를 구성해야 합니다. 앱은 기본적으로 전달 헤더를 전역 신뢰하지 않습니다.

## 명령 확장

`Content/care-kinds.json`이 수동 입력 버튼, 종류 선택, 기본 필드 검증, AI 어휘/예시의 공통 카탈로그입니다.

- 기존 명령 표현 추가: 해당 종류의 `examples`에 문장을 추가하고 앱을 재시작합니다.
- 종류 추가: 고유 `id`, `label`, `icon`, `hasAmount`, `hasDuration`, `examples`를 가진 항목을 추가합니다. 같은 양/시간/메모 필드를 쓰는 종류는 화면·저장소 수정 없이 동작합니다.
- 새 전용 필드/동작: 모델, 검증, 편집기, `care-command.schema.json`, 테스트를 함께 확장합니다.
- 기존 기록이 참조하는 `id`는 삭제/변경하지 마세요. 별도 마이그레이션이 필요합니다.

예: 목욕은 `hasAmount=false`, `hasDuration=true`로 추가할 수 있습니다. AI 공급자 변경은 `ICareInterpreter` 구현체를 교체하면 됩니다.

## 저장·테넌트 정책

모든 조회·저장·수정·삭제는 로그인 사용자와 가족 구성원 여부를 서버에서 검사합니다. 테넌트 ID는 AI에서 받지 않습니다. AI 수정 대상은 서버가 해당 가족에서 선택한 최근 기록/진행 중 기록으로 제한합니다.

가족별 JSON aggregate를 SQLite에 저장하며 조건부 UPDATE의 revision으로 가족 단위 변경 충돌을 방지합니다. 기록별 version으로 오래된 편집/삭제도 차단합니다. 수정자·시각·변경 전 값은 감사 이력에 보관합니다. 초대 코드는 해시만 저장하며 새 발급 시 이전 코드가 무효화됩니다.

단일 서버·소수 가족을 위한 MVP입니다. 장기 운영에서 기록/감사 이력이 커지면 정규화된 기록 테이블과 페이지 조회로 이전하는 것이 다음 단계입니다. 현재 가족 구성원 제거·관리자 역할 세분화, 자동 실시간 동기화, 오프라인 저장은 포함하지 않습니다. 서비스 중에는 DB·Identity 키를 함께 영구 보관하고 백업하세요.

시각 입력/표시는 현재 기기의 UTC 오프셋을 사용합니다. 요약은 선택한 날짜에 **시작한** 기록을 기준으로 하며, 수면은 종료된 기록만 합산합니다. 여행/서머타임을 포함한 IANA 시간대별 과거 시각 처리는 후속 범위입니다.

## 검증

```powershell
dotnet test "20_SOURCES/200. Tests/BabyCare.Web.Tests/BabyCare.Web.Tests.csproj" -f net8.0 -p:TargetFrameworks=net8.0
```

테스트는 테넌트 접근 차단, 1회용 초대, 재발급, 다중 저장소 동시 수정, 삭제 후 오래된 수정 차단, 검증, AI 대상 제한, 확인 질문, 카탈로그 확장을 포함합니다. 라이브 Codex 및 실제 마이크는 테스트 더블로 대체하지 않은 별도 연동 점검 대상입니다.

일부 SDK 환경에서 최초 restore 직후 xUnit 어댑터가 발견되지 않으면 같은 명령을 다시 빌드하거나 설치된 `xunit.runner.visualstudio`의 `build/net8.0`을 `--test-adapter-path`로 지정하세요.

## 사진과 새 기록

대표 사진 1장, 기록당 최대 3장(장당 8MB, JPEG/PNG/WebP)을 지원합니다. 사진은 가족 권한 검사 후 제공되며 `BabyCare:DataPath/photos`에 저장합니다. DB·사진·키를 함께 백업하세요. 원본 사진 메타데이터는 유지되며 첨부 해제는 물리 파일 삭제가 아닙니다.

직수는 좌/우/양쪽과 시간을 기록하고 mL로 환산하지 않습니다. 이유식은 선택적인 g 및 음식·재료 메모로 남깁니다. 유축과 이유식은 아기 우유 섭취량 합계와 별도입니다. 실제 Codex 연결 검증은 배포 환경에서 수행하며 CI 테스트에 외부 계정은 필요하지 않습니다.
