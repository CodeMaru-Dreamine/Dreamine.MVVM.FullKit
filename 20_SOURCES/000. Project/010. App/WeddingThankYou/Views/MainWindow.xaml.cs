using Dreamine.Hybrid.Wpf.Controls;
using Dreamine.Hybrid.Wpf.Hosting;
using Microsoft.Web.WebView2.Wpf;
using System.ComponentModel;
using System.Net.Http;
using System.Windows;

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
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var webView = HybridWebViewHost.CreateWebView();
        WebViewHost.Child = webView;

        await NavigateAsync(webView, $"http://localhost:{_serverOptions.Port}",
            _serverOptions.InstanceId, "/admin");
    }

    private static async Task NavigateAsync(WebView2 webView, string baseUrl, string expectedInstanceId,
        string path = "/admin", int timeoutMs = 15000, int intervalMs = 500)
    {
        var targetUrl = $"{baseUrl}{path}";
        var instanceUrl = $"{baseUrl}/_dreamine/instance";
        try
        {
            await Task.Delay(1200);

            using var cts = new CancellationTokenSource(timeoutMs);
            using var http = new HttpClient();
            var alive = false;

            while (!cts.IsCancellationRequested)
            {
                try
                {
                    var id = await http.GetStringAsync(instanceUrl, cts.Token);
                    if (string.Equals(id.Trim(), expectedInstanceId, StringComparison.Ordinal))
                    { alive = true; break; }
                }
                catch (HttpRequestException) { }
                catch (TaskCanceledException) { }

                await Task.Delay(intervalMs, cts.Token);
            }

            if (!alive)
            {
                await HybridWebViewHost.ShowOfflineMessageAsync(webView, targetUrl);
                return;
            }

            webView.Source = new Uri(targetUrl);
        }
        catch (TaskCanceledException) { }
    }
}
