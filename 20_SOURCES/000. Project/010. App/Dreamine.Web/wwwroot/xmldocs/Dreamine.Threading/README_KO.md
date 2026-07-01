# Dreamine.Threading

**Dreamine.Threading**은 Dreamine 애플리케이션을 위한 핵심 스레딩 추상화 계층입니다.

이 패키지는 Dreamine 기반 애플리케이션에서 사용할 계약, 모델, 스케줄링 정책, Worker Thread 개념, Polling Job 구조, CPU 기반 Cycle Policy, Thread Manager 구조를 정의합니다.

> 이 패키지는 .NET ThreadPool이나 Task Parallel Library를 대체하려는 목적이 아닙니다.  
> 목적은 Worker Thread, Polling Job, Scheduling, CPU 기반 Delay 정책, Core Assignment 규칙을 Dreamine 내부 경계로 분리하는 것입니다.

[➡️ English README](./README.md)

## 목적

Dreamine.Threading은 장비 소프트웨어나 장시간 실행되는 데스크톱 애플리케이션에서 자주 발생하는 스레딩 문제를 줄이기 위해 설계되었습니다.

- 정책 없이 Raw Thread가 과도하게 생성됨
- Polling Loop가 Service, ViewModel 등에 흩어짐
- CPU Core 지정 로직이 비즈니스 로직과 섞임
- Static ThreadManager에 수명 관리가 숨겨짐
- UI, OS, 실행 정책이 강하게 결합됨
- 고속 Polling Loop가 CPU를 제한 없이 점유함

Dreamine.Threading은 이러한 책임을 명확한 계약과 교체 가능한 서비스로 분리합니다.

## 핵심 개념

### Worker Thread

Worker Thread는 실제 실행 루프를 담당합니다.

```text
IDreamineThread
 ├─ Start
 ├─ Stop
 ├─ Pause
 ├─ Resume
 └─ AddJob
```

Worker Thread는 실행 루프, 수명 상태, 할당된 Job, Core Assignment 정보, Cycle Count를 보유합니다.

### Thread Job

Job은 Worker Thread 안에서 실행되는 작업 단위입니다.

```text
IDreamineThreadJob
 ├─ Name
 ├─ Interval
 ├─ ShouldRun
 └─ ExecuteAsync
```

Job을 많이 등록한다고 해서 OS Thread를 같은 수만큼 생성하지 않습니다.

### Dedicated Worker와 Overflow Polling

Dreamine.Threading은 CPU Core당 전용 Worker Thread 수를 제한하는 정책을 지원합니다.

예:

```text
Logical Core 수: 2
AutoThreadsPerCore: 2

전용 Worker 용량:
2 Core × 2 Worker = 4 Worker
```

만약 40개의 Thread Job을 등록하면:

```text
4개  -> 전용 Worker Thread
36개 -> 기존 Worker에 배정되는 Overflow Polling Job
```

이 방식은 무분별한 Thread 생성을 막고 불필요한 Context Switching을 줄입니다.

### 0ms 고속 Worker

`IntervalMs = 0`을 지원합니다.

Dreamine.Threading에서 0ms Interval은 다음 의미입니다.

```text
가능한 한 빠르게 반복 실행
```

FA/장비 소프트웨어에서는 다음 작업에 필요할 수 있습니다.

- 고속 IO Scan
- Interlock Scan
- Motion 상태 Scan
- Emergency 조건 Scan
- 고속 Sequence 상태 Scan

다만 0ms Loop는 CPU를 공격적으로 점유할 수 있습니다. 그래서 Dreamine.Threading은 두 정책을 분리합니다.

```text
Raw 0ms Worker
 → IntervalMs = 0
 → UseAdaptiveCpuDelay = false

Adaptive 0ms Worker
 → IntervalMs = 0
 → UseAdaptiveCpuDelay = true
```

### Adaptive CPU Delay

`AdaptiveCpuCyclePolicy`는 프로세스 CPU 사용률이 높아질 때 동적으로 Delay를 추가할 수 있습니다.

예시 정책:

```text
CPU >= 70% -> 5ms Delay
CPU >= 50% -> 3ms Delay
CPU >= 30% -> 1ms Delay
CPU <  30% -> 0ms Delay
```

이 방식은 FA 스타일의 고속 Loop를 허용하면서도 CPU 폭주를 줄이는 데 목적이 있습니다.

