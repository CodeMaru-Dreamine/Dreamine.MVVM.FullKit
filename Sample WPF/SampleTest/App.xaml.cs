using Dreamine.MVVM.Attributes;
using System.Windows;

namespace SampleTest
{
	/// <summary>
	/// 📌 Dreamine 애플리케이션의 진입점 클래스를 지정하는 전용 Attribute입니다.
	/// 
	/// 이 Attribute는 반드시 <c>App.xaml.cs</c>의 <c>partial class App</c>에만 사용되어야 하며,  
	/// <c>Dreamine CLI</c> 또는 자동화 도구들이 해당 클래스를 애플리케이션의 **시작점**으로 인식하는 데 사용됩니다.
	/// 
	/// ❗ 다른 클래스에 부착 시 예외가 발생하거나 예기치 않은 동작이 발생할 수 있습니다.
	/// </summary>
	/// <remarks>
	/// - Dreamine CLI 또는 템플릿 엔진에서 해당 클래스를 기준으로 종속 구조를 자동 분석합니다. <br/>
	/// - 필수 조건: WPF <c>App.xaml</c>과 연결된 <c>partial class App : Application</c>에만 부착
	/// </remarks>
	/// <example>
	/// <code>
	/// [DreamineEntry]
	/// public partial class App : Application
	/// {
	///     // WPF 시작점 클래스
	/// }
	/// </code>
	/// </example>
	[DreamineEntry]
	public partial class App : Application
	{
	}
}
