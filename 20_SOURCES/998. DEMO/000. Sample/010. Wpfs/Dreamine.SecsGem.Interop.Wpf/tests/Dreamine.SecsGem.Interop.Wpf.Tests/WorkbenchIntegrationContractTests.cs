using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows.Input;
using Dreamine.Communication.Abstractions.Enums;
using Dreamine.MVVM.Core.AutoRegistration;
using Dreamine.MVVM.Core.DependencyInjection;
using Dreamine.Secs.Abstractions.Diagnostics;
using Dreamine.Secs.Abstractions.Enums;
using Dreamine.Secs.Abstractions.Hsms;
using Dreamine.Secs.Abstractions.Interfaces;
using Dreamine.Secs.Abstractions.Model;
using Dreamine.Secs.Abstractions.Options;
using Dreamine.Secs.Abstractions.Providers;
using Dreamine.Secs.Com;
using Dreamine.SecsGem.Interop.Runtime.Evidence;
using Dreamine.SecsGem.Interop.Runtime.Logging;
using Dreamine.SecsGem.Interop.Runtime.Profiles;
using Dreamine.SecsGem.Interop.Runtime.Scenarios;
using Dreamine.SecsGem.Interop.Wpf.Events;
using Dreamine.SecsGem.Interop.Wpf.Managers;
using Dreamine.SecsGem.Interop.Wpf.Models;
using Dreamine.SecsGem.Interop.Wpf.ViewModels;
using Xunit;

namespace Dreamine.SecsGem.Interop.Wpf.Tests;

public sealed class WorkbenchIntegrationContractTests
{
    [Fact]
    public void ConnectionSettingsAppliesValidatedProfileAndReportsImmutableChanges()
    {
        var settings = new ConnectionSettings();
        var before = settings.ToProfile();
        var profile = before with
        {
            Role = SecsRole.Equipment,
            Mode = SecsConnectionMode.Passive,
            Host = "0.0.0.0",
            Port = 7100,
            SessionId = 7,
            AutoReconnect = false,
            Timers = new ConnectionTimerProfileV1(30, 8, 9, 10, 11),
            LogPolicyId = ConnectionLogPolicyIds.ExcludedV1
        };

        var diff = settings.ApplyProfile(profile);

        Assert.Equal(ConnectionProfileApplyDisposition.RecreateRequired, diff.Disposition);
        Assert.Contains(nameof(SingleConnectionProfileV1.Host), diff.RecreateRequiredChanges);
        Assert.Contains(nameof(SingleConnectionProfileV1.LogPolicyId), diff.ImmediateChanges);
        Assert.Equal(profile, settings.ToProfile());
    }

