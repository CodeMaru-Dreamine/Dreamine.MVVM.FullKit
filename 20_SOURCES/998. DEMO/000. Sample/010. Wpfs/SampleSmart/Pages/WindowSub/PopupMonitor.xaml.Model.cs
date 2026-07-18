using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleSmart.Pages.WindowSub
{
    /// <summary>
    /// \if KO
    /// <para>PopupMonitor 모델입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates popup monitor model functionality and related state.</para>
    /// \endif
    /// </summary>
    public sealed class PopupMonitorModel
    {
        /// <summary>
        /// \if KO
        /// <para>화면 제목을 가져옵니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets the title value.</para>
        /// \endif
        /// </summary>
        public string Title { get; } = "Popup Monitor";
    }
}
