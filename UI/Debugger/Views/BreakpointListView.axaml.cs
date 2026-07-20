using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DataBoxControl;
using Mesen.Debugger;
using Mesen.Debugger.Utilities;
using Mesen.Debugger.ViewModels;
using Mesen.Debugger.Windows;
using Mesen.Interop;
using System;
using System.Linq;
using static Mesen.Debugger.ViewModels.BreakpointListViewModel;

namespace Mesen.Debugger.Views
{
	public class BreakpointListView : UserControl
	{
		public BreakpointListView()
		{
			InitializeComponent();
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
		}

		protected override void OnDataContextChanged(EventArgs e)
		{
			if(DataContext is BreakpointListViewModel vm) {
				vm.InitContextMenu(this);
			}
			base.OnDataContextChanged(e);
		}

		private void OnCellClick(DataBoxCell cell)
		{
			if(DataContext is BreakpointListViewModel bpList && cell.DataContext is BreakpointViewModel) {
				string? columnName = cell.Column?.ColumnName ?? "";
				if(columnName == "Enabled" || columnName == "Marked" || columnName == "Read" || columnName == "Write" || columnName == "Exec") {
					bool newValue = !bpList.Selection.SelectedItems.Any(bp => {
						if(bp == null) return false;
						return columnName switch {
							"Enabled" => bp.Breakpoint.Enabled,
							"Marked" => bp.Breakpoint.MarkEvent,
							"Read" => bp.Breakpoint.BreakOnRead,
							"Write" => bp.Breakpoint.BreakOnWrite,
							"Exec" => bp.Breakpoint.BreakOnExec,
							_ => false,
						};
					});

					foreach(BreakpointViewModel? bp in bpList.Selection.SelectedItems) {
						if(bp != null && !bp.Breakpoint.Forbid) {
							switch(columnName) {
								case "Enabled": bp.Breakpoint.Enabled = newValue; break;
								case "Marked": bp.Breakpoint.MarkEvent = newValue; break;
								case "Read": bp.Breakpoint.BreakOnRead = newValue; break;
								case "Write": bp.Breakpoint.BreakOnWrite = newValue; break;
								case "Exec": bp.Breakpoint.BreakOnExec = newValue; break;
							}
						}
					}

					DebugWorkspaceManager.AutoSave();
					BreakpointManager.RefreshBreakpoints();
				}
			}
		}

		private void OnCellDoubleClick(DataBoxCell cell)
		{
			if(cell.DataContext is not BreakpointViewModel vm) {
				return;
			}

			string? colName = cell.Column?.ColumnName;
			if(colName == "Address") {
				if(vm.Breakpoint.SupportsExec) {
					int addr = vm.Breakpoint.GetRelativeAddress();
					if(addr >= 0 && DataContext is BreakpointListViewModel listModel) {
						listModel.Debugger.ScrollToAddress(addr);
						return;
					}
				}
				MemoryToolsWindow.ShowInMemoryTools(vm.Breakpoint.MemoryType, (int)vm.Breakpoint.StartAddress);
			} else {
				BreakpointEditWindow.EditBreakpoint(vm.Breakpoint, this);
			}
		}
	}
}
