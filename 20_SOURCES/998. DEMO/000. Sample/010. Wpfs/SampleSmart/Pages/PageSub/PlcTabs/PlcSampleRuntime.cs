using Dreamine.PLC.Abstractions.Clients;
using Dreamine.PLC.Abstractions.Connections;
using Dreamine.PLC.Core.Clients;
using Dreamine.PLC.Core.Devices;
using Dreamine.PLC.Core.Simulation;
using Dreamine.PLC.Wpf.ViewModels;

namespace SampleSmart.Pages.PageSub.PlcTabs;

/// <summary>
/// \brief PLC 샘플 전체에서 공유되는 Runtime 컨텍스트입니다.
/// </summary>
public sealed class PlcSampleRuntime
{
    private readonly InMemoryPlcClient _inMemoryClient = new();
    private readonly DefaultPlcAddressParser _addressParser = new();
    private IPlcClient _activeClient;
    private PlcSimulatorServer? _simulatorServer;
    private PlcSimulatorTcpClient? _simulatorClient;

    /// <summary>
    /// \brief PlcSampleRuntime 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    public PlcSampleRuntime()
    {
        _activeClient = _inMemoryClient;
        Monitor = new PlcMonitorViewModel(_inMemoryClient, "InMemory PLC");
    }

    /// <summary>
    /// \brief PLC Monitor ViewModel입니다.
    /// </summary>
    public PlcMonitorViewModel Monitor { get; }

    /// <summary>
    /// \brief PLC Simulator 서버 상태 메시지입니다.
    /// </summary>
    public string ServerStatus { get; private set; } = "Server stopped.";

    /// <summary>
    /// \brief InMemory PLC Client를 모니터에 연결합니다.
    /// </summary>
    public void UseInMemoryClient()
    {
        _activeClient = _inMemoryClient;
        Monitor.SetClient(_inMemoryClient, "InMemory PLC");
        Monitor.StatusMessage = "InMemory PLC client selected.";
    }

    /// <summary>
    /// \brief TCP Simulator Client를 모니터에 연결합니다.
    /// </summary>
    /// <param name="host">서버 Host입니다.</param>
    /// <param name="port">서버 Port입니다.</param>
    public void UseSimulatorTcpClient(string host, int port)
    {
        _simulatorClient = new PlcSimulatorTcpClient(new PlcSimulatorClientOptions
        {
            Host = host,
            Port = port
        });

        _activeClient = _simulatorClient;
        Monitor.SetClient(_simulatorClient, $"PLC Simulator TCP Client ({host}:{port})");
        Monitor.StatusMessage = "PLC Simulator TCP client selected.";
    }

    /// <summary>
    /// \brief TCP PLC Simulator Server를 시작합니다.
    /// </summary>
    /// <param name="host">Bind Host입니다.</param>
    /// <param name="port">Bind Port입니다.</param>
    public async Task StartSimulatorServerAsync(string host, int port)
    {
        if (_simulatorServer is not null)
        {
            ServerStatus = "Server already running.";
            Monitor.StatusMessage = ServerStatus;
            return;
        }

        _simulatorServer = new PlcSimulatorServer(new PlcSimulatorServerOptions
        {
            Host = host,
            Port = port,
            EnableAutoWordResponse = true,
            AutoResponseTriggerAddress = "D100",
            AutoResponseAddress = "D101",
            AutoResponseIncrement = 1
        });

        _simulatorServer.StatusChanged += OnSimulatorServerStatusChanged;
        await _simulatorServer.StartAsync();
        ServerStatus = $"Server running. {host}:{port}";
        Monitor.StatusMessage = ServerStatus;
    }

    /// <summary>
    /// \brief TCP PLC Simulator Server를 중지합니다.
    /// </summary>
    public async Task StopSimulatorServerAsync()
    {
        if (_simulatorServer is null)
        {
            ServerStatus = "Server stopped.";
            Monitor.StatusMessage = ServerStatus;
            return;
        }

        _simulatorServer.StatusChanged -= OnSimulatorServerStatusChanged;
        await _simulatorServer.StopAsync();
        _simulatorServer = null;
        ServerStatus = "Server stopped.";
        Monitor.StatusMessage = ServerStatus;
    }

    /// <summary>
    /// \brief Runs a client-server auto-response handshake test using D100 as client write and D101 as server response.
    /// </summary>
    /// <param name="startValue">The first client value.</param>
    /// <param name="iterations">The number of handshake iterations.</param>
    /// <param name="delayMs">The delay between iterations in milliseconds.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task RunHandshakeTestAsync(
        short startValue,
        int iterations,
        int delayMs,
        CancellationToken cancellationToken = default)
    {
        if (iterations <= 0)
        {
            Monitor.StatusMessage = "Handshake iterations must be greater than zero.";
            Monitor.AppendLog("Handshake", "D100/D101", iterations.ToString(), false, Monitor.StatusMessage);
            return;
        }

        if (_activeClient.State != PlcConnectionState.Connected)
        {
            Monitor.StatusMessage = "Handshake test requires a connected PLC client.";
            Monitor.AppendLog("Handshake", "D100/D101", string.Empty, false, Monitor.StatusMessage);
            return;
        }

        var clientWriteAddress = _addressParser.Parse("D100").Value;
        var serverResponseAddress = _addressParser.Parse("D101").Value;
        var value = startValue;

        for (var index = 0; index < iterations; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var writeResult = await _activeClient.WriteWordsAsync(clientWriteAddress, [value], cancellationToken).ConfigureAwait(true);
            if (!writeResult.IsSuccess)
            {
                Monitor.StatusMessage = writeResult.Message ?? "Handshake write failed.";
                Monitor.AppendLog("HandshakeWrite", "D100", value.ToString(), false, Monitor.StatusMessage);
                return;
            }

            var expected = checked((short)(value + 1));
            var readResult = await _activeClient.ReadWordsAsync(serverResponseAddress, 1, cancellationToken).ConfigureAwait(true);
            var actual = readResult.Value is { Length: > 0 } ? readResult.Value[0] : short.MinValue;
            if (!readResult.IsSuccess || actual != expected)
            {
                Monitor.StatusMessage = readResult.IsSuccess
                    ? $"Handshake mismatch. expected={expected}, actual={actual}."
                    : readResult.Message ?? "Handshake read failed.";
                Monitor.AppendLog("HandshakeRead", "D101", $"expected={expected}, actual={actual}", false, Monitor.StatusMessage);
                return;
            }

            Monitor.StatusMessage = $"Handshake OK {index + 1}/{iterations}. D100={value}, D101={actual}";
            Monitor.AppendLog("Handshake", "D100/D101", $"{value}->{actual}", true, Monitor.StatusMessage);
            value = actual;

            if (delayMs > 0)
            {
                await Task.Delay(delayMs, cancellationToken).ConfigureAwait(true);
            }
        }
    }

    private void OnSimulatorServerStatusChanged(object? sender, string e)
    {
        ServerStatus = e;
        Monitor.StatusMessage = e;
    }
}
