namespace DreamineVMS.Services.Dashboard;

/// <summary>
/// \if KO
/// <para>\brief VmsDashboardState를 도메인 이벤트 기반으로 자동 갱신하는 서비스입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates i vms dashboard state service functionality and related state.</para>
/// \endif
/// </summary>
/// <remarks>
/// \if KO
/// <para>MainWindowViewModel과 같은 UI 계층이 아니라, 카메라 도메인(Repository / RuntimeState)을 직접 구독하여 IHybridStateStore&lt;VmsDashboardState&gt;를 갱신합니다. 이렇게 함으로써 WPF UI가 떠 있지 않아도 Server Dashboard(6080)와 Embedded Dashboard에 정확한 카메라 카운트와 상태가 노출됩니다.</para>
/// \endif
/// \if EN
/// <para>Describes behavior and usage considerations for this member.</para>
/// \endif
/// </remarks>
public interface IVmsDashboardStateService
{
    /// <summary>
    /// \if KO
    /// <para>\brief 서비스를 시작하고 도메인 이벤트 구독을 활성화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the start operation.</para>
    /// \endif
    /// </summary>
    /// <remarks>
    /// \if KO
    /// <para>동일 인스턴스에 대해 두 번 이상 호출해도 안전하도록 구현해야 합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Describes behavior and usage considerations for this member.</para>
    /// \endif
    /// </remarks>
    void Start();

    /// <summary>
    /// \if KO
    /// <para>\brief 현재 카메라 도메인 상태를 즉시 다시 계산하여 상태 스토어에 반영합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the refresh operation.</para>
    /// \endif
    /// </summary>
    /// <param name="lastEvent">
    /// \if KO
    /// <para>갱신과 함께 기록할 마지막 이벤트 메시지입니다. null이면 기존 메시지를 유지합니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for last event.</para>
    /// \endif
    /// </param>
    void Refresh(string? lastEvent = null);
}
