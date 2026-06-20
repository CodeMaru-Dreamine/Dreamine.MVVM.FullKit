using System.Collections.ObjectModel;
using Dreamine.MVVM.ViewModels;

namespace FamiliesAutoWriter.ViewModels;

/// <summary>앱 수준 ViewModel — 탭 세션 목록과 현재 활성 세션만 관리</summary>
public sealed partial class MainViewModel : ViewModelBase
{
    public ObservableCollection<WriterSession> Sessions { get; } = [];

    private WriterSession? _activeSession;
    public WriterSession? ActiveSession
    {
        get => _activeSession;
        set => SetProperty(ref _activeSession, value);
    }
}
