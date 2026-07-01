using FamiliesApp.Models;

namespace FamiliesApp.Services;

/// <summary>
/// \file IGlobalSettingsStore.cs
/// \brief 전체 사이트 공통 설정(GlobalSettings) 저장소 추상화.
/// </summary>
public interface IGlobalSettingsStore
{
    Task<GlobalSettings> GetAsync(CancellationToken ct = default);
    Task SaveAsync(GlobalSettings settings, CancellationToken ct = default);
}
