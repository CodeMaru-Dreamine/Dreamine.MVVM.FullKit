using System.Windows.Controls;

namespace SampleCrossUi.Wpf.Views;

/// <summary>
/// \if KO
/// <para>Light Bulb View 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates light bulb view functionality and related state.</para>
/// \endif
/// </summary>
public partial class LightBulbView : UserControl
{
    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="LightBulbView"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="LightBulbView"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    public LightBulbView()
    {
        InitializeComponent();
    }
}
