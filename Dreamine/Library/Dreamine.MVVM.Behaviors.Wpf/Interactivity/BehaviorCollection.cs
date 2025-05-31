using Dreamine.MVVM.Behaviors.Core.Base;
using Dreamine.MVVM.Behaviors.Core.Interfaces;
using System.Diagnostics;
using System.Windows;
using System.Windows.Markup;

namespace Dreamine.MVVM.Behaviors.Wpf.Interactivity
{
	/// <summary>
	/// 📦 Dreamine용 Behavior 컬렉션입니다.
	/// - XAML에서 여러 개의 Behavior 요소를 포함할 수 있는 컬렉션 컨테이너입니다.
	/// </summary>
	public sealed class BehaviorCollection : FreezableCollection<DependencyObject>, IAttachedObject
	{
		private DependencyObject _associatedObject;  // 연결된 대상 객체
		public DependencyObject AssociatedObject => _associatedObject; // 연결된 대상 객체의 접근자

		/// <summary>
		/// <para>Attach 메서드:</para>
		/// `BehaviorCollection`이 특정 `DependencyObject`에 연결됩니다. 
		/// `AssociatedObject`가 설정되지 않은 경우에만 연결됩니다.
		/// 각 `Behavior`가 `associatedObject`에 Attach 처리됩니다.
		/// </summary>
		public void Attach(DependencyObject associatedObject)
		{
			// 이미 다른 객체에 연결되었으면 예외 발생
			if (_associatedObject != associatedObject)
			{
				// 이미 다른 객체에 연결된 상태라면 오류 처리
				if (_associatedObject != null)
					throw new InvalidOperationException("Cannot attach a collection to more than one object.");

				_associatedObject = associatedObject; // 새로운 객체에 연결

				// `BehaviorCollection`의 각 항목에 대해 `Attach()` 호출
				foreach (var item in this)
				{
					// `Behavior<FrameworkElement>` 타입인 경우만 Attach 처리
					if (item is Behavior<FrameworkElement> behavior)
					{						
						behavior.Attach(associatedObject);  // behavior Attach 처리
					}
					else if (item is IBehavior ibehavior)  // `IBehavior` 타입도 처리
					{
						ibehavior.Attach(associatedObject);  // behavior Attach 처리
					}
				}
			}
		}

		/// <summary>
		/// <para>Detach 메서드:</para>
		/// `BehaviorCollection`을 `associatedObject`에서 분리합니다.
		/// 연결된 `Behavior` 항목에 대해 `Detach()`를 호출합니다.
		/// </summary>
		public void Detach()
		{
			// `BehaviorCollection`의 각 항목에 대해 `Detach()` 호출
			foreach (var item in this)
			{
				// `Behavior<FrameworkElement>` 타입인 경우 Detach 처리
				if (item is Behavior<FrameworkElement> behavior)
				{
					behavior.Detach();  // behavior Detach 처리
				}
				else if (item is IBehavior ibehavior)  // `IBehavior` 타입도 처리
				{
					ibehavior.Detach();  // behavior Detach 처리
				}
			}

			_associatedObject = null; // 분리 후 `associatedObject`를 null로 설정
		}
	}
}
