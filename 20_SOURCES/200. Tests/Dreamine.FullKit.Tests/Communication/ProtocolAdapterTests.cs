using System.Text;
using Dreamine.Communication.Abstractions.Models;
using Dreamine.Communication.Core.Extensions;
using Dreamine.Communication.Core.Protocols;

namespace Dreamine.FullKit.Tests.Communication;

public sealed class ProtocolAdapterTests
{
    [Fact]
    public void PlainTextProtocolAdapter_DecodesExternalTextToEnvelope()
    {
        var adapter = new PlainTextProtocolAdapter(Encoding.UTF8, "external.text", "Text");

        var message = adapter.Decode(Encoding.UTF8.GetBytes("hello"));

        Assert.Equal("Text", message.Name);
        Assert.Equal("external.text", message.Route);
        Assert.Equal("hello", message.GetPayloadAsString());
        Assert.Equal("PlainText", message.Headers["Protocol"]);
    }

    [Fact]
    public void RawJsonProtocolAdapter_ExtractsRouteAndNameFromJson()
    {
        var adapter = new RawJsonProtocolAdapter("fallback.route", "Fallback");

        var message = adapter.Decode(Encoding.UTF8.GetBytes("""{"route":"machine.1","name":"Telemetry","value":3}"""));

        Assert.Equal("machine.1", message.Route);
        Assert.Equal("Telemetry", message.Name);
        Assert.Equal("RawJson", message.Headers["Protocol"]);
    }

    [Fact]
    public void RawJsonProtocolAdapter_GeneratesJsonWhenMessagePayloadIsEmpty()
    {
        var adapter = new RawJsonProtocolAdapter();

        var payload = adapter.Encode(new MessageEnvelope { Route = "route", Name = "Name" });
        var json = Encoding.UTF8.GetString(payload);

        Assert.Contains("\"name\":\"Name\"", json);
        Assert.Contains("\"route\":\"route\"", json);
    }

    [Fact]
    public void MessageEnvelopeExtensions_CreateAndReadStringPayload()
    {
        var message = MessageEnvelopeExtensions.FromStringPayload("route", "Name", "payload");

        Assert.Equal("route", message.Route);
        Assert.Equal("Name", message.Name);
        Assert.Equal("payload", message.GetPayloadAsString());
    }
}
