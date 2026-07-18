namespace SampleCrossUi.Shared.Services;

/// <summary>
/// \if KO
/// <para>Counter Service 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Provides the default counter calculation implementation.</para>
/// \endif
/// </summary>
public sealed class CounterService : ICounterService
{
    /// <summary>
    /// \if KO
    /// <para>Increment 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the increment operation.</para>
    /// \endif
    /// </summary>
    /// <param name="currentValue">
    /// \if KO
    /// <para>current Value에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for current value.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Increment 작업에서 생성한 <c>int</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> result produced by the increment operation.</para>
    /// \endif
    /// </returns>
    public int Increment(int currentValue) => currentValue + 1;

    /// <summary>
    /// \if KO
    /// <para>Reset 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the reset operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Reset 작업에서 생성한 <c>int</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> result produced by the reset operation.</para>
    /// \endif
    /// </returns>
    public int Reset() => 0;
}
