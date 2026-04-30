using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SampleSmart.Pages.WindowSub
{
    /// <summary>
    /// PopupMonitor 이벤트를 처리합니다.
    /// </summary>
    public sealed class PopupMonitorEvent
    {
        /// <summary>
        /// 새로고침 동작을 실행합니다.
        /// </summary>
        public void Refresh()
        {
            MessageBox.Show("상태 새로고침");
        }
    }
}
