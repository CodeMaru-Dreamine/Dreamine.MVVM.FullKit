using System.Windows;

namespace SampleSmart.Pages.WindowSub
{
	/// <summary>
	/// 드리마인 페이지 전용 이벤트 클래스입니다.
	/// 기본 제공 이벤트(예: 닫기, 최소화 등)를 원할 경우 아래 주석을 해제하세요.
	/// </summary>
	public class WindowSubEvent
	{
		///public void Ok() => MessageBox.Show("확인 클릭됨!");		

		///#region [옵션] 드리마인 스타일 창 제어용 기본 이벤트

		///private readonly IViewManager _viewManager;
		///public MainWindowEvent(IViewManager viewManager) => _viewManager = viewManager;

		///public void Minimize()
		///{
		///	var w = GetActiveWindow();
		///	if (w != null) w.WindowState = WindowState.Minimized;
		///}

		///public void Maximize()
		///{
		///	var w = GetActiveWindow();
		///	if (w != null)
		///	{
		///		w.WindowState = w.WindowState == WindowState.Maximized
		///			? WindowState.Normal
		///			: WindowState.Maximized;
		///	}
		///}

		///public void Close()
		///{
		///	GetActiveWindow()?.Close();
		///}

		///private Window? GetActiveWindow()
		///{
		///	return Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
		///}

		///#endregion
		///
		public string ReadmeCleck()
		{
			return "Readme 눌림";
		}
	}
}
