<!--!
\file README_KO.md
\brief Dreamine.UI.Wpf.Equipment - 설비급 WPF 컴포넌트: 가상 키보드, 깜빡임 팝업, 알람 구성
\author Dreamine Core Team
\date 2026-06-12
\version 1.0.0
-->

# Dreamine.UI.Wpf.Equipment

**Dreamine.UI.Wpf.Equipment**는 산업용·설비급 애플리케이션을 위한 WPF 컴포넌트를 제공합니다.

`Dreamine.UI.Abstractions`에서 정의된 추상화를 구현하며 다음을 제공합니다.

- 완전한 기능의 화면 가상 키보드
- 설정 가능한 깜빡임 팝업 창 시스템
- Dreamine DI 컨테이너에 등록된 팝업 서비스

[➡️ English Documentation](./README.md)

---

## 이 라이브러리가 해결하는 문제

터치 패널이나 잠금 단말기에서 실행되는 산업용 WPF 애플리케이션에는 다음이 필요합니다.

- IME를 후킹하지 않고 WPF 텍스트 입력과 통합되는 화면 키보드
- 운영자의 주의를 끌기 위해 깜빡이는 팝업 창
- 비즈니스 코드가 창 타입을 직접 참조하지 않도록 하는 팝업 서비스 추상화
- 다중 모니터 인식 키보드 위치 계산
- 키보드 닫기 전 유효성 검사를 위한 Enter 키 액션 프로바이더

---

## 주요 기능

- **DreamineVirtualKeyboard** — 언어 전환 기능이 있는 영숫자 + 숫자 + 소수 키보드
- **DreamineVirtualKeyboardWindow** — Win32 모니터 API로 포커스된 입력 요소에 상대적으로 위치하는 플로팅 창
- **DreamineVirtualKeyboardAssist** — 코드 없이 XAML로 키보드를 활성화하는 첨부 프로퍼티 헬퍼
- **DreamineBlinkPopupWindow** — `Alt+F4` / `SC_CLOSE` 차단이 가능한 색상 교대 애니메이션 팝업
- **DreaminePopupService** — `IPopupService` 구현; 비동기 모달 및 비모달 표시
- **KeyboardLayoutSelectorConverter** — 레이아웃에 따라 올바른 키보드 `DataTemplate`을 선택하는 `IValueConverter`

---

## 요구 사항

- **대상 프레임워크**: `net8.0-windows`
- **의존 패키지**:
  - `Dreamine.UI.Abstractions`
  - `Dreamine.UI.Wpf`
  - `Dreamine.UI.Wpf.Controls`
  - `Dreamine.MVVM.ViewModels`
  - `SharpHook` 5.3.8+

---

## 설치

### NuGet

```bash
dotnet add package Dreamine.UI.Wpf.Equipment
```

### PackageReference

```xml
<PackageReference Include="Dreamine.UI.Wpf.Equipment" />
```

---

## 프로젝트 구조

```text
Dreamine.UI.Wpf.Equipment
├── Popup/
│   ├── DreamineBlinkPopupWindow.xaml(.cs)
│   ├── DreamineBlinkPopupWindowViewModel.cs
│   └── DreaminePopupService.cs
└── VirtualKeyboard/
    ├── DreamineEnterActionGroupProvider.cs
    ├── DreamineFullKeyboardLayout.xaml(.cs)
    ├── DreamineNumericKeyboardLayout.xaml(.cs)
    ├── DreamineVirtualKeyboard.cs
    ├── DreamineVirtualKeyboardAssist.cs
    ├── DreamineVirtualKeyboardUI.xaml(.cs)
    ├── DreamineVirtualKeyboardWindow.xaml(.cs)
    ├── DreamineVkbIconAdorner.cs
    ├── Key.cs
    ├── KeyboardLayoutSelectorConverter.cs
    └── ShiftWindowOntoScreenHelper.cs
```

---

## 아키텍처 역할

