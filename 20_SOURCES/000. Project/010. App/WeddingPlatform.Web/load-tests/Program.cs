using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

var paths = new[]
{
    "/",
    "/seojun-hayeon",
    "/minsu",
    "/jihoon-sua",
    "/jiho-yujin",
    "/minjun-seoyeon",
    "/doyun-harin",
    "/sangsua"
};

int concurrency = ReadInt(args, "--clients", 5);
int durationSeconds = ReadInt(args, "--duration", 15);
double maxServerErrorRate = ReadDouble(args, "--max-5xx-rate", 0.01);
double maxNetworkErrorRate = ReadDouble(args, "--max-network-error-rate", 0.03);
int timeoutSeconds = ReadInt(args, "--timeout", 10);
string baseUrl = ReadString(args, "--base-url", "https://wedding.codemaru.co.kr");
string connectIp = ReadString(args, "--connect-ip", string.Empty);
string outputPath = ReadString(args, "--output",
    Path.Combine("results", $"load-{DateTime.Now:yyyyMMdd-HHmmss}-{concurrency}vu.json"));
var urls = paths.Select(path => baseUrl.TrimEnd('/') + path).ToArray();

using var handler = new SocketsHttpHandler
{
    AutomaticDecompression = DecompressionMethods.All,
    MaxConnectionsPerServer = Math.Max(concurrency, 16),
    PooledConnectionLifetime = TimeSpan.FromMinutes(2),
    ConnectTimeout = TimeSpan.FromSeconds(timeoutSeconds)
};
if (!string.IsNullOrWhiteSpace(connectIp))
{
    var targetAddress = IPAddress.Parse(connectIp);
    handler.ConnectCallback = async (context, cancellationToken) =>
    {
        var socket = new Socket(targetAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        try
        {
            await socket.ConnectAsync(targetAddress, context.DnsEndPoint.Port, cancellationToken);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    };
}
using var client = new HttpClient(handler)
{
    Timeout = TimeSpan.FromSeconds(timeoutSeconds)
};
client.DefaultRequestHeaders.UserAgent.ParseAdd("Codemaru-Authorized-LoadTest/1.0");

var samples = new ConcurrentBag<Sample>();
using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(durationSeconds));
var started = Stopwatch.StartNew();

Console.WriteLine($"Starting authorized production GET test: {concurrency} clients, {durationSeconds}s");

var workers = Enumerable.Range(0, concurrency).Select(workerId => Task.Run(async () =>
{
    int index = workerId % urls.Length;
    while (!stop.IsCancellationRequested)
    {
        string url = urls[index++ % urls.Length];
        var watch = Stopwatch.StartNew();
        try
        {
            using var response = await client.GetAsync(
                url, HttpCompletionOption.ResponseContentRead, stop.Token);
            samples.Add(new Sample(url, (int)response.StatusCode, watch.Elapsed.TotalMilliseconds,
                response.Content.Headers.ContentLength ?? 0, null));
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested)
        {
            break;
        }
        catch (Exception ex)
        {
            samples.Add(new Sample(url, 0, watch.Elapsed.TotalMilliseconds, 0,
                ex.GetType().Name + ": " + ex.Message));
        }

        if (samples.Count >= 20)
        {
            double serverErrorRate = samples.Count(x => x.StatusCode >= 500)
                                     / (double)samples.Count;
            double networkErrorRate = samples.Count(x => x.StatusCode == 0)
                                      / (double)samples.Count;
            if (serverErrorRate > maxServerErrorRate ||
                networkErrorRate > maxNetworkErrorRate)
            {
                Console.WriteLine(
                    $"STOP: 5xx={serverErrorRate:P2} (limit {maxServerErrorRate:P2}), " +
                    $"network={networkErrorRate:P2} (limit {maxNetworkErrorRate:P2})");
                stop.Cancel();
                break;
            }
        }
    }
})).ToArray();

await Task.WhenAll(workers);
started.Stop();

var ordered = samples.OrderBy(x => x.ElapsedMs).ToArray();
var errors = ordered.Where(x => x.StatusCode is < 200 or >= 400).ToArray();
var summary = new
{
    StartedAt = DateTimeOffset.Now - started.Elapsed,
    DurationSeconds = started.Elapsed.TotalSeconds,
    Clients = concurrency,
    Requests = ordered.Length,
    RequestsPerSecond = ordered.Length / Math.Max(started.Elapsed.TotalSeconds, 0.001),
    ErrorCount = errors.Length,
    ErrorRate = ordered.Length == 0 ? 1 : errors.Length / (double)ordered.Length,
    ServerErrorRate = ordered.Length == 0 ? 1 :
        ordered.Count(x => x.StatusCode >= 500) / (double)ordered.Length,
    NetworkErrorRate = ordered.Length == 0 ? 1 :
        ordered.Count(x => x.StatusCode == 0) / (double)ordered.Length,
    LatencyMs = new
    {
        Average = ordered.Length == 0 ? 0 : ordered.Average(x => x.ElapsedMs),
        P50 = Percentile(ordered, 0.50),
        P95 = Percentile(ordered, 0.95),
        P99 = Percentile(ordered, 0.99),
        Max = ordered.Length == 0 ? 0 : ordered[^1].ElapsedMs
    },
    ByUrl = ordered.GroupBy(x => x.Url).Select(group => new
    {
        Url = group.Key,
        Requests = group.Count(),
        Errors = group.Count(x => x.StatusCode is < 200 or >= 400),
        AverageMs = group.Average(x => x.ElapsedMs),
        P95Ms = Percentile(group.OrderBy(x => x.ElapsedMs).ToArray(), 0.95)
    }),
    ByStatus = ordered.GroupBy(x => x.StatusCode).ToDictionary(x => x.Key, x => x.Count()),
    Errors = errors.Take(50)
};

Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);
await File.WriteAllTextAsync(outputPath, JsonSerializer.Serialize(summary, new JsonSerializerOptions
{
    WriteIndented = true
}));

Console.WriteLine(JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"Saved: {Path.GetFullPath(outputPath)}");
return summary.ServerErrorRate > maxServerErrorRate ||
       summary.NetworkErrorRate > maxNetworkErrorRate ? 2 : 0;

static double Percentile(Sample[] values, double percentile)
{
    if (values.Length == 0) return 0;
    int index = (int)Math.Ceiling(percentile * values.Length) - 1;
    return values[Math.Clamp(index, 0, values.Length - 1)].ElapsedMs;
}

static int ReadInt(string[] values, string name, int fallback) =>
    int.TryParse(ReadString(values, name, fallback.ToString()), out int value) ? value : fallback;

static double ReadDouble(string[] values, string name, double fallback) =>
    double.TryParse(ReadString(values, name, fallback.ToString()), out double value) ? value : fallback;

static string ReadString(string[] values, string name, string fallback)
{
    int index = Array.IndexOf(values, name);
    return index >= 0 && index + 1 < values.Length ? values[index + 1] : fallback;
}

internal sealed record Sample(
    string Url,
    int StatusCode,
    double ElapsedMs,
    long ContentLength,
    string? Error);
