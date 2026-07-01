# Dreamine.Threading.Wpf

**Dreamine.Threading.Wpf**는 Dreamine.Threading을 위한 WPF 모니터링 UI 패키지입니다.

이 패키지는 WPF 애플리케이션에서 Worker Thread 상태, Priority, Interval, 할당된 CPU Core, Affinity 상태, Job 수, Cycle Count, Fault 상태 등을 표시합니다.

[➡️ English README](./README.md)

## 목적

Core 패키지인 `Dreamine.Threading`은 Worker Thread, Polling Job, Scheduling Policy, Thread 상태 모델을 정의합니다.  
이 패키지는 해당 Runtime 상태를 관찰하기 위한 WPF UI 구성 요소를 제공합니다.

```text
Dreamine.Threading
 └─ Thread 상태 및 Manager 추상화

Dreamine.Threading.Wpf
 ├─ Thread Monitor View
 ├─ Thread Monitor ViewModel
 ├─ WPF UI Dispatcher
 └─ 상태/우선순위 Converter
```

## 책임

이 패키지의 책임:

- 등록된 Worker Thread 표시
- Thread Status 및 Priority 표시
- Interval 및 할당된 CPU Core 표시
- Affinity 상태 표시
- Cycle Count 및 Job Count 표시
- Last Error 정보 표시
- WPF UI Thread에서 안전하게 모니터링 데이터 갱신
- WPF 전용 코드를 Core Threading 패키지 밖으로 분리

## 패키지 구조

```text
Dreamine.Threading.Wpf
├─ Services
│  └─ WpfThreadUiDispatcher.cs
│
├─ ViewModels
│  └─ DreamineThreadMonitorViewModel.cs
│
├─ Views
│  └─ DreamineThreadMonitorView.xaml
│
├─ Converters
│  ├─ ThreadPriorityTextConverter.cs
│  └─ ThreadStatusBrushConverter.cs
│
└─ Registration
   └─ DreamineThreadingWpfRegistration.cs
```

## Thread Monitor View

Monitor View는 WPF `DataGrid`에 Thread 정보를 표시합니다.

표시 컬럼:

```text
Name
Status
Priority
Interval
Core
Affinity
Jobs
Cycles
Last Error
```

이 Monitor로 다음 상태를 관찰할 수 있습니다.

- Raw 0ms Worker
- Adaptive 0ms Worker
- Normal Polling Worker
- Overflow Job 분산 상태
- Cycle Count 차이
- Affinity 할당
- Fault 상태

## 선택 상세 패널

하단 상세 패널은 선택된 Worker Thread 정보를 표시합니다.

표시 정보:

```text
Name
Status
Priority
Interval
Core
Affinity
Job Count
Cycle Count
Started At
Stopped At
Last Error
```

Refresh 중에도 선택 상태가 유지되도록 선택된 Thread 이름을 기준으로 재선택합니다.

## WPF UI Dispatching

`WpfThreadUiDispatcher`는 Monitor 갱신이 WPF UI Thread에서 수행되도록 보장합니다.

```text
Background Refresh Timer
 → IDreamineThreadManager.GetThreadInfos()
 → WpfThreadUiDispatcher.BeginInvoke(...)
 → ObservableCollection 갱신
```

## 등록

현재 등록 방식:

```csharp
using Dreamine.Threading.Wpf.Registration;

DreamineThreadingWpfRegistration.Register();
```

이 호출은 다음 항목을 등록합니다.

```text
WpfThreadUiDispatcher
DreamineThreadMonitorViewModel
DreamineThreadMonitorView
```

> 이 패키지는 Worker Thread를 직접 생성하거나 제어하지 않습니다. `IDreamineThreadManager`의 상태만 관찰합니다.

## 사용 예시

```xml
<UserControl
    xmlns:threadingViews="clr-namespace:Dreamine.Threading.Wpf.Views;assembly=Dreamine.Threading.Wpf">

    <threadingViews:DreamineThreadMonitorView />

</UserControl>
```

Wrapper Page 내부에서 사용할 경우:

```xml
<threadingViews:DreamineThreadMonitorView
    DataContext="{Binding ThreadMonitor}" />
```

## 설계 메모

이 패키지는 `Dreamine.Threading`의 추상화에 의존합니다.  
Native Windows Thread 동작을 직접 제어하지 않아야 합니다.

WPF Monitor는 아래 정보를 통해 Thread 상태를 관찰합니다.

```text
IDreamineThreadManager
DreamineThreadInfo
DreamineThreadStatus
DreamineThreadPriority
```

이렇게 하면 UI 계층이 Scheduling 및 Platform 로직과 분리됩니다.

## 검증 시나리오

WPF Monitor는 다음 구성으로 검증되었습니다.

```text
High Adaptive Job: 5
High Raw Job:      5
Normal Job:        30
Total Job:         40
```

Monitor에서 관찰된 동작:

```text
Raw Worker
 → 매우 높은 Cycle Count

Adaptive Worker
 → Adaptive Delay가 적용되어 낮은 Cycle Count

Normal Worker
 → 100ms 기준의 안정적인 Cycle 동작

Overflow Job
 → 일부 Worker의 Jobs Count 증가로 확인 가능

Affinity
 → Core 및 Affinity 컬럼에서 확인 가능

Last Error
 → 정상 검증 중 비어 있음
```

## 관련 패키지

```text
Dreamine.Threading
Dreamine.Threading.Windows
Dreamine.Threading.Wpf
```

## 상태

구현됨:

- WPF Thread Monitor View
- Thread Monitor ViewModel
- Observable Thread 정보 목록
- 주기적 Refresh
- Status Brush Converter
- Priority Text Converter
- 선택 Thread 상세 패널
- WPF UI Dispatcher
- Registration Helper

향후 계획:

- Summary Header
- 전체 Worker 수 표시
- 전체 Job 수 표시
- Overflow Job 수 표시
- Adaptive / Raw / Normal 그룹 표시
- Refresh Interval 옵션
- Start / Stop / Pause / Resume Command 버튼
- 더 안정적인 Incremental Collection Update

## License

MIT License
