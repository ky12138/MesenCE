using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DataBoxControl;
using Mesen.Debugger.Labels;
using Mesen.Debugger.ViewModels;
using Mesen.Debugger.Windows;
using System;
using static Mesen.Debugger.ViewModels.FunctionListViewModel;

namespace Mesen.Debugger.Views
{
	public class FunctionListView : UserControl
	{
		public FunctionListView()
		{
			InitializeComponent();
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
		}

		protected override void OnDataContextChanged(EventArgs e)
		{
			if(DataContext is FunctionListViewModel model) {
				model.InitContextMenu(this);
			}
			base.OnDataContextChanged(e);
		}

		private void OnCellDoubleClick(DataBoxCell cell)
		{
			if(DataContext is not FunctionListViewModel listModel || cell.DataContext is not FunctionViewModel entry) {
				return;
			}

			string? colName = cell.Column?.ColumnName;
			if(colName == "Function") {
				LabelEditWindow.EditLabel(listModel.CpuType, this, entry.Label ?? new CodeLabel(entry.FuncAbsAddr));
			} else if(colName == "RelAddr") {
				if(entry.FuncRelAddr.Address >= 0) {
					listModel.Debugger.ScrollToAddress(entry.FuncRelAddr.Address);
				}
			} else if(colName == "AbsAddr") {
				MemoryToolsWindow.ShowInMemoryTools(entry.FuncAbsAddr.Type, entry.FuncAbsAddr.Address);
			}
		}
	}
}
