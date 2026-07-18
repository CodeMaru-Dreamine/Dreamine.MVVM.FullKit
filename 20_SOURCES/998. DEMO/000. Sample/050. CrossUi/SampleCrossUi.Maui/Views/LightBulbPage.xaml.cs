using System.ComponentModel;
using SampleCrossUi.Shared.ViewModels;

namespace SampleCrossUi.Maui.Views;

/// <summary>
/// \if KO
/// <para>Light Bulb Page 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates light bulb page functionality and related state.</para>
/// \endif
/// </summary>
public partial class LightBulbPage : ContentView
{
    /// <summary>
    /// \if KO
    /// <para>view Model 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the view model value.</para>
    /// \endif
    /// </summary>
    private readonly LightBulbViewModel _viewModel;
    /// <summary>
    /// \if KO
    /// <para>refreshing 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the refreshing value.</para>
    /// \endif
    /// </summary>
    private bool _refreshing;

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="LightBulbPage"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="LightBulbPage"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="viewModel">
    /// \if KO
    /// <para>view Model에 사용할 <c>LightBulbViewModel</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>LightBulbViewModel</c> value used for view model.</para>
    /// \endif
    /// </param>
    public LightBulbPage(LightBulbViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        Refresh();
    }

    /// <summary>
    /// \if KO
    /// <para>Toggle Clicked 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the toggle clicked event or state change.</para>
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
    private void OnToggleClicked(object? sender, EventArgs e)
    {
        _viewModel.ToggleCommand.Execute(null);
    }

    /// <summary>
    /// \if KO
    /// <para>Power Changed 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the power changed event or state change.</para>
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
    private void OnPowerChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (_refreshing || _viewModel.IsOn == e.Value) return;
        _viewModel.ToggleCommand.Execute(null);
    }

    /// <summary>
    /// \if KO
    /// <para>View Model Property Changed 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the view model property changed event or state change.</para>
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
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(Refresh);
    }

    /// <summary>
    /// \if KO
    /// <para>Refresh 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the refresh operation.</para>
    /// \endif
    /// </summary>
    private void Refresh()
    {
        _refreshing = true;
        PowerCheckBox.IsChecked = _viewModel.IsOn;
        Bulb.IsOn = _viewModel.IsOn;
        StatusLabel.Text = _viewModel.StatusText;
        CountLabel.Text = $"Toggled {_viewModel.ToggleCount}x";
        StatusLabel.TextColor = _viewModel.IsOn ? Color.FromArgb("#B7791F") : Color.FromArgb("#64748B");
        _refreshing = false;
    }
}
