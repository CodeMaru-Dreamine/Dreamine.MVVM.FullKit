using Dreamine.MVVM.Attributes;
using SampleEnterprise.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleEnterprise.Models
{
	/// <summary>
	/// MainWindow에 대한 ViewModel 클래스입니다.
	/// Model과 Event 사이의 바인딩을 담당합니다.
	/// </summary>
	public partial class MainWindowModel
	{
		[DreamineProperty]
		private string _title = "타이틀 없는 드래그 가능한 창";

		[DreamineProperty]
		private string _message = "Dreamine 프레임워크로 구성된 MVVM 템플릿입니다.";
	}
}
