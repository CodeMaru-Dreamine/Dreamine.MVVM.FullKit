/// \file Program.cs
/// \brief WPF + GenericHost 엔트리 포인트.
/// \details
///  - Host를 먼저 Build/Start 한 뒤, WPF App.Run을 수행한다.
///  - IHostedService(예: BlazorServerHostedService)를 사용하려면 StartAsync가 필수.
/// \author Dreamine
/// \date 2026-01-28
/// \version 1.2.0
using Dreamine.Hybrid.Wpf.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;
using System.Windows;
using Sample01.Hosting;
using Sample01.Views;
using Sample01.ViewModels;

namespace Sample01
{
    /// <summary>프로그램 진입점</summary>
    public static class Program
    {
        /// <summary>메인</summary>
        [STAThread]
        public static void Main()
        {
            var host = BuildHost();

            // WPF App에 ServiceProvider 주입
            App.ServiceProvider = host.Services;

            // Host 기동(HostedService 포함)
            host.StartAsync().GetAwaiter().GetResult();

            var app = new App();
            app.Exit += async (_, __) =>
            {
                await host.StopAsync();
                host.Dispose();
            };

            app.Run();
        }

        /// <summary>GenericHost를 구성합니다.</summary>
        private static IHost BuildHost()
        {
            var builder = Host.CreateApplicationBuilder();

            // 하이브리드 기본 등록(BlazorWebView + MessageBus + Blazor VM)
            builder.Services.AddDreamineHybridWpf();

            // WPF 구성요소
            builder.Services.AddSingleton<MainWindow>();
            builder.Services.AddSingleton<MainWindowViewModel>();

            // (옵션) Blazor Server 호스팅
            builder.Services.AddHostedService<BlazorServerHostedService>();

            return builder.Build();
        }
    }
}
