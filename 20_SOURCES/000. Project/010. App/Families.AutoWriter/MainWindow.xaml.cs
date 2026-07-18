using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using FamiliesAutoWriter.ViewModels;
using Microsoft.Web.WebView2.Wpf;

namespace FamiliesAutoWriter;

/// <summary>
/// \if KO
/// <para>Main Window 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates main window functionality and related state.</para>
/// \endif
/// </summary>
public partial class MainWindow : Window
{
    // ── 탭 상태 ─────────────────────────────────────────────────
    /// <summary>
    /// \if KO
    /// <para>Browser Tab 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates browser tab functionality and related state.</para>
    /// \endif
    /// </summary>
    private sealed class BrowserTab
    {
        /// <summary>
        /// \if KO
        /// <para>Id 값을 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets the id value.</para>
        /// \endif
        /// </summary>
        public required string         Id         { get; init; }
        /// <summary>
        /// \if KO
        /// <para>Label 값을 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets the label value.</para>
        /// \endif
        /// </summary>
        public required string         Label      { get; set; }
        /// <summary>
        /// \if KO
        /// <para>Web View 값을 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets the web view value.</para>
        /// \endif
        /// </summary>
        public required WebView2       WebView    { get; init; }
        /// <summary>
        /// \if KO
        /// <para>Session 값을 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets the session value.</para>
        /// \endif
        /// </summary>
        public required WriterSession  Session    { get; init; }
        /// <summary>
        /// \if KO
        /// <para>Header Btn 값을 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets the header btn value.</para>
        /// \endif
        /// </summary>
        public ToggleButton?           HeaderBtn  { get; set; }
        /// <summary>
        /// \if KO
        /// <para>Is Ready 값을 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets the is ready value.</para>
        /// \endif
        /// </summary>
        public bool                    IsReady    { get; set; }
        /// <summary>
        /// \if KO
        /// <para>Current Url 값을 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets the current url value.</para>
        /// \endif
        /// </summary>
        public string                  CurrentUrl { get; set; } = "https://claude.ai/new";
    }

