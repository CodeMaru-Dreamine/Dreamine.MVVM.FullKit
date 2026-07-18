using Dreamine.MVVM.ViewModels;
using Dreamine.Threading.Wpf.ViewModels;

namespace SampleSmart.Pages.PageSub;

/// <summary>
/// \if KO
/// <para>Page Thread Monitor View Model 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Provides the ViewModel for the sample thread monitor page.</para>
/// \endif
/// </summary>
public sealed class PageThreadMonitorViewModel : ViewModelBase
{
    /// <summary>
    /// \if KO
    /// <para>Thread Monitor 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the Dreamine thread monitor ViewModel.</para>
    /// \endif
    /// </summary>
    public DreamineThreadMonitorViewModel ThreadMonitor { get; }

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="PageThreadMonitorViewModel"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="PageThreadMonitorViewModel"/> class.</para>
    /// \endif
    /// </summary>
    /// <param name="threadMonitor">
    /// \if KO
    /// <para>thread Monitor에 사용할 <c>DreamineThreadMonitorViewModel</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The Dreamine thread monitor ViewModel.</para>
    /// \endif
    /// </param>
    public PageThreadMonitorViewModel(DreamineThreadMonitorViewModel threadMonitor)
    {
        ThreadMonitor = threadMonitor;
    }
}