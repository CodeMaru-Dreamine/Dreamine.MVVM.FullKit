using System.Windows.Controls;
using Dreamine.MVVM.Core;

namespace SampleSmart.Pages.PageSub.CommunicationTabs;

/// <summary>
/// \brief InMemory Communication 샘플 탭입니다.
/// </summary>
public partial class InMemoryCommunicationTab : UserControl
{
    /// <summary>
    /// \brief InMemoryCommunicationTab 클래스의 새 인스턴스를 초기화합니다.
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
