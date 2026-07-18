using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Dreamine.FullKit.Tests.Generators;

/// <summary>
/// \if KO
/// <para>Source Generator Tests 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates source generator tests functionality and related state.</para>
/// \endif
/// </summary>
public sealed class SourceGeneratorTests
{
    /// <summary>
    /// \if KO
    /// <para>Dreamine Command Source Generator Generates Simple Command Property For Partial View Model 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the dreamine command source generator generates simple command property for partial view model operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void DreamineCommandSourceGenerator_GeneratesSimpleCommandPropertyForPartialViewModel()
    {
        var source = """
            using Dreamine.MVVM.Attributes;

            namespace Sample;

            public partial class MainViewModel
            {
                [DreamineCommand]
                private void Save()
                {
                }
            }
            """;

        var runResult = RunGenerator(source, new DreamineCommandSourceGenerator());

        var generated = Assert.Single(runResult.GeneratedSources);
        var text = generated.SourceText.ToString();
        Assert.Contains("SaveCommand", text);
        Assert.Contains("ICommand", text);
        Assert.Contains("new __DreamineGeneratedCommand_Save(Save)", text);
    }

    /// <summary>
    /// \if KO
    /// <para>Dreamine Command Source Generator Generates Forwarding Method For Partial Declaration 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the dreamine command source generator generates forwarding method for partial declaration operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void DreamineCommandSourceGenerator_GeneratesForwardingMethodForPartialDeclaration()
    {
        var source = """
            using Dreamine.MVVM.Attributes;

            namespace Sample;

            public partial class MainViewModel
            {
                private string? Result { get; set; }

                private string Load() => "Loaded";

                [DreamineCommand("Load", BindTo = "Result")]
                private partial void LoadResult();
            }
            """;

        var runResult = RunGenerator(source, new DreamineCommandSourceGenerator());

        var generated = Assert.Single(runResult.GeneratedSources);
        var text = generated.SourceText.ToString();
        Assert.Contains("LoadResultCommand", text);
        Assert.Contains("var __result = Load();", text);
        Assert.Contains("Result = __result;", text);
    }

    /// <summary>
    /// \if KO
    /// <para>Dreamine Command Source Generator Reports Diagnostic For Non Partial Type 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the dreamine command source generator reports diagnostic for non partial type operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void DreamineCommandSourceGenerator_ReportsDiagnosticForNonPartialType()
    {
        var source = """
            using Dreamine.MVVM.Attributes;

            namespace Sample;

            public class MainViewModel
            {
                [DreamineCommand]
                private void Save()
                {
                }
            }
            """;

        var runResult = RunGenerator(source, new DreamineCommandSourceGenerator());

        Assert.Contains(runResult.Diagnostics, diagnostic => diagnostic.Id == "DMCMD002");
    }

    /// <summary>
    /// \if KO
    /// <para>Dreamine Auto Wiring Generator Generates Property For Dreamine Property Field 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the dreamine auto wiring generator generates property for dreamine property field operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void DreamineAutoWiringGenerator_GeneratesPropertyForDreaminePropertyField()
    {
        var source = """
            using Dreamine.MVVM.Attributes;

            namespace Sample;

            public partial class MainViewModel
            {
                [DreamineProperty]
                private string _title = "";

                protected bool SetProperty<T>(ref T field, T value)
                {
                    field = value;
                    return true;
                }
            }
            """;

        var runResult = RunGenerator(source, new DreamineAutoWiringGenerator());

        var generated = Assert.Single(runResult.GeneratedSources);
        Assert.Contains("public string Title", generated.SourceText.ToString());
    }

    /// <summary>
    /// \if KO
    /// <para>Run Generator 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the run generator operation.</para>
    /// \endif
    /// </summary>
    /// <param name="source">
    /// \if KO
    /// <para>source에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for source.</para>
    /// \endif
    /// </param>
    /// <param name="generator">
    /// \if KO
    /// <para>generator에 사용할 <c>IIncrementalGenerator</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IIncrementalGenerator</c> value used for generator.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Run Generator 작업에서 생성한 <c>GeneratorRunResult</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>GeneratorRunResult</c> result produced by the run generator operation.</para>
    /// \endif
    /// </returns>
    private static GeneratorRunResult RunGenerator(string source, IIncrementalGenerator generator)
    {
        var compilation = CSharpCompilation.Create(
            "GeneratorTests",
            new[] { CSharpSyntaxTree.ParseText(source) },
            GetReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

        return driver.GetRunResult().Results.Single();
    }

    /// <summary>
    /// \if KO
    /// <para>References 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the references value.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Get References 작업에서 생성한 <c>IEnumerable&lt;MetadataReference&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IEnumerable&lt;MetadataReference&gt;</c> result produced by the get references operation.</para>
    /// \endif
    /// </returns>
    private static IEnumerable<MetadataReference> GetReferences()
    {
        var trustedPlatformAssemblies = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
            ?.Split(Path.PathSeparator)
            ?? Array.Empty<string>();

        return trustedPlatformAssemblies
            .Concat(new[] { typeof(DreamineCommandAttribute).Assembly.Location })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(path => MetadataReference.CreateFromFile(path));
    }
}
