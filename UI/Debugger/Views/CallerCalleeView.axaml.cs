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

		protected override void OnDataContextChanged(EventArgs e)
		{
			if(DataContext is CallerCalleeViewModel model) {
				// Function-level menu (color/block/mark) goes on the Callers/Callees
				// grids only. The access panel gets its own lightweight menu.
				var callers = NameScope.GetNameScope(this)?.Find<DataBox>("callersGrid");
				var callees = NameScope.GetNameScope(this)?.Find<DataBox>("calleesGrid");
				var access = NameScope.GetNameScope(this)?.Find<DataBox>("accessGrid");
				if(callers != null && callees != null) {
					model.CallersGrid = callers;
					model.CalleesGrid = callees;
					model.InitContextMenu(callers, callees);
				}
				if(access != null) {
					model.AccessGrid = access;
					model.InitAccessContextMenu(access);
				}
				var reverse = NameScope.GetNameScope(this)?.Find<DataBox>("reverseGrid");
				if(reverse != null) {
					model.ReverseGrid = reverse;
					model.InitReverseContextMenu(reverse);
				}
			}
			base.OnDataContextChanged(e);
		}

		// Select the access row under the pointer on right-press, so the context
		// menu (opened on release) acts on the correct row. Left-click multi-select
		// is handled by DataBox's SelectionMode="Multiple" — don't override it.
		private void OnAccessPointerPressed(object? sender, PointerPressedEventArgs e)
		{
			if(DataContext is not CallerCalleeViewModel model) return;
			if(!e.GetCurrentPoint(this).Properties.IsRightButtonPressed) return;
			if(e.Source is Visual src) {
				var row = src.GetSelfAndVisualAncestors().OfType<DataBoxRow>().FirstOrDefault();
				if(row?.DataContext is AccessRangeViewModel range)
					model.AccessRangeSelection.SelectedItem = range;
			}
		}

		private void OnCellClick(DataBoxCell cell)
		{
			if(DataContext is not CallerCalleeViewModel model || cell.DataContext is not CallerCalleeEntry entry) {
				return;
			}

			if(cell.Column?.ColumnName == "Marked") {
				bool newValue = !entry.IsMarked;
				// 互斥同步已移入 ViewModel（OnCallerOrCalleeSelectionChanged），
				// 同一时刻只有一个 grid 持有选中项。这里按 entry 所属集合取对应
				// SelectedItems，在 SingleSelect 下最多 1 项，等价于只切换当前行。
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
				if(entry.IsPageInUse) {
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

		private void OnSelectedFuncPointerPressed(object? sender, PointerPressedEventArgs e)
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

		private void OnReverseCellDoubleClick(DataBoxCell cell)
		{
			if(DataContext is not CallerCalleeViewModel model || cell.DataContext is not MemoryAccessFunctionEntry entry) {
				return;
			}
			FunctionListViewModel.ShowInFunctionList(entry.FuncAbsAddr);
		}
	}
}
