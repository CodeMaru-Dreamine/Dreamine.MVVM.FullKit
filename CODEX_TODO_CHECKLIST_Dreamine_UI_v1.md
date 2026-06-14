# Dreamine.UI.* — 코드 재평가 TODO v1

> 작성일: 2026-06-12  
> 기준: UI.Abstractions / UI.WinForms / UI.Wpf / UI.Wpf.Controls / UI.Wpf.Equipment / UI.Wpf.Themes 직접 코드 리뷰  
> 우선순위: 🔴 High · 🟡 Medium · 🟢 Low  
> 분류: [Thread] 스레드 안전 · [Async] 비동기 · [Design] 설계 · [Memory] 메모리 누수 · [Build] 빌드

---

## 🔴 High Priority

### [Async] UI-001 · async void DisableButtonsAndEnableLater

**파일**: `UI.Wpf.Controls/MessageBox/DreamineMessageBoxWindow.xaml.cs:50`

```csharp
// 현재 — 예외 전파 불가
private async void DisableButtonsAndEnableLater(int delaySeconds)
```

**문제**: `async void`는 예외를 호출자에게 전파하지 못함.
`Task.Delay` 중 창이 닫히면 `btn.Content` 접근 시 `InvalidOperationException`이 발생해도 앱 비정상 종료.
`DisableButtonsAndEnableLater`와 `StartAutoClick`이 동시에 실행될 경우 버튼 텍스트 경쟁 조건 발생.

**수정 방향**:
```csharp
private async Task DisableButtonsAndEnableLaterAsync(int delaySeconds, CancellationToken ct = default)
{
    try { await Task.Delay(TimeSpan.FromSeconds(delaySeconds), ct); }
    catch (OperationCanceledException) { return; }
    // ...
}
// 생성자에서: _ = DisableButtonsAndEnableLaterAsync(..., _cts.Token);
```

---

### [Async] UI-002 · async void StartAutoClick + 이중 Close 위험

**파일**: `UI.Wpf.Controls/MessageBox/DreamineMessageBoxWindow.xaml.cs:83`

```csharp
private async void StartAutoClick(MessageBoxResult target, int delaySeconds)
```

**문제**: UI-001과 동일한 `async void` 문제.
추가로 창이 이미 닫힌 후에도 `BtnOk_Click(null!, null!)`으로 `Close()`가 재호출됨.

**수정 방향**: `CancellationTokenSource`를 필드로 유지, `OnClosed`에서 `Cancel()`. `async void` → `async Task`.

---

### [Async] UI-003 · async void 키보드 KeyUp 핸들러

**파일**: `UI.Wpf.Equipment/VirtualKeyboard/DreamineVirtualKeyboardWindow.xaml.cs:70`

```csharp
private async void DreamineVirtualKeyboardWindow_KeyUp(object sender, KeyEventArgs e)
```

**문제**: `InvokeProvdersAsync()` 중 예외 발생 시 완전 소멸.
`await Task.Delay(100)` 후 창이 닫힌 상태에서 `Hide()` 호출 시 예외 가능.

**수정 방향**:
```csharp
private async void DreamineVirtualKeyboardWindow_KeyUp(object sender, KeyEventArgs e)
{
    try { await HandleKeyUpAsync(e); }
    catch (Exception ex) { Debug.WriteLine($"[VK] KeyUp error: {ex}"); }
}
private async Task HandleKeyUpAsync(KeyEventArgs e) { ... }
```

---

### [Thread] UI-004 · _hookRunning / _hookSubscribed volatile 누락

**파일**: `UI.Wpf.Equipment/VirtualKeyboard/DreamineVirtualKeyboard.cs:32~34`

```csharp
// 현재 — volatile 없음
private bool _hookRunning;
private bool _hookSubscribed;
```

**문제**: `_hookRunning`은 UI 스레드에서 쓰고, `Task.Run` 내부 SharpHook 스레드에서 `finally { _hookRunning = false; }`로 씀.
`volatile` 없이는 변경이 UI 스레드에 보이지 않아 후크 중복 시작 가능.

