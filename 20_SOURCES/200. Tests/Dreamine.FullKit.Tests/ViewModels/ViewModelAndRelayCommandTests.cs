using System.ComponentModel;
using System.Threading;
using Dreamine.MVVM.ViewModels;

namespace Dreamine.FullKit.Tests.ViewModels;

public sealed class ViewModelAndRelayCommandTests
{
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

    [Fact]
    public void AsyncRelayCommand_LastExceptionVisibleAfterFailure()
    {
        var ex = new InvalidOperationException("test");
        var command = new AsyncRelayCommand(() => Task.FromException(ex));

        command.Execute(null);
        Thread.Sleep(50);

        Assert.Same(ex, command.LastException);
    }

    private sealed class TestViewModel : ViewModelBase
    {
        private string _name = string.Empty;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }
    }
}
