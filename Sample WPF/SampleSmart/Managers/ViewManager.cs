using Dreamine.MVVM.Core;
using Dreamine.MVVM.Interfaces.Navigation;
using SampleSmart.Interfaces;
using System.Windows;
using System.Windows.Controls;

namespace SampleSmart.Managers
{
	public class ViewManager : IViewManager
	{
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
				// ❗ 그 외엔 기존처럼 Window로 표시
				var viewTypeName = typeof(TViewModel).FullName!
					.Replace(".ViewModels.", ".Pages.")
					.Replace("ViewModel", "");

				var asm = typeof(TViewModel).Assembly;
				var viewType = asm.GetType(viewTypeName);
				if (viewType is null) return;

				var instance = Activator.CreateInstance(viewType);
				if (instance is Window w)
				{
					w.DataContext = vm;
					w.Show();
				}
				else if (instance is UserControl uc)
				{
					uc.DataContext = vm;
					new Window { Content = uc, Width = 800, Height = 600 }.Show();
				}
			}
		}

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
