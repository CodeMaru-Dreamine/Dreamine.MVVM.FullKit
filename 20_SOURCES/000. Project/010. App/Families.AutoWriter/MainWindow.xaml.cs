using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using FamiliesAutoWriter.ViewModels;
using Microsoft.Web.WebView2.Wpf;

namespace FamiliesAutoWriter;

public partial class MainWindow : Window
{
    // ── 탭 상태 ─────────────────────────────────────────────────
    private sealed class BrowserTab
    {
        public required string         Id         { get; init; }
        public required string         Label      { get; set; }
        public required WebView2       WebView    { get; init; }
        public required WriterSession  Session    { get; init; }
        public ToggleButton?           HeaderBtn  { get; set; }
        public bool                    IsReady    { get; set; }
        public string                  CurrentUrl { get; set; } = "https://claude.ai/new";
    }

    private readonly List<BrowserTab> _tabs = [];
    private BrowserTab? _activeTab;
    private int _tabSeq = 0;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    // ── 초기화 ───────────────────────────────────────────────────
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        NewTabNameBox.Focus();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        foreach (var tab in _tabs)
            tab.Session.StopAutomation();
    }

    // ── 탭 생성 ─────────────────────────────────────────────────
    private async Task<BrowserTab> CreateTabAsync(string label, string url, string? initialPrompt = null, SessionMode mode = SessionMode.Travel)
    {
        var vm      = (MainViewModel)DataContext;
        var session = new WriterSession(initialPrompt) { Mode = mode };

        var wv = new WebView2 { Visibility = Visibility.Collapsed };
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
    private void ActivateTab(BrowserTab tab)
    {
        _activeTab = tab;
        var vm = (MainViewModel)DataContext;

        foreach (var t in _tabs)
        {
            t.WebView.Visibility = t == tab ? Visibility.Visible : Visibility.Collapsed;
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
    private async void OnAddTabClick(object sender, RoutedEventArgs e) => await AddTabFromInputAsync();
    private async void OnNewTabNameKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) await AddTabFromInputAsync();
    }

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
    private void OnNavigateClick(object sender, RoutedEventArgs e)  => NavigateCurrent();
    private void OnUrlKeyDown(object sender, KeyEventArgs e)         { if (e.Key == Key.Enter) NavigateCurrent(); }
    private void OnBackClick(object sender, RoutedEventArgs e)       => _activeTab?.WebView.GoBack();

    private void NavigateCurrent()
    {
        if (_activeTab == null || !_activeTab.IsReady) return;
        var url = UrlBox.Text.Trim();
        if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase)) url = "https://" + url;
        UrlBox.Text = url;
        _activeTab.WebView.CoreWebView2.Navigate(url);
    }

    // ── 빠른 이동 버튼 ──────────────────────────────────────────
    private void OnClaudeClick(object sender, RoutedEventArgs e)   => NavigateTo("https://claude.ai/new");
    private void OnChatGptClick(object sender, RoutedEventArgs e)  => NavigateTo("https://chatgpt.com/");
    private void OnGeminiClick(object sender, RoutedEventArgs e)   => NavigateTo("https://gemini.google.com/app");

    private void NavigateTo(string url)
    {
        if (_activeTab == null || !_activeTab.IsReady) return;
        UrlBox.Text = url;
        _activeTab.WebView.CoreWebView2.Navigate(url);
    }
}
