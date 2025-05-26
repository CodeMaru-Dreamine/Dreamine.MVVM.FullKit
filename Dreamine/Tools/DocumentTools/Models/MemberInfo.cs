using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentTools.Models
{
	public class MemberInfo
	{
		public string Name { get; set; }           // 예: PropertyName
		public string Access { get; set; }         // 예: public
		public string Type { get; set; }           // 예: string
		public string Description { get; set; }    // 예: 생성될 속성 이름
	}
}
