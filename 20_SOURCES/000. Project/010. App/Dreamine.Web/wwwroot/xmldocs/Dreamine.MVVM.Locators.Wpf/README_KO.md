# Dreamine.MVVM.Locators.Wpf

Dreamine MVVM Locator 시스템의 WPF 통합 레이어입니다.

이 패키지는 `Dreamine.MVVM.Locators`를 WPF 런타임에 연결하기 위한 기능을 제공합니다.

주요 역할:

- View ↔ ViewModel 자동 바인딩
- ViewModel 기반 View 동적 생성
- Region 기반 ContentControl 네비게이션
- `ContentControl` 전용 네비게이터 제공

무거운 프레임워크 없이 WPF 환경에서 Locator 기반 구성을 적용하려는 경우를 위한 패키지입니다.

[➡️ English Version](./README.md)

---

## 목적

`Dreamine.MVVM.Locators.Wpf`는 다음 요소를 연결하는 WPF 런타임 브리지입니다.

- `Dreamine.MVVM.Locators`
- WPF `FrameworkElement`
- XAML Attached Property
- Region / ContentControl 네비게이션 패턴

즉, WPF 애플리케이션에서 View와 ViewModel을 실제 런타임에 연결하는 역할을 담당합니다.

---

## 주요 구성 요소

### `ViewModelBinder`

WPF AttachedProperty 기반 ViewModel 자동 연결 헬퍼입니다.

역할:

- `AutoWireViewModel` AttachedProperty 제공
- `ViewModelLocator.Resolve(view.GetType())`를 통해 ViewModel 생성
- 생성된 ViewModel을 `DataContext`에 할당
- ViewModel 인스턴스로부터 View를 네이밍 규칙 기반으로 생성

예시:

```xml
<Window
    xmlns:locator="clr-namespace:Dreamine.MVVM.Locators.Wpf"
    locator:ViewModelBinder.AutoWireViewModel="True">
</Window>
```

---

### `RegionBinder`

`ContentControl`을 Region 이름으로 등록하고 탐색하는 AttachedProperty 바인더입니다.

역할:

- Region 이름 기준 `ContentControl` 등록
- Region 키로 탐색 수행
- `ViewModelBinder.ResolveView`를 사용해 View 생성 후 표시
- region 참조를 weak reference 로 유지하고 control 이 unload 되거나 이름이 바뀌면 등록 해제

예시:

```xml
<ContentControl
    local:RegionBinder.RegionName="MainRegion" />
```

```csharp
RegionBinder.Navigate("MainRegion", viewModel);
```

`RegionBinder`는 등록된 `ContentControl`을 강하게 참조하지 않습니다. 따라서 닫힌 Window 또는 unload 된 control 이 static region registry 때문에 계속 살아남는 일을 방지합니다.

---

### `ContentControlNavigator`

`ContentControl` 기반의 단순한 `INavigator` 구현체입니다.

역할:

- ViewModel에 대응하는 View 생성
- 생성한 View를 `ContentControl.Content`에 설정

예시:

```csharp
var navigator = new ContentControlNavigator(MainContentControl);
navigator.Navigate(viewModel);
```

---

### `RegionBinderHelper`

`RegionBinder.RegionName` 기준으로 `ContentControl`을 찾는 VisualTree helper 입니다.

역할:

- WPF root element 하위 visual tree 검색
- 일치하는 region control 반환
- root 가 null 이면 `ArgumentNullException` 발생
- region 이름이 비어 있으면 `ArgumentException` 발생

예시:

```csharp
ContentControl? region = RegionBinderHelper.FindRegionControl(window, "MainRegion");
```

---

## View 해석 규칙

`ViewModelBinder.ResolveView(object viewModel)`는 여러 네이밍 규칙을 순차적으로 시도합니다.

지원되는 대표 규칙:

- `ViewModels` → `Views`
- `ViewModel` → `View`
- `ViewModel` 제거
- `ViewModels` → `Pages`
- `PageModels` → `Pages`
- `PageModel` → `Page`

따라서 WPF 프로젝트마다 조금씩 다른 네이밍 스타일도 수용할 수 있습니다.

---

## 런타임 참고 사항

- WPF helper 는 `FrameworkElement`를 생성하고 갱신하므로 UI thread 에서 호출하는 것을 전제로 합니다.
- 라이브러리 예외 메시지는 로그/콘솔 파싱을 위해 이모지 없는 일반 텍스트를 사용합니다.
- 등록되지 않은 region 탐색은 region 이름을 포함한 `InvalidOperationException`을 발생시킵니다.

---

## 요구사항

- .NET 8.0
- WPF 사용 설정
- `Dreamine.MVVM.Locators` 참조 필요

---

## 설치

```bash
dotnet add package Dreamine.MVVM.Locators.Wpf
```

또는 프로젝트 파일에 직접 추가:

```xml
<PackageReference Include="Dreamine.MVVM.Locators.Wpf" Version="1.0.3" />
```

---

## 아키텍처 위치

이 패키지는 Dreamine MVVM 스택의 WPF 통합 레이어에 해당합니다.

```text
Dreamine.MVVM.Locators
        ↓
Dreamine.MVVM.Locators.Wpf
        ↓
WPF Views / Regions / Navigation
```

---

## 사용 대상

다음 경우에 사용하면 됩니다.

- Dreamine MVVM 기반 WPF 애플리케이션을 만들 때
- XAML에서 ViewModel 자동 바인딩이 필요할 때
- ViewModel로부터 View를 동적으로 생성해야 할 때
- 대형 프레임워크 없이 가벼운 Region / Content 네비게이션이 필요할 때

WPF 외 환경에서는 사용 대상이 아닙니다.

---

## License

MIT License
