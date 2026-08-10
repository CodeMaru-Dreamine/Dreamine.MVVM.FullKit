using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace Dreamine.SecsGem.Interop.Wpf.Behaviors;

public static class DataGridAutoScrollBehavior
{
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled", typeof(bool), typeof(DataGridAutoScrollBehavior), new PropertyMetadata(false, OnIsEnabledChanged));

    public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);
    public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

    private static void OnIsEnabledChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is not DataGrid grid) return;
        grid.Loaded -= OnLoaded;
        grid.Unloaded -= OnUnloaded;
        if ((bool)args.NewValue)
        {
            grid.Loaded += OnLoaded;
            grid.Unloaded += OnUnloaded;
            if (grid.IsLoaded) Subscribe(grid);
        }
        else Unsubscribe(grid);
    }

    private static void OnLoaded(object sender, RoutedEventArgs args) => Subscribe((DataGrid)sender);
    private static void OnUnloaded(object sender, RoutedEventArgs args) => Unsubscribe((DataGrid)sender);

    private static void Subscribe(DataGrid grid)
    {
        Unsubscribe(grid);
        if (grid.ItemsSource is not INotifyCollectionChanged source) return;
        NotifyCollectionChangedEventHandler handler = (_, args) =>
        {
            if (!GetIsEnabled(grid) || args.Action != NotifyCollectionChangedAction.Add || grid.Items.Count == 0) return;
            grid.ScrollIntoView(grid.Items[^1]);
        };
        source.CollectionChanged += handler;
        grid.SetValue(SubscriptionProperty, new Subscription(source, handler));
    }

    private static void Unsubscribe(DataGrid grid)
    {
        if (grid.GetValue(SubscriptionProperty) is not Subscription subscription) return;
        subscription.Source.CollectionChanged -= subscription.Handler;
        grid.ClearValue(SubscriptionProperty);
    }

    private sealed record Subscription(INotifyCollectionChanged Source, NotifyCollectionChangedEventHandler Handler);

    private static readonly DependencyProperty SubscriptionProperty = DependencyProperty.RegisterAttached(
        "Subscription", typeof(Subscription), typeof(DataGridAutoScrollBehavior));
}
