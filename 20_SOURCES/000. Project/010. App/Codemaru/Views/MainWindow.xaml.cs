using Dreamine.Hybrid.Wpf.Controls;
using Dreamine.Hybrid.Wpf.Hosting;
using Microsoft.Web.WebView2.Wpf;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http;
using System.Windows;

namespace Codemaru.Views;

public partial class MainWindow : Window
{
    private readonly DreamineBlazorServerHostOptions _serverOptions;

    public MainWindow(DreamineBlazorServerHostOptions serverOptions)
    {
        _serverOptions = serverOptions ?? throw new ArgumentNullException(nameof(serverOptions));

        InitializeComponent();

        if (DesignerProperties.GetIsInDesignMode(this))
        {
            return;
        }

        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var hybrid = new HybridHostControl
        {
            HostPage = "wwwroot/index.html",
            Services = App.ServiceProvider,
            RootComponentType = typeof(Codemaru.Blazor.Pages.Index),
            RootSelector = "#app"
        };
        EmbeddedTab.Content = hybrid;

        var webView = HybridWebViewHost.CreateWebView();
        ServerTab.Content = webView;

        await NavigateServerAsync(webView, $"http://localhost:{_serverOptions.Port}", _serverOptions.InstanceId);
    }

    private static async Task NavigateServerAsync(WebView2 webView, string baseUrl, string expectedInstanceId, int timeoutMs = 15000, int intervalMs = 500)
    {
        var cardHybridUrl = $"{baseUrl}/cardhybrid";
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
                    var instanceId = await http.GetStringAsync(instanceUrl, cts.Token);
                    if (string.Equals(instanceId.Trim(), expectedInstanceId, StringComparison.Ordinal))
                    {
                        alive = true;
                        break;
                    }
                }
                catch (HttpRequestException)
                {
                }
                catch (TaskCanceledException)
                {
                }

                await Task.Delay(intervalMs, cts.Token);
            }

            if (!alive)
            {
                Debug.WriteLine($"[CardHybrid] Server instance mismatch or timeout: {baseUrl}");
                await HybridWebViewHost.ShowOfflineMessageAsync(webView, cardHybridUrl);
                return;
            }

            webView.Source = new Uri(cardHybridUrl);
        }
        catch (TaskCanceledException)
        {
            Debug.WriteLine("[CardHybrid] Server navigation canceled.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[CardHybrid] {ex}");
        }
    }
}
