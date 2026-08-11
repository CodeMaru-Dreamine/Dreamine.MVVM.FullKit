using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Input;
using Dreamine.MVVM.Attributes;
using Dreamine.SecsGem.Interop.Wpf.Events;
using Dreamine.SecsGem.Interop.Wpf.Managers;
using Dreamine.SecsGem.Interop.Wpf.Models;
using Dreamine.SecsGem.Interop.Wpf.ViewModels;
using Xunit;

namespace Dreamine.SecsGem.Interop.Wpf.Tests;

public sealed class MainWindowViewModelContractTests
{
    private static readonly string[] ExpectedCommands =
    [
        "AddChildItemCommand",
        "AddEquipmentCommand",
        "AddRootItemCommand",
        "BroadcastEquipmentCommand",
        "BrowseEquipmentSidecarCommand",
        "BrowseEquipmentSidecarEvidenceCommand",
        "BrowseExportCommand",
        "BrowseSimulatorCommand",
        "CancelCommand",
        "ClearLogCommand",
        "ConnectAllEquipmentCommand",
        "ConnectCommand",
        "ConnectSelectedEquipmentCommand",
        "DisconnectAllEquipmentCommand",
        "DisconnectCommand",
        "DisconnectSelectedEquipmentCommand",
        "EnableResponderCommand",
        "ExportCommand",
        "ExportEquipmentConfigurationCommand",
        "ForceStopEquipmentSidecarCommand",
        "ImportEquipmentConfigurationCommand",
        "ImportFactoryResultCommand",
        "LaunchSimulatorCommand",
        "LinktestAllEquipmentCommand",
        "LinktestCommand",
        "LinktestSelectedEquipmentCommand",
        "MarkWaitingCommand",
        "ReconnectCommand",
        "ReconnectSelectedEquipmentCommand",
        "RemoveEquipmentCommand",
        "RemoveItemCommand",
        "ResetScenariosCommand",
        "RunAllCommand",
        "RunAllEquipmentScenariosCommand",
        "RunMultiEquipmentSelfTestCommand",
        "RunSelectedCommand",
        "RunSelectedEquipmentScenarioCommand",
        "RunSelfTestCommand",
        "SelectCommand",
        "SendCommand",
        "SendSelectedEquipmentCommand",
        "SeparateCommand",
        "StartEquipmentSidecarCommand"
    ];

    [Fact]
    public async Task FacadeExposesGeneratedEventPropertiesAndAllCommandBindings()
    {
        await using var viewModel = CreateViewModel();

        Assert.IsType<MainWindowEvent>(viewModel.Event);

        var attributedFields = typeof(MainWindowViewModel)
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .Where(field => field.GetCustomAttribute<DreaminePropertyAttribute>() is not null)
            .ToArray();
        Assert.True(attributedFields.Length >= 25);
        foreach (var field in attributedFields)
        {
            var normalized = field.Name.TrimStart('_');
            var propertyName = char.ToUpperInvariant(normalized[0]) + normalized[1..];
            Assert.NotNull(typeof(MainWindowViewModel).GetProperty(propertyName));
        }

        var commandProperties = typeof(MainWindowViewModel)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => typeof(ICommand).IsAssignableFrom(property.PropertyType))
            .OrderBy(property => property.Name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(ExpectedCommands, commandProperties.Select(property => property.Name));
        Assert.All(commandProperties, property => Assert.NotNull(property.GetValue(viewModel)));
    }

    [Fact]
    public async Task GeneratedStateAndCanExecuteRemainCoherentAcrossHarnessModes()
    {
        await using var viewModel = CreateViewModel();
        var changed = new List<string?>();
        viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        viewModel.Stream = 7;
        Assert.Equal((byte)7, viewModel.Stream);
        Assert.Contains(nameof(MainWindowViewModel.Stream), changed);

        Assert.True(viewModel.ConnectCommand.CanExecute(null));
        Assert.False(viewModel.AddEquipmentCommand.CanExecute(null));

        viewModel.HostMode = "Multi Equipment Host";

        Assert.True(viewModel.IsMultiEquipmentMode);
        Assert.Equal(1, viewModel.SelectedMainTabIndex);
        Assert.False(viewModel.ConnectCommand.CanExecute(null));
        Assert.True(viewModel.AddEquipmentCommand.CanExecute(null));
    }

