using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleSmart.Pages.WindowSub
{
    /// <summary>
    /// PopupNotice 모델입니다.
    /// </summary>
    public sealed class PopupNoticeModel
    {
        /// <summary>
        /// 팝업 메시지를 가져옵니다.
        /// </summary>
        public string Message { get; } = "공지 팝업입니다. 여러 종류의 팝업이 동시에 열리는지 확인합니다.";
    }
}
