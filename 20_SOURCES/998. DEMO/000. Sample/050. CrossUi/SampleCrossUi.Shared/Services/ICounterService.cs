namespace SampleCrossUi.Shared.Services;

/// <summary>
/// \if KO
/// <para>I Counter Service 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Provides counter calculation features for sample applications.</para>
/// \endif
/// </summary>
public interface ICounterService
{
    /// <summary>
    /// \if KO
    /// <para>Increment 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Increments the specified value.</para>
    /// \endif
    /// </summary>
    /// <param name="currentValue">
    /// \if KO
    /// <para>current Value에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The current counter value.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Increment 작업에서 생성한 <c>int</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The incremented value.</para>
    /// \endif
    /// </returns>
    int Increment(int currentValue);

    /// <summary>
    /// \if KO
    /// <para>Reset 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Resets the counter value.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Reset 작업에서 생성한 <c>int</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The reset counter value (always 0).</para>
    /// \endif
    /// </returns>
    int Reset();
}
