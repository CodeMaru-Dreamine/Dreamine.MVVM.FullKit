namespace GamePlatform.Domain;

/// <summary>공격 요청이 시작된 입력 경로를 구분합니다.</summary>
public enum AttackSource
{
    /// <summary>터치, 포인터 또는 키보드로 시작한 공격입니다.</summary>
    Manual,

    /// <summary>해금된 자동공격 설정으로 시작한 공격입니다.</summary>
    Automatic
}

/// <summary>서버가 관리하는 전투 진행 상태입니다.</summary>
public enum CombatPhase
{
    /// <summary>콘텐츠를 불러오는 중입니다.</summary>
    Loading,

    /// <summary>보스 또는 지역 진입 연출 중입니다.</summary>
    Intro,

    /// <summary>공격을 받을 수 있는 전투 중입니다.</summary>
    Fighting,

    /// <summary>적 처치 결과를 표시하는 중입니다.</summary>
    Victory,

    /// <summary>플레이어가 쓰러져 재정비하는 중입니다.</summary>
    Defeat,

    /// <summary>다음 스테이지로 전환하는 중입니다.</summary>
    Transitioning
}
