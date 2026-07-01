# Dreamine.Threading.Windows

**Dreamine.Threading.Windows**는 Dreamine.Threading을 위한 Windows 전용 스레딩 서비스 패키지입니다.

이 패키지는 CPU Affinity, Timer Resolution, Windows CPU 정보, 프로세스 CPU 사용률 측정 같은 플랫폼 의존 기능을 구현합니다.  
`Dreamine.Threading`을 참조하며, Windows 데스크톱 애플리케이션에서 사용할 실제 구현체를 제공합니다.

[➡️ English README](./README.md)

## 목적

`Dreamine.Threading` Core 패키지는 추상화와 스케줄링 정책을 정의합니다.  
이 패키지는 그 추상화에 대한 Windows 구현을 제공합니다.

```text
Dreamine.Threading
 ├─ IThreadAffinityService
 ├─ ITimerResolutionService
 ├─ ICpuUsageProvider
 └─ CPU/Core 관련 추상화

Dreamine.Threading.Windows
 ├─ WindowsThreadAffinityService
 ├─ WindowsTimerResolutionService
 ├─ WindowsCpuInfoProvider
 └─ WindowsProcessCpuUsageProvider
```

## 책임

이 패키지의 책임:

- 현재 Windows Thread에 CPU Affinity 적용
- 지원되는 환경에서 Timer Resolution 제어
- Windows CPU 정보 제공
- 현재 프로세스 CPU 사용률 측정
- Windows 전용 등록 헬퍼 제공
- P/Invoke 코드를 Core Threading 패키지 밖으로 분리

## 패키지 구조

```text
Dreamine.Threading.Windows
├─ Native
│  ├─ Kernel32NativeMethods.cs
│  └─ WinMmNativeMethods.cs
│
├─ Services
│  ├─ WindowsCpuInfoProvider.cs
│  ├─ WindowsProcessCpuUsageProvider.cs
│  ├─ WindowsThreadAffinityService.cs
│  └─ WindowsTimerResolutionService.cs
│
└─ Registration
   └─ DreamineThreadingWindowsRegistration.cs
```

## 서비스

### WindowsThreadAffinityService

구현 인터페이스:

```text
IThreadAffinityService
```

Windows Native API를 사용해 현재 Thread에 CPU Affinity를 적용합니다.

```text
CoreIndex = 2
 → 현재 Thread의 Affinity Mask를 CPU Core 2로 설정
```

### WindowsTimerResolutionService

구현 인터페이스:

```text
ITimerResolutionService
```

`timeBeginPeriod`, `timeEndPeriod`를 사용해 Timer Resolution을 제어합니다.

고정밀 Polling이 필요한 경우 사용할 수 있지만, Timer Resolution은 시스템 전체에 영향을 줄 수 있으므로 신중하게 사용해야 합니다.

### WindowsCpuInfoProvider

Windows CPU 정보를 제공합니다.

```text
Environment.ProcessorCount
```

### WindowsProcessCpuUsageProvider

구현 인터페이스:

```text
ICpuUsageProvider
```

현재 프로세스 CPU 사용률을 다음 정보를 기준으로 계산합니다.

```text
Process.GetCurrentProcess().TotalProcessorTime
경과 시간
Environment.ProcessorCount
```

이 Provider는 `AdaptiveCpuCyclePolicy`에서 CPU 사용률이 높아질 때 동적 Delay를 적용하기 위해 사용됩니다.

## Adaptive CPU Delay 연동

`WindowsProcessCpuUsageProvider`를 사용하면 0ms Worker도 CPU 사용률 기준으로 제어할 수 있습니다.

예시 동작:

```text
CPU >= 70% -> 5ms Delay
CPU >= 50% -> 3ms Delay
CPU >= 30% -> 1ms Delay
CPU <  30% -> 0ms Delay
```

이 방식은 `IntervalMs = 0`이 필요한 FA 스타일 모니터링 루프에서 CPU 폭주를 막는 데 유용합니다.

## 등록

현재 등록 방식:

```csharp
using Dreamine.Threading.Windows.Registration;

DreamineThreadingWindowsRegistration.Register();
```

이 호출은 다음 Windows 전용 서비스를 등록합니다.

```text
IThreadAffinityService  -> WindowsThreadAffinityService
ITimerResolutionService -> WindowsTimerResolutionService
ICpuUsageProvider       -> WindowsProcessCpuUsageProvider
WindowsCpuInfoProvider
```

> 이 패키지는 다른 어셈블리에서 `DMContainer`를 partial class로 확장하지 않습니다. 등록은 별도의 Registration 클래스를 통해 제공합니다.

## 설계 메모

Windows 전용 API는 `Dreamine.Threading` Core 안에 들어가면 안 됩니다.

이 패키지는 다음 의존성 방향을 지키기 위해 존재합니다.

```text
Dreamine.Threading
        ↑
Dreamine.Threading.Windows
```

Core 패키지는 플랫폼 독립성을 유지하고, 이 패키지는 Windows 전용 동작을 담당합니다.

## 검증 시나리오

다음 구성으로 검증했습니다.

```text
High Adaptive Job: 5
High Raw Job:      5
Normal Job:        30
Total Job:         40
```

관찰된 동작:

```text
전용 Worker에 CPU Affinity 적용
Adaptive Worker는 프로세스 CPU 사용률을 기준으로 Cycle 속도 감소
Raw 0ms Worker는 Full-speed로 동작
Normal Worker는 100ms Polling 유지
전체 CPU 사용률은 샘플 실행 기준 약 25~30%대로 안정적 유지
```

## 관련 패키지

```text
Dreamine.Threading
Dreamine.Threading.Windows
Dreamine.Threading.Wpf
```

## 상태

구현됨:

- Windows CPU Affinity Service
- Windows Timer Resolution Service
- Windows CPU 정보 Provider
- Windows Process CPU Usage Provider
- Windows Registration Helper

향후 계획:

- Restore Token 기반 Affinity 복원
- Timer Resolution Period 옵션화
- 더 풍부한 CPU 사용률 지표
- Core 0 제외 정책 옵션
- Windows 전용 동작 통합 테스트

## License

MIT License
