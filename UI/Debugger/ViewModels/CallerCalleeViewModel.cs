using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Selection;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;
using System.Linq;
using System.Threading.Tasks;

namespace Mesen.Debugger.ViewModels
{
	public partial class CallerCalleeViewModel : DisposableViewModel
	{
		public CpuType CpuType { get; }
		public DebuggerWindowViewModel Debugger { get; }

		[ObservableProperty] public partial MesenList<CallerCalleeEntry> Callers { get; private set; } = new();
		[ObservableProperty] public partial MesenList<CallerCalleeEntry> Callees { get; private set; } = new();
		[ObservableProperty] public partial bool HasCallers { get; private set; }
		[ObservableProperty] public partial bool HasCallees { get; private set; }
		[ObservableProperty] public partial SelectionModel<CallerCalleeEntry?> CallerSelection { get; set; } = new() { SingleSelect = true };
		[ObservableProperty] public partial SelectionModel<CallerCalleeEntry?> CalleeSelection { get; set; } = new() { SingleSelect = true };
		[ObservableProperty] public partial string SelectedFunctionName { get; private set; } = "";
		[ObservableProperty] public partial AddressInfo SelectedFunctionAddress { get; private set; }

		public bool SelectedFunctionMarked {
			get => SelectedFunctionAddress.Address >= 0 && Debugger.GetFuncMeta(SelectedFunctionAddress)?.Marked == true;
			set {
				if(SelectedFunctionAddress.Address < 0 || value == SelectedFunctionMarked) return;
				var entry = Entry;
				if(entry != null) entry.IsMarked = value; // reuse FunctionNode.IsMarked setter
			}
		}

		[ObservableProperty] public partial MesenList<AccessRangeViewModel> MarkedAccessRanges { get; private set; } = new();
		[ObservableProperty] public partial bool HasAccessData { get; private set; }
		[ObservableProperty] public partial bool ShowAllFunctions { get; set; }
		[ObservableProperty] public partial bool ShowBlockedRanges { get; set; } = true;

		[ObservableProperty] public partial SelectionModel<AccessRangeViewModel?> AccessRangeSelection { get; set; } = new() { SingleSelect = false };
		public AccessRangeViewModel? SelectedAccessRange => AccessRangeSelection.SelectedItem;

		[ObservableProperty] public partial SortState AccessRangeSortState { get; set; } = new();
		public ICommand AccessRangeSortCommand { get; }

		public List<int> ColumnWidths { get; } = new() { 20, 45, 70, 70, 40 };
		public List<int> AccessRangesColumnWidths { get; } = new() { 160, 25, 50, 30, 30, 30 };

		private readonly Dictionary<string, Func<AccessRangeViewModel, AccessRangeViewModel, int>> _accessRangeComparers = new() {
			{ "Range", (a, b) => a.Start.CompareTo(b.Start) },
			{ "Rw", (a, b) => { int c = ((int)a.Flags).CompareTo((int)b.Flags); return c != 0 ? c : a.Start.CompareTo(b.Start); } },
			{ "MemType", (a, b) => { int c = ((int)a.MemType).CompareTo((int)b.MemType); return c != 0 ? c : a.Start.CompareTo(b.Start); } },
			{ "ReadCount", (a, b) => { int c = a.ReadCount.CompareTo(b.ReadCount); return c != 0 ? c : a.Start.CompareTo(b.Start); } },
			{ "WriteCount", (a, b) => { int c = a.WriteCount.CompareTo(b.WriteCount); return c != 0 ? c : a.Start.CompareTo(b.Start); } },
			{ "AccessCount", (a, b) => { int c = a.AccessCount.CompareTo(b.AccessCount); return c != 0 ? c : a.Start.CompareTo(b.Start); } },
		};

		private List<AccessRangeViewModel> _topAccessRanges = new();
		private Dictionary<RangeIdentity, AccessRangeViewModel> _accessRangeByIdentity = new();

		private CallerCalleeEntry? Entry => CallerSelection.SelectedItem ?? CalleeSelection.SelectedItem;
		public NavigationHistory<AddressInfo> History { get; } = new();
		private bool _navigating;

		public bool CanGoBack => History.CanGoBack();
		public bool CanGoForward => History.CanGoForward();

