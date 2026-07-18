using System.ComponentModel;
using System.Threading;
using Dreamine.MVVM.ViewModels;

namespace Dreamine.FullKit.Tests.ViewModels;

/// <summary>
/// \if KO
/// <para>View Model And Relay Command Tests 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates view model and relay command tests functionality and related state.</para>
/// \endif
/// </summary>
public sealed class ViewModelAndRelayCommandTests
{
    /// <summary>
    /// \if KO
    /// <para>Relay Command Executes And Raises Can Execute Changed 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the relay command executes and raises can execute changed operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void RelayCommand_ExecutesAndRaisesCanExecuteChanged()
    {
        var executed = false;
        var raised = false;
        var command = new RelayCommand(() => executed = true, () => true);
        command.CanExecuteChanged += (_, _) => raised = true;

        command.Execute(null);
        command.RaiseCanExecuteChanged();

        Assert.True(executed);
        Assert.True(command.CanExecute(null));
        Assert.True(raised);
    }

    /// <summary>
    /// \if KO
    /// <para>Generic Relay Command Rejects Invalid Parameter 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the generic relay command rejects invalid parameter operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void GenericRelayCommand_RejectsInvalidParameter()
    {
        var command = new RelayCommand<int>(_ => { }, value => value > 0);

        // CanExecute returns false for a bad parameter type — Execute silently no-ops.
        Assert.False(command.CanExecute("bad"));
        var executed = false;
        command = new RelayCommand<int>(_ => executed = true, value => value > 0);
        command.Execute("bad");
        Assert.False(executed);
    }

    /// <summary>
    /// \if KO
    /// <para>View Model Base Set Property Raises Only When Value Changes 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the view model base set property raises only when value changes operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void ViewModelBase_SetPropertyRaisesOnlyWhenValueChanges()
    {
        var viewModel = new TestViewModel();
        var propertyNames = new List<string?>();
        viewModel.PropertyChanged += (_, args) => propertyNames.Add(args.PropertyName);

        viewModel.Name = "one";
        viewModel.Name = "one";
        viewModel.Name = "two";

        Assert.Equal(new[] { "Name", "Name" }, propertyNames);
    }

    /// <summary>
    /// \if KO
    /// <para>Async Relay Command Prevents Concurrent Execution 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the async relay command prevents concurrent execution operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Async Relay Command Prevents Concurrent Execution 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the async relay command prevents concurrent execution operation.</para>
    /// \endif
    /// </returns>
    [Fact]
    public async Task AsyncRelayCommand_PreventsConcurrentExecution()
    {
        var concurrentCount = 0;
        var maxConcurrent = 0;
        var command = new AsyncRelayCommand(async () =>
        {
            var c = Interlocked.Increment(ref concurrentCount);
            Interlocked.Exchange(ref maxConcurrent, Math.Max(Volatile.Read(ref maxConcurrent), c));
            await Task.Delay(30);
            Interlocked.Decrement(ref concurrentCount);
        });

        var tasks = Enumerable.Range(0, 10).Select(_ => Task.Run(() => command.Execute(null)));
        await Task.WhenAll(tasks);
        await Task.Delay(100); // let the one accepted execution finish

        Assert.Equal(1, maxConcurrent);
    }

    /// <summary>
    /// \if KO
    /// <para>Async Relay Command Last Exception Visible After Failure 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the async relay command last exception visible after failure operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void AsyncRelayCommand_LastExceptionVisibleAfterFailure()
    {
        var ex = new InvalidOperationException("test");
        var command = new AsyncRelayCommand(() => Task.FromException(ex));

        command.Execute(null);
        Thread.Sleep(50);

        Assert.Same(ex, command.LastException);
    }

    /// <summary>
    /// \if KO
    /// <para>Test View Model 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates test view model functionality and related state.</para>
    /// \endif
    /// </summary>
    private sealed class TestViewModel : ViewModelBase
    {
        /// <summary>
        /// \if KO
        /// <para>name 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the name value.</para>
        /// \endif
        /// </summary>
        private string _name = string.Empty;

        /// <summary>
        /// \if KO
        /// <para>Name 값을 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets the name value.</para>
        /// \endif
        /// </summary>
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }
    }
}