**수정 방향**:
```csharp
private volatile bool _hookRunning;
private volatile bool _hookSubscribed;
```

---

### [Memory] UI-005 · DreamineCheckLed Tick 이벤트 중복 구독

**파일**: `UI.WinForms/Controls/DreamineCheckLed.cs`

```csharp
// 현재 — IsPulse: false→true 전환 시 중복 구독
private void StartPulse()
{
    _pulseTimer ??= new System.Windows.Forms.Timer { Interval = 40 };
    _pulseTimer.Tick += OnPulseTick;  // 매번 추가됨
    _pulseTimer.Start();
}
```

**문제**: `IsPulse`가 true → false → true 전환 시 `StartPulse`가 재호출되어
`OnPulseTick`이 중복 등록됨 → 두 배 속도 애니메이션, 시각 버그.

**수정 방향**:
```csharp
private void StartPulse()
{
    if (_pulseTimer == null)
    {
        _pulseTimer = new System.Windows.Forms.Timer { Interval = 40 };
        _pulseTimer.Tick += OnPulseTick;  // 최초 1회만
    }
    _pulseTimer.Start();
}
```

---

### [Memory] UI-006 · PopupWindowManager ContentRendered 이벤트 미해제

**파일**: `UI.Wpf.Controls/Navigation/PopupWindowManager.cs:154~184`

```csharp
owner.ContentRendered += OnOwnerReady;
// OnOwnerReady 내부에서만 -= 처리 — 팝업이 먼저 닫히면 미해제
```

**문제**: 팝업이 `e.Cancel = true`(Hide 재사용) 없이 닫히지 않으면 `OnOwnerReady`가 영구 미해제.
`owner`가 살아있는 한 `popup` 람다 캡처가 메모리에 남아 누수.

**수정 방향**: `popup.Closed`에서도 `owner.ContentRendered -= OnOwnerReady` 보장:
```csharp
popup.Closed += (_, _) => owner.ContentRendered -= OnOwnerReady;
```

---

### [Design] UI-007 · SharpHook 예외 완전 무음 처리

**파일**: `UI.Wpf.Equipment/VirtualKeyboard/DreamineVirtualKeyboard.cs:207~216, 722~725`

```csharp
catch { /* swallow on shutdown */ }
catch { /* 완전 빈 catch */ }
```

**문제**: SharpHook `RunAsync()` 실패 시 `_hookRunning = false`(finally)는 실행되지만
실패 원인을 전혀 알 수 없어 디버깅 불가.

**수정 방향**:
```csharp
catch (Exception ex)
{
    Debug.WriteLine($"[VK Hook] RunAsync failed: {ex}");
}
```

---

### [Design] UI-008 · PopupWindowManager Owner 설정 예외 무음

**파일**: `UI.Wpf.Controls/Navigation/PopupWindowManager.cs:126~130`

```csharp
try { popup.Owner = owner; }
catch { }  // 주석도 없음
```

**문제**: Owner 설정 실패 원인(다른 스레드 소유, 이미 닫힘 등)이 완전히 가려짐.

**수정 방향**:
```csharp
catch (Exception ex)
{
    Debug.WriteLine($"[Popup] Owner 설정 실패({name}): {ex.Message}");
}
```

---

### [Design] UI-009 · RequestCloseNextTick 재시도 예외 삼킴

**파일**: `UI.Wpf.Equipment/Popup/DreamineBlinkPopupWindow.xaml.cs:226~239`

```csharp
try { Close(); }
catch
{
    Dispatcher.BeginInvoke(...);  // 재시도에서 예외 발생 시 완전 삼킴
}
```

**수정 방향**: 재시도 BeginInvoke 내부에도 try/catch(Exception ex) + `Debug.WriteLine` 추가.

---

### [Design] UI-010 · ViewLoader 어셈블리 탐색 예외 무음

**파일**: `UI.Wpf.Controls/Navigation/ViewLoader.cs:509~511`

```csharp
try { res = selector(a); }
catch { /* skip */ }
```

