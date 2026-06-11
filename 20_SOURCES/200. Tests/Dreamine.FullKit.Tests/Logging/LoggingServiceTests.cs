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

[Collection(DMContainerCollection.Name)]
public sealed class LoggingServiceTests
{
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

    private sealed class RecordingSink : IDreamineLogSink
    {
        public List<DreamineLogEntry> Entries { get; } = new();

        public void Write(DreamineLogEntry entry)
        {
            Entries.Add(entry);
        }
    }

    private sealed class ThrowingSink : IDreamineLogSink
    {
        public void Write(DreamineLogEntry entry)
        {
            throw new InvalidOperationException("sink failed");
        }
    }
}
