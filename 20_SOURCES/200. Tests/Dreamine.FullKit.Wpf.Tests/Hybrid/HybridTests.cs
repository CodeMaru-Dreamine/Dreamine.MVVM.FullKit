using System.Globalization;
using System.Windows;
using Dreamine.Hybrid.Messaging;
using Dreamine.Hybrid.State;
using Dreamine.Hybrid.Wpf.Converters;
using Dreamine.MVVM.Wpf;

namespace Dreamine.FullKit.Wpf.Tests.Hybrid;

public sealed class HybridTests
{
    [Fact]
    public async Task InMemoryHybridMessageBus_PublishesAndUnsubscribesByMessageType()
    {
        var bus = new InMemoryHybridMessageBus();
        var received = new List<int>();

        using var subscription = bus.Subscribe<CounterChangedMessage>((message, _) =>
        {
            received.Add(message.Count);
            return Task.CompletedTask;
        });

        await bus.PublishAsync(new CounterChangedMessage(3));
        subscription.Dispose();
        await bus.PublishAsync(new CounterChangedMessage(4));

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
}
