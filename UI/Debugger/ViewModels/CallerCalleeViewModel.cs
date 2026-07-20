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

		private string _format;

		[Obsolete("For designer only")]
		public CallerCalleeViewModel() : this(CpuType.Snes, new()) { }

		public CallerCalleeViewModel(CpuType cpuType, DebuggerWindowViewModel debugger)
		{
			CpuType = cpuType;
			Debugger = debugger;
			_format = "X" + cpuType.GetAddressSize();
		}

		public void UpdateForFunction(AddressInfo funcAddr)
		{
			if(funcAddr.Address < 0) {
				SelectedFunctionName = "";
				SelectedFunctionAddress = default;
				Callers.Replace(new List<CallerCalleeEntryModel>());
				Callees.Replace(new List<CallerCalleeEntryModel>());
				return;
			}

			CodeLabel? label = LabelManager.GetLabel(funcAddr);
			string format = "X" + CpuType.GetAddressSize();
			SelectedFunctionName = label != null
				? label.Label + " ($" + funcAddr.Address.ToString(format) + ")"
				: "$" + funcAddr.Address.ToString(format);
			SelectedFunctionAddress = funcAddr;

			CallerCalleeRecord record = DebugApi.GetCallerCallee(CpuType, funcAddr);

			List<CallerCalleeEntryModel> callers = new();
			for(int i = 0; i < record.CallerCount && i < 64; i++) {
				CallerCalleeEntry caller = record.Callers[i];
				if(caller.Address.Address >= 0) {
					AddressInfo relAddr = DebugApi.GetRelativeAddress(caller.Address, CpuType);
					callers.Add(new CallerCalleeEntryModel {
						FuncAbsAddr = caller.Address,
						FunctionName = GetFunctionName(caller.Address),
						FuncRelAddr = relAddr,
						RelAddress = relAddr.Address,
						RelAddressDisplay = relAddr.Address >= 0 ? "$" + relAddr.Address.ToString(_format) : ResourceHelper.GetMessage("lblUnavailable"),
						AbsAddressDisplay = "$" + caller.Address.Address.ToString(_format),
						CallCount = caller.CallCount.ToString(),
						CallCountValue = caller.CallCount,
					});
				}
			}

			List<CallerCalleeEntryModel> callees = new();
			for(int i = 0; i < record.CalleeCount && i < 64; i++) {
				CallerCalleeEntry callee = record.Callees[i];
				if(callee.Address.Address >= 0) {
					AddressInfo relAddr = DebugApi.GetRelativeAddress(callee.Address, CpuType);
					callees.Add(new CallerCalleeEntryModel {
						FuncAbsAddr = callee.Address,
						FunctionName = GetFunctionName(callee.Address),
						FuncRelAddr = relAddr,
						RelAddress = relAddr.Address,
						RelAddressDisplay = relAddr.Address >= 0 ? "$" + relAddr.Address.ToString(_format) : ResourceHelper.GetMessage("lblUnavailable"),
						AbsAddressDisplay = "$" + callee.Address.Address.ToString(_format),
						CallCount = callee.CallCount.ToString(),
						CallCountValue = callee.CallCount
					});
				}
			}

			Callers.Replace(callers);
			Callees.Replace(callees);
		}

		private string GetFunctionName(AddressInfo addr)
		{
			CodeLabel? label = LabelManager.GetLabel(addr);
			return label?.Label ?? ResourceHelper.GetMessage("lblNoLabel");
		}

		private bool IsAbs() {
			if(Entry != null && Entry.AbsAddressDisplay != Entry.RelAddressDisplay) {
				return true;
			}
			return false;
		}

		private string GetHint(bool isAbs = false)
		{
			if(Entry == null) {
				return string.Empty;
			}
			
			if(isAbs) {
				return Entry.AbsAddressDisplay + " [" + Entry.FuncAbsAddr.Type.GetShortName() + "]";
			} else if(Entry.RelAddress >= 0) {
				AddressInfo relAddr = DebugApi.GetRelativeAddress(Entry.FuncAbsAddr, CpuType);
				return Entry.RelAddressDisplay + " [" + relAddr.Type.GetShortName() + "]";
			}
			return string.Empty;
		}

		public void InitContextMenu(Control parent)
		{
			AddDisposables(DebugShortcutManager.CreateContextMenu(parent, new object[] {
				new ContextMenuAction() {
					ActionType = ActionType.EditLabel,
					HintText = () => GetHint(),
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
					HintText = () => GetHint(),
					IsEnabled = () => Entry != null,
					OnClick = () => {
						if(Entry != null) {
							BreakpointManager.EditBreakpointAtAddress(Entry.FuncRelAddr, CpuType, parent);
						}
					}
				},
				new ContextMenuAction() {
					ActionType = ActionType.ToggleBreakpoint,
					HintText = () => GetHint(true),
					IsVisible = () => IsAbs(),
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
					HintText = () => GetHint(),
					IsEnabled = () => Entry?.RelAddress >= 0,
					OnClick = () => {
						if(Entry != null && Entry.RelAddress >= 0) {
							DisassemblySearchOptions options = new() { MatchCase = true, MatchWholeWord = true };
							Debugger.FindAllOccurrences(Entry.FunctionName ?? Entry.RelAddressDisplay, options);
						}
					}
				},

				new ContextMenuAction() {
					ActionType = ActionType.GoToLocation,
					HintText = () => GetHint(),
					IsEnabled = () => Entry?.RelAddress >= 0,
					OnClick = () => {
						if(Entry != null && Entry.RelAddress >= 0) {
							Debugger.ScrollToAddress(Entry.RelAddress);
						}
					}
				},

				new ContextMenuAction() {
					ActionType = ActionType.LocateInFunctionList,
					HintText = () => GetHint(),
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
					HintText = () => GetHint(),
					IsEnabled = () => Entry != null,
					OnClick = () => {
						if(Entry != null) {
							MemoryToolsWindow.ShowInMemoryTools(Entry.FuncRelAddr.Type, Entry.FuncRelAddr.Address);
						}
					}
				},
				new ContextMenuAction() {
					ActionType = ActionType.ViewInMemoryViewer,
					HintText = () => GetHint(true),
					IsVisible = () => IsAbs(),
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
		public string FunctionName { get; set; } = "";
		public AddressInfo FuncRelAddr { get; set; }
		public int RelAddress { get; set; }
		public string RelAddressDisplay { get; set; } = "";
		public string AbsAddressDisplay { get; set; } = "";
		public string CallCount { get; set; } = "";
		public UInt64 CallCountValue { get; set; }
	}
}
