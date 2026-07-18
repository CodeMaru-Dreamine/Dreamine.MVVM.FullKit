using System;

namespace Sample01.States
{
    /// <summary>
    /// \if KO
    /// <para>Counter State 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>\brief Represents the shared counter state for the hybrid sample.</para>
    /// \endif
    /// </summary>
    public sealed record CounterState(
        int Count,
        string LastSource,
        DateTime? LastUpdated);
}