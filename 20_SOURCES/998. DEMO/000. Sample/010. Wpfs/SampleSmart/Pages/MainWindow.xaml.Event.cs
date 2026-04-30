using Dreamine.MVVM.Interfaces.Navigation;
using SampleSmart.Pages.PageSub;
using SampleSmart.Pages.WindowSub;
using System.Windows;

namespace SampleSmart.Pages
{
	public class MainWindowEvent
	{
		private readonly IViewManager _viewManager;
		public MainWindowEvent(IViewManager viewManager) => _viewManager = viewManager;

		public void Ok() => MessageBox.Show("확인 클릭됨!");
		public void Cancel() => MessageBox.Show("취소 클릭됨!");

		public void SubPage() => _viewManager.Show<PageSubViewModel>();

        public void SubPage2() => _viewManager.Show<PageSub2ViewModel>();

        public void SubWindow() => _viewManager.Show<WindowSubViewModel>();

        public void NoticeWindow() => _viewManager.Show<PopupNoticeViewModel>();

        public void MonitorWindow() => _viewManager.Show<PopupMonitorViewModel>();

        public void SettingWindow() => _viewManager.Show<PopupSettingViewModel>();

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
