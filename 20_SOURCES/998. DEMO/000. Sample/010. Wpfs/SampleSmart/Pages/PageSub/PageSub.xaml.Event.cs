using System.Windows;

namespace SampleSmart.Pages.PageSub
{
    /// <summary>
    /// PageSub 화면 이벤트를 처리합니다.
    /// </summary>
    public class PageSubEvent
    {
        private readonly PageSubModel _model;

        /// <summary>
        /// PageSubEvent 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="model">PageSub 화면 모델입니다.</param>
        public PageSubEvent(PageSubModel model)
        {
            _model = model;
        }

        /// <summary>
        /// OK 동작을 실행합니다.
        /// </summary>
        public void Ok()
        {
            MessageBox.Show($"[{_model.Message}] 확인 클릭됨!");
        }

        /// <summary>
        /// Cancel 동작을 실행합니다.
        /// </summary>
        public void Cancel()
        {
            MessageBox.Show($"[{_model.Message}] 취소 클릭됨!");
        }
    }
}