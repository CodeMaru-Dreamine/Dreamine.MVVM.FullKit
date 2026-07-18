using Dreamine.Hybrid.Wpf.Controls;
using Dreamine.Hybrid.Wpf.Hosting;
using Microsoft.Web.WebView2.Wpf;
using System.ComponentModel;
using System.Net.Http;
using System.Windows;

namespace PortfolioApp.Views;

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
        var webView = HybridWebViewHost.CreateWebView();
        WebViewHost.Child = webView;

        await NavigateAsync(webView, $"http://localhost:{_serverOptions.Port}",
            _serverOptions.InstanceId, "/admin");
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
        string path = "/", int timeoutMs = 15000, int intervalMs = 500)
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
