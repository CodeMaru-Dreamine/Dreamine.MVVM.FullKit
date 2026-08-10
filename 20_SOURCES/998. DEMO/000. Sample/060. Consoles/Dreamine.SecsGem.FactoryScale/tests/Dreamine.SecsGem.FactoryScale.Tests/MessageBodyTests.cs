using Dreamine.Secs.Abstractions.Model;
using Dreamine.Secs.Com.Codecs;
using Dreamine.SecsGem.FactoryScale.Scenarios;

namespace Dreamine.SecsGem.FactoryScale.Tests;

public sealed class MessageBodyTests
{
    [Theory]
    [InlineData("normal")]
    [InlineData("busy")]
    [InlineData("trace")]
    [InlineData("soak")]
    public void RateProfiles_ContainTheExactRequestedBinaryPayload(string profile)
    {
        const int requestedBytes = 4_097;
        var body = FactoryMessageBodyFactory.Create(profile, sequence: 2, requestedBytes);
        var payloads = Descendants(body).OfType<SecsBinaryItem>()
            .Where(item => item.BodyLength == requestedBytes)
            .ToArray();

        Assert.Single(payloads);
        Assert.Equal(requestedBytes, payloads[0].Values.Length);
        Assert.True(FactoryMessageBodyFactory.HsmsHeaderLength + new SecsItemCodec().Encode(body).Length <=
                    FactoryMessageBodyFactory.MaximumHsmsFrameLength);
    }

    [Theory]
    [InlineData("normal")]
    [InlineData("busy")]
    [InlineData("trace")]
    [InlineData("soak")]
    [InlineData("large-message")]
    public void MaximumPayloadAccountsForSecsItemAndHsmsHeaderOverhead(string profile)
    {
        var maximum = FactoryMessageBodyFactory.MaximumPayloadBytes(profile);

        Assert.InRange(maximum, 1, FactoryMessageBodyFactory.MaximumSecsItemBodyLength);
        Assert.Equal(FactoryMessageBodyFactory.MaximumHsmsFrameLength,
            FactoryMessageBodyFactory.GetMaximumHsmsFrameLength(profile, maximum));
        Assert.True(FactoryMessageBodyFactory.GetMaximumHsmsFrameLength(profile, maximum + 1) >
                    FactoryMessageBodyFactory.MaximumHsmsFrameLength);
    }

    [Theory]
    [InlineData("factory-normal")]
    [InlineData("factory-busy")]
    [InlineData("trace-burst")]
    [InlineData("soak")]
    [InlineData("large-message")]
    public void ScenarioValidationRejectsPayloadThatWouldExceedItsFrame(string scenario)
    {
        var maximum = FactoryMessageBodyFactory.MaximumPayloadBytesForScenario(scenario);
        var options = new FactoryScenarioRunOptions
        {
            Scenario = scenario,
            EquipmentCount = 1,
            DisconnectCount = 0,
            PortRangeStart = 30_000,
            PortRangeEnd = 30_000,
            MessageBytes = maximum
        };

        options.Validate();
        Assert.Throws<ArgumentOutOfRangeException>(() => (options with { MessageBytes = maximum + 1 }).Validate());
    }

    private static IEnumerable<SecsItem> Descendants(SecsItem item)
    {
        yield return item;
        if (item is not SecsListItem list) yield break;
        foreach (var child in list.Items)
        foreach (var descendant in Descendants(child))
            yield return descendant;
    }
}
