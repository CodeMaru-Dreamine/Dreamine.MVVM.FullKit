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
            RegionBinder.Clear();
            var control = new ContentControl();

            RegionBinder.SetRegionName(control, "StoresRegionName_Main");

            Assert.Equal("StoresRegionName_Main", RegionBinder.GetRegionName(control));
            RegionBinder.Clear();
        });
    }

    [Fact]
    public void RegionBinder_NavigateUsesCurrentRegionNameOnly()
    {
        RunSta(() =>
        {
            RegionBinder.Clear();
            var control = new ContentControl();
            var viewModel = new SampleViewModel();

            RegionBinder.SetRegionName(control, "NavigateCurrent_Main");
            RegionBinder.SetRegionName(control, "NavigateCurrent_Secondary");

            var exception = Assert.Throws<InvalidOperationException>(
                () => RegionBinder.Navigate("NavigateCurrent_Main", viewModel));
            Assert.Equal("Region \"NavigateCurrent_Main\" is not registered.", exception.Message);

            RegionBinder.Navigate("NavigateCurrent_Secondary", viewModel);

            Assert.IsType<SampleView>(control.Content);
            RegionBinder.Clear();
        });
    }

    [Fact]
    public void RegionBinder_NavigateThrowsPlainMessageWhenRegionIsMissing()
    {
        RunSta(() =>
        {
            RegionBinder.Clear();

            var exception = Assert.Throws<InvalidOperationException>(
                () => RegionBinder.Navigate("Missing", new SampleViewModel()));

            Assert.Equal("Region \"Missing\" is not registered.", exception.Message);
            Assert.DoesNotContain("\u274c", exception.Message);
        });
    }

    [Fact]
    public void RegionBinderHelper_FindRegionControlReturnsMatchingRegion()
    {
        RunSta(() =>
        {
            RegionBinder.Clear();
            var root = new Grid();
            var region = new ContentControl();

            RegionBinder.SetRegionName(region, "FindRegionControl_Main");
            root.Children.Add(region);

            ContentControl? found = RegionBinderHelper.FindRegionControl(root, "FindRegionControl_Main");

            Assert.Same(region, found);
            RegionBinder.Clear();
        });
    }

    [Fact]
    public void RegionBinderHelper_FindRegionControlThrowsForNullRoot()
    {
        Assert.Throws<ArgumentNullException>(
            () => RegionBinderHelper.FindRegionControl(null!, "FindRegionControl_Main"));
    }

    [Fact]
    public void RegionBinderHelper_FindRegionControlThrowsForEmptyRegionName()
    {
        RunSta(() =>
        {
            var root = new Grid();

            Assert.Throws<ArgumentException>(
                () => RegionBinderHelper.FindRegionControl(root, ""));
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
