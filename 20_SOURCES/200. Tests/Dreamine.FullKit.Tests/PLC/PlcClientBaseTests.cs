using Dreamine.PLC.Abstractions.Connections;
using Dreamine.PLC.Abstractions.Devices;
using Dreamine.PLC.Abstractions.Results;
using Dreamine.PLC.Core.Clients;

namespace Dreamine.FullKit.Tests.PLC;

public sealed class PlcClientBaseTests
{
    [Fact]
    public async Task ConnectAsync_CancellationReturnsDisconnectedInsteadOfFaulted()
    {
        var client = new CancelingConnectClient();

        var result = await client.ConnectAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(PlcConnectionState.Disconnected, client.State);
    }

    private sealed class CancelingConnectClient : PlcClientBase
    {
        protected override Task<PlcResult> ConnectCoreAsync(CancellationToken cancellationToken)
        {
            throw new OperationCanceledException("cancelled");
        }

        protected override Task<PlcResult> DisconnectCoreAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(PlcResult.Success());
        }

        protected override Task<PlcResult<bool[]>> ReadBitsCoreAsync(
            PlcAddress address,
            int count,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(PlcResult<bool[]>.Success(new bool[count]));
        }

        protected override Task<PlcResult<short[]>> ReadWordsCoreAsync(
            PlcAddress address,
            int count,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(PlcResult<short[]>.Success(new short[count]));
        }

        protected override Task<PlcResult> WriteBitsCoreAsync(
            PlcAddress address,
            IReadOnlyList<bool> values,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(PlcResult.Success());
        }

        protected override Task<PlcResult> WriteWordsCoreAsync(
            PlcAddress address,
            IReadOnlyList<short> values,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(PlcResult.Success());
        }
    }
}
