using Avalonia.Controls;
using Mesen.Interop;
using Mesen.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mesen.Debugger.Utilities
{
	public static class ExpressionTooltipHelper
	{
		public static StackPanel GetHelpTooltip(CpuType cpuType, bool forWatch)
		{
			StackPanel panel = new();

			void addRow(string text) { panel.Children.Add(new TextBlock() { Text = text }); }
			void addBoldRow(string text) { panel.Children.Add(new TextBlock() { Text = text, FontWeight = Avalonia.Media.FontWeight.Bold }); }

			addBoldRow(ResourceHelper.GetMessage("ExprTooltip_Notes"));
			addRow(ResourceHelper.GetMessage("ExprTooltip_SyntaxCpp"));
			addRow(ResourceHelper.GetMessage("ExprTooltip_HexPrefix"));
			addRow(ResourceHelper.GetMessage("ExprTooltip_Labels"));
			addRow(" ");
			addBoldRow(ResourceHelper.GetMessage("ExprTooltip_AvailableValues", ResourceHelper.GetEnumText(cpuType)));

			string[] tokens = DebugApi.GetTokenList(cpuType);

			Grid tokenGrid = new Grid() {
				ColumnDefinitions = new("Auto, Auto, Auto, Auto"),
				RowDefinitions = new(string.Join(",", Enumerable.Repeat("Auto", (tokens.Length / 4) + 1))),
				Margin = new Avalonia.Thickness(5, 0, 0, 0)
			};

			int col = 0;
			int row = 0;
			foreach(string token in tokens) {
				TextBlock txt = new() { Text = token, Padding = new Avalonia.Thickness(0, 0, 5, 0) };
				tokenGrid.Children.Add(txt);
				Grid.SetColumn(txt, col);
				Grid.SetRow(txt, row);
				col++;
				if(col == 4) {
					col = 0;
					row++;
				}
			}

			panel.Children.Add(tokenGrid);

			if(!forWatch) {
				addRow(" ");
				addBoldRow(ResourceHelper.GetMessage("ExprTooltip_OtherValues"));
				addRow(ResourceHelper.GetMessage("ExprTooltip_OpPc"));
				addRow(ResourceHelper.GetMessage("ExprTooltip_Address"));
				addRow(ResourceHelper.GetMessage("ExprTooltip_MemAddress"));
				addRow(ResourceHelper.GetMessage("ExprTooltip_Value"));
				addRow(ResourceHelper.GetMessage("ExprTooltip_IsRead"));
				addRow(ResourceHelper.GetMessage("ExprTooltip_IsWrite"));
				addRow(ResourceHelper.GetMessage("ExprTooltip_IsDma"));
				addRow(ResourceHelper.GetMessage("ExprTooltip_IsDummy"));

				addRow(" ");
				addBoldRow(ResourceHelper.GetMessage("ExprTooltip_AccessCounters"));
				addRow(ResourceHelper.GetMessage("ExprTooltip_ReadCount"));
				addRow(ResourceHelper.GetMessage("ExprTooltip_WriteCount"));
				addRow(ResourceHelper.GetMessage("ExprTooltip_ExecCount"));
				addRow(ResourceHelper.GetMessage("ExprTooltip_LastRead"));
				addRow(ResourceHelper.GetMessage("ExprTooltip_LastWrite"));
				addRow(ResourceHelper.GetMessage("ExprTooltip_LastExec"));
			}

			addRow(" ");
			addBoldRow(ResourceHelper.GetMessage("ExprTooltip_AccessingMemory"));
			addRow(ResourceHelper.GetMessage("ExprTooltip_Memory8Bit"));
			addRow(ResourceHelper.GetMessage("ExprTooltip_Memory16Bit"));
			addRow(ResourceHelper.GetMessage("ExprTooltip_Memory32Bit"));
			addRow(ResourceHelper.GetMessage("ExprTooltip_RomRamAddress"));

			addRow(" ");
			addBoldRow(ResourceHelper.GetMessage("ExprTooltip_Examples"));
			addRow("  a == 10 || x == $23");
			addRow("  scanline == 10 && (cycle >= 55 && cycle <= 100)");
			addRow("  x == [$150] || y == [10]");
			addRow(ResourceHelper.GetMessage("ExprTooltip_Example1"));
			addRow(ResourceHelper.GetMessage("ExprTooltip_Example2"));
			addRow(ResourceHelper.GetMessage("ExprTooltip_Example3"));
			addRow(ResourceHelper.GetMessage("ExprTooltip_Example4"));

			if(!forWatch) {
				addRow("  rc > 100");
				addRow("  wc > 0 && rc == 0");
				addRow("  [$300] == $FF && rc > 10");
			}

			return panel;
		}
	}

	public interface IToolHelpTooltip
	{
		object HelpTooltip { get; }
	}
}
