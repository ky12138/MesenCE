using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Selection;
using Avalonia.Media;
using Avalonia.Threading;
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

		public bool SelectedFunctionMarked
		{
			get => SelectedFunctionAddress.Address >= 0 && Debugger.GetFuncMeta(SelectedFunctionAddress)?.Marked == true;
			set
			{
				if(SelectedFunctionAddress.Address < 0 || value == SelectedFunctionMarked) return;
				// 标记的是面板头部当前查看的函数本身（SelectedFunctionAddress），而非列表里选中的行。
				// 直接写入 FuncMeta，与 FunctionNode.IsMarked 的逻辑保持一致。
				var m = Debugger.GetOrAddFuncMeta(SelectedFunctionAddress);
				m.Marked = value;
				Debugger.MarkCacheDirty();
				Debugger.NotifyFuncMetaChanged();
				OnPropertyChanged(new PropertyChangedEventArgs(nameof(SelectedFunctionMarked)));
				DebugApi.SetFunctionMemoryAccessTracked(CpuType, SelectedFunctionAddress, value);
			}
		}

		[ObservableProperty] public partial MesenList<AccessRangeViewModel> MarkedAccessRanges { get; private set; } = new();
		[ObservableProperty] public partial bool HasAccessData { get; private set; }
		[ObservableProperty] public partial bool ShowAllFunctions { get; set; }
		[ObservableProperty] public partial bool ShowBlockedRanges { get; set; } = true;

		[ObservableProperty] public partial SelectionModel<AccessRangeViewModel?> AccessRangeSelection { get; set; } = new() { SingleSelect = false };
		public AccessRangeViewModel? SelectedAccessRange => AccessRangeSelection.SelectedItem;

		// ----- 断点记录（反向内存访问） -----
		[ObservableProperty] public partial MesenList<MemoryAccessFunctionEntry> AccessedByFunctions { get; private set; } = new();
		[ObservableProperty] public partial SelectionModel<MemoryAccessFunctionEntry?> AccessedBySelection { get; set; } = new() { SingleSelect = true };
		[ObservableProperty] public partial bool HasReverseData { get; private set; }
		[ObservableProperty] public partial bool IsBreakpointMode { get; private set; }
		[ObservableProperty] public partial string SelectedBreakpointTitle { get; private set; } = "";
		public ICommand ClearReverseCommand { get; }

		[ObservableProperty] public partial SortState AccessRangeSortState { get; set; } = new();
		public ICommand AccessRangeSortCommand { get; }

		public List<int> ColumnWidths { get; } = new() { 30, 45, 70, 70, 40 };
		public List<int> AccessRangesColumnWidths { get; } = new() { 170, 25, 50, 30, 30, 30 };
		public List<int> ReverseColumnWidths { get; } = new() { 30, 45, 70, 70, 40, 40 };

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
		public NavigationHistory<NavigationEntry> History { get; } = new();
		private bool _navigating;

		// 布局切换：断点记录模式隐藏 Callers/Callees/访问记录面板，仅显示反向面板
		public bool ShowCallers => !IsBreakpointMode && HasCallers;
		public bool ShowCallees => !IsBreakpointMode && HasCallees;
		public bool ShowFuncPanel => !IsBreakpointMode && !string.IsNullOrEmpty(SelectedFunctionName);

		partial void OnIsBreakpointModeChanged(bool value)
		{
			OnPropertyChanged(new PropertyChangedEventArgs(nameof(ShowCallers)));
			OnPropertyChanged(new PropertyChangedEventArgs(nameof(ShowCallees)));
			OnPropertyChanged(new PropertyChangedEventArgs(nameof(ShowFuncPanel)));
		}
		partial void OnHasCallersChanged(bool value) => OnPropertyChanged(new PropertyChangedEventArgs(nameof(ShowCallers)));
		partial void OnHasCalleesChanged(bool value) => OnPropertyChanged(new PropertyChangedEventArgs(nameof(ShowCallees)));
		partial void OnSelectedFunctionNameChanged(string value) => OnPropertyChanged(new PropertyChangedEventArgs(nameof(ShowFuncPanel)));
		partial void OnSelectedFunctionAddressChanged(AddressInfo value) => OnPropertyChanged(new PropertyChangedEventArgs(nameof(SelectedFunctionMarked)));

		// 反向（断点记录）查询参数，供 Clear 后重新加载
		private AddressInfo _reverseStartAddr;
		private uint _reverseEndAddr;
		private MemoryType _reverseMemType;

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
			ClearReverseCommand = new RelayCommand(() => ClearReverse());
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
			// 离开断点记录模式，回到函数记录模式
			IsBreakpointMode = false;
			AccessedByFunctions.Replace(new List<MemoryAccessFunctionEntry>());
			HasReverseData = false;
			SelectedBreakpointTitle = "";

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
			if(!_navigating) History.AddHistory(new NavigationEntry(NavigationKind.Function, funcAddr));

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

		// Jump to a historical entry (function or breakpoint-access). _navigating
		// suppresses re-recording into the history.
		private void NavigateTo(NavigationEntry entry)
		{
			if(entry == null) return;
			_navigating = true;
			try {
				if(entry.Kind == NavigationKind.Function) {
					var fl = Debugger.FunctionList;
					if(fl != null) {
						var match = fl.Functions.FirstOrDefault(f => f.FuncAbsAddr.Type == entry.Address.Type && f.FuncAbsAddr.Address == entry.Address.Address);
						if(match != null) {
							if(fl.Selection.SelectedItem != match) fl.Selection.SelectedItem = match;
							else UpdateForFunction(entry.Address, MemoryHelper.GetFunctionName(entry.Address, true));
							return;
						}
					}
					UpdateForFunction(entry.Address, MemoryHelper.GetFunctionName(entry.Address, true));
				} else {
					// 断点记录（反向）历史条目
					LoadBreakpointAccess(entry.Address, entry.EndAddress, entry.Address.Type, entry.Title);
				}
			} finally {
				// FunctionList.Selection.SelectionChanged 触发的 UpdateForFunction 可能被延迟到
				// 下一个 dispatcher 帧执行；若此处同步复位 _navigating，则那次回调会把回退/前进
				// 动作误当作新导航写入历史，AddHistory 中的 ClearForwardHistory 会清空前进栈，
				// 导致 GoBack/GoForward 失效。用低优先级 Post 复位可覆盖同步/异步两种时序。
				Dispatcher.UIThread.Post(() => _navigating = false, DispatcherPriority.Background);
			}
		}

		// 从断点列表单击 Record 断点进入：加载被哪些函数访问过该地址
		public void ShowForBreakpoint(Breakpoint bp)
		{
			Debugger.EnsureCacheLoaded();
			if(bp.StartAddress > bp.EndAddress) {
				return;
			}
			LoadBreakpointAccess(
				new AddressInfo { Type = bp.MemoryType, Address = (int)bp.StartAddress },
				bp.EndAddress,
				bp.MemoryType,
				bp.GetAddressString(true)
			);
			if(!_navigating) {
				History.AddHistory(new NavigationEntry(
					NavigationKind.BreakpointAccess,
					new AddressInfo { Type = bp.MemoryType, Address = (int)bp.StartAddress },
					bp.EndAddress,
					bp.GetAddressString(true)
				));
			}
		}

		// 加载某地址区间被哪些函数访问过（反向），并切换到断点记录模式
		private void LoadBreakpointAccess(AddressInfo startAddr, uint endAddr, MemoryType memType, string? title)
		{
			IsBreakpointMode = true;
			SelectedBreakpointTitle = title ?? "";
			_reverseStartAddr = startAddr;
			_reverseEndAddr = endAddr;
			_reverseMemType = memType;

			// 清空函数记录模式的展示
			Callers.Replace(new List<CallerCalleeEntry>());
			HasCallers = false;
			Callees.Replace(new List<CallerCalleeEntry>());
			HasCallees = false;
			MarkedAccessRanges.Replace(new List<AccessRangeViewModel>());
			HasAccessData = false;
			SelectedFunctionName = "";
			SelectedFunctionAddress = default;
			OnPropertyChanged(new PropertyChangedEventArgs(nameof(SelectedFunctionMarked)));

			var records = DebugApi.GetMemoryAccessFunctions(CpuType, memType, (uint)startAddr.Address, endAddr);
			var entries = new List<MemoryAccessFunctionEntry>();
			foreach(var r in records) {
				var node = GetFunctionNode(new AddressInfo { Type = r.FuncType, Address = r.FuncAddress });
				entries.Add(new MemoryAccessFunctionEntry(node, r.AccessCount, r.Flags));
			}
			AccessedByFunctions.Replace(entries);
			HasReverseData = entries.Count > 0;
		}

		// 清空反向（断点记录）数据后重新加载当前断点视图
		public void ClearReverse()
		{
			DebugApi.ResetReverseMemoryAccess(CpuType);
			// 标记缓存脏，使清空态持久化（SaveCache 发现无反向数据会删除旧 JSON）。
			Debugger.MarkCacheDirty();
			if(IsBreakpointMode) {
				LoadBreakpointAccess(_reverseStartAddr, _reverseEndAddr, _reverseMemType, SelectedBreakpointTitle);
			}
		}

		// 暂停/继续时按「当前模式」刷新面板数据，但不切换模式、不写入导航历史：
		// - 函数模式：重新拉取 caller/callee 与访问计数（实时更新）；
		// - 断点记录模式：重新拉取反向内存访问数据（实时更新）。
		// 若不刷新，断点记录模式在暂停后既不更新也不会被误清空。
		public void RefreshCurrent()
		{
			_navigating = true;
			try {
				if(IsBreakpointMode) {
					if(_reverseStartAddr.Address >= 0) {
						LoadBreakpointAccess(_reverseStartAddr, _reverseEndAddr, _reverseMemType, SelectedBreakpointTitle);
					}
				} else if(SelectedFunctionAddress.Address >= 0) {
					UpdateForFunction(SelectedFunctionAddress, SelectedFunctionName);
				}
			} finally {
				Dispatcher.UIThread.Post(() => _navigating = false, DispatcherPriority.Background);
			}
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

			List<object> BuildRangeColorActions()
			{
				var acts = FunctionListViewModel.ColorPalette.Select(c => (object)new ContextMenuAction {
					ActionType = ActionType.Custom,
					CustomText = ResourceHelper.GetMessage(c.Key),
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

	// 统一导航历史条目：区分"函数记录"与"断点记录（反向）"两类目标，
	// 使 GoBack/GoForward 可在两者间任意切换。
	public enum NavigationKind
	{
		Function,
		BreakpointAccess
	}

	public class NavigationEntry
	{
		public NavigationKind Kind { get; }
		// 函数模式：函数绝对地址；断点模式：断点起始地址（Type = 断点 MemoryType）
		public AddressInfo Address { get; }
		public uint EndAddress { get; }
		public string? Title { get; }

		public NavigationEntry(NavigationKind kind, AddressInfo address, uint endAddress = 0, string? title = null)
		{
			Kind = kind;
			Address = address;
			EndAddress = endAddress;
			Title = title;
		}

		public override bool Equals(object? obj)
		{
			if(obj is not NavigationEntry other) {
				return false;
			}
			return Kind == other.Kind
				&& Address.Type == other.Address.Type
				&& Address.Address == other.Address.Address
				&& EndAddress == other.EndAddress;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine((int)Kind, (int)Address.Type, Address.Address, EndAddress);
		}
	}

	// 反向（断点记录）面板中的一行：某函数访问过目标地址的 r/w/e 类型与次数
	public class MemoryAccessFunctionEntry
	{
		private readonly FunctionNode _node;
		public uint AccessCount { get; }
		public RwFlags Flags { get; }
		public string RweDisplay => FormatRwe(Flags);

		public AddressInfo FuncAbsAddr => _node.AbsAddr;
		public AddressInfo FuncRelAddr => _node.RelAddr;
		public string FunctionName => _node.FunctionName;
		public string RelAddressDisplay => _node.RelAddressDisplay;
		public string AbsAddressDisplay => _node.AbsAddressDisplay;
		public object RowBackground => _node.RowBackground;
		public object RowForeground => _node.RowForeground;
		public FontStyle RowStyle => _node.RowStyle;
		public FontWeight RowWeight => _node.RowWeight;
		public double RowOpacity => _node.RowOpacity;
		public bool IsBlocked => _node.IsBlocked;
		public CodeLabel? Label => _node.Label;
		public bool IsMarked { get => _node.IsMarked; set => _node.IsMarked = value; }

		public MemoryAccessFunctionEntry(FunctionNode node, uint accessCount, RwFlags flags)
		{
			_node = node;
			AccessCount = accessCount;
			Flags = flags;
		}

		private static string FormatRwe(RwFlags f)
		{
			string s = "";
			if(f.HasFlag(RwFlags.Read)) s += "R";
			if(f.HasFlag(RwFlags.Write)) s += "W";
			if(f.HasFlag(RwFlags.Execute)) s += "X";
			return s;
		}
	}
}
