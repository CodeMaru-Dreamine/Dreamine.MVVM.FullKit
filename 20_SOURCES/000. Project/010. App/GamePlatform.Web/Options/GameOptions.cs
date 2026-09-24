namespace GamePlatform.Options;

public sealed class GameOptions
{
    public string BaseUrl { get; init; } = "https://games.codemaru.co.kr";

    public string DatabasePath { get; init; } = Path.Combine(
        AppContext.BaseDirectory,
        "App_Data",
        "game.db");

    public static GameOptions From(IConfiguration configuration)
    {
        var section = configuration.GetSection("Game");
        var configuredPath = section["DatabasePath"];
        var fallbackPath = Path.Combine(AppContext.BaseDirectory, "App_Data", "game.db");
        var databasePath = string.IsNullOrWhiteSpace(configuredPath)
            ? fallbackPath
            : Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredPath));

        return new GameOptions
        {
            BaseUrl = section["BaseUrl"] ?? "https://games.codemaru.co.kr",
            DatabasePath = databasePath
        };
    }
}
