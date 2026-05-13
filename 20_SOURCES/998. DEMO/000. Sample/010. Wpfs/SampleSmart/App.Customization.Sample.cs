using Dreamine.Communication.Abstractions.Interfaces;
using Dreamine.Communication.Core.Buses;
using Dreamine.Logging.Formatters;
using Dreamine.Logging.Interfaces;
using Dreamine.Logging.Models;
using Dreamine.Logging.Services;
using Dreamine.Logging.Sinks;
using Dreamine.Logging.Wpf.Registration;
using Dreamine.Logging.Wpf.Services;
using Dreamine.Logging.Wpf.ViewModels;
using Dreamine.Logging.Wpf.Views;
using Dreamine.MVVM.Core;
using Dreamine.MVVM.Wpf;
using Dreamine.Threading.Allocators;
using Dreamine.Threading.Interfaces;
using Dreamine.Threading.Models;
using Dreamine.Threading.Policies;
using Dreamine.Threading.Services;
using Dreamine.Threading.Windows.Registration;
using Dreamine.Threading.Wpf.Registration;
using Dreamine.Threading.Wpf.ViewModels;
using SampleSmart.Pages;
using SampleSmart.Pages.PageSub;
using SampleSmart.Pages.PageSub.CommunicationTabs;
using System;
using System.IO;
using System.Windows;

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
            RegisterThreading();

            DMContainer.RegisterSingleton<IMessageBus, InMemoryMessageBus>();
            DMContainer.RegisterSingleton<CommunicationSampleRuntime>();
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
            logger.Info("Threading system registered.");     
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

        private static void RegisterLogging()
        {
            DreamineLoggingWpfRegistration.Register(options =>
            {
                options.Category = "SampleSmart";
                options.LogDirectory = System.IO.Path.Combine(AppContext.BaseDirectory, "Logs");
                options.StoreCapacity = 1000;
                options.QueueCapacity = 8192;
                options.DrainBatchSize = 256;
                options.FlushEveryWriteCount = 20;
                options.ShutdownTimeout = TimeSpan.FromSeconds(2);
            });
        }

        /// <summary>
        /// Registers Dreamine threading services.
        /// </summary>
        private static void RegisterThreading()
        {
            DreamineThreadingWpfRegistration.Register(options =>
            {
                options.RegisterWindowsServices = true;
                options.UseAdaptiveCpuPolicy = true;
                options.RegisterThreadMonitor = true;
            });
        }
    }
}