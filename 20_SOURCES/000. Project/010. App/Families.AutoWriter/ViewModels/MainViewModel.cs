using System.Collections.ObjectModel;
using Dreamine.MVVM.ViewModels;

namespace FamiliesAutoWriter.ViewModels;

/// <summary>
/// \if KO
/// <para>앱 수준 ViewModel — 탭 세션 목록과 현재 활성 세션만 관리</para>
/// \endif
/// \if EN
/// <para>Encapsulates main view model functionality and related state.</para>
/// \endif
/// </summary>
public sealed partial class MainViewModel : ViewModelBase
{
    /// <summary>
    /// \if KO
    /// <para>Sessions 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the sessions value.</para>
    /// \endif
    /// </summary>
    public ObservableCollection<WriterSession> Sessions { get; } = [];

    /// <summary>
    /// \if KO
    /// <para>active Session 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the active session value.</para>
    /// \endif
    /// </summary>
    private WriterSession? _activeSession;
    /// <summary>
    /// \if KO
    /// <para>Active Session 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the active session value.</para>
    /// \endif
    /// </summary>
    public WriterSession? ActiveSession
    {
        get => _activeSession;
        set => SetProperty(ref _activeSession, value);
    }
}
