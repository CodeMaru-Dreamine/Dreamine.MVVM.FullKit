using Dreamine.Threading.Interfaces;
using Dreamine.Threading.Models;
using Dreamine.Threading.Options;
using Dreamine.Threading.Policies;
using Dreamine.Threading.Registration;
using Dreamine.MVVM.Core;
using Dreamine.FullKit.Tests.Core;

namespace Dreamine.FullKit.Tests.Threading;

/// <summary>
/// \if KO
/// <para>Threading Policy Tests 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates threading policy tests functionality and related state.</para>
/// \endif
/// </summary>
[Collection(DMContainerCollection.Name)]
public sealed class ThreadingPolicyTests
{
    /// <summary>
    /// \if KO
    /// <para>Thread Options Normalize Repairs Invalid Values 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the thread options normalize repairs invalid values operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void ThreadOptions_NormalizeRepairsInvalidValues()
    {
        var options = new DreamineThreadOptions
        {
            Name = "",
            IntervalMs = -5,
            AutoThreadsPerCore = 0,
            OverflowPollingIntervalMs = -1
        }.Normalize();

        Assert.Equal("DreamineThread", options.Name);
        Assert.Equal(10, options.IntervalMs);
        Assert.Equal(2, options.AutoThreadsPerCore);
        Assert.Equal(100, options.OverflowPollingIntervalMs);
    }

    /// <summary>
    /// \if KO
    /// <para>Thread Job Options Normalize Repairs Invalid Values 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the thread job options normalize repairs invalid values operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void ThreadJobOptions_NormalizeRepairsInvalidValues()
    {
        var options = new DreamineThreadJobOptions
        {
            Name = " ",
            IntervalMs = -1
        }.Normalize();

        Assert.Equal("DreamineThreadJob", options.Name);
        Assert.Equal(10, options.IntervalMs);
    }

    /// <summary>
    /// \if KO
    /// <para>Fixed And Overflow Policies Return Expected Delays 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the fixed and overflow policies return expected delays operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void FixedAndOverflowPolicies_ReturnExpectedDelays()
    {
        var options = new DreamineThreadOptions { IntervalMs = 25, OverflowPollingIntervalMs = 80 };
        var context = new DreamineThreadCycleContext("worker", 1, 1, null, false, DateTimeOffset.UtcNow);

        Assert.Equal(25, new FixedIntervalCyclePolicy().GetDelayMs(options, DreamineThreadCoreAssignment.None(), context));
        Assert.Equal(80, new FixedIntervalCyclePolicy().GetDelayMs(options, DreamineThreadCoreAssignment.Overflow(), context));
        Assert.Equal(80, new OverflowPollingPolicy().GetDelayMs(options, DreamineThreadCoreAssignment.None(), context));
    }

    /// <summary>
    /// \if KO
    /// <para>Adaptive Cpu Policy Returns Delay By Cpu Usage 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the adaptive cpu policy returns delay by cpu usage operation.</para>
    /// \endif
    /// </summary>
    /// <param name="cpuUsage">
    /// \if KO
    /// <para>cpu Usage에 사용할 <c>double</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>double</c> value used for cpu usage.</para>
    /// \endif
    /// </param>
    /// <param name="expectedDelay">
    /// \if KO
    /// <para>expected Delay에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for expected delay.</para>
    /// \endif
    /// </param>
    [Theory]
    [InlineData(10, 0)]
    [InlineData(30, 1)]
    [InlineData(50, 3)]
    [InlineData(70, 5)]
    public void AdaptiveCpuPolicy_ReturnsDelayByCpuUsage(double cpuUsage, int expectedDelay)
    {
        var policy = new AdaptiveCpuCyclePolicy(new CpuUsageProvider(cpuUsage));
        var options = new DreamineThreadOptions { IntervalMs = 0, UseAdaptiveCpuDelay = true };
        var context = new DreamineThreadCycleContext("worker", 1, 1, null, false, DateTimeOffset.UtcNow);

        var delay = policy.GetDelayMs(options, DreamineThreadCoreAssignment.None(), context);

        Assert.Equal(expectedDelay, delay);
    }

    /// <summary>
    /// \if KO
    /// <para>Core Assignment Factories Expose Expected Flags 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the core assignment factories expose expected flags operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void CoreAssignmentFactories_ExposeExpectedFlags()
    {
        Assert.False(DreamineThreadCoreAssignment.None().UseAffinity);
        Assert.True(DreamineThreadCoreAssignment.Dedicated(2, true).IsDedicatedWorker);
        Assert.True(DreamineThreadCoreAssignment.Overflow().IsOverflowPolling);
        Assert.True(new DreamineThreadingOptions().RegisterWindowsServices);
    }

    /// <summary>
    /// \if KO
    /// <para>Threading Registration Uses Fixed Policy When Cpu Usage Provider Is Missing 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the threading registration uses fixed policy when cpu usage provider is missing operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void ThreadingRegistration_UsesFixedPolicyWhenCpuUsageProviderIsMissing()
    {
        DMContainer.Reset();

        try
        {
            DreamineThreadingRegistration.Register(new DreamineThreadingOptions
            {
                UseAdaptiveCpuPolicy = true
            });

            Assert.IsType<FixedIntervalCyclePolicy>(
                DMContainer.Resolve<IThreadCyclePolicy>());
            Assert.NotNull(DMContainer.Resolve<IDreamineThreadManager>());
        }
        finally
        {
            DMContainer.Reset();
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Thread Options Normalize Repairs Invalid Stop Timeout 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the thread options normalize repairs invalid stop timeout operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void ThreadOptions_NormalizeRepairsInvalidStopTimeout()
    {
        var options = new DreamineThreadOptions
        {
            StopTimeout = TimeSpan.Zero
        }.Normalize();

        Assert.Equal(TimeSpan.FromSeconds(2), options.StopTimeout);
    }

    /// <summary>
    /// \if KO
    /// <para>Cpu Usage Provider 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates cpu usage provider functionality and related state.</para>
    /// \endif
    /// </summary>
    private sealed class CpuUsageProvider : ICpuUsageProvider
    {
        /// <summary>
        /// \if KO
        /// <para>cpu Usage 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the cpu usage value.</para>
        /// \endif
        /// </summary>
        private readonly double _cpuUsage;

        /// <summary>
        /// \if KO
        /// <para>지정한 설정으로 <see cref="CpuUsageProvider"/> 클래스의 새 인스턴스를 초기화합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Initializes a new instance of the <see cref="CpuUsageProvider"/> class with the specified settings.</para>
        /// \endif
        /// </summary>
        /// <param name="cpuUsage">
        /// \if KO
        /// <para>cpu Usage에 사용할 <c>double</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>double</c> value used for cpu usage.</para>
        /// \endif
        /// </param>
        public CpuUsageProvider(double cpuUsage)
        {
            _cpuUsage = cpuUsage;
        }

        /// <summary>
        /// \if KO
        /// <para>Total Cpu Usage Percent 값을 가져옵니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets the total cpu usage percent value.</para>
        /// \endif
        /// </summary>
        /// <returns>
        /// \if KO
        /// <para>Get Total Cpu Usage Percent 작업에서 생성한 <c>double</c> 결과입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>double</c> result produced by the get total cpu usage percent operation.</para>
        /// \endif
        /// </returns>
        public double GetTotalCpuUsagePercent()
        {
            return _cpuUsage;
        }
    }
}
