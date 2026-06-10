using Dreamine.Threading.Interfaces;
using Dreamine.Threading.Models;
using Dreamine.Threading.Options;
using Dreamine.Threading.Policies;
using Dreamine.Threading.Registration;
using Dreamine.MVVM.Core;

namespace Dreamine.FullKit.Tests.Threading;

public sealed class ThreadingPolicyTests
{
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

    [Fact]
    public void FixedAndOverflowPolicies_ReturnExpectedDelays()
    {
        var options = new DreamineThreadOptions { IntervalMs = 25, OverflowPollingIntervalMs = 80 };
        var context = new DreamineThreadCycleContext("worker", 1, 1, null, false, DateTimeOffset.UtcNow);

        Assert.Equal(25, new FixedIntervalCyclePolicy().GetDelayMs(options, DreamineThreadCoreAssignment.None(), context));
        Assert.Equal(80, new FixedIntervalCyclePolicy().GetDelayMs(options, DreamineThreadCoreAssignment.Overflow(), context));
        Assert.Equal(80, new OverflowPollingPolicy().GetDelayMs(options, DreamineThreadCoreAssignment.None(), context));
    }

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

    [Fact]
    public void CoreAssignmentFactories_ExposeExpectedFlags()
    {
        Assert.False(DreamineThreadCoreAssignment.None().UseAffinity);
        Assert.True(DreamineThreadCoreAssignment.Dedicated(2, true).IsDedicatedWorker);
        Assert.True(DreamineThreadCoreAssignment.Overflow().IsOverflowPolling);
        Assert.True(new DreamineThreadingOptions().RegisterWindowsServices);
    }

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

    [Fact]
    public void ThreadOptions_NormalizeRepairsInvalidStopTimeout()
    {
        var options = new DreamineThreadOptions
        {
            StopTimeout = TimeSpan.Zero
        }.Normalize();

        Assert.Equal(TimeSpan.FromSeconds(2), options.StopTimeout);
    }

    private sealed class CpuUsageProvider : ICpuUsageProvider
    {
        private readonly double _cpuUsage;

        public CpuUsageProvider(double cpuUsage)
        {
            _cpuUsage = cpuUsage;
        }

        public double GetTotalCpuUsagePercent()
        {
            return _cpuUsage;
        }
    }
}
