using System.Reflection;
using System.Windows;
using Dreamine.MVVM.Locators;

namespace Dreamine.MVVM.Core
{
	/// <summary>
	/// Dreamine 앱 구동을 위한 초기화 도우미입니다.
	/// </summary>
	public static class DreamineAppBuilder
	{
		public static void Initialize(Assembly rootAssembly)
		{
			// ① View ↔ ViewModel 자동 매핑
			ViewModelLocator.RegisterAll(rootAssembly);

			// ② DI 자동 등록 (Model, Event, Manager, View, ViewModel)
			DMContainer.AutoRegisterAll(rootAssembly);

			// ③ Loaded 시 ViewModel 자동 주입
			EventManager.RegisterClassHandler(
				typeof(FrameworkElement),
				FrameworkElement.LoadedEvent,
				new RoutedEventHandler(AttachViewModelIfExists));
		}

		/// <summary>
		/// WPF FrameworkElement의 Loaded 이벤트에서 호출되는 핸들러입니다.
		/// View의 DataContext가 비어 있을 경우, ViewModelLocator를 통해 ViewModel을 자동 연결합니다.
		/// </summary>
		/// <param name="sender">이벤트가 발생한 View 인스턴스 (FrameworkElement)</param>
		/// <param name="e">이벤트 인자</param>
		private static void AttachViewModelIfExists(object sender, RoutedEventArgs e)
		{
			if (sender is FrameworkElement view && view.DataContext == null)
			{
				var vm = ViewModelLocator.Resolve(view.GetType());
				if (vm != null)
					view.DataContext = vm;
			}
		}
	}
}
