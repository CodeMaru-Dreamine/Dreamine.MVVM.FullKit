using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using Dreamine.Secs.Abstractions.Diagnostics;
using Dreamine.Secs.Abstractions.Hsms;

namespace Dreamine.SecsGem.Interop.Runtime.Logging;

internal interface IWireLogRecordSink : IAsyncDisposable
{
    ValueTask AppendAsync(WireLogRecord record, CancellationToken cancellationToken);
    ValueTask CompleteAsync(CancellationToken cancellationToken);
}

internal sealed class WireLogRecorder : IAsyncDisposable
{
    private readonly IHsmsWireObservationSource _source;
    private readonly WireLogIdentity _identity;
    private readonly WireLogRecordFactory _factory;
    private readonly IWireLogRecordSink _sink;
    private readonly Channel<WireLogRecord> _queue;
    private readonly CancellationTokenSource _readerCancellation = new();
    private readonly CancellationTokenSource _writerCancellation = new();
    private readonly TimeSpan _shutdownTimeout;
    private readonly object _disposeGate = new();
    private readonly Task _reader;
    private readonly Task _writer;
    private long _recorderDrops;
    private long _recordSequence;
    private long _written;
    private int _flushCompleted;
    private Task? _disposeTask;
    private Task? _deferredCleanup;
    private string? _writerFailure;
    private string? _readerFailure;

