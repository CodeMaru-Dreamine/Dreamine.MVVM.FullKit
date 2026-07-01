# Dreamine.Logging

Dreamine.Logging은 Dreamine 애플리케이션을 위한 핵심 로그 인프라 패키지입니다.
Logger 추상화, 구조화된 로그 엔트리, 메모리 진단 저장소, 텍스트 포매터, 복합 Sink, 일자별 텍스트 파일 출력, 그리고 고빈도 멀티스레드 로그 처리를 위한 비동기 큐 Sink를 제공합니다.

[➡️ English Version](./README.md)

## 목적

이 패키지는 Dreamine 기반 애플리케이션에서 사용하는 UI 비의존 로그 Core 계층입니다.
로그 생성과 로그 출력을 분리하여, 호출 쓰레드가 파일 I/O나 UI 갱신 비용에 직접 묶이지 않도록 설계되었습니다.

`Dreamine.Logging`은 WPF 또는 특정 UI 프레임워크를 참조하면 안 됩니다.
WPF 전용 로그 패널은 `Dreamine.Logging.Wpf` 패키지가 담당합니다.

## 주요 기능

- `IDreamineLogger` 추상화
- 기본 Logger 구현체 `DreamineLogger`
- `Trace`, `Debug`, `Info`, `Warning`, `Error`, `Fatal` 로그 레벨
- 구조화된 로그 모델 `DreamineLogEntry`
- 출력 대상 확장을 위한 `IDreamineLogSink`
- 여러 Sink에 동시에 기록하는 `CompositeLogSink`
- 제한된 백그라운드 큐 기반 비차단 로그 처리를 위한 `AsyncQueueSink`
- 런타임 진단 및 UI 연동을 위한 Bounded Ring Buffer 저장소 `InMemoryLogStore` (기본 capacity 1000, O(1) 용량 관리)
- 일반 텍스트 변환을 위한 `DreamineTextLogFormatter`
- 버퍼링 및 제어된 Flush를 지원하는 일자별 파일 Sink `TextFileLogSink`
- `AsyncQueueSink.ShutdownAsync(...)` 기반 종료 시 Flush 처리

## 권장 구조

기본 등록 API는 Sink 체인을 구성하고 종료 시 사용할 비동기 dispose handle을 반환합니다.
이 구조는 작업 쓰레드가 파일 I/O 또는 UI 표시용 저장소 갱신 때문에 직접 지연되는 것을 막습니다.

```text
Application threads
  -> IDreamineLogger
     -> DreamineLogger
        -> AsyncQueueSink
           -> CompositeLogSink
              ├─► InMemoryLogStore   (LogAdded 이벤트 발행)
              └─► TextFileLogSink
```

`DreamineLogPanelViewModel`은 이 체인의 일부가 아닙니다.
ViewModel은 `InMemoryLogStore.LogAdded` 이벤트를 구독하는 별도 Observer 입니다.
WPF 표시 계층 흐름은 `Dreamine.Logging.Wpf` 문서를 참고하십시오.

## AsyncQueueSink가 필요한 이유

로그 시스템은 쓰레드 루프, 백그라운드 Worker, 통신 처리기, Motion/IO Polling Job, UI Command 등 다양한 위치에서 호출됩니다.
로그 호출마다 파일에 직접 쓰거나 UI 표시용 저장소를 즉시 갱신하면 호출 쓰레드가 지연될 수 있습니다.
로그가 지속적으로 발생하면 Dispatcher 작업 또는 표시용 로그 컬렉션이 과도하게 누적될 수도 있습니다.

`AsyncQueueSink`는 이 중 호출 쓰레드 지연 문제를 해결합니다.

- 생산자는 로그 엔트리를 빠르게 큐에 넣습니다.
- 하나의 백그라운드 Worker가 큐를 소비합니다.
- 큐는 capacity로 상한을 가집니다.
- 큐가 가득 차면 호출자를 블락하지 않고 가장 오래된 대기 로그를 드롭합니다 (`BoundedChannelFullMode.DropOldest`).
- 드롭된 누적 개수는 `AsyncQueueSink.DroppedCount` 프로퍼티로 모니터링할 수 있습니다.
- 종료 시 대기 로그를 최대한 비우고 종료할 수 있습니다.

