using System.IO;
using System.Text.Json;
using DreamineWeb.Models;

namespace DreamineWeb.Services;

/// <summary>
/// Stores and manages playground demo content as JSON.
/// Creates the file from seed data on first run and backfills newly added seed items.
/// </summary>
public sealed class JsonPlaygroundStore : IPlaygroundStore
{
    private readonly string _path;
    private List<PlaygroundDemo>? _cache;
    private static readonly SemaphoreSlim _lock = new(1, 1);

    private static readonly JsonSerializerOptions _json = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public JsonPlaygroundStore(DreamineOptions opts)
    {
        var dir = opts.ResolvedDataPath;
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "playground.json");
    }

    public async Task<List<PlaygroundDemo>> GetAllAsync()
    {
        if (_cache is not null) return _cache;

        await _lock.WaitAsync();
        try
        {
            if (_cache is not null) return _cache;

            if (!File.Exists(_path))
            {
                _cache = SeedDefaults();
                await PersistAsync(_cache);
                return _cache;
            }

            var json = await File.ReadAllTextAsync(_path);
            _cache = JsonSerializer.Deserialize<List<PlaygroundDemo>>(json, _json) ?? [];

            var seed = SeedDefaults();

            // Add seed items that are not present in JSON yet.
            var missing = seed.Where(s => !_cache.Any(c => c.Id == s.Id)).ToList();
            var changed = false;
            if (missing.Count > 0)
            {
                _cache.AddRange(missing);
                changed = true;
            }

            changed |= RepairSeedCodeFields(_cache, seed);
            if (changed) await PersistAsync(_cache);

            return _cache;
        }
        finally { _lock.Release(); }
    }

    public async Task<PlaygroundDemo?> GetAsync(string id)
    {
        var all = await GetAllAsync();
        return all.FirstOrDefault(x => x.Id == id);
    }

    public async Task SaveAsync(PlaygroundDemo demo)
    {
        await _lock.WaitAsync();
        try
        {
            var all = _cache ?? [];
            var idx = all.FindIndex(x => x.Id == demo.Id);
            demo.UpdatedAt = DateTime.UtcNow;
            if (idx >= 0) all[idx] = demo;
            else all.Add(demo);
            _cache = all;
            await PersistAsync(all);
        }
        finally { _lock.Release(); }
    }

    public async Task DeleteAsync(string id)
    {
        await _lock.WaitAsync();
        try
        {
            var all = _cache ?? [];
            all.RemoveAll(x => x.Id == id);
            _cache = all;
            await PersistAsync(all);
        }
        finally { _lock.Release(); }
    }

    private async Task PersistAsync(List<PlaygroundDemo> list)
    {
        var json = JsonSerializer.Serialize(list, _json);
        await File.WriteAllTextAsync(_path, json);
    }

    private static bool RepairSeedCodeFields(List<PlaygroundDemo> demos, List<PlaygroundDemo> seed)
    {
        var changed = false;

        foreach (var demo in demos)
        {
            var seedDemo = seed.FirstOrDefault(x => x.Id == demo.Id);
            if (seedDemo is null) continue; // \uAD00\uB9AC\uC790\uAC00 \uC0C8\uB85C \uCD94\uAC00\uD55C \uB370\uBAA8\uB294 \uAC74\uB4DC\uB9AC\uC9C0 \uC54A\uB294\uB2E4.

            // \uCF54\uB4DC \uC2A4\uB2C8\uD3AB(View/ViewModel)\uC740 "\uC2E4\uC81C \uC2E4\uD589 \uCF54\uB4DC\uC758 \uBB38\uC11C"\uB2E4.
            // \uCCB4\uD5D8\uC5D0 \uBCF4\uC774\uB294 \uCF54\uB4DC\uAC00 \uC2E4\uC81C \uCEF4\uD3EC\uB10C\uD2B8 \uC0AC\uC6A9\uBC95\uACFC \uC5B4\uAE0B\uB098\uBA74 \uC548 \uB418\uBBC0\uB85C
            // \uD56D\uC0C1 \uC2DC\uB4DC\uB97C \uC815\uB2F5\uC73C\uB85C \uB3D9\uAE30\uD654\uD55C\uB2E4(\uAD00\uB9AC\uC790 \uD3B8\uC9D1 \uB300\uC0C1\uC774 \uC544\uB2C8\uB2E4).
            changed |= SyncCodeField(seedDemo.BlazorCode, demo.BlazorCode, v => demo.BlazorCode = v);
            changed |= SyncCodeField(seedDemo.VmCode, demo.VmCode, v => demo.VmCode = v);
            changed |= SyncCodeField(seedDemo.WpfCode, demo.WpfCode, v => demo.WpfCode = v);
            changed |= SyncCodeField(seedDemo.WinFormsCode, demo.WinFormsCode, v => demo.WinFormsCode = v);
            changed |= SyncCodeField(seedDemo.MauiCode, demo.MauiCode, v => demo.MauiCode = v);

            // \uC601\uBB38 \uC81C\uBAA9/\uC124\uBA85\uC740 \uBE44\uC5B4 \uC788\uC744 \uB54C\uB9CC \uCC44\uC6B4\uB2E4(\uC81C\uBAA9\u00B7\uC124\uBA85\uC740 \uAD00\uB9AC\uC790 \uD3B8\uC9D1 \uBCF4\uC874).
            if (string.IsNullOrWhiteSpace(demo.TitleEn) && !string.IsNullOrWhiteSpace(seedDemo.TitleEn))
            {
                demo.TitleEn = seedDemo.TitleEn;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(demo.DescriptionEn) && !string.IsNullOrWhiteSpace(seedDemo.DescriptionEn))
            {
                demo.DescriptionEn = seedDemo.DescriptionEn;
                changed = true;
            }
        }

        return changed;
    }

    /// <summary>\uC2DC\uB4DC \uAC12\uC774 \uC788\uACE0 \uD604\uC7AC \uAC12\uACFC \uB2E4\uB974\uBA74 \uC2DC\uB4DC \uAC12\uC73C\uB85C \uB36E\uC5B4\uC4F4\uB2E4.</summary>
    private static bool SyncCodeField(string seedValue, string current, Action<string> apply)
    {
        if (!string.IsNullOrWhiteSpace(seedValue) && !string.Equals(seedValue, current, StringComparison.Ordinal))
        {
            apply(seedValue);
            return true;
        }
        return false;
    }

    // ══════════════════════════════════════════════════════════
    //  Seed data - currently hard-coded control demos
    // ══════════════════════════════════════════════════════════
    private static List<PlaygroundDemo> SeedDefaults() =>
    [
        new()
        {
            Id = "bulb", SortOrder = 5, NavLabel = "Light Bulb",
            Title = "전구 — 버튼 & 체크박스",
            TitleEn = "Light Bulb — Button & Checkbox",
            Description = "버튼(커맨드)과 체크박스(양방향 바인딩)가 같은 상태를 조작합니다. 로직은 Event, 상태는 Model, 노출은 ViewModel — Dreamine의 3분할 그대로입니다.",
            DescriptionEn = "A button (command) and a checkbox (two-way binding) drive the same state. Logic in Event, state in Model, exposure in ViewModel.",
            WpfShot = "/img/demos/lightbulb-wpf.svg", WinFormsShot = "/img/demos/lightbulb-winforms.svg", MauiShot = "/img/demos/lightbulb-maui.svg",
            BlazorCode = @"@* @using Dreamine.UI.Blazor *@
<DreamineLightBulb IsOn=""@Bulb.IsOn"" Diameter=""120"" />
<DreamineButton Variant=""DreamineButtonVariant.Primary"" OnClick=""Bulb.Toggle"">Toggle</DreamineButton>
<DreamineCheckBox @bind-Checked=""Bulb.IsOn"">Power</DreamineCheckBox>
<span>@Bulb.StatusText · toggled @Bulb.ToggleCount times</span>",
            VmCode = @"public sealed class LightBulbModel { public bool IsOn; public int ToggleCount; }

public sealed class LightBulbEvent
{
    private readonly LightBulbModel _m;
    public LightBulbEvent(LightBulbModel m) => _m = m;
    public void Toggle() { _m.IsOn = !_m.IsOn; _m.ToggleCount++; }
}

public sealed class LightBulbViewModel : ViewModelBase
{
    public bool IsOn { get; set; }
    public int ToggleCount { get; }
    public string StatusText => IsOn ? ""ON"" : ""OFF"";
}",
            WpfCode = @"<!-- WPF (XAML) -->
<StackPanel>
    <ctrl:DreamineLightBulb IsOn=""{Binding IsOn}"" Diameter=""96"" />
    <CheckBox Content=""Power"" IsChecked=""{Binding IsOn}"" />
    <Button Content=""Toggle"" Command=""{Binding ToggleCommand}"" />
    <TextBlock Text=""{Binding ToggleCount, StringFormat=Toggled {0} times}"" />
</StackPanel>",
            WinFormsCode = @"var bulb = new DreamineLightBulb { Diameter = 96 };

toggleButton.Click += (_, _) => vm.ToggleCommand.Execute(null);
checkPower.DataBindings.Add(""Checked"", vm, nameof(vm.IsOn),
    false, DataSourceUpdateMode.OnPropertyChanged);

vm.PropertyChanged += (_, _) => bulb.IsOn = vm.IsOn;
bulb.IsOn = vm.IsOn;",
            MauiCode = @"<dc:DreamineLightBulb IsOn=""{Binding IsOn}"" Diameter=""112"" />
<CheckBox IsChecked=""{Binding IsOn}"" />
<Button Text=""Toggle"" Command=""{Binding ToggleCommand}"" />
<Label Text=""{Binding ToggleCount, StringFormat='Toggled {0} times'}"" />"
        },
        new()
        {
            Id = "button", SortOrder = 10, NavLabel = "Button",
            Title = "Button — DreamineButton",
            Description = "커맨드에 바인딩된 버튼. 클릭할 때마다 Event가 카운트를 올리고 로그를 남깁니다.",
            DescriptionEn = "A command-bound button. Each click increments the count and appends a log via the Event.",
            WpfShot = "/img/demos/button-wpf.svg", WinFormsShot = "/img/demos/button-winforms.svg", MauiShot = "/img/demos/button-maui.svg",
            BlazorCode = @"@* @using Dreamine.UI.Blazor *@
<DreamineButton Variant=""DreamineButtonVariant.Primary"" OnClick=""Vm.ClickMe"">Primary</DreamineButton>
<DreamineButton OnClick=""Vm.ClickMe"">Secondary</DreamineButton>
<DreamineButton Variant=""DreamineButtonVariant.Danger"" OnClick=""Vm.ClickMe"">Danger</DreamineButton>
<span>Clicks @Vm.ClickCount</span>
@foreach (var line in Vm.ActivityLog) { <li>@line</li> }",
            VmCode = @"// Shared ViewModel (Dreamine Source Generator)
[DreamineProperty] private int _clickCount;
public ObservableCollection<string> ActivityLog { get; } = new();

[DreamineCommand]
private void ClickMe()
{
    ClickCount++;
    ActivityLog.Insert(0, $""[{DateTime.Now:HH:mm:ss}] Clicked ({ClickCount})"");
}",
            WpfCode = @"<!-- xmlns:ctrl=""...Dreamine.UI.Wpf.Controls"" -->
<ctrl:DreamineButton Content=""Primary"" Background=""#1E90FF"" Foreground=""White""
                     Width=""100"" Height=""34""
                     Command=""{Binding ClickMeCommand}"" />
<ctrl:DreamineButton Content=""Blue Glow"" ShineColor=""#1E90FF""
                     Command=""{Binding ClickMeCommand}"" />
<TextBlock Text=""{Binding ClickCount, StringFormat='Click count: {0}'}"" />",
            WinFormsCode = @"var btnPrimary = new DreamineButton {
    Text = ""Primary"", Width = 100, Height = 34,
    BackColor = Color.FromArgb(0x1E, 0x90, 0xFF),
    ForeColor = Color.White
};
btnPrimary.Click += (_, _) => vm.ClickMeCommand.Execute(null);

var lblCount = new Label();
vm.PropertyChanged += (_, _) =>
    lblCount.Text = $""Click count: {vm.ClickCount}"";",
            MauiCode = @"<!-- .NET MAUI -->
<Button Text=""Primary"" BackgroundColor=""#1E90FF"" TextColor=""White""
        Command=""{Binding ClickMeCommand}"" />
<Button Text=""Secondary"" Command=""{Binding ClickMeCommand}"" />
<Label  Text=""{Binding ClickCount, StringFormat='Click count: {0}'}"" />"
        },
        new()
        {
            Id = "checkbox", SortOrder = 20, NavLabel = "CheckBox",
            Title = "CheckBox — DreamineCheckBox",
            Description = "IsChecked 양방향 바인딩. 마지막 항목은 IsEnabled=False로 비활성 상태입니다.",
            DescriptionEn = "IsChecked two-way binding. The last item is disabled (IsEnabled=False).",
            WpfShot = "/img/demos/checkbox-wpf.svg", WinFormsShot = "/img/demos/checkbox-winforms.svg", MauiShot = "/img/demos/checkbox-maui.svg",
            BlazorCode = @"@* @using Dreamine.UI.Blazor *@
<DreamineCheckBox @bind-Checked=""Vm.Check1"">Check 1</DreamineCheckBox>
<DreamineCheckBox @bind-Checked=""Vm.Check2"">Check 2</DreamineCheckBox>
<DreamineCheckBox @bind-Checked=""Vm.Check3"" Disabled=""true"">Check 3</DreamineCheckBox>
<p>Check1 = @Vm.Check1 · Check2 = @Vm.Check2</p>",
            VmCode = @"[DreamineProperty] private bool _check1 = true;
[DreamineProperty] private bool _check2;
[DreamineProperty] private bool _check3;",
            WpfCode = @"<ctrl:DreamineCheckBox Content=""Check 1 (checked by default)"" IsChecked=""{Binding Check1}"" />
<ctrl:DreamineCheckBox Content=""Check 2"" IsChecked=""{Binding Check2}"" />
<ctrl:DreamineCheckBox Content=""Check 3 (disabled)"" IsChecked=""{Binding Check3}"" IsEnabled=""False"" />",
            WinFormsCode = @"var chk1 = new DreamineCheckBox { Text = ""Check 1"" };
chk1.DataBindings.Add(""Checked"", vm, nameof(vm.Check1),
    false, DataSourceUpdateMode.OnPropertyChanged);

var chk2 = new DreamineCheckBox { Text = ""Check 2"" };
chk2.DataBindings.Add(""Checked"", vm, nameof(vm.Check2),
    false, DataSourceUpdateMode.OnPropertyChanged);

var chk3 = new DreamineCheckBox { Text = ""Check 3 (disabled)"", Enabled = false };
chk3.DataBindings.Add(""Checked"", vm, nameof(vm.Check3));",
            MauiCode = @"<ctrl:DreamineCheckBox Text=""Check 1"" IsChecked=""{Binding Check1}"" />
<ctrl:DreamineCheckBox Text=""Check 2"" IsChecked=""{Binding Check2}"" />
<ctrl:DreamineCheckBox Text=""Check 3 (disabled)"" IsChecked=""{Binding Check3}""
                       IsEnabled=""False"" />"
        },
        new()
        {
            Id = "radio", SortOrder = 30, NavLabel = "RadioButton",
            Title = "RadioButton — DreamineRadioButton",
            Description = "같은 GroupName으로 묶인 라디오. 선택 시 RelayCommand<string>로 값을 전달합니다.",
            DescriptionEn = "Radios sharing a GroupName; selection passes the value via RelayCommand<string>.",
            WpfShot = "/img/demos/radio-wpf.svg", WinFormsShot = "/img/demos/radio-winforms.svg", MauiShot = "/img/demos/radio-maui.svg",
            BlazorCode = @"@* @using Dreamine.UI.Blazor *@
@foreach (var opt in new[] { ""Option A"", ""Option B"", ""Option C"" })
{
    <DreamineRadioButton Name=""grp"" Value=""@opt""
                         SelectedValue=""@Vm.SelectedRadio""
                         SelectedValueChanged=""Vm.SelectRadio"">@opt</DreamineRadioButton>
}
<p>Selected: @Vm.SelectedRadio</p>",
            VmCode = @"[DreamineProperty] private string _selectedRadio = ""Option A"";

[DreamineCommand]
private void SelectRadio(string option) => SelectedRadio = option;",
            WpfCode = @"<ctrl:DreamineRadioButton Content=""Option A"" GroupName=""demo"" IsChecked=""True""
    ctrl:DreamineRadioButton.Command=""{Binding SelectRadioCommand}""
    ctrl:DreamineRadioButton.CommandParameter=""Option A""
    ctrl:DreamineRadioButton.CommandTriggerName=""Click"" />
<ctrl:DreamineRadioButton Content=""Option B"" GroupName=""demo""
    ctrl:DreamineRadioButton.Command=""{Binding SelectRadioCommand}""
    ctrl:DreamineRadioButton.CommandParameter=""Option B"" ... />
<TextBlock Text=""{Binding SelectedRadio, StringFormat='Selected: {0}'}"" />",
            WinFormsCode = @"var radA = new DreamineRadioButton { Text = ""Option A"", Checked = true };
var radB = new DreamineRadioButton { Text = ""Option B"" };
var radC = new DreamineRadioButton { Text = ""Option C"" };

radA.CheckedChanged += (_, _) => { if (radA.Checked) vm.SelectedRadio = ""Option A""; };
radB.CheckedChanged += (_, _) => { if (radB.Checked) vm.SelectedRadio = ""Option B""; };
radC.CheckedChanged += (_, _) => { if (radC.Checked) vm.SelectedRadio = ""Option C""; };",
            MauiCode = @"<RadioButton Content=""Option A"" Value=""Option A""
             GroupName=""demo"" IsChecked=""True"" />
<RadioButton Content=""Option B"" Value=""Option B""
             GroupName=""demo"" />
<RadioButton Content=""Option C"" Value=""Option C""
             GroupName=""demo"" />
<Label Text=""{Binding SelectedRadio, StringFormat='Selected: {0}'}"" />"
        },
        new()
        {
            Id = "checkled", SortOrder = 40, NavLabel = "CheckLed",
            Title = "CheckLed — DreamineCheckLed",
            Description = "Dreamine 시그니처 LED 인디케이터. IsOn / IsPulse 를 바인딩합니다 (장비 상태 표시용).",
            DescriptionEn = "Dreamine's signature LED indicator. Bind IsOn / IsPulse (great for equipment status).",
            WpfShot = "/img/demos/checkled-wpf.svg", WinFormsShot = "/img/demos/checkled-winforms.svg", MauiShot = "/img/demos/checkled-maui.svg",
            BlazorCode = @"@* @using Dreamine.UI.Blazor *@
<DreamineCheckLed IsOn=""Vm.LedIsOn"" IsPulse=""Vm.LedIsPulse"" Diameter=""34"" />
<DreamineButton OnClick=""Vm.ToggleLed"">ON / OFF</DreamineButton>
<DreamineButton OnClick=""Vm.TogglePulse"">Pulse</DreamineButton>
<p>IsOn = @Vm.LedIsOn · IsPulse = @Vm.LedIsPulse</p>",
            VmCode = @"[DreamineProperty] private bool _ledIsOn = true;
[DreamineProperty] private bool _ledIsPulse;

[DreamineCommand] private void ToggleLed()   => LedIsOn = !LedIsOn;
[DreamineCommand] private void TogglePulse() => LedIsPulse = !LedIsPulse;",
            WpfCode = @"<ctrl:DreamineCheckLed IsOn=""{Binding LedIsOn}""
                       IsPulse=""{Binding LedIsPulse}""
                       Width=""36"" Height=""36"" />
<ctrl:DreamineButton Content=""ON / OFF"" Command=""{Binding ToggleLedCommand}"" />
<ctrl:DreamineButton Content=""Pulse""    Command=""{Binding TogglePulseCommand}"" />",
            WinFormsCode = @"var led = new DreamineCheckLed { Width = 36, Height = 36 };
led.DataBindings.Add(""IsOn"", vm, nameof(vm.LedIsOn));
led.DataBindings.Add(""IsPulse"", vm, nameof(vm.LedIsPulse));

var btnToggle = new DreamineButton { Text = ""ON / OFF"" };
btnToggle.Click += (_, _) => vm.ToggleLedCommand.Execute(null);

var btnPulse = new DreamineButton { Text = ""Pulse"" };
btnPulse.Click += (_, _) => vm.TogglePulseCommand.Execute(null);",
            MauiCode = @"<ctrl:DreamineCheckLed IsOn=""{Binding LedIsOn}""
                       IsPulse=""{Binding LedIsPulse}""
                       WidthRequest=""36"" HeightRequest=""36"" />
<Button Text=""ON / OFF"" Command=""{Binding ToggleLedCommand}"" />
<Button Text=""Pulse""    Command=""{Binding TogglePulseCommand}"" />"
        },
        new()
        {
            Id = "textbox", SortOrder = 50, NavLabel = "TextBox",
            Title = "TextBox / PasswordBox — DreamineTextBox",
            Description = "Hint(placeholder) 지원 입력 컨트롤. Clear 커맨드로 값을 비웁니다.",
            DescriptionEn = "Input controls with Hint (placeholder). Clear via command.",
            WpfShot = "/img/demos/textbox-wpf.svg", WinFormsShot = "/img/demos/textbox-winforms.svg", MauiShot = "/img/demos/textbox-maui.svg",
            BlazorCode = @"@* @using Dreamine.UI.Blazor *@
<DreamineTextBox @bind-Value=""Vm.TextInput"" Hint=""Type here...""
                 OnFocus=""() => _showKeyboard = true"" />
<DreamineButton Size=""DreamineButtonSize.Small"" OnClick=""Vm.ClearText"">Clear</DreamineButton>
<p>Value: @Vm.TextInput</p>

@* 가상 키보드는 대상 텍스트박스 바로 아래에 표시 — 시선이 튀지 않게. *@
@if (_showKeyboard)
{
    <DreamineVirtualKeyboard IsVisible=""_showKeyboard""
                             Value=""@Vm.TextInput""
                             ValueChanged=""v => Vm.TextInput = v""
                             OnEnter=""() => _showKeyboard = false"" />
}

<DreamineTextBox @bind-Value=""Vm.Password"" IsPassword=""true"" Hint=""Password..."" />
<DreamineButton Size=""DreamineButtonSize.Small"" OnClick=""Vm.ClearPassword"">Clear</DreamineButton>
<p>Length: @Vm.Password.Length</p>

@code { private bool _showKeyboard; }",
            VmCode = @"[DreamineProperty] private string _textInput = string.Empty;
[DreamineProperty] private string _password  = string.Empty;

[DreamineCommand] private void ClearText()     => TextInput = string.Empty;
[DreamineCommand] private void ClearPassword() => Password  = string.Empty;",
            WpfCode = @"<ctrl:DreamineTextBox
    Text=""{Binding TextInput, UpdateSourceTrigger=PropertyChanged}""
    Hint=""Type text here..."" Height=""40"" />
<ctrl:DreamineButton Content=""Clear"" Command=""{Binding ClearTextCommand}"" />

<ctrl:DreaminePasswordBox
    Password=""{Binding Password, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}""
    Hint=""Enter password..."" Height=""40"" />
<TextBlock Text=""{Binding Password.Length, StringFormat='Length: {0} chars'}"" />",
            WinFormsCode = @"var txt = new DreamineTextBox { Hint = ""Type here..."" };
txt.DataBindings.Add(""Text"", vm, nameof(vm.TextInput),
    false, DataSourceUpdateMode.OnPropertyChanged);

var pwd = new DreaminePasswordBox { Hint = ""Password..."" };
pwd.DataBindings.Add(""Password"", vm, nameof(vm.Password),
    false, DataSourceUpdateMode.OnPropertyChanged);

var btnClear = new DreamineButton { Text = ""Clear"" };
btnClear.Click += (_, _) => vm.ClearTextCommand.Execute(null);",
            MauiCode = @"<Entry Placeholder=""Type here...""
       Text=""{Binding TextInput}"" />
<Entry Placeholder=""Password..."" IsPassword=""True""
       Text=""{Binding Password}"" />
<Button Text=""Clear"" Command=""{Binding ClearTextCommand}"" />"
        },
        new()
        {
            Id = "combobox", SortOrder = 60, NavLabel = "ComboBox",
            Title = "ComboBox — DreamineComboBox",
            Description = "ItemsSource / SelectedItem 바인딩.",
            DescriptionEn = "ItemsSource / SelectedItem binding.",
            WpfShot = "/img/demos/combobox-wpf.svg", WinFormsShot = "/img/demos/combobox-winforms.svg", MauiShot = "/img/demos/combobox-maui.svg",
            BlazorCode = @"@* @using Dreamine.UI.Blazor *@
<DreamineComboBox Items=""Vm.FruitItems"" @bind-SelectedItem=""Vm.SelectedFruit"" />
<span>Selected: @Vm.SelectedFruit</span>",
            VmCode = @"public string[] FruitItems { get; } =
    { ""Apple"", ""Banana"", ""Cherry"", ""Grape"", ""Mango"", ""Melon"" };

[DreamineProperty] private string _selectedFruit = ""Cherry"";",
            WpfCode = @"<ctrl:DreamineComboBox
    ItemsSource=""{Binding FruitItems}""
    SelectedItem=""{Binding SelectedFruit}""
    Width=""200"" Height=""36"" />
<TextBlock Text=""{Binding SelectedFruit, StringFormat='Selected item: {0}'}"" />",
            WinFormsCode = @"var cmb = new DreamineComboBox();
cmb.DataSource = vm.FruitItems;
cmb.DataBindings.Add(""SelectedItem"", vm, nameof(vm.SelectedFruit),
    false, DataSourceUpdateMode.OnPropertyChanged);

vm.PropertyChanged += (_, _) =>
    lblSelected.Text = $""Selected: {vm.SelectedFruit}"";",
            MauiCode = @"<Picker Title=""Select fruit""
        ItemsSource=""{Binding FruitItems}""
        SelectedItem=""{Binding SelectedFruit}"" />
<Label Text=""{Binding SelectedFruit, StringFormat='Selected: {0}'}"" />"
        },
        new()
        {
            Id = "expander", SortOrder = 70, NavLabel = "Expander",
            Title = "Expander — DreamineExpander",
            Description = "IsExpanded를 VM에 바인딩해 코드에서도 펼침/접힘을 제어합니다.",
            DescriptionEn = "Bind IsExpanded to the VM to control expand/collapse from code too.",
            WpfShot = "/img/demos/expander-wpf.svg", WinFormsShot = "/img/demos/expander-winforms.svg", MauiShot = "/img/demos/expander-maui.svg",
            BlazorCode = @"@* @using Dreamine.UI.Blazor *@
<DreamineExpander Header=""Expand / Collapse"" @bind-IsExpanded=""Vm.IsExpanded"">
    Any content can go here. IsExpanded = @Vm.IsExpanded
</DreamineExpander>",
            VmCode = @"[DreamineProperty] private bool _isExpanded = true;

[DreamineCommand] private void ToggleExpand() => IsExpanded = !IsExpanded;",
            WpfCode = @"<ctrl:DreamineExpander Header=""Expand / Collapse""
                       IsExpanded=""{Binding IsExpanded}"">
    <StackPanel Margin=""16,8"">
        <TextBlock Text=""Any content can go here."" />
    </StackPanel>
</ctrl:DreamineExpander>",
            WinFormsCode = @"var exp = new DreamineExpander { HeaderText = ""Expand / Collapse"" };
exp.DataBindings.Add(""IsExpanded"", vm, nameof(vm.IsExpanded),
    false, DataSourceUpdateMode.OnPropertyChanged);

exp.ContentPanel.Controls.Add(new Label {
    Text = ""Any content can go here."",
    Dock = DockStyle.Fill
});",
            MauiCode = @"<ctrl:DreamineExpander Header=""Expand / Collapse""
                       IsExpanded=""{Binding IsExpanded}"">
    <Label Text=""Any content can go here.""
           Margin=""16,8"" />
</ctrl:DreamineExpander>"
        },
        new()
        {
            Id = "numeric", SortOrder = 80, NavLabel = "Numeric",
            Title = "Numeric — NumericRangeBehavior",
            Description = "0~100 범위로 자동 보정(Clamp)되는 숫자 입력. 슬라이더/숫자 어느 쪽으로 바꿔도 값이 동기화됩니다.",
            DescriptionEn = "Numeric input clamped to 0–100. Slider and number stay in sync.",
            WpfShot = "/img/demos/numeric-wpf.svg", WinFormsShot = "/img/demos/numeric-winforms.svg", MauiShot = "",
            BlazorCode = @"@* @using Dreamine.UI.Blazor *@
<DreamineNumericSlider @bind-Value=""Vm.NumericInput"" Min=""0"" Max=""100"" />
<span>Value: @Vm.NumericInput</span>",
            VmCode = @"private double _numericInput = 50;
public double NumericInput
{
    get => _numericInput;
    set { _numericInput = Math.Clamp(value, 0, 100); OnPropertyChanged(); }
}",
            WpfCode = @"<!-- behaviors:...Dreamine.UI.Wpf.Behaviors -->
<TextBox Text=""{Binding NumericInput, UpdateSourceTrigger=PropertyChanged}""
         behaviors:NumericRangeBehavior.IsEnabled=""True""
         behaviors:NumericRangeBehavior.Min=""0""
         behaviors:NumericRangeBehavior.Max=""100""
         behaviors:NumericRangeBehavior.Mode=""Clamp"" Width=""120"" />
<!-- Values outside the range are clamped to 0-100 automatically. -->",
            WinFormsCode = @"// WinForms uses the standard NumericUpDown with manual clamping.
var nud = new NumericUpDown {
    Minimum = 0, Maximum = 100, Value = 50,
    DecimalPlaces = 0
};
nud.DataBindings.Add(""Value"", vm, nameof(vm.NumericInput),
    false, DataSourceUpdateMode.OnPropertyChanged);",
            MauiCode = @"<Slider Minimum=""0"" Maximum=""100""
        Value=""{Binding NumericInput}"" />
<Entry Keyboard=""Numeric""
       Text=""{Binding NumericInput}"" />
<Label Text=""{Binding NumericInput, StringFormat='Value: {0:F0}'}"" />"
        },
        new()
        {
            Id = "listbox", SortOrder = 90, NavLabel = "ListBox",
            Title = "ListBox — DreamineListBox",
            Description = "선택(클릭) + 더블클릭 시 커맨드 실행(활성화).",
            DescriptionEn = "Select (click) + run a command on double-click (activation).",
            WpfShot = "/img/demos/listbox-wpf.svg", WinFormsShot = "/img/demos/listbox-winforms.svg", MauiShot = "/img/demos/listbox-maui.svg",
            BlazorCode = @"@* @using Dreamine.UI.Blazor *@
<DreamineListBox Items=""Vm.FruitItems""
                 @bind-SelectedItem=""Vm.SelectedFruit""
                 OnActivate=""Vm.ActivateListItem"" />
<p>Selected: @Vm.SelectedFruit · @Vm.ListActivated</p>",
            VmCode = @"public string[] FruitItems { get; } =
    { ""Apple"", ""Banana"", ""Cherry"", ""Grape"", ""Mango"", ""Melon"" };

[DreamineProperty] private string _selectedFruit = ""Cherry"";
[DreamineProperty] private string _listActivated = ""(double-click an item)"";

[DreamineCommand]
private void ActivateListItem(string item) => ListActivated = $""Activated: {item}"";",
            WpfCode = @"<ctrl:DreamineListBox
    ItemsSource=""{Binding FruitItems}""
    SelectedItem=""{Binding SelectedFruit}""
    ctrl:DreamineListBox.Command=""{Binding ListBoxActivatedCommand}""
    ctrl:DreamineListBox.CommandParameter=""{Binding SelectedFruit}""
    ctrl:DreamineListBox.CommandTriggerName=""MouseDoubleClick"" />",
            WinFormsCode = @"var lst = new DreamineListBox();
lst.DataSource = vm.FruitItems;
lst.DataBindings.Add(""SelectedItem"", vm, nameof(vm.SelectedFruit),
    false, DataSourceUpdateMode.OnPropertyChanged);

lst.DoubleClick += (_, _) =>
    vm.ListBoxActivatedCommand.Execute(vm.SelectedFruit);",
            MauiCode = @"<CollectionView ItemsSource=""{Binding FruitItems}""
                SelectionMode=""Single""
                SelectedItem=""{Binding SelectedFruit}"">
    <CollectionView.ItemTemplate>
        <DataTemplate>
            <Label Text=""{Binding .}"" Padding=""12,8"" />
        </DataTemplate>
    </CollectionView.ItemTemplate>
</CollectionView>"
        },
        new()
        {
            Id = "counter", SortOrder = 100, NavLabel = "Counter",
            Title = "Counter — Increment / Reset",
            Description = "SampleCrossUi의 카운터 데모. Increment는 서비스를 거쳐 값을 올리고, Reset은 0으로 초기화합니다. 로그가 자동으로 쌓입니다.",
            DescriptionEn = "The SampleCrossUi counter demo. Increment goes through a service, Reset zeroes the count, and logs accumulate automatically.",
            WpfShot = "/img/demos/counter-wpf.svg", WinFormsShot = "/img/demos/counter-winforms.svg", MauiShot = "/img/demos/counter-maui.svg",
            BlazorCode = @"@* @using Dreamine.UI.Blazor *@
<span class=""dw-counter-value"">@Vm.Count</span>
<DreamineButton Variant=""DreamineButtonVariant.Primary"" OnClick=""Vm.Increment"">Increment</DreamineButton>
<DreamineButton OnClick=""Vm.ResetCounter"">Reset</DreamineButton>
@foreach (var line in Vm.CounterLogs) { <li>@line</li> }",
            VmCode = @"[DreamineProperty] private int _count;
public ObservableCollection<string> CounterLogs { get; } = new();

[DreamineCommand]
private void Increment()
{
    Count++;
    CounterLogs.Insert(0, $""[{DateTime.Now:HH:mm:ss}] Incremented → {Count}"");
}

[DreamineCommand]
private void ResetCounter()
{
    Count = 0;
    CounterLogs.Insert(0, $""[{DateTime.Now:HH:mm:ss}] Counter reset."");
}",
            WpfCode = @"<!-- CounterView.xaml — SampleCrossUi.Wpf -->
<ctrl:DreamineLabel Content=""{Binding Count, StringFormat='Count: {0}'}""
                    FontSize=""52"" FontWeight=""Bold"" />

<ctrl:DreamineButton Content=""Increment""
    Command=""{Binding IncrementCommand}"" Width=""130"" Height=""40"" />
<ctrl:DreamineButton Content=""Reset""
    Command=""{Binding ResetCommand}"" Width=""100"" Height=""40"" />

<ListBox ItemsSource=""{Binding Logs}""
         behaviors:AutoScrollListBoxBehavior.IsEnabled=""True"" />",
            WinFormsCode = @"// CounterPage — SampleCrossUi.WinForms
var lblCount = new Label { Font = new Font(""Segoe UI"", 52, FontStyle.Bold) };
vm.PropertyChanged += (_, _) =>
    lblCount.Text = $""Count: {vm.Count}"";

var btnInc = new DreamineButton { Text = ""Increment"" };
btnInc.Click += (_, _) => vm.IncrementCommand.Execute(null);

var btnReset = new DreamineButton { Text = ""Reset"" };
btnReset.Click += (_, _) => vm.ResetCommand.Execute(null);",
            MauiCode = @"<!-- CounterPage — SampleCrossUi.Maui -->
<Label Text=""{Binding Count, StringFormat='Count: {0}'}""
       FontSize=""52"" FontAttributes=""Bold""
       HorizontalOptions=""Center"" />
<Button Text=""Increment"" Command=""{Binding IncrementCommand}"" />
<Button Text=""Reset""     Command=""{Binding ResetCommand}"" />"
        },
        new()
        {
            Id = "datagrid", SortOrder = 110, NavLabel = "DataGrid",
            Title = "DataGrid — DreamineDataGrid",
            Description = "ItemsSource 바인딩 + ClickToDeselect Behavior. 읽기 전용 테이블에 장비 상태를 표시합니다.",
            DescriptionEn = "ItemsSource binding + ClickToDeselect Behavior. Displays equipment status in a read-only table.",
            WpfShot = "/img/demos/datagrid-wpf.svg", WinFormsShot = "/img/demos/datagrid-winforms.svg", MauiShot = "/img/demos/datagrid-maui.svg",
            BlazorCode = @"@* @using Dreamine.UI.Blazor *@
<DreamineDataGrid TItem=""GridRow"" Items=""Vm.GridRows"" @bind-SelectedItem=""Vm.SelectedRow"">
    <HeaderContent><th>No</th><th>Name</th><th>Status</th></HeaderContent>
    <RowContent>
        <td>@context.No</td><td>@context.Name</td><td>@context.Status</td>
    </RowContent>
</DreamineDataGrid>",
            VmCode = @"public ObservableCollection<GridRow> GridRows { get; } = new()
{
    new() { No = 1, Name = ""Device A"", Status = ""Running"" },
    new() { No = 2, Name = ""Device B"", Status = ""Stopped"" },
    new() { No = 3, Name = ""Device C"", Status = ""Running"" },
    new() { No = 4, Name = ""Device D"", Status = ""Error"" },
};

[DreamineProperty] private GridRow? _selectedRow;",
            WpfCode = @"<ctrl:DreamineDataGrid
    ItemsSource=""{Binding GridRows}""
    AutoGenerateColumns=""False""
    behaviors:DataGridBehaviors.EnableClickToDeselect=""True""
    IsReadOnly=""True"">
    <ctrl:DreamineDataGrid.Columns>
        <DataGridTextColumn Header=""No""     Binding=""{Binding No}""     Width=""50"" />
        <DataGridTextColumn Header=""Name""   Binding=""{Binding Name}""   Width=""150"" />
        <DataGridTextColumn Header=""Status"" Binding=""{Binding Status}"" Width=""120"" />
    </ctrl:DreamineDataGrid.Columns>
</ctrl:DreamineDataGrid>",
            WinFormsCode = @"var dgv = new DreamineDataGrid {
    AutoGenerateColumns = false, ReadOnly = true
};
dgv.Columns.Add(new DataGridViewTextBoxColumn {
    HeaderText = ""No"", DataPropertyName = ""No"", Width = 50 });
