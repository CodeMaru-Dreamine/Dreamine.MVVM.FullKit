using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Dreamine.MVVM.Locators.Wpf;

namespace Dreamine.MVVM.Extensions
{
	/// <summary>
	/// RegionBinder에 등록된 RegionName을 기준으로 ContentControl을 찾아주는 헬퍼 클래스입니다.
	/// </summary>
	public static class RegionBinderHelper
	{
		/// <summary>
		/// VisualTree를 탐색하여 RegionName이 일치하는 ContentControl을 찾습니다.
		/// </summary>
		public static ContentControl? FindRegionControl(DependencyObject root, string regionName)
		{
			if (root == null)
				return null;

			int childCount = VisualTreeHelper.GetChildrenCount(root);
			for (int i = 0; i < childCount; i++)
			{
				var child = VisualTreeHelper.GetChild(root, i);

				if (child is ContentControl cc)
				{
					var rn = RegionBinder.GetRegionName(cc);
					if (rn == regionName)
						return cc;
				}

				var result = FindRegionControl(child, regionName);
				if (result != null)
					return result;
			}

			return null;
		}
	}
}
