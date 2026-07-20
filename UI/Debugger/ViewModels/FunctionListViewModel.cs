using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Selection;
using Avalonia.Media;
using DataBoxControl;
using Mesen.Config;
using Mesen.Debugger.Labels;
using Mesen.Debugger.Utilities;
using Mesen.Debugger.Windows;
using Mesen.Interop;
using Mesen.Localization;
using Mesen.Utilities;
using Mesen.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Mesen.Debugger.ViewModels
{
	public partial class FunctionListViewModel : DisposableViewModel
	{
		[ObservableProperty] public partial MesenList<FunctionViewModel> Functions { get; private set; } = new();
		[ObservableProperty] public partial SelectionModel<FunctionViewModel?> Selection { get; set; } = new() { SingleSelect = false };
		[ObservableProperty] public partial SortState SortState { get; set; } = new();
		public List<int> ColumnWidths { get; } = ConfigManager.Config.Debug.Debugger.FunctionListColumnWidths;

		public CpuType CpuType { get; }
		public DebuggerWindowViewModel Debugger { get; }

		// [Obsolete("For designer only")]
		// public FunctionListViewModel() : this(CpuType.Snes, new()) { }

		public FunctionListViewModel(CpuType cpuType, DebuggerWindowViewModel debugger)
		{
			CpuType = cpuType;
			Debugger = debugger;

			SortState.SetColumnSort("AbsAddr", ListSortDirection.Ascending, true);
		}

		public static void ShowInFunctionList(AddressInfo addr)
		{
			DebuggerWindow? wnd = DebugWindowManager.GetDebugWindow<DebuggerWindow>(x => x.CpuType == addr.Type.ToCpuType());
			if(wnd?.DataContext is DebuggerWindowViewModel model && model.FunctionList != null) {
				FunctionViewModel? bestMatch = null;
				foreach(var func in model.FunctionList.Functions) {
					if(func.FuncAbsAddr.Type == addr.Type && func.FuncAbsAddr.Address >= 0) {
						int funcLen = (int)func.FunctionLength;
						if(funcLen > 0) {
							if(addr.Address >= func.FuncAbsAddr.Address && addr.Address < func.FuncAbsAddr.Address + funcLen) {
								if(bestMatch == null || func.FuncAbsAddr.Address > bestMatch.FuncAbsAddr.Address) {
									bestMatch = func;
								}
							}
						} else {
							if(func.FuncAbsAddr.Address == addr.Address) {
								bestMatch = func;
								break;
							}
						}
					}
				}
				if(bestMatch != null) {
					model.FunctionList.Selection.SelectedItem = bestMatch;
				}
			}
		}


		public void Sort(object? param)
		{
			UpdateFunctionList();
		}

		private Dictionary<string, Func<FunctionViewModel, FunctionViewModel, int>> _comparers = new() {
			{ "Function", (a, b) => string.Compare(a.LabelName, b.LabelName, StringComparison.OrdinalIgnoreCase) },
			{ "RelAddr", (a, b) => a.FuncRelAddr.Address.CompareTo(b.FuncRelAddr.Address) },
			{ "AbsAddr", (a, b) => a.FuncAbsAddr.Address.CompareTo(b.FuncAbsAddr.Address) },
			{ "FuncLength", (a, b) => a.FunctionLength.CompareTo(b.FunctionLength) },
			{ "ExecCount", (a, b) => a.ExecCountValue.CompareTo(b.ExecCountValue) },
			{ "LastExec", (a, b) => a.LastExecValue.CompareTo(b.LastExecValue) },
		};

		public void UpdateFunctionList()
		{
			Debugger.EnsureCacheLoaded();
			List<int> selectedIndexes = Selection.SelectedIndexes.ToList();

			MemoryType prgMemType = CpuType.GetPrgRomMemoryType();
			var (funcAddresses, funcLengths) = DebugApi.GetCdlFunctionsWithLength(CpuType.GetPrgRomMemoryType());
			List<FunctionViewModel> sortedFunctions = funcAddresses.Select((addr, i) => {
				AddressInfo absAddr = new() { Address = (int)addr, Type = prgMemType };
				AddressInfo relAddr = DebugApi.GetRelativeAddress(absAddr, CpuType);
				if(!Debugger.RelAddressDisplayCache.TryGetValue(absAddr, out string? relAddressDisplay) ||
					(relAddressDisplay == ResourceHelper.GetMessage("lblUnavailable") && relAddr.Address >= 0)) {
					relAddressDisplay = relAddr.Address >= 0
						? MemoryHelper.GetAddressStr(relAddr, false, true)
						: ResourceHelper.GetMessage("lblUnavailable");
					Debugger.RelAddressDisplayCache[absAddr] = relAddressDisplay;
					Debugger.MarkCacheDirty();
				}
				var entry = new FunctionViewModel(absAddr, CpuType, relAddressDisplay);
				if(i < funcLengths.Length) {
					entry.SetFunctionLength(funcLengths[i]);
				}
				return entry;
			}).ToList();

			// Batch-fetch memory access counters for all functions
			int memSize = DebugApi.GetMemorySize(prgMemType);
			if(memSize > 0) {
				AddressCounters[] counters = DebugApi.GetMemoryAccessCounts((uint)0, (uint)memSize, prgMemType);
				UInt64 masterClock = EmuApi.GetTimingInfo(CpuType).MasterClock;

				foreach(FunctionViewModel entry in sortedFunctions) {
					int addr = entry.FuncAbsAddr.Address;
					if(addr >= 0 && addr < counters.Length) {
						entry.SetCounters(counters[addr], masterClock);
					}
				}
			}

			SortHelper.SortList(sortedFunctions, SortState.SortOrder, _comparers, "AbsAddr");

			Functions.Replace(sortedFunctions);
			Selection.SelectIndexes(selectedIndexes, Functions.Count);
		}

		private string GetHintText(bool isAbs = false)
		{
			if(Selection.SelectedItem is not FunctionViewModel entry) {
				return "";
			}
			if(isAbs) {
				return MemoryHelper.GetAddressStr(entry.FuncAbsAddr);
			} else if(entry.FuncRelAddr.Address >= 0) {
				return MemoryHelper.GetAddressStr(entry.FuncRelAddr);
			}
			return "";
		}
		private string GetRangeHintText(bool isAbs = false)
		{
			if(Selection.SelectedItem is not FunctionViewModel entry) {
				return "";
			}
			if(isAbs) {
				return MemoryHelper.GetAddressStr(entry.FuncAbsAddr,entry.FunctionLength);
			} else if(entry.FuncRelAddr.Address >= 0) {
				return MemoryHelper.GetAddressStr(entry.FuncRelAddr,entry.FunctionLength);
			}
			return "";
		}
		public void InitContextMenu(Control parent)
		{
			AddDisposables(DebugShortcutManager.CreateContextMenu(parent, new object[] {
				new ContextMenuAction() {
					ActionType = ActionType.EditLabel,
					HintText = () => GetHintText(),
					Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.FunctionList_EditLabel),
					IsEnabled = () => Selection.SelectedItems.Count == 1,
					OnClick = () => {
						if(Selection.SelectedItem is FunctionViewModel entry) {
							LabelEditWindow.EditLabel(CpuType, parent, entry.Label ?? new CodeLabel(entry.FuncAbsAddr));
						}
					}
				},
				new ContextMenuSeparator(),
				new ContextMenuAction() {
					ActionType = ActionType.ToggleBreakpoint,
					HintText = () => GetHintText(),
					Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.FunctionList_ToggleBreakpoint),
					IsEnabled = () => Selection.SelectedItems.Count == 1,
					OnClick = () => {
						if(Selection.SelectedItem is FunctionViewModel entry) {
							BreakpointManager.EditBreakpointAtRange(entry.FuncRelAddr, entry.FunctionLength, CpuType, parent);
						}
					}
				},
				new ContextMenuAction() {
					ActionType = ActionType.ToggleBreakpoint,
					HintText = () => GetHintText(true),
					Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.FunctionList_ToggleBreakpoint),
					IsEnabled = () => Selection.SelectedItems.Count == 1,
					OnClick = () => {
						if(Selection.SelectedItem is FunctionViewModel entry) {
							BreakpointManager.EditBreakpointAtRange(entry.FuncAbsAddr, entry.FunctionLength, CpuType, parent);
						}
					}
				},
				new ContextMenuSeparator(),
				new ContextMenuAction() {
					ActionType = ActionType.FindOccurrences,
					HintText = () => GetHintText(),
					Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.FunctionList_FindOccurrences),
					IsEnabled = () => Selection.SelectedItems.Count == 1 && Selection.SelectedItem is FunctionViewModel entry && entry.FuncRelAddr.Address >= 0,
					OnClick = () => {
						if(Selection.SelectedItem is FunctionViewModel entry && entry.FuncRelAddr.Address >= 0) {
							DisassemblySearchOptions options = new() { MatchCase = true, MatchWholeWord = true };
							Debugger.FindAllOccurrences(entry.Label?.Label ?? entry.RelAddressDisplay, options);
						}
					}
				},
				new ContextMenuAction() {
					ActionType = ActionType.GoToLocation,
					HintText = () => GetHintText(),
					Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.FunctionList_GoToLocation),
					IsEnabled = () => Selection.SelectedItems.Count == 1 && Selection.SelectedItem is FunctionViewModel entry && entry.FuncRelAddr.Address >= 0,
					OnClick = () => {
						if(Selection.SelectedItem is FunctionViewModel entry) {
							if(entry.FuncRelAddr.Address >= 0) {
								Debugger.ScrollToAddress(entry.FuncRelAddr.Address);
							}
						}
					}
				},
				new ContextMenuAction() {
					ActionType = ActionType.ViewInMemoryViewer,
					HintText = () => GetHintText(),
					Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.FunctionList_ViewInMemoryViewer),
					IsEnabled = () => Selection.SelectedItems.Count == 1,
					OnClick = () => {
						if(Selection.SelectedItem is FunctionViewModel entry) {
							MemoryToolsWindow.ShowInMemoryTools(entry.FuncRelAddr.Type, entry.FuncRelAddr.Address);
						}
					}
				},
				new ContextMenuAction() {
					ActionType = ActionType.ViewInMemoryViewer,
					HintText = () => GetHintText(true),
					Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.FunctionList_ViewInMemoryViewer),
					IsEnabled = () => Selection.SelectedItems.Count == 1,
					OnClick = () => {
						if(Selection.SelectedItem is FunctionViewModel entry) {
							MemoryToolsWindow.ShowInMemoryTools(entry.FuncAbsAddr.Type, entry.FuncRelAddr.Address);
						}
					}
				},
			}));
		}
	}

	public class FunctionViewModel : INotifyPropertyChanged
	{
		private CpuType _cpuType;

		public AddressInfo FuncAbsAddr { get; private set; }
		public AddressInfo FuncRelAddr { get; private set; }

		public string AbsAddressDisplay { get; }
		public string RelAddressDisplay { get; }
		public object RowBrush => FuncRelAddr.Address >= 0 ? AvaloniaProperty.UnsetValue : Brushes.Gray;
		public FontStyle RowStyle => FuncRelAddr.Address >= 0 ? FontStyle.Normal : FontStyle.Italic;

		public CodeLabel? Label => LabelManager.GetLabel(FuncAbsAddr);
		public string LabelName => Label?.Label ?? ResourceHelper.GetMessage("lblNoLabel");

		public string ExecCount { get; private set; }
		public string LastExec { get; private set; }
		public UInt64 ExecCountValue { get; private set; }
		public UInt64 LastExecValue { get; private set; }

		public UInt32 FunctionLength { get; private set; }
		public string FunctionLengthDisplay { get; private set; }

		public event PropertyChangedEventHandler? PropertyChanged;

		public void Refresh()
		{
			AddressInfo addr = DebugApi.GetRelativeAddress(FuncAbsAddr, _cpuType);
			if(addr.Address != FuncRelAddr.Address) {
				FuncRelAddr = addr;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RowBrush)));
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RowStyle)));
			}

			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LabelName)));
		}

		public void SetCounters(AddressCounters counters, UInt64 masterClock)
		{
			ExecCount = CodeTooltipHelper.FormatCount(counters.ExecCounter);
			ExecCountValue = counters.ExecCounter;

			if(counters.ExecStamp == 0) {
				LastExec = "n/a";
				LastExecValue = 0;
			} else {
				LastExecValue = masterClock - counters.ExecStamp;
				if(ConfigManager.Config.Debug.Debugger.ShowLastExecTimeInSeconds) {
					TimingInfo timing = EmuApi.GetTimingInfo(_cpuType);
					double seconds = (double)LastExecValue / timing.MasterClockRate;
					LastExec = seconds.ToString("0.###") + "s";
				} else {
					LastExec = CodeTooltipHelper.FormatCount(LastExecValue);
				}
			}

			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ExecCount)));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LastExec)));
		}

		public void SetFunctionLength(UInt32 length)
		{
			FunctionLength = length;
			FunctionLengthDisplay = length.ToString();
		}

		public FunctionViewModel(AddressInfo funcAddr, CpuType cpuType, string relAddressDisplay)
		{
			FuncAbsAddr = funcAddr;
			_cpuType = cpuType;
			FuncRelAddr = DebugApi.GetRelativeAddress(FuncAbsAddr, _cpuType);
			AbsAddressDisplay = MemoryHelper.GetAddressStr(FuncAbsAddr, false, false);
			RelAddressDisplay = relAddressDisplay;

			ExecCount = "";
			LastExec = "";
			FunctionLength = 0;
			FunctionLengthDisplay = "";
		}
	}
}