		private const uint MemOpExecOpCode = 1u << 2;
		private const uint MemOpExecOperand = 1u << 3;
		private const uint MemOpDummyRead = 1u << 6;
		private const uint MemOpDummyWrite = 1u << 7;
		private const uint MemOpPpuRenderingRead = 1u << 8;
		private const uint MaskInstructionFetch = MemOpExecOpCode | MemOpExecOperand;
		private const uint MaskDummy = MemOpDummyRead | MemOpDummyWrite;
		private const uint MaskPpuRender = MemOpPpuRenderingRead;

		private uint MemAccessOptions => ConfigManager.Config.Debug.Debugger.FunctionMemoryAccessOptions;

		public bool TrackInstructionFetch
		{
			get => (MemAccessOptions & MaskInstructionFetch) != 0;
			set => SetMemAccessBit(MaskInstructionFetch, value);
		}
		public bool TrackDummy
		{
			get => (MemAccessOptions & MaskDummy) != 0;
			set => SetMemAccessBit(MaskDummy, value);
		}
		public bool TrackPpuRender
		{
			get => (MemAccessOptions & MaskPpuRender) != 0;
			set => SetMemAccessBit(MaskPpuRender, value);
		}

		partial void OnShowAllFunctionsChanged(bool value) => UpdateMarkedAccessRanges();

		public CallerCalleeViewModel(CpuType cpuType, DebuggerWindowViewModel debugger)
		{
			CpuType = cpuType;
			Debugger = debugger;
			RetrackCommand = new RelayCommand(() => Retrack());
			AccessRangeSortCommand = new RelayCommand<object?>(SortAccessRanges);
			AccessRangeSortState.SetColumnSort("MemType", ListSortDirection.Ascending, true);
			CallerSelection.SelectionChanged += (_, _) => UpdateMarkedAccessRanges();
			CalleeSelection.SelectionChanged += (_, _) => UpdateMarkedAccessRanges();
			Debugger.FuncMetaChanged += () => OnPropertyChanged(new PropertyChangedEventArgs(nameof(SelectedFunctionMarked)));
		}

		[Obsolete("For designer only")]
		public CallerCalleeViewModel() : this(CpuType.Snes, new()) { }

		public ICommand RetrackCommand { get; }

		public void Retrack()
		{
			DebugApi.SetFunctionMemoryAccessOptions(CpuType, MemAccessOptions);
			DebugApi.ResetFunctionMemoryAccess(CpuType);
			UpdateMarkedAccessRanges();
		}

		private void SetMemAccessBit(uint bit, bool on)
		{
			uint mask = MemAccessOptions;
			ConfigManager.Config.Debug.Debugger.FunctionMemoryAccessOptions = on ? mask | bit : mask & ~bit;
			Retrack();
		}

		public void SortAccessRanges(object? param = null) => RebuildAccessRangeList();

		private void RebuildAccessRangeList()
		{
			SortHelper.SortList(_topAccessRanges, AccessRangeSortState.SortOrder, _accessRangeComparers, "AccessCount");
			var flat = new List<AccessRangeViewModel>(_topAccessRanges.Count);
			foreach(var r in _topAccessRanges) {
				flat.Add(r);
				if(r.IsExpanded) flat.AddRange(r.Children);
			}
			MarkedAccessRanges.Replace(flat);
		}

		public void ToggleAccessRangeExpand(AccessRangeViewModel range)
		{
			if(range == null || !range.IsExpandable) return;
			if(!range.IsExpanded) {
				if(range.Children.Count == 0) {
					var details = DebugApi.GetFunctionMemoryAccessDetails(CpuType, range.FuncAddr, range.MemType, range.Start, range.End, range.Interval);
					foreach(var d in details) {
						if(d.MemType.IsRomMemory() && range._range.RelAddress.HasValue
							&& (d.Start & 0xFFFFF000u) == (range._range.Start & 0xFFFFF000u)) {
							d.RelPage = range._range.RelPage;
							d.RelAddress = range._range.RelAddress + (int)(d.Start - range._range.Start);
						}
						range.Children.Add(new AccessRangeViewModel(d, CpuType, Debugger, range.FuncAddr, isDetail: true));
					}
				}
				if(range.Children.Count == 0) return;
				range.IsExpanded = true;
			} else {
				range.IsExpanded = false;
			}
			RebuildAccessRangeList();
		}

