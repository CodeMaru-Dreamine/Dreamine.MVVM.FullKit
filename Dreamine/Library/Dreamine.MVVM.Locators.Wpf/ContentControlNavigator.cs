using Dreamine.MVVM.Interfaces.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Dreamine.MVVM.Locators.Wpf
{
	/// <summary>
	/// ContentControl을 기반으로 ViewModel에 대응하는 View를 표시하는 기본 네비게이터
	/// </summary>
	public class ContentControlNavigator : INavigator
	{
		private readonly ContentControl _target;

		public ContentControlNavigator(ContentControl target)
		{
			_target = target;
		}

		public void Navigate(object viewModel)
		{
			var view = ViewModelBinder.ResolveView(viewModel);
			_target.Content = view;
		}
	}
}
