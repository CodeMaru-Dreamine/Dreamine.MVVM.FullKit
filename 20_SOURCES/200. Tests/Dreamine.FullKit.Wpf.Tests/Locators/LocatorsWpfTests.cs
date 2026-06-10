using System.Windows.Controls;
using Dreamine.MVVM.Locators.Wpf;

namespace Dreamine.FullKit.Wpf.Tests.Locators;

public sealed class LocatorsWpfTests
{
    [Fact]
    public void RegionBinder_StoresRegionNameAttachedProperty()
    {
        RunSta(() =>
        {
            var control = new ContentControl();

            RegionBinder.SetRegionName(control, "Main");

            Assert.Equal("Main", RegionBinder.GetRegionName(control));
        });
    }

    [Fact]
    public void ViewModelBinder_ResolveViewCreatesMatchingViewAndAssignsDataContext()
    {
        RunSta(() =>
        {
            var viewModel = new SampleViewModel();

            var view = ViewModelBinder.ResolveView(viewModel);

            Assert.IsType<SampleView>(view);
            Assert.Same(viewModel, view.DataContext);
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

public sealed class SampleViewModel
{
}

public sealed class SampleView : UserControl
{
}
