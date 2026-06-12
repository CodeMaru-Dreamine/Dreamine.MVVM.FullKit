# Dreamine FullKit — 코드 재평가 TODO v3

> 작성일: 2026-06-11  
> 기준: 전체 서브모듈 직접 코드 리뷰 (v2 체크리스트 완료 후 재평가)  
> 우선순위: 🔴 High · 🟡 Medium · 🟢 Low  
> 분류: [Thread] 스레드 안전 · [Async] 비동기 · [Design] 설계 · [Test] 테스트 · [Build] 빌드

---

## 🔴 High Priority

### [Thread] NEW-001 · AsyncRelayCommand._isExecuting 비동기 경쟁 조건

**파일**: `20_SOURCES/100. Library/ViewModels/AsyncRelayCommand.cs`  
**문제**: `_isExecuting`이 plain `bool`이다. `CanExecute()`에서 읽고 `Execute()`에서 쓰는 사이에
두 스레드가 동시 진입할 수 있다. `volatile`이 없으므로 CPU 레지스터 캐싱으로 인해
한 스레드의 쓰기가 다른 스레드에 보이지 않을 수 있다.

```csharp
// 현재 — 경쟁 조건 가능
private bool _isExecuting;

public bool CanExecute(object? parameter) =>
    !_isExecuting && (_canExecute?.Invoke() ?? true);   // 읽기
...
_isExecuting = true;   // 쓰기, 동기화 없음
```

**수정 방향**:
- `_isExecuting` → `private volatile bool` 또는
- `private int _isExecuting`으로 선언하고 `Interlocked.CompareExchange`로 0→1 전환 (더 강함)

---

### [Thread] NEW-002 · AsyncRelayCommand._lastException 비동기 가시성

**파일**: `20_SOURCES/100. Library/ViewModels/AsyncRelayCommand.cs`  
**문제**: `_lastException`이 plain `Exception?`이다. `Execute()`(async void 내부)에서 쓰고
외부 스레드에서 `LastException` 프로퍼티를 읽는 패턴에서 stale 읽기가 발생할 수 있다.

**수정 방향**: `private volatile Exception? _lastException;`

---

### [Async] NEW-003 · DreamineThread.Stop() sync-over-async 패턴

**파일**: `20_SOURCES/100. Library/Threading/Services/DreamineThread.cs:193`  
**문제**: `StopAsync().AsTask().GetAwaiter().GetResult()` — WPF UI 스레드에서 호출되면
내부 `await Task.Run(...)` 때문에 데드락은 발생하지 않지만, SynchronizationContext가 있는
환경(예: STA test runner)에서는 데드락이 재현된다. sync-over-async는 공개 API에 노출하기에
부적절하다.

**수정 방향**:
```csharp
// Stop()을 폐기하고 호출자에게 StopAsync()를 사용하도록 유도
[Obsolete("Use StopAsync() instead to avoid blocking the calling thread.")]
public void Stop() => StopAsync().AsTask().GetAwaiter().GetResult();
```
또는 `IDreamineThread` 인터페이스에서 `Stop()` 제거.

---

### [Test] NEW-004 · AsyncRelayCommand 동시 실행 차단 테스트 부재

**파일**: `20_SOURCES/200. Tests/Dreamine.FullKit.Tests/ViewModels/`  
**문제**: `_isExecuting` 재진입 방지 로직의 경쟁 조건은 단일 스레드 테스트로 검증 불가.
현재 테스트(`AsyncRelayCommand_PreventsReentryWhileExecuting`)는 `await` 경계에서만 확인하며
멀티스레드 동시 진입은 검사하지 않는다.

**추가할 테스트**:
```csharp
[Fact]
public async Task AsyncRelayCommand_PreventsConcurrentExecution()
{
    var concurrentCount = 0;
    var maxConcurrent = 0;
    var command = new AsyncRelayCommand(async () =>
    {
        var c = Interlocked.Increment(ref concurrentCount);
        Interlocked.Exchange(ref maxConcurrent, Math.Max(maxConcurrent, c));
        await Task.Delay(50);
        Interlocked.Decrement(ref concurrentCount);
    });

    var tasks = Enumerable.Range(0, 10)
        .Select(_ => Task.Run(() => command.Execute(null)));
    await Task.WhenAll(tasks);

    Assert.Equal(1, maxConcurrent);
}
```

---

### [Test] NEW-005 · ResilientMessageTransport 상태 머신 동시성 테스트 부재

**파일**: `20_SOURCES/200. Tests/`  
**문제**: Connecting → Connected → Faulted → Reconnecting 상태 전환이 동시 호출에서
올바르게 직렬화되는지 테스트하는 케이스가 없다. `NotifyStateChangedIfNeeded`의 lock 패턴은
단일 스레드 테스트로는 검증되지 않는다.

