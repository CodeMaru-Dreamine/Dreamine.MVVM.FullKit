using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.Core;
using Dreamine.MVVM.Extensions;
using Dreamine.MVVM.Interfaces.Navigation;
using Dreamine.MVVM.Locators.Wpf;
using System.Windows;

namespace SampleCore
{
	/// <summary>
	/// 📌 Dreamine 애플리케이션의 진입점 클래스를 지정하는 전용 Attribute입니다.
	/// 
	/// 이 Attribute는 반드시 <c>App.xaml.cs</c>의 <c>partial class App</c>에만 사용되어야 하며,  
	/// <c>Dreamine CLI</c> 또는 자동화 도구들이 해당 클래스를 애플리케이션의 **시작점**으로 인식하는 데 사용됩니다.
	/// 
	/// ❗ 다른 클래스에 부착 시 예외가 발생하거나 예기치 않은 동작이 발생할 수 있습니다.
	/// </summary>
	/// <remarks>
	/// - Dreamine CLI 또는 템플릿 엔진에서 해당 클래스를 기준으로 종속 구조를 자동 분석합니다. <br/>
	/// - 필수 조건: WPF <c>App.xaml</c>과 연결된 <c>partial class App : Application</c>에만 부착
	/// </remarks>
	/// <example>
	/// <code>
	/// [DreamineEntry]
	/// public partial class App : Application
	/// {
	///     // WPF 시작점 클래스
	/// }
	/// </code>
	/// </example>
	[DreamineEntry]
	public partial class App : Application
	{
		/// <summary>
		/// DI 등록 전에 호출되는 사용자 정의 사전 등록 영역입니다.
		/// 예: 커스텀 로그, 설정 로딩 등
		/// </summary>
		private void RegisterBefore()
		{

		}

		/// <summary>
		/// DI 및 ViewModelLocator 등록 후 호출되는 사용자 정의 후처리입니다.
		/// 예: 초기 Navigation, 이벤트 바인딩 등
		/// </summary>
		private void RegisterAfter()
		{
			var mainWindow = Application.Current.MainWindow;

			if (mainWindow == null)
			{
				return;
			}

			mainWindow.Loaded += (s, e) =>
			{
				var region = RegionBinderHelper.FindRegionControl(mainWindow, "SubPage");

				if (region != null)
				{
					DMContainer.RegisterSingleton<INavigator>(new ContentControlNavigator(region));
				}
			};
		}

		/// <summary>
		/// 사용자가 수동으로 XAML이 아닌 동적으로 Main Window를 생성하고자 할 경우 이 메서드를 오버라이드하여 사용합니다.
		/// 일반적으로 DI Container에서 ViewModel을 Resolve한 뒤 View에 DataContext로 할당하여 Show 합니다.
		/// </summary>
		private void ShowMainWindow()
		{

		}
	}
}
