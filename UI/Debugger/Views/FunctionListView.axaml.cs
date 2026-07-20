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
			if(DataContext is not FunctionListViewModel listModel || cell.DataContext is not FunctionViewModel vm) {
				return;
			}

			string? colName = cell.Column?.ColumnName;
			if(colName == "Function") {
				CodeLabel? label = vm.Label;
				if(label == null) {
					label = new CodeLabel(vm.FuncAddr);
				}
				LabelEditWindow.EditLabel(listModel.CpuType, this, label);
			} else if(colName == "RelAddr") {
				if(vm.RelAddress >= 0) {
					listModel.Debugger.ScrollToAddress(vm.RelAddress);
				}
			} else if(colName == "AbsAddr") {
				MemoryToolsWindow.ShowInMemoryTools(vm.FuncAddr.Type, vm.FuncAddr.Address);
			}
		}
	}
}
