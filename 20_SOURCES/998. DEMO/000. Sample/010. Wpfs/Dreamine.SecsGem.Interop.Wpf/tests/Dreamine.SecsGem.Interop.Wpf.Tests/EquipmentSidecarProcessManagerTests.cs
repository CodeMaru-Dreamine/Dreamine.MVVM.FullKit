using System.Diagnostics;
using System.IO;
using Dreamine.SecsGem.Interop.Wpf.Managers;
using Dreamine.SecsGem.Interop.Wpf.Models;
using Xunit;

namespace Dreamine.SecsGem.Interop.Wpf.Tests;

public sealed class EquipmentSidecarProcessManagerTests
{
    [Fact]
    public void ExecutableStartInfoUsesArgumentListAndHiddenRedirectedStreams()
    {
        var executable = CreateTemporaryFile(".exe");
        var evidence = Path.Combine(Path.GetTempPath(), $"sidecar evidence {Guid.NewGuid():N}.jsonl");
        try
        {
            var startInfo = EquipmentSidecarProcessManager.CreateStartInfo(Options(executable, evidence));

            Assert.Equal(Path.GetFullPath(executable), startInfo.FileName);
            Assert.Equal(Path.GetDirectoryName(Path.GetFullPath(executable)), startInfo.WorkingDirectory);
            Assert.False(startInfo.UseShellExecute);
            Assert.True(startInfo.CreateNoWindow);
            Assert.Equal(ProcessWindowStyle.Hidden, startInfo.WindowStyle);
            Assert.True(startInfo.RedirectStandardOutput);
            Assert.True(startInfo.RedirectStandardError);
            Assert.Equal(new[]
            {
                "--bind", "127.0.0.1",
                "--port", "7100",
                "--session-id", "7",
                "--connection-limit", "2",
                "--request-host-time",
                "--evidence", Path.GetFullPath(evidence),
                "--telemetry-stdout"
            }, startInfo.ArgumentList);
            Assert.True(string.IsNullOrEmpty(startInfo.Arguments));
        }
        finally { File.Delete(executable); }
    }

    [Fact]
    public void AssemblyStartInfoUsesDotnetAndPreservesTheAssemblyPathAsOneArgument()
    {
        var assembly = CreateTemporaryFile(".dll");
        var evidence = Path.Combine(Path.GetTempPath(), $"sidecar-{Guid.NewGuid():N}.jsonl");
        try
        {
            var startInfo = EquipmentSidecarProcessManager.CreateStartInfo(Options(assembly, evidence) with
            {
                RequestHostTime = false,
                TelemetryStdout = false
            });

            Assert.Equal("dotnet", startInfo.FileName);
            Assert.Equal(Path.GetFullPath(assembly), startInfo.ArgumentList[0]);
            Assert.DoesNotContain("--request-host-time", startInfo.ArgumentList);
            Assert.DoesNotContain("--telemetry-stdout", startInfo.ArgumentList);
            Assert.Equal(Path.GetFullPath(evidence), startInfo.ArgumentList[^1]);
        }
        finally { File.Delete(assembly); }
    }

