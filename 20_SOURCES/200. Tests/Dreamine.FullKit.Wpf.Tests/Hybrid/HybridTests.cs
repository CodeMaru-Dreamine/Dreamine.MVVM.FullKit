using System.Globalization;
using System.Reflection;
using System.Windows;
using Dreamine.Hybrid.Messaging;
using Dreamine.Hybrid.Interfaces;
using Dreamine.Hybrid.State;
using Dreamine.Hybrid.Wpf.Converters;
using Dreamine.Hybrid.Wpf.Hosting;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Dreamine.MVVM.Wpf;

namespace Dreamine.FullKit.Wpf.Tests.Hybrid;

/// <summary>
/// \if KO
/// <para>Hybrid Tests 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates hybrid tests functionality and related state.</para>
/// \endif
/// </summary>
public sealed class HybridTests
{
    /// <summary>
    /// \if KO
    /// <para>In Memory Hybrid Message Bus Publishes And Unsubscribes By Message Type 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the in memory hybrid message bus publishes and unsubscribes by message type operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>In Memory Hybrid Message Bus Publishes And Unsubscribes By Message Type 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the in memory hybrid message bus publishes and unsubscribes by message type operation.</para>
    /// \endif
    /// </returns>
    [Fact]
    public async Task InMemoryHybridMessageBus_PublishesAndUnsubscribesByMessageType()
    {
        var bus = new InMemoryHybridMessageBus();
        var received = new List<int>();

        using var subscription = bus.Subscribe<TestMessage>((message, _) =>
        {
            received.Add(message.Value);
            return Task.CompletedTask;
        });

        await bus.PublishAsync(new TestMessage(3));
        subscription.Dispose();
        await bus.PublishAsync(new TestMessage(4));

        Assert.Equal(new[] { 3 }, received);
    }

    /// <summary>
    /// \if KO
    /// <para>Hybrid State Store Updates State And Raises Event 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the hybrid state store updates state and raises event operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void HybridStateStore_UpdatesStateAndRaisesEvent()
    {
        var store = new HybridStateStore<int>(1);
        var states = new List<int>();
        store.StateChanged += (_, args) => states.Add(args.State);

        store.SetState(2);
        store.Update(value => value + 3);

        Assert.Equal(5, store.State);
        Assert.Equal(new[] { 2, 5 }, states);
    }

    /// <summary>
    /// \if KO
    /// <para>Hybrid State Store Subscribe Disposes Event Handler 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the hybrid state store subscribe disposes event handler operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void HybridStateStore_SubscribeDisposesEventHandler()
    {
        var store = new HybridStateStore<int>(1);
        var states = new List<int>();

        using (store.Subscribe((_, args) => states.Add(args.State)))
        {
            store.SetState(2);
        }

        store.SetState(3);

        Assert.Equal(new[] { 2 }, states);
    }