    internal WireLogRecorder(
        IHsmsWireObservationSource source,
        WireLogIdentity identity,
        WireLogRecorderOptions options,
        IWireLogRecordSink sink,
        IWireBodyDecoder? decoder = null)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _identity = identity ?? throw new ArgumentNullException(nameof(identity));
        _identity.Validate();
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        _sink = sink ?? throw new ArgumentNullException(nameof(sink));
        _factory = new WireLogRecordFactory(options.Policy, decoder);
        _shutdownTimeout = options.ShutdownTimeout;
        _queue = Channel.CreateBounded<WireLogRecord>(new BoundedChannelOptions(options.QueueCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });
        _reader = ReadSourceAsync();
        _writer = WriteRecordsAsync();
    }

    internal WireLogHealth Health => new(
        _source.DroppedWireObservationCount,
        Interlocked.Read(ref _recorderDrops),
        Interlocked.Read(ref _written),
        Volatile.Read(ref _flushCompleted) != 0,
        Volatile.Read(ref _writerFailure) ?? Volatile.Read(ref _readerFailure));

    internal bool TryRecordDiagnostic(
        SecsDiagnosticEvent diagnostic,
        long connectionEpoch = 0,
        DateTimeOffset? timestampUtc = null) =>
        TryEnqueue(_factory.CreateDiagnostic(
            diagnostic,
            _identity,
            connectionEpoch,
            timestampUtc ?? DateTimeOffset.UtcNow));

    internal bool TryRecordState(
        SecsSessionStateChangedEventArgs transition,
        DateTimeOffset? timestampUtc = null) =>
        TryEnqueue(_factory.CreateState(
            transition,
            _identity,
            timestampUtc ?? DateTimeOffset.UtcNow));

    private async Task ReadSourceAsync()
    {
        try
        {
            await foreach (var observation in _source.ReadWireObservationsAsync(_readerCancellation.Token).ConfigureAwait(false))
            {
                var record = _factory.Create(observation, _identity);
                TryEnqueue(record, observation.SequenceNumber);
            }
        }
        catch (OperationCanceledException) when (_readerCancellation.IsCancellationRequested) { }
        catch (Exception exception) { Volatile.Write(ref _readerFailure, exception.Message); }
    }

    private async Task WriteRecordsAsync()
    {
        var failed = false;
        try
        {
            await foreach (var record in _queue.Reader.ReadAllAsync().ConfigureAwait(false))
            {
                if (failed) continue;
                try
                {
                    await _sink.AppendAsync(record, _writerCancellation.Token).ConfigureAwait(false);
                    Interlocked.Increment(ref _written);
                }
                catch (Exception exception)
                {
                    failed = true;
                    Volatile.Write(ref _writerFailure, exception.Message);
                }
            }

            if (!failed)
            {
                try
                {
                    await _sink.CompleteAsync(_writerCancellation.Token).ConfigureAwait(false);
                    Volatile.Write(ref _flushCompleted, 1);
                }
                catch (Exception exception) { Volatile.Write(ref _writerFailure, exception.Message); }
            }
        }
        finally
        {
            try { await _sink.DisposeAsync().ConfigureAwait(false); }
            catch (Exception exception) { Volatile.Write(ref _writerFailure, exception.Message); }
        }
    }

    public ValueTask DisposeAsync()
    {
        Task disposal;
        lock (_disposeGate)
        {
            disposal = _disposeTask ??= DisposeCoreAsync();
        }
        return new(disposal);
    }

    private async Task DisposeCoreAsync()
    {
        using var shutdown = new CancellationTokenSource(_shutdownTimeout);
        if (!_reader.IsCompleted)
        {
            var grace = TimeSpan.FromTicks(Math.Max(
                TimeSpan.FromMilliseconds(10).Ticks,
                _shutdownTimeout.Ticks / 2));
            var naturalCompletion = await Task.WhenAny(
                _reader,
                Task.Delay(grace, shutdown.Token)).ConfigureAwait(false);
            if (!ReferenceEquals(naturalCompletion, _reader) && !_reader.IsCompleted)
            {
                Volatile.Write(ref _readerFailure, "Wire observation capture stopped before the source completed.");
                _readerCancellation.Cancel();
            }
        }
        try { await _reader.WaitAsync(shutdown.Token).ConfigureAwait(false); }
        catch (OperationCanceledException) when (shutdown.IsCancellationRequested)
        {
            Volatile.Write(ref _readerFailure, "Wire observation reader exceeded the shutdown deadline.");
        }

        // Exact-wire enumeration can complete before the session returns its final diagnostic/state callbacks.
        // The facade unsubscribes those producers before calling DisposeAsync, so disposal owns the only safe
        // point at which the shared record queue can be closed.
        _queue.Writer.TryComplete();
        _writerCancellation.CancelAfter(_shutdownTimeout);
        try { await _writer.WaitAsync(shutdown.Token).ConfigureAwait(false); }
        catch (OperationCanceledException) when (shutdown.IsCancellationRequested)
        {
            Volatile.Write(ref _writerFailure, "Wire-log writer exceeded the shutdown deadline.");
        }

        if (_reader.IsCompleted && _writer.IsCompleted) DisposeLifetime();
        else
        {
            lock (_disposeGate)
                _deferredCleanup ??= CleanupWhenFinishedAsync();
        }
    }

    private async Task CleanupWhenFinishedAsync()
    {
        try { await Task.WhenAll(_reader, _writer).ConfigureAwait(false); }
        catch (Exception exception) { Volatile.Write(ref _writerFailure, exception.Message); }
        finally { DisposeLifetime(); }
    }

    private void DisposeLifetime()
    {
        _readerCancellation.Dispose();
        _writerCancellation.Dispose();
    }

    private bool TryEnqueue(WireLogRecord record, long? sourceSequence = null)
    {
        var sequenced = record with
        {
            Sequence = Interlocked.Increment(ref _recordSequence),
            SourceSequence = sourceSequence ?? record.SourceSequence
        };
        if (_queue.Writer.TryWrite(sequenced)) return true;
        Interlocked.Increment(ref _recorderDrops);
        return false;
    }
}

internal sealed class WireLogStorageOptions
{
    internal string RootDirectory { get; init; } = string.Empty;
    internal long MaximumSegmentBytes { get; init; } = 32L * 1024 * 1024;
    internal int RetainedSegments { get; init; } = 20;
    internal int MaximumRecordBytes { get; init; } = 20 * 1024 * 1024;

    internal string ValidateAndResolveRoot()
    {
        if (string.IsNullOrWhiteSpace(RootDirectory)) throw new ArgumentException("A product-owned log root is required.", nameof(RootDirectory));
        if (MaximumSegmentBytes is < 4096 or > 4L * 1024 * 1024 * 1024)
            throw new ArgumentOutOfRangeException(nameof(MaximumSegmentBytes));
        if (RetainedSegments is < 1 or > 10_000) throw new ArgumentOutOfRangeException(nameof(RetainedSegments));
        if (MaximumRecordBytes is < 1024 or > WireLogPolicy.MaximumAllowedBodyBytes + 4 * 1024 * 1024)
            throw new ArgumentOutOfRangeException(nameof(MaximumRecordBytes));
        return Path.GetFullPath(RootDirectory);
    }
}

