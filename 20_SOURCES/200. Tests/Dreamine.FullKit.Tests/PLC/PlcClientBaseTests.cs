using Dreamine.PLC.Abstractions.Connections;
using Dreamine.PLC.Abstractions.Devices;
using Dreamine.PLC.Abstractions.Results;
using Dreamine.PLC.Core.Clients;

namespace Dreamine.FullKit.Tests.PLC;

/// <summary>
/// \if KO
/// <para>Plc Client Base Tests 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates plc client base tests functionality and related state.</para>
/// \endif
/// </summary>
public sealed class PlcClientBaseTests
{
    /// <summary>
    /// \if KO
    /// <para>Connect Async Cancellation Returns Disconnected Instead Of Faulted 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the connect async cancellation returns disconnected instead of faulted operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Connect Async Cancellation Returns Disconnected Instead Of Faulted 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the connect async cancellation returns disconnected instead of faulted operation.</para>
    /// \endif
    /// </returns>
    [Fact]
    public async Task ConnectAsync_CancellationReturnsDisconnectedInsteadOfFaulted()
    {
        var client = new CancelingConnectClient();

        var result = await client.ConnectAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(PlcConnectionState.Disconnected, client.State);
    }

    /// <summary>
    /// \if KO
    /// <para>Canceling Connect Client 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates canceling connect client functionality and related state.</para>
    /// \endif
    /// </summary>
    private sealed class CancelingConnectClient : PlcClientBase
    {
        /// <summary>
        /// \if KO
        /// <para>Connect Core Async 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the connect core async operation.</para>
        /// \endif
        /// </summary>
        /// <param name="cancellationToken">
        /// \if KO
        /// <para>취소 요청을 감시하는 토큰입니다.</para>
        /// \endif
        /// \if EN
        /// <para>A token used to observe cancellation requests.</para>
        /// \endif
        /// </param>
        /// <returns>
        /// \if KO
        /// <para>Connect Core Async 작업에서 생성한 <c>Task&lt;PlcResult&gt;</c> 결과입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>Task&lt;PlcResult&gt;</c> result produced by the connect core async operation.</para>
        /// \endif
        /// </returns>
        /// <exception cref="OperationCanceledException">
        /// \if KO
        /// <para>Connect Core Async 작업을 완료할 수 없는 경우 <c>OperationCanceledException</c>이 발생합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Thrown as <c>OperationCanceledException</c> when the connect core async operation cannot be completed.</para>
        /// \endif
        /// </exception>
        protected override Task<PlcResult> ConnectCoreAsync(CancellationToken cancellationToken)
        {
            throw new OperationCanceledException("cancelled");
        }

        /// <summary>
        /// \if KO
        /// <para>Disconnect Core Async 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the disconnect core async operation.</para>
        /// \endif
        /// </summary>
        /// <param name="cancellationToken">
        /// \if KO
        /// <para>취소 요청을 감시하는 토큰입니다.</para>
        /// \endif
        /// \if EN
        /// <para>A token used to observe cancellation requests.</para>
        /// \endif
        /// </param>
        /// <returns>
        /// \if KO
        /// <para>Disconnect Core Async 작업에서 생성한 <c>Task&lt;PlcResult&gt;</c> 결과입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>Task&lt;PlcResult&gt;</c> result produced by the disconnect core async operation.</para>
        /// \endif
        /// </returns>
        protected override Task<PlcResult> DisconnectCoreAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(PlcResult.Success());
        }

        /// <summary>
        /// \if KO
        /// <para>Bits Core Async 데이터를 읽습니다.</para>
        /// \endif
        /// \if EN
        /// <para>Reads bits core async data.</para>
        /// \endif
        /// </summary>
        /// <param name="address">
        /// \if KO
        /// <para>address에 사용할 <c>PlcAddress</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>PlcAddress</c> value used for address.</para>
        /// \endif
        /// </param>
        /// <param name="count">
        /// \if KO
        /// <para>count에 사용할 <c>int</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>int</c> value used for count.</para>
        /// \endif
        /// </param>
        /// <param name="cancellationToken">
        /// \if KO
        /// <para>취소 요청을 감시하는 토큰입니다.</para>
        /// \endif
        /// \if EN
        /// <para>A token used to observe cancellation requests.</para>
        /// \endif
        /// </param>
        /// <returns>
        /// \if KO
        /// <para>Read Bits Core Async 작업에서 생성한 <c>Task&lt;PlcResult&lt;bool[]&gt;&gt;</c> 결과입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>Task&lt;PlcResult&lt;bool[]&gt;&gt;</c> result produced by the read bits core async operation.</para>
        /// \endif
        /// </returns>
        protected override Task<PlcResult<bool[]>> ReadBitsCoreAsync(
            PlcAddress address,
            int count,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(PlcResult<bool[]>.Success(new bool[count]));
        }

