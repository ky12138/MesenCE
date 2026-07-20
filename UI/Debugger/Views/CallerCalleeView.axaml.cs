using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.VisualTree;
using DataBoxControl;
using Mesen.Debugger.Labels;
using Mesen.Debugger.ViewModels;
using Mesen.Debugger.Windows;
using Mesen.Interop;
// Disambiguate: Mesen.Interop also exposes a CallerCalleeEntry struct used by the
// P/Invoke layer. This file only works with the debugger ViewModel wrapper below.
using CallerCalleeEntry = Mesen.Debugger.ViewModels.CallerCalleeEntry;
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

		// Mouse X-button navigation (back = XButton1, forward = XButton2), mirroring
		// DisassemblyView. Only acts on the X buttons so left/right clicks keep their
		// existing behavior (selection, context menu).
		protected override void OnPointerPressed(PointerPressedEventArgs e)
		{
			base.OnPointerPressed(e);

			if(DataContext is not CallerCalleeViewModel model) {
				return;
			}
			PointerPointProperties props = e.GetCurrentPoint(this).Properties;
			if(props.IsXButton1Pressed) {
				model.GoBack();
			} else if(props.IsXButton2Pressed) {
				model.GoForward();
			}
		}

		protected override void OnDataContextChanged(EventArgs e)
		{
			if(DataContext is CallerCalleeViewModel model) {
				// Function-level menu (color/block/mark) goes on the Callers/Callees
				// grids only. The access panel gets its own lightweight menu.
				var callers = NameScope.GetNameScope(this)?.Find<DataBox>("callersGrid");
				var callees = NameScope.GetNameScope(this)?.Find<DataBox>("calleesGrid");
				var access = NameScope.GetNameScope(this)?.Find<DataBox>("accessGrid");
				if(callers != null && callees != null) {
					model.InitContextMenu(callers, callees);
				}
				if(access != null) {
					model.InitAccessContextMenu(access);
				}
			}
			base.OnDataContextChanged(e);
		}

		private void OnAccessCellClick(DataBoxCell cell)
		{
			if(DataContext is not CallerCalleeViewModel model || cell.DataContext is not AccessRangeViewModel range) {
				return;
			}
			// Track the clicked row so the access-panel context menu acts on it
			// (covers right-click, which otherwise wouldn't change selection).
			model.AccessRangeSelection.SelectedItem = range;
		}

		// Select the access row under the pointer synchronously on right-press, so
		// the context menu (which opens on release) already has a valid target.
		private void OnAccessPointerPressed(object? sender, PointerPressedEventArgs e)
		{
			if(DataContext is not CallerCalleeViewModel model) {
				return;
			}
			if(!e.GetCurrentPoint(this).Properties.IsRightButtonPressed) {
				return;
			}
			if(e.Source is Visual src) {
				DataBoxRow? row = src.GetSelfAndVisualAncestors().OfType<DataBoxRow>().FirstOrDefault();
				if(row?.DataContext is AccessRangeViewModel range) {
					model.AccessRangeSelection.SelectedItem = range;
				}
			}
		}

		private void OnCellClick(DataBoxCell cell)
		{
			if(DataContext is not CallerCalleeViewModel model || cell.DataContext is not CallerCalleeEntry entry) {
				return;
			}

			if(cell.Column?.ColumnName == "Marked") {
				bool newValue = !entry.IsMarked;
				// Only toggle selections within the same grid as the clicked entry.
				// The Caller/Callee grids have independent SelectionModels, so a
				// selection in each would otherwise toggle both at once.
				var sameGridSelection = model.Callers.Contains(entry)
					? model.CallerSelection.SelectedItems
					: model.CalleeSelection.SelectedItems;
				if(sameGridSelection.Contains(entry)) {
					foreach(var s in sameGridSelection) {
						if(s != null) {
							s.IsMarked = newValue;
						}
					}
				} else {
					entry.IsMarked = newValue;
				}
				model.OnMarkedToggled(entry);
			}
		}

		private void OnCallerCellDoubleClick(DataBoxCell cell)
		{
			if(cell.DataContext is not CallerCalleeEntry entry || DataContext is not CallerCalleeViewModel model) {
				return;
			}

			HandleCellDoubleClick(entry, model, cell.Column?.ColumnName);
		}

		private void OnCalleeCellDoubleClick(DataBoxCell cell)
		{
			if(cell.DataContext is not CallerCalleeEntry entry || DataContext is not CallerCalleeViewModel model) {
				return;
			}

			HandleCellDoubleClick(entry, model, cell.Column?.ColumnName);
		}

		private void HandleCellDoubleClick(CallerCalleeEntry entry, CallerCalleeViewModel model, string? colName)
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

		private void OnAccessRangeCellDoubleClick(DataBoxCell cell)
		{
			if(DataContext is not CallerCalleeViewModel model || cell.DataContext is not AccessRangeViewModel range) {
				return;
			}
			// Double-clicking the address (Range) column opens it in the memory
			// tools viewer — for both parent ranges and expanded detail rows.
			// Any other column toggles the drill-down (expand/collapse) instead.
			if(cell.Column?.ColumnName == "Range" && !range.IsExpandable) {
				MemoryToolsWindow.ShowInMemoryTools(range.MemType, (int)range.Start);
			} else {
				model.ToggleAccessRangeExpand(range);
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