		public void UpdateForFunction(AddressInfo funcAddr, string funcName)
		{
			Debugger.EnsureCacheLoaded();
			if(funcAddr.Address < 0) {
				SelectedFunctionName = ""; SelectedFunctionAddress = default;
				Callers.Replace(new List<CallerCalleeEntry>()); HasCallers = false;
				Callees.Replace(new List<CallerCalleeEntry>()); HasCallees = false;
				MarkedAccessRanges.Replace(new List<AccessRangeViewModel>());
				HasAccessData = false;
				return;
			}
			SelectedFunctionName = funcName;
			SelectedFunctionAddress = funcAddr;
			OnPropertyChanged(new PropertyChangedEventArgs(nameof(SelectedFunctionMarked)));
			if(!_navigating) History.AddHistory(funcAddr);

			var record = DebugApi.GetCallerCallee(CpuType, funcAddr);
			var callers = Enumerable.Range(0, (int)Math.Min(record.CallerCount, 64))
				.Select(i => {
					var c = record.Callers[i]; return c.Address.Address >= 0 
						? new CallerCalleeEntry(GetFunctionNode(c.Address)) { CallCount = c.CallCount.ToString(), CallCountValue = c.CallCount } 
						: null; 
				})
				.Where(e => e != null).Cast<CallerCalleeEntry>().ToList();
			Callers.Replace(callers);
			HasCallers = callers.Count > 0;
			var callees = Enumerable.Range(0, (int)Math.Min(record.CalleeCount, 64))
				.Select(i => {
					var c = record.Callees[i]; return c.Address.Address >= 0 
						? new CallerCalleeEntry(GetFunctionNode(c.Address)) { CallCount = c.CallCount.ToString(), CallCountValue = c.CallCount } 
						: null; 
				})
				.Where(e => e != null).Cast<CallerCalleeEntry>().ToList();
			Callees.Replace(callees);
			HasCallees = callees.Count > 0;
			UpdateMarkedAccessRanges();
		}

		private FunctionNode GetFunctionNode(AddressInfo absAddr)
		{
			if(Debugger.FunctionList != null) return Debugger.FunctionList.GetOrCreateNode(absAddr);
			string display = Debugger.GetOrUpdateRelAddressDisplay(absAddr, out AddressInfo relAddr);
			return new FunctionNode(absAddr, CpuType, Debugger, display, relAddr, Debugger.GetCachedPage(absAddr));
		}

		public void GoBack()
		{
			if(History.CanGoBack()) {
				NavigateTo(History.GoBack());
			}
		}

		public void GoForward()
		{
			if(History.CanGoForward()) {
				NavigateTo(History.GoForward());
			}
		}

		// Jump to a historical function. The entry point is always FunctionList, so
		// we re-select the matching row to keep the highlight in sync; _navigating
		// suppresses re-recording into the history.
		private void NavigateTo(AddressInfo funcAddr)
		{
			if(funcAddr.Address < 0) return;
			_navigating = true;
			try {
				var fl = Debugger.FunctionList;
				if(fl != null) {
					var match = fl.Functions.FirstOrDefault(f => f.FuncAbsAddr.Type == funcAddr.Type && f.FuncAbsAddr.Address == funcAddr.Address);
					if(match != null) {
						if(fl.Selection.SelectedItem != match) fl.Selection.SelectedItem = match;
						else UpdateForFunction(funcAddr, MemoryHelper.GetFunctionName(funcAddr, true));
						return;
					}
				}
				UpdateForFunction(funcAddr, MemoryHelper.GetFunctionName(funcAddr, true));
			} finally { _navigating = false; }
		}