    [Fact]
    public async Task SidecarDiagnosticsProjectHeaderWithoutTreatingNormalStderrAsError()
    {
        var executable = Path.Combine(Path.GetTempPath(), $"sidecar-{Guid.NewGuid():N}.exe");
        var evidence = Path.Combine(Path.GetTempPath(), $"sidecar-{Guid.NewGuid():N}.jsonl");
        File.WriteAllText(executable, string.Empty);
        try
        {
            var factory = new FakeProcessFactory();
            var sidecar = new EquipmentSidecarProcessManager(factory);
            await using var viewModel = CreateViewModel(sidecar);
            viewModel.EquipmentSidecarExecutablePath = executable;
            viewModel.EquipmentSidecarEvidencePath = evidence;

            await viewModel.Event.StartEquipmentSidecar();

            Assert.Equal("Listening", viewModel.TcpState);
            Assert.Equal("NotConnected", viewModel.HsmsState);

            factory.Process.Emit(
                EquipmentSidecarOutputStream.StandardError,
                "HSMS StateChanged; state=Selected; frameLength=17.");

            Assert.Equal("Connected", viewModel.TcpState);
            Assert.Equal("Selected", viewModel.HsmsState);
            Assert.False(viewModel.DisconnectCommand.CanExecute(null));
            Assert.False(viewModel.SelectCommand.CanExecute(null));
            Assert.False(viewModel.LinktestCommand.CanExecute(null));
            Assert.False(viewModel.SeparateCommand.CanExecute(null));
            Assert.False(viewModel.ReconnectCommand.CanExecute(null));
            Assert.False(viewModel.SendCommand.CanExecute(null));
            var diagnostic = Assert.Single(
                viewModel.DiagnosticsLogs,
                entry => entry.Summary.StartsWith("HSMS StateChanged", StringComparison.Ordinal));
            Assert.Equal("Info", diagnostic.Level);
            Assert.Equal("None", viewModel.CurrentError);

            var exited = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            sidecar.Exited += (_, _) => exited.TrySetResult();
            factory.Process.CompleteExit(0);
            await exited.Task.WaitAsync(TimeSpan.FromSeconds(5));

            Assert.Equal("Disconnected", viewModel.TcpState);
            Assert.Equal("NotConnected", viewModel.HsmsState);
        }
        finally
        {
            File.Delete(executable);
            if (File.Exists(evidence))
            {
                File.Delete(evidence);
            }
        }
    }

    [Fact]
    public async Task GeneratedSendCommandReportsMalformedItemInsteadOfSwallowingIt()
    {
        await using var viewModel = CreateViewModel();
        viewModel.HsmsState = "Selected";
        viewModel.RootItems.Add(new SecsItemNodeViewModel(SecsItemEditorFormat.UInt8)
        {
            Value = "not-a-byte"
        });
        var errorObserved = new TaskCompletionSource<string>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(MainWindowViewModel.CurrentError) &&
                viewModel.CurrentError != "None")
            {
                errorObserved.TrySetResult(viewModel.CurrentError);
            }
        };

        Assert.True(viewModel.SendCommand.CanExecute(null));
        viewModel.SendCommand.Execute(null);
        var error = await errorObserved.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.NotEqual("None", error);
        Assert.Equal(error, viewModel.CurrentError);
        Assert.Equal(error, viewModel.LastError);
        Assert.Equal(error, viewModel.StatusText);
        Assert.Contains(
            viewModel.DiagnosticsLogs,
            entry => entry.Level == "Error" &&
                     entry.Category == "Command" &&
                     entry.Message == nameof(FormatException));
    }

    private static MainWindowViewModel CreateViewModel(
        EquipmentSidecarProcessManager? sidecar = null)
    {
        var log = new InteropLogManager();
        var connection = new ConnectionManager(log);
        return new MainWindowViewModel(
            connection,
            new MessageManager(connection, log),
            new ScenarioManager(log),
            log,
            new ResultExportManager(),
            new SimulatorProcessManager(),
            sidecar ?? new EquipmentSidecarProcessManager(),
            new FileDialogManager());
    }

    private sealed class FakeProcessFactory : IEquipmentSidecarProcessFactory
    {
        public FakeProcess Process { get; } = new();

        public IEquipmentSidecarProcess Create(ProcessStartInfo startInfo)
        {
            Process.StartInfo = startInfo;
            return Process;
        }
    }

    private sealed class FakeProcess : IEquipmentSidecarProcess
    {
        private readonly TaskCompletionSource _exit =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _exitCode;

        public event Action<EquipmentSidecarOutputStream, string>? OutputReceived;
        public ProcessStartInfo? StartInfo { get; set; }
        public int Id => 5353;
        public bool HasExited => _exit.Task.IsCompleted;
        public int ExitCode => HasExited
            ? _exitCode
            : throw new InvalidOperationException("Process is still running.");

        public void Start()
        {
        }

        public void Kill(bool entireProcessTree) => CompleteExit(-1);
        public Task WaitForExitAndDrainAsync() => _exit.Task;
        public void Dispose()
        {
        }

        public void Emit(EquipmentSidecarOutputStream stream, string line) =>
            OutputReceived?.Invoke(stream, line);

        public void CompleteExit(int exitCode)
        {
            _exitCode = exitCode;
            _exit.TrySetResult();
        }
    }
}
