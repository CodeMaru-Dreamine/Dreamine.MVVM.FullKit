using System;

namespace Dreamine.MVVM.Attributes
{
	/// <summary>
	/// 이 속성이 붙은 메서드는 ICommand로 자동 변환됩니다.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method)]
	public sealed class RelayCommandAttribute : Attribute
	{
		/// <summary>
		/// 생성할 커맨드 속성 이름입니다. 비워두면 메서드명 + "Command"로 자동 생성됩니다.
		/// </summary>
		public string CommandName { get; }

		/// <summary>
		/// RelayCommandAttribute 생성자
		/// </summary>
		/// <param name="commandName">명시적 커맨드 속성 이름</param>
		public RelayCommandAttribute(string commandName = null)
		{
			CommandName = commandName;
		}
	}
}