### YieldWhenIntervalIsZero

`YieldWhenIntervalIsZero`는 Delay가 0ms일 때 CPU Yield를 수행할지 결정합니다.

```text
YieldWhenIntervalIsZero = true
 → Delay가 0일 때 Thread.Yield() 호출

YieldWhenIntervalIsZero = false
 → 명시적 Yield 없이 Full-speed Loop 수행
```

엄격한 FA 고속 Loop에서는 `false`를 사용할 수 있습니다. 일반 애플리케이션에서는 `true`가 더 안전합니다.

## 프로젝트 범위

이 패키지에 포함되는 내용:

- 스레딩 인터페이스
- 스레딩 모델 및 옵션
- Worker Thread 추상화
- Thread Job 추상화
- Core Assignment 모델
- Cycle Context 모델
- Fixed Interval Cycle Policy
- Adaptive CPU Cycle Policy
- Overflow Polling Policy
- Auto Core Allocation 정책
- Thread Manager 및 Scheduler 서비스

이 패키지에 포함하지 않는 내용:

- Windows CPU Affinity 구현
- Windows Timer Resolution 구현
- Windows 프로세스 CPU 사용률 Provider 구현
- WPF 모니터링 UI

이 책임은 별도 패키지에서 처리합니다.

```text
Dreamine.Threading.Windows
Dreamine.Threading.Wpf
```

## 패키지 구조

```text
Dreamine.Threading
├─ Interfaces
│  ├─ ICpuUsageProvider.cs
│  ├─ IDreamineThread.cs
│  ├─ IDreamineThreadJob.cs
│  ├─ IDreamineThreadManager.cs
│  ├─ IDreamineThreadScheduler.cs
│  ├─ IThreadAffinityService.cs
│  ├─ IThreadCoreAllocator.cs
│  ├─ IThreadCyclePolicy.cs
│  └─ ITimerResolutionService.cs
│
├─ Models
│  ├─ DreamineThreadCoreAssignment.cs
│  ├─ DreamineThreadCoreMode.cs
│  ├─ DreamineThreadCycleContext.cs
│  ├─ DreamineThreadInfo.cs
│  ├─ DreamineThreadJobOptions.cs
│  ├─ DreamineThreadOptions.cs
│  ├─ DreamineThreadPriority.cs
│  └─ DreamineThreadStatus.cs
│
├─ Policies
│  ├─ AdaptiveCpuCyclePolicy.cs
│  ├─ FixedIntervalCyclePolicy.cs
│  └─ OverflowPollingPolicy.cs
│
├─ Allocators
│  └─ AutoCoreAllocator.cs
│
└─ Services
   ├─ DreamineThread.cs
   ├─ DreamineThreadJob.cs
   ├─ DreamineThreadManager.cs
   └─ DreamineThreadScheduler.cs
```

## 기본 사용 예시

```csharp
using Dreamine.Threading.Allocators;
using Dreamine.Threading.Models;
using Dreamine.Threading.Policies;
using Dreamine.Threading.Services;

var manager = new DreamineThreadManager(
    new AutoCoreAllocator(2),
    new FixedIntervalCyclePolicy(),
    new DreamineThreadScheduler());

for (var i = 0; i < 40; i++)
{
    var index = i;

    manager.Register(
        new DreamineThreadOptions
        {
            Name = $"Job-{index}",
            CoreMode = DreamineThreadCoreMode.Auto,
            AutoThreadsPerCore = 2,
            IntervalMs = 10,
            OverflowPollingIntervalMs = 100
        },
        token =>
        {
            Console.WriteLine($"Job {index} tick");
            return ValueTask.CompletedTask;
        });
}
```

Logical Core가 2개이고 `AutoThreadsPerCore = 2`라면 최대 4개의 전용 Worker Thread가 생성됩니다. 나머지 Job은 Overflow Polling Job으로 기존 Worker에 배정됩니다.

## 고속 Job과 일반 Job 예시

