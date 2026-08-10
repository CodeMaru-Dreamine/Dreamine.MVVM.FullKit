using System.Net.Sockets;

namespace Dreamine.SecsGem.FactoryScale.Infrastructure;

/// <summary>
/// Coordinates a validated loopback port range across FactoryScale processes.
/// The named mutex is held until the supplied starter has bound its listener,
/// so another FactoryScale worker cannot win the probe-to-bind race.
/// </summary>
internal sealed class ValidatedPortRangeManager
{
    private readonly int _minimumPort;
    private readonly int _maximumPort;
    private int _cursor;
    private long _collisions;

    internal ValidatedPortRangeManager(int minimumPort = 20_000, int maximumPort = 48_000, int? seed = null)
    {
        if (minimumPort is < 1 or > 65_535) throw new ArgumentOutOfRangeException(nameof(minimumPort));
        if (maximumPort is < 1 or > 65_535) throw new ArgumentOutOfRangeException(nameof(maximumPort));
        if (maximumPort < minimumPort) throw new ArgumentException("Maximum port must be greater than or equal to minimum port.");

        _minimumPort = minimumPort;
        _maximumPort = maximumPort;
        var width = maximumPort - minimumPort + 1;
        var source = seed is { } explicitSeed ? (long)explicitSeed : (long)Environment.ProcessId * 397L;
        var offset = checked((int)(Math.Abs(source) % width));
        _cursor = minimumPort + offset - 1;
    }

    internal long CollisionCount => Interlocked.Read(ref _collisions);

    internal async Task<T> StartBoundAsync<T>(
        Func<int, CancellationToken, Task<T>> startBoundListener,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(startBoundListener);
        var attempts = _maximumPort - _minimumPort + 1;
        Exception? lastBindFailure = null;

        for (var attempt = 0; attempt < attempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var port = NextPort();
            using var mutex = new Mutex(false, MutexName(port));
            var ownsMutex = false;
            try
            {
                try { ownsMutex = mutex.WaitOne(0); }
                catch (AbandonedMutexException) { ownsMutex = true; }
                if (!ownsMutex)
                {
                    Interlocked.Increment(ref _collisions);
                    continue;
                }

                try
                {
                    return await startBoundListener(port, cancellationToken).ConfigureAwait(false);
                }
                catch (SocketException exception)
                {
                    lastBindFailure = exception;
                    Interlocked.Increment(ref _collisions);
                }
                catch (IOException exception) when (exception.InnerException is SocketException)
                {
                    lastBindFailure = exception;
                    Interlocked.Increment(ref _collisions);
                }
            }
            finally
            {
                if (ownsMutex) mutex.ReleaseMutex();
            }
        }

        throw new IOException(
            $"No bindable loopback port remained in {_minimumPort}-{_maximumPort} after {attempts} attempts.",
            lastBindFailure);
    }

    private int NextPort()
    {
        while (true)
        {
            var current = Volatile.Read(ref _cursor);
            var next = current >= _maximumPort ? _minimumPort : current + 1;
            if (Interlocked.CompareExchange(ref _cursor, next, current) == current) return next;
        }
    }

    private static string MutexName(int port) => OperatingSystem.IsWindows()
        ? $"Local\\Dreamine.FactoryScale.Port.{port}"
        : $"Dreamine.FactoryScale.Port.{port}";
}
