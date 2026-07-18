using Dreamine.MVVM.Core.DependencyInjection;
using Dreamine.MVVM.Core;

namespace Dreamine.FullKit.Tests.Core;

// All classes in this file modify the DMContainer static facade.
// Placing them in the same xUnit Collection serializes their execution
// and prevents cross-class state leakage.
/// <summary>
/// \if KO
/// <para>DM Container Collection 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates dm container collection functionality and related state.</para>
/// \endif
/// </summary>
[CollectionDefinition(Name)]
public sealed class DMContainerCollection : ICollectionFixture<DMContainerFixture>
{
    /// <summary>
    /// \if KO
    /// <para>Name 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the name value.</para>
    /// \endif
    /// </summary>
    public const string Name = "DMContainer";
}

/// <summary>
/// \if KO
/// <para>DM Container Fixture 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Test fixture that resets the DMContainer static facade before and after each test class. Use with IClassFixture&lt;DMContainerFixture&gt; for test classes that register into DMContainer, or add <c>[Collection(DMContainerCollection.Name)]</c> to serialize DMContainer access.</para>
/// \endif
/// </summary>
public sealed class DMContainerFixture : IDisposable
{
    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="DMContainerFixture"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="DMContainerFixture"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    public DMContainerFixture() => DMContainer.Reset();
    /// <summary>
    /// \if KO
    /// <para>이 인스턴스가 소유한 리소스를 해제합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Releases resources owned by this instance.</para>
    /// \endif
    /// </summary>
    public void Dispose() => DMContainer.Reset();
}

/// <summary>
/// \if KO
/// <para>DM Container Fixture Pattern Tests 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Demonstrates using <see cref="DMContainerFixture"/> to isolate static container state.</para>
/// \endif
/// </summary>
[Collection(DMContainerCollection.Name)]
public sealed class DMContainerFixturePatternTests
{
    /// <summary>
    /// \if KO
    /// <para>DM Container Starts Empty When Fixture Is Used 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the dm container starts empty when fixture is used operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void DMContainer_StartsEmpty_WhenFixtureIsUsed()
    {
        DMContainer.Reset();
        Assert.False(DMContainer.IsRegistered<IDisposable>());
    }

    /// <summary>
    /// \if KO
    /// <para>DM Container Get Resolver Returns Working Resolver 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the dm container get resolver returns working resolver operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void DMContainer_GetResolver_ReturnsWorkingResolver()
    {
        DMContainer.Reset();
        DMContainer.Register<DMContainerFixture>();

        var resolver = DMContainer.GetResolver();
        var instance = resolver.Resolve<DMContainerFixture>();

        Assert.NotNull(instance);
    }

    /// <summary>
    /// \if KO
    /// <para>DM Container Try Resolve Returns False When Not Registered 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the dm container try resolve returns false when not registered operation.</para>
    /// \endif
    /// </summary>
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

/// <summary>
/// \if KO
/// <para>Dreamine Container Tests 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates dreamine container tests functionality and related state.</para>
/// \endif
/// </summary>
[Collection(DMContainerCollection.Name)]
public sealed class DreamineContainerTests
{
    /// <summary>
    /// \if KO
    /// <para>Resolve Creates Registered Transient With Constructor Dependencies 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the resolve creates registered transient with constructor dependencies operation.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>Resolve Reuses Registered Singleton Implementation 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the resolve reuses registered singleton implementation operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void Resolve_ReusesRegisteredSingletonImplementation()
    {
        var container = new DreamineContainer();
        container.RegisterSingleton<IClock, FixedClock>();

        var first = container.Resolve<IClock>();
        var second = container.Resolve<IClock>();

        Assert.Same(first, second);
    }

    /// <summary>
    /// \if KO
    /// <para>Resolve Creates Unregistered Concrete Type 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the resolve creates unregistered concrete type operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void Resolve_CreatesUnregisteredConcreteType()
    {
        var container = new DreamineContainer();

        var instance = container.Resolve<FixedClock>();

        Assert.Equal(new DateOnly(2026, 6, 7), instance.Today);
    }

    /// <summary>
    /// \if KO
    /// <para>Resolve Throws For Unregistered Interface 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the resolve throws for unregistered interface operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void Resolve_ThrowsForUnregisteredInterface()
    {
        var container = new DreamineContainer();

        var exception = Assert.Throws<InvalidOperationException>(
            () => container.Resolve<IClock>());

        Assert.Contains("is not registered", exception.Message);
    }

