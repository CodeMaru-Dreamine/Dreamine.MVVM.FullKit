using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SampleSmart.Pages.WindowSub
{
    /// <summary>
    /// \if KO
    /// <para>PopupMonitor 이벤트를 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates popup monitor event functionality and related state.</para>
    /// \endif
    /// </summary>
    public sealed class PopupMonitorEvent
    {
        /// <summary>
        /// \if KO
        /// <para>새로고침 동작을 실행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the refresh operation.</para>
        /// \endif
        /// </summary>
        public void Refresh()
        {
            MessageBox.Show("상태 새로고침");
        }
    }
}
