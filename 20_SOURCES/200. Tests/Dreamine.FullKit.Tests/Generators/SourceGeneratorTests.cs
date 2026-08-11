using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;
using System.Windows.Input;

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
        Assert.Contains("NotifySaveCommandCanExecuteChanged", text);
    }

    /// <summary>
    /// Task 반환 command가 비동기 실행 및 CanExecute 알림 코드를 생성하는지 확인합니다.
    /// </summary>
    [Fact]
    public void DreamineCommandSourceGenerator_GeneratesAsyncTaskCommandWithCanExecuteNotifier()
    {
        var source = """
            using System.Threading.Tasks;
            using Dreamine.MVVM.Attributes;

            namespace Sample;

            public partial class MainViewModel
            {
                private bool CanRun() => true;

                [DreamineCommand(CanExecute = nameof(CanRun))]
                private Task RunAsync() => Task.CompletedTask;
            }
            """;

        var (runResult, outputCompilation) = RunGeneratorWithCompilation(
            source,
            new DreamineCommandSourceGenerator());

        var generated = Assert.Single(runResult.GeneratedSources);
        var text = generated.SourceText.ToString();
        Assert.DoesNotContain(runResult.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(outputCompilation.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Contains("Func<global::System.Threading.Tasks.Task> _execute", text);
        Assert.Contains("await _execute().ConfigureAwait(true);", text);
        Assert.Contains("_isExecuting", text);
        Assert.Contains("CanRun", text);
        Assert.Contains("public void NotifyRunAsyncCommandCanExecuteChanged()", text);
        Assert.Contains("public Exception? RunAsyncCommandLastException", text);
        Assert.Contains("public event EventHandler<Exception>? RunAsyncCommandExecutionFailed", text);
        Assert.Contains("command.RaiseCanExecuteChanged();", text);
    }

    /// <summary>
    /// ValueTask forwarding command가 대상 호출을 반환하고 Task delegate로 변환되는지 확인합니다.
    /// </summary>
    [Fact]
    public void DreamineCommandSourceGenerator_GeneratesValueTaskForwardingCommand()
    {
        var source = """
            using System.Threading.Tasks;
            using Dreamine.MVVM.Attributes;

            namespace Sample;

            public sealed class MainEvent
            {
                public ValueTask ExecuteAsync() => ValueTask.CompletedTask;
            }

            public partial class MainViewModel
            {
                private MainEvent Event { get; } = new();

                [DreamineCommand("Event.ExecuteAsync", CommandName = "ExecuteCommand")]
                private partial ValueTask Execute();
            }
            """;

        var (runResult, outputCompilation) = RunGeneratorWithCompilation(
            source,
            new DreamineCommandSourceGenerator());

        var generated = Assert.Single(runResult.GeneratedSources);
        var text = generated.SourceText.ToString();
        Assert.DoesNotContain(runResult.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(outputCompilation.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Contains("return Event.ExecuteAsync();", text);
        Assert.Contains("() => Execute().AsTask()", text);
        Assert.Contains("public ICommand ExecuteCommand", text);
        Assert.Contains("public void NotifyExecuteCommandCanExecuteChanged()", text);
        Assert.Contains("public Exception? ExecuteCommandLastException", text);
        Assert.Contains("public event EventHandler<Exception>? ExecuteCommandExecutionFailed", text);
    }

    /// <summary>
    /// Task forwarding command가 대상 Task를 그대로 반환하는지 확인합니다.
    /// </summary>
    [Fact]
    public void DreamineCommandSourceGenerator_GeneratesTaskForwardingReturn()
    {
        var source = """
            using System.Threading.Tasks;
            using Dreamine.MVVM.Attributes;

            namespace Sample;

            public sealed class MainEvent
            {
                public Task ConnectAsync() => Task.CompletedTask;
            }

            public partial class MainViewModel
            {
                private MainEvent Event { get; } = new();

                [DreamineCommand("Event.ConnectAsync")]
                private partial Task Connect();
            }
            """;

        var (runResult, outputCompilation) = RunGeneratorWithCompilation(
            source,
            new DreamineCommandSourceGenerator());

        var generated = Assert.Single(runResult.GeneratedSources);
        var text = generated.SourceText.ToString();
        Assert.DoesNotContain(runResult.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(outputCompilation.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Contains("return Event.ConnectAsync();", text);
        Assert.Contains("new __DreamineGeneratedCommand_Connect(Connect)", text);
    }

    /// <summary>
    /// Task command 실패가 public failure surface로 관찰되고 재실행 가능한지 확인합니다.
    /// </summary>
    [Fact]
    public async Task DreamineCommandSourceGenerator_TaskFailureIsObservableAndCommandCanRunAgain()
    {
        var source = """
            using System;
            using System.Threading.Tasks;
            using Dreamine.MVVM.Attributes;

            namespace Sample;

            public partial class MainViewModel
            {
                public int Attempts { get; private set; }

                [DreamineCommand]
                private Task RunAsync()
                {
                    Attempts++;
                    return Attempts == 1
                        ? Task.FromException(new InvalidOperationException("task-failure"))
                        : Task.CompletedTask;
                }
            }
            """;

        var (instance, type) = CompileGeneratedType(source, "Sample.MainViewModel");
        var command = GetCommand(instance, type, "RunAsyncCommand");
        var failure = SubscribeToFailure(instance, type, "RunAsyncCommandExecutionFailed");

        await ExecuteAndWaitAsync(command);
        var observed = await failure.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal("task-failure", observed.Message);
        Assert.Same(observed, type.GetProperty("RunAsyncCommandLastException")!.GetValue(instance));
        Assert.Equal(1, type.GetProperty("Attempts")!.GetValue(instance));
        Assert.True(command.CanExecute(null));

        await ExecuteAndWaitAsync(command);

        Assert.Equal(2, type.GetProperty("Attempts")!.GetValue(instance));
        Assert.True(command.CanExecute(null));
    }

    /// <summary>
    /// ValueTask command 실패가 public failure surface로 관찰되고 재실행 가능한지 확인합니다.
    /// </summary>
    [Fact]
    public async Task DreamineCommandSourceGenerator_ValueTaskFailureIsObservableAndCommandCanRunAgain()
    {
        var source = """
            using System;
            using System.Threading.Tasks;
            using Dreamine.MVVM.Attributes;

            namespace Sample;

            public partial class MainViewModel
            {
                public int Attempts { get; private set; }

                [DreamineCommand]
                private ValueTask RunAsync()
                {
                    Attempts++;
                    return Attempts == 1
                        ? new ValueTask(Task.FromException(new InvalidOperationException("value-task-failure")))
                        : ValueTask.CompletedTask;
                }
            }
            """;

        var (instance, type) = CompileGeneratedType(source, "Sample.MainViewModel");
        var command = GetCommand(instance, type, "RunAsyncCommand");
        var failure = SubscribeToFailure(instance, type, "RunAsyncCommandExecutionFailed");

        await ExecuteAndWaitAsync(command);
        var observed = await failure.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal("value-task-failure", observed.Message);
        Assert.Same(observed, type.GetProperty("RunAsyncCommandLastException")!.GetValue(instance));
        Assert.Equal(1, type.GetProperty("Attempts")!.GetValue(instance));
        Assert.True(command.CanExecute(null));

        await ExecuteAndWaitAsync(command);

        Assert.Equal(2, type.GetProperty("Attempts")!.GetValue(instance));
        Assert.True(command.CanExecute(null));
    }

    /// <summary>
    /// 최초 및 최종 CanExecuteChanged subscriber 예외에도 command 실행 상태가 복구되는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task DreamineCommandSourceGenerator_ThrowingCanExecuteSubscriberDoesNotBlockReentry()
    {
        var source = """
            using System.Threading.Tasks;
            using Dreamine.MVVM.Attributes;

            namespace Sample;

            public partial class MainViewModel
            {
                public int Attempts { get; private set; }

                [DreamineCommand]
                private Task RunAsync()
                {
                    Attempts++;
                    return Task.CompletedTask;
                }
            }
            """;

        var (instance, type) = CompileGeneratedType(source, "Sample.MainViewModel");
        var command = GetCommand(instance, type, "RunAsyncCommand");
        var failure = SubscribeToFailure(instance, type, "RunAsyncCommandExecutionFailed");
        command.CanExecuteChanged += (_, _) => throw new InvalidOperationException("observer-failure");

        await ExecuteAndWaitAsync(command);
        var observed = await failure.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal("observer-failure", observed.Message);
        Assert.Equal(1, type.GetProperty("Attempts")!.GetValue(instance));
        Assert.True(command.CanExecute(null));

        await ExecuteAndWaitAsync(command);

        Assert.Equal(2, type.GetProperty("Attempts")!.GetValue(instance));
        Assert.True(command.CanExecute(null));
    }

    /// <summary>
    /// 모든 generated support surface 충돌이 DMCMD006으로 보고되고 command property는 DMCMD004를 유지하는지 확인합니다.
    /// </summary>
    [Fact]
    public void DreamineCommandSourceGenerator_ReportsGeneratedSupportMemberCollisions()
    {
        var source = """
            using System;
            using System.Threading.Tasks;
            using Dreamine.MVVM.Attributes;

            namespace Sample;

            public partial class NotifierCollision
            {
                public void NotifySaveCommandCanExecuteChanged() { }
                [DreamineCommand] private void Save() { }
            }

            public partial class FieldCollision
            {
                private object? _saveCommand;
                [DreamineCommand] private void Save() { }
            }

            public partial class HelperCollision
            {
                private sealed class __DreamineGeneratedCommand_Save { }
                [DreamineCommand] private void Save() { }
            }

            public partial class LastExceptionCollision
            {
                public Exception? RunAsyncCommandLastException => null;
                [DreamineCommand] private Task RunAsync() => Task.CompletedTask;
            }

            public partial class FailureEventCollision
            {
                public event EventHandler<Exception>? RunAsyncCommandExecutionFailed;
                [DreamineCommand] private Task RunAsync() => Task.CompletedTask;
            }

            public partial class CommandPropertyCollision
            {
                public object SaveCommand => new();
                [DreamineCommand] private void Save() { }
            }
            """;

        var (runResult, outputCompilation) = RunGeneratorWithCompilation(
            source,
            new DreamineCommandSourceGenerator());

        Assert.Empty(runResult.GeneratedSources);
        Assert.Contains(runResult.Diagnostics, diagnostic =>
            diagnostic.Id == "DMCMD006" &&
            diagnostic.GetMessage().Contains("NotifySaveCommandCanExecuteChanged", StringComparison.Ordinal));
        Assert.Contains(runResult.Diagnostics, diagnostic =>
            diagnostic.Id == "DMCMD006" &&
            diagnostic.GetMessage().Contains("_saveCommand", StringComparison.Ordinal));
        Assert.Contains(runResult.Diagnostics, diagnostic =>
            diagnostic.Id == "DMCMD006" &&
            diagnostic.GetMessage().Contains("__DreamineGeneratedCommand_Save", StringComparison.Ordinal));
        Assert.Contains(runResult.Diagnostics, diagnostic =>
            diagnostic.Id == "DMCMD006" &&
            diagnostic.GetMessage().Contains("RunAsyncCommandLastException", StringComparison.Ordinal));
        Assert.Contains(runResult.Diagnostics, diagnostic =>
            diagnostic.Id == "DMCMD006" &&
            diagnostic.GetMessage().Contains("RunAsyncCommandExecutionFailed", StringComparison.Ordinal));
        Assert.Contains(runResult.Diagnostics, diagnostic => diagnostic.Id == "DMCMD004");
        Assert.DoesNotContain(outputCompilation.GetDiagnostics(), diagnostic =>
            diagnostic.Severity == DiagnosticSeverity.Error);
    }

    /// <summary>
    /// 같은 타입의 command 후보들이 서로의 generated support name을 선점하면 DMCMD006을 보고하는지 확인합니다.
    /// </summary>
    [Fact]
    public void DreamineCommandSourceGenerator_ReportsCandidateToCandidateSupportCollision()
    {
        var source = """
            using Dreamine.MVVM.Attributes;

            namespace Sample;

            public partial class MainViewModel
            {
                [DreamineCommand(CommandName = "Alpha")]
                private void First() { }

                [DreamineCommand(CommandName = "NotifyAlphaCanExecuteChanged")]
                private void Second() { }
            }
            """;

        var (runResult, outputCompilation) = RunGeneratorWithCompilation(
            source,
            new DreamineCommandSourceGenerator());

        Assert.Empty(runResult.GeneratedSources);
        Assert.Contains(runResult.Diagnostics, diagnostic =>
            diagnostic.Id == "DMCMD006" &&
            diagnostic.GetMessage().Contains("NotifyAlphaCanExecuteChanged", StringComparison.Ordinal));
        Assert.DoesNotContain(outputCompilation.GetDiagnostics(), diagnostic =>
            diagnostic.Severity == DiagnosticSeverity.Error);
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
        return RunGeneratorWithCompilation(source, generator).RunResult;
    }

    /// <summary>
    /// Generator를 실행하고 생성 결과가 반영된 compilation을 함께 반환합니다.
    /// </summary>
    private static (GeneratorRunResult RunResult, Compilation OutputCompilation) RunGeneratorWithCompilation(
        string source,
        IIncrementalGenerator generator)
    {
        var compilation = CSharpCompilation.Create(
            "GeneratorTests_" + Guid.NewGuid().ToString("N"),
            new[] { CSharpSyntaxTree.ParseText(source) },
            GetReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var outputCompilation,
            out _);

        return (driver.GetRunResult().Results.Single(), outputCompilation);
    }

    /// <summary>
    /// 생성 코드를 emit하여 지정 타입의 인스턴스와 타입을 반환합니다.
    /// </summary>
    private static (object Instance, Type Type) CompileGeneratedType(string source, string typeName)
    {
        var (runResult, outputCompilation) = RunGeneratorWithCompilation(
            source,
            new DreamineCommandSourceGenerator());
        Assert.DoesNotContain(runResult.Diagnostics, diagnostic =>
            diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(outputCompilation.GetDiagnostics(), diagnostic =>
            diagnostic.Severity == DiagnosticSeverity.Error);

        using var assemblyStream = new MemoryStream();
        var emitResult = outputCompilation.Emit(assemblyStream);
        Assert.True(
            emitResult.Success,
            string.Join(Environment.NewLine, emitResult.Diagnostics.Select(diagnostic => diagnostic.ToString())));

        var assembly = Assembly.Load(assemblyStream.ToArray());
        var type = assembly.GetType(typeName, throwOnError: true)!;
        return (Activator.CreateInstance(type)!, type);
    }

    /// <summary>
    /// 생성된 public command property를 가져옵니다.
    /// </summary>
    private static ICommand GetCommand(object instance, Type type, string propertyName)
    {
        return Assert.IsAssignableFrom<ICommand>(type.GetProperty(propertyName)!.GetValue(instance));
    }

    /// <summary>
    /// 생성된 failure event를 Task로 관찰합니다.
    /// </summary>
    private static TaskCompletionSource<Exception> SubscribeToFailure(
        object instance,
        Type type,
        string eventName)
    {
        var source = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        EventHandler<Exception> handler = (_, exception) => source.TrySetResult(exception);
        type.GetEvent(eventName)!.AddEventHandler(instance, handler);
        return source;
    }

    /// <summary>
    /// command를 실행하고 최종 CanExecuteChanged가 실행 가능 상태를 알릴 때까지 기다립니다.
    /// </summary>
    private static async Task ExecuteAndWaitAsync(ICommand command)
    {
        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        EventHandler? handler = null;
        handler = (_, _) =>
        {
            if (!command.CanExecute(null))
            {
                return;
            }

            command.CanExecuteChanged -= handler;
            completion.TrySetResult(true);
        };

        command.CanExecuteChanged += handler;
        command.Execute(null);
        await completion.Task.WaitAsync(TimeSpan.FromSeconds(5));
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
