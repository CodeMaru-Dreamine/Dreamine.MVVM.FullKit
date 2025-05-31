using Dreamine.MVVM.Behaviors.Core.Base;
using System.Windows.Input;
using System.Windows;

namespace Dreamine.MVVM.Behaviors.Wpf.Interactivity
{
	/// <summary>
	/// 📌 WPF Window를 마우스로 드래그하여 이동 가능하게 만드는 Behavior입니다.
	/// 
	/// Dreamine에서는 View의 시각적 행동을 확장하는 유틸성 모듈로 이 클래스를 구성하며,
	/// MVVM 구조에서 코드비하인드 없이 사용자 상호작용을 구현하기 위해 사용됩니다.
	/// </summary>
	public class WindowDragBehavior : Behavior<FrameworkElement>
	{
		/// <summary>
		/// Behavior가 Attach될 때 호출됩니다.
		/// - `MouseLeftButtonDown` 이벤트를 `AssociatedObject`에 연결하여 드래그 기능을 활성화합니다.
		/// </summary>
		protected override void OnAttached()
		{
			base.OnAttached();  // 기본 동작 실행
								// AssociatedObject (이 경우 FrameworkElement)에 MouseLeftButtonDown 이벤트를 추가합니다.
			AssociatedObject.MouseLeftButtonDown += OnMouseLeftButtonDown;
		}

		/// <summary>
		/// Behavior가 Detach될 때 호출됩니다.
		/// - `MouseLeftButtonDown` 이벤트를 해제하여 드래그 기능을 비활성화합니다.
		/// </summary>
		protected override void OnDetaching()
		{
			base.OnDetaching();  // 기본 동작 실행
								 // AssociatedObject에서 MouseLeftButtonDown 이벤트를 제거합니다.
			AssociatedObject.MouseLeftButtonDown -= OnMouseLeftButtonDown;
		}

		/// <summary>
		/// 마우스 왼쪽 버튼이 눌렸을 때 발생하는 이벤트입니다.
		/// - 해당 이벤트에서 `Window`를 찾고, `DragMove`를 호출하여 창을 드래그 가능합니다.
		/// </summary>
		private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			// 마우스 버튼이 눌렸을 때만 동작
			if (e.ButtonState == MouseButtonState.Pressed)
			{
				// `AssociatedObject` (현재 프레임워크 요소)에서 `Window`를 가져옵니다.
				Window window = Window.GetWindow(AssociatedObject);

				// `window`가 null이 아니면 `DragMove` 메서드를 호출하여 창을 드래그합니다.
				window?.DragMove();
			}
		}
	}
}
