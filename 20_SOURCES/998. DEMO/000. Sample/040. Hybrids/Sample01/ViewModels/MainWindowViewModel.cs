using Dreamine.Hybrid.Interfaces;
using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;
using Sample01.Messages;
using Sample01.States;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Sample01.ViewModels
{
    /// <summary>
    /// \if KO
    /// <para>Main Window View Model 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>\brief Provides the ViewModel for MainWindow.</para>
    /// \endif
    /// </summary>
    public sealed partial class MainWindowViewModel : ViewModelBase, IDisposable
    {
        /// <summary>
        /// \if KO
        /// <para>bus 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the bus value.</para>
        /// \endif
        /// </summary>
        private readonly IHybridMessageBus _bus;
        /// <summary>
        /// \if KO
        /// <para>store 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the store value.</para>
        /// \endif
        /// </summary>
        private readonly IHybridStateStore<CounterState> _store;
        /// <summary>
        /// \if KO
        /// <para>dashboard Sub 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the dashboard sub value.</para>
        /// \endif
        /// </summary>
        private readonly IDisposable? _dashboardSub;
        /// <summary>
        /// \if KO
        /// <para>counter Sub 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the counter sub value.</para>
        /// \endif
        /// </summary>
        private readonly IDisposable? _counterSub;
        /// <summary>
        /// \if KO
        /// <para>disposed 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the disposed value.</para>
        /// \endif
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// \if KO
        /// <para>title 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the title value.</para>
        /// \endif
        /// </summary>
        [DreamineProperty] private string _title = "🌇 저녁시간이 다가옵니다!";
        /// <summary>
        /// \if KO
        /// <para>click Count 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the click count value.</para>
        /// \endif
        /// </summary>
        [DreamineProperty] private int _clickCount;
        /// <summary>
        /// \if KO
        /// <para>status Message 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the status message value.</para>
        /// \endif
        /// </summary>
        [DreamineProperty] private string _statusMessage = "대기 중...";
        /// <summary>
        /// \if KO
        /// <para>logs 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the logs value.</para>
        /// \endif
        /// </summary>
        [DreamineProperty] private ObservableCollection<string> _logs = new();

        /// <summary>
        /// \if KO
        /// <para>지정한 설정으로 <see cref="MainWindowViewModel"/> 클래스의 새 인스턴스를 초기화합니다.</para>
        /// \endif
        /// \if EN
        /// <para>\brief Initializes a new instance of the <see cref="MainWindowViewModel"/> class.</para>
        /// \endif
        /// </summary>
        /// <param name="bus">
        /// \if KO
        /// <para>bus에 사용할 <c>IHybridMessageBus</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The hybrid message bus.</para>
        /// \endif
        /// </param>
        /// <param name="store">
        /// \if KO
        /// <para>store에 사용할 <c>IHybridStateStore&lt;CounterState&gt;</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The shared counter state store.</para>
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
        /// \if KO
        /// <para>Click 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>\brief Executes the click command.</para>
        /// \endif
        /// </summary>
        [DreamineCommand("OnClick")]
        private partial void Click();

        /// <summary>
        /// \if KO
        /// <para>Reset 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>\brief Executes the reset command.</para>
        /// \endif
        /// </summary>
        [DreamineCommand("OnReset")]
        private partial void Reset();

        /// <summary>
        /// \if KO
        /// <para>Dashboard Action Requested Async 이벤트 또는 상태 변경을 처리합니다.</para>
        /// \endif
        /// \if EN
        /// <para>\brief Handles dashboard action request messages.</para>
        /// \endif
        /// </summary>
        /// <param name="message">
        /// \if KO
        /// <para>처리할 메시지입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The dashboard action request message.</para>
        /// \endif
        /// </param>
        /// <param name="cancellationToken">
        /// \if KO
        /// <para>취소 요청을 감시하는 토큰입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The cancellation token.</para>
        /// \endif
        /// </param>
        /// <returns>
        /// \if KO
        /// <para>On Dashboard Action Requested Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
        /// \endif
        /// \if EN
        /// <para>A task that represents the asynchronous operation.</para>
        /// \endif
        /// </returns>
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
        /// \if KO
        /// <para>Counter Changed Async 이벤트 또는 상태 변경을 처리합니다.</para>
        /// \endif
        /// \if EN
        /// <para>\brief Handles counter changed messages.</para>
        /// \endif
        /// </summary>
        /// <param name="message">
        /// \if KO
        /// <para>처리할 메시지입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The counter changed message.</para>
        /// \endif
        /// </param>
        /// <param name="cancellationToken">
        /// \if KO
        /// <para>취소 요청을 감시하는 토큰입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The cancellation token.</para>
        /// \endif
        /// </param>
        /// <returns>
        /// \if KO
        /// <para>On Counter Changed Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
        /// \endif
        /// \if EN
        /// <para>A task that represents the asynchronous operation.</para>
        /// \endif
        /// </returns>
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
        /// \if KO
        /// <para>Click 이벤트 또는 상태 변경을 처리합니다.</para>
        /// \endif
        /// \if EN
        /// <para>\brief Handles the WPF click action.</para>
        /// \endif
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
        /// \if KO
        /// <para>Reset 이벤트 또는 상태 변경을 처리합니다.</para>
        /// \endif
        /// \if EN
        /// <para>\brief Handles the WPF reset action.</para>
        /// \endif
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
        /// \if KO
        /// <para>Publish Counter Changed Async 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>\brief Publishes the current counter value.</para>
        /// \endif
        /// </summary>
        /// <param name="count">
        /// \if KO
        /// <para>count에 사용할 <c>int</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The counter value.</para>
        /// \endif
        /// </param>
        /// <returns>
        /// \if KO
        /// <para>Publish Counter Changed Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
        /// \endif
        /// \if EN
        /// <para>A task that represents the asynchronous operation.</para>
        /// \endif
        /// </returns>
        private Task PublishCounterChangedAsync(int count)
        {
            return _bus.PublishAsync(new CounterChangedMessage(count));
        }

        /// <summary>
        /// \if KO
        /// <para>Apply State 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>\brief Applies the shared counter state to the WPF ViewModel.</para>
        /// \endif
        /// </summary>
        /// <param name="state">
        /// \if KO
        /// <para>state에 사용할 <c>CounterState</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The shared counter state.</para>
        /// \endif
        /// </param>
        private void ApplyState(CounterState state)
        {
            ClickCount = state.Count;
            StatusMessage = $"현재 클릭 수: {ClickCount}";
        }

        /// <summary>
        /// \if KO
        /// <para>Run On Ui Async 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>\brief Runs the specified action on the WPF UI dispatcher.</para>
        /// \endif
        /// </summary>
        /// <param name="action">
        /// \if KO
        /// <para>action에 사용할 <c>Action</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The action to run.</para>
        /// \endif
        /// </param>
        /// <returns>
        /// \if KO
        /// <para>Run On Ui Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
        /// \endif
        /// \if EN
        /// <para>A task that represents the dispatcher operation.</para>
        /// \endif
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// \if KO
        /// <para>필수 입력 인자 중 하나가 <see langword="null"/>인 경우 발생합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Thrown when a required input argument is <see langword="null"/>.</para>
        /// \endif
        /// </exception>
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
        /// \if KO
        /// <para>이 인스턴스가 소유한 리소스를 해제합니다.</para>
        /// \endif
        /// \if EN
        /// <para>\brief Releases subscription resources.</para>
        /// \endif
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
        /// \if KO
        /// <para>Throw If Disposed 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>\brief Throws when the instance has already been disposed.</para>
        /// \endif
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// \if KO
        /// <para>Throw If Disposed 작업을 완료할 수 없는 경우 <c>ObjectDisposedException</c>이 발생합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Thrown as <c>ObjectDisposedException</c> when the throw if disposed operation cannot be completed.</para>
        /// \endif
        /// </exception>
        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(MainWindowViewModel));
            }
        }
    }
}
