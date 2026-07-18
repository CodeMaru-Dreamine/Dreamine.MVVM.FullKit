/// \file MainWindow.xaml.cs
/// \brief WPF + BlazorWebView + WebView2 하이브리드 통합 윈도우(샘플).
/// \author Dreamine
/// \date 2026-01-28
/// \version 1.2.0
using Dreamine.Hybrid.Wpf.Controls;
using Dreamine.Hybrid.Wpf.Hosting;
using Sample01.Blazor.Components;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Sample01.Views
{
    /// <summary>
    /// \if KO
    /// <para>메인 윈도우입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates main window functionality and related state.</para>
    /// \endif
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// \if KO
        /// <para>지정한 설정으로 <see cref="MainWindow"/> 클래스의 새 인스턴스를 초기화합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Initializes a new instance of the <see cref="MainWindow"/> class with the specified settings.</para>
        /// \endif
        /// </summary>
        public MainWindow()
        {
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
            var hybrid = new HybridHostControl
            {
                HostPage = "wwwroot/index.html",
                Services = App.ServiceProvider,
                RootComponentType = typeof(LocalCounter),
                RootSelector = "#app"
            };
            EmbeddedTab.Content = hybrid;

            var webView = HybridWebViewHost.CreateWebView();
            ServerTab.Content = webView;

            await NavigateServerAsync(webView, "http://localhost:5000");
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
                await Task.Delay(1500);

                using var cts = new CancellationTokenSource(timeoutMs);
                using var http = new HttpClient();
                var alive = false;

                while (!cts.IsCancellationRequested)
                {
                    try
                    {
                        var res = await http.GetAsync(url, cts.Token);
                        if ((int)res.StatusCode >= 200 && (int)res.StatusCode < 400)
                        {
                            alive = true;
                            break;
                        }
                    }
                    catch (HttpRequestException)
                    {
                        // Server may not be ready yet during polling; ignore and retry until timeout.
                    }
                    catch (TaskCanceledException)
                    {
                        // Ignore per-request timeout/cancellation during polling and continue retrying until overall timeout.
                    }

                    await Task.Delay(intervalMs, cts.Token);
                }

                if (!alive)
                {
                    Debug.WriteLine($"[ServerNav] Timeout: {url}");
                    await HybridWebViewHost.ShowOfflineMessageAsync(webView, url);
                    return;
                }

                webView.Source = new Uri(url);
            }
            catch (TaskCanceledException)
            {
                Debug.WriteLine("[ServerNav] Canceled.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ServerNav] {ex}");
            }
        }
    }
}