**문제**: 어떤 어셈블리에서 어떤 오류가 났는지 전혀 파악 불가.

**수정 방향**:
```csharp
catch (Exception ex)
{
    Debug.WriteLine($"[ViewLoader] Assembly scan 실패 ({a.FullName}): {ex.Message}");
}
```

---

### [Design] UI-011 · null! 강제 캐스팅으로 NPE 은폐

**파일**: `UI.Wpf.Controls/Navigation/ViewLoader.cs:30,34` / `ViewSwitcher.cs:51` / `DreamineVirtualKeyboardAssist.cs:495`

```csharp
public UserControl View { get; set; } = null!;
public FrameworkElement FrameworkView { get; set; } = null!;
private static DreamineVirtualKeyboardWindow _virtualKeyboardWindow = null!;
```

**문제**: 실제로 null이지만 nullable 경고가 억제되어 소비 코드에서 NPE 발생 가능.

**수정 방향**: 프로퍼티를 nullable로 선언하고 호출 측에서 null 체크:
```csharp
public UserControl? View { get; set; }
public FrameworkElement? FrameworkView { get; set; }
private static DreamineVirtualKeyboardWindow? _virtualKeyboardWindow;
```

---

### [Memory] UI-012 · ViewModelKeyCache static Dictionary 무제한 증가

**파일**: `UI.Wpf.Controls/Navigation/ViewModelKeyCache.cs:24`

```csharp
public static Dictionary<string, int> IndexMap { get; set; } = new();
```

**문제**: 인덱스 값이 무한 증가. 장시간 운영 산업용 앱에서 int 오버플로 가능성.
static이어서 테스트 간 상태 격리 불가.

**수정 방향**: 인덱스 생성에 `checked` 연산 또는 `long` 사용. 테스트 후 `Reset()` 지원.

---

### [Memory] UI-013 · DreamineNavigationBar static HashSet GC 루트 누수

**파일**: `UI.Wpf.Controls/Navigation/DreamineNavigationBar.xaml.cs:102, 284`

```csharp
private static readonly HashSet<DreamineNavigationBar> _instances = new();
...
_instances.Add(this);
Unloaded += (_, _) => _instances.Remove(this);  // Unloaded만 처리
```

**문제**: Window 강제 종료 또는 Unloaded 미발생 시 인스턴스가 static HashSet에 영구 잔류 → GC 루트가 되어 메모리 누수.

**수정 방향**: `WeakReference<DreamineNavigationBar>` 사용 또는 `Window.Closed`에도 Remove 연결:
```csharp
var parentWindow = Window.GetWindow(this);
if (parentWindow != null)
    parentWindow.Closed += (_, _) => _instances.Remove(this);
```

---

## 🟡 Medium Priority

### [Thread] UI-014 · _isInputHooked static bool 명시적 안전성 부재

**파일**: `UI.Wpf.Controls/Navigation/DreamineNavigationBar.xaml.cs:108`

```csharp
private static bool _isInputHooked = false;
```

**문제**: WPF UI 스레드 단일 사용이라 실제 위험은 낮지만 `volatile` 없이 static bool 패턴 사용으로 명시성 부족.

**수정 방향**: `private static volatile bool _isInputHooked;` 또는 주석으로 "UI 스레드 전용" 명시.

---

### [Design] UI-015 · DreaminePopupService List 스레드 안전성 미흡

**파일**: `UI.Wpf.Equipment/Popup/DreaminePopupService.cs:12~13`

```csharp
private readonly List<Window> _opened = new();
private readonly Dictionary<Window, BlinkPopupOptions> _optionsByWindow = new();
```

**문제**: `ShowBlinkAsync`는 Dispatcher를 통해 Add하지만 `CloseAll()`, `Close()` 등은 직접 호출 가능.
`Closed` 이벤트 람다에서도 `_opened.Remove(win)` 발생 → 서로 다른 진입점에서 동시 접근 가능.

**수정 방향**: 모든 조작을 UI Dispatcher에서만 수행하거나 주석으로 "UI 스레드 전용" 명시.

---

### [Design] UI-016 · PopupWindowManager.Instance set 접근자 공개

