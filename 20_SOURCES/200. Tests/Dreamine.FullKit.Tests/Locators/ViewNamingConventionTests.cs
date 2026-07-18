using Dreamine.MVVM.Locators;

namespace Dreamine.FullKit.Tests.Locators;

/// <summary>
/// \if KO
/// <para>View Naming Convention Tests 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates view naming convention tests functionality and related state.</para>
/// \endif
/// </summary>
public sealed class ViewNamingConventionTests
{
    /// <summary>
    /// \if KO
    /// <para>Is Likely View Type Uses Known View Suffixes 조건을 확인합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Determines whether is likely view type uses known view suffixes.</para>
    /// \endif
    /// </summary>
    /// <param name="type">
    /// \if KO
    /// <para>type에 사용할 <c>Type</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Type</c> value used for type.</para>
    /// \endif
    /// </param>
    /// <param name="expected">
    /// \if KO
    /// <para>expected에 사용할 <c>bool</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>bool</c> value used for expected.</para>
    /// \endif
    /// </param>
    [Theory]
    [InlineData(typeof(Sample.Views.MainWindow), true)]
    [InlineData(typeof(Sample.Views.SettingsPage), true)]
    [InlineData(typeof(Sample.ViewModels.MainWindowViewModel), false)]
    public void IsLikelyViewType_UsesKnownViewSuffixes(Type type, bool expected)
    {
        Assert.Equal(expected, ViewNamingConvention.IsLikelyViewType(type));
    }

    /// <summary>
    /// \if KO
    /// <para>View Model Type Name Candidates Includes View Models Namespace 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the view model type name candidates includes view models namespace value.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void GetViewModelTypeNameCandidates_IncludesViewModelsNamespace()
    {
        var candidates = ViewNamingConvention.GetViewModelTypeNameCandidates(typeof(Sample.Views.MainWindow));

        Assert.Contains("Dreamine.FullKit.Tests.Locators.MainWindowViewModel", candidates);
        Assert.Contains("MainWindowViewModel", candidates);
    }

    /// <summary>
    /// \if KO
    /// <para>View Type Name Candidates Maps View Model Back To View 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the view type name candidates maps view model back to view value.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void GetViewTypeNameCandidates_MapsViewModelBackToView()
    {
        var candidates = ViewNamingConvention.GetViewTypeNameCandidates(typeof(Sample.ViewModels.MainWindowViewModel));

        Assert.Contains("Dreamine.FullKit.Tests.Views.MainWindow", candidates);
        Assert.Contains("MainWindow", candidates);
    }

    /// <summary>
    /// \if KO
    /// <para>Sample 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates sample functionality and related state.</para>
    /// \endif
    /// </summary>
    private static class Sample
    {
        /// <summary>
        /// \if KO
        /// <para>Views 기능과 관련 상태를 캡슐화합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Encapsulates views functionality and related state.</para>
        /// \endif
        /// </summary>
        public static class Views
        {
            /// <summary>
            /// \if KO
            /// <para>Main Window 기능과 관련 상태를 캡슐화합니다.</para>
            /// \endif
            /// \if EN
            /// <para>Encapsulates main window functionality and related state.</para>
            /// \endif
            /// </summary>
            public sealed class MainWindow
            {
            }

            /// <summary>
            /// \if KO
            /// <para>Settings Page 기능과 관련 상태를 캡슐화합니다.</para>
            /// \endif
            /// \if EN
            /// <para>Encapsulates settings page functionality and related state.</para>
            /// \endif
            /// </summary>
            public sealed class SettingsPage
            {
            }
        }

        /// <summary>
        /// \if KO
        /// <para>View Models 기능과 관련 상태를 캡슐화합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Encapsulates view models functionality and related state.</para>
        /// \endif
        /// </summary>
        public static class ViewModels
        {
            /// <summary>
            /// \if KO
            /// <para>Main Window View Model 기능과 관련 상태를 캡슐화합니다.</para>
            /// \endif
            /// \if EN
            /// <para>Encapsulates main window view model functionality and related state.</para>
            /// \endif
            /// </summary>
            public sealed class MainWindowViewModel
            {
            }
        }
    }
}
