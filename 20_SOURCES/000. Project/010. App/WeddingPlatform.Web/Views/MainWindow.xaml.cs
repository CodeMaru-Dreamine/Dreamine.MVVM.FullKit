using Dreamine.Hybrid.Wpf.Controls;
using Dreamine.Hybrid.Wpf.Hosting;
using Microsoft.Web.WebView2.Wpf;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;

namespace WeddingPlatform.Views;

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
    /// <summary>
    /// \if KO
    /// <para>server Options 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the server options value.</para>
    /// \endif
    /// </summary>
    private readonly DreamineBlazorServerHostOptions _serverOptions;

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="MainWindow"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="MainWindow"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="serverOptions">
    /// \if KO
    /// <para>server Options에 사용할 <c>DreamineBlazorServerHostOptions</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>DreamineBlazorServerHostOptions</c> value used for server options.</para>
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
    public MainWindow(DreamineBlazorServerHostOptions serverOptions)
    {
        _serverOptions = serverOptions ?? throw new ArgumentNullException(nameof(serverOptions));
        InitializeComponent();

        if (DesignerProperties.GetIsInDesignMode(this)) return;

        Loaded += OnLoaded;
        Closed += (_, _) => Application.Current?.Shutdown();
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

    /// <summary>
    /// \if KO
    /// <para>Open External Admin Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the open external admin async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="baseUrl">
    /// \if KO
    /// <para>base Url에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for base url.</para>
    /// \endif
    /// </param>
    /// <param name="expectedInstanceId">
    /// \if KO
    /// <para>expected Instance Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for expected instance id.</para>
    /// \endif
    /// </param>
    /// <param name="path">
    /// \if KO
    /// <para>path에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for path.</para>
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
    /// <para>Open External Admin Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the open external admin async operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Navigate Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the navigate async operation.</para>
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
    /// <param name="baseUrl">
    /// \if KO
    /// <para>base Url에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for base url.</para>
    /// \endif
    /// </param>
    /// <param name="expectedInstanceId">
    /// \if KO
    /// <para>expected Instance Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for expected instance id.</para>
    /// \endif
    /// </param>
    /// <param name="path">
    /// \if KO
    /// <para>path에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for path.</para>
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
    /// <para>Navigate Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the navigate async operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Wait For Instance Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the wait for instance async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="baseUrl">
    /// \if KO
    /// <para>base Url에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for base url.</para>
    /// \endif
    /// </param>
    /// <param name="expectedInstanceId">
    /// \if KO
    /// <para>expected Instance Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for expected instance id.</para>
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
    /// <para>Wait For Instance Async 작업에서 생성한 <c>Task&lt;bool&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;bool&gt;</c> result produced by the wait for instance async operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Show External Browser Message 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the show external browser message operation.</para>
    /// \endif
    /// </summary>
    /// <param name="targetUrl">
    /// \if KO
    /// <para>target Url에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for target url.</para>
    /// \endif
    /// </param>
    /// <param name="message">
    /// \if KO
    /// <para>처리할 메시지입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The message to process.</para>
    /// \endif
    /// </param>
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

    /// <summary>
    /// \if KO
    /// <para>Open Url 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the open url operation.</para>
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
    private static void OpenUrl(string url)
    {
        Process.Start(new ProcessStartInfo(url)
        {
            UseShellExecute = true
        });
    }

    /// <summary>
    /// \if KO
    /// <para>Show Startup Error 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the show startup error operation.</para>
    /// \endif
    /// </summary>
    /// <param name="ex">
    /// \if KO
    /// <para>ex에 사용할 <c>Exception</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Exception</c> value used for ex.</para>
    /// \endif
    /// </param>
    private static void ShowStartupError(Exception ex)
    {
        Debug.WriteLine($"[WeddingPlatform.WebView2.StartupFailed] {ex}");
        MessageBox.Show(
            $"관리자 화면(WebView2)을 초기화하지 못했습니다.\n\n{ex.Message}",
            "Wedding Platform",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}
