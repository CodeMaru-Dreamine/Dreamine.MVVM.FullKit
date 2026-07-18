using System.Windows;

namespace SampleSmart.Pages.PageSub
{
    /// <summary>
    /// \if KO
    /// <para>PageSub 화면 이벤트를 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates page sub event functionality and related state.</para>
    /// \endif
    /// </summary>
    public class PageSubEvent
    {
        /// <summary>
        /// \if KO
        /// <para>model 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the model value.</para>
        /// \endif
        /// </summary>
        private readonly PageSubModel _model;

        /// <summary>
        /// \if KO
        /// <para>PageSubEvent 인스턴스를 생성합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Initializes a new instance of the <see cref="PageSubEvent"/> class with the specified settings.</para>
        /// \endif
        /// </summary>
        /// <param name="model">
        /// \if KO
        /// <para>PageSub 화면 모델입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>PageSubModel</c> value used for model.</para>
        /// \endif
        /// </param>
        public PageSubEvent(PageSubModel model)
        {
            _model = model;
        }

        /// <summary>
        /// \if KO
        /// <para>OK 동작을 실행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the ok operation.</para>
        /// \endif
        /// </summary>
        public void Ok()
        {
            MessageBox.Show($"[{_model.Message}] 확인 클릭됨!");
        }

        /// <summary>
        /// \if KO
        /// <para>Cancel 동작을 실행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Determines whether cancel.</para>
        /// \endif
        /// </summary>
        public void Cancel()
        {
            MessageBox.Show($"[{_model.Message}] 취소 클릭됨!");
        }
    }
}