		internal void UpdateMarkedAccessRanges()
		{
			bool hasData = false;

			var targets = new List<AddressInfo>();
			var entry = Entry;
			if(ShowAllFunctions) {
				if(entry != null) targets.Add(entry.FuncAbsAddr);
				if(SelectedFunctionAddress.Address >= 0) targets.Add(SelectedFunctionAddress);
				foreach(var m in Callers.Concat(Callees))
					if(m != entry && m.IsMarked) targets.Add(m.FuncAbsAddr);
			} else if(SelectedFunctionAddress.Address >= 0) {
				targets.Add(SelectedFunctionAddress);
			}

			var liveAll = new FuncMemoryAccess();
			var countMap = new Dictionary<(MemoryType, uint), (uint Read, uint Write, uint Access)>();
			foreach(var target in targets) {
				var live = DebugApi.GetFunctionMemoryAccess(CpuType, target);
				if(live == null) continue;
				foreach(var r in live.Ranges) {
					liveAll.Ranges.Add(r);
					countMap[(r.MemType, r.Start)] = (r.ReadCount, r.WriteCount, r.AccessCount);
				}
			}

			var cachedAll = new FuncMemoryAccess();
			foreach(var target in targets) {
				var cached = Debugger.GetFuncMeta(target)?.MemoryAccess;
				if(cached == null) continue;
				foreach(var r in cached.Ranges)
					if(!countMap.ContainsKey((r.MemType, r.Start)))
						cachedAll.Ranges.Add(r);
			}

			var merged = FuncMemoryAccess.Union(liveAll.Ranges.Count > 0 ? liveAll : null,
				cachedAll.Ranges.Count > 0 ? cachedAll : null);

			var newTop = new List<AccessRangeViewModel>();
			foreach(var r in merged.Ranges) {
				AddressInfo funcAddr = targets.FirstOrDefault(t => t.Address >= 0);

				// Restore persisted color/block state from FuncMetaCache.
				foreach(var meta in Debugger.FuncMetaCache.Values) {
					if(meta.MemoryAccess?.Ranges == null) continue;
					var cached = meta.MemoryAccess.Ranges.FirstOrDefault(c => c.Start == r.Start && c.MemType == r.MemType);
					if(cached == null) continue;
					r.RangeColor = cached.RangeColor;
					r.Blocked = cached.Blocked;
					break;
				}

				var id = r.Identity;
				countMap.TryGetValue((r.MemType, r.Start), out var counts);
				if(_accessRangeByIdentity.TryGetValue(id, out var existing)) {
					existing.UpdateCounts(counts.Read, counts.Write, counts.Access);
				} else {
					r.ReadCount = counts.Read; r.WriteCount = counts.Write; r.AccessCount = counts.Access;
					existing = new AccessRangeViewModel(r, CpuType, Debugger, funcAddr);
					_accessRangeByIdentity[id] = existing;
				}
				newTop.Add(existing);
				hasData = true;
			}

			var seenIds = new HashSet<RangeIdentity>(newTop.Select(vm => new RangeIdentity(vm.MemType, vm.Start, vm.Length, vm.Flags, vm.Interval)));
			foreach(var key in _accessRangeByIdentity.Keys.ToList())
				if(!seenIds.Contains(key)) _accessRangeByIdentity.Remove(key);

			// Filter blocked ranges unless ShowBlockedRanges is enabled.
			if(!ShowBlockedRanges) {
				newTop = newTop.Where(r => !r.IsBlocked).ToList();
				hasData = newTop.Count > 0;
			}

			_topAccessRanges = newTop;
			HasAccessData = hasData;
			RebuildAccessRangeList();
		}

		partial void OnShowBlockedRangesChanged(bool value) => UpdateMarkedAccessRanges();

		// ----- Colors / block / mark -----

		private void SetColorForSelected(string? hex)
		{
			var e = Entry;
			if(e == null) return;
			Debugger.GetOrAddFuncMeta(e.FuncAbsAddr).FunctionColor = hex;
			e.RefreshMeta();
			Debugger.MarkCacheDirty();
			Debugger.NotifyFuncAppearanceChanged();
		}

		private List<object> BuildColorActions(Control parent)
		{
			var actions = FunctionListViewModel.ColorPalette.Select(c => (object)new ContextMenuAction {
				ActionType = ActionType.Custom,
				CustomText = ResourceHelper.GetMessage(c.Key),
				OnClick = () => SetColorForSelected(c.Hex)
			}).ToList();
			actions.Add(new ContextMenuSeparator());
			actions.Add(new ContextMenuAction {
				ActionType = ActionType.Custom,
				CustomText = ResourceHelper.GetMessage("mnuClearColor"),
				OnClick = () => SetColorForSelected(null)
			});
			actions.Add(new ContextMenuAction {
				ActionType = ActionType.Custom,
				CustomText = ResourceHelper.GetMessage("mnuCustomColor"),
				OnClick = () => _ = PickCustomColor(parent)
			});
			return actions;
		}

