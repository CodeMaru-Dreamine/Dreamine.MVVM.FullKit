using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Dreamine.Threading.Models;
using Dreamine.Threading.Interfaces;
using Dreamine.Threading.Windows.Services;
using Dreamine.Threading.Wpf.Converters;
using Dreamine.Threading.Wpf.Services;
using Dreamine.Threading.Wpf.ViewModels;
using System.Windows.Threading;

namespace Dreamine.FullKit.Wpf.Tests.Threading;

/// <summary>
/// \if KO
/// <para>Threading Wpf Tests 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates threading wpf tests functionality and related state.</para>
/// \endif
/// </summary>
public sealed class ThreadingWpfTests
{
    /// <summary>
    /// \if KO
    /// <para>Thread Converters Map Known Values 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the thread converters map known values operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void ThreadConverters_MapKnownValues()
    {
        var statusConverter = new ThreadStatusBrushConverter();
        var priorityConverter = new ThreadPriorityTextConverter();

        Assert.Same(Brushes.ForestGreen, statusConverter.Convert(DreamineThreadStatus.Running, typeof(Brush), null!, CultureInfo.InvariantCulture));
        Assert.Same(Brushes.Crimson, statusConverter.Convert(DreamineThreadStatus.Faulted, typeof(Brush), null!, CultureInfo.InvariantCulture));
        Assert.Same(Brushes.Gray, statusConverter.Convert("bad", typeof(Brush), null!, CultureInfo.InvariantCulture));
        Assert.Equal("High", priorityConverter.Convert(DreamineThreadPriority.High, typeof(string), null!, CultureInfo.InvariantCulture));
        Assert.Equal(string.Empty, priorityConverter.Convert("bad", typeof(string), null!, CultureInfo.InvariantCulture));
        Assert.Same(Binding.DoNothing, priorityConverter.ConvertBack("", typeof(DreamineThreadPriority), null!, CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// \if KO
    /// <para>Thread Info Row Updates Changed Fields And Derived Flags 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the thread info row updates changed fields and derived flags operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void ThreadInfoRow_UpdatesChangedFieldsAndDerivedFlags()
    {
        var row = new ThreadInfoRow(CreateInfo(DreamineThreadStatus.Created, cycles: 1));
        var changed = new List<string?>();
        row.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        var updated = row.UpdateFrom(CreateInfo(DreamineThreadStatus.Running, cycles: 2));

        Assert.True(updated);
        Assert.Equal(DreamineThreadStatus.Running, row.Status);
        Assert.True(row.IsRunning);
        Assert.Contains(nameof(ThreadInfoRow.Status), changed);
        Assert.Contains(nameof(ThreadInfoRow.CycleCount), changed);
        Assert.Contains(nameof(ThreadInfoRow.IsRunning), changed);
    }

    /// <summary>
    /// \if KO
    /// <para>Windows Cpu Info Provider Reports Valid Processor Range 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the windows cpu info provider reports valid processor range operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void WindowsCpuInfoProvider_ReportsValidProcessorRange()
    {
        ICpuInfoProvider provider = new WindowsCpuInfoProvider();

        Assert.True(provider.GetLogicalProcessorCount() >= 1);
        Assert.True(provider.IsValidCoreIndex(0));
        Assert.False(provider.IsValidCoreIndex(-1));
        Assert.False(provider.IsValidCoreIndex(provider.GetLogicalProcessorCount()));
    }

    /// <summary>
    /// \if KO
    /// <para>Thread Monitor View Model Accepts Dispatcher Abstraction 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the thread monitor view model accepts dispatcher abstraction operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void ThreadMonitorViewModel_AcceptsDispatcherAbstraction()
    {
        using var viewModel = new DreamineThreadMonitorViewModel(
            new TestThreadManager(),
            new TestThreadUiDispatcher(),
            TimeSpan.FromSeconds(1));

        Assert.Empty(viewModel.Threads);
    }

    /// <summary>
    /// \if KO
    /// <para>Info 값을 생성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Creates the info value.</para>
    /// \endif
    /// </summary>
    /// <param name="status">
    /// \if KO
    /// <para>status에 사용할 <c>DreamineThreadStatus</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>DreamineThreadStatus</c> value used for status.</para>
    /// \endif
    /// </param>
    /// <param name="cycles">
    /// \if KO
    /// <para>cycles에 사용할 <c>long</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>long</c> value used for cycles.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Create Info 작업에서 생성한 <c>DreamineThreadInfo</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>DreamineThreadInfo</c> result produced by the create info operation.</para>
    /// \endif
    /// </returns>
    private static DreamineThreadInfo CreateInfo(DreamineThreadStatus status, long cycles)
    {
        return new DreamineThreadInfo(
            "Worker",
            status,
            DreamineThreadPriority.Normal,
            intervalMs: 10,
            coreIndex: null,
            useAffinity: false,
            jobCount: 1,
            cycleCount: cycles,
            startedAt: null,
            stoppedAt: null,
            lastErrorMessage: null);
    }

    /// <summary>
    /// \if KO
    /// <para>Test Thread Ui Dispatcher 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates test thread ui dispatcher functionality and related state.</para>
    /// \endif
    /// </summary>
    private sealed class TestThreadUiDispatcher : IThreadUiDispatcher
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

    /// <summary>
    /// \if KO
    /// <para>Test Thread Manager 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates test thread manager functionality and related state.</para>
    /// \endif
    /// </summary>
    private sealed class TestThreadManager : IDreamineThreadManager
    {
        /// <summary>
        /// \if KO
        /// <para>Register 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the register operation.</para>
        /// \endif
        /// </summary>
        /// <param name="options">
        /// \if KO
        /// <para>동작을 구성하는 설정입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The options that configure the operation.</para>
        /// \endif
        /// </param>
        /// <param name="action">
        /// \if KO
        /// <para>action에 사용할 <c>Func&lt;CancellationToken, ValueTask&gt;</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>Func&lt;CancellationToken, ValueTask&gt;</c> value used for action.</para>
        /// \endif
        /// </param>
        /// <returns>
        /// \if KO
        /// <para>Register 작업에서 생성한 <c>IDreamineThreadJob</c> 결과입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>IDreamineThreadJob</c> result produced by the register operation.</para>
        /// \endif
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// \if KO
        /// <para>요청한 작업이 지원되지 않는 경우 발생합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Thrown when the requested operation is not supported.</para>
        /// \endif
        /// </exception>
        public IDreamineThreadJob Register(DreamineThreadOptions options, Func<CancellationToken, ValueTask> action)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// \if KO
        /// <para>Start 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the start operation.</para>
        /// \endif
        /// </summary>
        /// <param name="threadName">
        /// \if KO
        /// <para>thread Name에 사용할 <c>string</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>string</c> value used for thread name.</para>
        /// \endif
        /// </param>
        /// <returns>
        /// \if KO
        /// <para>Start 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
        /// \endif
        /// \if EN
        /// <para><see langword="true"/> when the start condition is satisfied; otherwise, <see langword="false"/>.</para>
        /// \endif
        /// </returns>
        public bool Start(string threadName) => true;

        /// <summary>
        /// \if KO
        /// <para>Stop 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the stop operation.</para>
        /// \endif
        /// </summary>
        /// <param name="threadName">
        /// \if KO
        /// <para>thread Name에 사용할 <c>string</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>string</c> value used for thread name.</para>
        /// \endif
        /// </param>
        /// <returns>
        /// \if KO
        /// <para>Stop 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
        /// \endif
        /// \if EN
        /// <para><see langword="true"/> when the stop condition is satisfied; otherwise, <see langword="false"/>.</para>
        /// \endif
        /// </returns>
        public bool Stop(string threadName) => true;

        /// <summary>
        /// \if KO
        /// <para>Stop Async 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the stop async operation.</para>
        /// \endif
        /// </summary>
        /// <param name="threadName">
        /// \if KO
        /// <para>thread Name에 사용할 <c>string</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>string</c> value used for thread name.</para>
        /// \endif
        /// </param>
        /// <returns>
        /// \if KO
        /// <para>Stop Async 작업에서 생성한 <c>ValueTask&lt;bool&gt;</c> 결과입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>ValueTask&lt;bool&gt;</c> result produced by the stop async operation.</para>
        /// \endif
        /// </returns>
        public ValueTask<bool> StopAsync(string threadName) => ValueTask.FromResult(true);

        /// <summary>
        /// \if KO
        /// <para>Pause 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the pause operation.</para>
        /// \endif
        /// </summary>
        /// <param name="threadName">
        /// \if KO
        /// <para>thread Name에 사용할 <c>string</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>string</c> value used for thread name.</para>
        /// \endif
        /// </param>
        /// <returns>
        /// \if KO
        /// <para>Pause 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
        /// \endif
        /// \if EN
        /// <para><see langword="true"/> when the pause condition is satisfied; otherwise, <see langword="false"/>.</para>
        /// \endif
        /// </returns>
        public bool Pause(string threadName) => true;

        /// <summary>
        /// \if KO
        /// <para>Resume 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the resume operation.</para>
        /// \endif
        /// </summary>
        /// <param name="threadName">
        /// \if KO
        /// <para>thread Name에 사용할 <c>string</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>string</c> value used for thread name.</para>
        /// \endif
        /// </param>
        /// <returns>
        /// \if KO
        /// <para>Resume 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
        /// \endif
        /// \if EN
        /// <para><see langword="true"/> when the resume condition is satisfied; otherwise, <see langword="false"/>.</para>
        /// \endif
        /// </returns>
        public bool Resume(string threadName) => true;

        /// <summary>
        /// \if KO
        /// <para>Start All 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the start all operation.</para>
        /// \endif
        /// </summary>
        public void StartAll()
        {
        }

        /// <summary>
        /// \if KO
        /// <para>Stop All 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the stop all operation.</para>
        /// \endif
        /// </summary>
        public void StopAll()
        {
        }

        /// <summary>
        /// \if KO
        /// <para>Stop All Async 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the stop all async operation.</para>
        /// \endif
        /// </summary>
        /// <returns>
        /// \if KO
        /// <para>Stop All Async 작업에서 생성한 <c>ValueTask</c> 결과입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>ValueTask</c> result produced by the stop all async operation.</para>
        /// \endif
        /// </returns>
        public ValueTask StopAllAsync() => ValueTask.CompletedTask;

        /// <summary>
        /// \if KO
        /// <para>Pause All 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the pause all operation.</para>
        /// \endif
        /// </summary>
        public void PauseAll()
        {
        }

        /// <summary>
        /// \if KO
        /// <para>Resume All 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the resume all operation.</para>
        /// \endif
        /// </summary>
        public void ResumeAll()
        {
        }

        /// <summary>
        /// \if KO
        /// <para>Get Thread 작업을 시도하고 성공 여부를 반환합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Attempts to get thread and returns whether the operation succeeds.</para>
        /// \endif
        /// </summary>
        /// <param name="threadName">
        /// \if KO
        /// <para>thread Name에 사용할 <c>string</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>string</c> value used for thread name.</para>
        /// \endif
        /// </param>
        /// <param name="thread">
        /// \if KO
        /// <para>thread에 사용할 <c>IDreamineThread?</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>IDreamineThread?</c> value used for thread.</para>
        /// \endif
        /// </param>
        /// <returns>
        /// \if KO
        /// <para>Try Get Thread 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
        /// \endif
        /// \if EN
        /// <para><see langword="true"/> when the try get thread condition is satisfied; otherwise, <see langword="false"/>.</para>
        /// \endif
        /// </returns>
        public bool TryGetThread(string threadName, out IDreamineThread? thread)
        {
            thread = null;
            return false;
        }

        /// <summary>
        /// \if KO
        /// <para>Threads 값을 가져옵니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets the threads value.</para>
        /// \endif
        /// </summary>
        /// <returns>
        /// \if KO
        /// <para>Get Threads 작업에서 생성한 <c>IReadOnlyList&lt;IDreamineThread&gt;</c> 결과입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>IReadOnlyList&lt;IDreamineThread&gt;</c> result produced by the get threads operation.</para>
        /// \endif
        /// </returns>
        public IReadOnlyList<IDreamineThread> GetThreads() => Array.Empty<IDreamineThread>();

        /// <summary>
        /// \if KO
        /// <para>Thread Infos 값을 가져옵니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets the thread infos value.</para>
        /// \endif
        /// </summary>
        /// <returns>
        /// \if KO
        /// <para>Get Thread Infos 작업에서 생성한 <c>IReadOnlyList&lt;DreamineThreadInfo&gt;</c> 결과입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>IReadOnlyList&lt;DreamineThreadInfo&gt;</c> result produced by the get thread infos operation.</para>
        /// \endif
        /// </returns>
        public IReadOnlyList<DreamineThreadInfo> GetThreadInfos() => Array.Empty<DreamineThreadInfo>();

        /// <summary>
        /// \if KO
        /// <para>이 인스턴스가 소유한 리소스를 해제합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Releases resources owned by this instance.</para>
        /// \endif
        /// </summary>
        public void Dispose()
        {
        }
    }
}
