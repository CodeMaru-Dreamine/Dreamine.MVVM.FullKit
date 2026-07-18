using Dreamine.Logging.Formatters;
using Dreamine.Logging.Interfaces;
using Dreamine.Logging.Models;
using Dreamine.Logging.Options;
using Dreamine.Logging.Registration;
using Dreamine.Logging.Services;
using Dreamine.Logging.Sinks;
using Dreamine.MVVM.Core;
using Dreamine.FullKit.Tests.Core;

namespace Dreamine.FullKit.Tests.Logging;

/// <summary>
/// \if KO
/// <para>Logging Service Tests 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates logging service tests functionality and related state.</para>
/// \endif
/// </summary>
[Collection(DMContainerCollection.Name)]
public sealed class LoggingServiceTests
{
    /// <summary>
    /// \if KO
    /// <para>In Memory Log Store Enforces Capacity And Raises Event 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the in memory log store enforces capacity and raises event operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void InMemoryLogStore_EnforcesCapacityAndRaisesEvent()
    {
        var store = new InMemoryLogStore(capacity: 2);
        var added = new List<string>();
        store.LogAdded += (_, entry) => added.Add(entry.Message);

        store.Add(Entry("one"));
        store.Add(Entry("two"));
        store.Add(Entry("three"));

        Assert.Equal(new[] { "one", "two", "three" }, added);
        Assert.Equal(new[] { "two", "three" }, store.GetEntries().Select(entry => entry.Message));
    }

    /// <summary>
    /// \if KO
    /// <para>Dreamine Logger Filters Below Minimum And Suppresses Sink Failures 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the dreamine logger filters below minimum and suppresses sink failures operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void DreamineLogger_FiltersBelowMinimumAndSuppressesSinkFailures()
    {
        var sink = new RecordingSink();
        var throwing = new ThrowingSink();
        var logger = new DreamineLogger(new IDreamineLogSink[] { sink, throwing }, DreamineLogLevel.Warning, "Test");

        logger.Info("ignored");
        logger.Warning("kept");

        var entry = Assert.Single(sink.Entries);
        Assert.Equal(DreamineLogLevel.Warning, entry.Level);
        Assert.Equal("Test", entry.Category);
        Assert.Equal("kept", entry.Message);
    }

    /// <summary>
    /// \if KO
    /// <para>Composite Log Sink Writes To All Sinks And Suppresses Failures 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the composite log sink writes to all sinks and suppresses failures operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void CompositeLogSink_WritesToAllSinksAndSuppressesFailures()
    {
        var first = new RecordingSink();
        var second = new RecordingSink();
        using var composite = new CompositeLogSink(new IDreamineLogSink[]
        {
            first,
            new ThrowingSink(),
            second
        });

        composite.Write(Entry("message"));

        Assert.Single(first.Entries);
        Assert.Single(second.Entries);
    }

    /// <summary>
    /// \if KO
    /// <para>Text File Log Sink Writes Formatted Entry To Daily File 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the text file log sink writes formatted entry to daily file operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void TextFileLogSink_WritesFormattedEntryToDailyFile()
    {
        var directory = Path.Combine(Path.GetTempPath(), "DreamineFullKitTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            using (var sink = new TextFileLogSink(directory, new DreamineTextLogFormatter(), flushEveryWriteCount: 1))
            {
                sink.Write(Entry("file message"));
            }

            var file = Assert.Single(Directory.GetFiles(directory, "*.log"));
            Assert.Contains("file message", File.ReadAllText(file));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Dreamine Logging Registration Returns Shutdown Handle Without Concrete Sink Coupling 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the dreamine logging registration returns shutdown handle without concrete sink coupling operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Dreamine Logging Registration Returns Shutdown Handle Without Concrete Sink Coupling 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the dreamine logging registration returns shutdown handle without concrete sink coupling operation.</para>
    /// \endif
    /// </returns>
    [Fact]
    public async Task DreamineLoggingRegistration_ReturnsShutdownHandleWithoutConcreteSinkCoupling()
    {
        var directory = Path.Combine(Path.GetTempPath(), "DreamineFullKitTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        DMContainer.Reset();

        try
        {
            await using IAsyncDisposable shutdownHandle =
                DreamineLoggingRegistration.Register(new DreamineLoggingOptions
                {
                    Category = "RegistrationTest",
                    LogDirectory = directory,
                    ShutdownTimeout = TimeSpan.FromMilliseconds(500)
                });

            Assert.NotNull(shutdownHandle);
            Assert.IsNotType<AsyncQueueSink>(shutdownHandle);
            Assert.NotNull(DMContainer.Resolve<IDreamineLogger>());
            Assert.NotNull(DMContainer.Resolve<IDreamineLogSink>());
            Assert.NotNull(DMContainer.Resolve<AsyncQueueSink>());
        }
        finally
        {
            DMContainer.Reset();
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Entry 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the entry operation.</para>
    /// \endif
    /// </summary>
    /// <param name="message">
    /// \if KO
    /// <para>처리할 메시지입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The message to process.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Entry 작업에서 생성한 <c>DreamineLogEntry</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>DreamineLogEntry</c> result produced by the entry operation.</para>
    /// \endif
    /// </returns>
    private static DreamineLogEntry Entry(string message)
    {
        return new DreamineLogEntry(
            DateTimeOffset.UnixEpoch,
            DreamineLogLevel.Info,
            "Test",
            message,
            exception: null,
            threadId: 1);
    }

    /// <summary>
    /// \if KO
    /// <para>Recording Sink 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates recording sink functionality and related state.</para>
    /// \endif
    /// </summary>
    private sealed class RecordingSink : IDreamineLogSink
    {
        /// <summary>
        /// \if KO
        /// <para>Entries 값을 가져옵니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets the entries value.</para>
        /// \endif
        /// </summary>
        public List<DreamineLogEntry> Entries { get; } = new();

        /// <summary>
        /// \if KO
        /// <para>데이터를 씁니다.</para>
        /// \endif
        /// \if EN
        /// <para>Writes data.</para>
        /// \endif
        /// </summary>
        /// <param name="entry">
        /// \if KO
        /// <para>entry에 사용할 <c>DreamineLogEntry</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>DreamineLogEntry</c> value used for entry.</para>
        /// \endif
        /// </param>
        public void Write(DreamineLogEntry entry)
        {
            Entries.Add(entry);
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Throwing Sink 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates throwing sink functionality and related state.</para>
    /// \endif
    /// </summary>
    private sealed class ThrowingSink : IDreamineLogSink
    {
        /// <summary>
        /// \if KO
        /// <para>데이터를 씁니다.</para>
        /// \endif
        /// \if EN
        /// <para>Writes data.</para>
        /// \endif
        /// </summary>
        /// <param name="entry">
        /// \if KO
        /// <para>entry에 사용할 <c>DreamineLogEntry</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>DreamineLogEntry</c> value used for entry.</para>
        /// \endif
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// \if KO
        /// <para>현재 객체 상태에서 Write 작업을 수행할 수 없는 경우 발생합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Thrown when the write operation is not valid for the current object state.</para>
        /// \endif
        /// </exception>
        public void Write(DreamineLogEntry entry)
        {
            throw new InvalidOperationException("sink failed");
        }
    }
}