    [Fact]
    public void InvalidConfigurationIsRejectedBeforeAProcessIsCreated()
    {
        var executable = CreateTemporaryFile(".exe");
        var unsupported = CreateTemporaryFile(".txt");
        var evidence = Path.Combine(Path.GetTempPath(), $"sidecar-{Guid.NewGuid():N}.jsonl");
        try
        {
            var valid = Options(executable, evidence);

            Assert.Throws<ArgumentOutOfRangeException>(() => (valid with { Port = 0 }).Validate());
            Assert.Throws<ArgumentOutOfRangeException>(() => (valid with { SessionId = 32768 }).Validate());
            Assert.Throws<ArgumentOutOfRangeException>(() => (valid with { ConnectionLimit = 0 }).Validate());
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                (valid with { NaturalExitGracePeriod = TimeSpan.FromMinutes(2) }).Validate());
            Assert.Throws<ArgumentException>(() => (valid with { BindAddress = " " }).Validate());
            Assert.Throws<ArgumentException>(() => (valid with { EvidencePath = " " }).Validate());
            Assert.Throws<ArgumentException>(() => (valid with { ExecutablePath = unsupported }).Validate());
            Assert.Throws<FileNotFoundException>(() =>
                (valid with { ExecutablePath = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.exe") }).Validate());
        }
        finally
        {
            File.Delete(executable);
            File.Delete(unsupported);
        }
    }

    [Fact]
    public async Task DuplicateStartIsRejectedWhileOutputAndNaturalExitAreMonitored()
    {
        var executable = CreateTemporaryFile(".exe");
        var factory = new FakeProcessFactory();
        await using var manager = new EquipmentSidecarProcessManager(factory);
        var output = new List<EquipmentSidecarOutputEventArgs>();
        var states = new List<EquipmentSidecarState>();
        var exited = new TaskCompletionSource<EquipmentSidecarExitedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        manager.OutputReceived += (_, _) => throw new InvalidOperationException("expected callback failure");
        manager.OutputReceived += (_, value) => output.Add(value);
        manager.StateChanged += (_, value) => states.Add(value.CurrentState);
        manager.Exited += (_, value) => exited.TrySetResult(value);

        try
        {
            var options = Options(executable, TemporaryEvidencePath());
            await manager.StartAsync(options);

            await Assert.ThrowsAsync<InvalidOperationException>(() => manager.StartAsync(options));
            Assert.Equal(1, factory.CreateCount);
            Assert.True(manager.IsRunning);
            Assert.Equal(4242, manager.ProcessId);

            factory.Process.Emit(EquipmentSidecarOutputStream.StandardOutput, "out-line");
            factory.Process.Emit(EquipmentSidecarOutputStream.StandardError, "error-line");
            factory.Process.CompleteExit(0);
            var exit = await exited.Task.WaitAsync(TimeSpan.FromSeconds(2));

            Assert.Equal(new[] { "out-line", "error-line" }, output.Select(static value => value.Line));
            Assert.Equal(new[]
            {
                EquipmentSidecarOutputStream.StandardOutput,
                EquipmentSidecarOutputStream.StandardError
            }, output.Select(static value => value.Stream));
            Assert.Equal(new[]
            {
                EquipmentSidecarState.Starting,
                EquipmentSidecarState.Running,
                EquipmentSidecarState.Exited
            }, states);
            Assert.Equal(4242, exit.ProcessId);
            Assert.Equal(0, exit.ExitCode);
            Assert.False(exit.ForceStopped);
            Assert.False(manager.IsRunning);
            Assert.Null(manager.ProcessId);
            Assert.Equal(0, manager.LastExitCode);
        }
        finally { File.Delete(executable); }
    }

    [Fact]
    public async Task ForceStopKillsOnlyTheOwnedProcessTreeAndWaitsForItsExit()
    {
        var executable = CreateTemporaryFile(".exe");
        var factory = new FakeProcessFactory { ExitCodeWhenKilled = 137 };
        await using var manager = new EquipmentSidecarProcessManager(factory);
        var exited = new TaskCompletionSource<EquipmentSidecarExitedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        manager.Exited += (_, value) => exited.TrySetResult(value);

        try
        {
            await manager.StartAsync(Options(executable, TemporaryEvidencePath()));

            Assert.True(await manager.ForceStopAsync());
            var exit = await exited.Task.WaitAsync(TimeSpan.FromSeconds(2));

            Assert.Equal(1, factory.Process.KillCount);
            Assert.True(factory.Process.LastKillIncludedProcessTree);
            Assert.True(exit.ForceStopped);
            Assert.Equal(137, exit.ExitCode);
            Assert.False(await manager.ForceStopAsync());
        }
        finally { File.Delete(executable); }
    }

    [Fact]
    public async Task DisposeIsIdempotentPrefersNaturalExitAndRejectsFutureStarts()
    {
        var executable = CreateTemporaryFile(".exe");
        var factory = new FakeProcessFactory();
        var manager = new EquipmentSidecarProcessManager(factory);
        try
        {
            var options = Options(executable, TemporaryEvidencePath()) with
            {
                NaturalExitGracePeriod = TimeSpan.Zero
            };
            await manager.StartAsync(options);

            var firstDispose = manager.DisposeAsync().AsTask();
            var secondDispose = manager.DisposeAsync().AsTask();
            await Task.WhenAll(firstDispose, secondDispose).WaitAsync(TimeSpan.FromSeconds(2));

            Assert.Equal(1, factory.Process.KillCount);
            Assert.Equal(1, factory.Process.DisposeCount);
            Assert.Equal(EquipmentSidecarState.Disposed, manager.State);
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.StartAsync(options));
        }
        finally
        {
            await manager.DisposeAsync();
            File.Delete(executable);
        }
    }

    [Fact]
    public async Task DisposeDoesNotKillAProcessThatAlreadyExitedNaturally()
    {
        var executable = CreateTemporaryFile(".exe");
        var factory = new FakeProcessFactory();
        var manager = new EquipmentSidecarProcessManager(factory);
        var exited = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        manager.Exited += (_, _) => exited.TrySetResult();
        try
        {
            await manager.StartAsync(Options(executable, TemporaryEvidencePath()));
            factory.Process.CompleteExit(0);
            await exited.Task.WaitAsync(TimeSpan.FromSeconds(2));

            await manager.DisposeAsync();

            Assert.Equal(0, factory.Process.KillCount);
            Assert.Equal(1, factory.Process.DisposeCount);
            Assert.Equal(EquipmentSidecarState.Disposed, manager.State);
        }
        finally
        {
            await manager.DisposeAsync();
            File.Delete(executable);
        }
    }

    private static EquipmentSidecarStartOptions Options(string executable, string evidence) => new()
    {
        ExecutablePath = executable,
        BindAddress = "127.0.0.1",
        Port = 7100,
        SessionId = 7,
        ConnectionLimit = 2,
        RequestHostTime = true,
        EvidencePath = evidence,
        TelemetryStdout = true
    };

    private static string CreateTemporaryFile(string extension)
    {
        var path = Path.Combine(Path.GetTempPath(), $"sidecar-{Guid.NewGuid():N}{extension}");
        File.WriteAllText(path, string.Empty);
        return path;
    }

    private static string TemporaryEvidencePath() =>
        Path.Combine(Path.GetTempPath(), $"sidecar-{Guid.NewGuid():N}.jsonl");

    private sealed class FakeProcessFactory : IEquipmentSidecarProcessFactory
    {
        public FakeProcess Process { get; } = new();
        public int CreateCount { get; private set; }
        public int ExitCodeWhenKilled { set => Process.ExitCodeWhenKilled = value; }

        public IEquipmentSidecarProcess Create(ProcessStartInfo startInfo)
        {
            ArgumentNullException.ThrowIfNull(startInfo);
            CreateCount++;
            Process.StartInfo = startInfo;
            return Process;
        }
    }

    private sealed class FakeProcess : IEquipmentSidecarProcess
    {
        private readonly TaskCompletionSource _exit = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _exitCode;

        public event Action<EquipmentSidecarOutputStream, string>? OutputReceived;
        public ProcessStartInfo? StartInfo { get; set; }
        public int Id => 4242;
        public bool HasExited => _exit.Task.IsCompleted;
        public int ExitCode => HasExited ? _exitCode : throw new InvalidOperationException("Process is still running.");
        public int ExitCodeWhenKilled { get; set; } = -1;
        public int StartCount { get; private set; }
        public int KillCount { get; private set; }
        public int DisposeCount { get; private set; }
        public bool LastKillIncludedProcessTree { get; private set; }

        public void Start() => StartCount++;

        public void Kill(bool entireProcessTree)
        {
            KillCount++;
            LastKillIncludedProcessTree = entireProcessTree;
            CompleteExit(ExitCodeWhenKilled);
        }

        public Task WaitForExitAndDrainAsync() => _exit.Task;
        public void Emit(EquipmentSidecarOutputStream stream, string line) => OutputReceived?.Invoke(stream, line);

        public void CompleteExit(int exitCode)
        {
            _exitCode = exitCode;
            _exit.TrySetResult();
        }

        public void Dispose() => DisposeCount++;
    }
}
