using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.Core;
using Dreamine.MVVM.ViewModels;
using SampleSmart.Pages.PageSub;

namespace SampleSmart.Pages.WindowSub
{
    /// <summary>
    /// \if KO
    /// <para>WindowSub에 대한 ViewModel 클래스입니다. Model과 Event 사이의 바인딩을 담당합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates window sub view model functionality and related state.</para>
    /// \endif
    /// </summary>
    public partial class WindowSubViewModel : ViewModelBase
    {
        /// <summary>
        /// \if KO
        /// <para>model 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the model value.</para>
        /// \endif
        /// </summary>
        [DreamineModel]
        private WindowSubModel _model;

        /// <summary>
        /// \if KO
        /// <para>readme 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the readme value.</para>
        /// \endif
        /// </summary>
        [DreamineProperty]
        private string _readme = string.Empty;

        /// <summary>
        /// \if KO
        /// <para>event 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the event value.</para>
        /// \endif
        /// </summary>
        [DreamineEvent]
        private WindowSubEvent _event;

        /// <summary>
        /// \if KO
        /// <para>ReadmeClick 동작 예제입니다. DreamineCommand 방식에서는 Event.ReadmeClick() 호출 후 결과를 Readme에 반영하는 형태로 구현할 수 있습니다. 현재 샘플은 동일 동작을 DreamineCommand 선언 한 줄로 대체하는 방식을 보여줍니다.</para>
        /// \endif
        /// \if EN
        /// <para>Reads me click data.</para>
        /// \endif
        /// </summary>
        [DreamineCommand("Event.ReadmeClick", BindTo = nameof(Readme))]
        private partial void ReadmeClick();


        /// <summary>
        /// \if KO
        /// <para>지정한 설정으로 <see cref="WindowSubViewModel"/> 클래스의 새 인스턴스를 초기화합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Initializes a new instance of the <see cref="WindowSubViewModel"/> class with the specified settings.</para>
        /// \endif
        /// </summary>
        public WindowSubViewModel()
        {
            _model = null!;
            _event = null!;

            _ = _model;
            _ = _event;

            Readme = Model.Readme;
        }
    }
}