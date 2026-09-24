using System.Text.Json;

namespace GamePlatform.Domain;

/// <summary>독자 생성 음악 한 곡의 재생 및 라이선스 메타데이터입니다.</summary>
public sealed record AudioTrackDefinition
{
    /// <summary>영구 음악 자산 키입니다.</summary>
    public required string AssetKey { get; init; }
    /// <summary>화면에 표시할 곡명입니다.</summary>
    public required string Title { get; init; }
    /// <summary>작곡 또는 생성 출처입니다.</summary>
    public required string ComposerOrSource { get; init; }
    /// <summary>이용 조건입니다.</summary>
    public required string License { get; init; }
    /// <summary>루프 시작 초입니다.</summary>
    public required double LoopStart { get; init; }
    /// <summary>루프 종료 초입니다.</summary>
    public required double LoopEnd { get; init; }
    /// <summary>정규화 게인 배율입니다.</summary>
    public required double VolumeNormalization { get; init; }
    /// <summary>프로시저럴 원음의 기준 주파수입니다.</summary>
    public required double RootHz { get; init; }
    /// <summary>재현 가능한 음색 시드입니다.</summary>
    public required int Seed { get; init; }
    /// <summary>실제 압축 음원 파일명입니다. 생략하면 자산 키와 같은 MP3를 사용합니다.</summary>
    public string? FileName { get; init; }
    /// <summary>브라우저에서 우선 재생할 압축 음원 자산 URL입니다.</summary>
    public string SourceUrl => $"/audio/maru-idle/{FileName ?? $"{AssetKey}.mp3"}?v=20260821-suno-1";
}

/// <summary>짧은 전투 효과음 파일의 출력 정책과 출처 메타데이터입니다.</summary>
public sealed record AudioEffectDefinition
{
    /// <summary>전투 코드가 참조하는 논리 자산 키입니다.</summary>
    public required string AssetKey { get; init; }
    /// <summary>실제 압축 효과음 파일명입니다.</summary>
    public required string FileName { get; init; }
    /// <summary>원본 또는 생성 출처입니다.</summary>
    public required string Source { get; init; }
    /// <summary>프로젝트 이용 조건입니다.</summary>
    public required string License { get; init; }
    /// <summary>개별 효과음 정규화 게인입니다.</summary>
    public required double VolumeNormalization { get; init; }
    /// <summary>반복감을 줄이기 위한 최소 재생 속도입니다.</summary>
    public required double PlaybackRateMin { get; init; }
    /// <summary>반복감을 줄이기 위한 최대 재생 속도입니다.</summary>
    public required double PlaybackRateMax { get; init; }
    /// <summary>동시에 살아 있을 수 있는 동일 효과음 보이스 수입니다.</summary>
    public required int MaxVoices { get; init; }
    /// <summary>동일 효과음 재생 사이의 최소 간격입니다.</summary>
    public required int MinimumIntervalMilliseconds { get; init; }
    /// <summary>브라우저용 효과음 URL입니다.</summary>
    public string SourceUrl => $"/audio/maru-idle/{FileName}?v=20260821-suno-1";
}

/// <summary>검증된 전투 효과음 자산 카탈로그입니다.</summary>
public sealed class AudioEffectCatalog
{
    private readonly IReadOnlyList<AudioEffectDefinition> _effects;
    private readonly IReadOnlyDictionary<string, AudioEffectDefinition> _byKey;

    /// <summary>효과음 정의를 검증하고 조회 카탈로그를 만듭니다.</summary>
    public AudioEffectCatalog(IEnumerable<AudioEffectDefinition> effects)
    {
        _effects = effects.ToArray();
        if (_effects.Count == 0) throw new InvalidOperationException("효과음 카탈로그가 비어 있습니다.");
        if (_effects.Any(effect => string.IsNullOrWhiteSpace(effect.AssetKey)
                                 || string.IsNullOrWhiteSpace(effect.FileName)
                                 || effect.VolumeNormalization is <= 0 or > 2
                                 || effect.PlaybackRateMin is < .5 or > 2
                                 || effect.PlaybackRateMax < effect.PlaybackRateMin
                                 || effect.MaxVoices is < 1 or > 16
                                 || effect.MinimumIntervalMilliseconds is < 0 or > 5000))
            throw new InvalidOperationException("효과음 메타데이터가 유효하지 않습니다.");
        _byKey = _effects.ToDictionary(effect => effect.AssetKey, StringComparer.OrdinalIgnoreCase);
        if (_byKey.Count != _effects.Count) throw new InvalidOperationException("효과음 자산 키가 중복되었습니다.");
    }

