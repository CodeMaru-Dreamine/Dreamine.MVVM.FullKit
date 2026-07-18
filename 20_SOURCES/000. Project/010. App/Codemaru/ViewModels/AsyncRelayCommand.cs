using System.Windows.Input;

namespace Codemaru.ViewModels;

/// <summary>
/// \if KO
/// <para>\brief 비동기 ICommand 구현입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates async relay command functionality and related state.</para>
/// \endif
/// </summary>
public sealed class AsyncRelayCommand : ICommand
{
    /// <summary>
    /// \if KO
    /// <para>execute 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the execute value.</para>
    /// \endif
    /// </summary>
    private readonly Func<Task> _execute;
    /// <summary>
    /// \if KO
    /// <para>can Execute 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the can execute value.</para>
    /// \endif
    /// </summary>
    private readonly Func<bool>? _canExecute;
    /// <summary>
    /// \if KO
    /// <para>is Running 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the is running value.</para>
    /// \endif
    /// </summary>
    private bool _isRunning;

    /// <summary>
    /// \if KO
    /// <para>\brief AsyncRelayCommand 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="AsyncRelayCommand"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="execute">
    /// \if KO
    /// <para>실행할 비동기 작업입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Func&lt;Task&gt;</c> value used for execute.</para>
    /// \endif
    /// </param>
    /// <param name="canExecute">
    /// \if KO
    /// <para>실행 가능 여부를 반환하는 조건입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Func&lt;bool&gt;?</c> value used for can execute.</para>
    /// \endif
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// \if KO
    /// <para>필수 입력 인자 중 하나가 <see langword="null"/>인 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when a required input argument is <see langword="null"/>.</para>
    /// \endif
    /// </exception>
    public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <summary>
    /// \if KO
    /// <para>Can Execute Changed 상황이 발생할 때 알립니다.</para>
    /// \endif
    /// \if EN
    /// <para>Occurs when can execute changed takes place.</para>
    /// \endif
    /// </summary>
    public event EventHandler? CanExecuteChanged;

    /// <summary>
    /// \if KO
    /// <para>Can Execute 조건을 확인합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Determines whether can execute.</para>
    /// \endif
    /// </summary>
    /// <param name="parameter">
    /// \if KO
    /// <para>parameter에 사용할 <c>object?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>object?</c> value used for parameter.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Can Execute 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the can execute condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    public bool CanExecute(object? parameter) => !_isRunning && (_canExecute?.Invoke() ?? true);

    /// <summary>
    /// \if KO
    /// <para>Execute 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the execute operation.</para>
    /// \endif
    /// </summary>
    /// <param name="parameter">
    /// \if KO
    /// <para>parameter에 사용할 <c>object?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>object?</c> value used for parameter.</para>
    /// \endif
    /// </param>
    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        try
        {
            _isRunning = true;
            RaiseCanExecuteChanged();
            await _execute();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AsyncRelayCommand] {ex}");
        }
        finally
        {
            _isRunning = false;
            RaiseCanExecuteChanged();
        }
    }

    /// <summary>
    /// \if KO
    /// <para>\brief CanExecuteChanged 이벤트를 UI Dispatcher에서 발생시킵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the raise can execute changed operation.</para>
    /// \endif
    /// </summary>
    public void RaiseCanExecuteChanged()
    {
        System.Windows.Threading.Dispatcher? dispatcher =
            System.Windows.Application.Current?.Dispatcher;

        if (dispatcher is null || dispatcher.CheckAccess())
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        dispatcher.InvokeAsync(() => CanExecuteChanged?.Invoke(this, EventArgs.Empty));
    }
}
