using Codemaru.Services;
using Codemaru.ViewModels;
using Codemaru.Views;
using Dreamine.Hybrid.Wpf.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace Codemaru;

public partial class App : Application, IDreamineServiceProviderAware
{
    public static IServiceProvider? ServiceProvider { get; private set; }

    public void SetServiceProvider(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

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