        /// <summary>
        /// \if KO
        /// <para>Words Core Async 데이터를 읽습니다.</para>
        /// \endif
        /// \if EN
        /// <para>Reads words core async data.</para>
        /// \endif
        /// </summary>
        /// <param name="address">
        /// \if KO
        /// <para>address에 사용할 <c>PlcAddress</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>PlcAddress</c> value used for address.</para>
        /// \endif
        /// </param>
        /// <param name="count">
        /// \if KO
        /// <para>count에 사용할 <c>int</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>int</c> value used for count.</para>
        /// \endif
        /// </param>
        /// <param name="cancellationToken">
        /// \if KO
        /// <para>취소 요청을 감시하는 토큰입니다.</para>
        /// \endif
        /// \if EN
        /// <para>A token used to observe cancellation requests.</para>
        /// \endif
        /// </param>
        /// <returns>
        /// \if KO
        /// <para>Read Words Core Async 작업에서 생성한 <c>Task&lt;PlcResult&lt;short[]&gt;&gt;</c> 결과입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>Task&lt;PlcResult&lt;short[]&gt;&gt;</c> result produced by the read words core async operation.</para>
        /// \endif
        /// </returns>
        protected override Task<PlcResult<short[]>> ReadWordsCoreAsync(
            PlcAddress address,
            int count,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(PlcResult<short[]>.Success(new short[count]));
        }

        /// <summary>
        /// \if KO
        /// <para>Bits Core Async 데이터를 씁니다.</para>
        /// \endif
        /// \if EN
        /// <para>Writes bits core async data.</para>
        /// \endif
        /// </summary>
        /// <param name="address">
        /// \if KO
        /// <para>address에 사용할 <c>PlcAddress</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>PlcAddress</c> value used for address.</para>
        /// \endif
        /// </param>
        /// <param name="values">
        /// \if KO
        /// <para>values에 사용할 <c>IReadOnlyList&lt;bool&gt;</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>IReadOnlyList&lt;bool&gt;</c> value used for values.</para>
        /// \endif
        /// </param>
        /// <param name="cancellationToken">
        /// \if KO
        /// <para>취소 요청을 감시하는 토큰입니다.</para>
        /// \endif
        /// \if EN
        /// <para>A token used to observe cancellation requests.</para>
        /// \endif
        /// </param>
        /// <returns>
        /// \if KO
        /// <para>Write Bits Core Async 작업에서 생성한 <c>Task&lt;PlcResult&gt;</c> 결과입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>Task&lt;PlcResult&gt;</c> result produced by the write bits core async operation.</para>
        /// \endif
        /// </returns>
        protected override Task<PlcResult> WriteBitsCoreAsync(
            PlcAddress address,
            IReadOnlyList<bool> values,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(PlcResult.Success());
        }

        /// <summary>
        /// \if KO
        /// <para>Words Core Async 데이터를 씁니다.</para>
        /// \endif
        /// \if EN
        /// <para>Writes words core async data.</para>
        /// \endif
        /// </summary>
        /// <param name="address">
        /// \if KO
        /// <para>address에 사용할 <c>PlcAddress</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>PlcAddress</c> value used for address.</para>
        /// \endif
        /// </param>
        /// <param name="values">
        /// \if KO
        /// <para>values에 사용할 <c>IReadOnlyList&lt;short&gt;</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>IReadOnlyList&lt;short&gt;</c> value used for values.</para>
        /// \endif
        /// </param>
        /// <param name="cancellationToken">
        /// \if KO
        /// <para>취소 요청을 감시하는 토큰입니다.</para>
        /// \endif
        /// \if EN
        /// <para>A token used to observe cancellation requests.</para>
        /// \endif
        /// </param>
        /// <returns>
        /// \if KO
        /// <para>Write Words Core Async 작업에서 생성한 <c>Task&lt;PlcResult&gt;</c> 결과입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>Task&lt;PlcResult&gt;</c> result produced by the write words core async operation.</para>
        /// \endif
        /// </returns>
        protected override Task<PlcResult> WriteWordsCoreAsync(
            PlcAddress address,
            IReadOnlyList<short> values,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(PlcResult.Success());
        }
    }
}
