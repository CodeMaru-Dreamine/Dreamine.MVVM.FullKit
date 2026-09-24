# GamePlatform.Web

CodeMaru의 공유 로그인을 사용하는 여러 게임용 독립 웹 서비스입니다.

## 서비스 구조

- 운영 주소: `https://games.codemaru.co.kr`
- 기본 로컬 포트: `6080`
- 인증: `Dreamine.Identity` 공유 쿠키 소비자 모드
- 게임 저장: `Dreamine.Database.Abstractions` + `Dreamine.Database.Sqlite`
- 공통 포털 UI: `Dreamine.UI.Blazor`
- 게임 DB: `C:\Codemaru\App_Data\game.db`

Games 서비스는 OAuth 로그인이나 사용자 비밀번호를 직접 관리하지 않습니다. 로그인과 계정 관리는 중앙 `codemaru.co.kr` 포털이 담당하고, Games 서비스는 `.Dreamine.Identity` 쿠키와 공용 DataProtection 키를 읽어 사용자 ID를 확인합니다.

루트(`/`)는 게임 라이브러리이며 각 게임은 독립 경로를 사용합니다. 첫 게임인 마루 원정대는 `/maru-idle`, 플레이 화면은 `/maru-idle/play`입니다. 진행도는 게임별 테이블(현재 `MaruIdlePlayers`)로 분리하며 이후 게임도 같은 방식으로 경로와 저장 모델을 추가합니다.

## 첫 번째 기능 범위

- 로그인 계정별 신규 진행도 생성
- 스테이지 1부터 시작
- 터치·Space·누르고 있는 동안의 수동 연속 공격
- Stage 15 최초 클리어 시 영구 해금되는 1초 AUTO ON/OFF
- 수동·AUTO가 공유하는 서버 데미지 정책과 사용자별 원자적 저장
- 적 격파, 보스 보상, 다음 스테이지, 금화 보상
- 도메인 정책으로 분리된 검 강화
- 1~100 스테이지의 10개 독자 지역, 10스테이지 단위 보스, Stage 100 이후 마지막 지역 순환
- 지역별 WebP 배경, 조명·환경 효과 레이어와 다음 배경 프리로드
- PC 중앙 정렬 및 360px 무가로스크롤 9:16 모바일 화면

기존 초당 방치 정산은 자동공격과 결합하지 않도록 제거했다. 오프라인 보상은 실제 전투 요청을 반복하지 않는 별도 기능으로 남기며, 향후 Stage 30 해금 경계만 `OfflineRewardPolicy`에 정의한다.

정적 월드 데이터는 `Content/regions.json`, 자산 생성 출처와 최적화 절차는 `Assets/README_KO.md`에서 관리한다.

## 운영 설정

`appsettings.json`의 경로는 운영 기본값이며 환경변수로 재정의할 수 있습니다.

- `Game__DatabasePath`
- `Authentication__UsersDbPath`
- `Authentication__DataProtectionKeysPath`
- `Authentication__CookieDomain`

리버스 프록시는 `games.codemaru.co.kr`을 이 서비스의 `6080` 포트로 전달해야 합니다.

## 로컬 플레이 테스트

Development 환경에서 `localhost` 또는 `127.0.0.1`로 실행하면 마루 원정대 소개 화면에 `로컬 테스트 계정으로 시작` 버튼이 표시됩니다. 이 계정과 로그인 엔드포인트는 Development 환경의 로컬 요청에만 열리며 운영 환경에서는 생성되지 않습니다.

## 배포 설정

저장소 기본 설정에는 관리자 이메일을 포함하지 않습니다. 운영 환경에서 `Administration__AllowedEmails__0`, `Game__DatabasePath`, `Authentication__UsersDbPath`, `Authentication__DataProtectionKeysPath`를 별도로 지정하세요. 기본 데이터는 상대 경로 `App_Data`를 사용합니다. 기존 서버의 환경 설정·데이터·키를 덮어쓰지 마세요.
