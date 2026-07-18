using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Dreamine.MVVM.Core;
using Dreamine.MVVM.Interfaces.Navigation;
using Dreamine.MVVM.Locators.Wpf;
using Dreamine.MVVM.Wpf;

namespace Dreamine.FullKit.Wpf.Tests.Wpf;

/// <summary>
/// \if KO
/// <para>Dreamine App Builder Tests 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates dreamine app builder tests functionality and related state.</para>
/// \endif
/// </summary>
public sealed class DreamineAppBuilderTests
{
    /// <summary>
    /// \if KO
    /// <para>Register Navigator From Window Replaces Existing Navigator With Current Window Region 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the register navigator from window replaces existing navigator with current window region operation.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>Run Sta 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the run sta operation.</para>
    /// \endif
    /// </summary>
    /// <param name="action">
    /// \if KO
    /// <para>action에 사용할 <c>Action</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Action</c> value used for action.</para>
    /// \endif
    /// </param>
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

/// <summary>
/// \if KO
/// <para>App Builder Sample View Model 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates app builder sample view model functionality and related state.</para>
/// \endif
/// </summary>
public sealed class AppBuilderSampleViewModel
{
}

/// <summary>
/// \if KO
/// <para>App Builder Sample View 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates app builder sample view functionality and related state.</para>
/// \endif
/// </summary>
public sealed class AppBuilderSampleView : UserControl
{
}
