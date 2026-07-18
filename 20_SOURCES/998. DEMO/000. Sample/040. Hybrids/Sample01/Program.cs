using Dreamine.Hybrid.Interfaces;
using Dreamine.Hybrid.State;
using Dreamine.Hybrid.Wpf.DependencyInjection;
using Dreamine.Hybrid.Wpf.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sample01.Blazor;
using Sample01.States;
using Sample01.ViewModels;
using Sample01.Views;

namespace Sample01
{
    /// <summary>
    /// \if KO
    /// <para>Program 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>\brief Application entry point.</para>
    /// \endif
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// \if KO
        /// <para>Main 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>\brief Starts the WPF hybrid sample application.</para>
        /// \endif
        /// </summary>
        [STAThread]
        public static void Main()
        {
            HostApplicationBuilder builder = Host.CreateApplicationBuilder();

            builder.Services.AddDreamineHybridWpf();

            builder.Services.AddSingleton<MainWindow>();
            builder.Services.AddSingleton<MainWindowViewModel>();

            builder.Services.AddSingleton<IHybridStateStore<CounterState>>(
                new HybridStateStore<CounterState>(
                    new CounterState(
                        Count: 0,
                        LastSource: "-",
                        LastUpdated: null)));

            builder.Services.AddDreamineBlazorServer<AppShell>(options =>
            {
                options.Port = 5000;

                options.SharedServiceTypes.Add(
                    typeof(IHybridStateStore<CounterState>));
            });

            builder.Build().RunDreamineWpfApp<App>();
        }
    }
}