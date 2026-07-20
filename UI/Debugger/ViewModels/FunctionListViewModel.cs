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
using Mesen.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mesen.Debugger.ViewModels
{
	public partial class FunctionListViewModel : DisposableViewModel
	{
		[ObservableProperty] public partial MesenList<FunctionNode> Functions { get; private set; } = new();
		[ObservableProperty] public partial SelectionModel<FunctionNode?> Selection { get; set; } = new() { SingleSelect = false };
		[ObservableProperty] public partial SortState SortState { get; set; } = new();
		[ObservableProperty] public partial bool ShowBlocked { get; set; } = ConfigManager.Config.Debug.Debugger.ShowBlockedFunctions;

		public List<int> ColumnWidths { get; } = ConfigManager.Config.Debug.Debugger.FunctionListColumnWidths;
		public CpuType CpuType { get; }
		public DebuggerWindowViewModel Debugger { get; }

		// Canonical nodes keyed by absolute address; reused across rebuilds so the
		// Caller/Callee panel shares the same instance and selection stays stable.
		private Dictionary<AddressInfo, FunctionNode> _index = new();

		internal static readonly (string Key, string Hex)[] ColorPalette = new[] {
			("colColorRed", "#E53935"), ("colColorGreen", "#43A047"), ("colColorBlue", "#1E88E5"),
			("colColorYellow", "#FDD835"), ("colColorOrange", "#FB8C00"), ("colColorPurple", "#8E24AA"),
			("colColorCyan", "#00ACC1"), ("colColorGray", "#757575")
		};

		private readonly Dictionary<string, Func<FunctionNode, FunctionNode, int>> _comparers = new() {
			{ "Function", (a, b) => string.Compare(a.FunctionName, b.FunctionName, StringComparison.OrdinalIgnoreCase) },
			{ "RelAddr", (a, b) => a.FuncRelAddr.Address.CompareTo(b.FuncRelAddr.Address) },
			{ "AbsAddr", (a, b) => a.FuncAbsAddr.Address.CompareTo(b.FuncAbsAddr.Address) },
			{ "FuncLength", (a, b) => a.FunctionLength.CompareTo(b.FunctionLength) },
			{ "ExecCount", (a, b) => a.ExecCountValue.CompareTo(b.ExecCountValue) },
			{ "LastExec", (a, b) => a.LastExecValue.CompareTo(b.LastExecValue) },
		};

		partial void OnShowBlockedChanged(bool value)
		{
			ConfigManager.Config.Debug.Debugger.ShowBlockedFunctions = value;
			UpdateFunctionList();
		}

		public FunctionListViewModel(CpuType cpuType, DebuggerWindowViewModel debugger)
		{
			CpuType = cpuType;
			Debugger = debugger;
			SortState.SetColumnSort("AbsAddr", ListSortDirection.Ascending, true);
		}

		public void Sort(object? param) => UpdateFunctionList();

		public static void ShowInFunctionList(AddressInfo addr)
		{
			var wnd = DebugWindowManager.GetDebugWindow<DebuggerWindow>(x => x.CpuType == addr.Type.ToCpuType());
			if(wnd?.DataContext is DebuggerWindowViewModel model && model.FunctionList != null) {
				FunctionNode? bestMatch = null;
				foreach(var func in model.FunctionList.Functions) {
					if(func.FuncAbsAddr.Type == addr.Type && func.FuncAbsAddr.Address >= 0) {
						int len = (int)func.FunctionLength;
						if(len > 0) {
							if(addr.Address >= func.FuncAbsAddr.Address && addr.Address < func.FuncAbsAddr.Address + len) {
								if(bestMatch == null || func.FuncAbsAddr.Address > bestMatch.FuncAbsAddr.Address)
									bestMatch = func;
							}
						} else if(func.FuncAbsAddr.Address == addr.Address) {
							bestMatch = func; break;
						}
					}
				}
				if(bestMatch != null) model.FunctionList.Selection.SelectedItem = bestMatch;
			}
		}

		public void UpdateFunctionList()
		{
			Debugger.EnsureCacheLoaded();
			Debugger.RefreshUsedPages();

			List<int> selectedIndexes = Selection.SelectedIndexes.ToList();
			MemoryType prgMemType = CpuType.GetPrgRomMemoryType();
			var (funcAddresses, funcLengths) = DebugApi.GetCdlFunctionsWithLength(prgMemType);

			List<FunctionNode> sortedFunctions = funcAddresses.Select((addr, i) => {
				AddressInfo absAddr = new() { Address = (int)addr, Type = prgMemType };
				if(!_index.TryGetValue(absAddr, out FunctionNode? node)) {
					string display = Debugger.GetOrUpdateRelAddressDisplay(absAddr, out AddressInfo relAddr);
					node = new FunctionNode(absAddr, CpuType, Debugger, display, relAddr, Debugger.GetCachedPage(absAddr));
					_index[absAddr] = node;
				}
				if(i < funcLengths.Length) node.SetFunctionLength(funcLengths[i]);
				node.RefreshName();
				node.RefreshRelAddress();
				return node;
			}).ToList();

			int memSize = DebugApi.GetMemorySize(prgMemType);
			if(memSize > 0) {
				AddressCounters[] counters = DebugApi.GetMemoryAccessCounts((uint)0, (uint)memSize, prgMemType);
				UInt64 masterClock = EmuApi.GetTimingInfo(CpuType).MasterClock;
				foreach(var entry in sortedFunctions) {
					int addr = entry.AbsAddr.Address;
					if(addr >= 0 && addr < counters.Length) entry.SetCounters(counters[addr], masterClock);
				}
			}

			List<FunctionNode> visible = ShowBlocked ? sortedFunctions : sortedFunctions.Where(f => !f.IsBlocked).ToList();
			SortHelper.SortList(visible, SortState.SortOrder, _comparers, "AbsAddr");
			Functions.Replace(visible);
			Selection.SelectIndexes(selectedIndexes, Functions.Count);

			if(_index.Count > sortedFunctions.Count) {
				var live = new HashSet<AddressInfo>(sortedFunctions.Select(f => f.AbsAddr));
				foreach(var key in _index.Keys.ToList())
					if(!live.Contains(key)) _index.Remove(key);
			}
		}

		public FunctionNode GetOrCreateNode(AddressInfo absAddr)
		{
			if(_index.TryGetValue(absAddr, out FunctionNode? node)) return node;
			string display = Debugger.GetOrUpdateRelAddressDisplay(absAddr, out AddressInfo relAddr);
			return new FunctionNode(absAddr, CpuType, Debugger, display, relAddr, Debugger.GetCachedPage(absAddr));
		}

		// ----- Context menu helpers -----

		private string GetHintText(bool isAbs = false)
		{
			if(Selection.SelectedItem is not FunctionNode entry) return "";
			if(isAbs) return MemoryHelper.GetAddrStr(entry.FuncAbsAddr);
			return entry.FuncRelAddr.Address >= 0 ? MemoryHelper.GetAddrStr(entry.FuncRelAddr) : "";
		}

		private void SetColorForSelected(string? hex)
		{
			foreach(var fv in Selection.SelectedItems.OfType<FunctionNode>()) {
				Debugger.GetOrAddFuncMeta(fv.FuncAbsAddr).FunctionColor = hex;
				fv.RefreshMeta();
			}
			Debugger.MarkCacheDirty();
			Debugger.NotifyFuncMetaChanged();
		}

		private List<object> BuildColorActions(Control parent)
		{
			var actions = ColorPalette.Select(c => new ContextMenuAction {
				ActionType = ActionType.Custom,
				CustomText = ResourceHelper.GetMessage(c.Key),
				OnClick = () => SetColorForSelected(c.Hex)
			}).Cast<object>().ToList();
			actions.Add(new ContextMenuSeparator());
			actions.Add(new ContextMenuAction { ActionType = ActionType.Custom, CustomText = ResourceHelper.GetMessage("mnuClearColor"), OnClick = () => SetColorForSelected(null) });
			actions.Add(new ContextMenuAction { ActionType = ActionType.Custom, CustomText = ResourceHelper.GetMessage("mnuCustomColor"), OnClick = () => _ = PickCustomColor(parent) });
			return actions;
		}

		private async Task PickCustomColor(Control parent)
		{
			ColorPickerViewModel model = new() { Color = Colors.White };
			if(await new ColorPickerWindow { DataContext = model }.ShowCenteredDialog<bool>(parent.GetWindow()))
				SetColorForSelected(model.Color.ToString());
		}

		private void BlockSelected(bool blocked)
		{
			foreach(var fv in Selection.SelectedItems.OfType<FunctionNode>()) {
				Debugger.GetOrAddFuncMeta(fv.FuncAbsAddr).Blocked = blocked;
				fv.RefreshMeta();
			}
			Debugger.MarkCacheDirty();
			Debugger.NotifyFuncMetaChanged();
			UpdateFunctionList();
		}

		private void SetBlockedByColor(string? color, bool blocked)
		{
			if(color == null) return;
			foreach(var kvp in Debugger.FuncMetaCache.Where(kv => kv.Value.FunctionColor == color))
				kvp.Value.Blocked = blocked;
			Debugger.MarkCacheDirty();
			Debugger.NotifyFuncMetaChanged();
			UpdateFunctionList();
		}

		private List<object> BuildColorBlockActions(Control parent, bool blocked)
		{
			return ColorPalette.Select(c => (object)new ContextMenuAction {
				ActionType = ActionType.Custom,
				CustomText = ResourceHelper.GetMessage(c.Key),
				OnClick = () => SetBlockedByColor(c.Hex, blocked)
			}).ToList();
		}

		private List<object> BuildBlockActions(Control parent) => new() {
			new ContextMenuAction {
				ActionType = ActionType.Custom,
				CustomText = ResourceHelper.GetMessage("mnuBlockFunction"),
				IsEnabled = () => Selection.SelectedItems.Count > 0,
				OnClick = () => BlockSelected(true)
			},
			new ContextMenuAction {
				ActionType = ActionType.Custom,
				CustomText = ResourceHelper.GetMessage("mnuUnblockFunction"),
				IsEnabled = () => Selection.SelectedItems.Count > 0,
				OnClick = () => BlockSelected(false)
			},
			new ContextMenuSeparator(),
			new ContextMenuAction {
				ActionType = ActionType.Custom,
				CustomText = ResourceHelper.GetMessage("mnuBlockByColor"),
				SubActions = BuildColorBlockActions(parent, true)
			},
			new ContextMenuAction {
				ActionType = ActionType.Custom,
				CustomText = ResourceHelper.GetMessage("mnuUnblockByColor"),
				SubActions = BuildColorBlockActions(parent, false)
			},
		};

		private void MarkSelected()
		{
			foreach(var fv in Selection.SelectedItems.OfType<FunctionNode>())
				fv.IsMarked = true;
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
						if(Selection.SelectedItem is FunctionNode entry) {
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
						if(Selection.SelectedItem is FunctionNode entry) {
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
						if(Selection.SelectedItem is FunctionNode entry) {
							BreakpointManager.EditBreakpointAtRange(entry.FuncAbsAddr, entry.FunctionLength, CpuType, parent);
						}
					}
				},
				new ContextMenuSeparator(),
				new ContextMenuAction() {
					ActionType = ActionType.FindOccurrences,
					HintText = () => GetHintText(),
					Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.FunctionList_FindOccurrences),
					IsEnabled = () => Selection.SelectedItems.Count == 1 && Selection.SelectedItem is FunctionNode entry && entry.FuncRelAddr.Address >= 0,
					OnClick = () => {
						if(Selection.SelectedItem is FunctionNode entry && entry.FuncRelAddr.Address >= 0) {
							DisassemblySearchOptions options = new() { MatchCase = true, MatchWholeWord = true };
							Debugger.FindAllOccurrences(entry.Label?.Label ?? entry.RelAddressDisplay, options);
						}
					}
				},
				new ContextMenuAction() {
					ActionType = ActionType.GoToLocation,
					HintText = () => GetHintText(),
					Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.FunctionList_GoToLocation),
					IsEnabled = () => Selection.SelectedItems.Count == 1 && Selection.SelectedItem is FunctionNode entry && entry.FuncRelAddr.Address >= 0,
					OnClick = () => {
						if(Selection.SelectedItem is FunctionNode entry) {
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
						if(Selection.SelectedItem is FunctionNode entry) {
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
						if(Selection.SelectedItem is FunctionNode entry) {
							MemoryToolsWindow.ShowInMemoryTools(entry.FuncAbsAddr.Type, entry.FuncRelAddr.Address);
						}
					}
				},
				new ContextMenuSeparator(),
				new ContextMenuAction() {
					ActionType = ActionType.Custom,
					CustomText = ResourceHelper.GetMessage("mnuFunctionColor"),
					SubActions = BuildColorActions(parent)
				},
				new ContextMenuAction() {
					ActionType = ActionType.Custom,
					CustomText = ResourceHelper.GetMessage("mnuBlockMenu"),
					SubActions = BuildBlockActions(parent)
				},
				new ContextMenuAction() {
					ActionType = ActionType.Custom,
					CustomText = ResourceHelper.GetMessage("mnuMarkFunctionAccess"),
					IsEnabled = () => Selection.SelectedItems.Count > 0,
					OnClick = () => MarkSelected()
				},
			}));
		}
	}

	public class FunctionNode : INotifyPropertyChanged
	{
		private CpuType _cpuType;
		private DebuggerWindowViewModel _debugger;

		public AddressInfo AbsAddr { get; }
		public AddressInfo RelAddr { get; private set; }
		public int Page { get; private set; }

		public AddressInfo FuncAbsAddr => AbsAddr;
		public AddressInfo FuncRelAddr => RelAddr;

		public string AbsAddressDisplay { get; }
		public string RelAddressDisplay { get; private set; }
		public string FunctionName { get; private set; }
		public UInt32 FunctionLength { get; private set; }
		public string FunctionLengthDisplay { get; private set; }
		public string ExecCount { get; private set; }
		public string LastExec { get; private set; }
		public UInt64 ExecCountValue { get; private set; }
		public UInt64 LastExecValue { get; private set; }

		public CodeLabel? Label => LabelManager.GetLabel(AbsAddr);
		public event PropertyChangedEventHandler? PropertyChanged;

		public FunctionNode(AddressInfo absAddr, CpuType cpuType, DebuggerWindowViewModel debugger, string relAddressDisplay, AddressInfo relAddr, int page)
		{
			AbsAddr = absAddr;
			_debugger = debugger;
			_cpuType = cpuType;
			RelAddr = relAddr;
			Page = page;
			RelAddressDisplay = relAddressDisplay;
			AbsAddressDisplay = MemoryHelper.GetAddrStr(absAddr, false, false);
			FunctionName = MemoryHelper.GetFunctionName(absAddr, true);
			FunctionLengthDisplay = "";
			ExecCount = "";
			LastExec = "";
		}

		public object RowBackground
		{
			get
			{
				string? c = _debugger.GetFuncMeta(AbsAddr)?.FunctionColor;
				if(!string.IsNullOrEmpty(c)) { try { return new SolidColorBrush(Color.Parse(c)); } catch { } }
				return AvaloniaProperty.UnsetValue;
			}
		}
		public object RowForeground
		{
			get
			{
				if(_debugger.GetFuncMeta(AbsAddr)?.Blocked == true) return Brushes.Gray;
				return IsPageInUse ? AvaloniaProperty.UnsetValue : Brushes.Gray;
			}
		}
		public FontStyle RowStyle => IsPageInUse ? FontStyle.Normal : FontStyle.Italic;
		public FontWeight RowWeight => _debugger.GetFuncMeta(AbsAddr)?.Blocked == true ? FontWeight.Bold : FontWeight.Normal;
		public bool IsBlocked => _debugger.GetFuncMeta(AbsAddr)?.Blocked ?? false;

		private bool IsPageInUse
		{
			get
			{
				if(Page < 0) return RelAddr.Address >= 0;
				return _debugger.UsedPages.Count == 0 || _debugger.UsedPages.Contains(Page);
			}
		}

		public bool IsMarked
		{
			get => _debugger.GetFuncMeta(AbsAddr)?.Marked ?? false;
			set
			{
				var m = _debugger.GetOrAddFuncMeta(AbsAddr);
				if(m.Marked == value) return;
				m.Marked = value;
				_debugger.MarkCacheDirty();
				_debugger.NotifyFuncMetaChanged();
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsMarked)));
				DebugApi.SetFunctionMemoryAccessTracked(_cpuType, AbsAddr, value);
			}
		}

		public void RefreshName()
		{
			string name = MemoryHelper.GetFunctionName(AbsAddr, true);
			if(name != FunctionName) {
				FunctionName = name;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FunctionName)));
			}
		}

		public void RefreshMeta()
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RowBackground)));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RowForeground)));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RowStyle)));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RowWeight)));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsBlocked)));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsMarked)));
		}

		public void RefreshRelAddress()
		{
			string display = _debugger.GetOrUpdateRelAddressDisplay(AbsAddr, out AddressInfo relAddr);
			int page = _debugger.GetCachedPage(AbsAddr);
			if(relAddr.Address == RelAddr.Address && display == RelAddressDisplay && page == Page) return;
			RelAddr = relAddr;
			RelAddressDisplay = display;
			Page = page;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RelAddressDisplay)));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RowStyle)));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RowForeground)));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RowBackground)));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RowWeight)));
		}

		public void Refresh()
		{
			string display = _debugger.GetOrUpdateRelAddressDisplay(AbsAddr, out AddressInfo addr);
			int page = _debugger.GetCachedPage(AbsAddr);
			bool addrChanged = addr.Address != RelAddr.Address;
			if(addrChanged) RelAddr = addr;
			if(!addrChanged && display == RelAddressDisplay && page == Page) return;
			RelAddressDisplay = display;
			Page = page;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RelAddressDisplay)));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RowStyle)));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RowForeground)));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RowBackground)));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RowWeight)));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FunctionName)));
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
				LastExec = ConfigManager.Config.Debug.Debugger.ShowLastExecTimeInSeconds
					? ((double)LastExecValue / EmuApi.GetTimingInfo(_cpuType).MasterClockRate).ToString("0.###") + "s"
					: CodeTooltipHelper.FormatCount(LastExecValue);
			}
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ExecCount)));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LastExec)));
		}

		public void SetFunctionLength(UInt32 length)
		{
			FunctionLength = length;
			FunctionLengthDisplay = length.ToString();
		}
	}
}
