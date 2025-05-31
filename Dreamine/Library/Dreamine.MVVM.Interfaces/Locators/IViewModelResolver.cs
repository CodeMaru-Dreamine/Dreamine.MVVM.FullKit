using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dreamine.MVVM.Interfaces.Locators
{
	/// <summary>
	/// 📌 ViewModel 생성 전략을 위한 DI 추상 인터페이스입니다.
	///
	/// Dreamine의 ViewModelLocator와 함께 사용되며,
	/// DI 컨테이너 또는 수동 인스턴스화 전략을 연결하는 역할을 합니다.
	/// </summary>
	public interface IViewModelResolver
	{
		/// <summary>
		/// 지정된 ViewModel 타입을 생성하여 반환합니다.
		/// </summary>
		/// <param name="viewModelType">생성할 ViewModel의 Type 정보</param>
		/// <returns>ViewModel 인스턴스 또는 null</returns>
		object? Resolve(Type viewModelType);
	}
}
