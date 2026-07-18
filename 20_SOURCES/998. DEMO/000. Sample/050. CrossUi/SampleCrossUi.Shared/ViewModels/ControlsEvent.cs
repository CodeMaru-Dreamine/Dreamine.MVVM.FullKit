using System;
using System.Collections.ObjectModel;

namespace SampleCrossUi.Shared.ViewModels;

/// <summary>
/// \if KO
/// <para>Controls 데모의 실제 동작(버튼 클릭/라디오 선택/LED 토글/텍스트 클리어 등)을 처리합니다. ControlsViewModel은 [DreamineCommand]를 통해 이 클래스의 메서드를 호출만 한다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates controls event functionality and related state.</para>
/// \endif
/// </summary>
public sealed class ControlsEvent
{
    /// <summary>
    /// \if KO
    /// <para>커맨드로 인해 상태가 바뀔 때마다 발생합니다. ViewModel이 구독해서 OnPropertyChanged를 전달합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Occurs when changed takes place.</para>
    /// \endif
    /// </summary>
    public event EventHandler? Changed;

    /// <summary>
    /// \if KO
    /// <para>Raise 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the raise operation.</para>
    /// \endif
    /// </summary>
    /// <param name="status">
    /// \if KO
    /// <para>status에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for status.</para>
    /// \endif
    /// </param>
    private void Raise(string status)
    {
        StatusMessage = status;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    // ── Button ────────────────────────────────────────────
    /// <summary>
    /// \if KO
    /// <para>Click Count 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the click count value.</para>
    /// \endif
    /// </summary>
    public int ClickCount { get; private set; }

    /// <summary>
    /// \if KO
    /// <para>Activity Log 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the activity log value.</para>
    /// \endif
    /// </summary>
    public ObservableCollection<string> ActivityLog { get; } = new();

    /// <summary>
    /// \if KO
    /// <para>Click Me 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the click me operation.</para>
    /// \endif
    /// </summary>
    public void ClickMe()
    {
        ClickCount++;
        ActivityLog.Add($"[{DateTime.Now:HH:mm:ss}] Clicked ({ClickCount})");
        Raise($"Button clicked {ClickCount} time(s)");
    }

    // ── RadioButton ───────────────────────────────────────
    /// <summary>
    /// \if KO
    /// <para>Selected Radio 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the selected radio value.</para>
    /// \endif
    /// </summary>
    public string SelectedRadio { get; private set; } = "Option A";

    /// <summary>
    /// \if KO
    /// <para>Select Radio 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the select radio operation.</para>
    /// \endif
    /// </summary>
    /// <param name="option">
    /// \if KO
    /// <para>option에 사용할 <c>string?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for option.</para>
    /// \endif
    /// </param>
    public void SelectRadio(string? option)
    {
        SelectedRadio = option ?? string.Empty;
        Raise($"Radio selected: {SelectedRadio}");
    }

    // ── CheckLed ──────────────────────────────────────────
    /// <summary>
    /// \if KO
    /// <para>Led Is On 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the led is on value.</para>
    /// \endif
    /// </summary>
    public bool LedIsOn { get; private set; } = true;
    /// <summary>
    /// \if KO
    /// <para>Led Is Pulse 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the led is pulse value.</para>
    /// \endif
    /// </summary>
    public bool LedIsPulse { get; private set; }

    /// <summary>
    /// \if KO
    /// <para>Toggle Led 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the toggle led operation.</para>
    /// \endif
    /// </summary>
    public void ToggleLed()
    {
        LedIsOn = !LedIsOn;
        Raise($"LED is {(LedIsOn ? "ON" : "OFF")}");
    }

    /// <summary>
    /// \if KO
    /// <para>Toggle Pulse 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the toggle pulse operation.</para>
    /// \endif
    /// </summary>
    public void TogglePulse()
    {
        LedIsPulse = !LedIsPulse;
        Raise($"Pulse is {(LedIsPulse ? "ON" : "OFF")}");
    }

    // ── TextBox / PasswordBox ─────────────────────────────
    // 사용자가 직접 타이핑하는 값이라 Changed 이벤트로 묶지 않고
    // ViewModel 래퍼 프로퍼티가 개별적으로 OnPropertyChanged를 호출한다(타이핑 중 포커스/캐럿 튐 방지).
    /// <summary>
    /// \if KO
    /// <para>Text Input 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the text input value.</para>
    /// \endif
    /// </summary>
    public string TextInput { get; set; } = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>Password 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the password value.</para>
    /// \endif
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// \if KO
    /// <para>Clear Text 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the clear text operation.</para>
    /// \endif
    /// </summary>
    public void ClearText()
    {
        TextInput = string.Empty;
        Raise("TextBox cleared");
    }

    /// <summary>
    /// \if KO
    /// <para>Clear Password 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the clear password operation.</para>
    /// \endif
    /// </summary>
    public void ClearPassword()
    {
        Password = string.Empty;
        Raise("Password cleared");
    }

    // ── ListBox 활성화(더블클릭) ───────────────────────────
    /// <summary>
    /// \if KO
    /// <para>List Box Activated 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the list box activated operation.</para>
    /// \endif
    /// </summary>
    /// <param name="item">
    /// \if KO
    /// <para>item에 사용할 <c>string?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for item.</para>
    /// \endif
    /// </param>
    public void ListBoxActivated(string? item)
    {
        Raise($"List item activated: {item}");
    }

    // ── Status ────────────────────────────────────────────
    /// <summary>
    /// \if KO
    /// <para>Status Message 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the status message value.</para>
    /// \endif
    /// </summary>
    public string StatusMessage { get; private set; } = "Ready";
}
