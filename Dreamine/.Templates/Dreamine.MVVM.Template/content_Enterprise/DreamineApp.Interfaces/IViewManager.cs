namespace DreamineApp.Interfaces
{
	/// <summary>
	/// View ↔ ViewModel 연결을 통해 화면을 표시하는 관리자 인터페이스입니다.
	/// </summary>
	public interface IViewManager
	{
		/// <summary>
		/// ViewModel 타입 T를 기준으로 View를 찾아서 화면에 표시합니다.
		/// </summary>
		/// <typeparam name="TViewModel">ViewModel 타입</typeparam>
		void Show<TViewModel>() where TViewModel : class;

		/// <summary>
		/// 런타임에 제공된 타입을 기준으로 View를 표시합니다.
		/// </summary>
		/// <param name="viewModelType">ViewModel 타입 정보</param>
		void Show(Type viewModelType);
	}
}

