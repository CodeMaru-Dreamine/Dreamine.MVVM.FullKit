using Dreamine.MVVM.Attributes;
using System.Windows;

namespace SampleSmart
{    
    /// <summary>
    /// \if KO
    /// <para>SampleSmart WPF 애플리케이션 진입점입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates app functionality and related state.</para>
    /// \endif
    /// </summary>
    /// <remarks>
    /// \if KO
    /// <para><see cref="DreamineEntryAttribute"/>가 적용된 App 클래스는 Dreamine Source Generator에 의해 자동 부트스트랩 코드가 생성됩니다. 기본 설정에서는 별도의 초기화 코드가 필요하지 않습니다. Dreamine은 DI 컨테이너 초기화, ViewModel 자동 등록, View/ViewModel 매핑, ViewModel 자동 연결, ViewManager, WindowStateService, Region Navigator 등록을 자동으로 수행합니다. 시작 과정에서 사용자 정의 처리가 필요하면 partial hook을 선택적으로 구현할 수 있습니다.</para>
    /// \endif
    /// \if EN
    /// <para>Describes behavior and usage considerations for this member.</para>
    /// \endif
    /// </remarks>
    [DreamineEntry]
    public partial class App : Application
    {
    }
}