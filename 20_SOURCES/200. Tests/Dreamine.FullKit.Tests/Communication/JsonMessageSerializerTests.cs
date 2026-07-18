using System.Text;
using Dreamine.Communication.Abstractions.Models;
using Dreamine.Communication.Core.Serialization;

namespace Dreamine.FullKit.Tests.Communication;

/// <summary>
/// \if KO
/// <para>Json Message Serializer Tests 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates json message serializer tests functionality and related state.</para>
/// \endif
/// </summary>
public sealed class JsonMessageSerializerTests
{
    /// <summary>
    /// \if KO
    /// <para>Serialize Then Deserialize Preserves Envelope Values 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the serialize then deserialize preserves envelope values operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void SerializeThenDeserialize_PreservesEnvelopeValues()
    {
        var serializer = new JsonMessageSerializer();
        var envelope = new MessageEnvelope
        {
            MessageId = "message-1",
            Name = "Telemetry",
            Route = "/machine/1",
            Payload = Encoding.UTF8.GetBytes("payload"),
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "text/plain"
            },
            CreatedAt = new DateTimeOffset(2026, 6, 7, 1, 2, 3, TimeSpan.Zero)
        };

        var data = serializer.Serialize(envelope);
        var result = serializer.Deserialize(data);

        Assert.Equal(envelope.MessageId, result.MessageId);
        Assert.Equal(envelope.Name, result.Name);
        Assert.Equal(envelope.Route, result.Route);
        Assert.Equal(envelope.Payload, result.Payload);
        Assert.Equal(envelope.Headers, result.Headers);
        Assert.Equal(envelope.CreatedAt, result.CreatedAt);
    }

    /// <summary>
    /// \if KO
    /// <para>Deserialize Accepts Case Insensitive Property Names 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the deserialize accepts case insensitive property names operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void Deserialize_AcceptsCaseInsensitivePropertyNames()
    {
        var json = """
            {
              "messageid": "lower-case",
              "name": "Ping",
              "route": "/ping",
              "payload": "AQID",
              "headers": { "A": "B" },
              "createdat": "2026-06-07T00:00:00+00:00"
            }
            """;

        var result = new JsonMessageSerializer().Deserialize(Encoding.UTF8.GetBytes(json));

        Assert.Equal("lower-case", result.MessageId);
        Assert.Equal(new byte[] { 1, 2, 3 }, result.Payload);
        Assert.Equal("B", result.Headers["A"]);
    }
}
