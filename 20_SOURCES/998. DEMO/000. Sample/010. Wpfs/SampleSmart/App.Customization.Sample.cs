using Dreamine.Logging.Formatters;
using Dreamine.Logging.Interfaces;
using Dreamine.Logging.Services;
using Dreamine.Logging.Sinks;
using Dreamine.Logging.Wpf.Services;
using Dreamine.Logging.Wpf.ViewModels;
using Dreamine.Logging.Wpf.Views;
using Dreamine.MVVM.Core;
using Dreamine.MVVM.Wpf;
using SampleSmart.Pages.PageSub;
using System;
using System.IO;

namespace SampleSmart
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
            RegisterLogging();
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
            var logger = DMContainer.Resolve<IDreamineLogger>();

            logger.Info("Application initialized.");
            logger.Info("Logging system registered.");
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

        /// <summary>
        /// Registers Dreamine logging services.
        /// </summary>
        private static void RegisterLogging()
        {
            var logStore = new InMemoryLogStore();

            var formatter = new DreamineTextLogFormatter();

            var textFileSink = new TextFileLogSink(
                Path.Combine(AppContext.BaseDirectory, "Logs"),
                formatter);

            var compositeSink = new CompositeLogSink(new IDreamineLogSink[]
            {
        logStore,
        textFileSink
            });

            DMContainer.RegisterSingleton(logStore);

            DMContainer.RegisterSingleton<IDreamineLogStore>(logStore);

            // IDreamineLogSink는 이제 MemoryStore가 아니라 Composite를 등록해야 함.
            DMContainer.RegisterSingleton<IDreamineLogSink>(compositeSink);

            DMContainer.RegisterSingleton<IDreamineLogFormatter>(formatter);

            DMContainer.RegisterSingleton<IDreamineLogger>(
                new DreamineLogger(compositeSink, "SampleSmart"));

            DMContainer.RegisterSingleton<WpfLogUiDispatcher>(
                new WpfLogUiDispatcher());

            DMContainer.Register<DreamineLogPanelViewModel>(() =>
                new DreamineLogPanelViewModel(
                    DMContainer.Resolve<IDreamineLogStore>(),
                    DMContainer.Resolve<WpfLogUiDispatcher>()));

            DMContainer.Register<PageLogViewModel>(() =>
                new PageLogViewModel(
                    DMContainer.Resolve<DreamineLogPanelViewModel>()));
        }
    }
}