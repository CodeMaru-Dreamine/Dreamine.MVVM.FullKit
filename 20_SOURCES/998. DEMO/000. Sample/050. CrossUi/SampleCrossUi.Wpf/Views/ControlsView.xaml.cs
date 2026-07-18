using System.Windows.Controls;

namespace SampleCrossUi.Wpf.Views;

/// <summary>
/// \if KO
/// <para>Controls View 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates controls view functionality and related state.</para>
/// \endif
/// </summary>
public partial class ControlsView : UserControl
{
    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="ControlsView"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="ControlsView"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    public ControlsView()
    {
        InitializeComponent();
    }
}
