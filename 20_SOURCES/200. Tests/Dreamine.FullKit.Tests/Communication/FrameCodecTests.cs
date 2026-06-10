using System.Buffers.Binary;
using System.Text;
using Dreamine.Communication.Core.Framing;

namespace Dreamine.FullKit.Tests.Communication;

public sealed class FrameCodecTests
{
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

    [Fact]
    public async Task LengthPrefixedCodec_ReturnsNullWhenStreamEndsBeforePayloadCompletes()
    {
        var header = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(header, 5);
        using var stream = new MemoryStream(header.Concat(new byte[] { 1, 2 }).ToArray());

        var result = await new LengthPrefixedMessageFrameCodec().ReadFrameAsync(stream);

        Assert.Null(result);
    }

    [Fact]
    public async Task LengthPrefixedCodec_RejectsFramesLargerThanLimit()
    {
        using var stream = new MemoryStream();
        var codec = new LengthPrefixedMessageFrameCodec(maxFrameLength: 2);

        await Assert.ThrowsAsync<InvalidDataException>(
            () => codec.WriteFrameAsync(stream, new byte[] { 1, 2, 3 }));
    }

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

    [Fact]
    public async Task DelimiterCodec_ReturnsBufferedPayloadWhenStreamEndsWithoutDelimiter()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("partial"));

        var result = await new DelimiterMessageFrameCodec("|", Encoding.UTF8, 32)
            .ReadFrameAsync(stream);

        Assert.Equal("partial", Encoding.UTF8.GetString(result!));
    }

    [Fact]
    public async Task DelimiterCodec_RejectsFramesLargerThanLimit()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("abcd"));
        var codec = new DelimiterMessageFrameCodec("|", Encoding.UTF8, maxFrameLength: 3);

        await Assert.ThrowsAsync<InvalidDataException>(() => codec.ReadFrameAsync(stream));
    }
}
