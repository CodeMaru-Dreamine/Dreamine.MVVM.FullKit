namespace SampleSmart.Pages.PageSub.PlcTabs;

/// <summary>
/// \if KO
/// <para>\brief PLC Simulator TCP 테스트 이벤트 처리 클래스입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates plc simulator tab event functionality and related state.</para>
/// \endif
/// </summary>
public sealed class PlcSimulatorTabEvent
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
    /// <para>\brief PlcSimulatorTabEvent 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="PlcSimulatorTabEvent"/> class with the specified settings.</para>
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
    public PlcSimulatorTabEvent(PlcSampleRuntime runtime)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
    }

    /// <summary>
    /// \if KO
    /// <para>\brief Simulator Host입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the host value.</para>
    /// \endif
    /// </summary>
    public string Host { get; set; } = "127.0.0.1";

    /// <summary>
    /// \if KO
    /// <para>\brief Simulator Port 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the port text value.</para>
    /// \endif
    /// </summary>
    public string PortText { get; set; } = "55000";

    /// <summary>
    /// \if KO
    /// <para>\brief PLC Simulator 서버를 시작합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the start server operation.</para>
    /// \endif
    /// </summary>
    public void StartServer()
    {
        if (!TryGetPort(out var port))
        {
            return;
        }

        var bindHost = Host == "127.0.0.1" ? "0.0.0.0" : Host;
        _ = _runtime.StartSimulatorServerAsync(bindHost, port);
    }

    /// <summary>
    /// \if KO
    /// <para>\brief PLC Simulator 서버를 중지합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the stop server operation.</para>
    /// \endif
    /// </summary>
    public void StopServer()
    {
        _ = _runtime.StopSimulatorServerAsync();
    }

    /// <summary>
    /// \if KO
    /// <para>\brief PLC Simulator TCP Client를 선택합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the use tcp client operation.</para>
    /// \endif
    /// </summary>
    public void UseTcpClient()
    {
        if (!TryGetPort(out var port))
        {
            return;
        }

        _runtime.UseSimulatorTcpClient(Host, port);
    }

    /// <summary>
    /// \if KO
    /// <para>Get Port 작업을 시도하고 성공 여부를 반환합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Attempts to get port and returns whether the operation succeeds.</para>
    /// \endif
    /// </summary>
    /// <param name="port">
    /// \if KO
    /// <para>port에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for port.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Try Get Port 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the try get port condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    private bool TryGetPort(out int port)
    {
        if (!int.TryParse(PortText, out port) || port is <= 0 or > 65535)
        {
            _runtime.Monitor.StatusMessage = "Port must be between 1 and 65535.";
            return false;
        }

        return true;
    }
}
