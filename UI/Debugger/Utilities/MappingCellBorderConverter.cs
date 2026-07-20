using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Mesen.Debugger.Utilities
{
	// Draws slightly heavier borders between 4x4 tile blocks to aid orientation
	// in the 16x16 character mapping grid. Values: (col, row).
	public class MappingCellBorderConverter : IMultiValueConverter
	{
		public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
		{
			int col = 0;
			int row = 0;
			if(values.Count >= 2) {
				if(values[0] is int c) col = c;
				if(values[1] is int r) row = r;
			}

			double right = ((col + 1) % 4 == 0) ? 2 : 1;
			double bottom = ((row + 1) % 4 == 0) ? 2 : 1;
			return new Avalonia.Thickness(1, 1, right, bottom);
		}

		public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}
