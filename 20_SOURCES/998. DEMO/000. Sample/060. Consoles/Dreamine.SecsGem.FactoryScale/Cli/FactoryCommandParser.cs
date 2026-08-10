using System.Globalization;

namespace Dreamine.SecsGem.FactoryScale.Cli;

internal static class FactoryCommandParser
{
    private static readonly IReadOnlyDictionary<string, FactoryCommandKind> Commands =
        new Dictionary<string, FactoryCommandKind>(StringComparer.OrdinalIgnoreCase)
        {
            ["host"] = FactoryCommandKind.Host,
            ["worker"] = FactoryCommandKind.Worker,
            ["equipment"] = FactoryCommandKind.Equipment,
            ["scenario"] = FactoryCommandKind.Scenario,
            ["in-process"] = FactoryCommandKind.InProcess,
            ["multi-process"] = FactoryCommandKind.MultiProcess
        };

    private static readonly HashSet<string> ScenarioNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "factory-smoke",
        "scale",
        "idle-factory",
        "factory-normal",
        "normal-factory",
        "factory-busy",
        "busy-factory",
        "trace-burst",
        "large-message",
        "reconnect-storm",
        "host-restart",
        "fault-isolation",
        "soak"
    };

    private static readonly IReadOnlyDictionary<string, OptionSpec> OptionSpecs =
        new Dictionary<string, OptionSpec>(StringComparer.OrdinalIgnoreCase)
        {
            ["--equipment-count"] = OptionSpec.Int32(1, 1_000),
            ["--start-index"] = OptionSpec.Int32(1, 1_000_000),
            ["--count"] = OptionSpec.Int32(1, 1_000),
            ["--worker-count"] = OptionSpec.Int32(1, 100),
            ["--equipment-per-worker"] = OptionSpec.Int32(1, 100),
            ["--disconnect-count"] = OptionSpec.Int32(1, 1_000),
            ["--duration"] = OptionSpec.Duration(),
            ["--output"] = OptionSpec.Path(),
            ["--snapshot-directory"] = OptionSpec.Path(),
            ["--run-id"] = OptionSpec.Identifier(),
            ["--worker-id"] = OptionSpec.Identifier(),
            ["--control-directory"] = OptionSpec.Path(),
            ["--manifest"] = OptionSpec.Path(),
            ["--host"] = OptionSpec.String(),
            ["--mode"] = OptionSpec.Enum("in-process", "multi-process"),
            ["--port-range-start"] = OptionSpec.Int32(1_024, 65_535),
            ["--port-range-end"] = OptionSpec.Int32(1_024, 65_535),
            ["--connect-concurrency"] = OptionSpec.Int32(1, 1_024),
            ["--reconnect-concurrency"] = OptionSpec.Int32(1, 1_024),
            ["--message-concurrency"] = OptionSpec.Int32(1, 4_096),
            ["--receive-queue-capacity"] = OptionSpec.Int32(1, 1_000_000),
            ["--log-queue-capacity"] = OptionSpec.Int32(1, 1_000_000),
            ["--export-queue-capacity"] = OptionSpec.Int32(1, 1_000_000),
            ["--pending-per-equipment"] = OptionSpec.Int32(1, 100_000),
            ["--pending-global"] = OptionSpec.Int32(1, 1_000_000),
            ["--messages-per-second"] = OptionSpec.Double(0.001, 100_000),
            ["--message-bytes"] = OptionSpec.Int32(0, 16 * 1024 * 1024 - 14),
            ["--snapshot-interval"] = OptionSpec.Duration(),
            ["--heartbeat-interval"] = OptionSpec.Duration(),
            ["--ready-timeout"] = OptionSpec.Duration(),
            ["--shutdown-timeout"] = OptionSpec.Duration(),
            ["--worker-executable"] = OptionSpec.Path()
        };

    private static readonly HashSet<string> HostOptions = Set(
        "--equipment-count", "--duration", "--output", "--snapshot-directory", "--run-id",
        "--manifest", "--host", "--connect-concurrency", "--reconnect-concurrency",
        "--message-concurrency", "--receive-queue-capacity", "--log-queue-capacity",
        "--export-queue-capacity", "--pending-per-equipment", "--pending-global",
        "--snapshot-interval", "--shutdown-timeout");

    private static readonly HashSet<string> WorkerOptions = Set(
        "--start-index", "--count", "--duration", "--output", "--snapshot-directory",
        "--run-id", "--worker-id", "--control-directory", "--host", "--port-range-start",
        "--port-range-end", "--receive-queue-capacity", "--log-queue-capacity",
        "--export-queue-capacity", "--heartbeat-interval", "--shutdown-timeout");

    private static readonly HashSet<string> EquipmentOptions = Set(
        "--equipment-count", "--start-index", "--count", "--duration", "--output",
        "--snapshot-directory", "--run-id", "--control-directory", "--host",
        "--port-range-start", "--port-range-end", "--receive-queue-capacity",
        "--log-queue-capacity", "--heartbeat-interval", "--shutdown-timeout");

    private static readonly HashSet<string> ScenarioOptions = Set(
        "--equipment-count", "--start-index", "--count", "--worker-count",
        "--equipment-per-worker", "--disconnect-count", "--duration", "--output",
        "--snapshot-directory", "--run-id", "--control-directory", "--manifest", "--host",
        "--mode", "--port-range-start", "--port-range-end", "--connect-concurrency",
        "--reconnect-concurrency", "--message-concurrency", "--receive-queue-capacity",
        "--log-queue-capacity", "--export-queue-capacity", "--pending-per-equipment",
        "--pending-global", "--messages-per-second", "--message-bytes", "--snapshot-interval",
        "--heartbeat-interval", "--ready-timeout", "--shutdown-timeout", "--worker-executable");

    internal static string Usage => """
        Dreamine SECS/GEM Factory-Scale Host validation runner

        Usage:
          Dreamine.SecsGem.FactoryScale host [options]
          Dreamine.SecsGem.FactoryScale worker --start-index N --count N [options]
          Dreamine.SecsGem.FactoryScale equipment [options]
          Dreamine.SecsGem.FactoryScale scenario NAME [options]
          Dreamine.SecsGem.FactoryScale in-process NAME [options]
          Dreamine.SecsGem.FactoryScale multi-process NAME [options]

        Scenario names:
          factory-smoke | scale | idle-factory | factory-normal | factory-busy
          trace-burst | large-message | reconnect-storm | host-restart
          fault-isolation | soak

        Common scenario options:
          --equipment-count N          Requested equipment count (1..1000)
          --duration HH:MM:SS          Positive duration, up to 7 days
          --output PATH                JSON result path
          --snapshot-directory PATH    Periodic soak snapshot directory
          --mode MODE                  in-process | multi-process
          --worker-count N             Worker process count (1..100)
          --equipment-per-worker N     Equipment per worker (1..100)
          --disconnect-count N         Reconnect-storm target count
          --messages-per-second N      Aggregate or per-scenario target rate
          --message-bytes N            Binary payload bytes (0..16777202; profile overhead also applies)
          --connect-concurrency N       Bounded connect concurrency
          --reconnect-concurrency N     Bounded reconnect concurrency
          --message-concurrency N       Bounded message concurrency
          --receive-queue-capacity N    Bounded protocol receive queue
          --log-queue-capacity N        Bounded diagnostic log queue
          --export-queue-capacity N     Bounded result export queue
          --pending-per-equipment N     Per-equipment pending transaction limit
          --pending-global N            Global pending transaction limit
          --port-range-start N          Managed test-port range start
          --port-range-end N            Managed test-port range end
          --ready-timeout HH:MM:SS      Worker ready deadline
          --shutdown-timeout HH:MM:SS   Graceful worker stop deadline
          --help                        Show this help

        Examples:
          dotnet run --project Dreamine.SecsGem.FactoryScale.csproj -- scenario factory-normal --mode in-process --equipment-count 500 --duration 01:00:00
          dotnet run --project Dreamine.SecsGem.FactoryScale.csproj -- multi-process reconnect-storm --equipment-count 500 --disconnect-count 100
          dotnet run --project Dreamine.SecsGem.FactoryScale.csproj -- worker --start-index 1 --count 100

        Exit codes: 0 success, 1 runtime failure, 2 acceptance failure, 64 usage error, 130 canceled.
        """;

    internal static bool IsHelpRequest(IReadOnlyList<string> arguments) =>
        arguments.Count == 1 && arguments[0] is "--help" or "-h" or "help" ||
        arguments.Count == 2 && Commands.ContainsKey(arguments[0]) && arguments[1] is "--help" or "-h";

    internal static FactoryCommandLine Parse(IReadOnlyList<string> arguments)
    {
        if (arguments.Count == 0)
            throw new FactoryCommandLineException("A command is required.");
        if (!Commands.TryGetValue(arguments[0], out var command))
            throw new FactoryCommandLineException($"Unknown command '{arguments[0]}'.");

        var index = 1;
        string? scenario = null;
        if (command is FactoryCommandKind.Scenario or FactoryCommandKind.InProcess or FactoryCommandKind.MultiProcess)
        {
            if (index >= arguments.Count || arguments[index].StartsWith("--", StringComparison.Ordinal))
                throw new FactoryCommandLineException($"Command '{arguments[0]}' requires a scenario name.");
            scenario = arguments[index++];
            if (!ScenarioNames.Contains(scenario))
                throw new FactoryCommandLineException($"Unknown scenario '{scenario}'.");
        }
        else if (index < arguments.Count && !arguments[index].StartsWith("--", StringComparison.Ordinal))
        {
            throw new FactoryCommandLineException($"Command '{arguments[0]}' does not accept positional value '{arguments[index]}'.");
        }

        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var allowed = AllowedOptions(command);
        while (index < arguments.Count)
        {
            var name = arguments[index++];
            if (!name.StartsWith("--", StringComparison.Ordinal) || name.Contains('='))
                throw new FactoryCommandLineException($"Expected a '--name value' option but found '{name}'.");
            if (!OptionSpecs.TryGetValue(name, out var spec))
                throw new FactoryCommandLineException($"Unknown option '{name}'.");
            if (!allowed.Contains(name))
                throw new FactoryCommandLineException($"Option '{name}' is not valid for command '{arguments[0]}'.");
            if (index >= arguments.Count || arguments[index].StartsWith("--", StringComparison.Ordinal))
                throw new FactoryCommandLineException($"Option '{name}' requires a value.");
            if (!options.TryAdd(name, arguments[index++]))
                throw new FactoryCommandLineException($"Option '{name}' was specified more than once.");
            ValidateValue(name, options[name], spec);
        }

        ValidateSemantics(command, options);
        return new FactoryCommandLine(command, scenario, options);
    }

    internal static TimeSpan ParseDuration(string value, string name)
    {
        if (!TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var duration) ||
            duration <= TimeSpan.Zero || duration > TimeSpan.FromDays(7))
            throw new FactoryCommandLineException(
                $"Option '{Normalize(name)}' must be a positive duration no greater than 7 days.");
        return duration;
    }

    private static HashSet<string> AllowedOptions(FactoryCommandKind command) => command switch
    {
        FactoryCommandKind.Host => HostOptions,
        FactoryCommandKind.Worker => WorkerOptions,
        FactoryCommandKind.Equipment => EquipmentOptions,
        _ => ScenarioOptions
    };

    private static void ValidateValue(string name, string value, OptionSpec spec)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new FactoryCommandLineException($"Option '{name}' cannot be empty.");
        switch (spec.Kind)
        {
            case OptionValueKind.Int32:
                if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer) ||
                    integer < spec.Minimum || integer > spec.Maximum)
                    throw new FactoryCommandLineException(
                        $"Option '{name}' must be an integer between {spec.Minimum:0} and {spec.Maximum:0}.");
                break;
            case OptionValueKind.Double:
                if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ||
                    !double.IsFinite(number) || number < spec.Minimum || number > spec.Maximum)
                    throw new FactoryCommandLineException(
                        $"Option '{name}' must be a number between {spec.Minimum} and {spec.Maximum}.");
                break;
            case OptionValueKind.Duration:
                _ = ParseDuration(value, name);
                break;
            case OptionValueKind.Identifier:
                if (value.Length > 64 || value.Any(character =>
                        !char.IsAsciiLetterOrDigit(character) && character is not '-' and not '_' and not '.'))
                    throw new FactoryCommandLineException(
                        $"Option '{name}' must contain at most 64 ASCII letters, digits, '.', '-' or '_'.");
                break;
            case OptionValueKind.Path:
                try { _ = Path.GetFullPath(value); }
                catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
                {
                    throw new FactoryCommandLineException($"Option '{name}' is not a valid path: {exception.Message}");
                }
                break;
            case OptionValueKind.Enum:
                if (spec.AllowedValues is null || !spec.AllowedValues.Contains(value))
                    throw new FactoryCommandLineException(
                        $"Option '{name}' must be one of: {string.Join(", ", spec.AllowedValues ?? [])}.");
                break;
            case OptionValueKind.String:
                break;
            default:
                throw new InvalidOperationException($"Unsupported option type {spec.Kind}.");
        }
    }

    private static void ValidateSemantics(
        FactoryCommandKind command,
        IReadOnlyDictionary<string, string> options)
    {
        if (command == FactoryCommandKind.Worker)
        {
            foreach (var required in new[] { "--start-index", "--count" })
                if (!options.ContainsKey(required))
                    throw new FactoryCommandLineException($"Worker command requires option '{required}'.");
            if (ReadInt(options, "--count", 0) > 100)
                throw new FactoryCommandLineException(
                    "A worker may own at most 100 equipment endpoints. Use multiple workers for a larger fleet.");
        }

        var hasRangeStart = options.TryGetValue("--port-range-start", out var rangeStartText);
        var hasRangeEnd = options.TryGetValue("--port-range-end", out var rangeEndText);
        if (hasRangeStart != hasRangeEnd)
            throw new FactoryCommandLineException(
                "Options '--port-range-start' and '--port-range-end' must be specified together.");
        if (hasRangeStart && hasRangeEnd)
        {
            var start = int.Parse(rangeStartText!, CultureInfo.InvariantCulture);
            var end = int.Parse(rangeEndText!, CultureInfo.InvariantCulture);
            if (end < start)
                throw new FactoryCommandLineException("Port range end must be greater than or equal to its start.");
            var requiredPorts = ReadInt(options, "--count", ReadInt(options, "--equipment-count", 1));
            if (end - start + 1 < requiredPorts)
                throw new FactoryCommandLineException(
                    $"The managed port range contains fewer than the requested {requiredPorts} equipment endpoints.");
        }

        var equipmentCount = ReadInt(options, "--equipment-count", 0);
        var disconnectCount = ReadInt(options, "--disconnect-count", 0);
        if (equipmentCount > 0 && disconnectCount > equipmentCount)
            throw new FactoryCommandLineException(
                "Option '--disconnect-count' cannot exceed '--equipment-count'.");

        var workers = ReadInt(options, "--worker-count", 0);
        var perWorker = ReadInt(options, "--equipment-per-worker", 0);
        if (equipmentCount > 0 && workers > 0 && perWorker > 0 && workers * perWorker < equipmentCount)
            throw new FactoryCommandLineException(
                "Worker capacity is smaller than '--equipment-count'. Increase '--worker-count' or '--equipment-per-worker'.");

        var perEquipment = ReadInt(options, "--pending-per-equipment", 0);
        var global = ReadInt(options, "--pending-global", 0);
        if (perEquipment > 0 && global > 0 && global < perEquipment)
            throw new FactoryCommandLineException(
                "Option '--pending-global' cannot be smaller than '--pending-per-equipment'.");

        if (command == FactoryCommandKind.InProcess &&
            options.TryGetValue("--mode", out var inProcessMode) &&
            !inProcessMode.Equals("in-process", StringComparison.OrdinalIgnoreCase))
            throw new FactoryCommandLineException("The in-process command cannot select multi-process mode.");
        if (command == FactoryCommandKind.MultiProcess &&
            options.TryGetValue("--mode", out var multiProcessMode) &&
            !multiProcessMode.Equals("multi-process", StringComparison.OrdinalIgnoreCase))
            throw new FactoryCommandLineException("The multi-process command cannot select in-process mode.");
    }

    private static int ReadInt(IReadOnlyDictionary<string, string> options, string name, int fallback) =>
        options.TryGetValue(name, out var value)
            ? int.Parse(value, CultureInfo.InvariantCulture)
            : fallback;

    private static HashSet<string> Set(params string[] values) =>
        new(values, StringComparer.OrdinalIgnoreCase);

    private static string Normalize(string name) => name.StartsWith("--", StringComparison.Ordinal)
        ? name
        : "--" + name;

    private enum OptionValueKind
    {
        String,
        Int32,
        Double,
        Duration,
        Identifier,
        Path,
        Enum
    }

    private sealed record OptionSpec(
        OptionValueKind Kind,
        double Minimum = 0,
        double Maximum = 0,
        HashSet<string>? AllowedValues = null)
    {
        internal static OptionSpec String() => new(OptionValueKind.String);
        internal static OptionSpec Int32(int minimum, int maximum) =>
            new(OptionValueKind.Int32, minimum, maximum);
        internal static OptionSpec Double(double minimum, double maximum) =>
            new(OptionValueKind.Double, minimum, maximum);
        internal static OptionSpec Duration() => new(OptionValueKind.Duration);
        internal static OptionSpec Identifier() => new(OptionValueKind.Identifier);
        internal static OptionSpec Path() => new(OptionValueKind.Path);
        internal static OptionSpec Enum(params string[] values) =>
            new(OptionValueKind.Enum, AllowedValues: new HashSet<string>(values, StringComparer.OrdinalIgnoreCase));
    }
}
