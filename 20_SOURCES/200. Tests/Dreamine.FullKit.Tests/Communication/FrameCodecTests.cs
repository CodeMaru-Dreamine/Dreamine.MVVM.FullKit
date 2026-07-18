using System.Buffers.Binary;
using System.Text;
using Dreamine.Communication.Core.Framing;

namespace Dreamine.FullKit.Tests.Communication;

/// <summary>
/// \if KO
/// <para>Frame Codec Tests 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates frame codec tests functionality and related state.</para>
/// \endif
/// </summary>
public sealed class FrameCodecTests
{
    /// <summary>
    /// \if KO
    /// <para>Length Prefixed Codec Round Trips Payload 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the length prefixed codec round trips payload operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Length Prefixed Codec Round Trips Payload 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the length prefixed codec round trips payload operation.</para>
    /// \endif
    /// </returns>
    [Fact]
    public async Task LengthPrefixedCodec_RoundTripsPayload()
    {
        var codec = new LengthPrefixedMessageFrameCodec();
        var payload = Encoding.UTF8.GetBytes("hello frame");
        using var stream = new MemoryStream();

        await codec.WriteFrameAsync(stream, payload);
        stream.Position = 0;

        var result = await codec.ReadFrameAsync(stream);

        Assert.Equal(payload, result);
    }

    /// <summary>
    /// \if KO
    /// <para>Length Prefixed Codec Returns Null When Stream Ends Before Payload Completes 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the length prefixed codec returns null when stream ends before payload completes operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Length Prefixed Codec Returns Null When Stream Ends Before Payload Completes 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the length prefixed codec returns null when stream ends before payload completes operation.</para>
    /// \endif
    /// </returns>
    [Fact]
    public async Task LengthPrefixedCodec_ReturnsNullWhenStreamEndsBeforePayloadCompletes()
    {
        var header = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(header, 5);
        using var stream = new MemoryStream(header.Concat(new byte[] { 1, 2 }).ToArray());

        var result = await new LengthPrefixedMessageFrameCodec().ReadFrameAsync(stream);

        Assert.Null(result);
    }

    /// <summary>
    /// \if KO
    /// <para>Length Prefixed Codec Rejects Frames Larger Than Limit 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the length prefixed codec rejects frames larger than limit operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Length Prefixed Codec Rejects Frames Larger Than Limit 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the length prefixed codec rejects frames larger than limit operation.</para>
    /// \endif
    /// </returns>
    [Fact]
    public async Task LengthPrefixedCodec_RejectsFramesLargerThanLimit()
    {
        using var stream = new MemoryStream();
        var codec = new LengthPrefixedMessageFrameCodec(maxFrameLength: 2);

        await Assert.ThrowsAsync<InvalidDataException>(
            () => codec.WriteFrameAsync(stream, new byte[] { 1, 2, 3 }));
    }

    /// <summary>
    /// \if KO
    /// <para>Delimiter Codec Round Trips Payload Without Delimiter 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the delimiter codec round trips payload without delimiter operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Delimiter Codec Round Trips Payload Without Delimiter 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the delimiter codec round trips payload without delimiter operation.</para>
    /// \endif
    /// </returns>
    [Fact]
    public async Task DelimiterCodec_RoundTripsPayloadWithoutDelimiter()
    {
        var codec = new DelimiterMessageFrameCodec("|", Encoding.UTF8, maxFrameLength: 32);
        var payload = Encoding.UTF8.GetBytes("one message");
        using var stream = new MemoryStream();

        await codec.WriteFrameAsync(stream, payload);
        stream.Position = 0;

        var result = await codec.ReadFrameAsync(stream);

        Assert.Equal(payload, result);
    }

    /// <summary>
    /// \if KO
    /// <para>Delimiter Codec Returns Buffered Payload When Stream Ends Without Delimiter 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the delimiter codec returns buffered payload when stream ends without delimiter operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Delimiter Codec Returns Buffered Payload When Stream Ends Without Delimiter 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the delimiter codec returns buffered payload when stream ends without delimiter operation.</para>
    /// \endif
    /// </returns>
    [Fact]
    public async Task DelimiterCodec_ReturnsBufferedPayloadWhenStreamEndsWithoutDelimiter()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("partial"));

        var result = await new DelimiterMessageFrameCodec("|", Encoding.UTF8, 32)
            .ReadFrameAsync(stream);

        Assert.Equal("partial", Encoding.UTF8.GetString(result!));
    }

    /// <summary>
    /// \if KO
    /// <para>Delimiter Codec Preserves Bytes After Delimiter For Next Frame 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the delimiter codec preserves bytes after delimiter for next frame operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Delimiter Codec Preserves Bytes After Delimiter For Next Frame 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the delimiter codec preserves bytes after delimiter for next frame operation.</para>
    /// \endif
    /// </returns>
    [Fact]
    public async Task DelimiterCodec_PreservesBytesAfterDelimiterForNextFrame()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("first|second|"));
        var codec = new DelimiterMessageFrameCodec("|", Encoding.UTF8, 32);

        var first = await codec.ReadFrameAsync(stream);
        var second = await codec.ReadFrameAsync(stream);

        Assert.Equal("first", Encoding.UTF8.GetString(first!));
        Assert.Equal("second", Encoding.UTF8.GetString(second!));
    }

    /// <summary>
    /// \if KO
    /// <para>Delimiter Codec Rejects Frames Larger Than Limit 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the delimiter codec rejects frames larger than limit operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Delimiter Codec Rejects Frames Larger Than Limit 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the delimiter codec rejects frames larger than limit operation.</para>
    /// \endif
    /// </returns>
    [Fact]
    public async Task DelimiterCodec_RejectsFramesLargerThanLimit()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("abcd"));
        var codec = new DelimiterMessageFrameCodec("|", Encoding.UTF8, maxFrameLength: 3);

        await Assert.ThrowsAsync<InvalidDataException>(() => codec.ReadFrameAsync(stream));
    }
}
