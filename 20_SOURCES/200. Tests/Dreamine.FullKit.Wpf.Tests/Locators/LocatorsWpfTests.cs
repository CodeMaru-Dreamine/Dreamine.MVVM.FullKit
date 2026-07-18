using System.Windows.Controls;
using Dreamine.MVVM.Locators.Wpf;

namespace Dreamine.FullKit.Wpf.Tests.Locators;

/// <summary>
/// \if KO
/// <para>Locators Wpf Tests 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates locators wpf tests functionality and related state.</para>
/// \endif
/// </summary>
public sealed class LocatorsWpfTests
{
    /// <summary>
    /// \if KO
    /// <para>Region Binder Stores Region Name Attached Property 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the region binder stores region name attached property operation.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>Region Binder Navigate Uses Current Region Name Only 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the region binder navigate uses current region name only operation.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>Region Binder Navigate Throws Plain Message When Region Is Missing 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the region binder navigate throws plain message when region is missing operation.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>Region Binder Helper Find Region Control Returns Matching Region 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the region binder helper find region control returns matching region operation.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>Region Binder Helper Find Region Control Throws For Null Root 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the region binder helper find region control throws for null root operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void RegionBinderHelper_FindRegionControlThrowsForNullRoot()
    {
        Assert.Throws<ArgumentNullException>(
            () => RegionBinderHelper.FindRegionControl(null!, "FindRegionControl_Main"));
    }

    /// <summary>
    /// \if KO
    /// <para>Region Binder Helper Find Region Control Throws For Empty Region Name 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the region binder helper find region control throws for empty region name operation.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>View Model Binder Resolve View Creates Matching View And Assigns Data Context 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the view model binder resolve view creates matching view and assigns data context operation.</para>
    /// \endif
    /// </summary>
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
/// <para>Sample View Model 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates sample view model functionality and related state.</para>
/// \endif
/// </summary>
public sealed class SampleViewModel
{
}

/// <summary>
/// \if KO
/// <para>Sample View 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates sample view functionality and related state.</para>
/// \endif
/// </summary>
public sealed class SampleView : UserControl
{
}
