/// \file App.xaml.cs
/// \brief WPF Application. Program(BuildApp)에서 Host의 ServiceProvider를 주입받아 초기화.
/// \author Dreamine
/// \date 2026-01-28
/// \version 1.1.0
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using Sample01.ViewModels;
using Sample01.Views;
namespace Sample01
{
    /// <summary>WPF Application</summary>
    public partial class App : Application
    {
        /// <summary>호스트가 주입한 ServiceProvider</summary>
        public static IServiceProvider? ServiceProvider { get; internal set; }

        /// <inheritdoc/>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (ServiceProvider is null)
                throw new InvalidOperationException("ServiceProvider 초기화 실패: Program.BuildHost가 먼저 호출되어야 합니다.");

            var vm = ServiceProvider.GetRequiredService<MainWindowViewModel>();
            var win = ServiceProvider.GetRequiredService<MainWindow>();
            win.DataContext = vm;

            MainWindow = win;
            win.Show();
        }
    }
}
