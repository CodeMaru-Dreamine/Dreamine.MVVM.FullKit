using Dreamine.Hybrid.Messaging;
using Dreamine.MVVM.Attributes;
using System;
using System.Collections.ObjectModel;

namespace Sample01.ViewModels
{
    /// <summary>MainWindow용 ViewModel 입니다.</summary>
    public sealed partial class MainWindowViewModel : IDisposable
    {
        private readonly IHybridMessageBus _bus;
        private readonly IDisposable? _dashboardSub;
        private readonly IDisposable? _counterSub;
        private bool _disposed;

        [DreamineProperty] private string _title = "🌇 저녁시간이 다가옵니다!";
        [DreamineProperty] private int _clickCount;
        [DreamineProperty] private string _statusMessage = "대기 중...";
        [DreamineProperty] private ObservableCollection<string> _logs = new();

        public MainWindowViewModel(IHybridMessageBus bus)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));

            _dashboardSub = _bus.Subscribe<DashboardActionRequestedMessage>(m =>
            {
                Logs.Add($"[{DateTime.Now:HH:mm:ss}] Dashboard Action: {m.Action}");
            });

            _counterSub = _bus.Subscribe<CounterChangedMessage>(m =>
            {
                ClickCount = m.Count;
                StatusMessage = $"현재 클릭 수: {ClickCount}";
                Logs.Add($"[{DateTime.Now:HH:mm:ss}] (Blazor) 카운트 변경: {ClickCount}");
            });
        }

        [DreamineCommand("OnClick")]
        private partial void Click();

        [DreamineCommand("OnReset")]
        private partial void Reset();

        private void OnClick()
        {
            ThrowIfDisposed();

            ClickCount++;
            StatusMessage = $"현재 클릭 수: {ClickCount}";
            Logs.Add($"[{DateTime.Now:HH:mm:ss}] (WPF) 버튼 클릭 {ClickCount}회");
            _bus.Publish(new CounterChangedMessage(ClickCount));
        }

        private void OnReset()
        {
            ThrowIfDisposed();

            ClickCount = 0;
            StatusMessage = "초기화 완료";
            Logs.Clear();
            _bus.Publish(new CounterChangedMessage(ClickCount));
        }

        /// <summary>구독 리소스를 해제합니다.</summary>
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

        /// <summary>해제된 객체 접근을 방지합니다.</summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(MainWindowViewModel));
            }
        }
    }
}