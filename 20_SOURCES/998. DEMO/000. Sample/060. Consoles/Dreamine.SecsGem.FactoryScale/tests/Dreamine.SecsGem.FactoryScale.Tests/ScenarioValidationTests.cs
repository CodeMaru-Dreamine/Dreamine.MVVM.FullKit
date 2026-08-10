using Dreamine.SecsGem.FactoryScale.Scenarios;

namespace Dreamine.SecsGem.FactoryScale.Tests;

public sealed class ScenarioValidationTests
{
    [Fact]
    public void FaultIsolation_InProcessRequiresOneAdditionalListenerPort()
    {
        var insufficient = new FactoryScenarioRunOptions
        {
            Scenario = "fault-isolation",
            ExecutionMode = "in-process",
            EquipmentCount = 6,
            DisconnectCount = 0,
            PortRangeStart = 30_000,
            PortRangeEnd = 30_005
        };

        var error = Assert.Throws<ArgumentException>(insufficient.Validate);
        Assert.Contains("7 ports", error.Message, StringComparison.Ordinal);
        (insufficient with { PortRangeEnd = 30_006 }).Validate();
    }

    [Fact]
    public void FaultIsolation_MultiProcessDoesNotReserveTheInProcessListener()
    {
        new FactoryScenarioRunOptions
        {
            Scenario = "fault-isolation",
            ExecutionMode = "multi-process",
            EquipmentCount = 6,
            DisconnectCount = 0,
            PortRangeStart = 30_000,
            PortRangeEnd = 30_005
        }.Validate();
    }
}
