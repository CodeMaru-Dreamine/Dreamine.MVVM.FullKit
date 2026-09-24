using GamePlatform.Application;
using GamePlatform.Domain;
using Microsoft.JSInterop;

namespace GamePlatform.Infrastructure;

/// <summary>Web Audio API를 통해 음악, 효과음, 크로스페이드를 출력합니다.</summary>
public sealed class BrowserAudioService(IJSRuntime js, IAudioAssetCatalog assets) : IAudioService
{
    private bool _disposed;

    /// <inheritdoc />
    public ValueTask<bool> InitializeAsync(SoundGameSettings settings, CancellationToken cancellationToken = default) =>
        js.InvokeAsync<bool>("maruAudio.initialize", cancellationToken, AudioSettingsDto.From(settings), assets.GetAllEffects());

    /// <inheritdoc />
    public ValueTask<bool> UnlockAsync(CancellationToken cancellationToken = default) =>
        js.InvokeAsync<bool>("maruAudio.unlock", cancellationToken);

    /// <inheritdoc />
    public ValueTask ApplySettingsAsync(SoundGameSettings settings, CancellationToken cancellationToken = default) =>
        js.InvokeVoidAsync("maruAudio.applySettings", cancellationToken, AudioSettingsDto.From(settings));

    /// <inheritdoc />
    public ValueTask<bool> PlayMusicAsync(AudioTrackDefinition track, SoundGameSettings settings, CancellationToken cancellationToken = default) =>
        js.InvokeAsync<bool>("maruAudio.playMusic", cancellationToken, track, AudioSettingsDto.From(settings));

    /// <inheritdoc />
    public ValueTask SetPausedAsync(bool paused, CancellationToken cancellationToken = default) =>
        js.InvokeVoidAsync("maruAudio.setPaused", cancellationToken, paused);

    /// <inheritdoc />
    public ValueTask PlayEffectAsync(string effectKey, bool uiEffect, CancellationToken cancellationToken = default) =>
        uiEffect
            ? js.InvokeVoidAsync("maruAudio.playEffect", cancellationToken, effectKey, true)
            : js.InvokeVoidAsync("maruAudio.playEffect", cancellationToken, assets.ResolveEffect(effectKey), false);

    /// <summary>브라우저 오디오 객체와 타이머를 정리합니다.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        try { await js.InvokeVoidAsync("maruAudio.dispose"); }
        catch (JSDisconnectedException) { }
    }

    private sealed record AudioSettingsDto(double Bgm, double Sfx, double Ui, bool Muted, int CrossfadeMilliseconds)
    {
        public static AudioSettingsDto From(SoundGameSettings settings) => new(
            GameSettingsPolicy.OutputVolume(settings.MasterVolume, settings.BgmVolume, settings.Muted),
            GameSettingsPolicy.OutputVolume(settings.MasterVolume, settings.SfxVolume, settings.Muted),
            GameSettingsPolicy.OutputVolume(settings.MasterVolume, settings.UiVolume, settings.Muted),
            settings.Muted,
            settings.CrossfadeMilliseconds);
    }
}
