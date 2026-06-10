using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Dreamine.FullKit.Tests.Generators;

public sealed class SourceGeneratorTests
{
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
