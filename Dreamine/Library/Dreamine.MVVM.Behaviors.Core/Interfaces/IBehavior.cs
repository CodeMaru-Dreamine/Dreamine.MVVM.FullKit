using System.Windows;

namespace Dreamine.MVVM.Behaviors.Core.Interfaces
{
	/// <summary>
	/// 📌 Dreamine에서 모든 Behavior가 구현해야 하는 핵심 인터페이스입니다.
	/// 
	/// - View 요소(DependencyObject)에 연결/분리되는 확장 동작을 정의합니다.
	/// - XAML 또는 코드에서 동적으로 attach 가능한 구조 기반입니다.
	/// </summary>
	public interface IBehavior
	{
		/// <summary>
		/// 지정된 UI 요소에 이 Behavior를 연결합니다.
		/// </summary>
		/// <param name="dependencyObject">연결 대상 요소</param>
		void Attach(DependencyObject dependencyObject);

		/// <summary>
		/// 현재 연결된 UI 요소에서 이 Behavior를 분리합니다.
		/// </summary>
		void Detach();
	}
}
