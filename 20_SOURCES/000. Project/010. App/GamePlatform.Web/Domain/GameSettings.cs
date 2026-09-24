namespace GamePlatform.Domain;

/// <summary>게임 화질 단계를 나타냅니다.</summary>
public enum GameQuality { Low, Medium, High }

/// <summary>게임 표시 프레임 정책을 나타냅니다.</summary>
public enum GameFrameRate { Fps30 = 30, Fps60 = 60 }

/// <summary>계정에 저장되는 전체 게임 설정입니다.</summary>
public sealed class GameSettings
{
    /// <summary>일반 설정입니다.</summary>
    public GeneralGameSettings General { get; set; } = new();
    /// <summary>음향 설정입니다.</summary>
    public SoundGameSettings Sound { get; set; } = new();
    /// <summary>화면 설정입니다.</summary>
    public DisplayGameSettings Display { get; set; } = new();
    /// <summary>전투 표현 및 자동화 설정입니다.</summary>
    public CombatGameSettings Combat { get; set; } = new();
}

/// <summary>언어와 편의 기능 설정입니다.</summary>
public sealed class GeneralGameSettings
{
    /// <summary>마지막으로 접속한 게임 채널 키입니다.</summary>
    public string ChannelKey { get; set; } = "cheongun-1";
    /// <summary>계정의 최초 채널 선택이 확정되어 더 이상 변경할 수 없는지 여부입니다.</summary>
    public bool ChannelConfirmed { get; set; }
    /// <summary>표시 언어 코드입니다.</summary>
    public string Language { get; set; } = "ko-KR";
    /// <summary>게임 알림 사용 여부입니다.</summary>
    public bool NotificationsEnabled { get; set; } = true;
    /// <summary>지원 브라우저에서 진동 피드백을 사용할지 여부입니다.</summary>
    public bool VibrationEnabled { get; set; } = true;
    /// <summary>절전 모드 사용 여부입니다.</summary>
    public bool PowerSavingEnabled { get; set; }
    /// <summary>마지막 입력 후 절전 화면을 자동으로 여는 대기 시간(분)입니다. 0이면 자동 진입하지 않습니다.</summary>
    public int ScreenSaverTimeoutMinutes { get; set; } = 3;
    /// <summary>본문을 확인한 계정 우편 키 목록입니다.</summary>
    public List<string> ReadMailKeys { get; set; } = [];
}

/// <summary>음량과 음악 재생 설정입니다.</summary>
public sealed class SoundGameSettings
{
    /// <summary>전체 음량(0~100)입니다.</summary>
    public int MasterVolume { get; set; } = 80;
    /// <summary>배경 음악 음량(0~100)입니다.</summary>
    public int BgmVolume { get; set; } = 40;
    /// <summary>전투 효과음 음량(0~100)입니다.</summary>
    public int SfxVolume { get; set; } = 80;
    /// <summary>UI 효과음 음량(0~100)입니다.</summary>
    public int UiVolume { get; set; } = 55;
    /// <summary>전체 음소거 여부입니다.</summary>
    public bool Muted { get; set; }
    /// <summary>탭이 숨겨져도 음악을 유지할지 여부입니다.</summary>
    public bool BackgroundPlayback { get; set; }
    /// <summary>곡 전환 크로스페이드 밀리초입니다.</summary>
    public int CrossfadeMilliseconds { get; set; } = 1500;
}

/// <summary>렌더링 품질과 전투 표현 설정입니다.</summary>
public sealed class DisplayGameSettings
{
    /// <summary>화질 단계입니다.</summary>
    public GameQuality Quality { get; set; } = GameQuality.High;
    /// <summary>프레임 정책입니다.</summary>
    public GameFrameRate FrameRate { get; set; } = GameFrameRate.Fps60;
    /// <summary>화면 흔들림 표시 여부입니다.</summary>
    public bool ScreenShake { get; set; } = true;
    /// <summary>검격 효과 표시 여부입니다.</summary>
    public bool SlashEffects { get; set; } = true;
    /// <summary>환경 파티클 표시 여부입니다.</summary>
    public bool AmbientEffects { get; set; } = true;
    /// <summary>데미지 숫자 표시 여부입니다.</summary>
    public bool DamageNumbers { get; set; } = true;
    /// <summary>절전 모드 사용 여부입니다.</summary>
    public bool PowerSaving { get; set; }
}

