using System.Globalization;

namespace Dreamine.SecsGem.FactoryScale.Cli;

internal enum FactoryCommandKind
{
    Host,
    Worker,
    Equipment,
    Scenario,
    InProcess,
    MultiProcess
}

internal sealed class FactoryCommandLine
{
    private readonly IReadOnlyDictionary<string, string> _options;

    internal FactoryCommandLine(
        FactoryCommandKind command,
        string? scenario,
        IReadOnlyDictionary<string, string> options)
    {
        Command = command;
        Scenario = scenario;
        _options = options;
    }

    internal FactoryCommandKind Command { get; }
    internal string? Scenario { get; }
    internal IReadOnlyDictionary<string, string> Options => _options;

    internal bool HasOption(string name) => _options.ContainsKey(Normalize(name));

    internal string GetString(string name, string fallback = "") =>
        _options.TryGetValue(Normalize(name), out var value) ? value : fallback;

    internal int GetInt32(string name, int fallback = 0) =>
        _options.TryGetValue(Normalize(name), out var value)
            ? int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture)
            : fallback;

    internal long GetInt64(string name, long fallback = 0) =>
        _options.TryGetValue(Normalize(name), out var value)
            ? long.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture)
            : fallback;

    internal double GetDouble(string name, double fallback = 0) =>
        _options.TryGetValue(Normalize(name), out var value)
            ? double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture)
            : fallback;

    internal TimeSpan GetTimeSpan(string name, TimeSpan fallback) =>
        _options.TryGetValue(Normalize(name), out var value)
            ? FactoryCommandParser.ParseDuration(value, name)
            : fallback;

    private static string Normalize(string name) => name.StartsWith("--", StringComparison.Ordinal)
        ? name
        : "--" + name;
}

internal sealed class FactoryCommandLineException(string message) : Exception(message);
