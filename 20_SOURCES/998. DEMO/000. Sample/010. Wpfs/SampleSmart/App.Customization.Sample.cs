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
using SampleSmart.Pages.PageSub.IoTabs;
using SampleSmart.Pages.PageSub.PlcTabs;
using System;
using System.IO;
using System.Windows;

namespace SampleSmart
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
            RegisterLogging();
            RegisterThreading();

            DMContainer.RegisterSingleton<IMessageBus, InMemoryMessageBus>();
            DMContainer.RegisterSingleton<CommunicationSampleRuntime>();
            DMContainer.RegisterSingleton<IoSampleRuntime>();
            DMContainer.RegisterSingleton<PlcSampleRuntime>();
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
            var logger = DMContainer.Resolve<IDreamineLogger>();           

            logger.Info("Application initialized.");
            logger.Info("Logging system registered.");
            logger.Info("Threading system registered.");     
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

        }

        /// <summary>
        /// \if KO
        /// <para>Register Logging 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the register logging operation.</para>
        /// \endif
        /// </summary>
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
        /// \if KO
        /// <para>Register Threading 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Registers Dreamine threading services.</para>
        /// \endif
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