    /// <summary>
    /// \if KO
    /// <para>Resolve Detects Circular Dependencies 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the resolve detects circular dependencies operation.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>Resolve Creates Singleton Only Once Across Concurrent Calls 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the resolve creates singleton only once across concurrent calls operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Resolve Creates Singleton Only Once Across Concurrent Calls 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the resolve creates singleton only once across concurrent calls operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Resolve Does Not Share Circular State Across Concurrent Calls 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the resolve does not share circular state across concurrent calls operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Resolve Does Not Share Circular State Across Concurrent Calls 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the resolve does not share circular state across concurrent calls operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Register Replaces Previous Singleton Cache 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the register replaces previous singleton cache operation.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>Dreamine Auto Registrar Register All Single Assembly Overload Registers In DM Container 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the dreamine auto registrar register all single assembly overload registers in dm container operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void DreamineAutoRegistrar_RegisterAll_SingleAssemblyOverload_RegistersInDMContainer()
    {
        DMContainer.Reset();

        // The no-registry overload scans the given assembly and registers into DMContainer directly.
        Dreamine.MVVM.Core.DreamineAutoRegistrar.RegisterAll(typeof(DreamineContainerTests).Assembly);

        // AutoRegistrationViewModel matches the naming convention filter used by DreamineAutoRegistrar.
        Assert.True(DMContainer.IsRegistered<AutoRegistrationViewModel>());
    }

    /// <summary>
    /// \if KO
    /// <para>DM Container Reset Clears Static Facade Registrations 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the dm container reset clears static facade registrations operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void DMContainer_ResetClearsStaticFacadeRegistrations()
    {
        DMContainer.Reset();
        DMContainer.Register<IClock, FixedClock>();

        Assert.True(DMContainer.IsRegistered<IClock>());

        DMContainer.Reset();

        Assert.False(DMContainer.IsRegistered<IClock>());
    }

    /// <summary>
    /// \if KO
    /// <para>Dreamine Container Dispose Disposes I Disposable Singletons 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the dreamine container dispose disposes i disposable singletons operation.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>Dreamine Container Dispose Is Idempotent 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the dreamine container dispose is idempotent operation.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>DM Container Reset Disposes Old Container Singletons 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the dm container reset disposes old container singletons operation.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>I Clock 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates i clock functionality and related state.</para>
    /// \endif
    /// </summary>
    private interface IClock
    {
        /// <summary>
        /// \if KO
        /// <para>Today 값을 가져옵니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets the today value.</para>
        /// \endif
        /// </summary>
        DateOnly Today { get; }
    }

    /// <summary>
    /// \if KO
    /// <para>Fixed Clock 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates fixed clock functionality and related state.</para>
    /// \endif
    /// </summary>
    private sealed class FixedClock : IClock
    {
        /// <summary>
        /// \if KO
        /// <para>Today 값을 가져옵니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets the today value.</para>
        /// \endif
        /// </summary>
        public DateOnly Today { get; } = new(2026, 6, 7);
    }

    /// <summary>
    /// \if KO
    /// <para>Alternate Clock 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates alternate clock functionality and related state.</para>
    /// \endif
    /// </summary>
    private sealed class AlternateClock : IClock
    {
        /// <summary>
        /// \if KO
        /// <para>Today 값을 가져옵니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets the today value.</para>
        /// \endif
        /// </summary>
        public DateOnly Today { get; } = new(2026, 6, 8);
    }

    /// <summary>
    /// \if KO
    /// <para>Slow Singleton 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates slow singleton functionality and related state.</para>
    /// \endif
    /// </summary>
    private sealed class SlowSingleton
    {
        /// <summary>
        /// \if KO
        /// <para>created Count 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the created count value.</para>
        /// \endif
        /// </summary>
        private static int _createdCount;

        /// <summary>
        /// \if KO
        /// <para>지정한 설정으로 <see cref="SlowSingleton"/> 클래스의 새 인스턴스를 초기화합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Initializes a new instance of the <see cref="SlowSingleton"/> class with the specified settings.</para>
        /// \endif
        /// </summary>
        public SlowSingleton()
        {
            Thread.Sleep(20);
            Interlocked.Increment(ref _createdCount);
        }

        /// <summary>
        /// \if KO
        /// <para>Created Count 값을 가져옵니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets the created count value.</para>
        /// \endif
        /// </summary>
        public static int CreatedCount => Volatile.Read(ref _createdCount);

