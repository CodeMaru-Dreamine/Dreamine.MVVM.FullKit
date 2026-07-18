using System.Windows.Controls;
using Dreamine.MVVM.Core;

namespace SampleSmart.Pages.PageSub.CommunicationTabs;

/// <summary>
/// \if KO
/// <para>\brief InMemory Communication 샘플 탭입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates in memory communication tab functionality and related state.</para>
/// \endif
/// </summary>
public partial class InMemoryCommunicationTab : UserControl
{
    /// <summary>
    /// \if KO
    /// <para>\brief InMemoryCommunicationTab 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="InMemoryCommunicationTab"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    public InMemoryCommunicationTab()
    {
        InitializeComponent();

        if (DataContext is null)
        {
            DataContext = DMContainer.Resolve<InMemoryCommunicationTabViewModel>();
        }
    }
}
