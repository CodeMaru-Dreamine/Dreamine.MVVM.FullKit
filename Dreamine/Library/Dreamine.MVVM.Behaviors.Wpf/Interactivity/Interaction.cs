using Dreamine.MVVM.Behaviors.Core.Interfaces;
using System.ComponentModel;
using System.Windows;

namespace Dreamine.MVVM.Behaviors.Wpf.Interactivity
{
	/// <summary>
	/// 🧩 Dreamine용 Interaction static 헬퍼 클래스입니다.
	/// - WPF의 XAML에서 Behavior 컬렉션을 선언적으로 연결할 수 있게 도와줍니다.
	/// </summary>
	public static class Interaction
	{
		/// <summary>
		/// 'Behaviors'라는 이름으로 BehaviorCollection을 등록하는 DependencyProperty입니다.
		/// - 이 프로퍼티를 통해 XAML에서 Behavior 컬렉션을 연결합니다.
		/// </summary>
		public static readonly DependencyProperty BehaviorsProperty =
			DependencyProperty.RegisterAttached(
				"ShadowBehaviors", // XAML에서 사용될 프로퍼티 이름
				typeof(BehaviorCollection), // 이 프로퍼티가 가질 데이터 타입
				typeof(Interaction), // 프로퍼티가 등록될 타입 (Interaction)
				new FrameworkPropertyMetadata(
					new PropertyChangedCallback(OnBehaviorsChanged)) // 프로퍼티 변경 시 호출될 메서드
			);

		/// <summary>
		/// XAML에서 특정 DependencyObject에 연결된 Behavior 컬렉션을 가져옵니다.
		/// - BehaviorCollection이 없으면 새로 생성하고, 기본 Behavior를 추가합니다.
		/// </summary>
		/// <param name="obj">BehaviorCollection을 가져올 대상 객체</param>
		/// <returns>연결된 BehaviorCollection</returns>
		public static BehaviorCollection GetBehaviors(DependencyObject obj)
		{
			// 현재 BehaviorCollection을 가져옵니다.
			var collection = (BehaviorCollection)obj.GetValue(BehaviorsProperty);

			// 만약 BehaviorCollection이 없으면 새로 생성하고 기본 Behavior를 추가합니다.
			if (collection == null)
			{
				collection = new BehaviorCollection();

				// 예시로 WindowDragBehavior를 추가
				var behavior = new WindowDragBehavior();
				collection.Add(behavior); // Behavior를 컬렉션에 추가

				// 새로 생성한 컬렉션을 해당 객체에 연결
				SetBehaviors(obj, collection);
			}

			return collection; // BehaviorCollection을 반환
		}

		/// <summary>
		/// XAML에서 특정 DependencyObject에 BehaviorCollection을 설정합니다.
		/// </summary>
		/// <param name="obj">BehaviorCollection을 설정할 대상 객체</param>
		/// <param name="value">설정할 BehaviorCollection</param>
		public static void SetBehaviors(DependencyObject obj, BehaviorCollection value)
		{
			// XAML 객체에 BehaviorCollection을 설정
			obj.SetValue(BehaviorsProperty, value);
		}

		/// <summary>
		/// BehaviorCollection이 변경될 때마다 호출되는 콜백 함수입니다.
		/// - BehaviorCollection이 변경되면, 해당 컬렉션에 대해 Attach 또는 Detach를 처리합니다.
		/// </summary>
		/// <param name="obj">변경된 객체</param>
		/// <param name="args">속성 변경 인자</param>
		private static void OnBehaviorsChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			// 이전과 새로운 BehaviorCollection을 가져옵니다.
			BehaviorCollection oldCollection = (BehaviorCollection)args.OldValue;
			BehaviorCollection newCollection = (BehaviorCollection)args.NewValue;

			// 컬렉션이 다르면 Detach 및 Attach 작업을 처리합니다.
			if (oldCollection != newCollection)
			{
				// 이전 컬렉션이 있으면, 그 컬렉션에서 Detach 처리
				if (oldCollection != null && ((IAttachedObject)oldCollection).AssociatedObject != null)
				{
					oldCollection.Detach(); // 이전 BehaviorCollection에서 Detach 호출
				}

				// 새로운 컬렉션이 있으면, 그 컬렉션에 Attach 처리
				if (newCollection != null && obj != null)
				{
					// 이미 AssociatedObject가 있으면, 추가적으로 처리할 필요가 있습니다.
					if (((IAttachedObject)newCollection).AssociatedObject != null)
					{
						// 예외처리: 동일 객체에 여러 번 Behaviors를 추가하려고 할 때 발생
						// throw new InvalidOperationException("Cannot attach multiple behaviors to the same object.");
					}

					// 새로운 BehaviorCollection에 Attach 호출
					newCollection.Attach(obj);
				}
			}
		}
	}
}
