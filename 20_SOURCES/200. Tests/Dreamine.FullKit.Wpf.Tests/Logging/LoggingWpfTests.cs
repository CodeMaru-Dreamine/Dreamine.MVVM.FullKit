using Dreamine.Logging.Interfaces;
using Dreamine.Logging.Models;
using Dreamine.Logging.Services;
using Dreamine.Logging.Wpf.Services;
using Dreamine.Logging.Wpf.ViewModels;
using System.Windows.Threading;

namespace Dreamine.FullKit.Wpf.Tests.Logging;

/// <summary>
/// \if KO
/// <para>Logging Wpf Tests 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates logging wpf tests functionality and related state.</para>
/// \endif
/// </summary>
public sealed class LoggingWpfTests
{
    /// <summary>
    /// \if KO
    /// <para>Log Panel View Model Seeds Existing Entries And Updates Selection Detail 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the log panel view model seeds existing entries and updates selection detail operation.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>Log Panel View Model Clear Clears Underlying Store Immediately 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the log panel view model clear clears underlying store immediately operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void LogPanelViewModel_ClearClearsUnderlyingStoreImmediately()
    {
        var store = new InMemoryLogStore();
        store.Add(Entry("message"));

        using var viewModel = new DreamineLogPanelViewModel(store, new WpfLogUiDispatcher());

        viewModel.Clear();

        Assert.Empty(store.GetEntries());
    }

    /// <summary>
    /// \if KO
    /// <para>Log Panel View Model Accepts Dispatcher Abstraction 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the log panel view model accepts dispatcher abstraction operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void LogPanelViewModel_AcceptsDispatcherAbstraction()
    {
        var store = new InMemoryLogStore();

        using var viewModel = new DreamineLogPanelViewModel(
            store,
            new TestLogUiDispatcher());

        Assert.Empty(viewModel.Entries);
    }

    /// <summary>
    /// \if KO
    /// <para>Entry 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the entry operation.</para>
    /// \endif
    /// </summary>
    /// <param name="message">
    /// \if KO
    /// <para>처리할 메시지입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The message to process.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Entry 작업에서 생성한 <c>DreamineLogEntry</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>DreamineLogEntry</c> result produced by the entry operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Test Log Ui Dispatcher 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates test log ui dispatcher functionality and related state.</para>
    /// \endif
    /// </summary>
    private sealed class TestLogUiDispatcher : ILogUiDispatcher
    {
        /// <summary>
        /// \if KO
        /// <para>Dispatcher 값을 가져옵니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets the dispatcher value.</para>
        /// \endif
        /// </summary>
        public Dispatcher Dispatcher { get; } = Dispatcher.CurrentDispatcher;

        /// <summary>
        /// \if KO
        /// <para>Invoke 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the invoke operation.</para>
        /// \endif
        /// </summary>
        /// <param name="action">
        /// \if KO
        /// <para>action에 사용할 <c>Action</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>Action</c> value used for action.</para>
        /// \endif
        /// </param>
        public void Invoke(Action action)
        {
            action();
        }

        /// <summary>
        /// \if KO
        /// <para>Begin Invoke 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the begin invoke operation.</para>
        /// \endif
        /// </summary>
        /// <param name="action">
        /// \if KO
        /// <para>action에 사용할 <c>Action</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>Action</c> value used for action.</para>
        /// \endif
        /// </param>
        public void BeginInvoke(Action action)
        {
            action();
        }
    }
}
