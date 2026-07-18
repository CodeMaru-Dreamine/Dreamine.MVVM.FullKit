using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;
using System.Collections.ObjectModel;

namespace SampleSmart.Pages.WindowSub
{
    /// <summary>
    /// \if KO
    /// <para>PopupSetting ViewModel입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates popup setting view model functionality and related state.</para>
    /// \endif
    /// </summary>
    public partial class PopupSettingViewModel : ViewModelBase
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
        private PopupSettingModel _model;

        /// <summary>
        /// \if KO
        /// <para>event 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the event value.</para>
        /// \endif
        /// </summary>
        [DreamineEvent]
        private PopupSettingEvent _event;

        /// <summary>
        /// \if KO
        /// <para>자동 시작 사용 여부입니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the use auto start value.</para>
        /// \endif
        /// </summary>
        [DreamineProperty]
        private bool _useAutoStart;

        /// <summary>
        /// \if KO
        /// <para>로그 저장 사용 여부입니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the use log save value.</para>
        /// \endif
        /// </summary>
        [DreamineProperty]
        private bool _useLogSave;

        /// <summary>
        /// \if KO
        /// <para>선택 가능한 모드 목록입니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets the modes value.</para>
        /// \endif
        /// </summary>
        public ObservableCollection<string> Modes { get; } = new()
        {
            "Normal",
            "Manual",
            "Simulation"
        };

        /// <summary>
        /// \if KO
        /// <para>선택된 모드입니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the selected mode value.</para>
        /// \endif
        /// </summary>
        [DreamineProperty]
        private string _selectedMode = "Normal";

        /// <summary>
        /// \if KO
        /// <para>PopupSettingViewModel 인스턴스를 생성합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Initializes a new instance of the <see cref="PopupSettingViewModel"/> class with the specified settings.</para>
        /// \endif
        /// </summary>
        public PopupSettingViewModel()
        {
            _model = null!;
            _event = null!;

            UseAutoStart = Model.DefaultUseAutoStart;
            UseLogSave = Model.DefaultUseLogSave;
            SelectedMode = "Normal";
        }

        /// <summary>
        /// \if KO
        /// <para>설정 적용 동작을 실행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the apply operation.</para>
        /// \endif
        /// </summary>
        [DreamineCommand("Event.Apply")]
        private partial void Apply();
    }
}
