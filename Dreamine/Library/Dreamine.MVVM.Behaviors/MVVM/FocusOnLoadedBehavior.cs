using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Dreamine.MVVM.Behaviors.Core.Base;

namespace Dreamine.MVVM.Behaviors.MVVM
{
	/// <summary>
	/// 📌 컨트롤이 로드되면 자동으로 포커스를 설정하는 Behavior입니다.
	/// 
	/// 주로 로그인 화면, 검색창, 입력 폼 등에서 첫 포커스 입력 요소에 사용되며,
	/// MVVM 구조를 해치지 않고 View에서 손쉽게 설정 가능합니다.
	/// </summary>
	public class FocusOnLoadedBehavior : Behavior<FrameworkElement>
	{
		/// <summary>
		/// 포커스 활성화 여부를 나타내는 의존 속성입니다.
		/// </summary>
		public static readonly DependencyProperty IsEnabledProperty =
			DependencyProperty.RegisterAttached(
				"IsEnabled",
				typeof(bool),
				typeof(FocusOnLoadedBehavior),
				new PropertyMetadata(false, OnIsEnabledChanged));

		/// <summary>
		/// Behavior의 속성에서 포커스 활성화 여부를 가져옵니다.
		/// </summary>
		public static bool GetIsEnabled(DependencyObject obj)
		{
			return (bool)obj.GetValue(IsEnabledProperty);
		}

		/// <summary>
		/// Behavior의 속성에서 포커스 활성화 여부를 설정합니다.
		/// </summary>
		public static void SetIsEnabled(DependencyObject obj, bool value)
		{
			obj.SetValue(IsEnabledProperty, value);
		}

		/// <summary>
		/// IsEnabled 값이 변경되었을 때의 처리입니다.
		/// 로드 완료 시점에 포커스를 설정합니다.
		/// </summary>
		private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is FrameworkElement element && (bool)e.NewValue)
			{
				// Loaded 이벤트가 중복 연결되지 않도록 방지
				element.Loaded -= OnElementLoaded;
				element.Loaded += OnElementLoaded;
			}
		}

		/// <summary>
		/// 컨트롤이 로드되면 포커스를 자동으로 설정합니다.
		/// </summary>
		private static void OnElementLoaded(object sender, RoutedEventArgs e)
		{
			if (sender is FrameworkElement element)
			{
				element.Loaded -= OnElementLoaded;

				// 포커스를 이동할 수 있는 경우에만 처리
				if (element.Focusable && element.IsEnabled && element.Visibility == Visibility.Visible)
				{
					element.Focus();
					Keyboard.Focus(element);
				}
			}
		}

		protected override void OnAttached()
		{
			AssociatedObject.Loaded += (s, e) =>
			{
				AssociatedObject.Focus();
			};
		}
	}
}
