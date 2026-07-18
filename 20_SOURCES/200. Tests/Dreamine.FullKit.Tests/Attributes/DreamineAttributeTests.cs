using Dreamine.MVVM.Attributes;

namespace Dreamine.FullKit.Tests.Attributes;

/// <summary>
/// \if KO
/// <para>Dreamine Attribute Tests 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates dreamine attribute tests functionality and related state.</para>
/// \endif
/// </summary>
public sealed class DreamineAttributeTests
{
    /// <summary>
    /// \if KO
    /// <para>Dreamine Command Attribute Stores Configured Values 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the dreamine command attribute stores configured values operation.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>Dreamine Command Attribute Rejects Null Target Method 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the dreamine command attribute rejects null target method operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void DreamineCommandAttribute_RejectsNullTargetMethod()
    {
        Assert.Throws<ArgumentNullException>(() => new DreamineCommandAttribute(null!));
    }

    /// <summary>
    /// \if KO
    /// <para>Optional Name Attributes Expose Configured Names 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the optional name attributes expose configured names operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void OptionalNameAttributes_ExposeConfiguredNames()
    {
        Assert.Equal("Model", new DreamineModelAttribute("Model").PropertyName);
        Assert.Equal("Event", new DreamineEventAttribute("Event").PropertyName);
        Assert.Equal("Name", new DreaminePropertyAttribute("Name").PropertyName);
        var commandAttribute = new DreamineCommandAttribute
        {
            CommandName = "SaveCommand"
        };
        Assert.Null(commandAttribute.TargetMethod);
        Assert.Equal("SaveCommand", commandAttribute.CommandName);
        Assert.Equal("Text", new DreamineModelPropertyAttribute("Text").ModelPropertyName);
    }
}
