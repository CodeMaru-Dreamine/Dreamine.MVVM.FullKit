using System.Windows;
using System.Windows.Input;
using FamiliesAutoWriter.ViewModels;

namespace FamiliesAutoWriter;

public partial class MainWindow : Window
{
    private bool _webReady = false;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await WebBrowser.EnsureCoreWebView2Async();
        _webReady = true;

        var vm = (MainViewModel)DataContext;

        // AI 스크립트 실행 위임
        vm.ExecuteScriptAsync = async script =>
            await WebBrowser.ExecuteScriptAsync(script);

        // 페이지 이동 시 URL 바 동기화
        WebBrowser.CoreWebView2.SourceChanged += (_, _) =>
        {
            vm.BrowserUrl = WebBrowser.CoreWebView2.Source;
        };

        // 초기 페이지 로드
        WebBrowser.CoreWebView2.Navigate(vm.BrowserUrl);
    }

    // ── 네비게이션 ────────────────────────────────────────────────
    private void OnNavigateClick(object sender, RoutedEventArgs e) => Navigate();
    private void OnUrlKeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) Navigate(); }

    private void Navigate()
    {
        if (!_webReady) return;
        var vm = (MainViewModel)DataContext;
        var url = vm.BrowserUrl.Trim();
        if (!url.StartsWith("http")) url = "https://" + url;
        vm.BrowserUrl = url;
        WebBrowser.CoreWebView2.Navigate(url);
    }

    private void OnClaudeClick(object sender, RoutedEventArgs e) =>
        NavigateTo("https://claude.ai/new");

    private void OnChatGptClick(object sender, RoutedEventArgs e) =>
        NavigateTo("https://chatgpt.com/");

    private void OnGeminiClick(object sender, RoutedEventArgs e) =>
        NavigateTo("https://gemini.google.com/app");

    private void NavigateTo(string url)
    {
        if (!_webReady) return;
        ((MainViewModel)DataContext).BrowserUrl = url;
        WebBrowser.CoreWebView2.Navigate(url);
    }
}
