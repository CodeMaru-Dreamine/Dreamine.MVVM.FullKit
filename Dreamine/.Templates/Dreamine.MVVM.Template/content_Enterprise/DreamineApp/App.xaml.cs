using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.Core;
using Dreamine.MVVM.Extensions;
using Dreamine.MVVM.Interfaces.Navigation;
using Dreamine.MVVM.Locators.Wpf;
using DreamineApp.ViewModels;
using DreamineApp.Views;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace DreamineApp
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	[DreamineEntry]
	public partial class App : Application
	{
		/// <summary>
		/// DI 등록 전에 호출되는 사용자 정의 사전 등록 영역입니다.
		/// 예: 커스텀 로그, 설정 로딩 등
		/// </summary>
		private void RegisterBefore()
		{
			ForceLoadAssembly("DreamineApp.ViewModels.dll");
			ForceLoadAssembly("DreamineApp.Views.dll");
			ForceLoadAssembly("DreamineApp.Events.dll");
			ForceLoadAssembly("DreamineApp.Interfaces.dll");
			ForceLoadAssembly("DreamineApp.Managers.dll");
			ForceLoadAssembly("DreamineApp.Models.dll");
			ForceLoadAssembly("DreamineApp.tEST.dll");
		}

		/// <summary>
		/// DI 및 ViewModelLocator 등록 후 호출되는 사용자 정의 후처리입니다.
		/// 예: 초기 Navigation, 이벤트 바인딩 등
		/// </summary>
		private void RegisterAfter()
		{

		}

		/// <summary>
		/// 사용자가 수동으로 XAML이 아닌 동적으로 Main Window를 생성하고자 할 경우 이 메서드를 오버라이드하여 사용합니다.
		/// 일반적으로 DI Container에서 ViewModel을 Resolve한 뒤 View에 DataContext로 할당하여 Show 합니다.
		/// </summary>
		private void ShowMainWindow()
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