**파일**: `UI.Wpf.Controls/Navigation/PopupWindowManager.cs:22`

```csharp
public static PopupWindowManager Instance { get; set; } = new();  // set 공개
```

**문제**: 외부에서 Instance 교체 가능 → `DreamineTabHost` 등 참조처와 일관성 붕괴.

**수정 방향**:
```csharp
public static PopupWindowManager Instance { get; } = new();
```

---

### [Design] UI-017 · SetPopupSize — 오타 + Instance 자기참조

**파일**: `UI.Wpf.Controls/Navigation/PopupWindowManager.cs:25~28`

```csharp
public void SetPopupSize(string name, double whidth, double height)  // 오타: whidth
{
    Instance.Windows[name].Width = whidth;   // 인스턴스 메서드인데 Instance. 경유
```

**수정 방향**:
```csharp
public void SetPopupSize(string name, double width, double height)
{
    _popupWindows[name].Width = width;
    _popupWindows[name].Height = height;
}
```

---

### [Design] UI-018 · _virtualKeyboardWindow = null! 후 즉시 NPE 위험

**파일**: `UI.Wpf.Equipment/VirtualKeyboard/DreamineVirtualKeyboardAssist.cs:493~497`

```csharp
public static void ResetDreamineVirtualKeyboard()
{
    _virtualKeyboardWindow = null!;  // 이후 IsVisible 접근 시 NPE
```

**수정 방향**: 필드를 nullable로 선언 후 접근 측에서 null 조건부 연산자 사용:
```csharp
private static DreamineVirtualKeyboardWindow? _virtualKeyboardWindow;
// 접근: _virtualKeyboardWindow?.IsVisible
```

---

### [Design] UI-019 · ownerDisabled 변수명 가독성 문제

**파일**: `UI.Wpf.Equipment/Popup/DreaminePopupService.cs:207~210`

```csharp
ownerDisabled = win.Owner.IsEnabled;  // IsEnabled(true/false)를 저장하는데 변수명이 ownerDisabled
```

**수정 방향**: 변수명을 `wasOwnerEnabled`로 변경.

---

### [Design] UI-020 · ButtonData.ImageSource 예외 무음

**파일**: `UI.Wpf.Controls/Navigation/DreamineNavigationBar.xaml.cs:548~551`

```csharp
catch { return null!; }  // 잘못된 ImagePath가 있어도 알 수 없음
```

**수정 방향**:
```csharp
catch (Exception ex)
{
    Debug.WriteLine($"[ButtonData] ImageSource 로드 실패({ImagePath}): {ex.Message}");
    return null!;
}
```

---

### [Build] UI-021 · UI 스레드에서 Dispatcher.Invoke 불필요 호출

**파일**: `UI.Wpf.Equipment/VirtualKeyboard/DreamineVirtualKeyboard.cs:676`

```csharp
Dispatcher.Invoke(UpdateKeys);  // 이미 UI 스레드에서 실행 중
```

**문제**: `KeyClick`은 WPF 이벤트 핸들러로 이미 UI 스레드. 불필요한 Dispatcher 경유.

**수정 방향**:
```csharp
UpdateKeys();  // 직접 호출
```

---

### [Design] UI-022 · 라이브러리에 프로젝트별 팝업 이름 하드코딩

**파일**: `UI.Wpf.Controls/Navigation/PopupWindowManager.cs:100~119`

```csharp
if (name.Contains("LoginAsync")) { popup.Width = 340; popup.Height = 400; }
else if (name == "VsAlarmEdit") { ... }
else if (name.Contains("Alarm")) { ... }
```

**문제**: 라이브러리 코드에 특정 프로젝트 팝업 이름이 하드코딩 → 재사용성 완전 파괴.

**수정 방향**: `CreatePopup`에 `double? width, double? height` 파라미터 추가, 하드코딩 제거.
```csharp
public void CreatePopup(string name, LoadedViewInfo info, double? width = null, double? height = null)
```

---

### [Design] UI-023 · UI.Abstractions에서 WPF Dispatcher 직접 의존

