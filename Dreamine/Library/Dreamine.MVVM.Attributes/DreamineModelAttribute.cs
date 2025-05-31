using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dreamine.MVVM.Attributes
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Field)]
	public sealed class DreamineModelAttribute : Attribute 
	{
		/// <summary>
		/// 생성될 속성 이름입니다. 지정하지 않으면 필드명 기반으로 자동 생성됩니다.
		/// </summary>
		public string PropertyName { get; }

		/// <summary>
		/// DreaminePropertyAttribute 생성자
		/// </summary>
		/// <param name="propertyName">명시적 속성 이름</param>
		public DreamineModelAttribute(string propertyName = null)
		{
			PropertyName = propertyName;
		}
	}
}
