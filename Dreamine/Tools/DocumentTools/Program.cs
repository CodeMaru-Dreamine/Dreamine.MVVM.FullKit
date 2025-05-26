using System;

namespace Dreamine.Tools.DocumentTools
{
	internal class Program
	{
		static void Main(string[] args)
		{
			var generator = new ModuleReadmeGenerator();
			generator.GenerateAll();
			Console.WriteLine("🎉 모든 클래스.md 생성 완료!");
		}
	}
}
