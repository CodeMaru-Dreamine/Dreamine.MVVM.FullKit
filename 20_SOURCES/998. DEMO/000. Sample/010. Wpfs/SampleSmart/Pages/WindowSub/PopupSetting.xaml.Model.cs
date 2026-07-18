namespace SampleSmart.Pages.WindowSub
{
    /// <summary>
    /// \if KO
    /// <para>PopupSetting 모델입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates popup setting model functionality and related state.</para>
    /// \endif
    /// </summary>
    public sealed class PopupSettingModel
    {
        /// <summary>
        /// \if KO
        /// <para>자동 시작 기본값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets the default use auto start value.</para>
        /// \endif
        /// </summary>
        public bool DefaultUseAutoStart { get; } = true;

        /// <summary>
        /// \if KO
        /// <para>로그 저장 기본값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets the default use log save value.</para>
        /// \endif
        /// </summary>
        public bool DefaultUseLogSave { get; } = true;
    }
}
