/// \file IndexViewModel.cs
/// \brief Blazor 대시보드 ViewModel.
/// \details Blazor는 요청만 발행하고, 최종 실행/상태 소유는 WPF 쉘이 담당한다.
/// \author Dreamine
/// \date 2026-01-28
/// \version 1.1.0

using Dreamine.Hybrid.Interfaces;
using Dreamine.Hybrid.Messaging;
using System;
using System.Threading.Tasks;

namespace Sample01.Blazor.ViewModels
{
    /// <summary>
    /// \brief Provides interaction logic for the Dreamine Blazor dashboard.
    /// </summary>
    public sealed class IndexViewModel
    {
        private readonly IHybridMessageBus _bus;

        /// <summary>
        /// \brief Initializes a new instance of the <see cref="IndexViewModel"/> class.
        /// </summary>
        /// <param name="bus">The hybrid message bus.</param>
        public IndexViewModel(IHybridMessageBus bus)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        /// <summary>
        /// \brief Gets the display version.
        /// </summary>
        public string Version => "1.1.0";

        /// <summary>
        /// \brief Gets the sample build time.
        /// </summary>
        public DateTime BuildTime => DateTime.Now;

        /// <summary>
        /// \brief Requests the WPF shell to open the project view.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task OpenProjectAsync()
        {
            return _bus.PublishAsync(
                new DashboardActionRequestedMessage(DashboardAction.OpenProject));
        }

        /// <summary>
        /// \brief Requests the WPF shell to open the NuGet view.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task OpenNugetAsync()
        {
            return _bus.PublishAsync(
                new DashboardActionRequestedMessage(DashboardAction.OpenNuget));
        }

        /// <summary>
        /// \brief Requests the WPF shell to open the documentation view.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task OpenDocsAsync()
        {
            return _bus.PublishAsync(
                new DashboardActionRequestedMessage(DashboardAction.OpenDocs));
        }

        /// <summary>
        /// \brief Requests the WPF shell to open the settings view.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task OpenSettingsAsync()
        {
            return _bus.PublishAsync(
                new DashboardActionRequestedMessage(DashboardAction.OpenSettings));
        }
    }
}