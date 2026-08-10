using Dreamine.SecsGem.FactoryScale.Infrastructure;
using Dreamine.SecsGem.FactoryScale.Runtime;

namespace Dreamine.SecsGem.FactoryScale.Tests;

[Collection(FactoryScaleTestCollection.Name)]
public sealed class WorkerProcessTests
{
    [Fact]
    [Trait("Category", "WorkerProcess")]
    public async Task Worker_PublishesReadyAndExitsNormallyAfterOwnedStopRequest()
    {
        var testDirectory = Path.Combine(Path.GetTempPath(), "Dreamine.FactoryScale.Tests", Guid.NewGuid().ToString("N"));
        var controlDirectory = Path.Combine(testDirectory, "control");
        var resultPath = Path.Combine(testDirectory, "worker-result.json");
        Directory.CreateDirectory(testDirectory);

        try
        {
            await using var coordinator = new WorkerProcessCoordinator();
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            var runId = "test-" + Guid.NewGuid().ToString("N");
            var programPath = typeof(FactoryHostRuntime).Assembly.Location;
            var request = new WorkerLaunchRequest(
                runId,
                "worker-1",
                programPath,
                Path.GetDirectoryName(programPath)!,
                StartIndex: 900_001,
                Count: 2,
                controlDirectory,
                resultPath,
                ReadyTimeout: TimeSpan.FromSeconds(30),
                ShutdownTimeout: TimeSpan.FromSeconds(20));

            var ready = await coordinator.StartAsync(request, deadline.Token);
            Assert.Equal(runId, ready.RunId);
            Assert.Equal("worker-1", ready.WorkerId);
            Assert.Equal(2, ready.Count);
            Assert.True(ready.ProcessId > 0);
            Assert.Equal(1, coordinator.RunningWorkerCount);

            var result = await coordinator.StopAsync("worker-1", deadline.Token);
            Assert.NotNull(result);
            Assert.True(result.ExitCode == 0,
                $"Worker exited {result.ExitCode}. stdout: {result.StandardOutputTail} stderr: {result.StandardErrorTail}");
            Assert.False(result.KilledAfterGracefulTimeout);
            Assert.Equal(ready.ProcessId, result.ProcessId);
            Assert.Equal(0, coordinator.RunningWorkerCount);
            Assert.True(File.Exists(result.ResultPath));
            Assert.NotNull(result.Result);
        }
        finally
        {
            TryDeleteTestDirectory(testDirectory);
        }
    }

    private static void TryDeleteTestDirectory(string directory)
    {
        try
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
