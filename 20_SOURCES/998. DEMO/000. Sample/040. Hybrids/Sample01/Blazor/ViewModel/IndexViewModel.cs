/// \file IndexViewModel.cs
/// \brief Blazor 대시보드 ViewModel.
/// \details Blazor는 요청만 발행하고, 최종 실행/상태 소유는 WPF 쉘이 담당한다.
/// \author Dreamine
/// \date 2026-01-28
/// \version 1.1.0

using Dreamine.Hybrid.Interfaces;
using Sample01.Messages;
using System;
using System.Threading.Tasks;

namespace Sample01.Blazor.ViewModels
{
    /// <summary>
    /// \if KO
    /// <para>Index View Model 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>\brief Provides interaction logic for the Dreamine Blazor dashboard.</para>
    /// \endif
    /// </summary>
    public sealed class IndexViewModel
    {
        /// <summary>
        /// \if KO
        /// <para>bus 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the bus value.</para>
        /// \endif
        /// </summary>
        private readonly IHybridMessageBus _bus;

        /// <summary>
        /// \if KO
        /// <para>지정한 설정으로 <see cref="IndexViewModel"/> 클래스의 새 인스턴스를 초기화합니다.</para>
        /// \endif
        /// \if EN
        /// <para>\brief Initializes a new instance of the <see cref="IndexViewModel"/> class.</para>
        /// \endif
        /// </summary>
        /// <param name="bus">
        /// \if KO
        /// <para>bus에 사용할 <c>IHybridMessageBus</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The hybrid message bus.</para>
        /// \endif
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// \if KO
        /// <para>필수 입력 인자 중 하나가 <see langword="null"/>인 경우 발생합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Thrown when a required input argument is <see langword="null"/>.</para>
        /// \endif
        /// </exception>
        public IndexViewModel(IHybridMessageBus bus)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        /// <summary>
        /// \if KO
        /// <para>Version 값을 가져옵니다.</para>
        /// \endif
        /// \if EN
        /// <para>\brief Gets the display version.</para>
        /// \endif
        /// </summary>
        public string Version => "1.1.0";

        /// <summary>
        /// \if KO
        /// <para>Build Time 값을 가져옵니다.</para>
        /// \endif
        /// \if EN
        /// <para>\brief Gets the sample build time.</para>
        /// \endif
        /// </summary>
        public DateTime BuildTime => DateTime.Now;

        /// <summary>
        /// \if KO
        /// <para>Open Project Async 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>\brief Requests the WPF shell to open the project view.</para>
        /// \endif
        /// </summary>
        /// <returns>
        /// \if KO
        /// <para>Open Project Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
        /// \endif
        /// \if EN
        /// <para>A task that represents the asynchronous operation.</para>
        /// \endif
        /// </returns>
        public Task OpenProjectAsync()
        {
            return _bus.PublishAsync(
                new DashboardActionRequestedMessage(DashboardAction.OpenProject));
        }

        /// <summary>
        /// \if KO
        /// <para>Open Nuget Async 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>\brief Requests the WPF shell to open the NuGet view.</para>
        /// \endif
        /// </summary>
        /// <returns>
        /// \if KO
        /// <para>Open Nuget Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
        /// \endif
        /// \if EN
        /// <para>A task that represents the asynchronous operation.</para>
        /// \endif
        /// </returns>
        public Task OpenNugetAsync()
        {
            return _bus.PublishAsync(
                new DashboardActionRequestedMessage(DashboardAction.OpenNuget));
        }

        /// <summary>
        /// \if KO
        /// <para>Open Docs Async 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>\brief Requests the WPF shell to open the documentation view.</para>
        /// \endif
        /// </summary>
        /// <returns>
        /// \if KO
        /// <para>Open Docs Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
        /// \endif
        /// \if EN
        /// <para>A task that represents the asynchronous operation.</para>
        /// \endif
        /// </returns>
        public Task OpenDocsAsync()
        {
            return _bus.PublishAsync(
                new DashboardActionRequestedMessage(DashboardAction.OpenDocs));
        }

        /// <summary>
        /// \if KO
        /// <para>Open Settings Async 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>\brief Requests the WPF shell to open the settings view.</para>
        /// \endif
        /// </summary>
        /// <returns>
        /// \if KO
        /// <para>Open Settings Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
        /// \endif
        /// \if EN
        /// <para>A task that represents the asynchronous operation.</para>
        /// \endif
        /// </returns>
        public Task OpenSettingsAsync()
        {
            return _bus.PublishAsync(
                new DashboardActionRequestedMessage(DashboardAction.OpenSettings));
        }
    }
}
