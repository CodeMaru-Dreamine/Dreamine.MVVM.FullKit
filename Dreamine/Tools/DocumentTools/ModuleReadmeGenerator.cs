using DocumentTools.Models;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Dreamine.Tools.DocumentTools
{
	/// <summary>
	/// 📄 Dreamine 프로젝트의 모듈별 클래스명.md를 자동 생성하는 도구입니다.
	/// 
	/// Library 내의 .cs 파일들을 기준으로, 대응하는 문서(.md)가 존재하는지 확인하고,
	/// ZZX.Document 내 Modules 폴더 하위에 누락된 클래스명.md 파일을 생성합니다.
	/// </summary>
	public class ModuleReadmeGenerator
	{
		/// <summary>
		/// 전체 모듈에 대한 누락된 클래스 문서를 생성합니다.
		/// </summary>
		/// <param name="codeBase">.cs 파일 기준 루트 디렉터리 (생략 시 상대 경로 자동 적용)</param>
		/// <param name="docBase">.md 파일 생성 루트 디렉터리 (생략 시 상대 경로 자동 적용)</param>
		public void GenerateAll(string codeBase = "", string docBase = "")
		{
			// 📌 기본 경로 설정
			if (string.IsNullOrWhiteSpace(codeBase))
				codeBase = @"..\..\Library";

			if (string.IsNullOrWhiteSpace(docBase))
				docBase = @"..\..\..\Document\Modules";

			// 📌 경로 정규화 및 bin/Tools 폴더 영향 제거
			codeBase = Path.GetFullPath(codeBase).Replace(@"\Tools\DocumentTools\bin", "");
			docBase = Path.GetFullPath(docBase).Replace(@"\Dreamine\Tools\DocumentTools", "");

			// 🔍 모듈별 탐색
			var moduleNames = Directory.GetDirectories(codeBase)
				.Select(Path.GetFileName)
				.Where(name => !string.IsNullOrWhiteSpace(name))
				.ToList();

			foreach (var moduleName in moduleNames)
			{
				var csPath = Path.Combine(codeBase, moduleName);   // 실제 코드 경로
				var mdPath = Path.Combine(docBase, moduleName);    // 문서 저장 경로

				if (!Directory.Exists(mdPath))
					Directory.CreateDirectory(mdPath);

				GenerateMissingClassDocs(csPath, mdPath, moduleName);
			}
		}

		/// <summary>
		/// 단일 모듈에 대해 누락된 클래스명.md 파일을 생성합니다.
		/// 하위 폴더 구조를 보존하여 Markdown 문서를 생성합니다.
		/// </summary>
		private void GenerateMissingClassDocs(string csPath, string mdPath, string moduleName)
		{
			// ❗ 제외할 자동 생성 파일 패턴
			var ignorePatterns = new[]
			{
		".g.cs", ".designer.cs", "AssemblyAttributes",
		"AssemblyInfo", ".Generated.", "TemporaryGeneratedFile"
	};

			// 📌 모든 .cs 파일 경로 (하위 포함)
			var csFilePaths = Directory.GetFiles(csPath, "*.cs", SearchOption.AllDirectories)
				.Where(f => !ignorePatterns.Any(p => f.Contains(p, StringComparison.OrdinalIgnoreCase)))
				.ToList();

			// 📌 이미 생성된 .md 파일 이름 목록
			var mdFiles = Directory.GetFiles(mdPath, "*.md", SearchOption.AllDirectories)
				.Select(f => Path.GetFileNameWithoutExtension(f))
				.Where(f => f != "README")
				.Distinct()
				.ToHashSet();

			int createdCount = 0;

			foreach (var fullCsPath in csFilePaths)
			{
				var className = Path.GetFileNameWithoutExtension(fullCsPath);
				if (mdFiles.Contains(className)) continue;

				// 📂 상대 경로 계산
				var relativeDir = Path.GetDirectoryName(fullCsPath)!.Substring(csPath.Length).TrimStart(Path.DirectorySeparatorChar);
				var mdFolderPath = Path.Combine(mdPath, relativeDir);
				var mdFilePath = Path.Combine(mdFolderPath, $"{className}.md");

				Directory.CreateDirectory(mdFolderPath);

				var content = GenerateClassDocContent(className, moduleName, fullCsPath);
				File.WriteAllText(mdFilePath, content, Encoding.UTF8);

				Console.WriteLine($"📄 {moduleName}/{relativeDir}/{className}.md 생성됨");
				createdCount++;
			}

			if (createdCount == 0)
				Console.WriteLine($"✅ {moduleName} - 모든 클래스 문서가 존재함");
		}


		/// <summary>
		/// 클래스 문서용 템플릿 콘텐츠 생성
		/// </summary>
		private string GenerateClassDocContent(string className, string moduleName, string fullCsPath)
		{
			var sb = new StringBuilder();

			sb.AppendLine("---");
			sb.AppendLine($"title: {className}");
			sb.AppendLine($"module: {moduleName}");
			sb.AppendLine($"generated: true");
			sb.AppendLine($"date: {DateTime.Now:yyyy-MM-dd}");
			sb.AppendLine("---");
			sb.AppendLine();

			sb.AppendLine($"# 🧾 {className}.cs");
			sb.AppendLine();
			sb.AppendLine("## 📌 개요");
			sb.AppendLine($"Dreamine `{moduleName}` 모듈의 `{className}.cs` 클래스에 대한 문서입니다.");
			sb.AppendLine("이 문서는 자동 생성되었으며, 향후 직접 내용을 보완할 수 있습니다.");
			sb.AppendLine();

			sb.AppendLine("## 📂 파일 경로");
			sb.AppendLine($"{moduleName}/{className}.cs");
			sb.AppendLine();

			var summary = ExtractSummary(fullCsPath);
			sb.AppendLine("## 🧠 주요 기능");
			sb.AppendLine($"{summary}");
			sb.AppendLine();

			sb.AppendLine("## 💡 사용 예시");
			sb.AppendLine("```csharp");
			sb.AppendLine("// 예시 코드 삽입 예정");
			sb.AppendLine("```");
			sb.AppendLine();

			var members = ExtractMembersSimple(fullCsPath);
			string structureSection = GenerateInternalStructureTable(members);
			sb.AppendLine("## 🛠️ 내부 구조");
			sb.AppendLine($"{structureSection}");
			sb.AppendLine();

			sb.AppendLine("## 🔒 제약 사항");
			sb.AppendLine("- 아직 정의되지 않음");
			sb.AppendLine();

			sb.AppendLine("## 🧩 관련 모듈");
			sb.AppendLine($"- {moduleName}");
			sb.AppendLine();

			var ver = GenerateVersionHistorySection(className, fullCsPath);
			sb.AppendLine("## 🗂️ 버전 관리");
			sb.AppendLine($"{ver}");
			sb.AppendLine();

			sb.AppendLine("## 📁 소속 모듈");
			sb.AppendLine(moduleName);
			sb.AppendLine();
		
			sb.Append(GenerateFooterSection());

			return sb.ToString();
		}

		/// <summary>
		/// 문서 하단부 서명 및 자유 영역 템플릿을 생성합니다.
		/// </summary>
		/// <returns>하단 마크다운 문자열</returns>
		private string GenerateFooterSection()
		{
			var sb = new StringBuilder();

			sb.AppendLine("---");
			sb.AppendLine();
			sb.AppendLine("## 🖋️ 기록 정보");
			sb.AppendLine();
			sb.AppendLine("| 항목       | 내용                             |");
			sb.AppendLine("|------------|----------------------------------|");
			sb.AppendLine("| ✍️ 작성자  | 아키로그 드림                    |");
			sb.AppendLine("| 🤖 협력자  | ChatGPT (프레임워크 유혹자)       |");
			sb.AppendLine($"| 📅 생성일  | {DateTime.Now:yyyy-MM-dd} (자동 생성됨) |");
			sb.AppendLine("| 🛠️ 생성도구 | Dreamine 문서화 자동화 도구         |");
			sb.AppendLine();
			sb.AppendLine("---");
			sb.AppendLine();
			sb.AppendLine("## ⛏️ 자유 작성 영역");
			sb.AppendLine();
			sb.AppendLine("- [ ] 설명 추가 또는 TODO 항목 작성");
			sb.AppendLine("- [ ] 특이점, 예외 상황, 사용자 주석 등 기술 메모 작성 가능");
			sb.AppendLine("- [ ] 이 영역은 자동 생성 도구에 의해 변경되지 않습니다.");
			sb.AppendLine("```yaml");
			sb.AppendLine("TODO:");
			sb.AppendLine("  - 여기에 설명 또는 작업 내용을 작성하세요");
			sb.AppendLine("```");

			return sb.ToString();
		}


		private string GenerateVersionHistorySection(string className, string filePath)
		{
			var date = File.GetLastWriteTime(filePath).ToString("yyyy-MM-dd");

			var sb = new StringBuilder();		
			sb.AppendLine("| 버전 | 변경 내용 | 날짜 |");
			sb.AppendLine("|------|-----------|------|");
			sb.AppendLine($"| v1.0 | {className}.cs 문서 자동 생성 | {date} |");
			sb.AppendLine();

			return sb.ToString();
		}

		private string ExtractSummary(string filePath)
		{
			var lines = File.ReadAllLines(filePath);
			var summaryLines = lines
				.SkipWhile(l => !l.Trim().StartsWith("/// <summary>"))
				.Skip(1)
				.TakeWhile(l => !l.Trim().StartsWith("/// </summary>"))
				.Select(l => l.Trim().Replace("///", "").Trim().TrimStart('-', ' ').Trim())
				.ToList();

			// 🔽 각 줄마다 줄바꿈 포함
			return summaryLines.Any()
					? "- " + string.Join(Environment.NewLine + "- ", summaryLines)
					: "- 아직 명세되지 않음";

		}

		private List<MemberInfo> ExtractMembersWithComments(string filePath)
		{
			var lines = File.ReadAllLines(filePath);
			var members = new List<MemberInfo>();
			string lastComment = "- 설명 없음";
			bool justPassedClassDeclaration = false;

			for (int i = 0; i < lines.Length; i++)
			{
				string line = lines[i].Trim();

				// 🔹 XML 주석 누적 (단 <summary> 등은 무시)
				if (line.StartsWith("///"))
				{
					var content = line.Replace("///", "").Trim();
					if (!Regex.IsMatch(content, @"^<\/?\w+")) // <summary>, <param> 등 무시
					{
						if (lastComment == "- 설명 없음")
							lastComment = content;
						else
							lastComment += " " + content;
					}
					continue;
				}

				// 🔹 클래스/인터페이스 정의는 스킵 + 플래그 설정
				if (Regex.IsMatch(line, @"^(public|internal|protected|private)?\s*(sealed\s+)?(class|interface|enum|struct)\b"))
				{
					lastComment = "- 설명 없음";
					justPassedClassDeclaration = true;
					continue;
				}

				// 🔹 무시할 코드 패턴들
				if (
					string.IsNullOrWhiteSpace(line) ||
					line.StartsWith("using") ||
					line.StartsWith("}") ||
					line.Trim() == ";" ||
					line.Contains(" +=") || line.Contains(" -=") ||
					Regex.IsMatch(line, @"^\b(if|else|switch|for|foreach|while|try|catch|throw|return)\b")
				)
					continue;

				// 🔹 라인에서 주석 제거
				var cleanLine = line.Split(new[] { "//" }, StringSplitOptions.None)[0].Trim();

				// 🔹 메서드 or 생성자
				if (cleanLine.Contains("(") && cleanLine.Contains(")"))
				{
					if (justPassedClassDeclaration)
					{
						lastComment = "- 설명 없음";
						justPassedClassDeclaration = false;
					}

					var match = Regex.Match(cleanLine, @"^(public|private|protected|internal)(\s+static)?\s+([\w<>\[\]?]+)\s+(\w+)\s*\(");
					if (match.Success)
					{
						string access = match.Groups[1].Value;
						string type = match.Groups[3].Value;
						string name = match.Groups[4].Value;

						members.Add(new MemberInfo
						{
							Name = name + "()",
							Access = cleanLine.Contains("static") ? $"{access} static" : access,
							Type = type,
							Description = lastComment
						});
						lastComment = "- 설명 없음";
					}
				}
				// 🔹 표현식 기반 프로퍼티 (public Type Name => ...)
				else if (cleanLine.Contains("=>"))
				{
					if (justPassedClassDeclaration)
					{
						lastComment = "- 설명 없음";
						justPassedClassDeclaration = false;
					}
					if (lastComment == "- 설명 없음")
					{
						var inlineComment = Regex.Match(line, @"//\s*(.+)$");
						if (inlineComment.Success)
						{
							lastComment = inlineComment.Groups[1].Value.Trim();
						}
					}

					var match = Regex.Match(cleanLine, @"^(public|private|protected|internal)(\s+static)?\s+([\w<>\[\]?]+)\s+(\w+)\s*=>");
					if (match.Success)
					{
						string access = match.Groups[1].Value;
						string type = match.Groups[3].Value;
						string name = match.Groups[4].Value;

						members.Add(new MemberInfo
						{
							Name = name,
							Access = cleanLine.Contains("static") ? $"{access} static" : access,
							Type = type,
							Description = lastComment
						});
						lastComment = "- 설명 없음";
					}
				}
				// 🔹 필드 or 세미콜론 기반 프로퍼티
				else if (cleanLine.EndsWith(";"))
				{
					if (justPassedClassDeclaration)
					{
						lastComment = "- 설명 없음";
						justPassedClassDeclaration = false;
					}
					
					if (lastComment == "- 설명 없음")
					{
						var inlineComment = Regex.Match(line, @"//\s*(.+)$");
						if (inlineComment.Success)
						{
							lastComment = inlineComment.Groups[1].Value.Trim();
						}
					}

					var match = Regex.Match(cleanLine, @"^(public|private|protected|internal)(\s+static)?\s+([\w<>\[\]?]+)\s+([_\w]+);");
					if (match.Success)
					{
						string access = match.Groups[1].Value;
						string type = match.Groups[3].Value;
						string name = match.Groups[4].Value;

						// get, set 등은 제외
						if (name is "get" or "set" or "{" or "}" or "=")
							continue;

						members.Add(new MemberInfo
						{
							Name = name,
							Access = cleanLine.Contains("static") ? $"{access} static" : access,
							Type = type,
							Description = lastComment
						});
						lastComment = "- 설명 없음";
					}
				}
			}

			return members;
		}

		private List<MemberInfo> ExtractMembersSimple(string filePath)
		{
			var lines = File.ReadAllLines(filePath);
			var members = new List<MemberInfo>();

			foreach (var rawLine in lines)
			{
				string line = rawLine.Trim();

				// 무시할 줄
				if (string.IsNullOrWhiteSpace(line) ||
					line.StartsWith("using") ||
					line.StartsWith("namespace") ||
					line.StartsWith("//") ||
					line.StartsWith("///") ||
					line.StartsWith("}") ||
					line.Contains(" +=") || line.Contains(" -=") ||
					line.Contains("partial ") || line.Contains("record ") ||
					line.Contains("class") || line.Contains("interface"))
					continue;

				// 메서드/생성자
				var methodMatch = Regex.Match(line, @"^(public|private|protected|internal)(\s+(static|virtual|override))*\s+([\w<>\[\]?]+)\s+(\w+)\s*\(");
				if (methodMatch.Success)
				{
					string access = methodMatch.Groups[1].Value;
					string type = methodMatch.Groups[4].Value;
					string name = methodMatch.Groups[5].Value;

					members.Add(new MemberInfo
					{
						Name = name + "()",
						Access = line.Contains("static") ? $"{access} static" : access,
						Type = type,
						Description = "(TODO)"
					});
					continue;
				}

				// 람다 프로퍼티
				var lambdaProp = Regex.Match(line, @"^(public|private|protected|internal)(\s+(static|virtual|override))*\s+([\w<>\[\]?]+)\s+(\w+)\s*=>");
				if (lambdaProp.Success)
				{
					string access = lambdaProp.Groups[1].Value;
					string type = lambdaProp.Groups[4].Value;
					string name = lambdaProp.Groups[5].Value;

					members.Add(new MemberInfo
					{
						Name = name,
						Access = line.Contains("static") ? $"{access} static" : access,
						Type = type,
						Description = "(TODO)"
					});
					continue;
				}

				// 필드 or auto property
				var fieldMatch = Regex.Match(line, @"^(public|private|protected|internal)(\s+(static|readonly))*\s+([\w<>\[\]?]+)\s+([_\w]+);");
				if (fieldMatch.Success)
				{
					string access = fieldMatch.Groups[1].Value;
					string type = fieldMatch.Groups[4].Value;
					string name = fieldMatch.Groups[5].Value;

					members.Add(new MemberInfo
					{
						Name = name,
						Access = line.Contains("static") ? $"{access} static" : access,
						Type = type,
						Description = "(TODO)"
					});
				}
			}

			return members;
		}



		private string GenerateInternalStructureTable(List<MemberInfo> members)
		{
			var sb = new StringBuilder();
			
			sb.AppendLine("| 멤버 이름 | 접근 수준 | 타입 | 설명 |");
			sb.AppendLine("| -------- | -------- | ---- | ---- |");

			foreach (var m in members)
			{
				sb.AppendLine($"| `{m.Name}` | `{m.Access}` | `{m.Type}` | {m.Description} |");
			}

			sb.AppendLine();
			return sb.ToString();
		}		
	}
}