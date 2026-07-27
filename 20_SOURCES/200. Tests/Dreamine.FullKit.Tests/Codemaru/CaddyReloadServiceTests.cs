using System.Text.Json;
using Codemaru.Models.Certificates;
using Codemaru.Options;
using Codemaru.Services.Certificates;

namespace Dreamine.FullKit.Tests.Caddy;

public sealed class CaddyReloadServiceTests
{
    [Fact]
    public async Task ReloadAsync_ValidatesBeforeGracefulReloadWithConfiguredArguments()
    {
        RecordingProcessRunner runner = new(
            Success(output: "valid configuration"),
            Success(output: "reloaded"));
        CaddyReloadService service = new(runner);
        CertificateMonitorOptions options = CreateOptions();

        ProcessExecutionResult result = await service.ReloadAsync(options, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, runner.Invocations.Count);

        ProcessInvocation validation = runner.Invocations[0];
        Assert.Equal(options.CaddyPath, validation.FileName);
        Assert.Equal(options.CaddyWorkingDirectory, validation.WorkingDirectory);
        Assert.Equal(
            ["validate", "--config", options.CaddyConfigPath, "--adapter", options.CaddyConfigAdapter],
            validation.Arguments);
        Assert.Equal(TimeSpan.FromSeconds(30), validation.Timeout);
        Assert.Equal(options.MaxCommandOutputChars, validation.MaxOutputChars);

        ProcessInvocation reload = runner.Invocations[1];
        Assert.Equal(
            [
                "reload",
                "--config",
                options.CaddyConfigPath,
                "--adapter",
                options.CaddyConfigAdapter,
                "--address",
                options.CaddyAdminAddress,
                "--force"
            ],
            reload.Arguments);
        Assert.Contains("[caddy validate]", result.Output, StringComparison.Ordinal);
        Assert.Contains("[caddy reload]", result.Output, StringComparison.Ordinal);
        Assert.Contains("graceful reload completed successfully", result.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReloadAsync_DoesNotReloadWhenValidationFails()
    {
        RecordingProcessRunner runner = new(
            Failure(exitCode: 2, error: "invalid directive"));
        CaddyReloadService service = new(runner);

        ProcessExecutionResult result = await service.ReloadAsync(
            CreateOptions(),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(2, result.ExitCode);
        Assert.Single(runner.Invocations);
        Assert.Equal("validate", runner.Invocations[0].Arguments[0]);
        Assert.Equal("invalid directive", result.Error);
        Assert.Contains("validation failed", result.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReloadAsync_ReportsBothStagesWhenGracefulReloadFails()
    {
        RecordingProcessRunner runner = new(
            Success(output: "validation output", error: "validation warning"),
            Failure(exitCode: 1, output: "reload output", error: "admin endpoint unavailable"));
        CaddyReloadService service = new(runner);

        ProcessExecutionResult result = await service.ReloadAsync(
            CreateOptions(),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(1, result.ExitCode);
        Assert.Equal(2, runner.Invocations.Count);
        Assert.Contains("validation output", result.Output, StringComparison.Ordinal);
        Assert.Contains("reload output", result.Output, StringComparison.Ordinal);
        Assert.Contains("validation warning", result.Error, StringComparison.Ordinal);
        Assert.Contains("admin endpoint unavailable", result.Error, StringComparison.Ordinal);
        Assert.Contains("graceful reload failed", result.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReloadAsync_OmitsOptionalFlagsWhenTheyAreDisabled()
    {
        RecordingProcessRunner runner = new(Success(), Success());
        CaddyReloadService service = new(runner);
        CertificateMonitorOptions options = CreateOptions();
        options.CaddyConfigAdapter = string.Empty;
        options.CaddyAdminAddress = string.Empty;
        options.CaddyForceReload = false;

        ProcessExecutionResult result = await service.ReloadAsync(options, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            ["validate", "--config", options.CaddyConfigPath],
            runner.Invocations[0].Arguments);
        Assert.Equal(
            ["reload", "--config", options.CaddyConfigPath],
            runner.Invocations[1].Arguments);
    }

    [Fact]
    public async Task ReloadAsync_RejectsMissingRequiredPathsBeforeStartingAProcess()
    {
        RecordingProcessRunner runner = new();
        CaddyReloadService service = new(runner);
        CertificateMonitorOptions options = CreateOptions();
        options.CaddyConfigPath = string.Empty;

        ProcessExecutionResult result = await service.ReloadAsync(options, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(-1, result.ExitCode);
        Assert.Empty(runner.Invocations);
        Assert.Contains("configuration path is empty", result.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SettingsWriter_PersistsOnlyTheCurrentCertificateAndCaddySchema()
    {
        string settingsPath = Path.GetTempFileName();

        try
        {
            CertificateMonitorOptions options = CreateOptions();
            options.CaddyForceReload = false;
            await File.WriteAllTextAsync(
                settingsPath,
                """{"UnrelatedSection":{"Preserved":true},"CertificateMonitor":{"LegacyOnly":true}}""");
            CertificateSettingsWriter writer = new(settingsPath);

            await writer.SaveAsync(options, CancellationToken.None);

            using JsonDocument document = JsonDocument.Parse(
                await File.ReadAllTextAsync(settingsPath));
            JsonElement section = document.RootElement.GetProperty("CertificateMonitor");
            Assert.True(
                document.RootElement
                    .GetProperty("UnrelatedSection")
                    .GetProperty("Preserved")
                    .GetBoolean());

            string[] expectedPropertyNames =
            [
                "CertificateDirectory",
                "CertificateFilePatterns",
                "PfxPassword",
                "WacsPath",
                "CaddyPath",
                "CaddyConfigPath",
                "CaddyWorkingDirectory",
                "CaddyConfigAdapter",
                "CaddyAdminAddress",
                "CaddyForceReload",
                "WarningDays",
                "CriticalDays",
                "MaxCommandOutputChars"
            ];

            Assert.Equal(
                expectedPropertyNames.Order(StringComparer.Ordinal),
                section.EnumerateObject()
                    .Select(property => property.Name)
                    .Order(StringComparer.Ordinal));
            Assert.Equal(options.CaddyPath, section.GetProperty("CaddyPath").GetString());
            Assert.Equal(options.CaddyConfigPath, section.GetProperty("CaddyConfigPath").GetString());
            Assert.Equal(options.CaddyAdminAddress, section.GetProperty("CaddyAdminAddress").GetString());
            Assert.False(section.GetProperty("CaddyForceReload").GetBoolean());
        }
        finally
        {
            File.Delete(settingsPath);
        }
    }

    private static CertificateMonitorOptions CreateOptions()
    {
        return new CertificateMonitorOptions
        {
            CertificateDirectory = @"D:\certificates",
            CertificateFilePatterns = ["*.pem", "*.pfx"],
            PfxPassword = "test-password",
            WacsPath = @"D:\win acme\wacs.exe",
            CaddyPath = @"C:\Program Files\Caddy\caddy.exe",
            CaddyConfigPath = @"D:\Caddy Config\Caddyfile",
            CaddyWorkingDirectory = @"D:\Caddy Config",
            CaddyConfigAdapter = "caddyfile",
            CaddyAdminAddress = "127.0.0.1:2020",
            CaddyForceReload = true,
            WarningDays = 21,
            CriticalDays = 7,
            MaxCommandOutputChars = 4321
        };
    }

    private static ProcessExecutionResult Success(
        string output = "",
        string error = "")
    {
        return new ProcessExecutionResult
        {
            IsSuccess = true,
            ExitCode = 0,
            Output = output,
            Error = error,
            Message = "Process completed successfully."
        };
    }

    private static ProcessExecutionResult Failure(
        int exitCode,
        string output = "",
        string error = "")
    {
        return new ProcessExecutionResult
        {
            IsSuccess = false,
            ExitCode = exitCode,
            Output = output,
            Error = error,
            Message = $"Process failed with exit code {exitCode}."
        };
    }

    private sealed class RecordingProcessRunner(
        params ProcessExecutionResult[] results) : IProcessRunner
    {
        private readonly Queue<ProcessExecutionResult> _results = new(results);

        public List<ProcessInvocation> Invocations { get; } = [];

        public Task<ProcessExecutionResult> RunAsync(
            string fileName,
            IReadOnlyList<string> arguments,
            string? workingDirectory,
            TimeSpan timeout,
            int maxOutputChars,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Invocations.Add(
                new ProcessInvocation(
                    fileName,
                    arguments.ToArray(),
                    workingDirectory,
                    timeout,
                    maxOutputChars));

            return Task.FromResult(_results.Dequeue());
        }
    }

    private sealed record ProcessInvocation(
        string FileName,
        IReadOnlyList<string> Arguments,
        string? WorkingDirectory,
        TimeSpan Timeout,
        int MaxOutputChars);
}
