using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DataBoxControl;
using Mesen.Debugger.ViewModels;
using Mesen.Debugger.Windows;
using Mesen.Interop;
using System;

namespace Mesen.Debugger.Views
{
	public class CallerCalleeView : UserControl
	{
		public CallerCalleeView()
		{
			InitializeComponent();
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
		}

		protected override void OnDataContextChanged(EventArgs e)
		{
			if(DataContext is CallerCalleeViewModel model) {
				model.InitContextMenu(this);
			}
			base.OnDataContextChanged(e);
		}

		private void OnCallerCellDoubleClick(DataBoxCell cell)
		{
			if(cell.DataContext is not CallerCalleeEntryModel entry || DataContext is not CallerCalleeViewModel model) {
				return;
			}

			string? colName = cell.Column?.ColumnName;
			if(colName == "Function" || colName == "Address") {
				if(entry.FuncAddr.Address >= 0) {
					int relAddr = DebugApi.GetRelativeAddress(entry.FuncAddr, model.CpuType).Address;
					if(relAddr >= 0) {
						model.Debugger.ScrollToAddress(relAddr);
					}
				}
			}
		}

		private void OnCalleeCellDoubleClick(DataBoxCell cell)
		{
			if(cell.DataContext is not CallerCalleeEntryModel entry || DataContext is not CallerCalleeViewModel model) {
				return;
			}

			string? colName = cell.Column?.ColumnName;
			if(colName == "Function" || colName == "Address") {
				if(entry.FuncAddr.Address >= 0) {
					int relAddr = DebugApi.GetRelativeAddress(entry.FuncAddr, model.CpuType).Address;
					if(relAddr >= 0) {
						model.Debugger.ScrollToAddress(relAddr);
					}
				}
			}
		}
	}
}
