using Dreamine.MVVM.Interfaces.Navigation;
using SampleCore.ViewModels.PageSub;
using System.Windows;

namespace SampleCore.Events
{
    /// <summary>
    /// \if KO
    /// <para>Main Window Event 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates main window event functionality and related state.</para>
    /// \endif
    /// </summary>
    public class MainWindowEvent
    {
        /// <summary>
        /// \if KO
        /// <para>view Manager 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the view manager value.</para>
        /// \endif
        /// </summary>
        private readonly IViewManager _viewManager;
        /// <summary>
        /// \if KO
        /// <para>지정한 설정으로 <see cref="MainWindowEvent"/> 클래스의 새 인스턴스를 초기화합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Initializes a new instance of the <see cref="MainWindowEvent"/> class with the specified settings.</para>
        /// \endif
        /// </summary>
        /// <param name="viewManager">
        /// \if KO
        /// <para>view Manager에 사용할 <c>IViewManager</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>IViewManager</c> value used for view manager.</para>
        /// \endif
        /// </param>
        public MainWindowEvent(IViewManager viewManager) => _viewManager = viewManager;

        /// <summary>
        /// \if KO
        /// <para>Ok 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the ok operation.</para>
        /// \endif
        /// </summary>
        public void Ok() => MessageBox.Show("확인 클릭됨!");
        /// <summary>
        /// \if KO
        /// <para>Cancel 조건을 확인합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Determines whether cancel.</para>
        /// \endif
        /// </summary>
        public void Cancel() => MessageBox.Show("취소 클릭됨!");

        /// <summary>
        /// \if KO
        /// <para>Sub Page 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the sub page operation.</para>
        /// \endif
        /// </summary>
        public void SubPage() => _viewManager.Show<PageSubViewModel>();

        /// <summary>
        /// \if KO
        /// <para>Minimize 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the minimize operation.</para>
        /// \endif
        /// </summary>
        public void Minimize()
        {
            var w = GetActiveWindow();
            if (w != null) w.WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// \if KO
        /// <para>Maximize 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the maximize operation.</para>
        /// \endif
        /// </summary>
        public void Maximize()
        {
            var w = GetActiveWindow();
            if (w != null)
            {
                w.WindowState = w.WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;
            }
        }

        /// <summary>
        /// \if KO
        /// <para>Close 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the close operation.</para>
        /// \endif
        /// </summary>
        public void Close()
        {
            GetActiveWindow()?.Close();
        }

        /// <summary>
        /// \if KO
        /// <para>Active Window 값을 가져옵니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets the active window value.</para>
        /// \endif
        /// </summary>
        /// <returns>
        /// \if KO
        /// <para>Get Active Window 작업에서 생성한 <c>Window?</c> 결과입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>Window?</c> result produced by the get active window operation.</para>
        /// \endif
        /// </returns>
        private Window? GetActiveWindow()
        {
            return Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
        }
    }
}
