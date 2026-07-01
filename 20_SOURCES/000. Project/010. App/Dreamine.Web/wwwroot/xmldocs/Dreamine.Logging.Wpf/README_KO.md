# Dreamine.Logging.Wpf

Dreamine.Logging.Wpf는 Dreamine 애플리케이션을 위한 WPF 전용 로그 UI 패키지입니다.
`Dreamine.Logging`의 로그 파이프라인을 WPF 화면에 연결하기 위해 로그 패널 View, 상한이 있는 로그 패널 ViewModel, 고빈도 로그 갱신에 적합한 UI Dispatcher Helper를 제공합니다.

[➡️ English Version](./README.md)

## 목적

이 패키지는 WPF 표시 계층과 UI 스레드 연동만 담당합니다.
핵심 로그 파이프라인, Sink, Formatter, 비동기 큐는 `Dreamine.Logging` 패키지가 담당합니다.

`Dreamine.Logging.Wpf`는 `Dreamine.Logging`에 의존합니다.
반대로 `Dreamine.Logging`은 WPF를 참조하면 안 됩니다.

## 주요 기능

- WPF 로그 표시용 `DreamineLogPanelView`
- 로그 엔트리 바인딩용 `DreamineLogPanelViewModel`
- `DefaultDisplayCapacity = 1000` 기준의 표시 로그 컬렉션 상한
- `BatchedDispatcher<T>` 기반 UI 배치 갱신
- WPF Dispatcher 접근을 위한 `WpfLogUiDispatcher`
- `IDreamineLogStore.LogAdded` 이벤트 기반 실시간 로그 표시
- `DreamineLogPanelViewModel.Clear()` 기반 Clear 지원
- `AutoScroll`, `EntryAppended` 기반 자동 선택/자동 스크롤 지원
- 선택된 로그의 상세 내용 표시
- `Dispose()`에서 이벤트 구독 해제하여 ViewModel 보존 누수 방지

## 권장 구조

WPF 패키지는 메모리 로그 저장소를 관찰하는 역할만 담당해야 합니다.
파일 기록, Logger 소유, 비동기 큐 생성은 WPF 패키지의 책임이 아닙니다.

```text
[Dreamine.Logging]
  Logger
    └─► AsyncQueueSink
          └─► CompositeLogSink
                ├─► InMemoryLogStore ──(LogAdded event)──┐
                └─► TextFileLogSink                      │
                                                         ▼
                                          [Dreamine.Logging.Wpf]
                                          DreamineLogPanelViewModel
                                            └─► DreamineLogPanelView
```

`DreamineLogPanelViewModel`은 Sink 체인의 자식이 아닙니다.
ViewModel은 `InMemoryLogStore.LogAdded` 이벤트를 구독하는 별도 Observer이며,
`BatchedDispatcher<T>`를 통해 UI 스레드로 배치 갱신을 전달합니다.

## Batched UI Dispatch가 필요한 이유

로그 패널은 여러 Worker Thread에서 동시에 발생하는 로그 이벤트를 받을 수 있습니다.
로그 1개마다 `Dispatcher.BeginInvoke`를 하나씩 예약하면 WPF Dispatcher Queue가 UI 처리 속도보다 빠르게 증가할 수 있습니다.
이 경우 메모리 압박이 생기고 UI가 멈춘 것처럼 느려질 수 있습니다.

`BatchedDispatcher<T>`는 들어오는 로그를 제한된 UI 배치로 묶어서 처리합니다.

- 생산자는 어느 Thread에서든 Enqueue만 합니다.
- 동시에 예약되는 Dispatcher 작업은 최대 1개로 제한됩니다.
- 한 번의 UI 배치에서 최대 `MaxBatchSize`개를 처리합니다.
- 남은 항목은 다음 Dispatcher 작업에서 이어서 처리됩니다.
- 표시 컬렉션은 설정된 display capacity까지만 유지됩니다.

## XAML 사용 예시

```xml
<UserControl x:Class="SampleSmart.Pages.PageSub.PageLog"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:logViews="clr-namespace:Dreamine.Logging.Wpf.Views;assembly=Dreamine.Logging.Wpf">
    <Grid>
        <logViews:DreamineLogPanelView DataContext="{Binding LogPanel}" />
    </Grid>
</UserControl>
```

## ViewModel 래퍼 예시

```csharp
public sealed class PageLogViewModel : ViewModelBase
{
    public DreamineLogPanelViewModel LogPanel { get; }

    public PageLogViewModel(DreamineLogPanelViewModel logPanel)
    {
        LogPanel = logPanel;
    }
}
```

## 등록 예시

로그 저장소는 Core Logging 등록에서 먼저 등록되어 있어야 합니다.
WPF 패키지는 UI Dispatcher와 로그 패널 ViewModel만 등록하면 됩니다.

