using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dreamine.Tools.MVVM.CLI.Services
{
	/// <summary>
	/// 템플릿 파일을 읽어 실제 파일로 생성하는 클래스입니다.
	/// </summary>
	public class ViewModelGenerator
	{
		/// <summary>
		/// 템플릿 파일을 읽어 치환 후 출력 파일로 저장합니다.
		/// </summary>
		/// <param name="templatePath">템플릿 경로</param>
		/// <param name="outputPath">출력 경로</param>
		/// <param name="name">치환할 View/ViewModel 이름</param>
		/// <param name="ns">네임스페이스 (기본: ViewModels, Views)</param>
		public void GenerateFile(string templatePath, string outputPath, string name, string ns = "Sample002")
		{
			string template = File.ReadAllText(templatePath);
			string result = template
				.Replace("{{name}}", name)
				.Replace("{{namespace}}", ns);

			File.WriteAllText(outputPath, result);
		}
	}
}
