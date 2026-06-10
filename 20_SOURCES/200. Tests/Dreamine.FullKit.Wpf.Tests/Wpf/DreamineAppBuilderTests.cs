using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Dreamine.MVVM.Core;
using Dreamine.MVVM.Interfaces.Navigation;
using Dreamine.MVVM.Locators.Wpf;
using Dreamine.MVVM.Wpf;

namespace Dreamine.FullKit.Wpf.Tests.Wpf;

public sealed class DreamineAppBuilderTests
{
    [Fact]
    public void RegisterNavigatorFromWindowReplacesExistingNavigatorWithCurrentWindowRegion()
    {
        RunSta(() =>
        {
            DMContainer.Reset();
            RegionBinder.Clear();

            var firstRegion = new ContentControl();
            RegionBinder.SetRegionName(firstRegion, "AppBuilder_Main");
            var firstWindow = new Window { Content = firstRegion };

            var secondRegion = new ContentControl();
            RegionBinder.SetRegionName(secondRegion, "AppBuilder_Main");
            var secondWindow = new Window { Content = secondRegion };

            try
            {
                firstWindow.Show();
                secondWindow.Show();

                DreamineAppBuilder.RegisterNavigatorFromWindow(firstWindow, "AppBuilder_Main");
                DreamineAppBuilder.RegisterNavigatorFromWindow(secondWindow, "AppBuilder_Main");

                INavigator navigator = DMContainer.Resolve<INavigator>();
                navigator.Navigate(new AppBuilderSampleViewModel());

                Assert.Null(firstRegion.Content);
                Assert.IsType<AppBuilderSampleView>(secondRegion.Content);
            }
            finally
            {
                firstWindow.Close();
                secondWindow.Close();
                RegionBinder.Clear();
                DMContainer.Reset();
            }
        });
    }

    private static void RunSta(Action action)
    {
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (exception is not null)
        {
            throw exception;
        }
    }

}

public sealed class AppBuilderSampleViewModel
{
}

public sealed class AppBuilderSampleView : UserControl
{
}