internal sealed class JsonlWireLogSink : IWireLogRecordSink
{
    internal const string OwnedPrefix = "dreamine-wire-";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly WireLogStorageOptions _options;
    private readonly string _root;
    private FileStream? _stream;
    private string? _activePath;
    private long _currentBytes;
    private int _completed;
    private readonly SemaphoreSlim _completionGate = new(1, 1);

    internal JsonlWireLogSink(WireLogStorageOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _root = options.ValidateAndResolveRoot();
        Directory.CreateDirectory(_root);
        RejectReparsePoint(_root, "The log root cannot be a reparse point.");
        RecoverActiveSegments();
        ApplyRetention();
    }

    internal IReadOnlyList<string> FinalizedSegments => Directory.EnumerateFiles(_root, $"{OwnedPrefix}*.jsonl")
        .OrderBy(value => value, StringComparer.Ordinal)
        .ToArray();

    public async ValueTask AppendAsync(WireLogRecord record, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _completed) != 0, this);
        ArgumentNullException.ThrowIfNull(record);
        var payload = JsonSerializer.SerializeToUtf8Bytes(record, JsonOptions);
        var recordBytes = checked(payload.Length + 1);
        if (recordBytes > _options.MaximumRecordBytes)
            throw new InvalidDataException($"A wire-log record exceeds {_options.MaximumRecordBytes} bytes.");
        if (recordBytes > _options.MaximumSegmentBytes)
            throw new InvalidDataException($"A wire-log record exceeds the segment limit of {_options.MaximumSegmentBytes} bytes.");
        if (_stream is null || (_currentBytes > 0 && _currentBytes + recordBytes > _options.MaximumSegmentBytes))
        {
            await FinalizeCurrentAsync(cancellationToken).ConfigureAwait(false);
            OpenSegment();
        }

        await _stream!.WriteAsync(payload, cancellationToken).ConfigureAwait(false);
        await _stream.WriteAsync("\n"u8.ToArray(), cancellationToken).ConfigureAwait(false);
        _currentBytes += recordBytes;
    }

    public async ValueTask CompleteAsync(CancellationToken cancellationToken)
    {
        if (Volatile.Read(ref _completed) != 0) return;
        await _completionGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (Volatile.Read(ref _completed) != 0) return;
            await FinalizeCurrentAsync(cancellationToken).ConfigureAwait(false);
            ApplyRetention();
            Volatile.Write(ref _completed, 1);
        }
        finally { _completionGate.Release(); }
    }

    public async ValueTask DisposeAsync()
    {
        if (Volatile.Read(ref _completed) == 0)
            await CompleteAsync(CancellationToken.None).ConfigureAwait(false);
    }

    private void OpenSegment()
    {
        var name = $"{OwnedPrefix}{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss-fffffff}-{Guid.NewGuid():N}.active";
        _activePath = Path.Combine(_root, name);
        EnsureOwnedPath(_activePath, ".active");
        _stream = new FileStream(_activePath, FileMode.CreateNew, FileAccess.Write, FileShare.Read,
            64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        _currentBytes = 0;
    }

    private async ValueTask FinalizeCurrentAsync(CancellationToken cancellationToken)
    {
        if (_activePath is null) return;
        if (_stream is not null)
        {
            await _stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            _stream.Flush(flushToDisk: true);
            await _stream.DisposeAsync().ConfigureAwait(false);
            _stream = null;
        }
        var finalPath = Path.ChangeExtension(_activePath, ".jsonl");
        EnsureOwnedPath(finalPath, ".jsonl");
        File.Move(_activePath, finalPath, overwrite: false);
        _activePath = null;
        _currentBytes = 0;
    }

    private void ApplyRetention()
    {
        var candidates = Directory.EnumerateFiles(_root, $"{OwnedPrefix}*.jsonl", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFullPath)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .ThenByDescending(value => value, StringComparer.Ordinal)
            .ToArray();
        foreach (var path in candidates.Skip(_options.RetainedSegments))
        {
            EnsureOwnedPath(path, ".jsonl");
            RejectReparsePoint(path, "Retention does not delete reparse points.");
            File.Delete(path);
        }
    }

    private void RecoverActiveSegments()
    {
        foreach (var path in Directory.EnumerateFiles(_root, $"{OwnedPrefix}*.active", SearchOption.TopDirectoryOnly))
        {
            EnsureOwnedPath(path, ".active");
            RejectReparsePoint(path, "Crash recovery does not read reparse points.");
            using var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None,
                64 * 1024, FileOptions.SequentialScan);
            var lastCompleteByte = FindLastNewline(stream);
            if (lastCompleteByte < 0)
            {
                stream.Dispose();
                File.Delete(path);
                continue;
            }
            if (stream.Length != lastCompleteByte + 1) stream.SetLength(lastCompleteByte + 1);
            stream.Flush(flushToDisk: true);
            stream.Dispose();
            var finalPath = Path.ChangeExtension(path, ".jsonl");
            if (File.Exists(finalPath))
                finalPath = Path.Combine(_root, $"{OwnedPrefix}recovered-{Guid.NewGuid():N}.jsonl");
            EnsureOwnedPath(finalPath, ".jsonl");
            File.Move(path, finalPath, overwrite: false);
        }
    }

    private static long FindLastNewline(FileStream stream)
    {
        var buffer = new byte[64 * 1024];
        var end = stream.Length;
        while (end > 0)
        {
            var count = checked((int)Math.Min(buffer.Length, end));
            var start = end - count;
            stream.Position = start;
            var read = stream.Read(buffer, 0, count);
            for (var index = read - 1; index >= 0; index--)
            {
                if (buffer[index] == (byte)'\n') return start + index;
            }
            end = start;
        }
        return -1;
    }

    private void EnsureOwnedPath(string path, string extension)
    {
        var full = Path.GetFullPath(path);
        if (!string.Equals(Path.GetDirectoryName(full), _root, StringComparison.OrdinalIgnoreCase) ||
            !Path.GetFileName(full).StartsWith(OwnedPrefix, StringComparison.Ordinal) ||
            !string.Equals(Path.GetExtension(full), extension, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The path is outside the product-owned wire-log naming boundary.");
        }
    }

    private static void RejectReparsePoint(string path, string message)
    {
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidOperationException(message);
    }
}