```text
Dreamine.UI.Abstractions
        │
Dreamine.UI.Wpf.Controls
Dreamine.UI.Wpf
        │
Dreamine.UI.Wpf.Equipment    ← 이 패키지
        │
애플리케이션 코드
```

---

## 빠른 시작

### 가상 키보드 — XAML 첨부 프로퍼티

첨부 프로퍼티를 설정하여 코드 없이 텍스트 입력에 가상 키보드를 활성화합니다.

```xml
xmlns:vk="clr-namespace:Dreamine.UI.Wpf.Equipment.DreamineVirtualKeyboard;assembly=Dreamine.UI.Wpf.Equipment"

<TextBox vk:DreamineVirtualKeyboardAssist.UseVirtualKeyBoard="True"
         vk:DreamineVirtualKeyboardAssist.Layout="Text" />

<TextBox vk:DreamineVirtualKeyboardAssist.UseVirtualKeyBoard="True"
         vk:DreamineVirtualKeyboardAssist.Layout="Numeric"
         vk:DreamineVirtualKeyboardAssist.Minimum="0"
         vk:DreamineVirtualKeyboardAssist.Maximum="9999" />
```

### 깜빡임 팝업 — 기본 사용

```csharp
var svc = DMContainer.Resolve<IPopupService>();

await svc.ShowBlinkAsync(ownerWindow, new BlinkPopupOptions
{
    Title           = "알람",
    Message         = "모터 과부하 감지",
    UseBlink        = true,
    BlinkIntervalMs = 500,
    Color1          = Colors.OrangeRed,
    Color2          = Colors.DarkRed,
    OkText          = "확인",
    IsModal         = true
});
```

### 깜빡임 팝업 — Alt+F4 차단

```csharp
var options = new BlinkPopupOptions
{
    Message     = "운영자가 반드시 확인해야 합니다",
    BlockAltF4  = true,
    OkText      = "확인"
};
```

### Enter 키 유효성 검사 프로바이더

```csharp
public class RangeCheckProvider : IEnterActionProvider
{
    public DependencyObject? PlacementTarget { get; set; }

    public async Task<ActionResult> ExecuteAsync()
    {
        double val = double.Parse(myTextBox.Text);
        if (val < 0 || val > 100)
            return ActionResult.Rejected;
        return ActionResult.Accepted;
    }
}
```

```xml
<TextBox>
    <vk:DreamineVirtualKeyboardAssist.EnterActionProvider>
        <local:RangeCheckProvider />
    </vk:DreamineVirtualKeyboardAssist.EnterActionProvider>
</TextBox>
```

---

## 열거형 참조

| 열거형 | 값 | 사용처 |
|---|---|---|
| `VkLayout` | `Text`, `Password`, `Numeric`, `Decimal` | 가상 키보드 |
| `LanguageCode` | `en_US`, `ko_KR`, `zh_CN`, `vi_VN` | 키보드 언어 |
| `ActionResult` | `Accepted`, `Rejected` | Enter 키 프로바이더 |
| `KeyboardInputMode` | `Text`, `Numeric`, `Password` | 입력 라우팅 |

---

## 설계 노트

- 다중 모니터 위치 계산은 P/Invoke를 통한 Win32 `MonitorFromPoint` / `GetMonitorInfo` 사용 — WinForms 의존 없음
- 가상 키보드 창은 애플리케이션당 싱글톤; `DreamineVirtualKeyboardAssist`가 표시/숨김 생명주기를 관리
- `DreamineBlinkPopupWindow`는 `BlockAltF4 = true`일 때 `HwndSource`를 통한 `WM_SYSCOMMAND / SC_CLOSE` 인터셉션으로 시스템 닫기를 차단
- `DreaminePopupService`는 동기 `ShowBlink`와 선택적 자동 닫힘 타임아웃 및 `CancellationToken`을 지원하는 비동기 `ShowBlinkAsync` 모두 지원

---

## 라이선스

MIT License
