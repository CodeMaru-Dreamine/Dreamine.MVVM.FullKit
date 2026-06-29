using System;
using System.Collections.ObjectModel;

namespace SampleCrossUi.Shared.ViewModels;

/// <summary>
/// Controls 데모의 실제 동작(버튼 클릭/라디오 선택/LED 토글/텍스트 클리어 등)을 처리합니다.
/// ControlsViewModel은 [DreamineCommand]를 통해 이 클래스의 메서드를 호출만 한다.
/// </summary>
public sealed class ControlsEvent
{
    /// <summary>커맨드로 인해 상태가 바뀔 때마다 발생합니다. ViewModel이 구독해서 OnPropertyChanged를 전달합니다.</summary>
    public event EventHandler? Changed;

    private void Raise(string status)
    {
        StatusMessage = status;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    // ── Button ────────────────────────────────────────────
    public int ClickCount { get; private set; }

    public ObservableCollection<string> ActivityLog { get; } = new();

    public void ClickMe()
    {
        ClickCount++;
        ActivityLog.Add($"[{DateTime.Now:HH:mm:ss}] Clicked ({ClickCount})");
        Raise($"Button clicked {ClickCount} time(s)");
    }

    // ── RadioButton ───────────────────────────────────────
    public string SelectedRadio { get; private set; } = "Option A";

    public void SelectRadio(string? option)
    {
        SelectedRadio = option ?? string.Empty;
        Raise($"Radio selected: {SelectedRadio}");
    }

    // ── CheckLed ──────────────────────────────────────────
    public bool LedIsOn { get; private set; } = true;
    public bool LedIsPulse { get; private set; }

    public void ToggleLed()
    {
        LedIsOn = !LedIsOn;
        Raise($"LED is {(LedIsOn ? "ON" : "OFF")}");
    }

    public void TogglePulse()
    {
        LedIsPulse = !LedIsPulse;
        Raise($"Pulse is {(LedIsPulse ? "ON" : "OFF")}");
    }

    // ── TextBox / PasswordBox ─────────────────────────────
    // 사용자가 직접 타이핑하는 값이라 Changed 이벤트로 묶지 않고
    // ViewModel 래퍼 프로퍼티가 개별적으로 OnPropertyChanged를 호출한다(타이핑 중 포커스/캐럿 튐 방지).
    public string TextInput { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    public void ClearText()
    {
        TextInput = string.Empty;
        Raise("TextBox cleared");
    }

    public void ClearPassword()
    {
        Password = string.Empty;
        Raise("Password cleared");
    }

    // ── ListBox 활성화(더블클릭) ───────────────────────────
    public void ListBoxActivated(string? item)
    {
        Raise($"List item activated: {item}");
    }

    // ── Status ────────────────────────────────────────────
    public string StatusMessage { get; private set; } = "Ready";
}