		private async Task PickCustomColor(Control parent)
		{
			var model = new ColorPickerViewModel() { Color = Colors.White };
			if(await new ColorPickerWindow { DataContext = model }.ShowCenteredDialog<bool>(parent.GetWindow()))
				SetColorForSelected(model.Color.ToString());
		}

		private void BlockSelected(bool blocked)
		{
			var e = Entry;
			if(e == null) return;
			Debugger.GetOrAddFuncMeta(e.FuncAbsAddr).Blocked = blocked;
			e.RefreshMeta();
			Debugger.MarkCacheDirty();
			Debugger.NotifyFuncAppearanceChanged();
		}

		private void SetBlockedByColor(bool blocked)
		{
			var e = Entry;
			if(e == null) return;
			string? color = Debugger.GetFuncMeta(e.FuncAbsAddr)?.FunctionColor;
			if(color == null) return;
			foreach(var kvp in Debugger.FuncMetaCache.Where(kv => kv.Value.FunctionColor == color))
				kvp.Value.Blocked = blocked;
			Debugger.MarkCacheDirty();
			Debugger.NotifyFuncAppearanceChanged();
		}

		private List<object> BuildBlockActions(Control parent)
		{
			string? GetColor() { var e = Entry; return e != null ? Debugger.GetFuncMeta(e.FuncAbsAddr)?.FunctionColor : null; }
			return new List<object> {
				new ContextMenuAction { ActionType = ActionType.Custom, CustomText = ResourceHelper.GetMessage("mnuBlockFunction"), IsEnabled = () => Entry != null, OnClick = () => BlockSelected(true) },
				new ContextMenuAction { ActionType = ActionType.Custom, CustomText = ResourceHelper.GetMessage("mnuUnblockFunction"), IsEnabled = () => Entry != null, OnClick = () => BlockSelected(false) },
				new ContextMenuSeparator(),
				new ContextMenuAction { ActionType = ActionType.Custom, CustomText = ResourceHelper.GetMessage("mnuBlockByColor"),
					HintText = () => FunctionListViewModel.GetColorDisplayName(GetColor()),
					IsEnabled = () => GetColor() != null,
					OnClick = () => SetBlockedByColor(true) },
				new ContextMenuAction { ActionType = ActionType.Custom, CustomText = ResourceHelper.GetMessage("mnuUnblockByColor"),
					HintText = () => FunctionListViewModel.GetColorDisplayName(GetColor()),
					IsEnabled = () => GetColor() != null,
					OnClick = () => SetBlockedByColor(false) },
			};
		}

		private void MarkSelected()
		{
			var e = Entry;
			if(e == null) {
				return;
			}
			e.IsMarked = true;
			OnMarkedToggled(e);
		}

		public void OnMarkedToggled(CallerCalleeEntry entry) { }

		private string GetHintText(bool isAbs = false)
		{
			if(Entry == null) {
				return "";
			}
			if(isAbs) {
				return MemoryHelper.GetAddrStr(Entry.FuncAbsAddr);
			} else if(Entry.FuncRelAddr.Address >= 0) {
				return MemoryHelper.GetAddrStr(Entry.FuncRelAddr);
			}
			return Debugger.GetRelAddressDisplay(Entry.FuncAbsAddr);
		}

		private List<object> BuildFunctionContextMenuActions(Control parent)
		{
			return new List<object> {
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
					IsEnabled = () => Entry != null,
					OnClick = () => MarkSelected()
				},
			};
		}

		// Function-level context menu (color / block / mark / breakpoints / memory
		// viewer) — attached ONLY to the Callers and Callees grids so the memory/ROM
		// access panel does not inherit it (its rows are addresses, not functions).
		public void InitContextMenu(DataBox callers, DataBox callees)
		{
			AddDisposables(DebugShortcutManager.CreateContextMenu(callers, BuildFunctionContextMenuActions(callers)));
			AddDisposables(DebugShortcutManager.CreateContextMenu(callees, BuildFunctionContextMenuActions(callees)));
		}

