using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dreamine.Tools.MVVM.CLI.Interfaces
{
	/// <summary>
	/// Dreamine CLI 명령 처리기 인터페이스입니다.
	/// </summary>
	public interface ICommandHandler
	{
		/// <summary>
		/// 명령어를 실행합니다.
		/// </summary>
		/// <param name="name">생성할 View 또는 ViewModel 이름</param>
		void Execute(string name);
	}
}
