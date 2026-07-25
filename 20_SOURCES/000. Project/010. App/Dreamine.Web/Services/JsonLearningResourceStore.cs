using System.IO;
using System.Text.Json;
using DreamineWeb.Models;

namespace DreamineWeb.Services;

/// <summary>영상과 샘플의 연결 정보를 App_Data JSON으로 관리합니다.</summary>
public sealed class JsonLearningResourceStore : ILearningResourceStore
{
    private readonly string _path;
    private readonly SiteSettingsService _siteSettings;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private List<LearningResource>? _cache;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public JsonLearningResourceStore(DreamineOptions options, SiteSettingsService siteSettings)
    {
        Directory.CreateDirectory(options.ResolvedDataPath);
        _path = Path.Combine(options.ResolvedDataPath, "learning_resources.json");
        _siteSettings = siteSettings;
    }

    public async Task<List<LearningResource>> GetAllAsync()
    {
        if (_cache is not null) return _cache;
        await _gate.WaitAsync();
        try
        {
            if (_cache is not null) return _cache;
            if (File.Exists(_path))
            {
                var json = await File.ReadAllTextAsync(_path);
                _cache = JsonSerializer.Deserialize<List<LearningResource>>(json, JsonOptions) ?? [];
            }
            else
            {
                var settings = _siteSettings.Current;
                _cache =
                [
                    new LearningResource
                    {
                        Id = "hello-dreamine",
                        Title = "HelloDreamine 시작하기",
                        TitleEn = "Getting started with HelloDreamine",
                        Description = "영상의 구현 과정을 따라가고 동일한 샘플 프로젝트를 내려받아 바로 실행해보세요.",
                        DescriptionEn = "Follow the video, then download and run the matching sample project.",
                        YouTubeUrl = settings.YouTubeUrl,
                        SampleName = settings.SampleDisplayName,
                        SampleDownloadUrl = settings.SampleDownloadUrl,
                        SortOrder = 10
                    }
                ];
                await PersistAsync();
            }

            return _cache;
        }
        finally { _gate.Release(); }
    }

    public async Task SaveAsync(LearningResource resource)
    {
        await GetAllAsync();
        await _gate.WaitAsync();
        try
        {
            var index = _cache!.FindIndex(x => x.Id == resource.Id);
            resource.UpdatedAt = DateTime.UtcNow;
            if (index >= 0) _cache[index] = resource;
            else _cache.Add(resource);
            await PersistAsync();
        }
        finally { _gate.Release(); }
    }

    public async Task DeleteAsync(string id)
    {
        await GetAllAsync();
        await _gate.WaitAsync();
        try
        {
            _cache!.RemoveAll(x => x.Id == id);
            await PersistAsync();
        }
        finally { _gate.Release(); }
    }

    private Task PersistAsync() => File.WriteAllTextAsync(
        _path, JsonSerializer.Serialize(_cache, JsonOptions));
}
