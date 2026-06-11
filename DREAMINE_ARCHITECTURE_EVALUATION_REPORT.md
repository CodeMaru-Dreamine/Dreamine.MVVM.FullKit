# Dreamine 프로젝트 아키텍처 평가 보고서

> 평가 기준일: 2026-06-11  
> 평가 방법: 저장소 전체 코드 직접 열람 + 자동 아키텍처 스캐너  
> 평가 대상: `20_SOURCES/100. Library/` 전체 (C# 파일 기준)

---

## 1. 요약

Dreamine.MVVM.FullKit은 WPF 기반 산업용/상용 애플리케이션을 위한 자체 제작 풀스택 MVVM 프레임워크이다. DI 컨테이너, 커맨드, Navigation, Behavior, Hybrid(Blazor), 통신(TCP/RabbitMQ/Serial), DB, PLC, I/O, 스레딩, 로깅 등을 포함하는 광범위한 라이브러리 군(群)이다.

핵심 설계 품질(인터페이스 분리, MVVM 순수성, 스레드 안전성)은 상당히 높은 수준으로 확인된다. 반면 정적 글로벌 상태(DMContainer, ViewModelLocator)에 의한 테스트 격리 어려움, `async void Execute` 패턴, 일부 ViewModel의 직접 의존이 주요 개선 과제로 드러난다.

**강점 요약:** 인터페이스 레이어 분리, ViewModelBase WPF 무관, 스레드 안전성(Interlocked/AsyncLocal), Source Generator, PLC/통신/DB 실무 통합  
**약점 요약:** `ViewManager` DIP 위반, `async void` ViewModel 메서드, IDialogService 미확인, MS.Extensions.DI 이중 관리  
**가장 중요한 개선 방향:** `async void` ViewModel 제거 + `ViewManager` 생성자 주입 전환

---

## 2. 저장소 구조 분석

| 프로젝트/폴더 | 역할 | 근거 파일 | 평가 |
|---|---|---|---|
| `Interfaces/` | 순수 계약 레이어 | `IServiceContainer.cs`, `INavigator.cs`, `IViewManager.cs`, `IViewModelResolver.cs` | ✅ WPF 타입 없음, 완전 분리 |
| `Core/` | DI 컨테이너 구현 | `DreamineContainer.cs`, `DMContainer.cs`, `DreamineAutoRegistrar.cs` | ✅ SRP 준수, Interlocked/AsyncLocal 안전 |
| `ViewModels/` | ViewModel 기반 타입 | `ViewModelBase.cs`, `RelayCommand.cs`, `AsyncRelayCommand.cs` | ✅ WPF 미참조 |
| `Locators/` | View-VM 매핑 (플랫폼 무관) | `ViewModelLocator.cs`, `ViewNamingConvention.cs` | ✅ WPF 무관 |
| `Locators.Wpf/` | WPF 전용 바인더 | `ViewModelBinder.cs`, `RegionBinder.cs` | ✅ WPF 격리 레이어 |
| `Behaviors.Core/` | 플랫폼 무관 Behavior 기반 | `Behavior<T>.cs`, `IAttachedObject.cs` | ✅ 추상 기반 분리 |
| `Behaviors.Wpf/` | WPF Behavior 구현체 | `WindowDragBehavior.cs`, `BehaviorCollection.cs`, `Interaction.cs` | ✅ WPF 레이어 격리 |
| `Wpf/` | WPF 진입점/런타임 | `DreamineAppBuilder.cs`, `ViewManager.cs`, `WindowStateService.cs` | ⚠️ `ViewManager`의 `DMContainer` 직접 의존 |
| `Hybrid/` | WPF+Blazor 하이브리드 | `HybridStateStore.cs`, `InMemoryHybridMessageBus.cs` | ✅ 인터페이스 분리, volatile 안전 |
| `Communication.*` | TCP/RabbitMQ/Serial 통신 | `ResilientMessageTransport.cs`, `TransportMessageBusAdapter.cs` | ✅ 인터페이스 기반 |
| `Generators/` | Roslyn Source Generator | `DreamineCommandSourceGenerator.cs`, `DreamineAutoWiringGenerator.cs` | ✅ Incremental Generator |
| `Logging.Wpf/` | WPF 로그 패널 VM | `DreamineLogPanelViewModel.cs`, `BatchedDispatcher.cs` | ✅ 인터페이스 주입 |
| `Threading.Wpf/` | 스레드 모니터 VM | `DreamineThreadMonitorViewModel.cs` | ⚠️ `async void` 2건 |
| `Database.*` | DB 추상화/구현 | `DatabaseProviderBase.cs`, SQLite/MySQL/Oracle/SqlServer | ✅ Template Method 패턴, SQL 캐시 |
| `PLC.*` | Mitsubishi/Omron PLC 클라이언트 | `MitsubishiMcPlcClient.cs` 등 | ✅ `PlcClientBase`로 공통화 |
| `200. Tests/` | xUnit 테스트 프로젝트 | 25개+ 테스트 파일, 162개 테스트 | ✅ 핵심 계약 커버 |

---

## 3. 핵심 아키텍처 분석

### 3-1. 레이어 의존성 방향

```
Interfaces  ←  Core  ←  ViewModels  ←  Locators  ←  Locators.Wpf
                                                   ←  Behaviors.Core ← Behaviors.Wpf
                                                   ←  Wpf (DreamineAppBuilder, ViewManager)
```

- **`Interfaces/`**: `System.*` 외부 어셈블리 의존 없음. `IServiceContainer`, `INavigator`, `IViewModelResolver` 등이 WPF 완전 무관 형태로 정의됨(실제 열람 확인).
- **`Core/DreamineContainer`**: `IServiceContainer` 구현, WPF 의존 없음.
- **`Locators/ViewModelLocator`**: `System.Reflection`만 사용, WPF 타입 없음.
- **WPF 의존**: `Wpf/`, `Behaviors.Wpf/`, `Locators.Wpf/`, `Logging.Wpf/` 레이어로 격리됨.

### 3-2. DMContainer 정적 퍼사드

`DMContainer.cs`: `private static IServiceContainer Container = new DreamineContainer()`인 전역 싱글턴 퍼사드. `SetContainer()` 및 `Reset()`으로 교체/초기화 가능. 모든 메서드가 `lock(SyncRoot)` 보호. `TryResolve<T>(out T?)` 예외 없는 조회 API 제공.

### 3-3. Source Generator

`DreamineCommandSourceGenerator`, `DreamineAutoWiringGenerator`, `DreamineEntryGenerator` 세 개의 Roslyn Incremental Generator. `[DreamineEntry]`, `[DreamineCommand]`, `[DreamineAutoWiring]` 어트리뷰트 기반 컴파일 타임 코드 생성.

---

## 4. SOLID 평가

| 원칙 | 평가 | 근거 | 문제점 | 개선안 |
|---|---|---|---|---|
| **SRP** | 대체로 양호 | `DreamineContainer`(DI), `ViewModelLocator`(VM 매핑), `ViewManager`(View 표시) 각각 단일 책임. `ViewNamingConvention`이 네이밍 규칙을 별도 분리 | `ViewModelLocator`가 타입 캐싱+네이밍+인스턴스 생성을 담당하나 `ViewNamingConvention`으로 일부 해소 | 캐시 관리를 별도 클래스로 추출 고려 |
| **OCP** | 양호 | `IViewModelResolver`로 VM 생성 전략 교체 가능. `IObjectActivator`, `IConstructorSelector`로 DI 활성화 전략 교체 가능. `Behavior<T>`의 `OnAttached()/OnDetaching()` 훅 제공 | `ViewManager.DisplayResolvedView`의 `switch(view)` 분기 — 새 View 타입 추가 시 내부 수정 필요 | `IViewDisplayStrategy` 등록 패턴 도입 |
| **LSP** | 양호 | `IServiceContainer : IServiceRegistry, IServiceResolver` 치환 가능. `IViewManager : INavigator` 계층 치환 가능 | 확인된 위반 없음 | - |
| **ISP** | 양호 | `IServiceRegistry`(등록)와 `IServiceResolver`(해석) 분리. `INavigator`와 `IViewManager` 분리. Behavior `IAttachedObject`, `IBehavior` 소규모 인터페이스 유지 | `IHybridMessageBus`가 Publish+Subscribe를 하나로 합침 — 허용 가능 수준 | - |
| **DIP** | 대체로 양호 | `DreamineLogPanelViewModel`이 `IDreamineLogStore`, `ILogUiDispatcher` 주입. `DreamineContainer`가 `IObjectActivator` 주입. `RabbitMqMessageBus`가 `IRabbitMqConnectionFactory` 주입 | `ViewManager`가 `DMContainer.Resolve()`를 직접 호출(생성자 주입 아님). `DMContainer`는 전역 정적 퍼사드 | `ViewManager` 생성자에 `IServiceResolver` 주입 |

---

## 5. MVVM 순수성 평가

| 항목 | 평가 | 근거 | 개선안 |
|---|---|---|---|
| **ViewModelBase UI 의존** | 없음 ✅ | `ViewModelBase.cs`: `INotifyPropertyChanged`, `INotifyPropertyChanging`만 구현. WPF 타입 미참조 | - |
| **RelayCommand UI 의존** | 허용 수준 ✅ | `System.Windows.Input.ICommand`는 `PresentationCore` — xUnit 테스트 가능. `SynchronizationContext` 캡처는 UI 마샬링 목적 명확 | - |
| **AsyncRelayCommand async void** | 주의 ⚠️ | `ICommand.Execute` 계약상 `async void` 불가피. 예외가 `ExecutionFailed` 이벤트로만 전달되어 미구독 시 소멸 | `ExecutionFailed` 구독 강제화 문서화 또는 기본 로깅 핸들러 제공 |
| **ViewModel Window/MessageBox 직접 의존** | 없음 ✅ | `ViewModels/` 3개 파일(ViewModelBase, RelayCommand, AsyncRelayCommand) 모두 Window/MessageBox 참조 없음 | - |
| **DreamineThreadMonitorViewModel async void** | 문제 🔴 | `StopSelectedThread()`, `StopAllThreads()`가 `async void`로 선언 — 예외가 AppDomain 미처리 예외로 전파됨 | `async Task`로 변경 후 Command 계층에서 래핑 |
| **Navigation/Dialog 추상화** | 양호 ✅ | `INavigator`, `IViewManager` 인터페이스로 ViewModel에서 View 표시 요청 추상화. `ViewManager` 구현체는 WPF 레이어에만 존재 | `IDialogService` 공식 인터페이스 추가 권장 |
| **Auto-wire 메커니즘** | WPF 레이어 격리 ✅ | `DreamineAppBuilder.AttachViewModelIfExists`가 `FrameworkElement.LoadedEvent` 전역 핸들러로 동작. `ViewModelLocator`(WPF 무관) + `DreamineAppBuilder`(WPF 전용) 이중 레이어 격리 | - |
| **Dispatcher 의존** | 허용 수준 ✅ | `DreamineVMS/MainWindowViewModel.cs`의 `DispatchToUi()`에서 `HasShutdownStarted` 가드 후 사용 — 종료 크래시 방지 목적으로 정당화됨 | - |

---

## 6. 프레임워크 확장성 평가

| 항목 | 현재 상태 | 리스크 | 개선안 |
|---|---|---|---|
| **DI 컨테이너 교체** | `DMContainer.SetContainer(IServiceContainer)` 제공. Microsoft.Extensions.DI 어댑터 구현으로 교체 가능 | `SetContainer()` 호출 이전 타이밍 요구 | Composition Root 패턴 명확히 문서화 |
| **ViewModel Resolver 교체** | `ViewModelLocator.RegisterResolver(IViewModelResolver)` 제공. 테스트에서 `FixedResolver`, `NullResolver`로 교체 확인 | - | - |
| **Behavior 교체** | `Behavior<T>` 추상 기반 클래스로 커스텀 구현 가능. `Interaction.GetBehaviors()` Attached Property 방식 | - | - |
| **Navigator 교체** | `INavigator` 등록만으로 교체 가능. `ContentControlNavigator`가 기본 구현체 | - | - |
| **View 디스플레이 전략** | `ViewManager`의 `switch(view)` 분기 — Window/UserControl/Page 외 커스텀 View 타입 지원 불가 | 새 플랫폼 타입 추가 시 소스 수정 필요 | `IViewDisplayStrategy` 등록 패턴 도입 |
| **Source Generator 확장** | `[DreamineCommand]`, `[DreamineAutoWiring]`, `[DreamineEntry]` 어트리뷰트 기반. Incremental Generator 사용 | 어트리뷰트 → 생성 코드를 사용자가 완전 제어 불가 | - |
| **MS.Extensions.DI 통합** | `Hybrid.Wpf`에서 `IServiceCollection.AddDreamineHybridWpf()` 확장 메서드 확인. Blazor+WPF 연동 | `DMContainer`와 `IServiceProvider` 이중 관리 리스크 | 단일 DI 계층 통합 권장 |

---

## 7. 테스트 가능성 평가

| 항목 | 평가 | 근거 | 개선안 |
|---|---|---|---|
| **테스트 프로젝트** | 있음 ✅ | `200. Tests/Dreamine.FullKit.Tests/`에 xUnit, 162개 테스트 통과 | - |
| **DI 컨테이너 테스트** | 양호 ✅ | `DreamineContainerTests`: 생성자 의존성, Singleton, 순환 의존성, Dispose, 동시성 모두 커버 | - |
| **ViewModelBase 테스트** | 매우 높음 ✅ | WPF 무관, 순수 C# — xUnit에서 직접 인스턴스화 가능 | - |
| **DMContainer 전역 상태** | 리스크 있음 ⚠️ | `DMContainer.Reset()` 없이 테스트 간 오염 가능. `DreamineContainerTests`는 `new DreamineContainer()`를 직접 사용하여 격리 | 테스트에서 `DMContainer.Reset()` Fixture 강제화 가이드 제공 |
| **async void ViewModel** | 리스크 있음 🔴 | `DreamineThreadMonitorViewModel.StopSelectedThread/StopAllThreads` — 테스트 중 예외 포착 불가 | `async Task`로 변경 |
| **WPF 전용 컴포넌트** | 제한적 ⚠️ | `ViewManager`, `DreamineAppBuilder`는 WPF 런타임 필요. 헤드리스 테스트 불가 | View 디스플레이 추상화 강화 또는 WPF UI 테스트 프레임워크 |
| **통신 컴포넌트** | 인터페이스 기반으로 Mock 용이 ✅ | `IRabbitMqConnectionFactory`, `IMessageTransport`, `IOutboundMessageQueue` 인터페이스 분리 확인 | - |

---

## 8. 타 프레임워크 비교

| 기능/특성 | Dreamine | Prism | CommunityToolkit.Mvvm | ReactiveUI | Caliburn.Micro | Microsoft.Xaml.Behaviors.Wpf |
|---|---|---|---|---|---|---|
| **DI 컨테이너** | 자체(DreamineContainer) + MS.Extensions.DI 연동 | Unity/DryIoc(외부 의존) | 없음(사용자 선택) | Splat(자체) | 자체(SimpleContainer) | 해당 없음 |
| **MVVM 기반 클래스** | ViewModelBase(경량) | BindableBase | ObservableObject(Source Gen) | ReactiveObject | Screen/Conductor | 해당 없음 |
| **커맨드** | RelayCommand, AsyncRelayCommand | DelegateCommand | RelayCommand(Source Gen) | ReactiveCommand(Rx) | ActionMessage | 해당 없음 |
| **Navigation** | INavigator/ContentControlNavigator | RegionManager(강력, 복잡) | 없음 | IScreen/RoutedViewHost | Conductor | 해당 없음 |
| **Behavior** | 자체 Behavior<T> + WPF Interaction | Microsoft.Xaml.Behaviors 의존 | 없음 | 없음 | 없음 | 원조 구현체 |
| **Source Generator** | 있음([DreamineCommand] 등) | 없음 | 있음(주력 기능) | 없음 | 없음 | 해당 없음 |
| **Hybrid(Blazor)** | 있음(HybridStateStore, MessageBus) | 없음 | 없음 | 없음 | 없음 | 없음 |
| **통신/PLC/DB** | 있음(풀스택) | 없음 | 없음 | 없음 | 없음 | 없음 |
| **테스트 가능성** | 중간(전역 상태 리스크) | 중간(RegionManager 전역) | 높음(순수 C#) | 높음(Rx Observable) | 중간 | 낮음(UI 테스트) |
| **학습 곡선** | 중간(자체 규칙) | 높음(방대한 API) | 낮음 | 높음(Rx 필요) | 중간 | 낮음 |
| **산업/하드웨어 통합** | **강점(PLC/I/O/TCP)** | 없음 | 없음 | 없음 | 없음 | 없음 |

---

## 9. Dreamine의 적정 포지션

Dreamine은 **WPF + 산업 자동화/하드웨어 통합 풀스택 프레임워크**로서 Prism이나 CommunityToolkit.Mvvm이 다루지 않는 영역(PLC, I/O, 직렬/TCP 통신, Hybrid Blazor+WPF)을 하나의 통합 라이브러리로 제공한다는 점에서 차별화된다.

> "Dreamine은 Prism 같은 대형 범용 프레임워크를 대체하기보다, WPF MVVM 실무에서 반복되는 구조 문제를 단순하고 명시적으로 해결하면서 산업용 하드웨어·통신·Hybrid UI까지 일관된 설계로 통합하는 실무형 프레임워크다."

이 포지션은 현재 코드와 부합한다. `INavigator`, `IViewManager`, `Behavior<T>`, `IMessageTransport`, `IPlcClient`, `IDatabaseProvider` 등의 인터페이스가 동일한 추상화 철학으로 설계되어 있으며, 레이어 분리와 WPF 격리가 일관되게 적용되어 있다.

---

## 10. 개선 우선순위

| 우선순위 | 개선 항목 | 이유 | 예상 효과 | 난이도 |
|---|---|---|---|---|
| 🔴 1 | `DreamineThreadMonitorViewModel`의 `async void StopSelectedThread/StopAllThreads` 제거 | 미처리 예외가 AppDomain으로 전파되어 앱 크래시 유발 가능 | 버그 방지, 테스트 가능성 향상 | 낮음 |
| 🔴 2 | `ViewManager`에서 `DMContainer` 직접 호출 → 생성자 주입 | DIP 위반, 테스트에서 `ViewManager` 단독 격리 불가 | 테스트 가능성, DIP 준수 | 낮음 |
| 🟡 3 | `IDialogService` 인터페이스 표준화 | ViewModel에서 다이얼로그 처리 공식 계약 없음 | MVVM 순수성 강화 | 중간 |
| 🟡 4 | `AsyncRelayCommand.ExecutionFailed` 기본 로깅 핸들러 제공 | 미구독 시 예외 소멸 | 디버깅 편의성 향상 | 낮음 |
| 🟡 5 | `DMContainer` 전역 상태 테스트 Fixture 가이드 공식화 | 테스트 간 오염 가능성 | 테스트 신뢰성 향상 | 낮음 |
| 🟢 6 | `ViewManager`의 `switch(view)` → `IViewDisplayStrategy` 등록 패턴 | OCP 위반, 새 View 타입 추가 시 내부 수정 필요 | 확장성 향상 | 중간 |
| 🟢 7 | MS.Extensions.DI와 DMContainer 이중 관리 통합 가이드 | Hybrid 시나리오에서 이중 등록 혼란 | 유지보수성 향상 | 높음 |

---

## 11. 최종 점수

| 평가 영역 | 배점 | 점수 | 근거 |
|---|---:|---:|---|
| **아키텍처 일관성** | 20 | 17 | `Interfaces→Core→ViewModels→Locators→Wpf` 레이어 분리 명확, 의존 방향 단방향. 감점: `ViewManager`의 `DMContainer` 직접 의존 |
| **MVVM 순수성** | 20 | 17 | `ViewModelBase`/`RelayCommand`/`AsyncRelayCommand` WPF 미참조. Navigation/Dialog 인터페이스 추상화 존재. 감점: `DreamineThreadMonitorViewModel` `async void` 2건 |
| **SOLID 준수** | 15 | 12 | SRP/LSP/ISP 양호. OCP는 `ViewManager.switch` 분기 위반. DIP는 `ViewManager→DMContainer` 직접 의존 |
| **확장성** | 15 | 12 | DI/Resolver/Behavior/Navigator 교체 포인트 제공. 감점: View 디스플레이 전략 확장 불가, MS.Extensions.DI 이중 관리 |
| **테스트 가능성** | 15 | 11 | xUnit 162개 테스트, 인터페이스 Mock 가능. 감점: 전역 DMContainer/ViewModelLocator 상태, WPF 전용 헤드리스 테스트 불가, `async void` 2건 |
| **실무 문제 해결력** | 10 | 9 | PLC/I/O/TCP/RabbitMQ/Serial/Hybrid/DB를 일관된 추상화로 통합. Source Generator, 스레드 안전성, 순환 의존성 감지 등 실무 문제 실제 해결 |
| **문서화/API 품질** | 5 | 4 | 거의 모든 public API에 XML doc 존재. 감점: 아키텍처 수준 설계 문서/다이어그램 미확인 |
| **합계** | **100** | **82** | |

---

## 12. 결론

Dreamine.MVVM.FullKit은 WPF 산업용 애플리케이션을 위한 **자체 완결형 풀스택 프레임워크**로서 아키텍처 레이어 분리, 인터페이스 추상화, 스레드 안전성 설계에서 상당한 수준의 완성도를 보인다.

**지금 공개해도 되는 수준인가?**  
핵심 아키텍처와 코드 품질은 공개 수준에 도달했다. 단, `DreamineThreadMonitorViewModel`의 `async void` 문제를 먼저 수정해야 한다.

**NuGet 패키지로 배포 가능한 수준인가?**  
개별 패키지(Dreamine.MVVM.Core, Dreamine.MVVM.ViewModels, Dreamine.Hybrid 등) 단위 배포는 가능한 품질이다. PLC/통신 패키지는 하드웨어 의존성이 있어 사용 환경 제약 문서화 필요.

**README에서 어떤 포지션으로 설명해야 하는가?**  
"WPF MVVM + 산업 하드웨어 통합을 단일 라이브러리 체계로 제공하는 실무형 경량 프레임워크. Prism 대체가 아니라 산업 현장의 WPF 앱 구조 문제를 일관된 방식으로 해결한다."

**다음 커밋에서 가장 먼저 해야 할 작업:**
1. `DreamineThreadMonitorViewModel.StopSelectedThread/StopAllThreads` → `async Task`로 변경
2. `ViewManager` 생성자에 `IServiceResolver` 주입으로 `DMContainer` 직접 호출 제거
3. `IDialogService` 인터페이스 추가 및 기본 WPF 구현체 제공

---

*평가 기준: 프레임워크가 반복되는 WPF MVVM 실무 문제를 안전하고 일관된 방식으로 줄이는가.*  
*스캐너 결과: D:\Work\DreamineScanner\scan_result.txt*
