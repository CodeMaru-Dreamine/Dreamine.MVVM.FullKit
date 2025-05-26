using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleTest.Interfaces
{
	public interface IViewManager
	{
		void Show<TViewModel>() where TViewModel : class;
	}

}
