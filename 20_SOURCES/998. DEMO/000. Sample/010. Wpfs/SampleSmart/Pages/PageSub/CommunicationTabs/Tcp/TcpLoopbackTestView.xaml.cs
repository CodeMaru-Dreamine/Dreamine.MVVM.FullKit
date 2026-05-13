using System.Windows.Controls;
using Dreamine.MVVM.Core;

namespace SampleSmart.Pages.PageSub.CommunicationTabs.Tcp;

/// <summary>
/// \brief TCP Loopback 테스트 View입니다.
/// </summary>
public partial class TcpLoopbackTestView : UserControl
{
    /// <summary>
    /// \brief TcpLoopbackTestView 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    public TcpLoopbackTestView()
    {
        InitializeComponent();

        if (DataContext is null)
        {
            DataContext = DMContainer.Resolve<TcpLoopbackTestViewModel>();
        }
    }
}
