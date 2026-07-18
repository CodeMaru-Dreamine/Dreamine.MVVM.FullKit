using System.Windows.Controls;
using Dreamine.MVVM.Core;

namespace SampleSmart.Pages.PageSub.CommunicationTabs.Tcp;

/// <summary>
/// \if KO
/// <para>\brief TCP Loopback 테스트 View입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates tcp loopback test view functionality and related state.</para>
/// \endif
/// </summary>
public partial class TcpLoopbackTestView : UserControl
{
    /// <summary>
    /// \if KO
    /// <para>\brief TcpLoopbackTestView 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="TcpLoopbackTestView"/> class with the specified settings.</para>
    /// \endif
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
