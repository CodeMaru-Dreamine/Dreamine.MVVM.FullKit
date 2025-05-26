using Dreamine.Tools.MVVM.CLI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dreamine.Tools.MVVM.CLI.Services
{
	/// <summary>
	/// 📌 "new view &lt;Name&gt;" 명령을 처리하는 핸들러 클래스입니다.
	/// 
	/// 지정된 이름을 기반으로 View(XAML), ViewModel, CodeBehind(.xaml.cs)를
	/// 템플릿에서 복사하여 현재 작업 디렉터리 하위에 자동 생성합니다.
	/// </summary>
	public class ViewCommandHandler : ICommandHandler
	{
		/// <summary>
		/// View + ViewModel + XAML.cs 파일을 생성하는 명령을 실행합니다.
		/// </summary>
		/// <param name="name">생성할 View의 이름 (예: "Main" → Main.xaml, MainViewModel.cs 등 생성)</param>
		public void Execute(string name)
		{
			// 📌 템플릿 폴더 경로 (CLI 실행 경로 기준)
			string templatePath = Path.Combine(AppContext.BaseDirectory, "Templates");

			// 📌 실제 파일 생성 위치 (현재 CLI 실행 위치 기준)
			string currentPath = Environment.CurrentDirectory;
			string viewFolder = Path.Combine(currentPath, "Views");
			string vmFolder = Path.Combine(currentPath, "ViewModels");

			// 📌 템플릿 파일 경로 정의
			string viewTemplatePath = Path.Combine(templatePath, "View.template.xaml.tmpl");
			string xamlCsTemplatePath = Path.Combine(templatePath, "View.template.xaml.cs.tmpl");
			string vmTemplatePath = Path.Combine(templatePath, "ViewModel.template.tmpl");

			// ❌ 템플릿 파일이 누락된 경우 경고 출력 후 종료
			if (!File.Exists(viewTemplatePath) || !File.Exists(vmTemplatePath) || !File.Exists(xamlCsTemplatePath))
			{
				return;
			}

			// 📁 출력 디렉터리 생성
			Directory.CreateDirectory(viewFolder);
			Directory.CreateDirectory(vmFolder);

			// 📄 출력 파일 경로 설정
			string viewPath = Path.Combine(viewFolder, $"{name}.xaml");
			string xamlCsPath = Path.Combine(viewFolder, $"{name}.xaml.cs");
			string vmPath = Path.Combine(vmFolder, $"{name}ViewModel.cs");

			// 📄 파일 생성 수행
			var generator = new ViewModelGenerator();
			generator.GenerateFile(viewTemplatePath, viewPath, name);
			generator.GenerateFile(xamlCsTemplatePath, xamlCsPath, name);
			generator.GenerateFile(vmTemplatePath, vmPath, name);

			// 🧹 기존 루트 경로에 남아있던 중복 파일 정리
			TryDelete(Path.Combine(currentPath, $"{name}.xaml"));
			TryDelete(Path.Combine(currentPath, $"{name}.xaml.cs"));
		}

		/// <summary>
		/// 기존 파일이 존재하면 삭제합니다.
		/// </summary>
		/// <param name="path">삭제할 파일 경로</param>
		private void TryDelete(string path)
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
	}
}
