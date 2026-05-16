namespace DreamineVMS.Services.Dashboard;

/// <summary>
/// \brief VmsDashboardState를 도메인 이벤트 기반으로 자동 갱신하는 서비스입니다.
/// </summary>
/// <remarks>
/// MainWindowViewModel과 같은 UI 계층이 아니라, 카메라 도메인(Repository / RuntimeState)을
/// 직접 구독하여 IHybridStateStore&lt;VmsDashboardState&gt;를 갱신합니다.
/// 이렇게 함으로써 WPF UI가 떠 있지 않아도 Server Dashboard(6080)와 Embedded Dashboard에
/// 정확한 카메라 카운트와 상태가 노출됩니다.
/// </remarks>
public interface IVmsDashboardStateService
{
    /// <summary>
    /// \brief 서비스를 시작하고 도메인 이벤트 구독을 활성화합니다.
    /// </summary>
    /// <remarks>
    /// 동일 인스턴스에 대해 두 번 이상 호출해도 안전하도록 구현해야 합니다.
    /// </remarks>
    void Start();

    /// <summary>
    /// \brief 현재 카메라 도메인 상태를 즉시 다시 계산하여 상태 스토어에 반영합니다.
    /// </summary>
    /// <param name="lastEvent">갱신과 함께 기록할 마지막 이벤트 메시지입니다. null이면 기존 메시지를 유지합니다.</param>
    void Refresh(string? lastEvent = null);
}
