using Dreamine.MVVM.Core.DependencyInjection;
using Dreamine.MVVM.Core;

namespace Dreamine.FullKit.Tests.Core;

// All classes in this file modify the DMContainer static facade.
// Placing them in the same xUnit Collection serializes their execution
// and prevents cross-class state leakage.
[CollectionDefinition(Name)]
public sealed class DMContainerCollection : ICollectionFixture<DMContainerFixture>
{
    public const string Name = "DMContainer";
}

/// <summary>
/// Test fixture that resets the DMContainer static facade before and after each test class.
/// Use with IClassFixture&lt;DMContainerFixture&gt; for test classes that register into DMContainer,
/// or add <c>[Collection(DMContainerCollection.Name)]</c> to serialize DMContainer access.
/// </summary>
/// <example>
/// <code>
/// [Collection(DMContainerCollection.Name)]
/// public sealed class MyTests
/// {
///     ...
/// }
/// </code>
/// </example>
public sealed class DMContainerFixture : IDisposable
{
    public DMContainerFixture() => DMContainer.Reset();
    public void Dispose() => DMContainer.Reset();
}

/// <summary>
/// Demonstrates using <see cref="DMContainerFixture"/> to isolate static container state.
/// </summary>
[Collection(DMContainerCollection.Name)]
public sealed class DMContainerFixturePatternTests
{
    [Fact]
    public void DMContainer_StartsEmpty_WhenFixtureIsUsed()
    {
        DMContainer.Reset();
        Assert.False(DMContainer.IsRegistered<IDisposable>());
    }

    [Fact]
    public void DMContainer_GetResolver_ReturnsWorkingResolver()
    {
        DMContainer.Reset();
        DMContainer.Register<DMContainerFixture>();

        var resolver = DMContainer.GetResolver();
        var instance = resolver.Resolve<DMContainerFixture>();

        Assert.NotNull(instance);
    }

    [Fact]
    public void DMContainer_TryResolve_ReturnsFalse_WhenNotRegistered()
    {
        DMContainer.Reset();
        var resolver = DMContainer.GetResolver();
        var found = resolver.TryResolve<IDisposable>(out var result);

        Assert.False(found);
        Assert.Null(result);
    }
}

[Collection(DMContainerCollection.Name)]
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

    [Fact]
    public async Task Resolve_CreatesSingletonOnlyOnceAcrossConcurrentCalls()
    {
        SlowSingleton.Reset();

        var container = new DreamineContainer();
        container.RegisterSingleton<SlowSingleton>();

        var tasks = Enumerable.Range(0, 32)
            .Select(_ => Task.Run(() => container.Resolve<SlowSingleton>()))
            .ToArray();

        var instances = await Task.WhenAll(tasks);

        Assert.Single(instances.Distinct());
        Assert.Equal(1, SlowSingleton.CreatedCount);
    }

    [Fact]
    public async Task Resolve_DoesNotShareCircularStateAcrossConcurrentCalls()
    {
        var container = new DreamineContainer();
        container.Register<IndependentA>();
        container.Register<IndependentB>();

        var tasks = Enumerable.Range(0, 16)
            .Select(index => Task.Run<object>(() =>
                index % 2 == 0
                    ? container.Resolve<IndependentA>()
                    : container.Resolve<IndependentB>()))
            .ToArray();

        var instances = await Task.WhenAll(tasks);

        Assert.Equal(16, instances.Length);
        Assert.All(instances, Assert.NotNull);
    }

    [Fact]
    public void Register_ReplacesPreviousSingletonCache()
    {
        var container = new DreamineContainer();
        container.RegisterSingleton<IClock, FixedClock>();

        _ = container.Resolve<IClock>();

        container.Register<IClock, AlternateClock>();

        var resolved = container.Resolve<IClock>();

        Assert.IsType<AlternateClock>(resolved);
    }

    [Fact]
    public void DreamineAutoRegistrar_RegisterAll_SingleAssemblyOverload_RegistersInDMContainer()
    {
        DMContainer.Reset();

        // The no-registry overload scans the given assembly and registers into DMContainer directly.
        Dreamine.MVVM.Core.DreamineAutoRegistrar.RegisterAll(typeof(DreamineContainerTests).Assembly);

        // AutoRegistrationViewModel matches the naming convention filter used by DreamineAutoRegistrar.
        Assert.True(DMContainer.IsRegistered<AutoRegistrationViewModel>());
    }

    [Fact]
    public void DMContainer_ResetClearsStaticFacadeRegistrations()
    {
        DMContainer.Reset();
        DMContainer.Register<IClock, FixedClock>();

        Assert.True(DMContainer.IsRegistered<IClock>());

        DMContainer.Reset();

        Assert.False(DMContainer.IsRegistered<IClock>());
    }

    [Fact]
    public void DreamineContainer_Dispose_DisposesIDisposableSingletons()
    {
        var container = new DreamineContainer();
        var disposable = new DisposableSingleton();
        container.RegisterSingleton<DisposableSingleton>(disposable);

        _ = container.Resolve<DisposableSingleton>();
        Assert.False(disposable.IsDisposed);

        container.Dispose();

        Assert.True(disposable.IsDisposed);
    }

    [Fact]
    public void DreamineContainer_Dispose_IsIdempotent()
    {
        var container = new DreamineContainer();
        var disposable = new DisposableSingleton();
        container.RegisterSingleton<DisposableSingleton>(disposable);

        container.Dispose();
        container.Dispose();

        Assert.Equal(1, disposable.DisposeCount);
    }

    [Fact]
    public void DMContainer_Reset_DisposesOldContainerSingletons()
    {
        DMContainer.Reset();
        var disposable = new DisposableSingleton();
        DMContainer.RegisterSingleton(disposable);

        _ = DMContainer.Resolve<DisposableSingleton>();
        Assert.False(disposable.IsDisposed);

        DMContainer.Reset();

        Assert.True(disposable.IsDisposed);
    }

    private interface IClock
    {
        DateOnly Today { get; }
    }

    private sealed class FixedClock : IClock
    {
        public DateOnly Today { get; } = new(2026, 6, 7);
    }

    private sealed class AlternateClock : IClock
    {
        public DateOnly Today { get; } = new(2026, 6, 8);
    }

    private sealed class SlowSingleton
    {
        private static int _createdCount;

        public SlowSingleton()
        {
            Thread.Sleep(20);
            Interlocked.Increment(ref _createdCount);
        }

        public static int CreatedCount => Volatile.Read(ref _createdCount);

        public static void Reset()
        {
            Volatile.Write(ref _createdCount, 0);
        }
    }

    private sealed class IndependentA
    {
    }

    private sealed class IndependentB
    {
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

    // Matches the ViewModel naming convention used by NamingConventionAutoRegistrationFilter.
    public sealed class AutoRegistrationViewModel { }

    private sealed class DisposableSingleton : IDisposable
    {
        private int _disposeCount;

        public bool IsDisposed => Volatile.Read(ref _disposeCount) > 0;
        public int DisposeCount => Volatile.Read(ref _disposeCount);

        public void Dispose() => Interlocked.Increment(ref _disposeCount);
    }
}