    /// <summary>
    /// \if KO
    /// <para>Boolean To Visibility Converter Converts Both Ways 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the boolean to visibility converter converts both ways operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void BooleanToVisibilityConverter_ConvertsBothWays()
    {
        var converter = BooleanToVisibilityConverter.Instance;

        Assert.Equal(Visibility.Visible, converter.Convert(true, typeof(Visibility), null!, CultureInfo.InvariantCulture));
        Assert.Equal(Visibility.Collapsed, converter.Convert(false, typeof(Visibility), null!, CultureInfo.InvariantCulture));
        Assert.True((bool)converter.ConvertBack(Visibility.Visible, typeof(bool), null!, CultureInfo.InvariantCulture));
        Assert.False((bool)converter.ConvertBack(Visibility.Collapsed, typeof(bool), null!, CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// \if KO
    /// <para>Dreamine Wpf Options Create Default Enables Bootstrap Features 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the dreamine wpf options create default enables bootstrap features operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void DreamineWpfOptions_CreateDefaultEnablesBootstrapFeatures()
    {
        var options = DreamineWpfOptions.CreateDefault();

        Assert.True(options.EnableGlobalAutoWireOnLoaded);
        Assert.True(options.RegisterDefaultServices);
        Assert.True(options.EnableAutoNavigatorRegistration);
        Assert.Equal("SubPage", options.DefaultRegionName);
    }

    /// <summary>
    /// \if KO
    /// <para>Blazor Server Hosted Service Rejects Disposable Shared Services By Default 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the blazor server hosted service rejects disposable shared services by default operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void BlazorServerHostedService_RejectsDisposableSharedServicesByDefault()
    {
        var rootServices = new ServiceCollection();
        rootServices.AddSingleton<DisposableSharedService>();
        rootServices.AddSingleton<IHybridMessageBus, InMemoryHybridMessageBus>();

        using ServiceProvider rootProvider = rootServices.BuildServiceProvider();
        var options = new DreamineBlazorServerHostOptions();
        options.SharedServiceTypes.Add(typeof(DisposableSharedService));

        var service = new DreamineBlazorServerHostedService<TestRootComponent>(
            rootProvider,
            rootProvider.GetRequiredService<IHybridMessageBus>(),
            options);

        MethodInfo method = typeof(DreamineBlazorServerHostedService<TestRootComponent>)
            .GetMethod("RegisterSharedServices", BindingFlags.Instance | BindingFlags.NonPublic)!;

        var targetServices = new ServiceCollection();
        TargetInvocationException exception = Assert.Throws<TargetInvocationException>(
            () => method.Invoke(service, new object[] { targetServices }));

        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    /// <summary>
    /// \if KO
    /// <para>Design Time Guard Is In Design Mode Can Be Read 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the design time guard is in design mode can be read operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void DesignTimeGuard_IsInDesignModeCanBeRead()
    {
        _ = Dreamine.Hybrid.Wpf.Utility.DesignTimeGuard.IsInDesignMode;
    }

    /// <summary>
    /// \if KO
    /// <para>Test Message 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates test message functionality and related state.</para>
    /// \endif
    /// </summary>
    private sealed class TestMessage : HybridMessageBase
    {
        /// <summary>
        /// \if KO
        /// <para>지정한 설정으로 <see cref="TestMessage"/> 클래스의 새 인스턴스를 초기화합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Initializes a new instance of the <see cref="TestMessage"/> class with the specified settings.</para>
        /// \endif
        /// </summary>
        /// <param name="value">
        /// \if KO
        /// <para>적용할 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The value to apply.</para>
        /// \endif
        /// </param>
        public TestMessage(int value)
        {
            Value = value;
        }

        /// <summary>
        /// \if KO
        /// <para>Value 값을 가져옵니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets the value value.</para>
        /// \endif
        /// </summary>
        public int Value { get; }
    }

    /// <summary>
    /// \if KO
    /// <para>Disposable Shared Service 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates disposable shared service functionality and related state.</para>
    /// \endif
    /// </summary>
    private sealed class DisposableSharedService : IDisposable
    {
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

    /// <summary>
    /// \if KO
    /// <para>Test Root Component 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates test root component functionality and related state.</para>
    /// \endif
    /// </summary>
    private sealed class TestRootComponent : IComponent
    {
        /// <summary>
        /// \if KO
        /// <para>대상 객체에 동작을 연결합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Attaches the behavior to a target object.</para>
        /// \endif
        /// </summary>
        /// <param name="renderHandle">
        /// \if KO
        /// <para>render Handle에 사용할 <c>RenderHandle</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>RenderHandle</c> value used for render handle.</para>
        /// \endif
        /// </param>
        public void Attach(RenderHandle renderHandle)
        {
        }

        /// <summary>
        /// \if KO
        /// <para>Parameters Async 값을 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Sets the parameters async value.</para>
        /// \endif
        /// </summary>
        /// <param name="parameters">
        /// \if KO
        /// <para>parameters에 사용할 <c>ParameterView</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>ParameterView</c> value used for parameters.</para>
        /// \endif
        /// </param>
        /// <returns>
        /// \if KO
        /// <para>Set Parameters Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>Task</c> result produced by the set parameters async operation.</para>
        /// \endif
        /// </returns>
        public Task SetParametersAsync(ParameterView parameters)
        {
            return Task.CompletedTask;
        }
    }
}
