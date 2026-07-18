using Dreamine.MVVM.Core;
using SampleSmart.Pages.PageSub.CommunicationTabs.Serial;
using System.Windows.Controls;

namespace SampleSmart.Pages.PageSub.CommunicationTabs.Udp;

/// <summary>
/// \if KO
/// <para>\brief UDP Peer Loopback 테스트 View입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates udp peer test view functionality and related state.</para>
/// \endif
/// </summary>
public partial class UdpPeerTestView : UserControl
{
    /// <summary>
    /// \if KO
    /// <para>\brief UdpPeerTestView 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="UdpPeerTestView"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    public UdpPeerTestView()
    {
        InitializeComponent();

        DataContext ??= DMContainer.Resolve<UdpPeerTestViewModel>();
    }
}
