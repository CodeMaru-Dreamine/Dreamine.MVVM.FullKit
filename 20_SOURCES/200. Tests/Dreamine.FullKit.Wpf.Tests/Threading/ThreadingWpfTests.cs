using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Dreamine.Threading.Models;
using Dreamine.Threading.Windows.Services;
using Dreamine.Threading.Wpf.Converters;
using Dreamine.Threading.Wpf.ViewModels;

namespace Dreamine.FullKit.Wpf.Tests.Threading;

public sealed class ThreadingWpfTests
{
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

    [Fact]
    public void WindowsCpuInfoProvider_ReportsValidProcessorRange()
    {
        var provider = new WindowsCpuInfoProvider();

        Assert.True(provider.GetLogicalProcessorCount() >= 1);
        Assert.True(provider.IsValidCoreIndex(0));
        Assert.False(provider.IsValidCoreIndex(-1));
        Assert.False(provider.IsValidCoreIndex(provider.GetLogicalProcessorCount()));
    }

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
}
