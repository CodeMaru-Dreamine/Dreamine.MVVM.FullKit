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

public sealed class HybridTests
{
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

    [Fact]
    public void BooleanToVisibilityConverter_ConvertsBothWays()
    {
        var converter = BooleanToVisibilityConverter.Instance;

        Assert.Equal(Visibility.Visible, converter.Convert(true, typeof(Visibility), null!, CultureInfo.InvariantCulture));
        Assert.Equal(Visibility.Collapsed, converter.Convert(false, typeof(Visibility), null!, CultureInfo.InvariantCulture));
        Assert.True((bool)converter.ConvertBack(Visibility.Visible, typeof(bool), null!, CultureInfo.InvariantCulture));
        Assert.False((bool)converter.ConvertBack(Visibility.Collapsed, typeof(bool), null!, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void DreamineWpfOptions_CreateDefaultEnablesBootstrapFeatures()
    {
        var options = DreamineWpfOptions.CreateDefault();

        Assert.True(options.EnableGlobalAutoWireOnLoaded);
        Assert.True(options.RegisterDefaultServices);
        Assert.True(options.EnableAutoNavigatorRegistration);
        Assert.Equal("SubPage", options.DefaultRegionName);
    }

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

    [Fact]
    public void DesignTimeGuard_IsInDesignModeCanBeRead()
    {
        _ = Dreamine.Hybrid.Wpf.Utility.DesignTimeGuard.IsInDesignMode;
    }

    private sealed class TestMessage : HybridMessageBase
    {
        public TestMessage(int value)
        {
            Value = value;
        }

        public int Value { get; }
    }

    private sealed class DisposableSharedService : IDisposable
    {
        public void Dispose()
        {
        }
    }

    private sealed class TestRootComponent : IComponent
    {
        public void Attach(RenderHandle renderHandle)
        {
        }

        public Task SetParametersAsync(ParameterView parameters)
        {
            return Task.CompletedTask;
        }
    }
}
