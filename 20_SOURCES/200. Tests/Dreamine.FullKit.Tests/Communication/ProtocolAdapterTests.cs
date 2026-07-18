using System.Text;
using Dreamine.Communication.Abstractions.Models;
using Dreamine.Communication.Core.Extensions;
using Dreamine.Communication.Core.Protocols;

namespace Dreamine.FullKit.Tests.Communication;

/// <summary>
/// \if KO
/// <para>Protocol Adapter Tests 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates protocol adapter tests functionality and related state.</para>
/// \endif
/// </summary>
public sealed class ProtocolAdapterTests
{
    /// <summary>
    /// \if KO
    /// <para>Plain Text Protocol Adapter Decodes External Text To Envelope 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the plain text protocol adapter decodes external text to envelope operation.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>Raw Json Protocol Adapter Extracts Route And Name From Json 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the raw json protocol adapter extracts route and name from json operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void RawJsonProtocolAdapter_ExtractsRouteAndNameFromJson()
    {
        var adapter = new RawJsonProtocolAdapter("fallback.route", "Fallback");

        var message = adapter.Decode(Encoding.UTF8.GetBytes("""{"route":"machine.1","name":"Telemetry","value":3}"""));

        Assert.Equal("machine.1", message.Route);
        Assert.Equal("Telemetry", message.Name);
        Assert.Equal("RawJson", message.Headers["Protocol"]);
    }

    /// <summary>
    /// \if KO
    /// <para>Raw Json Protocol Adapter Generates Json When Message Payload Is Empty 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the raw json protocol adapter generates json when message payload is empty operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void RawJsonProtocolAdapter_GeneratesJsonWhenMessagePayloadIsEmpty()
    {
        var adapter = new RawJsonProtocolAdapter();

        var payload = adapter.Encode(new MessageEnvelope { Route = "route", Name = "Name" });
        var json = Encoding.UTF8.GetString(payload);

        Assert.Contains("\"name\":\"Name\"", json);
        Assert.Contains("\"route\":\"route\"", json);
    }

    /// <summary>
    /// \if KO
    /// <para>Message Envelope Extensions Create And Read String Payload 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the message envelope extensions create and read string payload operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void MessageEnvelopeExtensions_CreateAndReadStringPayload()
    {
        var message = MessageEnvelopeExtensions.FromStringPayload("route", "Name", "payload");

        Assert.Equal("route", message.Route);
        Assert.Equal("Name", message.Name);
        Assert.Equal("payload", message.GetPayloadAsString());
    }
}
