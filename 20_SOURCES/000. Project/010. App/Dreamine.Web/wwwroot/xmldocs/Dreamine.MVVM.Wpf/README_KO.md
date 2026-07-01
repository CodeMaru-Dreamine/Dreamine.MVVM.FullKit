# Dreamine.MVVM.Wpf

Dreamine MVVM 프레임워크를 위한 WPF 전용 부트스트랩 및 런타임 통합 레이어입니다.

[➡️ English Version](README.md)

## 개요

`Dreamine.MVVM.Wpf` 는 Dreamine MVVM 애플리케이션에서 필요한 WPF 전용 시작 처리와 런타임 연결 로직을 담당합니다.

이 패키지의 책임은 다음과 같습니다.

- View ↔ ViewModel 자동 매핑 등록
- `DMContainer` 기반 자동 DI 등록
- View 로드 시 `DataContext` 자동 연결
- 현재 로드된 Window의 Region 기반 navigator 선택적 등록
- 플랫폼 중립 라이브러리와 WPF 런타임 책임 분리

## 왜 이 패키지가 필요한가

`Dreamine.MVVM.Core` 는 가능한 한 플랫폼 중립적으로 유지하는 것이 맞습니다.

하지만 `FrameworkElement.Loaded`, `EventManager`, View 런타임 바인딩 같은 요소는 명백한 WPF 전용 책임입니다.
이 책임들을 Core에 두면 계층이 흐려지고 대상 프레임워크도 WPF에 묶입니다.

그래서 WPF 전용 책임은 `Dreamine.MVVM.Wpf` 로 분리하는 구조가 맞습니다.

## 주요 타입

### `DreamineAppBuilder`

`DreamineAppBuilder` 는 WPF 애플리케이션 시작 시 Dreamine MVVM 런타임을 초기화하는 진입점입니다.

내부적으로 다음 작업을 수행합니다.

1. View ↔ ViewModel 매핑 등록
2. `DMContainer` 자동 등록 수행
3. WPF `Loaded` 이벤트를 연결하여 필요한 경우 `DataContext` 자동 주입
4. 옵션이 켜져 있으면 현재 Window의 Region을 기준으로 `INavigator` 등록

자동 navigator 등록을 사용하는 경우, Window가 로드될 때 해당 Window의 Region이 전역 `INavigator` 등록을 갱신합니다. 그래서 이전에 로드된 Window의 Region에 계속 고정되는 상황을 피할 수 있습니다.

## 사용 방법

애플리케이션 시작 시점에 `DreamineAppBuilder.Initialize(...)` 를 한 번 호출하면 됩니다.

```csharp
using System.Reflection;
using System.Windows;
using Dreamine.MVVM.Wpf;

namespace SampleApp;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DreamineAppBuilder.Initialize(Assembly.GetExecutingAssembly());
    }
}
```

## 프로젝트 역할 분리 기준

권장 책임 분리는 다음과 같습니다.

- `Dreamine.MVVM.Core`
  - 컨테이너
  - 커맨드 인프라
  - 플랫폼 중립 MVVM 런타임 지원
- `Dreamine.MVVM.ViewModels`
  - `ViewModelBase`
- `Dreamine.MVVM.Locators`
  - ViewModel 매핑 및 해석 로직
- `Dreamine.MVVM.Wpf`
  - WPF 시작 처리 / 부트스트랩 / 런타임 연결

## 대상 프레임워크

- `net8.0-windows`
- WPF 사용

## 라이선스

MIT License