/// <summary>서버 전투 규칙과 분리된 자동화 및 연출 설정입니다.</summary>
public sealed class CombatGameSettings
{
    /// <summary>AUTO 설정의 계정 저장값입니다.</summary>
    public bool AutoEnabled { get; set; }
    /// <summary>자동 스킬 사용 여부입니다.</summary>
    public bool AutoSkills { get; set; }
    /// <summary>자동 필살기 사용 여부입니다.</summary>
    public bool AutoUltimate { get; set; }
    /// <summary>보스 자동 도전 여부입니다.</summary>
    public bool AutoBossChallenge { get; set; } = true;
    /// <summary>데미지 숫자 합산 표시 여부입니다.</summary>
    public bool AggregateDamage { get; set; }
    /// <summary>치명타 강조 연출 여부입니다.</summary>
    public bool CriticalEffects { get; set; } = true;
    /// <summary>절전 중 전투 연출 생략 여부입니다.</summary>
    public bool SkipEffectsWhileSaving { get; set; } = true;
}

/// <summary>설정 범위와 음량 계산을 검증하는 도메인 정책입니다.</summary>
public static class GameSettingsPolicy
{
    /// <summary>설정값을 허용 범위로 복제·정규화합니다.</summary>
    public static GameSettings Normalize(GameSettings? value)
    {
        value ??= new GameSettings();
        var general = value.General ?? new GeneralGameSettings();
        var sound = value.Sound ?? new SoundGameSettings();
        var display = value.Display ?? new DisplayGameSettings();
        var combat = value.Combat ?? new CombatGameSettings();
        var powerSaving = general.PowerSavingEnabled || display.PowerSaving;
        return new GameSettings
        {
            General = new GeneralGameSettings
            {
                ChannelKey = general.ChannelKey is "cheongun-1" or "cheongun-2" or "baekya-1" or "baekya-2"
                    ? general.ChannelKey
                    : "cheongun-1",
                ChannelConfirmed = general.ChannelConfirmed,
                Language = general.Language is "ko-KR" or "en-US" ? general.Language : "ko-KR",
                NotificationsEnabled = general.NotificationsEnabled,
                VibrationEnabled = general.VibrationEnabled,
                PowerSavingEnabled = powerSaving,
                ScreenSaverTimeoutMinutes = general.ScreenSaverTimeoutMinutes is 0 or 1 or 3 or 5 or 10 or 15 or 30
                    ? general.ScreenSaverTimeoutMinutes
                    : 3,
                ReadMailKeys = (general.ReadMailKeys ?? [])
                    .Where(IsValidReadKey)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .TakeLast(500)
                    .ToList()
            },
            Sound = new SoundGameSettings
            {
                MasterVolume = ClampVolume(sound.MasterVolume),
                BgmVolume = ClampVolume(sound.BgmVolume),
                SfxVolume = ClampVolume(sound.SfxVolume),
                UiVolume = ClampVolume(sound.UiVolume),
                Muted = sound.Muted,
                BackgroundPlayback = sound.BackgroundPlayback,
                CrossfadeMilliseconds = Math.Clamp(sound.CrossfadeMilliseconds, 250, 5000)
            },
            Display = new DisplayGameSettings
            {
                Quality = Enum.IsDefined(display.Quality) ? display.Quality : GameQuality.High,
                FrameRate = Enum.IsDefined(display.FrameRate) ? display.FrameRate : GameFrameRate.Fps60,
                ScreenShake = display.ScreenShake,
                SlashEffects = display.SlashEffects,
                AmbientEffects = display.AmbientEffects,
                DamageNumbers = display.DamageNumbers,
                PowerSaving = powerSaving
            },
            Combat = new CombatGameSettings
            {
                AutoEnabled = combat.AutoEnabled,
                AutoSkills = combat.AutoSkills,
                AutoUltimate = combat.AutoUltimate,
                AutoBossChallenge = combat.AutoBossChallenge,
                AggregateDamage = combat.AggregateDamage,
                CriticalEffects = combat.CriticalEffects,
                SkipEffectsWhileSaving = combat.SkipEffectsWhileSaving
            }
        };
    }

    /// <summary>전체 음량과 채널 음량을 곱한 실제 출력값(0~1)을 계산합니다.</summary>
    public static double OutputVolume(int masterVolume, int channelVolume, bool muted) =>
        muted ? 0d : ClampVolume(masterVolume) / 100d * (ClampVolume(channelVolume) / 100d);

    private static int ClampVolume(int value) => Math.Clamp(value, 0, 100);

    private static bool IsValidReadKey(string? key)
    {
        if (key is "trial-arcade-open") return true;
        if (string.IsNullOrWhiteSpace(key)) return false;

        const string noticePrefix = "admin-notice:";
        const string mailPrefix = "admin-mail:";
        var id = key.StartsWith(noticePrefix, StringComparison.OrdinalIgnoreCase)
            ? key[noticePrefix.Length..]
            : key.StartsWith(mailPrefix, StringComparison.OrdinalIgnoreCase)
                ? key[mailPrefix.Length..]
                : string.Empty;
        return id.Length == 32 && Guid.TryParseExact(id, "N", out _);
    }
}
