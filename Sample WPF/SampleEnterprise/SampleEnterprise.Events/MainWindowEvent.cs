using SampleEnterprise.Interfaces;
using System.Windows;

namespace SampleEnterprise.Events
{
	public class MainWindowEvent
	{
		private readonly IViewManager _viewManager;
		public MainWindowEvent(IViewManager viewManager) => _viewManager = viewManager;

		public void Ok() => MessageBox.Show("확인 클릭됨!");
		public void Cancel() => MessageBox.Show("취소 클릭됨!");

		public void SubPage()
		{
			var vmType = Type.GetType("SampleEnterprise.ViewModels.PageSub.PageSubViewModel, SampleEnterprise.ViewModels");
			if (vmType != null)
				_viewManager.Show(vmType);
		}


		public void Minimize()
		{
			var w = GetActiveWindow();
			if (w != null) w.WindowState = WindowState.Minimized;
		}

		public void Maximize()
		{
			var w = GetActiveWindow();
			if (w != null)
			{
				w.WindowState = w.WindowState == WindowState.Maximized
					? WindowState.Normal
					: WindowState.Maximized;
			}
		}

		public void Close()
		{
			GetActiveWindow()?.Close();
		}

		private Window? GetActiveWindow()
		{
			return Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
		}
	}
}
