using System.Windows;

namespace Dreamine.MVVM.Behaviors.Core.Interfaces
{
	/// <summary>
	/// 📌 Dreamine의 모든 Behavior는 이 인터페이스를 통해 연결 객체를 관리합니다.
	/// 
	/// FrameworkElement 또는 DependencyObject와 연결되는 객체의 공통 인터페이스로,
	/// Behavior 및 트리거 확장 구현 시 연결 대상의 참조를 일관되게 제공합니다.
	/// </summary>
	public interface IAttachedObject
	{
		/// <summary>
		/// 연결된 객체 (DependencyObject)를 반환합니다.
		/// </summary>
		DependencyObject AssociatedObject { get; }

		/// <summary>
		/// 지정된 DependencyObject에 현재 객체를 연결합니다.
		/// </summary>
		/// <param name="dependencyObject">연결할 대상 객체입니다.</param>
		void Attach(DependencyObject dependencyObject);

		/// <summary>
		/// 현재 객체를 연결된 대상에서 분리합니다.
		/// </summary>
		void Detach();
	}	
}
