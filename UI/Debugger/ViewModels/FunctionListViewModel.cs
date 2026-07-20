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

		[Obsolete("For designer only")]
		public FunctionListViewModel() : this(CpuType.Snes, new()) { }

		public FunctionListViewModel(CpuType cpuType, DebuggerWindowViewModel debugger)
		{
			CpuType = cpuType;
			Debugger = debugger;

			SortState.SetColumnSort("AbsAddr", ListSortDirection.Ascending, true);
		}

		public static void ShowInFunctionList(MemoryType memType, int address)
		{
			DebuggerWindow? wnd = DebugWindowManager.GetDebugWindow<DebuggerWindow>(x => x.CpuType == memType.ToCpuType());
			if(wnd?.DataContext is DebuggerWindowViewModel model && model.FunctionList != null) {
				FunctionViewModel? target = model.FunctionList.Functions
					.FirstOrDefault(f => f.FuncAddr.Address == address && f.FuncAddr.Type == memType);
				if(target != null) {
					model.FunctionList.Selection.SelectedItem = target;
				}
			}
		}

		public static void ShowInFunctionList(AddressInfo addr)
		{
			ShowInFunctionList(addr.Type, addr.Address);
		}

		public static void ShowInFunctionListContaining(CpuType cpuType, int relAddress)
		{
			DebuggerWindow? wnd = DebugWindowManager.GetDebugWindow<DebuggerWindow>(x => x.CpuType == cpuType);
			if(wnd?.DataContext is DebuggerWindowViewModel model && model.FunctionList != null) {
				FunctionViewModel? bestMatch = null;
				foreach(var func in model.FunctionList.Functions) {
					if(func.RelAddress >= 0 && func.RelAddress <= relAddress) {
						if(bestMatch == null || func.RelAddress > bestMatch.RelAddress) {
							bestMatch = func;
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
			{ "RelAddr", (a, b) => a.RelAddress.CompareTo(b.RelAddress) },
			{ "AbsAddr", (a, b) => a.AbsAddress.CompareTo(b.AbsAddress) },
			{ "ExecCount", (a, b) => a.ExecCountValue.CompareTo(b.ExecCountValue) },
			{ "LastExec", (a, b) => a.LastExecValue.CompareTo(b.LastExecValue) },
		};

		public void UpdateFunctionList()
		{
			List<int> selectedIndexes = Selection.SelectedIndexes.ToList();

			MemoryType prgMemType = CpuType.GetPrgRomMemoryType();
			List<FunctionViewModel> sortedFunctions = DebugApi.GetCdlFunctions(CpuType.GetPrgRomMemoryType()).Select(f => new FunctionViewModel(new AddressInfo() { Address = (int)f, Type = prgMemType }, CpuType)).ToList();

			// Batch-fetch memory access counters for all functions
			int memSize = DebugApi.GetMemorySize(prgMemType);
			if(memSize > 0) {
				AddressCounters[] counters = DebugApi.GetMemoryAccessCounts((uint)0, (uint)memSize, prgMemType);
				UInt64 masterClock = EmuApi.GetTimingInfo(CpuType).MasterClock;

				foreach(FunctionViewModel vm in sortedFunctions) {
					int addr = vm.AbsAddress;
					if(addr >= 0 && addr < counters.Length) {
						vm.SetCounters(counters[addr], masterClock);
					}
				}
			}

			SortHelper.SortList(sortedFunctions, SortState.SortOrder, _comparers, "AbsAddr");

			Functions.Replace(sortedFunctions);
			Selection.SelectIndexes(selectedIndexes, Functions.Count);
		}

		public void InitContextMenu(Control parent)
		{
			AddDisposables(DebugShortcutManager.CreateContextMenu(parent, new object[] {
				new ContextMenuAction() {
					ActionType = ActionType.EditLabel,
					Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.FunctionList_EditLabel),
					IsEnabled = () => Selection.SelectedItems.Count == 1,
					OnClick = () => {
						if(Selection.SelectedItem is FunctionViewModel vm) {
							LabelEditWindow.EditLabel(CpuType, parent, vm.Label ?? new CodeLabel(vm.FuncAddr));
						}
					}
				},

				new ContextMenuSeparator(),

				new ContextMenuAction() {
					ActionType = ActionType.ToggleBreakpoint,
					Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.FunctionList_ToggleBreakpoint),
					IsEnabled = () => Selection.SelectedItems.Count == 1,
					OnClick = () => {
						if(Selection.SelectedItem is FunctionViewModel vm) {
							BreakpointManager.EditBreakpointAtAddress(vm.FuncAddr, CpuType, parent);
						}
					}
				},

				new ContextMenuSeparator(),

				new ContextMenuAction() {
					ActionType = ActionType.FindOccurrences,
					Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.FunctionList_FindOccurrences),
					IsEnabled = () => Selection.SelectedItems.Count == 1 && Selection.SelectedItem is FunctionViewModel vm && vm.RelAddress >= 0,
					OnClick = () => {
						if(Selection.SelectedItem is FunctionViewModel vm && vm.RelAddress >= 0) {
							DisassemblySearchOptions options = new() { MatchCase = true, MatchWholeWord = true };
							Debugger.FindAllOccurrences(vm.Label?.Label ?? vm.RelAddressDisplay, options);
						}
					}
				},

				new ContextMenuAction() {
					ActionType = ActionType.GoToLocation,
					Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.FunctionList_GoToLocation),
					IsEnabled = () => Selection.SelectedItems.Count == 1 && Selection.SelectedItem is FunctionViewModel vm && vm.RelAddress >= 0,
					OnClick = () => {
						if(Selection.SelectedItem is FunctionViewModel vm) {
							if(vm.RelAddress >= 0) {
								Debugger.ScrollToAddress(vm.RelAddress);
							}
						}
					}
				},

				new ContextMenuAction() {
					ActionType = ActionType.ViewInMemoryViewer,
					Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.FunctionList_ViewInMemoryViewer),
					IsEnabled = () => Selection.SelectedItems.Count == 1,
					OnClick = () => {
						if(Selection.SelectedItem is FunctionViewModel vm) {
							AddressInfo addr = new AddressInfo() { Address = vm.RelAddress, Type = CpuType.ToMemoryType() };
							if(addr.Address < 0) {
								addr = vm.FuncAddr;
							}
							MemoryToolsWindow.ShowInMemoryTools(addr.Type, addr.Address);
						}
					}
				},
			}));
		}
	}

	public class FunctionViewModel : INotifyPropertyChanged
	{
		private string _format;
		private CpuType _cpuType;

		public AddressInfo FuncAddr { get; private set; }

		public string AbsAddressDisplay { get; }
		public int AbsAddress => FuncAddr.Address;
		public int RelAddress { get; private set; }
		public string RelAddressDisplay => RelAddress >= 0 ? ("$" + RelAddress.ToString(_format)) : ResourceHelper.GetMessage("lblUnavailable");
		public object RowBrush => RelAddress >= 0 ? AvaloniaProperty.UnsetValue : Brushes.Gray;
		public FontStyle RowStyle => RelAddress >= 0 ? FontStyle.Normal : FontStyle.Italic;

		public CodeLabel? Label => LabelManager.GetLabel(FuncAddr);
		public string LabelName => Label?.Label ?? ResourceHelper.GetMessage("lblNoLabel");

		public string ExecCount { get; private set; }
		public string LastExec { get; private set; }
		public UInt64 ExecCountValue { get; private set; }
		public UInt64 LastExecValue { get; private set; }

		public event PropertyChangedEventHandler? PropertyChanged;

		public void Refresh()
		{
			int addr = DebugApi.GetRelativeAddress(FuncAddr, _cpuType).Address;
			if(addr != RelAddress) {
				RelAddress = addr;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RowBrush)));
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RowStyle)));
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RelAddressDisplay)));
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

		public FunctionViewModel(AddressInfo funcAddr, CpuType cpuType)
		{
			FuncAddr = funcAddr;
			_cpuType = cpuType;
			RelAddress = DebugApi.GetRelativeAddress(FuncAddr, _cpuType).Address;
			_format = "X" + cpuType.GetAddressSize();

			AbsAddressDisplay = "$" + FuncAddr.Address.ToString(_format);

			ExecCount = "";
			LastExec = "";
		}
	}
}
