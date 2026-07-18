using Dreamine.PLC.Abstractions.Devices;
using Dreamine.PLC.Core.Memory;

namespace Dreamine.FullKit.Tests.PLC;

/// <summary>
/// \if KO
/// <para>In Memory Plc Memory Tests 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates in memory plc memory tests functionality and related state.</para>
/// \endif
/// </summary>
public sealed class InMemoryPlcMemoryTests
{
    /// <summary>
    /// \if KO
    /// <para>Words Then Read Words Returns Values From Requested Offset 데이터를 씁니다.</para>
    /// \endif
    /// \if EN
    /// <para>Writes words then read words returns values from requested offset data.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>Bits Then Read Bits Returns Values From Requested Offset 데이터를 씁니다.</para>
    /// \endif
    /// \if EN
    /// <para>Writes bits then read bits returns values from requested offset data.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>Clear Removes Stored Bit And Word Values 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the clear removes stored bit and word values operation.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>And Write Return Failure For Invalid Counts 데이터를 읽습니다.</para>
    /// \endif
    /// \if EN
    /// <para>Reads and write return failure for invalid counts data.</para>
    /// \endif
    /// </summary>
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
