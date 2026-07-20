using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DataBoxControl;
using Mesen.Debugger.Labels;
using Mesen.Debugger.ViewModels;
using Mesen.Debugger.Windows;
using Mesen.Interop;
using System;
using System.Linq;

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

			HandleCellDoubleClick(entry, model, cell.Column?.ColumnName);
		}

		private void OnCalleeCellDoubleClick(DataBoxCell cell)
		{
			if(cell.DataContext is not CallerCalleeEntryModel entry || DataContext is not CallerCalleeViewModel model) {
				return;
			}

			HandleCellDoubleClick(entry, model, cell.Column?.ColumnName);
		}

		private void HandleCellDoubleClick(CallerCalleeEntryModel entry, CallerCalleeViewModel model, string? colName)
		{
			if(colName == "Function") {
				CodeLabel? label = LabelManager.GetLabel(entry.FuncAbsAddr);
				LabelEditWindow.EditLabel(model.CpuType, this, label ?? new CodeLabel(entry.FuncAbsAddr));
			} else if(colName == "RelAddr") {
				if(entry.FuncRelAddr.Address >= 0) {
					model.Debugger.ScrollToAddress(entry.FuncRelAddr.Address);
				}
			} else if(colName == "AbsAddr") {
				MemoryToolsWindow.ShowInMemoryTools(entry.FuncAbsAddr.Type, entry.FuncAbsAddr.Address);
			} else {				
				FunctionListViewModel.ShowInFunctionList(entry.FuncAbsAddr);
			}
		}

		private void OnSelectedFunctionPointerPressed(object? sender, PointerPressedEventArgs e)
		{
			if(DataContext is not CallerCalleeViewModel model) {
				return;
			}

			AddressInfo funcAddr = model.SelectedFunctionAddress;
			if(funcAddr.Address < 0) {
				return;
			}

			if(e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) {
				int relAddr = DebugApi.GetRelativeAddress(funcAddr, model.CpuType).Address;
				if(relAddr >= 0) {
					model.Debugger.ScrollToAddress(relAddr);
				} else {
					MemoryToolsWindow.ShowInMemoryTools(funcAddr.Type, funcAddr.Address);
				}
			}
		}
	}
}
