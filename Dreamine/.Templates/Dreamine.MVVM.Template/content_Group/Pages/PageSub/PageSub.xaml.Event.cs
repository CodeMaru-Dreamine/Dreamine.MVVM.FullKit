using System.Windows;

namespace DreamineApp.Pages.PageSub
{
	public class PageSubEvent
	{
		public void Ok() => MessageBox.Show("확인 클릭됨!");
		public void Cancel() => MessageBox.Show("취소 클릭됨!");
	}
}
