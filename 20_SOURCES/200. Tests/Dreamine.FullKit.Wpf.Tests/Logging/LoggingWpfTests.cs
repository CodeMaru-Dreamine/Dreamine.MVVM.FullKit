using Dreamine.Logging.Interfaces;
using Dreamine.Logging.Models;
using Dreamine.Logging.Services;
using Dreamine.Logging.Wpf.Services;
using Dreamine.Logging.Wpf.ViewModels;
using System.Windows.Threading;

namespace Dreamine.FullKit.Wpf.Tests.Logging;

public sealed class LoggingWpfTests
{
    [Fact]
    public void LogPanelViewModel_SeedsExistingEntriesAndUpdatesSelectionDetail()
    {
        var store = new InMemoryLogStore();
        var first = Entry("first");
        var second = Entry("second");
        store.Add(first);
        store.Add(second);

        using var viewModel = new DreamineLogPanelViewModel(
            store,
            new WpfLogUiDispatcher(),
            displayCapacity: 1);

        viewModel.SelectedEntry = second;

        Assert.Single(viewModel.Entries);
        Assert.Same(second, viewModel.Entries[0]);
        Assert.Contains("second", viewModel.SelectedDetailText);
    }

    [Fact]
    public void LogPanelViewModel_ClearClearsUnderlyingStoreImmediately()
    {
        var store = new InMemoryLogStore();
        store.Add(Entry("message"));

        using var viewModel = new DreamineLogPanelViewModel(store, new WpfLogUiDispatcher());

        viewModel.Clear();

        Assert.Empty(store.GetEntries());
    }

    [Fact]
    public void LogPanelViewModel_AcceptsDispatcherAbstraction()
    {
        var store = new InMemoryLogStore();

        using var viewModel = new DreamineLogPanelViewModel(
            store,
            new TestLogUiDispatcher());

        Assert.Empty(viewModel.Entries);
    }

    private static DreamineLogEntry Entry(string message)
    {
        return new DreamineLogEntry(
            DateTimeOffset.UnixEpoch,
            DreamineLogLevel.Info,
            "Test",
            message,
            exception: null,
            threadId: 1);
    }

    private sealed class TestLogUiDispatcher : ILogUiDispatcher
    {
        public Dispatcher Dispatcher { get; } = Dispatcher.CurrentDispatcher;

        public void Invoke(Action action)
        {
            action();
        }

        public void BeginInvoke(Action action)
        {
            action();
        }
    }
}
