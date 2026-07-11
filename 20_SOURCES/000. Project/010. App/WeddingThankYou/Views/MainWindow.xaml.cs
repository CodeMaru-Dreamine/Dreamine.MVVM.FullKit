using Dreamine.Hybrid.Wpf.Controls;
using Dreamine.Hybrid.Wpf.Hosting;
using Microsoft.Web.WebView2.Wpf;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;

namespace WeddingThankYou.Views;

public partial class MainWindow : Window
{
    private readonly DreamineBlazorServerHostOptions _serverOptions;

    public MainWindow(DreamineBlazorServerHostOptions serverOptions)
    {
        _serverOptions = serverOptions ?? throw new ArgumentNullException(nameof(serverOptions));
        InitializeComponent();

        if (DesignerProperties.GetIsInDesignMode(this)) return;

        Loaded += OnLoaded;
        Closed += (_, _) => Application.Current?.Shutdown();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var baseUrl = $"http://127.0.0.1:{_serverOptions.Port}";
            if (!_serverOptions.UseEmbeddedWebView)
            {
                await OpenExternalAdminAsync(baseUrl, _serverOptions.InstanceId, "/admin");
                return;
            }

            var webView = HybridWebViewHost.CreateWebView();
            WebViewHost.Child = webView;

            await NavigateAsync(webView, baseUrl, _serverOptions.InstanceId, "/admin");
        }
        catch (Exception ex)
        {
            ShowStartupError(ex);
        }
    }

    private async Task OpenExternalAdminAsync(string baseUrl, string expectedInstanceId,
        string path = "/admin", int timeoutMs = 15000, int intervalMs = 500)
    {
        var targetUrl = $"{baseUrl}{path}";
        var alive = await WaitForInstanceAsync(baseUrl, expectedInstanceId, timeoutMs, intervalMs);
        if (!alive)
        {
            ShowExternalBrowserMessage(targetUrl, "관리자 서버를 아직 확인하지 못했습니다.");
            return;
        }

        OpenUrl(targetUrl);
        ShowExternalBrowserMessage(targetUrl, "관리자 화면을 기본 브라우저에서 열었습니다.");
    }

    private static async Task NavigateAsync(WebView2 webView, string baseUrl, string expectedInstanceId,
        string path = "/admin", int timeoutMs = 15000, int intervalMs = 500)
    {
        var targetUrl = $"{baseUrl}{path}";
        try
        {
            if (!await WaitForInstanceAsync(baseUrl, expectedInstanceId, timeoutMs, intervalMs))
            {
                await HybridWebViewHost.ShowOfflineMessageAsync(webView, targetUrl);
                return;
            }

            webView.Source = new Uri(targetUrl);
        }
        catch (TaskCanceledException) { }
        catch (COMException ex)
        {
            ShowStartupError(ex);
        }
        catch (InvalidOperationException ex)
        {
            ShowStartupError(ex);
        }
    }

    private static async Task<bool> WaitForInstanceAsync(string baseUrl, string expectedInstanceId,
        int timeoutMs, int intervalMs)
    {
        var instanceUrl = $"{baseUrl}/_dreamine/instance";
        await Task.Delay(1200);

        using var cts = new CancellationTokenSource(timeoutMs);
        using var http = new HttpClient();

        while (!cts.IsCancellationRequested)
        {
            try
            {
                var id = await http.GetStringAsync(instanceUrl, cts.Token);
                if (string.Equals(id.Trim(), expectedInstanceId, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            catch (HttpRequestException) { }
            catch (TaskCanceledException) { }

            await Task.Delay(intervalMs, cts.Token);
        }

        return false;
    }

    private void ShowExternalBrowserMessage(string targetUrl, string message)
    {
        var panel = new StackPanel
        {
            Margin = new Thickness(32),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            MaxWidth = 520
        };

        panel.Children.Add(new TextBlock
        {
            Text = message,
            FontSize = 22,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 12)
        });
        panel.Children.Add(new TextBlock
        {
            Text = targetUrl,
            FontSize = 14,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 20)
        });

        var button = new Button
        {
            Content = "관리자 화면 다시 열기",
            Padding = new Thickness(18, 8, 18, 8),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        button.Click += (_, _) => OpenUrl(targetUrl);
        panel.Children.Add(button);

        WebViewHost.Child = panel;
    }

    private static void OpenUrl(string url)
    {
        Process.Start(new ProcessStartInfo(url)
        {
            UseShellExecute = true
        });
    }

    private static void ShowStartupError(Exception ex)
    {
        Debug.WriteLine($"[WeddingThankYou.WebView2.StartupFailed] {ex}");
        MessageBox.Show(
            $"관리자 화면(WebView2)을 초기화하지 못했습니다.\n\n{ex.Message}",
            "Wedding ThankYou",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}