dgv.Columns.Add(new DataGridViewTextBoxColumn {
    HeaderText = ""Name"", DataPropertyName = ""Name"", Width = 150 });
dgv.Columns.Add(new DataGridViewTextBoxColumn {
    HeaderText = ""Status"", DataPropertyName = ""Status"", Width = 120 });

dgv.DataSource = new BindingSource { DataSource = vm.GridRows };",
            MauiCode = @"<!-- MAUI - table-like layout with CollectionView -->
<CollectionView ItemsSource=""{Binding GridRows}""
                SelectionMode=""Single""
                SelectedItem=""{Binding SelectedRow}"">
    <CollectionView.ItemTemplate>
        <DataTemplate>
            <Grid ColumnDefinitions=""50,*,120"" Padding=""8,4"">
                <Label Grid.Column=""0"" Text=""{Binding No}"" />
                <Label Grid.Column=""1"" Text=""{Binding Name}"" />
                <Label Grid.Column=""2"" Text=""{Binding Status}"" />
            </Grid>
        </DataTemplate>
    </CollectionView.ItemTemplate>
</CollectionView>"
        },
        new()
        {
            Id = "timespinner", SortOrder = 120, NavLabel = "TimeSpinner",
            Title = "TimeSpinner — DreamineTimeSpinner",
            Description = "시:분:초를 ▲▼ 버튼으로 조절합니다. WPF에서는 스크롤 휠과 키보드도 지원합니다.",
            DescriptionEn = "Adjust hours, minutes, seconds with ▲▼ buttons. WPF also supports scroll wheel and keyboard.",
            WpfShot = "/img/demos/timespinner-wpf.svg", WinFormsShot = "", MauiShot = "/img/demos/timespinner-maui.svg",
            BlazorCode = @"<div class=""dw-timespinner"">
    <div>
        <button @onclick=""@(() => Vm.Hours++)"">▲</button>
        <input type=""number"" min=""0"" max=""23"" @bind=""Vm.Hours"" />
        <button @onclick=""@(() => Vm.Hours--)"">▼</button>
    </div>
    <span>:</span>
    <!-- Minutes and seconds use the same structure. -->
</div>
<p>Time: @Vm.TimeDisplay</p>",
            VmCode = @"[DreamineProperty] private int _hours = 9;
[DreamineProperty] private int _minutes = 30;
[DreamineProperty] private int _seconds;

public string TimeDisplay => $""{Hours:D2}:{Minutes:D2}:{Seconds:D2}"";",
            WpfCode = @"<ctrl:DreamineTimeSpinner
    Time=""{Binding Time, Mode=TwoWay}""
    Width=""190"" Height=""40""
    Foreground=""White"" Background=""#FF162040"" />
<TextBlock Text=""{Binding Time, StringFormat='Selected time: {0:hh\\:mm\\:ss}'}"" />",
            WinFormsCode = @"var picker = new DateTimePicker {
    Format = DateTimePickerFormat.Custom,
    CustomFormat = ""HH:mm:ss"",
    ShowUpDown = true
};
picker.DataBindings.Add(""Value"", vm, nameof(vm.Time),
    false, DataSourceUpdateMode.OnPropertyChanged);",
            MauiCode = @"<!-- MAUI - TimePicker -->
