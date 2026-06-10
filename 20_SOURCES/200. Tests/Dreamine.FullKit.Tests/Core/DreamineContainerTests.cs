using Dreamine.MVVM.Core.DependencyInjection;

namespace Dreamine.FullKit.Tests.Core;

public sealed class DreamineContainerTests
{
    [Fact]
    public void Resolve_CreatesRegisteredTransientWithConstructorDependencies()
    {
        var container = new DreamineContainer();
        container.Register<IClock, FixedClock>();
        container.Register<ReportService>();

        var first = container.Resolve<ReportService>();
        var second = container.Resolve<ReportService>();

        Assert.NotSame(first, second);
        Assert.IsType<FixedClock>(first.Clock);
        Assert.Equal("2026-06-07", first.CreateReportName());
    }

    [Fact]
    public void Resolve_ReusesRegisteredSingletonImplementation()
    {
        var container = new DreamineContainer();
        container.RegisterSingleton<IClock, FixedClock>();

        var first = container.Resolve<IClock>();
        var second = container.Resolve<IClock>();

        Assert.Same(first, second);
    }

    [Fact]
    public void Resolve_CreatesUnregisteredConcreteType()
    {
        var container = new DreamineContainer();

        var instance = container.Resolve<FixedClock>();

        Assert.Equal(new DateOnly(2026, 6, 7), instance.Today);
    }

    [Fact]
    public void Resolve_ThrowsForUnregisteredInterface()
    {
        var container = new DreamineContainer();

        var exception = Assert.Throws<InvalidOperationException>(
            () => container.Resolve<IClock>());

        Assert.Contains("is not registered", exception.Message);
    }

    [Fact]
    public void Resolve_DetectsCircularDependencies()
    {
        var container = new DreamineContainer();
        container.Register<CircularA>();
        container.Register<CircularB>();

        var exception = Assert.Throws<InvalidOperationException>(
            () => container.Resolve<CircularA>());

        Assert.Contains("Circular dependency", exception.Message);
    }

    private interface IClock
    {
        DateOnly Today { get; }
    }

    private sealed class FixedClock : IClock
    {
        public DateOnly Today { get; } = new(2026, 6, 7);
    }

    private sealed class ReportService
    {
        public ReportService(IClock clock)
        {
            Clock = clock;
        }

        public IClock Clock { get; }

        public string CreateReportName()
        {
            return Clock.Today.ToString("yyyy-MM-dd");
        }
    }

    private sealed class CircularA
    {
        public CircularA(CircularB dependency)
        {
            Dependency = dependency;
        }

        public CircularB Dependency { get; }
    }

    private sealed class CircularB
    {
        public CircularB(CircularA dependency)
        {
            Dependency = dependency;
        }

        public CircularA Dependency { get; }
    }
}
