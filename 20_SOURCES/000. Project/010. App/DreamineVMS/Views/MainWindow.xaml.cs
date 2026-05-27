using Dreamine.Hybrid.Wpf.Controls;
using Dreamine.Hybrid.Wpf.Internal;
using DreamineVMS.Blazor.Components;
using DreamineVMS.Options;
using DreamineVMS.ViewModels;
using Microsoft.Extensions.Options;
using Microsoft.Web.WebView2.Core;
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
    private bool _serverLiveInitialized;
    private bool _wpfLiveInitialized;

    /// <summary>
    /// \brief MainWindow 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="serverOptions">VMS 서버 옵션입니다.</param>
    public MainWindow(IOptions<VmsServerOptions> serverOptions)
    {
        _serverOptions = serverOptions?.Value ?? throw new ArgumentNullException(nameof(serverOptions));
        InitializeComponent();

        // 탭 헤더의 포트 번호를 실제 옵션 값으로 표시합니다.
        ServerDashboardTabHeader.Text = $"Server Live ({_serverOptions.Port})";

        if (DesignerProperties.GetIsInDesignMode(this))
        {
            return;
        }

        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
        Closed += OnClosed;
        MainTabControl.SelectionChanged += OnMainTabSelectionChanged;
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
        // 레거시 메시지 호환용입니다. Blazor Server의 첫 화면은 Live View이며, 탭 전환은 수행하지 않습니다.
        Dispatcher.Invoke(() =>
        {
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

        // Live WebView는 동시에 두 개를 바로 띄우지 않습니다.
        // 두 Live 탭이 동시에 /live에 접속하면 Blazor Circuit과 hls.js player가 중복 생성되어
        // StopAll → StartAll 이후 source/session 동기화가 꼬일 수 있습니다.
        await EnsureSelectedLiveWebViewAsync().ConfigureAwait(true);
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

    private async void OnMainTabSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
        {
            return;
        }

        await EnsureSelectedLiveWebViewAsync().ConfigureAwait(true);
    }

    private async Task EnsureSelectedLiveWebViewAsync()
    {
        if (MainTabControl.SelectedItem == ServerDashboardTab)
        {
            await EnsureServerLiveWebViewAsync().ConfigureAwait(true);
            return;
        }

        if (MainTabControl.SelectedItem == WpfLiveTab)
        {
            await EnsureWpfLiveWebViewAsync().ConfigureAwait(true);
        }
    }

    private async Task EnsureServerLiveWebViewAsync()
    {
        if (_serverLiveInitialized)
        {
            return;
        }

        WebView2 webView = WebView2Initializer.CreateConfiguredWebView2();
        ServerDashboardTab.Content = webView;
        _serverDashboardWebView = webView;

        string liveUrl = GetLiveUrl();
        RegisterWebViewRecovery(webView, () => liveUrl);
        await NavigateServerAsync(webView, liveUrl).ConfigureAwait(true);
        _serverLiveInitialized = true;
    }

    private async Task EnsureWpfLiveWebViewAsync()
    {
        if (_wpfLiveInitialized)
        {
            return;
        }

        WebView2 webView = WebView2Initializer.CreateConfiguredWebView2();
        WpfLiveTab.Content = webView;
        _wpfLiveWebView = webView;

        string liveUrl = GetLiveUrl();
        RegisterWebViewRecovery(webView, () => liveUrl);
        await NavigateServerAsync(webView, liveUrl).ConfigureAwait(true);
        _wpfLiveInitialized = true;
    }

    private string GetLiveUrl()
    {
        return $"http://localhost:{_serverOptions.Port}/live";
    }

    private void RegisterWebViewRecovery(WebView2 webView, Func<string> urlFactory)
    {
        webView.CoreWebView2InitializationCompleted += (_, e) =>
        {
            if (!e.IsSuccess || webView.CoreWebView2 is null)
            {
                Debug.WriteLine($"[DreamineVMS] WebView2 initialization failed: {e.InitializationException}");
                return;
            }

            webView.CoreWebView2.ProcessFailed += async (_, args) =>
            {
                Debug.WriteLine($"[DreamineVMS] WebView2 process failed: {args.ProcessFailedKind}. Reloading...");

                await Dispatcher.InvokeAsync(async () =>
                {
                    await Task.Delay(1000).ConfigureAwait(true);
                    TryNavigateWebView(webView, urlFactory());
                });
            };
        };

        webView.NavigationCompleted += (_, e) =>
        {
            if (e.IsSuccess)
            {
                return;
            }

            Debug.WriteLine($"[DreamineVMS] WebView2 navigation failed: {e.WebErrorStatus}. Reloading...");
            Dispatcher.InvokeAsync(async () =>
            {
                await Task.Delay(1500).ConfigureAwait(true);
                TryNavigateWebView(webView, urlFactory());
            });
        };
    }

    private static void TryNavigateWebView(WebView2 webView, string url)
    {
        try
        {
            webView.Source = new Uri(url);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DreamineVMS] WebView2 recovery navigation failed: {ex}");
        }
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