<TimePicker Time=""{Binding Time}""
            Format=""HH:mm:ss"" />
<Label Text=""{Binding Time, StringFormat='Selected time: {0:hh\\:mm\\:ss}'}"" />"
        },
        new()
        {
            Id = "image", SortOrder = 130, NavLabel = "Image",
            Title = "Image — DreamineImage",
            Description = "CornerRadius로 둥근 모서리, ClickCommand로 클릭 이벤트를 바인딩합니다. FallbackSource로 로드 실패 시 대체 이미지도 지정 가능합니다.",
            DescriptionEn = "CornerRadius for rounded corners, ClickCommand for click events. FallbackSource provides a fallback on load failure.",
            WpfShot = "/img/demos/image-wpf.svg", WinFormsShot = "", MauiShot = "/img/demos/image-maui.svg",
            BlazorCode = @"<div class=""dw-image-demo"" @onclick=""Vm.ClickImage"">
    <svg viewBox=""0 0 100 100"" width=""80"" height=""80"">
        <circle cx=""50"" cy=""50"" r=""50"" fill=""#1E90FF""/>
        <circle cx=""50"" cy=""50"" r=""22"" fill=""#fff""/>
    </svg>
</div>
<span>Image clicks @Vm.ImageClickCount</span>",
            VmCode = @"[DreamineProperty] private int _imageClickCount;

[DreamineCommand]
private void ClickImage() => ImageClickCount++;",
            WpfCode = @"<ctrl:DreamineImage
    Source=""{StaticResource DemoDrawingImage}""
    CornerRadius=""12""
    Width=""64"" Height=""64""
    ClickCommand=""{Binding ClickMeCommand}"" />
<!-- FallbackSource can provide a replacement image when loading fails. -->",
            WinFormsCode = @"var image = new PictureBox {
    ImageLocation = ""demo_icon.png"",
    SizeMode = PictureBoxSizeMode.Zoom,
    Width = 64,
    Height = 64
};
image.Click += (_, _) => vm.ClickMeCommand.Execute(null);",
            MauiCode = @"<Image Source=""demo_icon.png""
       WidthRequest=""64"" HeightRequest=""64"">
    <Image.GestureRecognizers>
        <TapGestureRecognizer Command=""{Binding ClickMeCommand}"" />
    </Image.GestureRecognizers>
