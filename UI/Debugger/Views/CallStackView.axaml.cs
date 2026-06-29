using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DataBoxControl;
using Mesen.Debugger.ViewModels;
using Mesen.Debugger.Windows;
using System;

namespace Mesen.Debugger.Views
{
	public class CallStackView : UserControl
	{
		public CallStackView()
		{
			InitializeComponent();
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
		}

		protected override void OnDataContextChanged(EventArgs e)
		{
			if(DataContext is CallStackViewModel model) {
				model.InitContextMenu(this);
			}
			base.OnDataContextChanged(e);
		}

		private void OnCellDoubleClick(DataBoxCell cell)
		{
			if(cell.DataContext is not StackInfo stack || DataContext is not CallStackViewModel model) {
				return;
			}

			string? colName = cell.Column?.ColumnName;
			if(colName == "Function" || colName == "PcAddress") {
				model.GoToLocation(stack);
			} else if(colName == "RomAddress" && stack.Address.Address >= 0) {
				MemoryToolsWindow.ShowInMemoryTools(stack.Address.Type, stack.Address.Address);
			}
		}
	}
}
