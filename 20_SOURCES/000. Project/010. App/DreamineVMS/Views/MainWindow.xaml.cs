using Dreamine.Hybrid.Wpf.Controls;
using Dreamine.Hybrid.Wpf.Internal;
using DreamineVMS.Blazor.Components;
using DreamineVMS.Options;
using DreamineVMS.ViewModels;
using Microsoft.Extensions.Options;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace DreamineVMS.Views;

/// <summary>
/// \brief DreamineVMS 메인 윈도우입니다.
/// </summary>
public partial class MainWindow : Window
{
    private readonly VmsServerOptions _serverOptions;
    private MainWindowViewModel? _attachedViewModel;
    private WebView2? _serverDashboardWebView;
    private WebView2? _wpfLiveWebView;

    /// <summary>
    /// \brief MainWindow 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="serverOptions">VMS 서버 옵션입니다.</param>
    public MainWindow(IOptions<VmsServerOptions> serverOptions)
    {
        _serverOptions = serverOptions?.Value ?? throw new ArgumentNullException(nameof(serverOptions));
        InitializeComponent();

        // 탭 헤더의 포트 번호를 실제 옵션 값으로 표시합니다.
        ServerDashboardTabHeader.Text = $"Server Dashboard ({_serverOptions.Port})";

        if (DesignerProperties.GetIsInDesignMode(this))
        {
            return;
        }

        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_attachedViewModel is not null)
        {
            _attachedViewModel.OpenLiveTabRequested -= OnOpenLiveTabRequested;
            _attachedViewModel = null;
        }

        if (DataContext is MainWindowViewModel viewModel)
        {
            _attachedViewModel = viewModel;
            _attachedViewModel.OpenLiveTabRequested += OnOpenLiveTabRequested;
        }
    }

    private void OnOpenLiveTabRequested(object? sender, EventArgs e)
    {
        // Server Dashboard에서 "Open Live View"를 누르면 WPF Live 탭으로 전환.
        Dispatcher.Invoke(() =>
        {
            MainTabControl.SelectedItem = WpfLiveTab;
            if (!IsActive)
            {
                Activate();
            }
        });
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        HybridHostControl embeddedDashboard = new()
        {
            HostPage = "wwwroot/index.html",
            Services = App.ServiceProvider,
            RootComponentType = typeof(VmsLocalDashboard),
            RootSelector = "#app"
        };

        EmbeddedDashboardTab.Content = embeddedDashboard;

        WebView2 serverDashboard = WebView2Initializer.CreateConfiguredWebView2();
        ServerDashboardTab.Content = serverDashboard;
        _serverDashboardWebView = serverDashboard;

        WebView2 wpfLive = WebView2Initializer.CreateConfiguredWebView2();
        WpfLiveTab.Content = wpfLive;
        _wpfLiveWebView = wpfLive;

        string baseUrl = $"http://localhost:{_serverOptions.Port}";

        // 두 WebView가 같은 Blazor Server를 바라보므로 가용성 체크는 한 번이면 충분합니다.
        // 다만 헬퍼는 URL 단위로 동작하므로 동시에 실행하고 끝납니다.
        Task dashboardTask = NavigateServerAsync(serverDashboard, baseUrl);
        Task liveTask = NavigateServerAsync(wpfLive, $"{baseUrl}/live");

        await Task.WhenAll(dashboardTask, liveTask).ConfigureAwait(true);
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        if (_attachedViewModel is not null)
        {
            _attachedViewModel.OpenLiveTabRequested -= OnOpenLiveTabRequested;
            _attachedViewModel = null;
        }

        // WebView2 인스턴스를 명시적으로 정리합니다.
        // Dispose하지 않으면 Chromium 서브프로세스가 살아남아 앱 종료를 막을 수 있습니다.
        TryDisposeWebView(ref _wpfLiveWebView);
        TryDisposeWebView(ref _serverDashboardWebView);

        // WPF Shutdown을 명시적으로 트리거합니다.
        // host.RunDreamineWpfApp의 구현에 따라 자동 종료가 안 될 수 있으므로 안전망입니다.
        Application.Current?.Shutdown();
    }

    private static void TryDisposeWebView(ref WebView2? webView)
    {
        if (webView is null)
        {
            return;
        }

        try
        {
            webView.Dispose();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DreamineVMS] WebView2 dispose failed: {ex}");
        }

        webView = null;
    }

    private static async Task NavigateServerAsync(WebView2 webView, string url, int timeoutMs = 15000, int intervalMs = 500)
    {
        try
        {
            await Task.Delay(1500).ConfigureAwait(true);

            using CancellationTokenSource cts = new(timeoutMs);
            using HttpClient http = new();
            bool alive = false;

            while (!cts.IsCancellationRequested)
            {
                try
                {
                    HttpResponseMessage response = await http.GetAsync(url, cts.Token).ConfigureAwait(true);
                    if ((int)response.StatusCode >= 200 && (int)response.StatusCode < 400)
                    {
                        alive = true;
                        break;
                    }
                }
                catch (HttpRequestException)
                {
                    // Server may not be ready yet.
                }
                catch (TaskCanceledException)
                {
                    // Retry until overall timeout.
                }

                await Task.Delay(intervalMs, cts.Token).ConfigureAwait(true);
            }

            if (!alive)
            {
                Debug.WriteLine($"[DreamineVMS] Server dashboard timeout: {url}");
                await WebView2Initializer.ShowOfflineMessageAsync(webView, url).ConfigureAwait(true);
                return;
            }

            webView.Source = new Uri(url);
        }
        catch (TaskCanceledException)
        {
            Debug.WriteLine("[DreamineVMS] Server navigation canceled.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DreamineVMS] Server navigation failed: {ex}");
        }
    }
}
