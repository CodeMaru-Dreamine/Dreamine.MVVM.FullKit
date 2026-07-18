using Dreamine.Hybrid.Wpf.Interfaces;
/// \file App.xaml.cs
/// \brief WPF Application. Program(BuildApp)에서 Host의 ServiceProvider를 주입받아 초기화.
/// \author Dreamine
/// \date 2026-01-28
/// \version 1.1.0
using Microsoft.Extensions.DependencyInjection;
using Sample01.ViewModels;
using Sample01.Views;
using System;
using System.Windows;
namespace Sample01
{
    /// <summary>
    /// \if KO
    /// <para>App 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>WPF Application</para>
    /// \endif
    /// </summary>
    public partial class App : Application, IDreamineServiceProviderAware
    {
        /// <summary>
        /// \if KO
        /// <para>호스트가 주입한 ServiceProvider</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets the service provider value.</para>
        /// \endif
        /// </summary>
        public static IServiceProvider? ServiceProvider { get; private set; }

        /// <summary>
        /// \if KO
        /// <para>Service Provider 값을 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Sets the service provider value.</para>
        /// \endif
        /// </summary>
        /// <param name="serviceProvider">
        /// \if KO
        /// <para>service Provider에 사용할 <c>IServiceProvider</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>IServiceProvider</c> value used for service provider.</para>
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
        public void SetServiceProvider(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// \if KO
        /// <para>Startup 이벤트 또는 상태 변경을 처리합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Handles the startup event or state change.</para>
        /// \endif
        /// </summary>
        /// <param name="e">
        /// \if KO
        /// <para>이벤트와 관련된 데이터를 포함합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Contains data associated with the event.</para>
        /// \endif
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// \if KO
        /// <para>현재 객체 상태에서 On Startup 작업을 수행할 수 없는 경우 발생합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Thrown when the on startup operation is not valid for the current object state.</para>
        /// \endif
        /// </exception>
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
