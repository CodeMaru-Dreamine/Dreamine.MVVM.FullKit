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
    /// \brief Application entry point.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// \brief Starts the WPF hybrid sample application.
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