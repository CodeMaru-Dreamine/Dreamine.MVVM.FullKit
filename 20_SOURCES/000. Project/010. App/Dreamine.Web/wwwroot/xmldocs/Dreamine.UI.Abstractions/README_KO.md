<!--!
\file README_KO.md
\brief Dreamine.UI.Abstractions - Dreamine UI 컴포넌트의 플랫폼 독립적 계약 인터페이스 및 열거형 정의
\author Dreamine Core Team
\date 2026-06-12
\version 1.0.0
-->

# Dreamine.UI.Abstractions

**Dreamine.UI.Abstractions**는 모든 Dreamine UI 패키지가 공유하는 플랫폼 독립적 계약 인터페이스와 열거형을 정의합니다.

WPF 전용 코드를 포함하지 않습니다.  
구체적인 구현은 `Dreamine.UI.Wpf.Equipment` 같은 플랫폼별 패키지에 있습니다.

[➡️ English Documentation](./README.md)

---

## 이 라이브러리가 해결하는 문제

UI 컴포넌트 계약은 렌더링 플랫폼과 독립적으로 정의되어야 합니다.

- WPF 어셈블리 없이도 추상화를 참조할 수 있음
- 비즈니스 레이어가 구체적인 UI 컨트롤 대신 인터페이스에 의존 가능
- 플랫폼별 패키지가 순환 의존 없이 계약을 구현

---

## 주요 기능

- 플랫폼 독립적 팝업 서비스 인터페이스 (`IPopupService`)
- 가상 키보드 레이아웃 및 입력 모드 열거형
- 키보드 지역화를 위한 언어 코드 열거형
- 가상 키보드 Enter 키 프로바이더용 액션 결과 열거형
- WPF 등 UI 프레임워크 의존 없음

---

## 요구 사항

- **대상 프레임워크**: `net8.0-windows`
- 외부 패키지 의존성 없음

---

## 설치

### NuGet

```bash
dotnet add package Dreamine.UI.Abstractions
```

### PackageReference

```xml
<PackageReference Include="Dreamine.UI.Abstractions" />
```

---

## 프로젝트 구조

```text
Dreamine.UI.Abstractions
├── Popup/
│   ├── BlinkPopupOptions.cs       — 깜빡임 팝업 창 옵션
│   └── IPopupService.cs           — 팝업 서비스 계약
└── VirtualKeyboard/
    ├── ActionResult.cs            — Enter 키 프로바이더 결과
    ├── IEnterActionProvider.cs    — Enter 키 액션 프로바이더 계약
    ├── KeyboardInputMode.cs       — 텍스트 / 숫자 / 비밀번호 입력 모드
    ├── LanguageCode.cs            — en_US / ko_KR / zh_CN / vi_VN
    └── VkLayout.cs                — Text / Password / Numeric / Decimal
```

---

## 아키텍처 역할

```text
Dreamine.UI.Abstractions
        │
        ├─ Dreamine.UI.Wpf.Equipment   (팝업 + 키보드 구현)
        ├─ Dreamine.UI.Wpf.Controls    (네비게이션 + 뷰 관리)
        └─ 애플리케이션 코드            (인터페이스만 의존)
```

이 패키지는 UI 스택의 가장 하위 레이어로, 상위 의존성이 없습니다.

---

## 주요 인터페이스

### `IPopupService`

깜빡임 팝업 창의 표시, 닫기, 상태 조회를 위한 플랫폼 중립 계약입니다.

```csharp
IPopupService popupService = DMContainer.Resolve<IPopupService>();

await popupService.ShowBlinkAsync(owner, new BlinkPopupOptions
{
    Message = "작업이 완료되었습니다.",
    OkText  = "확인"
});
```

### `BlinkPopupOptions`

깜빡임 팝업 창의 외형과 동작을 구성합니다.

```csharp
var options = new BlinkPopupOptions
{
    Title           = "경고",
    Message         = "설비 상태를 확인하세요",
    UseBlink        = true,
    BlinkIntervalMs = 400,
    Color1          = Colors.Red,
    Color2          = Colors.DarkRed,
    IsModal         = true
};
```

### `VkLayout`

사용자에게 표시할 가상 키보드 레이아웃을 선택합니다.

| 값        | 설명                   |
|-----------|----------------------|
| `Text`    | 영숫자 풀 키보드         |
| `Password`| 마스킹 텍스트 키보드      |
| `Numeric` | 숫자 패드               |
| `Decimal` | 소수 숫자 입력           |

### `LanguageCode`

가상 키보드의 언어를 선택합니다.

| 값      | 언어   |
|---------|------|
| `en_US` | 영어   |
| `ko_KR` | 한국어 |
| `zh_CN` | 중국어 |
| `vi_VN` | 베트남어|

---

## 설계 노트

이 패키지는 의도적으로 렌더링 프레임워크 의존성 없이 유지됩니다.

- `System.Windows` 타입 없음
- XAML 리소스 참조 없음
- 계약만 포함 — 모든 동작은 구현 패키지에 위치

---

## 라이선스

MIT License
