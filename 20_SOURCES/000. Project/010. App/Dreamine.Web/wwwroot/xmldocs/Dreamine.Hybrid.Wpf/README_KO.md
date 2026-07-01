<!--!
\file README_ko.md
Dreamine.Hybrid.Wpf - WPF + BlazorWebView(WebView2) 하이브리드 호스팅 인프라.
\details 아키텍처/설치/퀵스타트/컴포넌트 레퍼런스/진단 포인트를 정리합니다.
\author Dreamine
\date 2026-02-23
\version 1.0.0
-->

# Dreamine.Hybrid.Wpf

**WPF 애플리케이션 안에 Blazor UI를 임베드**하기 위한  
**BlazorWebView(WebView2) 기반 하이브리드 호스팅 레이어**입니다.  
Dreamine 아키텍처의 "WPF Shell + Blazor 화면" 조합을 **명시적(Explicit) 방식**으로 구현하는 데 초점을 둡니다.

[➡️ English Version](README.md)

---

## Screenshots

### Embedded Mode
![Embedded Mode](./assets/embedded-mode.png)

### Server Mode (WebView2)
![Server Mode (WebView2)](./assets/server-mode-webview2.png)

### External Browser Access
![External Browser Access](./assets/external-browser-access.png)

---

## 이 라이브러리가 해결하는 문제

이 모듈은 아래 요구를 동시에 만족시키기 위해 만들어졌습니다.

- **WPF Shell은 그대로 유지** (창 관리 / 장비 UI / 기존 자산)
- **Blazor 컴포넌트** (대시보드 / 리치 UI / 빠른 반복 개발)를 WPF 안에 호스팅
- MVVM 관점에서 **"마법(자동)"이 아니라 "명시적 연결"** 로 관리
- WebView2 흔한 문제 (캐시 경로 / 초기화 타이밍 / 디자이너 크래시) 를 회피

---

## 주요 기능

- **HybridHostControl**: WPF `UserControl` 안에 `BlazorWebView`를 호스팅
- **명시적 와이어링(MVVM 친화)**: `HostPage`, `RootComponentType`, `RootSelector`, `Services`
- **DI 확장 메서드**: `AddDreamineHybridWpf()` 한 번 호출로 필수 서비스 일괄 등록
- **디자인 타임 안전**: Visual Studio 디자이너에서 초기화를 자동 회피
- **내부 WebView2 진단 / 안전 캐시 경로** 적용 (한글 경로 문제 완화)

---

## 요구사항

- **.NET**: `net8.0-windows`
- **WPF**: 사용 (`<UseWPF>true</UseWPF>`)
- **패키지**:
  - `Microsoft.AspNetCore.Components.WebView.Wpf` 8.x
  - `Microsoft.Web.WebView2` 1.x
- **WebView2 Runtime** 설치 필요 (대부분 PC는 Evergreen 런타임으로 자동 충족)

**프로젝트 참조:**
- `Dreamine.Hybrid` — 코어 인터페이스 (예: `IHybridMessageBus`)
- 사용자의 Blazor 컴포넌트 프로젝트 — `HybridHostControl`에 전달할 루트 컴포넌트 타입

---

## 설치

### 옵션 A) 프로젝트 참조 (현재 권장)

WPF Shell 프로젝트에서 `Dreamine.Hybrid.Wpf`를 프로젝트 참조로 추가합니다.

```xml
<ItemGroup>
  <ProjectReference Include="..\Dreamine.Hybrid.Wpf\Dreamine.Hybrid.Wpf.csproj" />
</ItemGroup>
```

### 옵션 B) (향후) NuGet

NuGet으로 배포되면 동일한 퀵스타트 절차로 사용합니다.

---

## 프로젝트 구조

```
Dreamine.Hybrid.Wpf/
├── Controls/
│   ├── HybridHostControl.xaml          # BlazorWebView를 호스팅하는 UserControl
│   └── HybridHostControl.xaml.cs       # HostPage, Services, RootComponent 연결 코드
├── Converters/
│   └── BooleanToVisibilityConverter.cs # bool → Visibility 변환기 (싱글톤)
├── DependencyInjection/
│   └── ServiceCollectionExtensions.cs  # AddDreamineHybridWpf() 확장 메서드
├── Internal/
│   └── WebView2Initializer.cs          # WebView2 안전 생성 및 오프라인 안내 페이지
├── Utility/
│   └── DesignTimeGuard.cs              # XAML 디자이너 감지 유틸리티
└── Dreamine.Hybrid.Wpf.csproj
```

---

## 아키텍처

```
WPF Shell (Window / UserControl)
│
├── HybridHostControl
│   └── BlazorWebView (WebView2)
│       └── Blazor 루트 컴포넌트 (예: App.razor)
│
└── IHybridMessageBus (InMemoryHybridMessageBus)
    ↔ WPF ViewModel ↔ Blazor 컴포넌트 간 공유
```

---

## 퀵스타트

### 1) 서비스 등록 (DI)

WPF 앱 시작 시점 (예: `App.xaml.cs`) 에서:

