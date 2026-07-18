namespace SampleCrossUi.Shared.Models;

/// <summary>
/// \if KO
/// <para>Counter Log Item 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Represents a counter operation log item.</para>
/// \endif
/// </summary>
public sealed record CounterLogItem(
    DateTime CreatedAt,
    string Message);
