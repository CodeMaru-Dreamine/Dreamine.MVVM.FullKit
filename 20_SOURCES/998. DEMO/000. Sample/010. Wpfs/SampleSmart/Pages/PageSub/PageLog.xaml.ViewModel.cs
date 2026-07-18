using Dreamine.Logging.Wpf.ViewModels;
using Dreamine.MVVM.ViewModels;

namespace SampleSmart.Pages.PageSub
{
    /// <summary>
    /// \if KO
    /// <para>Page Log View Model 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Provides the ViewModel for the PageLog view.</para>
    /// \endif
    /// </summary>
    public sealed class PageLogViewModel : ViewModelBase
    {
        /// <summary>
        /// \if KO
        /// <para>Log Panel 값을 가져옵니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets the Dreamine log panel ViewModel.</para>
        /// \endif
        /// </summary>
        public DreamineLogPanelViewModel LogPanel { get; }

        /// <summary>
        /// \if KO
        /// <para>지정한 설정으로 <see cref="PageLogViewModel"/> 클래스의 새 인스턴스를 초기화합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Initializes a new instance of the <see cref="PageLogViewModel"/> class.</para>
        /// \endif
        /// </summary>
        /// <param name="logPanel">
        /// \if KO
        /// <para>log Panel에 사용할 <c>DreamineLogPanelViewModel</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The Dreamine log panel ViewModel.</para>
        /// \endif
        /// </param>
        public PageLogViewModel(DreamineLogPanelViewModel logPanel)
        {
            LogPanel = logPanel;
        }
    }
}