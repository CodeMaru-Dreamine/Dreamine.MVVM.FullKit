namespace SampleSmart.Pages.PageSub.PlcTabs;

/// <summary>
/// \if KO
/// <para>\brief InMemory PLC 테스트 이벤트 처리 클래스입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates in memory plc tab event functionality and related state.</para>
/// \endif
/// </summary>
public sealed class InMemoryPlcTabEvent
{
    /// <summary>
    /// \if KO
    /// <para>runtime 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the runtime value.</para>
    /// \endif
    /// </summary>
    private readonly PlcSampleRuntime _runtime;

    /// <summary>
    /// \if KO
    /// <para>\brief InMemoryPlcTabEvent 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="InMemoryPlcTabEvent"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="runtime">
    /// \if KO
    /// <para>PLC 샘플 공유 런타임입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>PlcSampleRuntime</c> value used for runtime.</para>
    /// \endif
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// \if KO
    /// <para>필수 입력 인자 중 하나가 <see langword="null"/>인 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when a required input argument is <see langword="null"/>.</para>
    /// \endif
    /// </exception>
    public InMemoryPlcTabEvent(PlcSampleRuntime runtime)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
    }

    /// <summary>
    /// \if KO
    /// <para>\brief InMemory PLC Client를 선택합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the use in memory operation.</para>
    /// \endif
    /// </summary>
    public void UseInMemory()
    {
        _runtime.UseInMemoryClient();
    }
}
