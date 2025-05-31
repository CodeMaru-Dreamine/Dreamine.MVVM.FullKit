using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dreamine.MVVM.Interfaces.Navigation
{
	/// <summary>
	/// 📌 ViewModel에 대응하는 View를 ContentControl에 표시하는 내비게이터 인터페이스입니다.
	/// </summary>
	public interface INavigator
	{
		/// <summary>
		/// 📌 ViewModel을 기반으로 View를 생성하고, ContentControl에 표시합니다.
		/// </summary>
		/// <param name="viewModel">표시할 ViewModel 인스턴스</param>
		void Navigate(object viewModel);
	}
}