    [Fact]
    public void WorkbenchCommandsAreGeneratorBackedAndDistinctFromSyntheticRunAll()
    {
        var expected = new[]
        {
            "LoadConnectionProfileCommand",
            "LoadEvidenceManifestCommand",
            "LoadMessageTemplateCatalogCommand",
            "LoadScenarioFileCommand",
            "OpenWireLogCommand",
            "RunScenarioFileCommand"
        };

        var actual = typeof(MainWindowViewModel)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => typeof(ICommand).IsAssignableFrom(property.PropertyType))
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.All(expected, command => Assert.Contains(command, actual));
        Assert.Contains("RunAllCommand", actual);
    }

    [Fact]
    public void WireViewerIsNamedForExactHsmsWireAndDoesNotAdvertiseSemanticLogsAsRaw()
    {
        var xaml = File.ReadAllText(FindRepositoryFile(
            "20_SOURCES", "998. DEMO", "000. Sample", "010. Wpfs",
            "Dreamine.SecsGem.Interop.Wpf", "Views", "MainWindow.xaml"));

        Assert.Contains("Header=\"HSMS Wire\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("SECS1 / HSMS Raw", xaml, StringComparison.Ordinal);
        Assert.Contains("OpenWireLogCommand", xaml, StringComparison.Ordinal);
        Assert.Contains("RunScenarioFileCommand", xaml, StringComparison.Ordinal);
        Assert.Contains("LoadEvidenceManifestCommand", xaml, StringComparison.Ordinal);
        Assert.Contains("Evidence Recorded → Manual Review → Verified", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void EquipmentResponderSelectorShowsTheFrozenProfileAndExplicitDemoOnlyFallback()
    {
        var xaml = File.ReadAllText(FindRepositoryFile(
            "20_SOURCES", "998. DEMO", "000. Sample", "010. Wpfs",
            "Dreamine.SecsGem.Interop.Wpf", "Views", "MainWindow.xaml"));
        var viewModel = File.ReadAllText(FindRepositoryFile(
            "20_SOURCES", "998. DEMO", "000. Sample", "010. Wpfs",
            "Dreamine.SecsGem.Interop.Wpf", "ViewModels", "MainWindowViewModel.cs"));

        Assert.Contains("E30-0611 derived subset profile v1 (Demo)", viewModel, StringComparison.Ordinal);
        Assert.Contains("Educational basic responder (Demo-only, not GEM)", viewModel, StringComparison.Ordinal);
        Assert.Contains("ItemsSource=\"{Binding EquipmentResponderProfileOptions}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("DisplayMemberPath=\"Value\"", xaml, StringComparison.Ordinal);
        Assert.Contains("SelectedValuePath=\"Key\"", xaml, StringComparison.Ordinal);
        Assert.Contains(
            "SelectedValue=\"{Binding SelectedEquipmentResponderProfile, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}\"",
            xaml,
            StringComparison.Ordinal);
    }

    [Fact]
    public void HeadlessStartupDoesNotDiscardAnySelfTestOrScenarioTask()
    {
        var source = File.ReadAllText(FindRepositoryFile(
            "20_SOURCES", "998. DEMO", "000. Sample", "010. Wpfs",
            "Dreamine.SecsGem.Interop.Wpf", "App.Bootstrapper.cs"));

        Assert.DoesNotContain("_ = RunHeadless", source, StringComparison.Ordinal);
        Assert.Contains("private static async void LaunchHeadlessProcess", source, StringComparison.Ordinal);
    }

    [Fact]
    public void HeadlessScenarioArgumentsRequireBothScenarioAndProfile()
    {
        var missingProfile = App.ParseScenarioArguments(["app.exe", "--scenario", "run.json"]);
        var complete = App.ParseScenarioArguments([
            "app.exe", "--scenario", "run.json", "--profile", "profile.json",
            "--output", "result.json"]);
        var upperCase = App.ParseScenarioArguments([
            "app.exe", "--SCENARIO", "run.json", "--PROFILE", "profile.json"]);

        Assert.False(missingProfile.IsValid);
        Assert.Equal(ScenarioExitCodesV1.Invalid, missingProfile.ExitCode);
        Assert.True(complete.IsValid);
        Assert.Equal(Path.GetFullPath("run.json"), complete.ScenarioPath);
        Assert.Equal(Path.GetFullPath("profile.json"), complete.ProfilePath);
        Assert.Equal(Path.GetFullPath("result.json"), complete.OutputPath);
        Assert.True(upperCase.IsValid);
    }

    [Fact]
    public async Task HeadlessScenarioResultIsWrittenAsOneStructuredAtomicDocument()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"dreamine-headless-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "result.json");
        try
        {
            var result = new ScenarioRunResultV1(
                ScenarioRunStatusV1.Passed,
                ScenarioExitCodesV1.Passed,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                []);

            await App.WriteScenarioResultAsync(path, result, CancellationToken.None);

            await using var stream = File.OpenRead(path);
            using var loaded = await JsonDocument.ParseAsync(stream);
            Assert.Equal(
                (int)ScenarioRunStatusV1.Passed,
                loaded.RootElement.GetProperty("Status").GetInt32());
            Assert.Equal(0, loaded.RootElement.GetProperty("TotalStepCount").GetInt32());
            Assert.False(loaded.RootElement.TryGetProperty("Steps", out _));
            Assert.False(loaded.RootElement.TryGetProperty("ErrorMessage", out _));
            Assert.Empty(Directory.EnumerateFiles(directory, "*.tmp", SearchOption.TopDirectoryOnly));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task HeadlessScenarioResultUsesClosedProjectionAndRejectsHostileFreeText()
    {
        const string canary = "PRIVATE-PROVIDER-CANARY-7CBB8E";
        var directory = Path.Combine(Path.GetTempPath(), $"dreamine-headless-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "result.json");
        var hostilePath = Path.Combine(directory, canary, "customer-profile.json");
        try
        {
            var started = DateTimeOffset.UtcNow.AddSeconds(-1);
            var result = new ScenarioRunResultV1(
                ScenarioRunStatusV1.Failed,
                ScenarioExitCodesV1.Failed,
                started,
                DateTimeOffset.UtcNow,
                [
                    new ScenarioStepResultV1(
                        hostilePath,
                        ScenarioStepStatusV1.Failed,
                        started,
                        DateTimeOffset.UtcNow,
                        $"hostile_{canary}",
                        $"Provider detail at {hostilePath}: {canary}")
                ],
                $"hostile_{canary}",
                $"Scenario {hostilePath} failed in provider {canary}",
                DroppedInboundMessageCount: 3);

            await App.WriteScenarioResultAsync(path, result, CancellationToken.None);

            var json = await File.ReadAllTextAsync(path);
            Assert.DoesNotContain(canary, json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(hostilePath, json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("ErrorMessage", json, StringComparison.Ordinal);
            Assert.DoesNotContain("Steps", json, StringComparison.Ordinal);
            using var document = JsonDocument.Parse(json);
            var names = document.RootElement.EnumerateObject()
                .Select(property => property.Name)
                .ToHashSet(StringComparer.Ordinal);
            Assert.Equal(
                new HashSet<string>(StringComparer.Ordinal)
                {
                    "Status",
                    "ExitCode",
                    "StartedAtUtc",
                    "CompletedAtUtc",
                    "TotalStepCount",
                    "PassedStepCount",
                    "FailedStepCount",
                    "TimedOutStepCount",
                    "CancelledStepCount",
                    "DroppedInboundMessageCount",
                    "ErrorCode"
                },
                names);
            Assert.Equal("failed", document.RootElement.GetProperty("ErrorCode").GetString());
            Assert.Equal(1, document.RootElement.GetProperty("TotalStepCount").GetInt32());
            Assert.Equal(1, document.RootElement.GetProperty("FailedStepCount").GetInt32());
            Assert.Equal(3, document.RootElement.GetProperty("DroppedInboundMessageCount").GetInt64());
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void RuntimeProjectDoesNotReferenceTheWpfWorkbench()
    {
        var project = File.ReadAllText(FindRepositoryFile(
            "20_SOURCES", "998. DEMO", "000. Sample", "060. Consoles",
            "Dreamine.SecsGem.Interop.Runtime", "Dreamine.SecsGem.Interop.Runtime.csproj"));

        Assert.DoesNotContain("Dreamine.SecsGem.Interop.Wpf", project, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WireViewerParsesDirectionAndUtcRangeIntoStreamingFilter()
    {
        var filter = MainWindowEvent.CreateWireLogFilter(
            "inbound",
            "1",
            "2",
            "0xAABBCCDD",
            "connection-1",
            "2026-08-12T12:00:00Z",
            "2026-08-12T13:00:00+00:00",
            errorsOnly: false);
        var matching = WireRecord(
            HsmsWireDirection.Inbound,
            DateTimeOffset.Parse("2026-08-12T12:30:00Z"));
        var wrongDirection = matching with { Direction = HsmsWireDirection.Outbound };

        Assert.True(filter.Matches(matching));
        Assert.False(filter.Matches(wrongDirection));
        Assert.Throws<FormatException>(() => MainWindowEvent.CreateWireLogFilter(
            "Inbound", "", "", "", "", "2026-08-12T12:00:00+09:00", "", false));
        Assert.Throws<ArgumentException>(() => MainWindowEvent.CreateWireLogFilter(
            "", "", "", "", "", "2026-08-12T13:00:00Z", "2026-08-12T12:00:00Z", false));
    }

    [Fact]
    public async Task HeadlessScenarioUsesProfileLoggingAndRecordsLifecycleThroughDispose()
    {
        var fixture = await CreateHeadlessFixtureAsync(ConnectionLogPolicyIds.ExcludedV1);
        try
        {
            ObservedHeadlessSession? observed = null;
            HsmsWireCaptureMode? captureMode = null;
            var exitCode = await App.RunHeadlessScenarioAsync(
                fixture.Arguments,
                CancellationToken.None,
                (profile, options) =>
                {
                    captureMode = options.DefaultCaptureMode;
                    return observed = CreateObservedSession(profile, options);
                },
                static (log, root) => new WireLogManager(log, root));

            var result = await ReadScenarioResultAsync(fixture.OutputPath);
            Assert.Equal(ScenarioExitCodesV1.Passed, exitCode);
            Assert.Equal(ScenarioRunStatusV1.Passed, result.Status);
            Assert.Equal(HsmsWireCaptureMode.Excluded, captureMode);
            Assert.True(observed?.Disposed);
            Assert.NotEmpty(Directory.EnumerateFiles(
                Path.Combine(fixture.Directory, "dreamine-wire-logs"),
                "*.jsonl",
                SearchOption.TopDirectoryOnly));
        }
        finally
        {
            Directory.Delete(fixture.Directory, recursive: true);
        }
    }

    [Fact]
    public async Task HeadlessScenarioWireLogAlwaysRedactsProfileEndpoint()
    {
        const string hostileHost = "private-endpoint-canary-7cbb8e.internal";
        const int hostilePort = 64231;
        var fixture = await CreateHeadlessFixtureAsync(
            ConnectionLogPolicyIds.HeaderOnlyV1,
            hostileHost,
            hostilePort);
        try
        {
            var exitCode = await App.RunHeadlessScenarioAsync(
                fixture.Arguments,
                CancellationToken.None,
                (profile, options) => CreateObservedSession(profile, options),
                static (log, root) => new WireLogManager(log, root));

            Assert.Equal(ScenarioExitCodesV1.Passed, exitCode);
            var wireLog = string.Join(
                Environment.NewLine,
                Directory.EnumerateFiles(
                        Path.Combine(fixture.Directory, "dreamine-wire-logs"),
                        "*.jsonl",
                        SearchOption.TopDirectoryOnly)
                    .Select(File.ReadAllText));
            Assert.Contains("redacted", wireLog, StringComparison.Ordinal);
            Assert.DoesNotContain(hostileHost, wireLog, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain($"{hostileHost}:{hostilePort}", wireLog, StringComparison.OrdinalIgnoreCase);
            using var firstRecord = JsonDocument.Parse(
                File.ReadLines(Directory.EnumerateFiles(
                    Path.Combine(fixture.Directory, "dreamine-wire-logs"),
                    "*.jsonl",
                    SearchOption.TopDirectoryOnly).First()).First());
            Assert.Equal("redacted", firstRecord.RootElement.GetProperty("endpoint").GetString());
        }
        finally
        {
            Directory.Delete(fixture.Directory, recursive: true);
        }
    }

    [Theory]
    [InlineData(true, 0, "cleanup_failed")]
    [InlineData(false, 1, "wire_log_unhealthy")]
    public async Task HeadlessScenarioCannotReportPassedWhenCleanupOrWireHealthFails(
        bool throwOnDispose,
        long forcedDrops,
        string expectedErrorCode)
    {
        var fixture = await CreateHeadlessFixtureAsync(ConnectionLogPolicyIds.HeaderOnlyV1);
        try
        {
            var exitCode = await App.RunHeadlessScenarioAsync(
                fixture.Arguments,
                CancellationToken.None,
                (profile, options) => CreateObservedSession(
                    profile,
                    options,
                    throwOnDispose,
                    forcedDrops),
                static (log, root) => new WireLogManager(log, root));

            var result = await ReadScenarioResultAsync(fixture.OutputPath);
            Assert.Equal(ScenarioExitCodesV1.Failed, exitCode);
            Assert.Equal(ScenarioRunStatusV1.Failed, result.Status);
            Assert.Equal(expectedErrorCode, result.ErrorCode);
        }
        finally
        {
            Directory.Delete(fixture.Directory, recursive: true);
        }
    }

    [Fact]
    public async Task PublicEvidenceManifestAttachmentDoesNotPromoteRecordedEvidenceToPassed()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"dreamine-manifest-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "public-evidence.json");
        var hash = new string('A', 64);
        var manifest = new InteropEvidenceManifest(
            InteropEvidenceManifest.CurrentSchemaVersion,
            "RUN-RECORDED",
            "operator",
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch.AddMinutes(1),
            "SEComSimulator",
            "4.0",
            hash,
            EvidenceReviewState.EvidenceRecorded,
            new WireLogHealth(0, 0, 2, true, null),
            [
                new EvidenceArtifact("Dreamine log", EvidenceArtifactKind.DreamineLog, hash),
                new EvidenceArtifact("Counterpart log", EvidenceArtifactKind.CounterpartLog, hash)
            ],
            [new EvidenceChecklistItem("manual", "Operator performed the documented action", true)]);
        await new EvidenceManifestStore().SaveAsync(path, manifest);
        await using var viewModel = CreateViewModel();
        try
        {
            await viewModel.Event.LoadEvidenceManifestAsync(path, CancellationToken.None);

            Assert.Equal("Evidence Recorded", viewModel.EvidenceReviewState);
            Assert.Equal("Not eligible for external PASS review.", viewModel.EvidenceEligibilityStatus);
            Assert.Contains(
                viewModel.EvidenceEligibilityReasons,
                reason => reason.Contains("Manual verification", StringComparison.Ordinal));
            Assert.Contains("never assigns Passed", viewModel.EvidenceManifestStatus, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task GeneratedWpfCompositionResolvesSingletonsOnceAndDisposesOnce()
    {
        var container = new DreamineContainer();
        var registrar = new AutoRegistrationService(
            new AssemblyTypeScanner(),
            new NamingConventionAutoRegistrationFilter());
        registrar.RegisterAll(typeof(MainWindowViewModel).Assembly, container);

        var viewModel = container.Resolve<MainWindowViewModel>();
        var connection = container.Resolve<ConnectionManager>();
        var messages = container.Resolve<MessageManager>();
        var scenarios = container.Resolve<ScenarioManager>();
        var log = container.Resolve<InteropLogManager>();
        var wireLog = container.Resolve<WireLogManager>();

        Assert.Same(viewModel, container.Resolve<MainWindowViewModel>());
        Assert.Same(connection, container.Resolve<ConnectionManager>());
        Assert.Same(messages, container.Resolve<MessageManager>());
        Assert.Same(scenarios, container.Resolve<ScenarioManager>());
        Assert.Same(log, container.Resolve<InteropLogManager>());
        Assert.Same(wireLog, container.Resolve<WireLogManager>());
        Assert.Same(connection, ReadPrivateField<ConnectionManager>(viewModel.Event, "_connection"));
        Assert.Same(messages, ReadPrivateField<MessageManager>(viewModel.Event, "_messages"));
        Assert.Same(wireLog, ReadPrivateField<WireLogManager>(viewModel.Event, "_wireLog"));
        Assert.Same(wireLog, ReadPrivateField<WireLogManager>(connection, "_wireLog"));

        await viewModel.DisposeAsync();
        await viewModel.DisposeAsync();
        Assert.Equal(1, ReadPrivateField<int>(viewModel.Event, "_disposed"));
        Assert.Equal(1, ReadPrivateField<int>(messages, "_disposed"));
        container.Dispose();
        container.Dispose();

        Assert.Equal(1, ReadPrivateField<int>(viewModel.Event, "_disposed"));
        Assert.Equal(1, ReadPrivateField<int>(messages, "_disposed"));
    }

    private static WireLogRecord WireRecord(HsmsWireDirection direction, DateTimeOffset timestamp) => new(
        WireLogRecord.CurrentSchemaVersion,
        1,
        1,
        timestamp,
        direction,
        "equipment-1",
        "connection-1",
        "127.0.0.1:7100",
        0,
        14,
        10,
        0,
        1,
        2,
        false,
        0,
        0,
        0xAABBCCDD,
        WireBodyCaptureMode.HeaderOnly,
        0,
        false,
        new byte[10],
        null,
        null,
        null);

    private static async Task<HeadlessFixture> CreateHeadlessFixtureAsync(
        string logPolicyId,
        string? host = null,
        int? port = null)
    {
        var directory = Path.Combine(Path.GetTempPath(), $"dreamine-headless-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var profilePath = Path.Combine(directory, "profile.json");
        var scenarioPath = Path.Combine(directory, "scenario.json");
        var outputPath = Path.Combine(directory, "result.json");
        var profile = new SingleConnectionProfileV1 { LogPolicyId = logPolicyId };
        if (host is not null || port is not null)
            profile = profile with
            {
                Host = host ?? profile.Host,
                Port = port ?? profile.Port
            };
        await ConnectionProfileStore.Create().SaveAsync(profilePath, profile);
        await new ScenarioFileStoreV1().SaveAsync(
            scenarioPath,
            new ScenarioDefinitionV1
            {
                Id = "headless-lifecycle",
                Name = "Headless lifecycle",
                Steps =
                [
                    new DelayScenarioStepV1
                    {
                        Id = "bounded-delay",
                        Target = ScenarioExecutionBindingV1.CurrentConnectionTarget,
                        DelayMilliseconds = 1,
                        TimeoutMilliseconds = 1_000
                    }
                ]
            });
        return new HeadlessFixture(
            directory,
            outputPath,
            ["app.exe", "--scenario", scenarioPath, "--profile", profilePath, "--output", outputPath]);
    }

    private static ObservedHeadlessSession CreateObservedSession(
        SingleConnectionProfileV1 profile,
        HsmsWireObservationOptions observationOptions,
        bool throwOnDispose = false,
        long forcedDrops = 0)
    {
        var settings = new ConnectionSettings();
        settings.ApplyProfile(profile);
        settings.WireObservation = observationOptions;
        var provider = new DreamineSecsCommunicationProvider(_ => settings.ToOptions());
        var inner = provider.CreateSession(new SecsConnectionOptions
        {
            ProviderKey = SecsProviderKeys.Dreamine,
            Role = profile.Role,
            Mode = profile.Mode
        });
        return new ObservedHeadlessSession(inner, throwOnDispose, forcedDrops);
    }

    private static async Task<HeadlessScenarioResultProjection> ReadScenarioResultAsync(string path)
    {
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<HeadlessScenarioResultProjection>(stream) ??
            throw new InvalidDataException("The headless result was empty.");
    }

    private static MainWindowViewModel CreateViewModel()
    {
        var log = new InteropLogManager();
        var wireLog = new WireLogManager(log);
        var connection = new ConnectionManager(log, wireLog);
        return new MainWindowViewModel(
            connection,
            new MessageManager(connection, log),
            new ScenarioManager(log),
            log,
            new ResultExportManager(),
            new SimulatorProcessManager(),
            new EquipmentSidecarProcessManager(),
            new FileDialogManager(),
            wireLog);
    }

    private static T ReadPrivateField<T>(object owner, string name) =>
        (T)(owner.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(owner) ??
            throw new MissingFieldException(owner.GetType().FullName, name));

    private sealed record HeadlessFixture(
        string Directory,
        string OutputPath,
        string[] Arguments);

    private sealed record HeadlessScenarioResultProjection(
        ScenarioRunStatusV1 Status,
        int ExitCode,
        int TotalStepCount,
        string? ErrorCode);

    private sealed class ObservedHeadlessSession(
        ISecsMessageSession inner,
        bool throwOnDispose,
        long forcedDrops) : ISecsMessageSession
    {
        public string ProviderKey => inner.ProviderKey;
        public ConnectionState State => inner.State;
        public SecsConnectionIdentity ConnectionIdentity => inner.ConnectionIdentity;
        public HsmsConnectionState HsmsState => inner.HsmsState;
        public ISecsPrimaryDispatcher PrimaryDispatcher => inner.PrimaryDispatcher;
        public bool IsWireObservationEnabled => inner.IsWireObservationEnabled;
        public long DroppedWireObservationCount =>
            inner.DroppedWireObservationCount + forcedDrops;
        public bool Disposed { get; private set; }

        public event EventHandler<SecsMessage>? MessageReceived;
        public event EventHandler<SecsDiagnosticEvent>? DiagnosticReceived;
        public event EventHandler<SecsSessionStateChangedEventArgs>? StateChanged;

        public Task ConnectAsync(CancellationToken cancellationToken = default) =>
            inner.ConnectAsync(cancellationToken);
        public Task DisconnectAsync(CancellationToken cancellationToken = default) =>
            inner.DisconnectAsync(cancellationToken);
        public Task SelectAsync(CancellationToken cancellationToken = default) =>
            inner.SelectAsync(cancellationToken);
        public Task DeselectAsync(CancellationToken cancellationToken = default) =>
            inner.DeselectAsync(cancellationToken);
        public Task LinktestAsync(CancellationToken cancellationToken = default) =>
            inner.LinktestAsync(cancellationToken);
        public Task SeparateAsync(CancellationToken cancellationToken = default) =>
            inner.SeparateAsync(cancellationToken);
        public SecsSystemBytes AllocateSystemBytes() => inner.AllocateSystemBytes();
        public Task SendAsync(SecsMessage message, CancellationToken cancellationToken = default) =>
            inner.SendAsync(message, cancellationToken);
        public Task<SecsMessage> SendPrimaryAsync(
            SecsMessage message,
            CancellationToken cancellationToken = default) =>
            inner.SendPrimaryAsync(message, cancellationToken);
        public Task SendAsync(
            SecsStream stream,
            SecsFunction function,
            SecsItem? item = null,
            CancellationToken cancellationToken = default) =>
            inner.SendAsync(stream, function, item, cancellationToken);
        public Task<SecsMessage> RequestAsync(
            SecsDialogueDefinition dialogue,
            SecsItem? item = null,
            CancellationToken cancellationToken = default) =>
            inner.RequestAsync(dialogue, item, cancellationToken);
        public IAsyncEnumerable<HsmsWireObservation> ReadWireObservationsAsync(
            CancellationToken cancellationToken = default) =>
            inner.ReadWireObservationsAsync(cancellationToken);

        public async ValueTask DisposeAsync()
        {
            DiagnosticReceived?.Invoke(
                this,
                new SecsDiagnosticEvent(
                    SecsDiagnosticKind.ConnectionClosed,
                    "Headless test disposal.",
                    HsmsState));
            StateChanged?.Invoke(
                this,
                new SecsSessionStateChangedEventArgs(
                    State,
                    ConnectionState.Disconnected,
                    HsmsState,
                    HsmsConnectionState.NotConnected,
                    ConnectionIdentity));
            await inner.DisposeAsync();
            Disposed = true;
            if (throwOnDispose) throw new InvalidOperationException("Injected cleanup failure.");
        }

        // The delay-only scenario never raises application messages; these helpers retain explicit
        // event ownership without forwarding unrelated callbacks from the native test session.
        private void RaiseMessage(SecsMessage message) => MessageReceived?.Invoke(this, message);
    }

    private static string FindRepositoryFile(params string[] segments)
    {
        var cursor = new DirectoryInfo(AppContext.BaseDirectory);
        while (cursor is not null)
        {
            var candidate = Path.Combine([cursor.FullName, .. segments]);
            if (File.Exists(candidate)) return candidate;
            cursor = cursor.Parent;
        }

        throw new FileNotFoundException($"Could not locate {Path.Combine(segments)} from the test output directory.");
    }
}
