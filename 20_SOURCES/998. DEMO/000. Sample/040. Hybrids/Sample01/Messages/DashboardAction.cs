namespace Sample01.Messages
{
    /// <summary>
    /// \if KO
    /// <para>Dashboard Action 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Sample dashboard actions handled by the WPF shell.</para>
    /// \endif
    /// </summary>
    public enum DashboardAction
    {
        /// <summary>
        /// \if KO
        /// <para>Open Project 값을 나타냅니다.</para>
        /// \endif
        /// \if EN
        /// <para>Represents the open project value.</para>
        /// \endif
        /// </summary>
        OpenProject,
        /// <summary>
        /// \if KO
        /// <para>Open Nuget 값을 나타냅니다.</para>
        /// \endif
        /// \if EN
        /// <para>Represents the open nuget value.</para>
        /// \endif
        /// </summary>
        OpenNuget,
        /// <summary>
        /// \if KO
        /// <para>Open Docs 값을 나타냅니다.</para>
        /// \endif
        /// \if EN
        /// <para>Represents the open docs value.</para>
        /// \endif
        /// </summary>
        OpenDocs,
        /// <summary>
        /// \if KO
        /// <para>Open Settings 값을 나타냅니다.</para>
        /// \endif
        /// \if EN
        /// <para>Represents the open settings value.</para>
        /// \endif
        /// </summary>
        OpenSettings
    }
}
