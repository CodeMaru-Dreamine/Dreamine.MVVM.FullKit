using Dreamine.MVVM.Core;
using Dreamine.MVVM.Wpf;
using System.Reflection;
using SampleEnterprise.ViewModels;
using SampleEnterprise.Views;
using Dreamine.MVVM.Interfaces.Navigation;
using Dreamine.MVVM.Locators.Wpf;

namespace SampleEnterprise
{
    /// <summary>
    /// \if KO
    /// <para>App 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Provides optional Dreamine application customization hooks.</para>
    /// \endif
    /// </summary>
    /// <remarks>
    /// \if KO
    /// <para>이 멤버의 동작과 사용 시 고려 사항을 설명합니다.</para>
    /// \endif
    /// \if EN
    /// <para>This file is optional. You may delete it when no custom startup behavior is required.</para>
    /// \endif
    /// </remarks>
    public partial class App
    {
        /// <summary>
        /// \if KO
        /// <para>Register Before 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Runs before Dreamine initializes its default services and automatic registration.</para>
        /// \endif
        /// </summary>
        /// <remarks>
        /// \if KO
        /// <para>이 멤버의 동작과 사용 시 고려 사항을 설명합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Use this hook when you need to register custom services before Dreamine performs automatic ViewModel, Model, Event, and Manager registration.</para>
        /// \endif
        /// </remarks>
        static partial void RegisterBefore()
        {
            Assembly.Load("SampleEnterprise.ViewModels");
            Assembly.Load("SampleEnterprise.Views");
            Assembly.Load("SampleEnterprise.Events");
            Assembly.Load("SampleEnterprise.Models");
        }

        /// <summary>
        /// \if KO
        /// <para>Configure Dreamine 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Allows customization of Dreamine WPF runtime options before initialization.</para>
        /// \endif
        /// </summary>
        /// <param name="options">
        /// \if KO
        /// <para>동작을 구성하는 설정입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The Dreamine WPF runtime options.</para>
        /// \endif
        /// </param>
        /// <remarks>
        /// \if KO
        /// <para>이 멤버의 동작과 사용 시 고려 사항을 설명합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Use this hook to change the default region name, disable global ViewModel auto-wiring, or disable automatic navigator registration.</para>
        /// \endif
        /// </remarks>
        static partial void ConfigureDreamine(DreamineWpfOptions options)
        {
        }

        /// <summary>
        /// \if KO
        /// <para>Register After 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Runs after Dreamine initialization has completed.</para>
        /// \endif
        /// </summary>
        /// <remarks>
        /// \if KO
        /// <para>이 멤버의 동작과 사용 시 고려 사항을 설명합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Use this hook for startup navigation, event subscriptions, or post-initialization logic. Avoid registering core Dreamine services here unless you intentionally want to override behavior after initialization.</para>
        /// \endif
        /// </remarks>
        static partial void RegisterAfter()
        {
        }

        /// <summary>
        /// \if KO
        /// <para>Show Main Window 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Allows manual MainWindow creation when StartupUri is not used.</para>
        /// \endif
        /// </summary>
        /// <remarks>
        /// \if KO
        /// <para>이 멤버의 동작과 사용 시 고려 사항을 설명합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Use this hook only when you want to create and show the main window manually. If App.xaml already defines StartupUri, leave this hook unused.</para>
        /// \endif
        /// </remarks>
        static partial void ShowMainWindow()
        {
            var vm = DMContainer.Resolve<MainWindowViewModel>();
            var view = new MainWindow
            {
                DataContext = vm
            };

            view.Loaded += (s, e) =>
            {
                var region = RegionBinderHelper.FindRegionControl(view, "SubPage");
                if (region != null)
                {
                    DMContainer.RegisterSingleton<INavigator>(new ContentControlNavigator(region));
                }
            };

            view.Show();
        }
    }
}
