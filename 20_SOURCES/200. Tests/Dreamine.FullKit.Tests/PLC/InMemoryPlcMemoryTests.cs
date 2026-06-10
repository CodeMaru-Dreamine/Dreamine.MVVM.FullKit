using Dreamine.PLC.Abstractions.Devices;
using Dreamine.PLC.Core.Memory;

namespace Dreamine.FullKit.Tests.PLC;

public sealed class InMemoryPlcMemoryTests
{
    [Fact]
    public void WriteWordsThenReadWords_ReturnsValuesFromRequestedOffset()
    {
        var memory = new InMemoryPlcMemory();
        var address = new PlcAddress(PlcDeviceType.D, 100);

        var write = memory.WriteWords(address, new short[] { 10, 20, 30 });
        var read = memory.ReadWords(new PlcAddress(PlcDeviceType.D, 101), 2);

        Assert.True(write.IsSuccess);
        Assert.True(read.IsSuccess);
        Assert.Equal(new short[] { 20, 30 }, read.Value);
    }

    [Fact]
    public void WriteBitsThenReadBits_ReturnsValuesFromRequestedOffset()
    {
        var memory = new InMemoryPlcMemory();
        var address = new PlcAddress(PlcDeviceType.M, 0);

        var write = memory.WriteBits(address, new[] { true, false, true });
        var read = memory.ReadBits(new PlcAddress(PlcDeviceType.M, 0), 4);

        Assert.True(write.IsSuccess);
        Assert.True(read.IsSuccess);
        Assert.Equal(new[] { true, false, true, false }, read.Value);
    }

    [Fact]
    public void Clear_RemovesStoredBitAndWordValues()
    {
        var memory = new InMemoryPlcMemory();

        memory.WriteWords(new PlcAddress(PlcDeviceType.D, 0), new short[] { 7 });
        memory.WriteBits(new PlcAddress(PlcDeviceType.M, 0), new[] { true });

        memory.Clear();

        Assert.Equal(new short[] { 0 }, memory.ReadWords(new PlcAddress(PlcDeviceType.D, 0), 1).Value);
        Assert.Equal(new[] { false }, memory.ReadBits(new PlcAddress(PlcDeviceType.M, 0), 1).Value);
    }

    [Fact]
    public void ReadAndWrite_ReturnFailureForInvalidCounts()
    {
        var memory = new InMemoryPlcMemory();
        var address = new PlcAddress(PlcDeviceType.D, 0);

        Assert.False(memory.ReadWords(address, 0).IsSuccess);
        Assert.False(memory.ReadBits(address, 0).IsSuccess);
        Assert.False(memory.WriteWords(address, Array.Empty<short>()).IsSuccess);
        Assert.False(memory.WriteBits(address, Array.Empty<bool>()).IsSuccess);
    }
}
