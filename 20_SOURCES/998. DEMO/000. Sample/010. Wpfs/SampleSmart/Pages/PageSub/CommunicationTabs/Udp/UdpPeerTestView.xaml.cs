using Dreamine.MVVM.Core;
using SampleSmart.Pages.PageSub.CommunicationTabs.Serial;
using System.Windows.Controls;

namespace SampleSmart.Pages.PageSub.CommunicationTabs.Udp;

/// <summary>
/// \brief UDP Peer Loopback 테스트 View입니다.
/// </summary>
public partial class UdpPeerTestView : UserControl
{
    /// <summary>
    /// \brief UdpPeerTestView 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    public UdpPeerTestView()
    {
        InitializeComponent();

        DataContext ??= DMContainer.Resolve<UdpPeerTestViewModel>();
    }
}
