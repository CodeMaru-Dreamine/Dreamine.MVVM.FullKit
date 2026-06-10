using Dreamine.MVVM.Locators;

namespace Dreamine.FullKit.Tests.Locators;

public sealed class ViewNamingConventionTests
{
    [Theory]
    [InlineData(typeof(Sample.Views.MainWindow), true)]
    [InlineData(typeof(Sample.Views.SettingsPage), true)]
    [InlineData(typeof(Sample.ViewModels.MainWindowViewModel), false)]
    public void IsLikelyViewType_UsesKnownViewSuffixes(Type type, bool expected)
    {
        Assert.Equal(expected, ViewNamingConvention.IsLikelyViewType(type));
    }

    [Fact]
    public void GetViewModelTypeNameCandidates_IncludesViewModelsNamespace()
    {
        var candidates = ViewNamingConvention.GetViewModelTypeNameCandidates(typeof(Sample.Views.MainWindow));

        Assert.Contains("Dreamine.FullKit.Tests.Locators.MainWindowViewModel", candidates);
        Assert.Contains("MainWindowViewModel", candidates);
    }

    [Fact]
    public void GetViewTypeNameCandidates_MapsViewModelBackToView()
    {
        var candidates = ViewNamingConvention.GetViewTypeNameCandidates(typeof(Sample.ViewModels.MainWindowViewModel));

        Assert.Contains("Dreamine.FullKit.Tests.Views.MainWindow", candidates);
        Assert.Contains("MainWindow", candidates);
    }

    private static class Sample
    {
        public static class Views
        {
            public sealed class MainWindow
            {
            }

            public sealed class SettingsPage
            {
            }
        }

        public static class ViewModels
        {
            public sealed class MainWindowViewModel
            {
            }
        }
    }
}
