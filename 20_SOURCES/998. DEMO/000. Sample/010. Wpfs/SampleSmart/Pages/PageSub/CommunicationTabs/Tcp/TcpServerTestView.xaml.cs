using System.Windows.Controls;
using Dreamine.MVVM.Core;

namespace SampleSmart.Pages.PageSub.CommunicationTabs.Tcp;

/// <summary>
/// \if KO
/// <para>\brief TCP Server 테스트 View입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates tcp server test view functionality and related state.</para>
/// \endif
/// </summary>
public partial class TcpServerTestView : UserControl
{
    /// <summary>
    /// \if KO
    /// <para>\brief TcpServerTestView 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="TcpServerTestView"/> class with the specified settings.</para>
    /// \endif
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
