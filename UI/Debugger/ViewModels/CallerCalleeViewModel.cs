using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Selection;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using DataBoxControl;
using Mesen.Config;
using Mesen.Debugger.Labels;
using Mesen.Debugger.Utilities;
using Mesen.Debugger.Windows;
using Mesen.Interop;
using Mesen.Localization;
using Mesen.Utilities;
using Mesen.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Mesen.Debugger.ViewModels
{
	public partial class CallerCalleeViewModel : DisposableViewModel
	{
		public CpuType CpuType { get; }
		public DebuggerWindowViewModel Debugger { get; }

		[ObservableProperty] public partial MesenList<CallerCalleeEntryModel> Callers { get; private set; } = new();
		[ObservableProperty] public partial MesenList<CallerCalleeEntryModel> Callees { get; private set; } = new();
		[ObservableProperty] public partial SelectionModel<CallerCalleeEntryModel?> CallerSelection { get; set; } = new() { SingleSelect = true };
		[ObservableProperty] public partial SelectionModel<CallerCalleeEntryModel?> CalleeSelection { get; set; } = new() { SingleSelect = true };
		[ObservableProperty] public partial string SelectedFunctionName { get; private set; } = "";
		[ObservableProperty] public partial AddressInfo SelectedFunctionAddress { get; private set; }

		public List<int> ColumnWidths { get; } = new() { 50, 70, 70, 40 };
		private CallerCalleeEntryModel? Entry => CallerSelection.SelectedItem ?? CalleeSelection.SelectedItem;

		// private string _format;

		[Obsolete("For designer only")]
		public CallerCalleeViewModel() : this(CpuType.Snes, new()) { }

		public CallerCalleeViewModel(CpuType cpuType, DebuggerWindowViewModel debugger)
		{
			CpuType = cpuType;
			Debugger = debugger;
			// _format = "X" + cpuType.GetAddressSize();
		}

		public void UpdateForFunction(AddressInfo funcAddr, string funcName)
		{
			if(funcAddr.Address < 0) {
				SelectedFunctionName = "";
				SelectedFunctionAddress = default;
				Callers.Replace(new List<CallerCalleeEntryModel>());
				Callees.Replace(new List<CallerCalleeEntryModel>());
				return;
			}

			SelectedFunctionName = funcName;
			SelectedFunctionAddress = funcAddr;

			CallerCalleeRecord record = DebugApi.GetCallerCallee(CpuType, funcAddr);

			List<CallerCalleeEntryModel> callers = new();
			for(int i = 0; i < record.CallerCount && i < 64; i++) {
				CallerCalleeEntry caller = record.Callers[i];
				AddressInfo absAddr = caller.Address;
				if(absAddr.Address >= 0) {
					AddressInfo relAddr = DebugApi.GetRelativeAddress(absAddr, CpuType);
					if(!Debugger.RelAddressDisplayCache.TryGetValue(absAddr, out string? relAddressDisplay) ||
						(relAddressDisplay == ResourceHelper.GetMessage("lblUnavailable") && relAddr.Address >= 0)) {
						relAddressDisplay = relAddr.Address >= 0
							? MemoryHelper.GetAddressStr(relAddr, false, true)
							: ResourceHelper.GetMessage("lblUnavailable");
						Debugger.RelAddressDisplayCache[absAddr] = relAddressDisplay;
					}
					callers.Add(new CallerCalleeEntryModel {
						FuncAbsAddr = absAddr,
						FuncRelAddr = relAddr,
						FunctionName = MemoryHelper.GetFunctionName(absAddr, true, false, false),
						// RelAddress = relAddr.Address,
						RelAddressDisplay = relAddressDisplay,
						AbsAddressDisplay = MemoryHelper.GetAddressStr(absAddr, false, false),
						CallCount = caller.CallCount.ToString(),
						CallCountValue = caller.CallCount,
					});
				}
			}

			List<CallerCalleeEntryModel> callees = new();
			for(int i = 0; i < record.CalleeCount && i < 64; i++) {
				CallerCalleeEntry callee = record.Callees[i];
				AddressInfo absAddr = callee.Address;
				if(absAddr.Address >= 0) {
					AddressInfo relAddr = DebugApi.GetRelativeAddress(absAddr, CpuType);
					if(!Debugger.RelAddressDisplayCache.TryGetValue(absAddr, out string? relAddressDisplay) ||
						(relAddressDisplay == ResourceHelper.GetMessage("lblUnavailable") && relAddr.Address >= 0)) {
						relAddressDisplay = relAddr.Address >= 0
							? MemoryHelper.GetAddressStr(relAddr, false, true)
							: ResourceHelper.GetMessage("lblUnavailable");
						Debugger.RelAddressDisplayCache[absAddr] = relAddressDisplay;
					}
					callees.Add(new CallerCalleeEntryModel {
						FuncAbsAddr = absAddr,
						FuncRelAddr = relAddr,
						FunctionName = MemoryHelper.GetFunctionName(absAddr, true),
						// RelAddress = relAddr.Address,
						RelAddressDisplay = relAddressDisplay,
						AbsAddressDisplay = MemoryHelper.GetAddressStr(absAddr, false, false),
						CallCount = callee.CallCount.ToString(),
						CallCountValue = callee.CallCount
					});
				}
			}

			Callers.Replace(callers);
			Callees.Replace(callees);
		}

		private string GetHintText(bool isAbs = false)
		{
			if(Entry == null) {
				return "";
			}
			if(isAbs) {
				return MemoryHelper.GetAddressStr(Entry.FuncAbsAddr);
			} else if(Entry.FuncRelAddr.Address >= 0) {
				return MemoryHelper.GetAddressStr(Entry.FuncRelAddr);
			}
			return Debugger.RelAddressDisplayCache[Entry.FuncAbsAddr] ?? "";
		}

		public void InitContextMenu(Control parent)
		{
			AddDisposables(DebugShortcutManager.CreateContextMenu(parent, new object[] {
				new ContextMenuAction() {
					ActionType = ActionType.EditLabel,
					HintText = () => GetHintText(),
					IsEnabled = () => Entry != null,
					OnClick = () => {
						if(Entry != null) {
							CodeLabel? label = LabelManager.GetLabel(Entry.FuncAbsAddr);
							LabelEditWindow.EditLabel(CpuType, parent, label ?? new CodeLabel(Entry.FuncAbsAddr));
						}
					}
				},
				new ContextMenuSeparator(),
				new ContextMenuAction() {
					ActionType = ActionType.ToggleBreakpoint,
					HintText = () => GetHintText(),
					IsEnabled = () => Entry?.FuncRelAddr.Address >= 0,
					OnClick = () => {
						if(Entry != null) {
							BreakpointManager.EditBreakpointAtAddress(Entry.FuncRelAddr, CpuType, parent);
						}
					}
				},
				new ContextMenuAction() {
					ActionType = ActionType.ToggleBreakpoint,
					HintText = () => GetHintText(true),
					IsVisible = () => Entry?.FuncRelAddr.Type != Entry?.FuncAbsAddr.Type,
					IsEnabled = () => Entry != null,
					OnClick = () => {
						if(Entry != null) {
							BreakpointManager.EditBreakpointAtAddress(Entry.FuncAbsAddr, CpuType, parent);
						}
					}
				},
				new ContextMenuSeparator(),
				new ContextMenuAction() {
					ActionType = ActionType.FindOccurrences,
					HintText = () => GetHintText(),
					IsEnabled = () => Entry?.FuncRelAddr.Address >= 0,
					OnClick = () => {
						if(Entry != null && Entry.FuncRelAddr.Address >= 0) {
							DisassemblySearchOptions options = new() { MatchCase = true, MatchWholeWord = true };
							Debugger.FindAllOccurrences(MemoryHelper.GetFunctionName(Entry.FuncRelAddr), options);
						}
					}
				},
				new ContextMenuAction() {
					ActionType = ActionType.GoToLocation,
					HintText = () => GetHintText(),
					IsEnabled = () => Entry?.FuncRelAddr.Address >= 0,
					OnClick = () => {
						if(Entry != null && Entry.FuncRelAddr.Address >= 0) {
							Debugger.ScrollToAddress(Entry.FuncRelAddr.Address);
						}
					}
				},
				new ContextMenuAction() {
					ActionType = ActionType.LocateInFunctionList,
					HintText = () => GetHintText(),
					Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.CodeWindow_GoToLocation),
					IsEnabled = () => Entry != null && Debugger.FunctionList != null,
					OnClick = () => {
						if(Entry != null) {
							FunctionListViewModel.ShowInFunctionList(Entry.FuncAbsAddr);
						}
					}
				},				
				new ContextMenuSeparator(),
				new ContextMenuAction() {
					ActionType = ActionType.ViewInMemoryViewer,
					HintText = () => GetHintText(),
					IsEnabled = () => Entry?.FuncRelAddr.Address >= 0,
					OnClick = () => {
						if(Entry != null) {
							MemoryToolsWindow.ShowInMemoryTools(Entry.FuncRelAddr.Type, Entry.FuncRelAddr.Address);
						}
					}
				},
				new ContextMenuAction() {
					ActionType = ActionType.ViewInMemoryViewer,
					HintText = () => GetHintText(true),
					IsVisible = () => Entry?.FuncRelAddr.Type != Entry?.FuncAbsAddr.Type,
					IsEnabled = () => Entry != null,
					OnClick = () => {
						if(Entry != null) {
							MemoryToolsWindow.ShowInMemoryTools(Entry.FuncAbsAddr.Type, Entry.FuncAbsAddr.Address);
						}
					}
				},
			}));
		}
	}

	public class CallerCalleeEntryModel
	{
		public AddressInfo FuncAbsAddr { get; set; }
		public AddressInfo FuncRelAddr { get; set; }
		public string FunctionName { get; set; } = "";
		// public int RelAddress { get; set; }
		public string RelAddressDisplay { get; set; } = "";
		public string AbsAddressDisplay { get; set; } = "";
		public string CallCount { get; set; } = "";
		public UInt64 CallCountValue { get; set; }
	}
}