---

## 🟡 Medium Priority

### [Thread] NEW-006 · DreamineAppBuilder 정적 bool 플래그 경쟁 조건

**파일**: `20_SOURCES/100. Library/Wpf/DreamineAppBuilder.cs:20-21`  
**문제**:
```csharp
private static bool _globalAutoWireHandlerRegistered;    // volatile 없음
private static bool _autoNavigatorHandlerRegistered;     // volatile 없음
```
`Initialize()`가 동시에 두 스레드에서 호출되면 두 조건 모두 `false`를 읽고
`EventManager.RegisterClassHandler`가 이중 등록될 수 있다.

**수정 방향**:
```csharp
private static int _globalAutoWireHandlerRegistered;

// 사용 시
if (Interlocked.CompareExchange(ref _globalAutoWireHandlerRegistered, 1, 0) == 0)
{
    EventManager.RegisterClassHandler(...);
}
```

---

### [Thread] NEW-007 · HybridStateStore 구독/해제 이벤트 경쟁 조건

**파일**: `20_SOURCES/100. Library/Hybrid/State/HybridStateStore.cs:35,119`  
**문제**: `StateChanged += handler`와 `-= handler`가 lock 없이 실행된다.
`Subscribe()`와 `Dispose()` 동시 호출 시 이벤트 체인 일관성이 보장되지 않는다.
(C# 이벤트 add/remove는 자체적으로 스레드 안전하지만, `StateSubscription.Dispose()`의
`_handler = null` → `_store.StateChanged -= handler` 사이에 null 체크 후 재진입
가능성이 있다.)

**수정 방향**: `StateSubscription.Dispose()`에서 `Interlocked.Exchange`로 단일 해제 보장:
```csharp
public void Dispose()
{
    var handler = Interlocked.Exchange(ref _handler, null);
    if (handler is null) return;
    _store.StateChanged -= handler;
}
```

---

### [Thread] NEW-008 · InMemoryHybridMessageBus.Subscription._disposed volatile 누락

**파일**: `20_SOURCES/100. Library/Hybrid/Messaging/InMemoryHybridMessageBus.cs:209`  
**문제**: `_disposed`가 plain `bool`. `PublishAsync()`에서 `subscription.IsDisposed`를 읽는
스레드와 `Dispose()`를 쓰는 스레드가 다를 경우 stale 읽기가 발생할 수 있다.

**수정 방향**: `private volatile bool _disposed;`

---

### [Thread] NEW-009 · ResilientMessageTransport._disposed double-dispose 경쟁 조건

**파일**: `20_SOURCES/100. Library/Communication.Core/Resilience/ResilientMessageTransport.cs:263`  
**문제**:
```csharp
if (_disposed) return;    // 읽기
_disposed = true;         // 쓰기 — 두 스레드 동시 진입 가능
```
`_disposed`가 volatile도 아니고 Interlocked 보호도 없다.

**수정 방향**:
```csharp
private int _disposeGuard;

public async ValueTask DisposeAsync()
{
    if (Interlocked.CompareExchange(ref _disposeGuard, 1, 0) != 0) return;
    _disposed = true;
    ...
}
```

---

### [Design] NEW-010 · TransportMessageBusAdapter 예외 무음 처리

**파일**: `20_SOURCES/100. Library/Communication.Core/Adapters/TransportMessageBusAdapter.cs:128`  
**문제**: `OnMessageReceived`의 catch 블록이 모든 예외를 무음으로 버린다.
`InMemoryHybridMessageBus`는 `IHybridMessageBusExceptionHandler`로 외부에 보고하는 반면,
`TransportMessageBusAdapter`는 이에 상응하는 실패 관찰 수단이 없다.

```csharp
catch
{
    // 완전 무음 — 디버깅 불가
}
```

**수정 방향**: `Action<Exception>? OnHandlerError` 프로퍼티 또는 `IHybridMessageBusExceptionHandler` 패턴 적용.

---

### [Design] NEW-011 · ViewManager View 미발견 무음 처리

**파일**: `20_SOURCES/100. Library/Wpf/ViewManager.cs:49`  
**문제**: `ViewModelLocator.ResolveView()`가 null을 반환할 때 메서드가 조용히 리턴한다.
개발자가 ViewModel→View 매핑을 누락했을 때 어떤 진단도 남지 않아 디버깅이 어렵다.

**수정 방향**:
```csharp
if (view is null)
{
    Debug.WriteLine(
        $"[ViewManager] No view registered for {viewModelType.FullName}. " +
        "Register it via ViewModelLocator.Register<TView, TViewModel>().");
    return;
}
```

---

### [Design] NEW-012 · DreamineContainer Reset() 시 IDisposable 싱글톤 미해제

