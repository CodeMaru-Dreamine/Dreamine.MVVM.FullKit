using Dreamine.Logging.Formatters;
using Dreamine.Logging.Models;

namespace Dreamine.FullKit.Tests.Logging;

/// <summary>
/// \if KO
/// <para>Dreamine Text Log Formatter Tests 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates dreamine text log formatter tests functionality and related state.</para>
/// \endif
/// </summary>
public sealed class DreamineTextLogFormatterTests
{
    /// <summary>
    /// \if KO
    /// <para>Format Writes Stable Single Line Log Text 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the format writes stable single line log text operation.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>Format Appends Exception Details When Present 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the format appends exception details when present operation.</para>
    /// \endif
    /// </summary>
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
