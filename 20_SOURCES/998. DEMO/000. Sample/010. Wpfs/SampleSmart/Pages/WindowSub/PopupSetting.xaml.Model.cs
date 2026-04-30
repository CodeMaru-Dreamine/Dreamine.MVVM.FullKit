namespace SampleSmart.Pages.WindowSub
{
    /// <summary>
    /// PopupSetting 모델입니다.
    /// </summary>
    public sealed class PopupSettingModel
    {
        /// <summary>
        /// 자동 시작 기본값입니다.
        /// </summary>
        public bool DefaultUseAutoStart { get; } = true;

        /// <summary>
        /// 로그 저장 기본값입니다.
        /// </summary>
        public bool DefaultUseLogSave { get; } = true;
    }
}
