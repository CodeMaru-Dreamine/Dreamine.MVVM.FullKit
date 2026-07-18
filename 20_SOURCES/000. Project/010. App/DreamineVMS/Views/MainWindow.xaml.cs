using Dreamine.Hybrid.Wpf.Controls;
using Dreamine.Hybrid.Wpf.Hosting;
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
/// \if KO
/// <para>\brief DreamineVMS 메인 윈도우입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates main window functionality and related state.</para>
/// \endif
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// \if KO
    /// <para>server Options 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the server options value.</para>
    /// \endif
    /// </summary>
    private readonly VmsServerOptions _serverOptions;
    /// <summary>
    /// \if KO
    /// <para>attached View Model 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the attached view model value.</para>
    /// \endif
    /// </summary>
    private MainWindowViewModel? _attachedViewModel;
    /// <summary>
    /// \if KO
    /// <para>wpf Live Web View 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the wpf live web view value.</para>
    /// \endif
    /// </summary>
    private WebView2? _wpfLiveWebView;
    /// <summary>
    /// \if KO
    /// <para>cameras Web View 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the cameras web view value.</para>
    /// \endif
    /// </summary>
    private WebView2? _camerasWebView;
    /// <summary>
    /// \if KO
    /// <para>wpf Live Initialized 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the wpf live initialized value.</para>
    /// \endif
    /// </summary>
    private bool _wpfLiveInitialized;
    /// <summary>
    /// \if KO
    /// <para>cameras Initialized 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the cameras initialized value.</para>
    /// \endif
    /// </summary>
    private bool _camerasInitialized;

    /// <summary>
    /// \if KO
    /// <para>\brief MainWindow 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="MainWindow"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="serverOptions">
    /// \if KO
    /// <para>VMS 서버 옵션입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IOptions&lt;VmsServerOptions&gt;</c> value used for server options.</para>
    /// \endif
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// \if KO
    /// <para>필수 입력 인자 중 하나가 <see langword="null"/>인 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when a required input argument is <see langword="null"/>.</para>
    /// \endif
    /// </exception>
    public MainWindow(IOptions<VmsServerOptions> serverOptions)
    {
        _serverOptions = serverOptions?.Value ?? throw new ArgumentNullException(nameof(serverOptions));
        InitializeComponent();

        if (DesignerProperties.GetIsInDesignMode(this))
        {
            return;
        }

        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
        Closed += OnClosed;
        MainTabControl.SelectionChanged += OnMainTabSelectionChanged;
    }

    /// <summary>
    /// \if KO
    /// <para>Data Context Changed 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the data context changed event or state change.</para>
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

    /// <summary>
    /// \if KO
    /// <para>Open Live Tab Requested 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the open live tab requested event or state change.</para>
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
        if (_attachedViewModel is not null)
        {
            _attachedViewModel.OpenLiveTabRequested -= OnOpenLiveTabRequested;
            _attachedViewModel = null;
        }

        // WebView2 인스턴스를 명시적으로 정리합니다.
        // Dispose하지 않으면 Chromium 서브프로세스가 살아남아 앱 종료를 막을 수 있습니다.
        TryDisposeWebView(ref _wpfLiveWebView);
        TryDisposeWebView(ref _camerasWebView);

        // WPF Shutdown을 명시적으로 트리거합니다.
        // host.RunDreamineWpfApp의 구현에 따라 자동 종료가 안 될 수 있으므로 안전망입니다.
        Application.Current?.Shutdown();
    }

    /// <summary>
    /// \if KO
    /// <para>Main Tab Selection Changed 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the main tab selection changed event or state change.</para>
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
    private async void OnMainTabSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (!IsLoaded) return;

        // 에이전트 설정 탭이 처음 열릴 때 PasswordBox에 저장된 비밀번호를 채워줍니다.
        // PasswordBox.Password는 DependencyProperty가 아니라 직접 바인딩이 불가능합니다.
        if (AgentPasswordBox is not null
            && DataContext is MainWindowViewModel vm
            && string.IsNullOrEmpty(AgentPasswordBox.Password)
            && !string.IsNullOrEmpty(vm.AgentSettings.Password))
        {
            AgentPasswordBox.Password = vm.AgentSettings.Password;
        }

        await EnsureSelectedLiveWebViewAsync().ConfigureAwait(true);
    }

    /// <summary>
    /// \if KO
    /// <para>Agent Password Box Password Changed 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the agent password box password changed operation.</para>
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
    private void AgentPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.AgentSettings.Password = AgentPasswordBox.Password;
    }

    /// <summary>
    /// \if KO
    /// <para>Ensure Selected Live Web View Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the ensure selected live web view async operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Ensure Selected Live Web View Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the ensure selected live web view async operation.</para>
    /// \endif
    /// </returns>
    private async Task EnsureSelectedLiveWebViewAsync()
    {
        if (MainTabControl.SelectedItem == WpfLiveTab)
        {
            await EnsureWpfLiveWebViewAsync().ConfigureAwait(true);
            return;
        }

        if (MainTabControl.SelectedItem == CamerasTab)
        {
            await EnsureCamerasWebViewAsync().ConfigureAwait(true);
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Ensure Wpf Live Web View Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the ensure wpf live web view async operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Ensure Wpf Live Web View Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the ensure wpf live web view async operation.</para>
    /// \endif
    /// </returns>
    private async Task EnsureWpfLiveWebViewAsync()
    {
        if (_wpfLiveInitialized)
        {
            return;
        }

        WebView2 webView = HybridWebViewHost.CreateWebView();
        WpfLiveTab.Content = webView;
        _wpfLiveWebView = webView;

        string liveUrl = GetLiveUrl();
        RegisterWebViewRecovery(webView, () => liveUrl);
        await NavigateServerAsync(webView, liveUrl).ConfigureAwait(true);
        _wpfLiveInitialized = true;
    }

    /// <summary>
    /// \if KO
    /// <para>Ensure Cameras Web View Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the ensure cameras web view async operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Ensure Cameras Web View Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the ensure cameras web view async operation.</para>
    /// \endif
    /// </returns>
    private async Task EnsureCamerasWebViewAsync()
    {
        if (_camerasInitialized)
        {
            return;
        }

        WebView2 webView = HybridWebViewHost.CreateWebView();
        CamerasTab.Content = webView;
        _camerasWebView = webView;

        string camerasUrl = $"http://localhost:{_serverOptions.Port}/cameras";
        RegisterWebViewRecovery(webView, () => camerasUrl);
        await NavigateServerAsync(webView, camerasUrl).ConfigureAwait(true);
        _camerasInitialized = true;
    }

    /// <summary>
    /// \if KO
    /// <para>Live Url 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the live url value.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Get Live Url 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the get live url operation.</para>
    /// \endif
    /// </returns>
    private string GetLiveUrl()
    {
        return $"http://localhost:{_serverOptions.Port}/live";
    }

    /// <summary>
    /// \if KO
    /// <para>Register Web View Recovery 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the register web view recovery operation.</para>
    /// \endif
    /// </summary>
    /// <param name="webView">
    /// \if KO
    /// <para>web View에 사용할 <c>WebView2</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>WebView2</c> value used for web view.</para>
    /// \endif
    /// </param>
    /// <param name="urlFactory">
    /// \if KO
    /// <para>url Factory에 사용할 <c>Func&lt;string&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Func&lt;string&gt;</c> value used for url factory.</para>
    /// \endif
    /// </param>
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

    /// <summary>
    /// \if KO
    /// <para>Navigate Web View 작업을 시도하고 성공 여부를 반환합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Attempts to navigate web view and returns whether the operation succeeds.</para>
    /// \endif
    /// </summary>
    /// <param name="webView">
    /// \if KO
    /// <para>web View에 사용할 <c>WebView2</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>WebView2</c> value used for web view.</para>
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

    /// <summary>
    /// \if KO
    /// <para>Dispose Web View 작업을 시도하고 성공 여부를 반환합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Attempts to dispose web view and returns whether the operation succeeds.</para>
    /// \endif
    /// </summary>
    /// <param name="webView">
    /// \if KO
    /// <para>web View에 사용할 <c>WebView2?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>WebView2?</c> value used for web view.</para>
    /// \endif
    /// </param>
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

    /// <summary>
    /// \if KO
    /// <para>Navigate Server Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the navigate server async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="webView">
    /// \if KO
    /// <para>web View에 사용할 <c>WebView2</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>WebView2</c> value used for web view.</para>
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
    /// <param name="timeoutMs">
    /// \if KO
    /// <para>timeout Ms에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for timeout ms.</para>
    /// \endif
    /// </param>
    /// <param name="intervalMs">
    /// \if KO
    /// <para>interval Ms에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for interval ms.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Navigate Server Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the navigate server async operation.</para>
    /// \endif
    /// </returns>
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
                await HybridWebViewHost.ShowOfflineMessageAsync(webView, url).ConfigureAwait(true);
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
