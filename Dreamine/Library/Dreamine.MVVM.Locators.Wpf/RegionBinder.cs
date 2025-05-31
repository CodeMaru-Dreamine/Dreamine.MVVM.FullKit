using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Dreamine.MVVM.Locators.Wpf
{
	/// <summary>
	/// 📌 Region 이름을 기준으로 ContentControl을 관리하는 바인더
	/// </summary>
	public static class RegionBinder
	{
		private static readonly Dictionary<string, ContentControl> _regionMap = new();

		public static readonly DependencyProperty RegionNameProperty =
			DependencyProperty.RegisterAttached(
				"RegionName",
				typeof(string),
				typeof(RegionBinder),
				new PropertyMetadata(null, OnRegionNameChanged));

		public static void SetRegionName(DependencyObject obj, string value)
			=> obj.SetValue(RegionNameProperty, value);

		public static string GetRegionName(DependencyObject obj)
			=> (string)obj.GetValue(RegionNameProperty);

		private static void OnRegionNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is ContentControl control && e.NewValue is string name)
			{
				_regionMap[name] = control;
			}
		}

		public static void Navigate(string regionName, object viewModel)
		{
			if (!_regionMap.TryGetValue(regionName, out var control))
				throw new InvalidOperationException($"❌ Region \"{regionName}\" 등록되지 않음.");

			var view = ViewModelBinder.ResolveView(viewModel);
			control.Content = view;
		}
	}
}
