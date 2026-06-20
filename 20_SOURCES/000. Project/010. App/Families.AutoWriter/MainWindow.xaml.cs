using System.Windows;
using System.Windows.Input;
using FamiliesAutoWriter.ViewModels;

namespace FamiliesAutoWriter;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await WebBrowser.EnsureCoreWebView2Async();
        var vm = (MainViewModel)DataContext;
        WebBrowser.Source = new Uri(vm.BrowserUrl);
    }

    private void OnNavigateClick(object sender, RoutedEventArgs e) => Navigate();
    private void OnUrlKeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) Navigate(); }

    private void Navigate()
    {
        var vm = (MainViewModel)DataContext;
        var url = vm.BrowserUrl.Trim();
        if (!url.StartsWith("http")) url = "https://" + url;
        WebBrowser.Source = new Uri(url);
    }

    private void OnClaudeClick(object sender, RoutedEventArgs e)
    {
        ((MainViewModel)DataContext).BrowserUrl = "https://claude.ai/new";
        WebBrowser.Source = new Uri("https://claude.ai/new");
    }

    private void OnChatGptClick(object sender, RoutedEventArgs e)
    {
        ((MainViewModel)DataContext).BrowserUrl = "https://chatgpt.com/";
        WebBrowser.Source = new Uri("https://chatgpt.com/");
    }

    private void OnGeminiClick(object sender, RoutedEventArgs e)
    {
        ((MainViewModel)DataContext).BrowserUrl = "https://gemini.google.com/app";
        WebBrowser.Source = new Uri("https://gemini.google.com/app");
    }
}
