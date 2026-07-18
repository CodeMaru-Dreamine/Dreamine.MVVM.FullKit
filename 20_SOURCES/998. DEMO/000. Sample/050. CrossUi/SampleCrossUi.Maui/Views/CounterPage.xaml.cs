using System.Collections.Specialized;
using System.ComponentModel;
using SampleCrossUi.Shared.ViewModels;

namespace SampleCrossUi.Maui.Views;

/// <summary>
/// \if KO
/// <para>Counter Page 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates counter page functionality and related state.</para>
/// \endif
/// </summary>
public partial class CounterPage : ContentView
{
    /// <summary>
    /// \if KO
    /// <para>view Model 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the view model value.</para>
    /// \endif
    /// </summary>
    private readonly CounterViewModel _viewModel;

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="CounterPage"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="CounterPage"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="viewModel">
    /// \if KO
    /// <para>view Model에 사용할 <c>CounterViewModel</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CounterViewModel</c> value used for view model.</para>
    /// \endif
    /// </param>
    public CounterPage(CounterViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _viewModel.PropertyChanged += OnPropertyChanged;
        _viewModel.Logs.CollectionChanged += OnLogsChanged;
        Refresh();
    }

    /// <summary>
    /// \if KO
    /// <para>Increment Clicked 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the increment clicked event or state change.</para>
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
    private void OnIncrementClicked(object? sender, EventArgs e)
        => _viewModel.IncrementCommand.Execute(null);

    /// <summary>
    /// \if KO
    /// <para>Reset Clicked 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the reset clicked event or state change.</para>
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
    private void OnResetClicked(object? sender, EventArgs e)
        => _viewModel.ResetCommand.Execute(null);

    /// <summary>
    /// \if KO
    /// <para>Property Changed 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the property changed event or state change.</para>
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
    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        => MainThread.BeginInvokeOnMainThread(Refresh);

    /// <summary>
    /// \if KO
    /// <para>Logs Changed 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the logs changed event or state change.</para>
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
    private void OnLogsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => MainThread.BeginInvokeOnMainThread(RefreshLogs);

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
        CountLabel.Text = $"Count: {_viewModel.Count}";
        RefreshLogs();
    }

    /// <summary>
    /// \if KO
    /// <para>Refresh Logs 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the refresh logs operation.</para>
    /// \endif
    /// </summary>
    private void RefreshLogs()
    {
        LogList.ItemsSource = _viewModel.Logs
            .Select(l => $"[{l.CreatedAt:HH:mm:ss}] {l.Message}")
            .ToList();
    }
}