    /// <summary>JSON 파일에서 효과음 카탈로그를 읽습니다.</summary>
    public static AudioEffectCatalog Load(string path)
    {
        var effects = JsonSerializer.Deserialize<List<AudioEffectDefinition>>(
            File.ReadAllText(path), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("효과음 메타데이터가 비어 있습니다.");
        return new AudioEffectCatalog(effects);
    }

    /// <summary>논리 키에 해당하는 효과음 정의를 반환합니다.</summary>
    public AudioEffectDefinition Get(string assetKey) =>
        _byKey.TryGetValue(assetKey, out var effect)
            ? effect
            : throw new InvalidOperationException($"효과음 자산 '{assetKey}'을 찾을 수 없습니다.");

    /// <summary>초기 브라우저 프리로드에 사용할 모든 효과음 정의입니다.</summary>
    public IReadOnlyCollection<AudioEffectDefinition> All => _effects;
}

/// <summary>검증된 프로젝트 음악 메타데이터 카탈로그입니다.</summary>
public sealed class AudioTrackCatalog
{
    private readonly IReadOnlyList<AudioTrackDefinition> _tracks;
    private readonly IReadOnlyDictionary<string, AudioTrackDefinition> _byKey;

    /// <summary>음악 정의를 검증하고 조회 카탈로그를 만듭니다.</summary>
    public AudioTrackCatalog(IEnumerable<AudioTrackDefinition> tracks)
    {
        _tracks = tracks.ToArray();
        if (_tracks.Count == 0) throw new InvalidOperationException("음악 카탈로그가 비어 있습니다.");
        if (_tracks.Any(track => string.IsNullOrWhiteSpace(track.AssetKey) || string.IsNullOrWhiteSpace(track.Title) ||
                                 track.LoopEnd <= track.LoopStart || track.VolumeNormalization is <= 0 or > 2))
            throw new InvalidOperationException("음악 메타데이터가 유효하지 않습니다.");
        _byKey = _tracks.ToDictionary(track => track.AssetKey, StringComparer.OrdinalIgnoreCase);
        if (_byKey.Count != _tracks.Count) throw new InvalidOperationException("음악 자산 키가 중복되었습니다.");
    }

    /// <summary>JSON 파일에서 음악 카탈로그를 읽습니다.</summary>
    public static AudioTrackCatalog Load(string path)
    {
        var tracks = JsonSerializer.Deserialize<List<AudioTrackDefinition>>(
            File.ReadAllText(path), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("음악 메타데이터가 비어 있습니다.");
        return new AudioTrackCatalog(tracks);
    }

    /// <summary>자산 키에 해당하는 음악 정의를 반환합니다.</summary>
    public AudioTrackDefinition Get(string assetKey) =>
        _byKey.TryGetValue(assetKey, out var track)
            ? track
            : throw new InvalidOperationException($"음악 자산 '{assetKey}'을 찾을 수 없습니다.");

    /// <summary>현재 곡을 기준으로 이전 또는 다음 곡을 순환 조회합니다.</summary>
    public AudioTrackDefinition Adjacent(string assetKey, int direction)
    {
        var index = Array.FindIndex(_tracks.ToArray(), track => string.Equals(track.AssetKey, assetKey, StringComparison.OrdinalIgnoreCase));
        if (index < 0) return _tracks[0];
        var next = (index + Math.Sign(direction) + _tracks.Count) % _tracks.Count;
        return _tracks[next];
    }
}
