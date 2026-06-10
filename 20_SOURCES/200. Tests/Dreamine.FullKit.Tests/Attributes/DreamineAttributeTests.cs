using Dreamine.MVVM.Attributes;

namespace Dreamine.FullKit.Tests.Attributes;

public sealed class DreamineAttributeTests
{
    [Fact]
    public void DreamineCommandAttribute_StoresConfiguredValues()
    {
        var attribute = new DreamineCommandAttribute("Service.Load")
        {
            BindTo = "Result",
            CommandName = "LoadCommand"
        };

        Assert.Equal("Service.Load", attribute.TargetMethod);
        Assert.Equal("Result", attribute.BindTo);
        Assert.Equal("LoadCommand", attribute.CommandName);
    }

    [Fact]
    public void DreamineCommandAttribute_RejectsNullTargetMethod()
    {
        Assert.Throws<ArgumentNullException>(() => new DreamineCommandAttribute(null!));
    }

    [Fact]
    public void OptionalNameAttributes_ExposeConfiguredNames()
    {
        Assert.Equal("Model", new DreamineModelAttribute("Model").PropertyName);
        Assert.Equal("Event", new DreamineEventAttribute("Event").PropertyName);
        Assert.Equal("Name", new DreaminePropertyAttribute("Name").PropertyName);
        Assert.Equal("SaveCommand", new RelayCommandAttribute("SaveCommand").CommandName);
        Assert.Equal("Text", new DreamineModelPropertyAttribute("Text").ModelPropertyName);
    }
}