**파일**: `20_SOURCES/100. Library/Core/DMContainer.cs`, `DreamineContainer.cs`  
**문제**: `DMContainer.Reset()`이 내부 컨테이너를 교체할 때 기존 싱글톤 인스턴스들 중
`IDisposable`/`IAsyncDisposable`을 구현한 것들이 해제되지 않는다.
테스트에서 `Reset()`을 반복 호출하면 리소스가 누수된다.

**수정 방향**: `DreamineContainer`가 `IDisposable`을 구현하고, `Dispose()`에서
`_singletonInstances.Values.OfType<IDisposable>().ForEach(d => d.Dispose())`.
`DMContainer.Reset()` 전에 이전 컨테이너를 Dispose.

---

### [Design] NEW-013 · ViewManager.TryResolve 예외 흐름 제어 사용

**파일**: `20_SOURCES/100. Library/Wpf/ViewManager.cs:176`  
**문제**: 미등록 타입 해결 시 `InvalidOperationException`을 throw 후 catch하는 방식으로
정상 경로를 처리한다. 예외 생성 비용(스택트레이스 포함)이 발생하며, 미등록 타입이 자주
조회되면 성능 문제가 된다.

**수정 방향**: `DreamineContainer`에 `TryResolve<T>(out T? result)` 메서드 추가.

---

### [Test] NEW-014 · HybridStateStore 동시 SetState 테스트 부재

**파일**: `20_SOURCES/200. Tests/`  
**문제**: 여러 스레드가 동시에 `SetState()`를 호출할 때 `StateChanged` 이벤트가
누락 없이 모두 발생하는지 검증하는 테스트가 없다.

---

### [Test] NEW-015 · DreamineContainer.Reset() 싱글톤 해제 테스트 부재

`Reset()` 후 이전 싱글톤의 `Dispose()`가 호출되었는지 확인하는 단위 테스트 추가.

---

## 🟢 Low Priority

### [Thread] NEW-016 · WindowStateService 이벤트 발생 원자성

**파일**: `20_SOURCES/100. Library/Wpf/WindowStateService.cs:41`  
**문제**: `_states[windowKey] = isOpen`(ConcurrentDictionary 쓰기)와
`StateChanged?.Invoke(...)` 사이가 원자적이지 않다. 두 스레드가 동시에 `MarkOpened`를
호출하면 이벤트가 순서 없이 발생할 수 있다.  
**영향**: 낮음 — WPF는 UI 스레드 단일 접근이 일반적.  
**수정 방향**: 필요 시 `lock` 블록으로 감싸거나, 문서에 "UI 스레드 전용" 명시.

---

### [Design] NEW-017 · ViewManager 폴백 Window 하드코딩 크기

**파일**: `20_SOURCES/100. Library/Wpf/ViewManager.cs:103,122`  
**문제**: UserControl/Page가 전용 Navigator 없이 표시될 때 800×600 Window가 고정 생성된다.
해상도나 콘텐츠 특성을 무시한다.

**수정 방향**: `DreamineWpfOptions`에 `FallbackWindowWidth`/`FallbackWindowHeight` 옵션 추가,
또는 UserControl의 `DesiredSize`를 참고하도록.

---

### [Build] NEW-018 · DreamineCommandSourceGenerator RS2000 경고

**파일**: `20_SOURCES/100. Library/Generators/`  
**문제**: DMCMD005 DiagnosticDescriptor가 `AnalyzerReleases.Unshipped.md`에 등록되지 않아
빌드 시 RS2000 경고가 발생한다.

**수정 방향**: `20_SOURCES/100. Library/Generators/AnalyzerReleases.Unshipped.md`에 추가:
```
## Unreleased

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
DMCMD005 | DreamineCommand | Warning | CanExecute method not found
```

---

### [Design] NEW-019 · InMemoryHybridMessageBus.ExceptionHandler 변경 가능 경쟁 조건

**파일**: `20_SOURCES/100. Library/Hybrid/Messaging/InMemoryHybridMessageBus.cs:23`  
**문제**: `ExceptionHandler`가 mutable public setter인데, `PublishAsync` 실행 중에
다른 스레드가 핸들러를 교체하면 `NullReferenceException`은 없지만 일관성 없는 핸들러가 호출된다.

**수정 방향**: 생성자 주입으로 변경하거나, setter에서 `ArgumentNullException.ThrowIfNull` 추가
+ `Volatile.Write`/`Read` 적용.

---

### [Design] NEW-020 · TransportMessageBusAdapter IDisposable(동기) 미구현

**파일**: `20_SOURCES/100. Library/Communication.Core/Adapters/TransportMessageBusAdapter.cs`  
**문제**: `IAsyncDisposable`만 구현. `using var adapter = new TransportMessageBusAdapter(...)`
처럼 동기 using을 쓰면 `MessageReceived` 구독이 해제되지 않는다.