```csharp
using Dreamine.Hybrid.Wpf.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System;

public partial class App
{
    private IServiceProvider? _services;

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        services.AddDreamineHybridWpf();

        _services = services.BuildServiceProvider();

        new MainWindow().Show();
    }

    public IServiceProvider Services => _services
        ?? throw new InvalidOperationException("ServiceProvider가 초기화되지 않았습니다.");
}
```

> `AddDreamineHybridWpf()` 내부에서 `AddWpfBlazorWebView()` 호출 및  
> `IHybridMessageBus → InMemoryHybridMessageBus` (싱글톤) 등록을 수행합니다. Debug 빌드에서는 BlazorWebView 개발자 도구도 활성화합니다.

---

### 2) XAML에 `HybridHostControl` 배치

```xml
<Window x:Class="Sample.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:hybrid="clr-namespace:Dreamine.Hybrid.Wpf.Controls;assembly=Dreamine.Hybrid.Wpf"
        Title="Hybrid Shell" Width="1200" Height="800">
    <Grid>
        <hybrid:HybridHostControl x:Name="HybridHost"
                                  HorizontalAlignment="Stretch"
                                  VerticalAlignment="Stretch"/>
    </Grid>
</Window>
```

---

### 3) 런타임에서 RootComponent + Services 연결

```csharp
public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();

        HybridHost.HostPage = "wwwroot/index.html";
        HybridHost.RootSelector = "#app";
        HybridHost.RootComponentType = typeof(MyBlazorApp.App);
        HybridHost.Services = ((App)System.Windows.Application.Current).Services;
    }
}
```

컨트롤은 `Loaded` 이벤트 시 자동으로 **한 번만** 초기화됩니다.  
`Services` 또는 `RootComponentType`이 설정되지 않으면 `InvalidOperationException`을 발생시킵니다.

---

## 컴포넌트 레퍼런스

### `HybridHostControl`

`BlazorWebView`를 래핑하는 WPF `UserControl`입니다. 컨트롤이 로드되기 **전에** 아래 프로퍼티를 설정해야 합니다:

| 프로퍼티 | 타입 | 기본값 | 필수 여부 | 설명 |
|---|---|---|---|---|
| `HostPage` | `string` | `"wwwroot/index.html"` | 선택 | Blazor 호스트 HTML 경로 |
| `RootComponentType` | `Type?` | `null` | **필수** | Blazor 루트 컴포넌트 타입 |
| `RootSelector` | `string` | `"#app"` | 선택 | 루트 마운트 CSS 셀렉터 |
| `Services` | `IServiceProvider?` | `null` | **필수** | BlazorWebView 등록이 포함된 DI 컨테이너 |

---

### `BooleanToVisibilityConverter`

표준 `bool → Visibility` 변환기입니다. XAML에서 싱글톤으로 사용:

```xml
<TextBlock Visibility="{Binding IsVisible,
    Converter={x:Static converters:BooleanToVisibilityConverter.Instance}}" />
```

---

### `DesignTimeGuard`

Visual Studio 디자이너 실행 여부를 감지하는 정적 클래스입니다.  
`IsInDesignMode` 프로퍼티를 XAML 트리거 또는 코드 비하인드에서 활용하여 런타임 로직이 디자이너에서 실행되는 것을 방지합니다.

---

## 디자인 타임 안정성

Visual Studio 디자이너에서 WebView2/Blazor 초기화가 동작하면 크래시가 나기 쉽습니다.  
이 모듈은 이를 회피하기 위한 장치를 포함합니다:

- `HybridHostControl` 내부에서 `DesignerProperties.GetIsInDesignMode(this)` 체크 후 조기 반환
- `DesignTimeGuard.IsInDesignMode` 제공 (XAML에서 사용 가능)

```xml
<!-- 디자이너에서만 숨김 처리 예시 -->
<hybrid:HybridHostControl
    Visibility="{Binding Source={x:Static local:DesignTimeGuard.IsInDesignMode},
                 Converter={x:Static converters:BooleanToVisibilityConverter.Instance}}"/>
```

---

## WebView2 안전 캐시 경로 / 진단 로그

사용자 프로필 경로에 한글 / 특수문자가 포함되어 WebView2가 문제를 일으키는 경우가 있습니다.  
이 패키지는 WebView2 초기화 헬퍼를 internal로 유지해서 WebView2 세부 설정이 public API로 굳어지지 않게 합니다. 번들 호스트는 `%LocalAppData%\Dreamine\WebView2Cache` 아래 ASCII-safe 캐시 경로를 사용하고, 초기화/네비게이션 진단을 `Debug.WriteLine`으로 남깁니다.

직접 `WebView2`를 저수준으로 호스팅해야 한다면 애플리케이션 레이어에서 `CoreWebView2CreationProperties.UserDataFolder`를 명시적으로 설정하세요.

---

## 로드맵

- NuGet 배포 (버전 정책 / 패키징 / 샘플 포함)
- 서비스 등록 옵션 확장 (로그 / 캐시 정책 / 환경별 설정)
- WPF Shell 부트스트랩 (Host Builder 스타일) 제공
- 메시지 버스 브릿지 / 네비게이션 / ViewModel 패턴 샘플 강화

---

## License

`LICENSE` 참고.
