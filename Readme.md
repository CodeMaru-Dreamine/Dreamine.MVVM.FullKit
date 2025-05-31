# 🌟 Dreamine.MVVM.FullKit

> All-in-One Package for WPF MVVM Development  
> Built with Dreamine philosophy: lightweight, declarative, and developer-focused.

---

## ⚠️ 면책 조항 / Disclaimer

> ⚠️ **이 프로젝트는 사내 업무와 무관하게 개인 시간에 개발되었으며, 누구나 자유롭게 사용할 수 있는 오픈소스입니다.**  
> 본 프로젝트는 어떤 기업이나 조직의 사유 자산이 아니며, 개인 개발자의 주도하에 유지보수되고 있습니다.

> ⚠️ **This project is developed entirely during personal time and is publicly available as open-source.**  
> It is not affiliated with or owned by any company or organization, and is maintained independently by the developer.

> 🛡️ 본 프로젝트는 **[연봉/근로계약서]** 기준, 회사 업무 외 시간에 개발되었고  
> 업무 산출물로 간주될 수 있는 조항은 존재하지 않음을 확인하였습니다.

---

## 📦 포함된 주요 모듈

| 모듈명 / Module | 설명 (한국어) | Description (English) |
|----------------|---------------|------------------------|
| [`Dreamine.MVVM.Attributes`](https://github.com/CodeMaru-Dreamine/Dreamine.MVVM.Attributes/blob/main/README.md) | ViewModel/Model 속성 자동화를 위한 특성 제공 | Attributes for automating ViewModel/Model properties |
| [`Dreamine.MVVM.Behaviors`](https://github.com/CodeMaru-Dreamine/Dreamine.MVVM.Behaviors/blob/main/README.md) | MVVM 패턴에서 WPF 이벤트를 명령으로 바인딩 | Bind WPF events to MVVM commands |
| [`Dreamine.MVVM.Behaviors.Core`](https://github.com/CodeMaru-Dreamine/Dreamine.MVVM.Behaviors.Core/blob/main/README.md) | Behavior의 내부 인터페이스 및 추상 구조 정의 | Core abstraction for custom behaviors |
| [`Dreamine.MVVM.Behaviors.Wpf`](https://github.com/CodeMaru-Dreamine/Dreamine.MVVM.Behaviors.Wpf/blob/main/README.md) | WPF 플랫폼에서 동작하는 실제 Behavior 구현 | Concrete WPF-specific behavior implementations |
| [`Dreamine.MVVM.Core`](https://github.com/CodeMaru-Dreamine/Dreamine.MVVM.Core/blob/main/README.md) | MVVM 핵심 로직 및 내부 서비스 제공 | Core logic and internal service infrastructure |
| [`Dreamine.MVVM.Extensions`](https://github.com/CodeMaru-Dreamine/Dreamine.MVVM.Extensions/blob/main/README.md) | 확장 메서드 및 Fluent API 지원 | Extension methods and fluent API utilities |
| [`Dreamine.MVVM.Generators`](https://github.com/CodeMaru-Dreamine/Dreamine.MVVM.Generators/blob/main/README.md) | Source Generator 기반 자동 코드 생성기 | Automatic ViewModel/Model code generators |
| [`Dreamine.MVVM.Interfaces`](https://github.com/CodeMaru-Dreamine/Dreamine.MVVM.Interfaces/blob/main/README.md) | 내부 의존성 분리를 위한 인터페이스 모음 | Interfaces for abstraction and DI |
| [`Dreamine.MVVM.Locators`](https://github.com/CodeMaru-Dreamine/Dreamine.MVVM.Locators/blob/main/README.md) | View ↔ ViewModel 자동 연결 및 매핑 지원 | Auto-mapping between Views and ViewModels |
| [`Dreamine.MVVM.Locators.Wpf`](https://github.com/CodeMaru-Dreamine/Dreamine.MVVM.Locators.Wpf/blob/main/README.md) | WPF 전용 ViewModel 로케이터 구현 | WPF-specific locator implementations |
| [`Dreamine.MVVM.ViewModels`](https://github.com/CodeMaru-Dreamine/Dreamine.MVVM.ViewModels/blob/main/README.md) | 공통 ViewModel 및 샘플 구현 포함 | Base and sample ViewModels for reuse |

---

## 🛠️ 통합 예시

```csharp
[DreamineEntry]
public partial class MainPageViewModel : VsViewModelBase
{
    [DreamineProperty]
    private string _title;

    [RelayCommand]
    private void Save() { }

    [DreamineModel]
    private UserModel _user;
}
```

```xaml
<Button Content="Save"
        local:Click.BehaviorCommand="{Binding SaveCommand}" />
```

---

## 🗂️ 프로젝트 구조 예시

```
- ViewModels/
  - MainPageViewModel.cs
- Models/
  - UserModel.cs
- Views/
  - MainPage.xaml
- App.xaml.cs
```

---

## 📜 라이선스

MIT License  
© 2024–present Jang Min Soo

---

📁 문서 분류: Dreamine FullKit 패키지  
📅 문서 작성일: 2025-05-31  
🤖 협력자: ChatGPT (GPT-4), 별명: 프레임워크 유혹자  
✍️ 직책: Dreamine Core 설계자 (코드마루 대표 설계자)  
🖋️ 기록자 서명: 아키로그 드림
