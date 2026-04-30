using Dreamine.MVVM.Wpf;

namespace SampleCore
{
    /// <summary>
    /// Provides optional Dreamine application customization hooks.
    /// </summary>
    /// <remarks>
    /// This file is optional. You may delete it when no custom startup behavior is required.
    /// </remarks>
    public partial class App
    {
        /// <summary>
        /// Runs before Dreamine initializes its default services and automatic registration.
        /// </summary>
        /// <remarks>
        /// Use this hook when you need to register custom services before Dreamine performs
        /// automatic ViewModel, Model, Event, and Manager registration.
        /// </remarks>
        /// <example>
        /// <code>
        /// static partial void RegisterBefore()
        /// {
        ///     DMContainer.RegisterSingleton&lt;IMyService&gt;(new MyService());
        /// }
        /// </code>
        /// </example>
        static partial void RegisterBefore()
        {
        }

        /// <summary>
        /// Allows customization of Dreamine WPF runtime options before initialization.
        /// </summary>
        /// <param name="options">The Dreamine WPF runtime options.</param>
        /// <remarks>
        /// Use this hook to change the default region name, disable global ViewModel auto-wiring,
        /// or disable automatic navigator registration.
        /// </remarks>
        /// <example>
        /// <code>
        /// static partial void ConfigureDreamine(DreamineWpfOptions options)
        /// {
        ///     options.DefaultRegionName = "MainRegion";
        ///     options.EnableGlobalAutoWireOnLoaded = false;
        /// }
        /// </code>
        /// </example>
        static partial void ConfigureDreamine(DreamineWpfOptions options)
        {
        }

        /// <summary>
        /// Runs after Dreamine initialization has completed.
        /// </summary>
        /// <remarks>
        /// Use this hook for startup navigation, event subscriptions, or post-initialization logic.
        /// Avoid registering core Dreamine services here unless you intentionally want to override
        /// behavior after initialization.
        /// </remarks>
        /// <example>
        /// <code>
        /// static partial void RegisterAfter()
        /// {
        ///     // Perform startup navigation or application-specific initialization here.
        /// }
        /// </code>
        /// </example>
        static partial void RegisterAfter()
        {
        }

        /// <summary>
        /// Allows manual MainWindow creation when StartupUri is not used.
        /// </summary>
        /// <remarks>
        /// Use this hook only when you want to create and show the main window manually.
        /// If App.xaml already defines StartupUri, leave this hook unused.
        /// </remarks>
        /// <example>
        /// <code>
        /// static partial void ShowMainWindow()
        /// {
        ///     var window = new MainWindow();
        ///     window.Show();
        /// }
        /// </code>
        /// </example>
        static partial void ShowMainWindow()
        {
        }
    }
}