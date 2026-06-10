using Dreamine.Logging.Formatters;
using Dreamine.Logging.Models;

namespace Dreamine.FullKit.Tests.Logging;

public sealed class DreamineTextLogFormatterTests
{
    [Fact]
    public void Format_WritesStableSingleLineLogText()
    {
        var entry = new DreamineLogEntry(
            new DateTimeOffset(2026, 6, 7, 12, 34, 56, 789, TimeSpan.Zero),
            DreamineLogLevel.Warning,
            "Machine",
            "Pressure changed",
            exception: null,
            threadId: 42);

        var text = new DreamineTextLogFormatter().Format(entry);

        Assert.Equal("[2026-06-07 12:34:56.789] [Warning] [Machine] [T42] Pressure changed", text);
    }

    [Fact]
    public void Format_AppendsExceptionDetailsWhenPresent()
    {
        var entry = new DreamineLogEntry(
            DateTimeOffset.UnixEpoch,
            DreamineLogLevel.Error,
            "Machine",
            "Failed",
            new InvalidOperationException("bad state"),
            threadId: 1);

        var text = new DreamineTextLogFormatter().Format(entry);

        Assert.Contains("[Error] [Machine] [T1] Failed", text);
        Assert.Contains(nameof(InvalidOperationException), text);
        Assert.Contains("bad state", text);
    }
}
