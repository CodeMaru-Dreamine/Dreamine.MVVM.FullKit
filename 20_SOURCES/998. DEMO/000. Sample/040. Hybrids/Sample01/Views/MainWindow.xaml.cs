/// \file MainWindow.xaml.cs
/// \brief WPF + BlazorWebView + WebView2 하이브리드 통합 윈도우(샘플).
/// \author Dreamine
/// \date 2026-01-28
/// \version 1.2.0
using Dreamine.Hybrid.Wpf.Controls;
using Dreamine.Hybrid.Wpf.Internal;
using Dreamine.Hybrid.BlazorApp.Components;
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
    /// <summary>메인 윈도우입니다.</summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            if (DesignerProperties.GetIsInDesignMode(this)) return;
            Loaded += OnLoaded;
        }

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

            var webView = WebView2Initializer.CreateConfiguredWebView2();
            ServerTab.Content = webView;

            await NavigateServerAsync(webView, "http://localhost:5000");
        }

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
                    await WebView2Initializer.ShowOfflineMessageAsync(webView, url);
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