</Image>"
        },
        new()
        {
            Id = "popup", SortOrder = 140, NavLabel = "Popup",
            Title = "Popup — MessageBox & BlinkPopup",
            Description = "메시지 박스와 점멸(Blink) 팝업. 설비 알람처럼 색이 번갈아 깜빡이는 모달을 띄우고, 자동 닫힘(카운트다운)도 지원합니다. 결과는 await로 받습니다.",
            DescriptionEn = "Message box and blinking popup. Show an alarm-style modal whose colors alternate, with optional auto-close countdown. The result is awaited.",
            WpfShot = "", WinFormsShot = "", MauiShot = "",
            BlazorCode = @"@* @using Dreamine.UI.Blazor
   @inject DreamineDialogService Dialog
   페이지 어딘가에 <DreamineDialogHost /> 를 한 번 배치해야 합니다. *@
<DreamineButton OnClick=""ShowMessageBox"">DreamineMessageBox (5s)</DreamineButton>
<DreamineButton Variant=""DreamineButtonVariant.Danger"" OnClick=""ShowAlarm"">ALARM (blink)</DreamineButton>

@code {
    private async Task ShowMessageBox()
    {
        var r = await Dialog.ShowMessageBoxAsync(
            ""Message box demo."", ""MessageBox"", autoClickDelaySeconds: 5);
    }

    private async Task ShowAlarm()
    {
        var r = await Dialog.ShowBlinkAsync(new BlinkPopupOptions
        {
            Title = ""⚠ ALARM"", Message = ""Equipment fault detected."",
            UseBlink = true, BlinkIntervalMs = 400,
            Color1 = ""#B41E1E"", Color2 = ""#500A0A"",
            ForegroundColor = ""#FFD700"", OkText = ""OK"", CancelText = ""Cancel""
        });
    }
}",
            VmCode = @"// DreamineDialogService는 DI에 Scoped로 등록합니다.
//   builder.Services.AddScoped<DreamineDialogService>();
// WPF의 IPopupService / WinForms·MAUI의 DreamineMessageBox·DreamineBlinkPopup과
// 같은 역할을 하며, Blazor에서는 <DreamineDialogHost /> 오버레이가 실제로 그립니다.
DreamineDialogResult result = await Dialog.ShowMessageBoxAsync(message, title);",
            WpfCode = @"// WPF — Dreamine.UI.Abstractions.Popup.IPopupService
var result = _popup.ShowMessageBox(
    ""Message box demo."", ""MessageBox"",
    autoClickDelaySeconds: 5);

_popup.ShowBlink(new BlinkPopupOptions
{
    Title = ""⚠ ALARM"", Message = ""Equipment fault detected."",
    UseBlink = true, BlinkIntervalMs = 400,
    Color1 = ""#B41E1E"", Color2 = ""#500A0A"",
    ForegroundColor = ""#FFD700""
});",
            WinFormsCode = @"// WinForms — DreamineMessageBox / DreamineBlinkPopup
var result = DreamineMessageBox.Show(
    ""Message box demo."", ""MessageBox"",
    autoClickDelaySeconds: 5);

DreamineBlinkPopup.Show(new BlinkPopupOptions
{
    Title = ""⚠ ALARM"", Message = ""Equipment fault detected."",
    UseBlink = true, BlinkIntervalMs = 400,
    Color1 = Color.FromArgb(0xB4, 0x1E, 0x1E),
    Color2 = Color.FromArgb(0x50, 0x0A, 0x0A)
});",
            MauiCode = @"<!-- MAUI — same DreamineMessageBox / DreamineBlinkPopup API -->
var result = await DreamineMessageBox.ShowAsync(
    ""Message box demo."", ""MessageBox"",
    autoClickDelaySeconds: 5);

await DreamineBlinkPopup.ShowAsync(new BlinkPopupOptions
{
    Title = ""⚠ ALARM"", Message = ""Equipment fault detected."",
    UseBlink = true, BlinkIntervalMs = 400,
    Color1 = ""#B41E1E"", Color2 = ""#500A0A""
});"
        },
    ];
}