		public void InitAccessContextMenu(DataBox accessGrid)
		{
			AddressInfo GetAbs() => SelectedAccessRange == null ? default : new AddressInfo { Type = SelectedAccessRange.MemType, Address = (int)SelectedAccessRange.Start };
			AddressInfo GetRel()
			{
				var range = SelectedAccessRange;
				if(range == null) return default;
				var rel = range.RelAddr;
				return rel.Address >= 0 ? rel : default;
			}

			void ForEachSelected(Action<AccessRangeViewModel> action)
			{
				foreach(var r in AccessRangeSelection.SelectedItems.OfType<AccessRangeViewModel>()) {
					action(r);
					foreach(var c in r.Children) action(c);
				}
			}
			void ForAllRanges(Action<AccessRangeViewModel> action)
			{
				foreach(var r in MarkedAccessRanges) {
					action(r);
					foreach(var c in r.Children) action(c);
				}
			}
			void MarkCacheDirty() => Debugger.MarkCacheDirty();

			void SetRangeColor(string? hex)
			{
				bool any = false;
				ForEachSelected(r => { r.RangeColor = hex; r.RefreshVisual(); any = true; });
				if(!any) { var r = SelectedAccessRange; if(r != null) { r.RangeColor = hex; r.RefreshVisual(); } }
				if(any || SelectedAccessRange != null) MarkCacheDirty();
			}
			async Task PickRangeColor(Control parent)
			{
				var model = new ColorPickerViewModel() { Color = Colors.White };
				if(await new ColorPickerWindow { DataContext = model }.ShowCenteredDialog<bool>(parent.GetWindow()))
					SetRangeColor(model.Color.ToString());
			}

			void BlockRangeByColor(bool blocked)
			{
				var color = AccessRangeSelection.SelectedItems.OfType<AccessRangeViewModel>().FirstOrDefault(r => r.RangeColor != null)?.RangeColor;
				if(color == null) return;
				ForAllRanges(r => { if(r.RangeColor == color) r.Blocked = blocked; });
				RefreshAccessRangeVisuals();
				MarkCacheDirty();
			}
			void BlockRangeByMemType(bool blocked)
			{
				var mt = AccessRangeSelection.SelectedItems.OfType<AccessRangeViewModel>().FirstOrDefault()?.MemType;
				if(mt == null) return;
				ForAllRanges(r => { if(r.MemType == mt.Value) r.Blocked = blocked; });
				RefreshAccessRangeVisuals();
				MarkCacheDirty();
			}
			void BlockRangeByRw(bool blocked)
			{
				var f = AccessRangeSelection.SelectedItems.OfType<AccessRangeViewModel>().FirstOrDefault()?.Flags;
				if(f == null) return;
				ForAllRanges(r => { if(r.Flags == f.Value) r.Blocked = blocked; });
				RefreshAccessRangeVisuals();
				MarkCacheDirty();
			}

			List<object> BuildRangeColorActions() {
				var acts = FunctionListViewModel.ColorPalette.Select(c => (object)new ContextMenuAction {
					ActionType = ActionType.Custom, CustomText = ResourceHelper.GetMessage(c.Key),
					OnClick = () => SetRangeColor(c.Hex)
				}).ToList();
				acts.Add(new ContextMenuSeparator());
				acts.Add(new ContextMenuAction {
					ActionType = ActionType.Custom,
					CustomText = ResourceHelper.GetMessage("mnuClearColor"),
					OnClick = () => SetRangeColor(null) 
				});
				acts.Add(new ContextMenuAction {
					ActionType = ActionType.Custom,
					CustomText = ResourceHelper.GetMessage("mnuCustomColor"),
					OnClick = () => _ = PickRangeColor(accessGrid) 
				});
				return acts;
			}
			AddDisposables(DebugShortcutManager.CreateContextMenu(accessGrid, new List<object> {
				new ContextMenuAction() {
					ActionType = ActionType.EditLabel,
					HintText = () => MemoryHelper.GetAddrStr(GetAbs()),
					Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.FunctionList_EditLabel),
					IsEnabled = () => SelectedAccessRange != null,
					OnClick = () => {
						var a = GetAbs();
						CodeLabel? label = LabelManager.GetLabel(a);
						if(a.Address >= 0 && SelectedAccessRange != null) {
							LabelEditWindow.EditLabel(CpuType, accessGrid, label ?? new CodeLabel(a));
						}
					}
				},
				new ContextMenuAction() {
					ActionType = ActionType.ToggleBreakpoint,
					HintText = () => MemoryHelper.GetAddrStr(GetRel()),
					IsEnabled = () => GetRel().Address >= 0 && SelectedAccessRange != null,
					OnClick = () => {
						var a = GetRel();
						if(a.Address >= 0 && SelectedAccessRange != null) {
							BreakpointManager.EditBreakpointAtRange(a, SelectedAccessRange.SpanLength, CpuType, accessGrid);
						}
					}
				},
				new ContextMenuAction() {
					ActionType = ActionType.ToggleBreakpoint,
					HintText = () => MemoryHelper.GetAddrStr(GetAbs()),
					IsVisible = () => GetRel().Type != GetAbs().Type,
					IsEnabled = () => SelectedAccessRange != null,
					OnClick = () => {
						var a = GetAbs();
						if(a.Address >= 0 && SelectedAccessRange != null) {
							BreakpointManager.EditBreakpointAtRange(a, SelectedAccessRange.SpanLength, CpuType, accessGrid);
						}
					}
				},
				new ContextMenuSeparator(),
				new ContextMenuAction() {
					ActionType = ActionType.ViewInMemoryViewer,
					HintText = () => MemoryHelper.GetAddrStr(GetRel()),
					IsEnabled = () => GetRel().Address >= 0,
					OnClick = () => {
						var a = GetRel();
						if(a.Address >= 0) {
							MemoryToolsWindow.ShowInMemoryTools(a.Type, a.Address);
						}
					}
				},
				new ContextMenuAction() {
					ActionType = ActionType.ViewInMemoryViewer,
					HintText = () => MemoryHelper.GetAddrStr(GetAbs()),
					IsVisible = () => GetRel().Type != GetAbs().Type,
					IsEnabled = () => SelectedAccessRange != null,
					OnClick = () => { 
						var a = GetAbs();
						if(a.Address >= 0) {
							MemoryToolsWindow.ShowInMemoryTools(a.Type, a.Address);
						}
					} 
				},
				new ContextMenuSeparator(),
				new ContextMenuAction() {
					ActionType = ActionType.Custom,
					CustomText = ResourceHelper.GetMessage("mnuFunctionColor"),
					SubActions = BuildRangeColorActions()
				},
				new ContextMenuAction() {
					ActionType = ActionType.Custom,
					CustomText = ResourceHelper.GetMessage("mnuBlockMenu"), SubActions = new List<object> {
						new ContextMenuAction {
							ActionType = ActionType.Custom,
							CustomText = ResourceHelper.GetMessage("mnuBlockFunction"),
							IsEnabled = () => AccessRangeSelection.SelectedItems.Count > 0,
							OnClick = () => { ForEachSelected(r => { r.Blocked = true; r.RefreshVisual(); }); RefreshAccessRangeVisuals(); MarkCacheDirty(); }
						},
						new ContextMenuAction {
							ActionType = ActionType.Custom,
							CustomText = ResourceHelper.GetMessage("mnuUnblockFunction"),
							IsEnabled = () => AccessRangeSelection.SelectedItems.Count > 0,
							OnClick = () => { ForEachSelected(r => { r.Blocked = false; r.RefreshVisual(); }); RefreshAccessRangeVisuals(); MarkCacheDirty(); }
						},
						new ContextMenuSeparator(),
						new ContextMenuAction {
							ActionType = ActionType.Custom,
							CustomText = ResourceHelper.GetMessage("mnuBlockByColor"),
							HintText = () => FunctionListViewModel.GetColorDisplayName(AccessRangeSelection.SelectedItems.OfType<AccessRangeViewModel>().FirstOrDefault(r => r.RangeColor != null)?.RangeColor),
							IsEnabled = () => AccessRangeSelection.SelectedItems.OfType<AccessRangeViewModel>().Any(r => r.RangeColor != null),
							OnClick = () => BlockRangeByColor(true) 
						},
						new ContextMenuAction {
							ActionType = ActionType.Custom,
							CustomText = ResourceHelper.GetMessage("mnuUnblockByColor"),
							HintText = () => FunctionListViewModel.GetColorDisplayName(AccessRangeSelection.SelectedItems.OfType<AccessRangeViewModel>().FirstOrDefault(r => r.RangeColor != null)?.RangeColor),
							IsEnabled = () => AccessRangeSelection.SelectedItems.OfType<AccessRangeViewModel>().Any(r => r.RangeColor != null),
							OnClick = () => BlockRangeByColor(false) 
						},
						new ContextMenuSeparator(),
						new ContextMenuAction {
							ActionType = ActionType.Custom,
							CustomText = ResourceHelper.GetMessage("mnuBlockByMemType"),
							HintText = () => { 
								var r = AccessRangeSelection.SelectedItems.OfType<AccessRangeViewModel>().FirstOrDefault();
								return r?.MemType.GetShortName() ?? "";
							},
							IsEnabled = () => AccessRangeSelection.SelectedItems.Count > 0,
							OnClick = () => BlockRangeByMemType(true)
						},
						new ContextMenuAction {
							ActionType = ActionType.Custom,
							CustomText = ResourceHelper.GetMessage("mnuUnblockByMemType"),
							HintText = () => { 
								var r = AccessRangeSelection.SelectedItems.OfType<AccessRangeViewModel>().FirstOrDefault();
								return r?.MemType.GetShortName() ?? "";
							},
							IsEnabled = () => AccessRangeSelection.SelectedItems.Count > 0,
							OnClick = () => BlockRangeByMemType(false)
						},
						new ContextMenuSeparator(),
						new ContextMenuAction {
							ActionType = ActionType.Custom,
							CustomText = ResourceHelper.GetMessage("mnuBlockByRw"),
							HintText = () => { 
								var r = AccessRangeSelection.SelectedItems.OfType<AccessRangeViewModel>().FirstOrDefault();
								return r?.RwDisplay ?? "";
							},
							IsEnabled = () => AccessRangeSelection.SelectedItems.Count > 0,
							OnClick = () => BlockRangeByRw(true) 
						},
						new ContextMenuAction {
							ActionType = ActionType.Custom,
							CustomText = ResourceHelper.GetMessage("mnuUnblockByRw"),
							HintText = () => { 
								var r = AccessRangeSelection.SelectedItems.OfType<AccessRangeViewModel>().FirstOrDefault();
								return r?.RwDisplay ?? "";
							},
							IsEnabled = () => AccessRangeSelection.SelectedItems.Count > 0,
							OnClick = () => BlockRangeByRw(false)
						},
					}
				},
			}));
		}

		private void RefreshAccessRangeVisuals()
		{
			foreach(var r in MarkedAccessRanges) {
				r.RefreshVisual();
				foreach(var c in r.Children) c.RefreshVisual();
			}
		}
	}

	public class CallerCalleeEntry : INotifyPropertyChanged
	{
		public FunctionNode Node { get; }
		public string CallCount { get; set; } = "";
		public UInt64 CallCountValue { get; set; }

		public AddressInfo FuncAbsAddr => Node.AbsAddr;
		public AddressInfo FuncRelAddr => Node.RelAddr;
		public string FunctionName => Node.FunctionName;
		public string RelAddressDisplay => Node.RelAddressDisplay;
		public string AbsAddressDisplay => Node.AbsAddressDisplay;
		public object RowBackground => Node.RowBackground;
		public object RowForeground => Node.RowForeground;
		public FontStyle RowStyle => Node.RowStyle;
		public FontWeight RowWeight => Node.RowWeight;
		public double RowOpacity => Node.RowOpacity;
		public bool IsBlocked => Node.IsBlocked;
		public CodeLabel? Label => Node.Label;
		public bool IsMarked { get => Node.IsMarked; set => Node.IsMarked = value; }

		public event PropertyChangedEventHandler? PropertyChanged;

		public CallerCalleeEntry(FunctionNode node) { Node = node; }

		public void RefreshMeta()
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RowBackground)));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RowForeground)));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RowStyle)));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RowWeight)));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsBlocked)));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsMarked)));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FunctionName)));
		}
	}
}
