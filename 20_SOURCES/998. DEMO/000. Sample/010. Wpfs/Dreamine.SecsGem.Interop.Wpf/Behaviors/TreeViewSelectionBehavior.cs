using System.Windows;
using System.Windows.Controls;

namespace Dreamine.SecsGem.Interop.Wpf.Behaviors;

public static class TreeViewSelectionBehavior
{
    public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.RegisterAttached(
        "SelectedItem", typeof(object), typeof(TreeViewSelectionBehavior),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemChanged));

    public static object? GetSelectedItem(DependencyObject value) => value.GetValue(SelectedItemProperty);
    public static void SetSelectedItem(DependencyObject value, object? item) => value.SetValue(SelectedItemProperty, item);

    private static void OnSelectedItemChanged(DependencyObject value, DependencyPropertyChangedEventArgs eventArgs)
    {
        if (value is not TreeView tree) return;
        tree.SelectedItemChanged -= HandleSelectionChanged;
        tree.SelectedItemChanged += HandleSelectionChanged;
    }

    private static void HandleSelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> eventArgs) =>
        SetSelectedItem((DependencyObject)sender, eventArgs.NewValue);
}
