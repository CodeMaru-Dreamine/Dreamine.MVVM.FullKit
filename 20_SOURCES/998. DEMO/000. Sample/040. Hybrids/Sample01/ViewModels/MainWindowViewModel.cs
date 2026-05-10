using Dreamine.Hybrid.Interfaces;
using Dreamine.Hybrid.Messaging;
using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;
using Sample01.States;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Sample01.ViewModels
{
    /// <summary>
    /// \brief Provides the ViewModel for MainWindow.
    /// </summary>
    public sealed partial class MainWindowViewModel : ViewModelBase, IDisposable
    {
        private readonly IHybridMessageBus _bus;
        private readonly IHybridStateStore<CounterState> _store;
        private readonly IDisposable? _dashboardSub;
        private readonly IDisposable? _counterSub;
        private bool _disposed;

        [DreamineProperty] private string _title = "🌇 저녁시간이 다가옵니다!";
        [DreamineProperty] private int _clickCount;
        [DreamineProperty] private string _statusMessage = "대기 중...";
        [DreamineProperty] private ObservableCollection<string> _logs = new();

        /// <summary>
        /// \brief Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
        /// </summary>
        /// <param name="bus">The hybrid message bus.</param>
        /// <param name="store">The shared counter state store.</param>
        public MainWindowViewModel(
            IHybridMessageBus bus,
            IHybridStateStore<CounterState> store)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _store = store ?? throw new ArgumentNullException(nameof(store));

            ApplyState(_store.State);

            _dashboardSub = _bus.Subscribe<DashboardActionRequestedMessage>(OnDashboardActionRequestedAsync);
            _counterSub = _bus.Subscribe<CounterChangedMessage>(OnCounterChangedAsync);
        }

        /// <summary>
        /// \brief Executes the click command.
        /// </summary>
        [DreamineCommand("OnClick")]
        private partial void Click();

        /// <summary>
        /// \brief Executes the reset command.
        /// </summary>
        [DreamineCommand("OnReset")]
        private partial void Reset();

        /// <summary>
        /// \brief Handles dashboard action request messages.
        /// </summary>
        /// <param name="message">The dashboard action request message.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private Task OnDashboardActionRequestedAsync(
            DashboardActionRequestedMessage message,
            CancellationToken cancellationToken)
        {
            if (_disposed || cancellationToken.IsCancellationRequested)
            {
                return Task.CompletedTask;
            }

            return RunOnUiAsync(() =>
            {
                Logs.Add($"[{DateTime.Now:HH:mm:ss}] Dashboard Action: {message.Action}");
            });
        }

        /// <summary>
        /// \brief Handles counter changed messages.
        /// </summary>
        /// <param name="message">The counter changed message.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private Task OnCounterChangedAsync(
            CounterChangedMessage message,
            CancellationToken cancellationToken)
        {
            if (_disposed || cancellationToken.IsCancellationRequested)
            {
                return Task.CompletedTask;
            }

            return RunOnUiAsync(() =>
            {
                ClickCount = message.Count;
                StatusMessage = $"현재 클릭 수: {ClickCount}";
                Logs.Add($"[{DateTime.Now:HH:mm:ss}] (Blazor) 카운트 변경: {ClickCount}");

                _store.SetState(new CounterState(
                    Count: ClickCount,
                    LastSource: "Blazor → WPF → StateStore",
                    LastUpdated: DateTime.Now));
            });
        }

        /// <summary>
        /// \brief Handles the WPF click action.
        /// </summary>
        private void OnClick()
        {
            ThrowIfDisposed();

            ClickCount++;
            StatusMessage = $"현재 클릭 수: {ClickCount}";
            Logs.Add($"[{DateTime.Now:HH:mm:ss}] (WPF) 버튼 클릭 {ClickCount}회");

            _store.SetState(new CounterState(
                Count: ClickCount,
                LastSource: "WPF → StateStore",
                LastUpdated: DateTime.Now));

            _ = PublishCounterChangedAsync(ClickCount);
        }

        /// <summary>
        /// \brief Handles the WPF reset action.
        /// </summary>
        private void OnReset()
        {
            ThrowIfDisposed();

            ClickCount = 0;
            StatusMessage = "초기화 완료";
            Logs.Clear();

            _store.SetState(new CounterState(
                Count: 0,
                LastSource: "WPF → StateStore",
                LastUpdated: DateTime.Now));

            _ = PublishCounterChangedAsync(ClickCount);
        }

        /// <summary>
        /// \brief Publishes the current counter value.
        /// </summary>
        /// <param name="count">The counter value.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private Task PublishCounterChangedAsync(int count)
        {
            return _bus.PublishAsync(new CounterChangedMessage(count));
        }

        /// <summary>
        /// \brief Applies the shared counter state to the WPF ViewModel.
        /// </summary>
        /// <param name="state">The shared counter state.</param>
        private void ApplyState(CounterState state)
        {
            ClickCount = state.Count;
            StatusMessage = $"현재 클릭 수: {ClickCount}";
        }

        /// <summary>
        /// \brief Runs the specified action on the WPF UI dispatcher.
        /// </summary>
        /// <param name="action">The action to run.</param>
        /// <returns>A task that represents the dispatcher operation.</returns>
        private static Task RunOnUiAsync(Action action)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            Application? application = Application.Current;

            if (application?.Dispatcher is null)
            {
                action();
                return Task.CompletedTask;
            }

            if (application.Dispatcher.CheckAccess())
            {
                action();
                return Task.CompletedTask;
            }

            return application.Dispatcher.InvokeAsync(action).Task;
        }

        /// <summary>
        /// \brief Releases subscription resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _counterSub?.Dispose();
            _dashboardSub?.Dispose();

            _disposed = true;
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// \brief Throws when the instance has already been disposed.
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(MainWindowViewModel));
            }
        }
    }
}