        /// <summary>
        /// \if KO
        /// <para>Reset 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the reset operation.</para>
        /// \endif
        /// </summary>
        public static void Reset()
        {
            Volatile.Write(ref _createdCount, 0);
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Independent A 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates independent a functionality and related state.</para>
    /// \endif
    /// </summary>
    private sealed class IndependentA
    {
    }

    /// <summary>
    /// \if KO
    /// <para>Independent B 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates independent b functionality and related state.</para>
    /// \endif
    /// </summary>
    private sealed class IndependentB
    {
    }

    /// <summary>
    /// \if KO
    /// <para>Report Service 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates report service functionality and related state.</para>
    /// \endif
    /// </summary>
    private sealed class ReportService
    {
        /// <summary>
        /// \if KO
        /// <para>지정한 설정으로 <see cref="ReportService"/> 클래스의 새 인스턴스를 초기화합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Initializes a new instance of the <see cref="ReportService"/> class with the specified settings.</para>
        /// \endif
        /// </summary>
        /// <param name="clock">
        /// \if KO
        /// <para>clock에 사용할 <c>IClock</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>IClock</c> value used for clock.</para>
        /// \endif
        /// </param>
        public ReportService(IClock clock)
        {
            Clock = clock;
        }

        /// <summary>
        /// \if KO
        /// <para>Clock 값을 가져옵니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets the clock value.</para>
        /// \endif
        /// </summary>
        public IClock Clock { get; }

        /// <summary>
        /// \if KO
        /// <para>Report Name 값을 생성합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Creates the report name value.</para>
        /// \endif
        /// </summary>
        /// <returns>
        /// \if KO
        /// <para>Create Report Name 작업에서 생성한 <c>string</c> 결과입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>string</c> result produced by the create report name operation.</para>
        /// \endif
        /// </returns>
        public string CreateReportName()
        {
            return Clock.Today.ToString("yyyy-MM-dd");
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Circular A 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates circular a functionality and related state.</para>
    /// \endif
    /// </summary>
    private sealed class CircularA
    {
        /// <summary>
        /// \if KO
        /// <para>지정한 설정으로 <see cref="CircularA"/> 클래스의 새 인스턴스를 초기화합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Initializes a new instance of the <see cref="CircularA"/> class with the specified settings.</para>
        /// \endif
        /// </summary>
        /// <param name="dependency">
        /// \if KO
        /// <para>dependency에 사용할 <c>CircularB</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>CircularB</c> value used for dependency.</para>
        /// \endif
        /// </param>
        public CircularA(CircularB dependency)
        {
            Dependency = dependency;
        }

        /// <summary>
        /// \if KO
        /// <para>Dependency 값을 가져옵니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets the dependency value.</para>
        /// \endif
        /// </summary>
        public CircularB Dependency { get; }
    }

    /// <summary>
    /// \if KO
    /// <para>Circular B 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates circular b functionality and related state.</para>
    /// \endif
    /// </summary>
    private sealed class CircularB
    {
        /// <summary>
        /// \if KO
        /// <para>지정한 설정으로 <see cref="CircularB"/> 클래스의 새 인스턴스를 초기화합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Initializes a new instance of the <see cref="CircularB"/> class with the specified settings.</para>
        /// \endif
        /// </summary>
        /// <param name="dependency">
        /// \if KO
        /// <para>dependency에 사용할 <c>CircularA</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>CircularA</c> value used for dependency.</para>
        /// \endif
        /// </param>
        public CircularB(CircularA dependency)
        {
            Dependency = dependency;
        }

        /// <summary>
        /// \if KO
        /// <para>Dependency 값을 가져옵니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets the dependency value.</para>
        /// \endif
        /// </summary>
        public CircularA Dependency { get; }
    }

    // Matches the ViewModel naming convention used by NamingConventionAutoRegistrationFilter.
    /// <summary>
    /// \if KO
    /// <para>Auto Registration View Model 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates auto registration view model functionality and related state.</para>
    /// \endif
    /// </summary>
    public sealed class AutoRegistrationViewModel { }

    /// <summary>
    /// \if KO
    /// <para>Disposable Singleton 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates disposable singleton functionality and related state.</para>
    /// \endif
    /// </summary>
    private sealed class DisposableSingleton : IDisposable
    {
        /// <summary>
        /// \if KO
        /// <para>dispose Count 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the dispose count value.</para>
        /// \endif
        /// </summary>
        private int _disposeCount;

        /// <summary>
        /// \if KO
        /// <para>Is Disposed 값을 가져옵니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets the is disposed value.</para>
        /// \endif
        /// </summary>
        public bool IsDisposed => Volatile.Read(ref _disposeCount) > 0;
        /// <summary>
        /// \if KO
        /// <para>Dispose Count 값을 가져옵니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets the dispose count value.</para>
        /// \endif
        /// </summary>
        public int DisposeCount => Volatile.Read(ref _disposeCount);

        /// <summary>
        /// \if KO
        /// <para>이 인스턴스가 소유한 리소스를 해제합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Releases resources owned by this instance.</para>
        /// \endif
        /// </summary>
        public void Dispose() => Interlocked.Increment(ref _disposeCount);
    }
}
