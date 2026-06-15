using System.Windows.Input;

namespace Codemaru.ViewModels;

/// <summary>
/// \brief 비동기 ICommand 구현입니다.
/// </summary>
public sealed class AsyncRelayCommand : ICommand
{
    private readonly Func<Task> _execute;
    private readonly Func<bool>? _canExecute;
    private bool _isRunning;

    /// <summary>
    /// \brief AsyncRelayCommand 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="execute">실행할 비동기 작업입니다.</param>
    /// <param name="canExecute">실행 가능 여부를 반환하는 조건입니다.</param>
    public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged;

    /// <inheritdoc />
    public bool CanExecute(object? parameter) => !_isRunning && (_canExecute?.Invoke() ?? true);

    /// <inheritdoc />
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
    /// \brief CanExecuteChanged 이벤트를 UI Dispatcher에서 발생시킵니다.
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
