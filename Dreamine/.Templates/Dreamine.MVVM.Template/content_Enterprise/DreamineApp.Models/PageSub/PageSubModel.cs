using Dreamine.MVVM.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DreamineApp.Models.PageSub
{
	public partial class PageSubModel
	{
		[DreamineProperty]
		private string _message = "이것은 Sub View Sample 입니다.";
	}
}