```csharp
DMContainer.RegisterSingleton<WpfLogUiDispatcher>(
    new WpfLogUiDispatcher());
DMContainer.RegisterSingleton<ILogUiDispatcher>(
    DMContainer.Resolve<WpfLogUiDispatcher>());

DMContainer.Register<DreamineLogPanelViewModel>(() =>
    new DreamineLogPanelViewModel(
        DMContainer.Resolve<IDreamineLogStore>(),
        DMContainer.Resolve<ILogUiDispatcher>()));
```

표시 로그 개수를 별도로 조정하려면 다음처럼 등록합니다.

```csharp
DMContainer.Register<DreamineLogPanelViewModel>(() =>
    new DreamineLogPanelViewModel(
        DMContainer.Resolve<IDreamineLogStore>(),
        DMContainer.Resolve<ILogUiDispatcher>(),
        displayCapacity: 2000));
```

## Logging + WPF 전체 등록 예시

```csharp
private static IAsyncDisposable? _loggingShutdown;

private static void RegisterLogging()
{
    _loggingShutdown = DreamineLoggingRegistration.Register(new DreamineLoggingOptions
    {
        Category = "SampleSmart",
        LogDirectory = Path.Combine(AppContext.BaseDirectory, "Logs")
    });

    DMContainer.RegisterSingleton<WpfLogUiDispatcher>(new WpfLogUiDispatcher());
    DMContainer.RegisterSingleton<ILogUiDispatcher>(
        DMContainer.Resolve<WpfLogUiDispatcher>());

    DMContainer.Register<DreamineLogPanelViewModel>(() =>
        new DreamineLogPanelViewModel(
            DMContainer.Resolve<IDreamineLogStore>(),
            DMContainer.Resolve<ILogUiDispatcher>()));
}
```

## 종료 처리

비동기 등록 handle을 사용할 경우 애플리케이션 종료 시 dispose 하여 대기 로그를 비워야 합니다.

```csharp
protected override void OnExit(ExitEventArgs e)
{
    try
    {
        _loggingShutdown?.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
    catch
    {
        // 종료 중 로그 오류가 애플리케이션 종료를 막으면 안 됩니다.
    }
    finally
    {
        base.OnExit(e);
    }
}
```

`OnExit`은 동기 메서드이므로 `async void` 대신 `GetAwaiter().GetResult()`로 대기해야
프로세스가 큐 Flush 완료 전에 종료되는 것을 막을 수 있습니다.

## 주의 사항

`DreamineLogPanelView`는 `DreamineLogPanelViewModel`을 `DataContext`로 받아야 합니다.
다른 페이지 내부에 로그 패널을 감싸서 사용할 경우 내부 View의 `DataContext`를 명시적으로 연결해야 합니다.

```xml
<logViews:DreamineLogPanelView DataContext="{Binding LogPanel}" />
```

상세 로그 텍스트는 읽기 전용 속성이므로 `TextBox.Text` 바인딩에는 `Mode=OneWay`를 사용해야 합니다.

```xml
<TextBox Text="{Binding SelectedDetailText, Mode=OneWay}" />
```

소유 Page 또는 Window가 완전히 제거될 때는 `DreamineLogPanelViewModel.Dispose()`가 호출되도록 처리해야 합니다.
이 처리는 `IDreamineLogStore.LogAdded` 이벤트 구독을 해제하여 ViewModel이 이벤트 소스에 의해 계속 살아있는 문제를 막습니다.

## 성능 참고 사항

- 표시용 `Entries` 컬렉션은 상한이 있어 무한 증가하지 않습니다.
- UI 갱신은 로그 1건당 Dispatcher 작업 1개가 아니라 배치 방식으로 처리됩니다.
- 로그 패널은 진단/모니터링 용도이며, 무제한 로그 보관소가 아닙니다.
- 장기 보관은 `Dreamine.Logging`의 `TextFileLogSink` 또는 다른 영속 Sink를 사용합니다.
- 운영 Thread Loop에서는 진단 모드가 아닌 한 매 Cycle 로그 출력을 피하는 것이 좋습니다.

## Dreamine.Logging과의 관계

```text
Dreamine.Logging.Wpf
  -> Dreamine.Logging
```

의존성 방향은 반드시 단방향으로 유지해야 합니다.
WPF 패키지는 Core Logging 패키지 위에 올라가는 표시 계층 Adapter입니다.

## 향후 계획

- `DMContainer.UseDreamineLoggingWpf()` (UI Dispatcher 및 패널 ViewModel 등록을 한 줄로 수행)
- `DMContainer.UseDreamineLoggingForWpf(...)` (Core + WPF 등록을 한 번에 수행하는 통합 진입점)
- 내장 Clear Command 바인딩
- 로그 레벨 필터
- 검색/필터 UI
- 선택 로그 내보내기
- 선택적 Auto-scroll Behavior Helper
- Async 로그 Drop 상태 표시 기능 (`AsyncQueueSink.DroppedCount` 표시 UI)

## 라이선스

MIT License