internal static class WireLogReader
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    internal static async IAsyncEnumerable<WireLogRecord> ReadAsync(
        string path,
        WireLogFilter? filter = null,
        int maximumRecordBytes = 20 * 1024 * 1024,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (maximumRecordBytes < 1024) throw new ArgumentOutOfRangeException(nameof(maximumRecordBytes));
        filter ??= new WireLogFilter();
        filter.Validate();
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite,
            64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        var buffer = new byte[64 * 1024];
        using var line = new MemoryStream();
        while (true)
        {
            var read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (read == 0) break;
            var start = 0;
            for (var index = 0; index < read; index++)
            {
                if (buffer[index] != (byte)'\n') continue;
                line.Write(buffer, start, index - start);
                var record = Deserialize(line, maximumRecordBytes);
                line.SetLength(0);
                start = index + 1;
                if (filter.Matches(record)) yield return record;
            }
            if (start < read) line.Write(buffer, start, read - start);
            if (line.Length > maximumRecordBytes)
                throw new InvalidDataException($"A wire-log record exceeds {maximumRecordBytes} bytes.");
        }
        if (line.Length > 0)
        {
            var record = Deserialize(line, maximumRecordBytes);
            if (filter.Matches(record)) yield return record;
        }
    }

    private static WireLogRecord Deserialize(MemoryStream line, int maximumRecordBytes)
    {
        if (line.Length > maximumRecordBytes)
            throw new InvalidDataException($"A wire-log record exceeds {maximumRecordBytes} bytes.");
        if (!line.TryGetBuffer(out var segment)) throw new InvalidOperationException("The line buffer is unavailable.");
        var record = JsonSerializer.Deserialize<WireLogRecord>(segment.AsSpan(0, checked((int)line.Length)), JsonOptions)
            ?? throw new InvalidDataException("A wire-log record was empty.");
        if (record.SchemaVersion != WireLogRecord.CurrentSchemaVersion)
            throw new InvalidDataException($"Unsupported wire-log schema version {record.SchemaVersion}.");
        return record;
    }
}