WPF 표시 컬렉션의 상한 및 UI Dispatcher 배치 처리는 `Dreamine.Logging.Wpf`가 담당합니다.

## 등록 예시

아래 등록 방식은 Logger는 단순하게 유지하고, 비동기 처리는 Sink 체인에 위임하는 구조입니다.

```csharp
private static IAsyncDisposable? _loggingShutdown;

private static void RegisterLogging()
{
    _loggingShutdown = DreamineLoggingRegistration.Register(new DreamineLoggingOptions
    {
        Category = "SampleSmart",
        LogDirectory = Path.Combine(AppContext.BaseDirectory, "Logs")
    });
}
```

## 종료 처리

비동기 등록 handle을 사용할 경우 애플리케이션 종료 시 dispose 하여 큐를 비우는 처리가 필요합니다.
최근 로그 손실을 줄이고, 내부 Sink 체인과 파일 핸들을 정상적으로 정리하기 위한 목적입니다.

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

## 로그 파일 출력

`TextFileLogSink`를 사용하면 로그는 날짜별 파일로 저장됩니다.

```text
Logs/yyyy-MM-dd.log
```

예시:

```text
[2026-05-09 18:30:22.718] [Info] [SampleSmart] [T1] Application initialized.
```

`TextFileLogSink`는 매 로그마다 파일을 열고 닫지 않고 현재 날짜의 파일을 열어둔 상태로 기록합니다.
설정된 쓰기 횟수마다 Flush하며, `Error` 이상 로그는 즉시 Flush합니다.

## 스레드 안전성

`DreamineLogger`, `AsyncQueueSink`, `CompositeLogSink`, `InMemoryLogStore`, `TextFileLogSink`는
애플리케이션 단위 단일 인스턴스로 공유 사용함을 전제로 합니다.
권장 등록 방식은 애플리케이션당 Logger 1개, Async Sink 1개, Sink 체인 1개입니다.

`TextFileLogSink`와 `InMemoryLogStore`는 내부 lock으로 보호되므로 `AsyncQueueSink` 없이 직접
멀티스레드에서 호출해도 thread-safe합니다. 다만 호출 쓰레드 블로킹을 피하려면 큐를 통해
사용하는 것이 좋습니다.

고빈도 루프에서는 운영 코드에서 매 Cycle 로그를 남기지 않는 것이 좋습니다.
권장 로그 지점은 다음과 같습니다.

- 상태 변경 시점
- Warning 또는 Error 발생 시점
- 작업 시작/종료 시점
- 진단 모드
- 주기 제한된 로그 출력

## 주의 사항

- `IDreamineLogger`는 `AsyncQueueSink`가 포함된 Sink 체인을 사용하는 `DreamineLogger`로 등록합니다.
- 외부 Consumer가 `IDreamineLogSink`를 직접 Resolve할 수 있다면 `AsyncQueueSink`를 등록하는 편이 안전합니다.
- Logger의 sink 목록과 `CompositeLogSink`의 children에 같은 Sink 인스턴스를 중복 추가하지 마십시오. 동일 로그가 두 번 기록됩니다.
- 이 패키지에는 WPF UI 컴포넌트를 넣지 않습니다.
- 화면 표시용 로그 패널은 `Dreamine.Logging.Wpf`를 사용합니다.

## Dreamine.Logging.Wpf와의 관계

```text
Dreamine.Logging.Wpf
  -> Dreamine.Logging
```

의존성 방향은 반드시 단방향으로 유지합니다.
`Dreamine.Logging`은 Core 계층이고, `Dreamine.Logging.Wpf`는 표시 계층 Adapter입니다.

## 향후 계획

- `DreamineLoggerOptions` (capacity, batch size, file directory 등 옵션을 한 객체로 통합)
- `DMContainer.UseDreamineLogging(...)` (위 등록 보일러플레이트를 한 줄로 줄이는 확장 메서드)
- `DreamineLogger`에 단일 Sink + Level + Category 편의 생성자 추가
- 로그 파일 롤링 정책 (일자 외에도 크기/개수 기준)
- Database Sink
- 외부 로그 서비스 Sink
- `AsyncQueueSink`의 Drop 로그 보고 Hook (이벤트 기반 알림)
- 선택적 로그 Throttling Helper

## 라이선스

MIT License
