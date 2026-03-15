using Dreamine.MVVM.Core;
using Dreamine.MVVM.Interfaces.Navigation;
using SampleSmart.Interfaces;
using System.Windows;
using System.Windows.Controls;

namespace SampleSmart.Managers
{	
	/// <summary>
	/// ViewModel을 기반으로 View를 자동 생성 및 표시하는 ViewManager입니다.
	/// - UserControl은 INavigator 사용
	/// - Window는 독립 창으로 표시
	/// </summary>
	public class ViewManager : IViewManager
	{
		/// <summary>
		/// ViewModel에 해당하는 View를 찾아 자동으로 표시합니다.
		/// </summary>
		/// <typeparam name="TViewModel">ViewModel 타입</typeparam>
		public void Show<TViewModel>() where TViewModel : class
		{
			var vm = DMContainer.Resolve<TViewModel>();
			var view = CreateViewFromViewModel(typeof(TViewModel));
			if (view == null) return;

			switch (view)
			{
				case Window window:
					window.DataContext = vm;
					window.Show();
					break;

				case UserControl control:
					control.DataContext = vm;

					var navigator = TryGetNavigator();
					if (navigator != null)
					{
						navigator.Navigate(vm); // 또는 Navigate(control)로 구조 변경 가능
					}
					else
					{
						new Window { Content = control, Width = 800, Height = 600 }.Show();
					}
					break;
			}
		}

		/// <summary>
		/// ViewModel 네이밍 규칙에 따라 View를 생성합니다.
		/// </summary>
		/// <param name="viewModelType">ViewModel 타입</param>
		/// <returns>Window 또는 UserControl 인스턴스</returns>
		private object? CreateViewFromViewModel(Type viewModelType)
		{
			var viewTypeName = viewModelType.FullName!
				.Replace(".ViewModels.", ".Pages.")
				.Replace("ViewModel", "");

			var asm = viewModelType.Assembly;
			var viewType = asm.GetType(viewTypeName);
			if (viewType == null) return null;

			return Activator.CreateInstance(viewType);
		}

		/// <summary>
		/// INavigator 인스턴스를 시도하여 가져옵니다.
		/// </summary>
		private INavigator? TryGetNavigator()
		{
			try { return DMContainer.Resolve<INavigator>(); }
			catch { return null; }
		}
	}
	
}
