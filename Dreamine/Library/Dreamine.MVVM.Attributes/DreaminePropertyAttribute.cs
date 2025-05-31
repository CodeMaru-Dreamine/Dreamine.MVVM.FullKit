using System;

namespace Dreamine.MVVM.Attributes
{
	/// <summary>
	/// 이 속성이 붙은 필드는 자동으로 INotifyPropertyChanged 속성으로 변환됩니다.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
	public sealed class DreaminePropertyAttribute : Attribute
	{		
		/// <summary>
		/// 생성될 속성 이름입니다. 지정하지 않으면 필드명 기반으로 자동 생성됩니다.
		/// </summary>
		public string PropertyName { get; }

		/// <summary>
		/// DreaminePropertyAttribute 생성자
		/// </summary>
		/// <param name="propertyName">명시적 속성 이름</param>
		public DreaminePropertyAttribute(string propertyName = null)
		{
			PropertyName = propertyName;
		}
	}
}