**파일**: `UI.Abstractions/VirtualKeyboard/EnterActionResult.cs:22~27`

```csharp
public void Show(TextBox textBox)
{
    Application.Current.Dispatcher.Invoke(() => { ... });
}
```

**문제**: Abstractions 레이어에서 `System.Windows`(WPF)에 직접 의존 → 계층 위반.

**수정 방향**: `Show()` 메서드를 Abstractions에서 제거하거나, UI 스레드 여부 확인 후 분기:
```csharp
if (Application.Current.Dispatcher.CheckAccess())
    UpdateTextBox(textBox);
else
    Application.Current.Dispatcher.BeginInvoke(() => UpdateTextBox(textBox));
```

---

### [Build] UI-024 · GetBindingExpression null 역참조

**파일**: `UI.Wpf.Equipment/VirtualKeyboard/DreamineVirtualKeyboardUI.xaml.cs:208`

```csharp
VkbTextBox.GetBindingExpression(TextBox.TextProperty).UpdateSource();
// GetBindingExpression이 null 반환 시 NPE
```

**수정 방향**:
```csharp
VkbTextBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
```

---

## 🟢 Low Priority

### [Design] UI-025 · Key._dicKeyData 가변 static + 키 미존재 방어 없음

**파일**: `UI.Wpf.Equipment/VirtualKeyboard/Key.cs:12`

```csharp
private static Dictionary<KeyCode, KeyData> _dicKeyData = new();
// UpdateKey에서 키 미존재 시 KeyNotFoundException 발생 (방어 없음)
```

**수정 방향**:
```csharp
private static readonly IReadOnlyDictionary<KeyCode, KeyData> _dicKeyData = BuildKeyMap();

// UpdateKey에서:
if (!_dicKeyData.TryGetValue(KeyCode, out var keyData)) return;
```

---

## 작업 우선순위 요약

```
🔴 즉시 처리 (안전성/정확성) — 13건
  UI-001  async void DisableButtonsAndEnableLater → async Task + CancellationToken
  UI-002  async void StartAutoClick → async Task + 이중 Close 방지
  UI-003  async void KeyUp 핸들러 → try/catch 래퍼
  UI-004  _hookRunning / _hookSubscribed → volatile
  UI-005  DreamineCheckLed Tick 중복 구독 → 최초 1회만 등록
  UI-006  ContentRendered 미해제 → popup.Closed에서도 -= 처리
  UI-007  SharpHook 빈 catch → Debug.WriteLine
  UI-008  Owner 설정 빈 catch → Debug.WriteLine
  UI-009  RequestCloseNextTick 재시도 빈 catch → Debug.WriteLine
  UI-010  ViewLoader 어셈블리 탐색 빈 catch → Debug.WriteLine
  UI-011  null! 강제 캐스팅 → nullable 선언
  UI-012  ViewModelKeyCache int 오버플로 → checked 또는 long
  UI-013  NavigationBar static HashSet 누수 → WeakReference 또는 Closed 처리

🟡 단기 처리 (품질/견고성) — 11건
  UI-014  _isInputHooked volatile 명시
  UI-015  PopupService List 스레드 안전성 주석
  UI-016  PopupWindowManager.Instance set 제거
  UI-017  SetPopupSize 오타(whidth) + Instance 자기참조
  UI-018  _virtualKeyboardWindow nullable 처리
  UI-019  ownerDisabled → wasOwnerEnabled 변수명
  UI-020  ButtonData.ImageSource 빈 catch → Debug.WriteLine
  UI-021  Dispatcher.Invoke → 직접 UpdateKeys() 호출
  UI-022  하드코딩 팝업 이름 제거 → CreatePopup 파라미터화
  UI-023  Abstractions WPF 직접 의존 제거
  UI-024  GetBindingExpression → ?. 연산자

🟢 장기/선택 처리 — 1건
  UI-025  Key._dicKeyData IReadOnlyDictionary + TryGetValue 방어
```

---

*총 25개 항목 | 🔴 13 · 🟡 11 · 🟢 1*