```csharp
// Adaptive CPU Delay가 적용되는 고속 Job.
for (var i = 0; i < 5; i++)
{
    var index = i;

    threadManager.Register(
        new DreamineThreadOptions
        {
            Name = $"HighMonitor-Adaptive-{index:00}",
            Priority = DreamineThreadPriority.High,
            IntervalMs = 0,
            CoreMode = DreamineThreadCoreMode.Auto,
            AutoThreadsPerCore = 2,
            OverflowPollingIntervalMs = 10,
            AutoStart = true,
            UseHighPrecisionTimer = false,
            YieldWhenIntervalIsZero = false,
            UseAdaptiveCpuDelay = true
        },
        token => ValueTask.CompletedTask);
}

// Adaptive CPU Delay가 없는 고속 Job.
for (var i = 0; i < 5; i++)
{
    var index = i;

    threadManager.Register(
        new DreamineThreadOptions
        {
            Name = $"HighMonitor-Raw-{index:00}",
            Priority = DreamineThreadPriority.High,
            IntervalMs = 0,
            CoreMode = DreamineThreadCoreMode.Auto,
            AutoThreadsPerCore = 2,
            OverflowPollingIntervalMs = 10,
            AutoStart = true,
            UseHighPrecisionTimer = false,
            YieldWhenIntervalIsZero = false,
            UseAdaptiveCpuDelay = false
        },
        token => ValueTask.CompletedTask);
}

// 일반 Polling Job.
for (var i = 0; i < 30; i++)
{
    var index = i;

    threadManager.Register(
        new DreamineThreadOptions
        {
            Name = $"NormalThread-{index:00}",
            Priority = DreamineThreadPriority.Normal,
            IntervalMs = 100,
            CoreMode = DreamineThreadCoreMode.Auto,
            AutoThreadsPerCore = 2,
            OverflowPollingIntervalMs = 500,
            AutoStart = true,
            UseHighPrecisionTimer = false,
            YieldWhenIntervalIsZero = true,
            UseAdaptiveCpuDelay = true
        },
        token => ValueTask.CompletedTask);
}
```

Logical Core가 16개인 환경의 기대 동작:

```text
AutoThreadsPerCore = 2
전용 Worker 용량 = 16 × 2 = 32

전체 Job = 40
전용 Worker = 32
Overflow Job = 8
```

## 런타임 검증 예시

다음 구성으로 샘플 검증을 수행했습니다.

```text
High Adaptive Job: 5
High Raw Job:      5
Normal Job:        30
Total Job:         40
```

관찰된 동작:

```text
Raw 0ms Worker
 → Cycle Count가 매우 빠르게 증가
 → Full-speed 실행

Adaptive 0ms Worker
 → Cycle Count가 낮게 유지
 → CPU 사용률 기반 Delay 적용

Normal 100ms Worker
 → 안정적인 저주기 Polling

전체 CPU 사용률
 → 샘플 실행 기준 약 25~30%대 유지
```

이 검증으로 `Core Assignment`, `Overflow Polling`, `0ms 고속 실행`, `Adaptive CPU Delay`, `WPF Monitoring`이 함께 동작함을 확인했습니다.

## 설계 원칙

Dreamine.Threading은 다음 원칙을 따릅니다.

- Thread 실행과 Job Scheduling 분리
- Core Allocation과 Worker 실행 분리
- Platform API와 Core 추상화 분리
- CPU 사용률 측정과 Thread 실행 분리
- Static ThreadManager 지양
- 생성자에서 Thread 시작 금지
- 테스트 가능하고 교체 가능한 구성 요소 유지
- WPF 및 Windows 전용 코드는 Core 패키지 밖에 위치
- FA 스타일 0ms 고속 루프를 허용하되 Adaptive CPU 보호 정책 지원

## 관련 패키지

```text
Dreamine.Threading
Dreamine.Threading.Windows
Dreamine.Threading.Wpf
Dreamine.Logging
```

## 상태

현재 이 패키지는 초기 구조 단계이며, 1차 런타임 검증을 완료했습니다.

구현됨:

- Worker Thread / Job 분리
- Auto Core Allocation
- Core당 Thread 수 제한
- Overflow Polling Job 배정
- 0ms 고속 Loop 지원
- Adaptive CPU Delay 정책
- Cycle Context 지원
- Logging 친화적인 Thread Manager 구조
- 별도 WPF 패키지를 통한 Monitor 연동

향후 계획:

- DMContainer 기반 간소화 등록 API
- Summary Metrics 추가
- Round-Robin Overflow Scheduler
- Job 단위 Monitoring 통계
- Core 0 제외 옵션
- Async Queue 기반 Dispatching
- Lifecycle Diagnostics 개선

## License

MIT License
