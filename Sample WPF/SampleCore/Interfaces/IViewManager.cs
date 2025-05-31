using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleCore.Interfaces
{
	public interface IViewManager
	{
		void Show<TViewModel>() where TViewModel : class;
	}

}
