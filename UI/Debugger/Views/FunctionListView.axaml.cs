using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DataBoxControl;
using Mesen.Debugger.Labels;
using Mesen.Debugger.ViewModels;
using Mesen.Debugger.Windows;
using System;
using System.Linq;
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
				model.Grid = NameScope.GetNameScope(this)?.Find<DataBox>("functionGrid");
				model.InitContextMenu(this);
			}
			base.OnDataContextChanged(e);
		}

		private void OnCellClick(DataBoxCell cell)
		{
			if(DataContext is not FunctionListViewModel model || cell.DataContext is not FunctionNode fv) {
				return;
			}

			if(cell.Column?.ColumnName == "Marked") {
				bool newValue = !fv.IsMarked;
				if(model.Selection.SelectedItems.Contains(fv)) {
					foreach(var s in model.Selection.SelectedItems) {
						if(s != null) {
							s.IsMarked = newValue;
						}
					}
				} else {
					fv.IsMarked = newValue;
				}
			}
		}

		private void OnCellDoubleClick(DataBoxCell cell)
		{
			if(DataContext is not FunctionListViewModel listModel || cell.DataContext is not FunctionNode entry) {
				return;
			}

			string? colName = cell.Column?.ColumnName;
			if(colName == "Function") {
				LabelEditWindow.EditLabel(listModel.CpuType, this, entry.Label ?? new CodeLabel(entry.FuncAbsAddr));
			} else if(colName == "RelAddr") {
				if(entry.IsPageInUse) {
					listModel.Debugger.ScrollToAddress(entry.FuncRelAddr.Address);
				}
			} else if(colName == "AbsAddr") {
				MemoryToolsWindow.ShowInMemoryTools(entry.FuncAbsAddr.Type, entry.FuncAbsAddr.Address);
			}
		}
	}
}
