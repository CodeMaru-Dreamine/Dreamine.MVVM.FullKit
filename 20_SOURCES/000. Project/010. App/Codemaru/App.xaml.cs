using Codemaru.Services;
using Codemaru.ViewModels;
using Codemaru.Views;
using Dreamine.Hybrid.Wpf.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace Codemaru;

/// <summary>
/// \if KO
/// <para>App 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates app functionality and related state.</para>
/// \endif
/// </summary>
public partial class App : Application, IDreamineServiceProviderAware
{
    /// <summary>
    /// \if KO
    /// <para>Service Provider 값을 가져오거나 설정합니다.</para>
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
        {
            throw new InvalidOperationException("ServiceProvider was not initialized before WPF startup.");
        }

        // InitializeAsync 내부의 await 완료 후 WPF UI SynchronizationContext로 복귀를
        // 시도하면 UI 스레드가 이미 블로킹 중이므로 데드락이 발생합니다.
        // Task.Run으로 SynchronizationContext 없는 스레드풀에서 실행해 이를 방지합니다.
        var session = ServiceProvider.GetRequiredService<CardHybridSession>();
        Task.Run(() => session.InitializeAsync()).GetAwaiter().GetResult();

        var vm = ServiceProvider.GetRequiredService<MainWindowViewModel>();
        var window = ServiceProvider.GetRequiredService<MainWindow>();
        window.DataContext = vm;

        MainWindow = window;
        window.Show();
    }
}
