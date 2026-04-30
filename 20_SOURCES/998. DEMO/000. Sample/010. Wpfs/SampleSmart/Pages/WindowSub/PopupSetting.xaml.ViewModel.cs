using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;
using System.Collections.ObjectModel;

namespace SampleSmart.Pages.WindowSub
{
    /// <summary>
    /// PopupSetting ViewModel입니다.
    /// </summary>
    public partial class PopupSettingViewModel : ViewModelBase
    {
        [DreamineModel]
        private PopupSettingModel _model;

        [DreamineEvent]
        private PopupSettingEvent _event;

        /// <summary>
        /// 자동 시작 사용 여부입니다.
        /// </summary>
        [DreamineProperty]
        private bool _useAutoStart;

        /// <summary>
        /// 로그 저장 사용 여부입니다.
        /// </summary>
        [DreamineProperty]
        private bool _useLogSave;

        /// <summary>
        /// 선택 가능한 모드 목록입니다.
        /// </summary>
        public ObservableCollection<string> Modes { get; } = new()
        {
            "Normal",
            "Manual",
            "Simulation"
        };

        /// <summary>
        /// 선택된 모드입니다.
        /// </summary>
        [DreamineProperty]
        private string _selectedMode = "Normal";

        /// <summary>
        /// PopupSettingViewModel 인스턴스를 생성합니다.
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
        /// 설정 적용 동작을 실행합니다.
        /// </summary>
        [DreamineCommand("Event.Apply")]
        private partial void Apply();
    }
}
