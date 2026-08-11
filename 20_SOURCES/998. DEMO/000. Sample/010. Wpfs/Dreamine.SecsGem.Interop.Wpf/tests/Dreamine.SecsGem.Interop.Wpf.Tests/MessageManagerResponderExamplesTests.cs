using System.Globalization;
using Dreamine.Secs.Abstractions.Model;
using Dreamine.SecsGem.Interop.Wpf.Managers;
using Xunit;

namespace Dreamine.SecsGem.Interop.Wpf.Tests;

public sealed class MessageManagerResponderExamplesTests
{
    [Fact]
    public void S1F3ReadsTypedRequestChildrenAndBuildsFlatS1F4List()
    {
        var request = Primary(1, 3, new SecsListItem(
            new SecsInt16Item(1),
            new SecsUInt16Item(99),
            new SecsInt16Item(2)));

        var response = Assert.IsType<SecsListItem>(MessageManager.BuildBasicResponseItem(request));

        Assert.Equal(
            ["COMMUNICATING", "UNKNOWN", "ONLINE"],
            response.Items.Cast<SecsAsciiItem>().Select(static item => item.Value));
    }

    [Fact]
    public void S1F11BuildsNestedS1F12List()
    {
        var response = Assert.IsType<SecsListItem>(MessageManager.BuildBasicResponseItem(Primary(1, 11)));

        Assert.Equal(2, response.Count);
        var firstRow = Assert.IsType<SecsListItem>(response.Items[0]);
        var secondRow = Assert.IsType<SecsListItem>(response.Items[1]);
        Assert.Equal(3, firstRow.Count);
        Assert.Equal(3, secondRow.Count);
        Assert.IsType<SecsInt16Item>(firstRow.Items[0]);
        Assert.Equal("CommunicationState", Assert.IsType<SecsAsciiItem>(firstRow.Items[1]).Value);
        Assert.Equal("ControlState", Assert.IsType<SecsAsciiItem>(secondRow.Items[1]).Value);
    }

    [Theory]
    [InlineData(15)]
    [InlineData(17)]
    public void StateRequestBuildsSingleByteBinaryAcknowledge(byte function)
    {
        var response = Assert.IsType<SecsBinaryItem>(MessageManager.BuildBasicResponseItem(Primary(1, function)));

        Assert.Equal(new byte[] { 0 }, response.Values.ToArray());
    }

    [Fact]
    public void S2F17BuildsAsciiScalarWithoutSurroundingList()
    {
        var response = Assert.IsType<SecsAsciiItem>(MessageManager.BuildBasicResponseItem(Primary(2, 17)));

        Assert.True(DateTime.TryParseExact(
            response.Value,
            "yyyyMMddHHmmssff",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out _));
    }

    [Fact]
    public void UnsupportedMessageReturnsNull()
    {
        Assert.Null(MessageManager.BuildBasicResponseItem(Primary(2, 41)));
    }

    [Fact]
    public void PrimaryWithoutWBitDoesNotCreateSecondaryBody()
    {
        Assert.Null(MessageManager.BuildBasicResponseItem(Primary(1, 3, replyExpected: false)));
    }

    private static SecsMessage Primary(
        byte stream,
        byte function,
        SecsItem? item = null,
        bool replyExpected = true) =>
        new(
            new SecsSessionId(0),
            new SecsStream(stream),
            new SecsFunction(function),
            replyExpected,
            new SecsSystemBytes(1),
            item);
}
