using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Dreamine.MVVM.Behaviors.Core.Base;

namespace Dreamine.MVVM.Behaviors.MVVM
{
	/// <summary>
	/// 사용자가 Enter 키를 누를 때 지정된 ICommand를 실행하는 Behavior입니다.
	/// MVVM 구조에서 ViewModel의 명령(예: 로그인, 검색 실행 등)
	/// TextBox 또는 Input 계열 컨트롤에서 직접 트리거할 수 있도록 연결합니다.
	/// </summary>
	public class EnterKeyCommandBehavior : Behavior<UIElement>
	{
		/// <summary>
		/// 실행할 커맨드를 지정하는 의존 속성입니다.
		/// </summary>
		public static readonly DependencyProperty CommandProperty =
			DependencyProperty.RegisterAttached(
				"Command",
				typeof(ICommand),
				typeof(EnterKeyCommandBehavior),
				new PropertyMetadata(null, OnCommandChanged));

		/// <summary>
		/// 커맨드를 가져옵니다.
		/// </summary>
		public static ICommand? GetCommand(DependencyObject obj)
		{
			return (ICommand?)obj.GetValue(CommandProperty);
		}

		/// <summary>
		/// 커맨드를 설정합니다.
		/// </summary>
		public static void SetCommand(DependencyObject obj, ICommand? value)
		{
			obj.SetValue(CommandProperty, value);
		}

		/// <summary>
		/// 커맨드 속성이 변경되면 KeyDown 이벤트를 연결합니다.
		/// </summary>
		private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is UIElement element)
			{
				element.KeyDown -= OnKeyDown;
				element.KeyDown += OnKeyDown;
			}
		}

		/// <summary>
		/// Enter 키가 눌릴 경우 Command를 실행합니다.
		/// </summary>
		private static void OnKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter && sender is DependencyObject obj)
			{
				ICommand? command = GetCommand(obj);
				if (command?.CanExecute(null) == true)
				{
					command.Execute(null);
					e.Handled = true;
				}
			}
		}
	}
}
