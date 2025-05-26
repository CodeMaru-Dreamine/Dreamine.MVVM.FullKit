# 0000002_Dreamine_UIWPF_프로젝트구조.md

## 🎯 목적
Dreamine의 UI는 WPF 기반의 MVVM 패턴을 따르며, 명확한 책임 분리를 통해 유지보수성과 확장성을 확보한다.

## 📁 Dreamine.UI.WPF 구조

```
/Dreamine.UI.WPF
├─ App/                   # App.xaml, App.xaml.cs 등 진입점
├─ Views/                 # View (XAML)
│   └─ Shared/            # 공통 View (예: Dialog)
├─ ViewModels/            # ViewModel 클래스들
│   └─ Base/              # ViewModelBase 등 공통 기반
├─ Converters/            # ValueConverters
├─ Navigation/            # View 전환 관련 매니저
├─ Resources/             # XAML ResourceDictionary
├─ Themes/                # 테마 정의 (Dark/Light 등)
├─ Locals/                # 다국어 문자열 리소스
├─ Extensions/            # 확장 메서드
├─ DI/                    # 의존성 주입 구성
├─ Startup/               # 초기화 로직 구성
└─ Dreamine.UI.WPF.csproj
```

## 📌 구성 원칙
- SRP, MVVM, DI, 확장성에 기반한 구성
- Core 로직은 Core에 위치, UI는 ViewModel/View에 집중
- 테마, 리소스, 지역화는 분리된 폴더에서 통합 관리

## 📎 참조 규약 문서
- [[0000000_Dreamine_아키텍처_가이드라인]]

📅 문서 작성일: 2025-04-07  
📁 문서 분류: `ZZX.Document/MD`
⏱️ 총 소요시간: 모름 
🤖 협력자: ChatGPT (GPT-4), 별명: 프레임워크 유혹자  
✍️ 직책: Dreamine Core 설계자 (코드마루 대표 설계자)  
🖋️ 기록자 서명: 아키로그 드림