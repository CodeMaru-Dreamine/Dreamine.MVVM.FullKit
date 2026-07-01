using Dreamine.Hybrid.Wpf.Hosting;
using Dreamine.Hybrid.Wpf.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace DreamineWeb;

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
            throw new InvalidOperationException("ServiceProvider was not initialized before WPF startup.");

        var window = ServiceProvider.GetRequiredService<Views.MainWindow>();
        MainWindow = window;
        window.Show();
    }
}
