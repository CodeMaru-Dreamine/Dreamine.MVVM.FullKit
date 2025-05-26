using Dreamine.MVVM.Core;
using DreamineApp.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace DreamineApp.Managers
{
	public class ViewManager : IViewManager
	{
		public void Show<TViewModel>() where TViewModel : class
		{
			var vmType = typeof(TViewModel);
			var viewTypeName = vmType.FullName!
				.Replace(".ViewModels.", ".Views.")
				.Replace("ViewModel", "");

			var asm = vmType.Assembly;
			var viewType = asm.GetType(viewTypeName);
			if (viewType is null) return;

			var instance = Activator.CreateInstance(viewType);

			if (instance is Window window)
			{
				// ViewModel 연결
				window.DataContext = DMContainer.Resolve<TViewModel>();
				window.Show();
			}
			else if (instance is UserControl uc)
			{
				// 💡 UserControl은 따로 Window로 감싸고 DataContext 직접 설정
				var host = new Window
				{
					Content = uc,
					Width = 800,
					Height = 600,
					Title = vmType.Name.Replace("ViewModel", "")
				};

				// ✅ 여기서 연결
				uc.DataContext = DMContainer.Resolve<TViewModel>();
				host.Show();
			}
		}

	}

}
