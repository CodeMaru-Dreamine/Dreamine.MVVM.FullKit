using SampleCrossUi.Maui.Views;

namespace SampleCrossUi.Maui;

/// <summary>
/// \if KO
/// <para>Main Page 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates main page functionality and related state.</para>
/// \endif
/// </summary>
public partial class MainPage : ContentPage
{
    /// <summary>
    /// \if KO
    /// <para>counter Page 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the counter page value.</para>
    /// \endif
    /// </summary>
    private readonly CounterPage _counterPage;
    /// <summary>
    /// \if KO
    /// <para>light Bulb Page 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the light bulb page value.</para>
    /// \endif
    /// </summary>
    private readonly LightBulbPage _lightBulbPage;
    /// <summary>
    /// \if KO
    /// <para>controls Page 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the controls page value.</para>
    /// \endif
    /// </summary>
    private readonly ControlsPage _controlsPage;
    /// <summary>
    /// \if KO
    /// <para>popup Page 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the popup page value.</para>
    /// \endif
    /// </summary>
    private readonly PopupPage _popupPage;

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="MainPage"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="MainPage"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="counterPage">
    /// \if KO
    /// <para>counter Page에 사용할 <c>CounterPage</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CounterPage</c> value used for counter page.</para>
    /// \endif
    /// </param>
    /// <param name="lightBulbPage">
    /// \if KO
    /// <para>light Bulb Page에 사용할 <c>LightBulbPage</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>LightBulbPage</c> value used for light bulb page.</para>
    /// \endif
    /// </param>
    /// <param name="controlsPage">
    /// \if KO
    /// <para>controls Page에 사용할 <c>ControlsPage</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ControlsPage</c> value used for controls page.</para>
    /// \endif
    /// </param>
    /// <param name="popupPage">
    /// \if KO
    /// <para>popup Page에 사용할 <c>PopupPage</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>PopupPage</c> value used for popup page.</para>
    /// \endif
    /// </param>
    public MainPage(CounterPage counterPage, LightBulbPage lightBulbPage, ControlsPage controlsPage, PopupPage popupPage)
    {
        InitializeComponent();
        _counterPage = counterPage;
        _lightBulbPage = lightBulbPage;
        _controlsPage = controlsPage;
        _popupPage = popupPage;

        Navigate(_counterPage);
    }

    /// <summary>
    /// \if KO
    /// <para>Nav Counter Clicked 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the nav counter clicked event or state change.</para>
    /// \endif
    /// </summary>
    /// <param name="sender">
    /// \if KO
    /// <para>이벤트를 발생시킨 객체입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The object that raised the event.</para>
    /// \endif
    /// </param>
    /// <param name="e">
    /// \if KO
    /// <para>이벤트와 관련된 데이터를 포함합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Contains data associated with the event.</para>
    /// \endif
    /// </param>
    private void OnNavCounterClicked(object? sender, EventArgs e) => Navigate(_counterPage);

    /// <summary>
    /// \if KO
    /// <para>Nav Light Bulb Clicked 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the nav light bulb clicked event or state change.</para>
    /// \endif
    /// </summary>
    /// <param name="sender">
    /// \if KO
    /// <para>이벤트를 발생시킨 객체입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The object that raised the event.</para>
    /// \endif
    /// </param>
    /// <param name="e">
    /// \if KO
    /// <para>이벤트와 관련된 데이터를 포함합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Contains data associated with the event.</para>
    /// \endif
    /// </param>
    private void OnNavLightBulbClicked(object? sender, EventArgs e) => Navigate(_lightBulbPage);

    /// <summary>
    /// \if KO
    /// <para>Nav Controls Clicked 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the nav controls clicked event or state change.</para>
    /// \endif
    /// </summary>
    /// <param name="sender">
    /// \if KO
    /// <para>이벤트를 발생시킨 객체입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The object that raised the event.</para>
    /// \endif
    /// </param>
    /// <param name="e">
    /// \if KO
    /// <para>이벤트와 관련된 데이터를 포함합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Contains data associated with the event.</para>
    /// \endif
    /// </param>
    private void OnNavControlsClicked(object? sender, EventArgs e) => Navigate(_controlsPage);

    /// <summary>
    /// \if KO
    /// <para>Nav Popup Clicked 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the nav popup clicked event or state change.</para>
    /// \endif
    /// </summary>
    /// <param name="sender">
    /// \if KO
    /// <para>이벤트를 발생시킨 객체입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The object that raised the event.</para>
    /// \endif
    /// </param>
    /// <param name="e">
    /// \if KO
    /// <para>이벤트와 관련된 데이터를 포함합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Contains data associated with the event.</para>
    /// \endif
    /// </param>
    private void OnNavPopupClicked(object? sender, EventArgs e) => Navigate(_popupPage);

    /// <summary>
    /// \if KO
    /// <para>Navigate 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the navigate operation.</para>
    /// \endif
    /// </summary>
    /// <param name="page">
    /// \if KO
    /// <para>page에 사용할 <c>View</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>View</c> value used for page.</para>
    /// \endif
    /// </param>
    private void Navigate(View page) => PageHost.Content = page;
}