**수정 방향**: `IDisposable`도 구현하거나 클래스 XML docs에 `await using` 강제 사용을 명시.

---

### [Design] NEW-021 · ViewModelBase propertyName = null! 기본값 문서화 부족

**파일**: `20_SOURCES/100. Library/ViewModels/ViewModelBase.cs:22`  
**문제**: `[CallerMemberName] string propertyName = null!`의 `null!`이 null을 의도한 것이
아니라 `CallerMemberName`이 항상 채워준다는 계약에 의존함을 알기 어렵다.

**수정 방향**: XML 주석 또는 인라인 주석으로 의도 명시:
```csharp
// CallerMemberName은 항상 호출 위치의 멤버 이름을 주입하므로
// 런타임에서 null이 전달되는 경우는 없습니다.
protected void OnPropertyChanged([CallerMemberName] string propertyName = null!) { ... }
```

---

### [Test] NEW-022 · DatabaseProviderBase SQL 캐시 적중 테스트 부재

**파일**: `20_SOURCES/200. Tests/`  
**문제**: `GetOrBuildSql<T>` (v2에서 추가한 SQL 캐시)가 실제로 같은 SQL 인스턴스를 재사용하는지
검증하는 테스트가 없다.

---

### [Test] NEW-023 · DreamineAutoRegistrar.RegisterAll(Assembly) 오버로드 테스트

**파일**: `20_SOURCES/100. Library/Core/DreamineAutoRegistrar.cs`  
**문제**: v2에서 추가한 파라미터 없는 `RegisterAll(Assembly)` 오버로드가
실제로 DMContainer에 등록하는지 확인하는 테스트 없음.

---

## 참고: 재평가 결과 이미 양호한 부분

| 컴포넌트 | 확인 결과 |
|---|---|
| `InMemoryHybridMessageBus.PublishAsync` | 구독자 실패 격리, 스냅샷 순회, OperationCanceled 분리 — 양호 |
| `HybridStateStore.SetState/Update` | lock 내부 스냅샷 캡처 후 외부 이벤트 발생 — 양호 |
| `PlcClientBase.DisposeAsync` | `Interlocked.CompareExchange` + `volatile _disposed` — 양호 |
| `DreamineContainer.Resolve` | `AsyncLocal` lock 외부 설정 — 양호 |
| `ResilientMessageTransport.NotifyStateChangedIfNeeded` | lock 내 비교/assign, lock 외 이벤트 발화 — 양호 |
| `RelayCommand.Execute` | `CanExecute` 선 체크 후 실행 — 양호 (v2에서 수정) |
| `TransportMessageBusAdapter.SubscribeAsync` | `lock(handlers)` 스냅샷 패턴 — 양호 |
| `WindowStateService` | `ConcurrentDictionary` 사용 — 단일 스레드 WPF 용도로 적합 |

---

## 작업 우선순위 요약

```
🔴 즉시 처리 (안전성/정확성)
  NEW-001  AsyncRelayCommand._isExecuting volatile/Interlocked
  NEW-002  AsyncRelayCommand._lastException volatile
  NEW-003  DreamineThread.Stop() Obsolete 표시 또는 제거
  NEW-004  AsyncRelayCommand 동시 실행 테스트
  NEW-005  ResilientMessageTransport 상태 머신 동시성 테스트

🟡 단기 처리 (품질/견고성)
  NEW-006  DreamineAppBuilder 정적 플래그 Interlocked
  NEW-007  HybridStateStore Dispose Interlocked.Exchange
  NEW-008  Subscription._disposed volatile
  NEW-009  ResilientMessageTransport DisposeAsync Interlocked
  NEW-010  TransportMessageBusAdapter 예외 보고 수단 추가
  NEW-011  ViewManager View 미발견 Debug.WriteLine
  NEW-012  DreamineContainer Reset() IDisposable 처리
  NEW-013  ViewManager TryResolve 예외 흐름 제거
  NEW-014  HybridStateStore 동시 SetState 테스트
  NEW-015  DreamineContainer Reset() 해제 테스트

🟢 장기/선택 처리
  NEW-016  WindowStateService 원자성 문서화
  NEW-017  ViewManager 폴백 Window 크기 옵션화
  NEW-018  RS2000 경고 AnalyzerReleases 등록
  NEW-019  ExceptionHandler Volatile.Read/Write
  NEW-020  TransportMessageBusAdapter IDisposable 추가
  NEW-021  ViewModelBase null! 주석
  NEW-022  SQL 캐시 테스트
  NEW-023  RegisterAll 오버로드 테스트
```

---

*총 23개 항목 | 🔴 5 · 🟡 10 · 🟢 8*
