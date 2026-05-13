using System.Windows.Controls;
using Dreamine.MVVM.Core;

namespace SampleSmart.Pages.PageSub.CommunicationTabs.Tcp;

/// <summary>
/// \brief TCP Server 테스트 View입니다.
/// </summary>
public partial class TcpServerTestView : UserControl
{
    /// <summary>
    /// \brief TcpServerTestView 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    public TcpServerTestView()
    {
        InitializeComponent();

        if (DataContext is null)
        {
            DataContext = DMContainer.Resolve<TcpServerTestViewModel>();
        }
    }
}
