using Dreamine.MVVM.Core;
using Dreamine.MVVM.Interfaces.Navigation;
using Dreamine.MVVM.Locators;
using DreamineApp.Interfaces;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace DreamineApp.Managers
{
	/// <summary>
	/// View ↔ ViewModel 자동 연결 및 화면 전환 기능을 담당합니다.
	/// INavigator가 있을 경우 Region 기반 네비게이션을 우선 사용합니다.
	/// 그렇지 않을 경우 ViewModelLocator 기반으로 직접 View를 생성합니다.
	/// </summary>
	public class ViewManager : IViewManager
	{
		/// <summary>
		/// 제네릭 타입 TViewModel을 기준으로 View를 생성하거나 Navigator를 통해 전환합니다.
		/// </summary>
		public void Show<TViewModel>() where TViewModel : class
		{
			var vm = DMContainer.Resolve<TViewModel>();
			var navigator = TryGetNavigator();

			if (navigator != null)
			{
				navigator.Navigate(vm);
			}
			else
			{
				var view = ViewModelLocator.ResolveView(typeof(TViewModel));
				if (view is FrameworkElement fe)
				{
					fe.DataContext = vm;
					TryShowView(view);
				}
			}
		}

		/// <summary>
		/// 런타임에 주어진 ViewModel Type을 기준으로 View를 생성하거나 Navigator를 통해 전환합니다.
		/// </summary>
		public void Show(Type viewModelType)
		{
			var vm = DMContainer.Resolve(viewModelType);
			var navigator = TryGetNavigator();

			if (navigator != null)
			{
				navigator.Navigate(vm);
			}
			else
			{
				var view = ViewModelLocator.ResolveView(viewModelType);
				if (view is FrameworkElement fe)
				{
					fe.DataContext = vm;
					TryShowView(view);
				}
			}
		}

		/// <summary>
		/// View 인스턴스가 Window인지 UserControl인지 구분하여 표시합니다.
		/// </summary>
		private void TryShowView(object view)
		{
			switch (view)
			{
				case Window w:
					w.Show();
					break;

				case UserControl uc:
					new Window
					{
						Content = uc,
						Width = 800,
						Height = 600,
						WindowStartupLocation = WindowStartupLocation.CenterScreen,
						Title = uc.GetType().Name.Replace("View", "")
					}.Show();
					break;

				default:
					break;
			}
		}

		/// <summary>
		/// INavigator를 DI에서 가져오되 실패하면 null 반환 (선택적 구성 허용)
		/// </summary>
		private INavigator? TryGetNavigator()
		{
			try
			{
				return DMContainer.Resolve<INavigator>();
			}
			catch
			{
				return null;
			}
		}
	}
}
