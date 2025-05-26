using Dreamine.Tools.MVVM.CLI.Interfaces;
using Dreamine.Tools.MVVM.CLI.Services;

namespace Dreamine.Tools.MVVM.CLI
{
	/// <summary>
	/// 📌 Dreamine MVVM CLI의 진입점 클래스입니다.
	/// 
	/// 본 클래스는 명령줄 인자를 파싱하여, MVVM 구조의 View 자동 생성 명령을 처리합니다.
	/// 사용법: <c>new view &lt;Name&gt;</c>
	/// </summary>
	class Program
	{
		/// <summary>
		/// 애플리케이션 실행 진입점입니다.
		/// <para>
		/// Dreamine CLI의 명령을 파싱하고, 지정된 명령 처리기로 위임합니다.
		/// </para>
		/// </summary>
		/// <param name="args">명령줄 인자 (예: new view MainPage)</param>
		static void Main(string[] args)
		{
			// 명령어 검증
			if (args.Length < 3 || args[0] != "new" || args[1] != "view")
			{
				Console.WriteLine("Dreamine MVVM CLI 사용법: new view <Name>");
				return;
			}

			/// <summary>
			/// 사용자가 생성할 View 이름 (예: MainPage)
			/// </summary>
			string name = args[2];

			/// <summary>
			/// View 생성 명령을 처리할 핸들러입니다.
			/// </summary>
			ICommandHandler handler = new ViewCommandHandler();

			/// <summary>
			/// 핸들러를 통해 View 생성 명령 실행
			/// </summary>
			handler.Execute(name);
		}
	}
}