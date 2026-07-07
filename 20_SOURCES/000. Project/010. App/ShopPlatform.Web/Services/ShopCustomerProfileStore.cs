using System.Text.Json;
using ShopPlatform.Models;

namespace ShopPlatform.Services;

public sealed class ShopCustomerProfileStore
{
    private readonly string _root;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public ShopCustomerProfileStore(ShopOptions options)
    {
        _root = options.ResolvedDataPath;
    }

    public async Task<ShopCustomerProfile?> GetAsync(string slug, string userId)
    {
        var profiles = await LoadAsync(slug).ConfigureAwait(false);
        return profiles.FirstOrDefault(x => string.Equals(x.UserId, userId, StringComparison.Ordinal));
    }

    public async Task SaveAsync(string slug, ShopCustomerProfile profile)
    {
        var profiles = await LoadAsync(slug).ConfigureAwait(false);
        var index = profiles.FindIndex(x => string.Equals(x.UserId, profile.UserId, StringComparison.Ordinal));
        profile.UpdatedAt = DateTime.UtcNow;

        if (index >= 0)
        {
            profiles[index] = profile;
        }
        else
        {
            profiles.Add(profile);
        }

        Directory.CreateDirectory(ShopDir(slug));
        await File.WriteAllTextAsync(ProfilePath(slug), JsonSerializer.Serialize(profiles, JsonOptions)).ConfigureAwait(false);
    }

    private async Task<List<ShopCustomerProfile>> LoadAsync(string slug)
    {
        var path = ProfilePath(slug);
        if (!File.Exists(path))
        {
            return [];
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<List<ShopCustomerProfile>>(stream, JsonOptions).ConfigureAwait(false) ?? [];
    }

    private string ShopDir(string slug) => Path.Combine(_root, slug);
    private string ProfilePath(string slug) => Path.Combine(ShopDir(slug), "customers.json");
}
