# BabyCare 메뉴 링크 — 수동 배포 안내

2026-09-24 사용자 요청에 따라 로컬 소스만 수정했습니다. 서버 전송, publish, 운영 재시작은 실행하지 않습니다.

## 이번에 수정한 프로젝트
- WeddingPlatform.Web: Blazor/Pages/Home.razor
- WeddingThankYou: Blazor/Pages/Home.razor
- Families.Web: Blazor/Pages/Home.razor
- Portfolio.Web: Blazor/Layout/PortfolioLayout.razor
- DreamineVMS.Web (CCTV): Blazor/TopNav.razor
- ShopPlatform.Web: Components/Layout/MainLayout.razor
- GamePlatform.Web: Components/Layout/MainLayout.razor

각 메뉴의 Families 다음에 BabyCare → https://babycare.codemaru.co.kr/ 항목 한 개를 추가했습니다. 기존 언어 전달 helper가 있는 서비스에서는 동일 helper를 사용합니다. 공용 헤더의 로그인 전후/모바일 메뉴에도 같은 목록이 적용됩니다.
CodeMaru의 Blazor/Layout/NavMenu.razor에는 이미 BabyCare가 있어 유지했습니다. CodeMaru/CardHybrid는 기존 소스에 링크가 있더라도 운영 반영 여부에 따라 수동 배포가 필요합니다.

## 배포 시 확인
각 프로젝트의 기존 배포 절차로 직접 배포하세요. 운영 설정·DB·키는 유지하세요. 배포 후 데스크톱/모바일 메뉴에서 BabyCare가 한 번 표시되는지, 링크가 BabyCare로 이동하는지 확인하세요. 언어 전달을 사용하는 서비스는 선택 언어도 확인하세요.

로컬 Release 빌드 로그: .artifacts/babycare-menu-links/*.build.log
운영 화면 및 실제 배포 후 이동은 미검증입니다.

## 2026-09-25 CodeMaru 홈페이지 서비스 카드
메뉴와 별도로 홈페이지 서비스 목록에 Games와 BabyCare 카드를 추가했습니다. 각 카드에 이미지, 10개 언어 소개, 해당 서비스로 이동하는 버튼이 있습니다.

- Codemaru/Blazor/Pages/Home.razor
- Codemaru/Services/SiteLocalization.cs
- Codemaru/wwwroot/img/babycare-service.png
- Codemaru/wwwroot/img/games-service.svg

CodeMaru Release 빌드 성공(경고 0, 오류 0). 기존 수동 배포 절차로 정적 이미지까지 함께 배포하세요. CodeMaru는 서버로 전송하거나 운영 적용하지 않았습니다.

검증 결과: 위 7개 변경 프로젝트와 Codemaru를 포함한 8개 프로젝트의 로컬 Release 빌드 모두 성공(각 경고 0, 오류 0).
