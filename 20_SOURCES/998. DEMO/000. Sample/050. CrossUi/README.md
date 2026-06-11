# Dreamine Cross-UI Sample

> **Dreamine Core / ViewModel 계층은 UI 기술에 종속되지 않는다** 는 것을 보여주는 샘플 세트입니다.
>
> This sample set demonstrates that the **Dreamine Core / ViewModel layer is completely UI-agnostic** — the same `CounterViewModel` and `CounterService` run identically in WPF, WinForms, Blazor, and MAUI without modification.

---

## 프로젝트 구성 / Project Structure

| 프로젝트 | 설명 |
|---|---|
| `SampleCrossUi.Shared` | ViewModel · Service · Model (순수 .NET 8, UI 의존성 없음) |
| `SampleCrossUi.Wpf` | WPF 프론트엔드 (XAML 바인딩, DMContainer) |
| `SampleCrossUi.WinForms` | WinForms 프론트엔드 (PropertyChanged 구독, 이벤트 위임) |
| `SampleCrossUi.Blazor` | Blazor Server 프론트엔드 (Interactive Server 렌더 모드) |
| `SampleCrossUi.Maui` | .NET MAUI 프론트엔드 (멀티 플랫폼, 별도 솔루션으로 빌드) |

---

## 핵심 아키텍처 포인트 / Key Architecture Points

### Shared 계층은 완전히 UI-독립적 (Shared layer has zero UI dependency)

```
SampleCrossUi.Shared
├── Models/     CounterLogItem  (record)
├── Services/   ICounterService + CounterService
└── ViewModels/ CounterViewModel  (extends Dreamine ViewModelBase)
```

- `CounterViewModel`은 WPF, WinForms, Blazor, MAUI 어느 쪽도 참조하지 않습니다.
- `ICommand` 인터페이스(`RelayCommand`)가 플랫폼 간 명령 실행을 통일합니다.
- `INotifyPropertyChanged` + `ObservableCollection`이 UI 갱신 계약을 제공합니다.

### 각 UI 레이어의 통합 방식 (How each UI layer integrates)

| 플랫폼 | DI 컨테이너 | ViewModel 연결 방식 |
|---|---|---|
| WPF | `DMContainer` | `DataContext = DMContainer.Resolve<CounterViewModel>()` |
| WinForms | `DMContainer` | `PropertyChanged` 구독 + 버튼 클릭 → `ICommand.Execute` |
| Blazor | `builder.Services` (Scoped) | `@inject CounterViewModel` + `PropertyChanged` → `StateHasChanged` |
| MAUI | `builder.Services` (Singleton) | `OnAppearing` 구독 + `MainThread.BeginInvokeOnMainThread` |

---

## 빌드 및 실행 / Build & Run

### WPF / WinForms / Blazor (메인 솔루션)

```bash
# 솔루션 루트에서
dotnet build SampleCrossUi.Wpf/SampleCrossUi.Wpf.csproj
dotnet run --project SampleCrossUi.Wpf/SampleCrossUi.Wpf.csproj

dotnet build SampleCrossUi.WinForms/SampleCrossUi.WinForms.csproj
dotnet run --project SampleCrossUi.WinForms/SampleCrossUi.WinForms.csproj

dotnet build SampleCrossUi.Blazor/SampleCrossUi.Blazor.csproj
dotnet run --project SampleCrossUi.Blazor/SampleCrossUi.Blazor.csproj
# 브라우저에서 http://localhost:5000 접속
```

### MAUI (별도 빌드 필요 / Requires MAUI workload)

MAUI 프로젝트는 `microsoft.maui` 워크로드가 필요하며 중앙 패키지 관리에서 제외됩니다.

```bash
# MAUI 워크로드 설치 (최초 1회)
dotnet workload install maui

# Windows 타겟으로 실행
cd SampleCrossUi.Maui
dotnet run -f net8.0-windows10.0.19041.0
```

---

## 테스트 / Tests

`CounterViewModel` 단위 테스트는 `Dreamine.FullKit.Tests` 프로젝트에 포함되어 있습니다.

```bash
dotnet test 20_SOURCES/200. Tests/Dreamine.FullKit.Tests/Dreamine.FullKit.Tests.csproj \
  --filter "FullyQualifiedName~CounterViewModelTests"
```