    /// <summary>
    /// \if KO
    /// <para>tabs 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the tabs value.</para>
    /// \endif
    /// </summary>
    private readonly List<BrowserTab> _tabs = [];
    /// <summary>
    /// \if KO
    /// <para>active Tab 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the active tab value.</para>
    /// \endif
    /// </summary>
    private BrowserTab? _activeTab;
    /// <summary>
    /// \if KO
    /// <para>tab Seq 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the tab seq value.</para>
    /// \endif
    /// </summary>
    private int _tabSeq = 0;

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="MainWindow"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="MainWindow"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    // ── 초기화 ───────────────────────────────────────────────────
    /// <summary>
    /// \if KO
    /// <para>Loaded 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the loaded event or state change.</para>
    /// \endif
    /// </summary>
    /// <param name="sender">
    /// \if KO
    /// <para>이벤트를 발생시킨 객체입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The object that raised the event.</para>
    /// \endif
    /// </param>
    /// <param name="e">
    /// \if KO
    /// <para>이벤트와 관련된 데이터를 포함합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Contains data associated with the event.</para>
    /// \endif
    /// </param>
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        NewTabNameBox.Focus();
    }

    /// <summary>
    /// \if KO
    /// <para>Closed 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the closed event or state change.</para>
    /// \endif
    /// </summary>
    /// <param name="sender">
    /// \if KO
    /// <para>이벤트를 발생시킨 객체입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The object that raised the event.</para>
    /// \endif
    /// </param>
    /// <param name="e">
    /// \if KO
    /// <para>이벤트와 관련된 데이터를 포함합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Contains data associated with the event.</para>
    /// \endif
    /// </param>
    private void OnClosed(object? sender, EventArgs e)
    {
        foreach (var tab in _tabs)
            tab.Session.StopAutomation();
    }

    // ── 탭 생성 ─────────────────────────────────────────────────
    /// <summary>
    /// \if KO
    /// <para>Tab Async 값을 생성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Creates the tab async value.</para>
    /// \endif
    /// </summary>
    /// <param name="label">
    /// \if KO
    /// <para>label에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for label.</para>
    /// \endif
    /// </param>
    /// <param name="url">
    /// \if KO
    /// <para>url에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for url.</para>
    /// \endif
    /// </param>
    /// <param name="initialPrompt">
    /// \if KO
    /// <para>initial Prompt에 사용할 <c>string?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for initial prompt.</para>
    /// \endif
    /// </param>
    /// <param name="mode">
    /// \if KO
    /// <para>mode에 사용할 <c>SessionMode</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>SessionMode</c> value used for mode.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Create Tab Async 작업에서 생성한 <c>Task&lt;BrowserTab&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;BrowserTab&gt;</c> result produced by the create tab async operation.</para>
    /// \endif
    /// </returns>
    private async Task<BrowserTab> CreateTabAsync(string label, string url, string? initialPrompt = null, SessionMode mode = SessionMode.Travel)
    {
        var vm      = (MainViewModel)DataContext;
        var session = new WriterSession(initialPrompt) { Mode = mode };

        var wv = new WebView2 { Visibility = Visibility.Hidden };
        WebViewHost.Children.Add(wv);

        var tab = new BrowserTab
        {
            Id         = $"tab{++_tabSeq}",
            Label      = label,
            WebView    = wv,
            Session    = session,
            CurrentUrl = url,
        };
        session.NavigateTab = signal => HandleNavigateTab(tab, signal);
        _tabs.Add(tab);
        vm.Sessions.Add(session);

        // 탭 헤더 버튼 생성
        var btnPanel = new StackPanel { Orientation = Orientation.Horizontal };
        btnPanel.Children.Add(new TextBlock
        {
            Text = label,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 6, 0),
        });
        var close = new Button
        {
            Content = "×",
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Foreground = new SolidColorBrush(Color.FromRgb(0x94, 0xa3, 0xb8)),
            FontSize = 13,
            Cursor = Cursors.Hand,
            Padding = new Thickness(0),
            Width = 16,
        };
        close.Click += (_, _) => CloseTab(tab);
        btnPanel.Children.Add(close);

        var toggle = new ToggleButton
        {
            Content = btnPanel,
            Style = (Style)Resources["TabBtn"],
            Margin = new Thickness(0, 0, 3, 0),
            Tag = tab,
        };
        toggle.Click += (_, _) => ActivateTab(tab);
        tab.HeaderBtn = toggle;
        TabHeaderPanel.Children.Add(toggle);

        // WebView2 초기화
        await wv.EnsureCoreWebView2Async();
        tab.IsReady = true;
        tab.Session.ExecuteScriptAsync = async script =>
            tab.IsReady ? await wv.ExecuteScriptAsync(script) : null;

        wv.CoreWebView2.SourceChanged += (_, _) =>
        {
            tab.CurrentUrl = wv.CoreWebView2.Source;
            if (_activeTab == tab)
                Dispatcher.Invoke(() => UrlBox.Text = tab.CurrentUrl);
        };
        wv.CoreWebView2.Navigate(url);

        return tab;
    }

    // ── 탭 활성화 ────────────────────────────────────────────────
    /// <summary>
    /// \if KO
    /// <para>Activate Tab 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the activate tab operation.</para>
    /// \endif
    /// </summary>
    /// <param name="tab">
    /// \if KO
    /// <para>tab에 사용할 <c>BrowserTab</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>BrowserTab</c> value used for tab.</para>
    /// \endif
    /// </param>
    private void ActivateTab(BrowserTab tab)
    {
        _activeTab = tab;
        var vm = (MainViewModel)DataContext;

        foreach (var t in _tabs)
        {
            // Collapsed이면 숨겨진 WebView2에서 JS focus()가 안 됨 → Hidden으로 레이아웃 유지
            t.WebView.Visibility = t == tab ? Visibility.Visible : Visibility.Hidden;
            if (t.HeaderBtn != null)
                t.HeaderBtn.IsChecked = t == tab;
        }

        UrlBox.Text = tab.CurrentUrl;

        // 왼쪽 패널이 이 탭의 세션을 바라보도록 전환
        vm.ActiveSession = tab.Session;

        // 이 탭의 WebView 스크립트 실행 위임 갱신 (IsReady 여부에 따라)
        tab.Session.ExecuteScriptAsync = async script =>
            tab.IsReady ? await tab.WebView.ExecuteScriptAsync(script) : null;
    }

    // ── 탭 닫기 ─────────────────────────────────────────────────
    /// <summary>
    /// \if KO
    /// <para>Close Tab 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the close tab operation.</para>
    /// \endif
    /// </summary>
    /// <param name="tab">
    /// \if KO
    /// <para>tab에 사용할 <c>BrowserTab</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>BrowserTab</c> value used for tab.</para>
    /// \endif
    /// </param>
    private void CloseTab(BrowserTab tab)
    {
        if (_tabs.Count <= 1) return;

        var vm  = (MainViewModel)DataContext;
        var idx = _tabs.IndexOf(tab);
        tab.Session.StopAutomation();
        _tabs.Remove(tab);
        vm.Sessions.Remove(tab.Session);

        if (tab.HeaderBtn != null)
            TabHeaderPanel.Children.Remove(tab.HeaderBtn);
        WebViewHost.Children.Remove(tab.WebView);

        if (_activeTab == tab)
        {
            var next = idx < _tabs.Count ? _tabs[idx] : _tabs[^1];
            ActivateTab(next);
        }
    }

    // ── ViewModel(세션)에서 요청하는 탭 이동 ───────────────────
    /// <summary>
    /// \if KO
    /// <para>Handle Navigate Tab 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the handle navigate tab operation.</para>
    /// \endif
    /// </summary>
    /// <param name="tab">
    /// \if KO
    /// <para>tab에 사용할 <c>BrowserTab</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>BrowserTab</c> value used for tab.</para>
    /// \endif
    /// </param>
    /// <param name="signal">
    /// \if KO
    /// <para>signal에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for signal.</para>
    /// \endif
    /// </param>
    private void HandleNavigateTab(BrowserTab tab, string signal)
    {
        if (!tab.IsReady) return;

        var currentUrl = tab.CurrentUrl;
        string newUrl;
        if (currentUrl.Contains("chatgpt.com", StringComparison.OrdinalIgnoreCase))
            newUrl = "https://chatgpt.com/";
        else if (currentUrl.Contains("gemini.google.com", StringComparison.OrdinalIgnoreCase))
            newUrl = "https://gemini.google.com/app";
        else
            newUrl = "https://claude.ai/new";

        Dispatcher.Invoke(() =>
        {
            tab.WebView.CoreWebView2.Navigate(newUrl);
            tab.CurrentUrl = newUrl;
            if (_activeTab == tab)
                UrlBox.Text = newUrl;
        });
    }

    // ── 탭 추가 버튼 / Enter키 ──────────────────────────────────
    /// <summary>
    /// \if KO
    /// <para>Add Tab Click 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the add tab click event or state change.</para>
    /// \endif
    /// </summary>
    /// <param name="sender">
    /// \if KO
    /// <para>이벤트를 발생시킨 객체입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The object that raised the event.</para>
    /// \endif
    /// </param>
    /// <param name="e">
    /// \if KO
    /// <para>이벤트와 관련된 데이터를 포함합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Contains data associated with the event.</para>
    /// \endif
    /// </param>
    private async void OnAddTabClick(object sender, RoutedEventArgs e) => await AddTabFromInputAsync();
    /// <summary>
    /// \if KO
    /// <para>New Tab Name Key Down 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the new tab name key down event or state change.</para>
    /// \endif
    /// </summary>
    /// <param name="sender">
    /// \if KO
    /// <para>이벤트를 발생시킨 객체입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The object that raised the event.</para>
    /// \endif
    /// </param>
    /// <param name="e">
    /// \if KO
    /// <para>이벤트와 관련된 데이터를 포함합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Contains data associated with the event.</para>
    /// \endif
    /// </param>
    private async void OnNewTabNameKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) await AddTabFromInputAsync();
    }

    /// <summary>
    /// \if KO
    /// <para>Tab From Input Async 항목을 추가합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Adds the tab from input async item.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Add Tab From Input Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the add tab from input async operation.</para>
    /// \endif
    /// </returns>
    private async Task AddTabFromInputAsync()
    {
        var label = NewTabNameBox.Text.Trim();
        if (string.IsNullOrEmpty(label)) label = $"탭 {_tabSeq + 1}";

        var modeItem = NewTabModeBox.SelectedItem as System.Windows.Controls.ComboBoxItem;
        var isCooking = modeItem?.Content?.ToString() == "요리";
        var mode = isCooking ? SessionMode.Cooking : SessionMode.Travel;
        var prompt = isCooking ? WriterSession.BuildCookingPrompt([]) : WriterSession.BuildTravelPrompt([]);

        var tab = await CreateTabAsync(label, "https://claude.ai/new", prompt, mode);
        ActivateTab(tab);

        NewTabNameBox.Clear();
        NewTabNameBox.Focus();
    }

    // ── URL 바 네비게이션 ────────────────────────────────────────
    /// <summary>
    /// \if KO
    /// <para>Navigate Click 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the navigate click event or state change.</para>
    /// \endif
    /// </summary>
    /// <param name="sender">
    /// \if KO
    /// <para>이벤트를 발생시킨 객체입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The object that raised the event.</para>
    /// \endif
    /// </param>
    /// <param name="e">
    /// \if KO
    /// <para>이벤트와 관련된 데이터를 포함합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Contains data associated with the event.</para>
    /// \endif
    /// </param>
    private void OnNavigateClick(object sender, RoutedEventArgs e)  => NavigateCurrent();
    /// <summary>
    /// \if KO
    /// <para>Url Key Down 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the url key down event or state change.</para>
    /// \endif
    /// </summary>
    /// <param name="sender">
    /// \if KO
    /// <para>이벤트를 발생시킨 객체입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The object that raised the event.</para>
    /// \endif
    /// </param>
    /// <param name="e">
    /// \if KO
    /// <para>이벤트와 관련된 데이터를 포함합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Contains data associated with the event.</para>
    /// \endif
    /// </param>
    private void OnUrlKeyDown(object sender, KeyEventArgs e)         { if (e.Key == Key.Enter) NavigateCurrent(); }
    /// <summary>
    /// \if KO
    /// <para>Back Click 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the back click event or state change.</para>
    /// \endif
    /// </summary>
    /// <param name="sender">
    /// \if KO
    /// <para>이벤트를 발생시킨 객체입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The object that raised the event.</para>
    /// \endif
    /// </param>
    /// <param name="e">
    /// \if KO
    /// <para>이벤트와 관련된 데이터를 포함합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Contains data associated with the event.</para>
    /// \endif
    /// </param>
    private void OnBackClick(object sender, RoutedEventArgs e)       => _activeTab?.WebView.GoBack();

    /// <summary>
    /// \if KO
    /// <para>Navigate Current 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the navigate current operation.</para>
    /// \endif
    /// </summary>
    private void NavigateCurrent()
    {
        if (_activeTab == null || !_activeTab.IsReady) return;
        var url = UrlBox.Text.Trim();
        if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase)) url = "https://" + url;
        UrlBox.Text = url;
        _activeTab.WebView.CoreWebView2.Navigate(url);
    }

    // ── 빠른 이동 버튼 ──────────────────────────────────────────
    /// <summary>
    /// \if KO
    /// <para>Claude Click 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the claude click event or state change.</para>
    /// \endif
    /// </summary>
    /// <param name="sender">
    /// \if KO
    /// <para>이벤트를 발생시킨 객체입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The object that raised the event.</para>
    /// \endif
    /// </param>
    /// <param name="e">
    /// \if KO
    /// <para>이벤트와 관련된 데이터를 포함합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Contains data associated with the event.</para>
    /// \endif
    /// </param>
    private void OnClaudeClick(object sender, RoutedEventArgs e)   => NavigateTo("https://claude.ai/new");
    /// <summary>
    /// \if KO
    /// <para>Chat Gpt Click 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the chat gpt click event or state change.</para>
    /// \endif
    /// </summary>
    /// <param name="sender">
    /// \if KO
    /// <para>이벤트를 발생시킨 객체입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The object that raised the event.</para>
    /// \endif
    /// </param>
    /// <param name="e">
    /// \if KO
    /// <para>이벤트와 관련된 데이터를 포함합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Contains data associated with the event.</para>
    /// \endif
    /// </param>
    private void OnChatGptClick(object sender, RoutedEventArgs e)  => NavigateTo("https://chatgpt.com/");
    /// <summary>
    /// \if KO
    /// <para>Gemini Click 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the gemini click event or state change.</para>
    /// \endif
    /// </summary>
    /// <param name="sender">
    /// \if KO
    /// <para>이벤트를 발생시킨 객체입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The object that raised the event.</para>
    /// \endif
    /// </param>
    /// <param name="e">
    /// \if KO
    /// <para>이벤트와 관련된 데이터를 포함합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Contains data associated with the event.</para>
    /// \endif
    /// </param>
    private void OnGeminiClick(object sender, RoutedEventArgs e)   => NavigateTo("https://gemini.google.com/app");

    /// <summary>
    /// \if KO
    /// <para>Navigate To 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the navigate to operation.</para>
    /// \endif
    /// </summary>
    /// <param name="url">
    /// \if KO
    /// <para>url에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for url.</para>
    /// \endif
    /// </param>
    private void NavigateTo(string url)
    {
        if (_activeTab == null || !_activeTab.IsReady) return;
        UrlBox.Text = url;
        _activeTab.WebView.CoreWebView2.Navigate(url);
    }
}
