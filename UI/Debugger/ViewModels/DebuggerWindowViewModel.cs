using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm.Controls;
using Mesen.Config;
using Mesen.Debugger.Disassembly;
using Mesen.Debugger.Integration;
using Mesen.Debugger.Labels;
using Mesen.Debugger.StatusViews;
using Mesen.Debugger.Utilities;
using Mesen.Debugger.ViewModels.DebuggerDock;
using Mesen.Debugger.Windows;
using Mesen.Interop;
using Mesen.Localization;
using Mesen.Utilities;
using Mesen.ViewModels;
using Mesen.Windows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Mesen.Debugger.ViewModels
{
	public partial class DebuggerWindowViewModel : DisposableViewModel
	{
		[ObservableProperty] public partial string Title { get; private set; } = "Debugger";
		[ObservableProperty] public partial WindowIcon? Icon { get; private set; } = null;
		[ObservableProperty] public partial bool IsMainCpuDebugger { get; private set; } = true;

		[ObservableProperty] public partial DebuggerConfig Config { get; private set; }

		[ObservableProperty] public partial DebuggerOptionsViewModel Options { get; private set; }

		[ObservableProperty] public partial DisassemblyViewModel Disassembly { get; private set; }
		[ObservableProperty] public partial BreakpointListViewModel BreakpointList { get; private set; }
		[ObservableProperty] public partial WatchListViewModel WatchList { get; private set; }
		[ObservableProperty] public partial BaseConsoleStatusViewModel? ConsoleStatus { get; private set; }
		[ObservableProperty] public partial LabelListViewModel LabelList { get; private set; }
		[ObservableProperty] public partial FunctionListViewModel? FunctionList { get; private set; }
		[ObservableProperty] public partial CallerCalleeViewModel? CallerCallee { get; private set; }
		[ObservableProperty] public partial CallStackViewModel CallStack { get; private set; }
		[ObservableProperty] public partial SourceViewViewModel? SourceView { get; private set; }
		[ObservableProperty] public partial MemoryMappingViewModel? MemoryMappings { get; private set; }
		[ObservableProperty] public partial FindResultListViewModel FindResultList { get; private set; }
		[ObservableProperty] public partial ControllerListViewModel ControllerList { get; private set; }

		[ObservableProperty] public partial DebuggerDockFactory DockFactory { get; private set; }
		[ObservableProperty] public partial IRootDock DockLayout { get; private set; }

		[ObservableProperty] public partial string BreakReason { get; private set; } = "";
		[ObservableProperty] public partial string BreakElapsedCycles { get; private set; } = "";
		[ObservableProperty] public partial string BreakElapsedCyclesTooltip { get; private set; } = "";
		[ObservableProperty] public partial string CdlStats { get; private set; } = "";

		[ObservableProperty] public partial List<ContextMenuAction> ToolbarItems { get; private set; } = new();

		[ObservableProperty] public partial List<ContextMenuAction> FileMenuItems { get; private set; } = new();
		[ObservableProperty] public partial List<ContextMenuAction> DebugMenuItems { get; private set; } = new();
		[ObservableProperty] public partial List<ContextMenuAction> SearchMenuItems { get; private set; } = new();
		[ObservableProperty] public partial List<ContextMenuAction> OptionMenuItems { get; private set; } = new();

		public CpuType CpuType { get; private set; }
		private UInt64 _masterClock = 0;

		// 函数相对地址缓存：以绝对地址为键，缓存 (RelPage, RelAddress) 两个整数
		//（均 null 表示无相对地址）。原 RelAddressDisplayCache 缓存的是 "page:$relAddr"
		// 字符串，现拆为 RelPage + RelAddress，显示串在需要时由二者重建，JSON 更紧凑。
		// RelPage 直接取自缓存，避免每次重建显示串都重算 GetPage（PCE 还会 P/Invoke）。
		public Dictionary<AddressInfo, (int? RelPage, int? RelAddress)> RelAddressCache { get; } = new();

		// 当前 CPU 地址空间正在使用的 PRG 页集合，由 MemoryMappings.CpuMappings 各块的
		// Page 汇总而来。Function List 以此为「页」单位判断函数是否处于当前可寻址空间：
		// 页不在此集合中的函数一律斜体。每次 UpdateFunctionList 重建前刷新，避免逐函数
		// 为斜体判定再去 P/Invoke 取 relAddr（先前靠 RelAddr.Address>=0 的判定由此替换）。
		public HashSet<int> UsedPages { get; } = new();

		public void RefreshUsedPages()
		{
			UsedPages.Clear();
			var mappings = MemoryMappings?.CpuMappings;
			if(mappings != null) {
				foreach(var block in mappings) {
					if(block.Page >= 0) {
						UsedPages.Add(block.Page);
					}
				}
			}
		}

		// 取函数所属 PRG 页：优先用缓存的 RelPage（Put 时无 P/Invoke），缺失时回落到
		// MemoryHelper.GetPage 直算。-1 表示无法确定页（如部分 RAM 段）。
		public int GetCachedPage(AddressInfo absAddr)
		{
			if(RelAddressCache.TryGetValue(absAddr, out var cached) && cached.RelPage.HasValue) {
				return cached.RelPage.Value;
			}
			return MemoryHelper.GetPage(absAddr, CpuType);
		}

		/// <summary>取缓存的相对地址显示串（"page:$relAddr"）；无相对地址或缺失时返回 ""。</summary>
		public string GetRelAddressDisplay(AddressInfo absAddr)
		{
			if(RelAddressCache.TryGetValue(absAddr, out var cached) && cached.RelAddress.HasValue) {
				return FormatRelDisplay(cached.RelPage, cached.RelAddress.Value);
			}
			return "";
		}

		/// <summary>确保相对地址已缓存（缺失，或曾为无相对地址但现已映射时重算），返回显示串；
		/// 同时通过 out 参数把相对地址（缓存命中时由 RelAddress 重建，无需 P/Invoke）交给调用方复用，
		/// 避免调用方再为 FuncRelAddr 单独调用一次 GetRelativeAddress。</summary>
		public string GetOrUpdateRelAddressDisplay(AddressInfo absAddr, out AddressInfo relAddr)
		{
			if(RelAddressCache.TryGetValue(absAddr, out var cached) && cached.RelAddress.HasValue) {
				// 缓存已有效：不调用 GetRelativeAddress（P/Invoke），直接用缓存重建相对地址。
				// 相对地址位于 CPU 地址空间，类型取 CPU 内存类型（与 GetRelativeAddress 返回值一致）。
				relAddr = new AddressInfo { Address = cached.RelAddress.Value, Type = CpuType.ToMemoryType() };
				return FormatRelDisplay(cached.RelPage, cached.RelAddress.Value);
			}
			relAddr = DebugApi.GetRelativeAddress(absAddr, CpuType);
			int? relPage = cached.RelPage ?? MemoryHelper.GetPage(absAddr, CpuType);
			int? relAddress = relAddr.Address >= 0 ? relAddr.Address : (int?)null;
			if(!RelAddressCache.TryGetValue(absAddr, out var existing) || existing.RelAddress != relAddress || existing.RelPage != relPage) {
				RelAddressCache[absAddr] = (relPage, relAddress);
				MarkCacheDirty();
			}
			return relAddress.HasValue ? FormatRelDisplay(relPage, relAddress.Value) : "";
		}

		// 由缓存的 RelPage + RelAddress 重建显示串，不再调用 GetPageText（避免重复计算 page）。
		// 相对地址是 CPU 地址空间内的地址（bank:offset 形式），其格式位数应由 CPU
		// 内存类型决定，而非 PRG ROM 类型——用后者会让十六进制位数与地址类型对不上。
		private string FormatRelDisplay(int? relPage, int relAddress)
		{
			return MemoryHelper.FormatRelDisplay(relPage, relAddress, CpuType);
		}

		private bool _cacheLoaded;
		private int _unsavedChanges;
		private DateTime _lastCacheSave = DateTime.MinValue;
		private object _cacheLock = new();

		private bool _autoSwitchToSourceView = false;

		private List<object> _gotoSubActions = new();

		[Obsolete("For designer only")]
		public DebuggerWindowViewModel() : this(null) { }

		public DebuggerWindowViewModel(CpuType? cpuType)
		{
			if(!Design.IsDesignMode) {
				DebugApi.InitializeDebugger();
			}

			ConsoleType consoleType;
			if(Design.IsDesignMode) {
				CpuType = CpuType.Snes;
				consoleType = ConsoleType.Snes;
			} else {
				RomInfo romInfo = EmuApi.GetRomInfo();
				consoleType = romInfo.ConsoleType;
				if(cpuType != null) {
					CpuType = cpuType.Value;
					if(consoleType.GetMainCpuType() != CpuType) {
						Title = ResourceHelper.GetEnumText(CpuType) + " Debugger";
						Icon = new WindowIcon(ImageUtilities.BitmapFromAsset("Assets/" + CpuType.ToString() + "Debugger.png"));
						IsMainCpuDebugger = false;
					} else {
						Icon = new WindowIcon(ImageUtilities.BitmapFromAsset("Assets/Debugger.png"));
					}
				} else {
					CpuType = romInfo.ConsoleType.GetMainCpuType();
					Icon = new WindowIcon(ImageUtilities.BitmapFromAsset("Assets/Debugger.png"));
				}
			}

			Config = ConfigManager.Config.Debug.Debugger;

			Options = new DebuggerOptionsViewModel(Config, CpuType);
			Disassembly = AddDisposable(new DisassemblyViewModel(this, ConfigManager.Config.Debug, CpuType));
			BreakpointList = AddDisposable(new BreakpointListViewModel(CpuType, this));
			LabelList = AddDisposable(new LabelListViewModel(CpuType, this));
			FindResultList = AddDisposable(new FindResultListViewModel(this));
			ControllerList = new ControllerListViewModel(consoleType);
			if(CpuType.SupportsFunctionList()) {
				FunctionList = AddDisposable(new FunctionListViewModel(CpuType, this));
				CallerCallee = AddDisposable(new CallerCalleeViewModel(CpuType, this));
			}
			CallStack = AddDisposable(new CallStackViewModel(CpuType, this));
			WatchList = AddDisposable(new WatchListViewModel(CpuType));
			ConsoleStatus = CpuType switch {
				CpuType.Snes => new SnesStatusViewModel(CpuType.Snes),
				CpuType.Spc => new SpcStatusViewModel(),
				CpuType.NecDsp => new NecDspStatusViewModel(),
				CpuType.Sa1 => new SnesStatusViewModel(CpuType.Sa1),
				CpuType.Gsu => new GsuStatusViewModel(),
				CpuType.Cx4 => new Cx4StatusViewModel(),
				CpuType.St018 => new St018StatusViewModel(),
				CpuType.Gameboy => new GbStatusViewModel(),
				CpuType.Nes => new NesStatusViewModel(),
				CpuType.Pce => new PceStatusViewModel(),
				CpuType.Sms => new SmsStatusViewModel(),
				CpuType.Gba => new GbaStatusViewModel(),
				CpuType.Ws => new WsStatusViewModel(),
				_ => null
			};

			if(ConsoleStatus is NesStatusViewModel nesStatus) {
				nesStatus.Debugger = this;
			}

			DockFactory = new DebuggerDockFactory(Config.SavedDockLayout);

			DockFactory.BreakpointListTool.Model = BreakpointList;
			DockFactory.LabelListTool.Model = LabelList;
			DockFactory.FunctionListTool.Model = FunctionList;
			DockFactory.CallerCalleeTool.Model = CallerCallee;
			DockFactory.CallStackTool.Model = CallStack;
			DockFactory.WatchListTool.Model = WatchList;
			DockFactory.FindResultListTool.Model = FindResultList;
			DockFactory.ControllerListTool.Model = ControllerList;
			DockFactory.DisassemblyTool.Model = Disassembly;
			DockFactory.SourceViewTool.Model = null;
			DockFactory.StatusTool.Model = ConsoleStatus;

			DockLayout = DockFactory.CreateLayout();
			InitDock();

			if(Design.IsDesignMode) {
				return;
			}

			AddDisposable(ReactiveHelper.RegisterRecursiveObserver(Config, Config_PropertyChanged));

			if(CpuType.SupportsMemoryMappings()) {
				MemoryMappings = new MemoryMappingViewModel(CpuType);
			}

			DebugWorkspaceManager.SymbolProviderChanged += DebugWorkspaceManager_SymbolProviderChanged;
			LabelManager.OnLabelUpdated += LabelManager_OnLabelUpdated;
			BreakpointManager.BreakpointsChanged += BreakpointManager_BreakpointsChanged;
			BreakpointManager.AddCpuType(CpuType);

			if(FunctionList != null && CallerCallee != null) {
				var functionList = FunctionList;
				var callerCallee = CallerCallee;
				functionList.Selection.SelectionChanged += (s, e) => {
					if(functionList.Selection?.SelectedItem is FunctionNode vm) {
						callerCallee.UpdateForFunction(vm.FuncAbsAddr,
							vm.FuncRelAddr.Address >= 0
								? GetRelAddressDisplay(vm.FuncAbsAddr)
								: vm.AbsAddressDisplay
						);
					} else {
						callerCallee.UpdateForFunction(new AddressInfo() { Address = -1, Type = MemoryType.None }, "");
					}
				};
				// Cross-view sync: when func meta (color/block/marked) changes in either view,
				// refresh the other view's display without recreating the list.
				FuncMetaChanged += () => {
					foreach(var f in functionList.Functions) {
						f.RefreshMeta();
					}
					foreach(var c in callerCallee.Callers) {
						c.RefreshMeta();
					}
					foreach(var c in callerCallee.Callees) {
						c.RefreshMeta();
					}
					callerCallee.UpdateMarkedAccessRanges();
				};
				// Appearance-only changes (color/block) refresh the list display but
				// must NOT rebuild the access panel (that would drop drill-down state).
				FuncAppearanceChanged += () => {
					foreach(var f in functionList.Functions) {
						f.RefreshMeta();
					}
					foreach(var c in callerCallee.Callers) {
						c.RefreshMeta();
					}
					foreach(var c in callerCallee.Callees) {
						c.RefreshMeta();
					}
				};
			}

			ConfigApi.SetDebuggerFlag(CpuType.GetDebuggerFlag(), true);
		}

		private void InitDock()
		{
			DockFactory.InitLayout(DockLayout);

			if(FunctionList == null) {
				DockFactory.CloseDockable(DockFactory.FunctionListTool);
			}
			if(CallerCallee == null) {
				DockFactory.CloseDockable(DockFactory.CallerCalleeTool);
			}

			// Inject CallerCalleeTool into the layout if saved layout doesn't include it
			if(CallerCallee != null && !IsToolVisible(DockFactory.CallerCalleeTool)) {
				InjectToolIntoSameDockAs(DockFactory.CallerCalleeTool, DockFactory.FunctionListTool);
			}

			if(Design.IsDesignMode) {
				return;
			}

			UpdateSourceViewState();
		}

		private void InjectToolIntoSameDockAs(Tool toolToInject, Tool referenceTool)
		{
			if(referenceTool.Owner is IDock parentDock && parentDock.VisibleDockables != null) {
				if(!parentDock.VisibleDockables.Contains(toolToInject)) {
					DockFactory.AddDockable(parentDock, toolToInject);
				}
			}
		}

		public void ScrollToAddress(int address)
		{
			Disassembly.SetSelectedRow(address, true, true);
			SourceView?.GoToRelativeAddress(address, true);

			IDockable codeTool = GetActiveCodeTool();
			DockFactory.SetFocusedDockable(DockLayout, codeTool);
		}

		private void DebugWorkspaceManager_SymbolProviderChanged(object? sender, EventArgs e)
		{
			UpdateSourceViewState();
		}

		public void Init()
		{
			WatchList.UpdateWatch();
			CallStack.UpdateCallStack();
			LabelList.UpdateLabelList();
			FunctionList?.UpdateFunctionList();
			BreakpointList.UpdateBreakpoints();
		}

		protected override void DisposeView()
		{
			Config.SavedDockLayout = DockFactory.ToDockDefinition(DockLayout);

			SaveCache();

			DebugWorkspaceManager.SymbolProviderChanged -= DebugWorkspaceManager_SymbolProviderChanged;
			LabelManager.OnLabelUpdated -= LabelManager_OnLabelUpdated;
			BreakpointManager.BreakpointsChanged -= BreakpointManager_BreakpointsChanged;
			BreakpointManager.RemoveCpuType(CpuType);
			ConfigApi.SetDebuggerFlag(CpuType.GetDebuggerFlag(), false);
		}

		public void MarkCacheDirty()
		{
			EnsureCacheLoaded();
			Interlocked.Increment(ref _unsavedChanges);
			DebounceSaveCache();
		}

		/// <summary>函数维度的标记元数据（按 AddressInfo 索引，运行时字典）</summary>
		public Dictionary<AddressInfo, FuncMeta> FuncMetaCache { get; } = new();

		/// <summary>函数标记元数据变更通知（颜色/block/marked 变化时触发，用于跨视图同步）</summary>
		public event Action? FuncMetaChanged;
		public void NotifyFuncMetaChanged() => FuncMetaChanged?.Invoke();

		/// <summary>仅外观变化（颜色/block）的通知：只刷新行显示，不重建访问面板</summary>
		public event Action? FuncAppearanceChanged;
		public void NotifyFuncAppearanceChanged() => FuncAppearanceChanged?.Invoke();

		public FuncMeta? GetFuncMeta(AddressInfo key)
		{
			EnsureCacheLoaded();
			return FuncMetaCache.TryGetValue(key, out var m) ? m : null;
		}

		public FuncMeta GetOrAddFuncMeta(AddressInfo key)
		{
			EnsureCacheLoaded();
			if(!FuncMetaCache.TryGetValue(key, out var m)) {
				m = new FuncMeta();
				FuncMetaCache[key] = m;
			}
			return m;
		}

		private async void DebounceSaveCache()
		{
			await Task.Delay(3000);
			SaveCache();
		}

		private void SampleMarkedFunctionMemoryAccess()
		{
			foreach(var kvp in FuncMetaCache) {
				if(kvp.Value.Marked) {
					FuncMemoryAccess? live = DebugApi.GetFunctionMemoryAccess(CpuType, kvp.Key);
					if(live != null && live.Ranges.Count > 0) {
						// Accumulate into the persistent cache instead of replacing it,
						// so the JSON records every address the function has ever
						// accessed (full cache), not just the latest session snapshot.
						kvp.Value.MemoryAccess = FuncMemoryAccess.Union(kvp.Value.MemoryAccess, live);
					}
				}
			}
		}

		public void SaveCache()
		{
			int changes = Interlocked.Exchange(ref _unsavedChanges, 0);
			if(changes == 0 && _cacheLoaded) {
				return;
			}

			string romName = EmuApi.GetRomInfo().GetRomName();
			if(string.IsNullOrEmpty(romName)) {
				return;
			}

			// Sample live memory access data for all marked functions before serializing,
			// so that functions marked only in FunctionList (which don't go through
			// CallerCallee's GetLive path) also get their access data persisted.
			SampleMarkedFunctionMemoryAccess();
			FillRangeDisplays();

			string path = Path.Combine(ConfigManager.DebuggerFolder, romName + "_RelAddr.json");

			var data = new RelAddressCacheData();
			var entries = new List<RelAddressCacheEntry>();
			foreach(var kvp in RelAddressCache) {
				var entry = new RelAddressCacheEntry {
					Address = kvp.Key.Address,
					Type = kvp.Key.Type,
					RelPage = kvp.Value.RelPage ?? -1,
					RelAddress = kvp.Value.RelAddress
				};
				if(FuncMetaCache.TryGetValue(kvp.Key, out var meta) && meta.HasData) {
					entry.FunctionColor = meta.FunctionColor;
					entry.Blocked = meta.Blocked;
					entry.Marked = meta.Marked;
					entry.MemoryAccess = meta.MemoryAccess;
				}
				entries.Add(entry);
			}
			data.CacheByCpu[CpuType] = entries;

			string json = JsonSerializer.Serialize(data, typeof(RelAddressCacheData), MesenSerializerContext.Default);
			FileHelper.WriteAllText(path, json);
		}

		// Fill each range's RelPage/RelAddress once (for marked functions whose
		// AccessRangeViewModel was never constructed, i.e. not shown in the panel)
		// so the split serializes inline into the Range entry of <rom>_RelAddr.json.
		// ROM only — RAM needs no page/rel split. Ranges already displayed have these
		// set by the VM's BuildRelAddrDisplay and are skipped via the HasValue guard,
		// so this only back-fills the unshown ones.
		private void FillRangeDisplays()
		{
			foreach(var meta in FuncMetaCache.Values) {
				if(meta.MemoryAccess?.Ranges == null) {
					continue;
				}
				foreach(var r in meta.MemoryAccess.Ranges) {
					if(!r.MemType.IsRomMemory() || r.RelAddress.HasValue) {
						continue;
					}
					AddressInfo absAddr = new AddressInfo { Address = (int)r.Start, Type = r.MemType };
					GetOrUpdateRelAddressDisplay(absAddr, out AddressInfo rel);
					if(rel.Address >= 0) {
						r.RelPage = GetCachedPage(absAddr);
						r.RelAddress = rel.Address;
					}
				}
			}
		}

		public void EnsureCacheLoaded()
		{
			if(_cacheLoaded) {
				return;
			}
			lock(_cacheLock) {
				if(_cacheLoaded) {
					return;
				}

				string romName = EmuApi.GetRomInfo().GetRomName();
				if(string.IsNullOrEmpty(romName)) {
					_cacheLoaded = true;
					return;
				}

				string path = Path.Combine(ConfigManager.DebuggerFolder, romName + "_RelAddr.json");
				if(File.Exists(path)) {
					try {
						string fileData = File.ReadAllText(path);
						var data = JsonSerializer.Deserialize(fileData, typeof(RelAddressCacheData), MesenSerializerContext.Default) as RelAddressCacheData;
						if(data?.CacheByCpu.TryGetValue(CpuType, out var entries) == true) {
							foreach(var e in entries) {
								var key = new AddressInfo { Address = e.Address, Type = e.Type };
								RelAddressCache.TryAdd(key, (
									e.RelPage >= 0 ? e.RelPage : (int?)null,
									e.RelAddress
								));
								if(e.FunctionColor != null || e.Blocked || e.Marked || e.MemoryAccess != null) {
									FuncMetaCache[key] = new FuncMeta {
										FunctionColor = e.FunctionColor,
										Blocked = e.Blocked,
										Marked = e.Marked,
										MemoryAccess = e.MemoryAccess
									};
								}
							}
							FillRangeDisplays();
						}
					} catch {
					}
				}

				_cacheLoaded = true;
				ApplyFunctionMemoryAccessState();
			}
		}

		// Push the saved tracking options + marked-function set into the C++ tracker so that
		// tracking resumes automatically when a workspace (with cached marked functions) is opened.
		private bool _funcMemAccessStateApplied = false;
		private void ApplyFunctionMemoryAccessState()
		{
			if(_funcMemAccessStateApplied) {
				return;
			}
			_funcMemAccessStateApplied = true;

			DebugApi.SetFunctionMemoryAccessOptions(CpuType, ConfigManager.Config.Debug.Debugger.FunctionMemoryAccessOptions);
			foreach(var kvp in FuncMetaCache) {
				if(kvp.Value.Marked) {
					DebugApi.SetFunctionMemoryAccessTracked(CpuType, kvp.Key, true);
				}
			}
		}

		private void Config_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			ConfigManager.Config.Debug.ApplyConfig();
			UpdateDisassembly(false);
		}

		private void UpdateSourceViewState()
		{
			ISymbolProvider? provider = DebugWorkspaceManager.SymbolProvider;
			if(provider != null) {
				SourceView = new SourceViewViewModel(this, provider, CpuType);
				DockFactory.SourceViewTool.Model = SourceView;
				SourceView.SetActiveAddress(Disassembly.ActiveAddress);
				if(!IsToolVisible(DockFactory.SourceViewTool)) {
					if(DockFactory.SourceViewTool.Owner is IDock dock && IsDockVisible(dock)) {
						DockFactory.AddDockable(dock, DockFactory.SourceViewTool);
					} else if(DockFactory.DisassemblyTool.Owner is IDock disassemblyDock) {
						DockFactory.AddDockable(disassemblyDock, DockFactory.SourceViewTool);
					}
				}
			} else {
				SourceView = null;
				DockFactory.SourceViewTool.Model = null;
				if(IsToolVisible(DockFactory.SourceViewTool)) {
					DockFactory.SourceViewTool.CanClose = true;
					DockFactory.CloseDockable(DockFactory.SourceViewTool);
					DockFactory.SourceViewTool.CanClose = false;
				}
			}
		}

		public void UpdateConsoleState()
		{
			ConsoleStatus?.UpdateConsoleState();
		}

		private void Manager_WatchChanged(object? sender, EventArgs e)
		{
			WatchList.UpdateWatch();
		}

		private void LabelManager_OnLabelUpdated(object? sender, EventArgs e)
		{
			LabelList.UpdateLabelList();
			BreakpointList.RefreshBreakpointList();
			CallStack.RefreshCallStack();
			FunctionList?.UpdateFunctionList();
			Disassembly.Refresh();
		}

		private void BreakpointManager_BreakpointsChanged(object? sender, EventArgs e)
		{
			Disassembly.InvalidateVisual();
			SourceView?.InvalidateVisual();
			BreakpointList.UpdateBreakpoints();
		}

		public void UpdateDebugger(bool forBreak = false, BreakEvent? evt = null)
		{
			ConsoleStatus?.UpdateUiState();

			if(forBreak) {
				if(ConsoleStatus?.EditAllowed == false) {
					ConsoleStatus.EditAllowed = true;
				}
				UpdateStatusBar(evt);
			}

			UpdateDisassembly(forBreak);
			MemoryMappings?.Refresh();
			BreakpointList.RefreshBreakpointList();
			LabelList.RefreshLabelList();
			FunctionList?.UpdateFunctionList();
			WatchList.UpdateWatch();
			CallStack.UpdateCallStack();
		}

		public void PartialRefresh(bool refreshWatch)
		{
			ConsoleStatus?.UpdateUiState(true);
			MemoryMappings?.Refresh();
			UpdateCdlStats();
			if(refreshWatch) {
				WatchList.UpdateWatch();
			}
		}

		private void UpdateStatusBar(BreakEvent? evt)
		{
			if(ConsoleStatus?.ElapsedCycles > 0) {
				string elapsedCycles = $"{CodeTooltipHelper.FormatValue(ConsoleStatus.ElapsedCycles, 999999)} cycles elapsed";
				string tooltip = $"{ConsoleStatus.ElapsedCycles:N0} cycles elapsed";

				if(CpuType == CpuType.Snes) {
					UInt64 prevMasterClock = _masterClock;
					_masterClock = EmuApi.GetTimingInfo(CpuType).MasterClock;
					if(prevMasterClock > 0 && prevMasterClock < _masterClock) {
						elapsedCycles += $" ({CodeTooltipHelper.FormatValue(_masterClock - prevMasterClock, 999999)} master clocks)";
						tooltip += $" ({_masterClock - prevMasterClock:N0} master clocks)";
					}
				}
				BreakElapsedCycles = elapsedCycles;
				BreakElapsedCyclesTooltip = tooltip;
			} else {
				BreakElapsedCycles = "";
				BreakElapsedCyclesTooltip = "";
			}

			UpdateCdlStats();

			if(evt != null) {
				string breakReason = "";
				BreakEvent brkEvent = evt.Value;
				if(brkEvent.Source != BreakSource.Unspecified) {
					breakReason = ResourceHelper.GetEnumText(brkEvent.Source);
					if(brkEvent.Source == BreakSource.Breakpoint) {
						Breakpoint? bp = BreakpointManager.GetBreakpointById(brkEvent.BreakpointId);
						if(bp?.IsAssert == true) {
							breakReason = "Assert failed: " + bp.Condition.Substring(2, bp.Condition.Length - 3);
						} else {
							breakReason += (
								": " +
								ResourceHelper.GetEnumText(brkEvent.Operation.Type) + " " +
								brkEvent.Operation.MemType.GetShortName() +
								" ($" + brkEvent.Operation.Address.ToString("X4") +
								":$" + brkEvent.Operation.Value.ToString("X2") + ")"
							);
						}
					}
				}
				BreakReason = breakReason;
			} else {
				BreakReason = "";
			}
		}

		private void UpdateCdlStats()
		{
			string statsString = "";
			if(CpuType.ToMemoryType().SupportsCdl()) {
				CdlStatistics stats = DebugApi.GetCdlStatistics(CpuType.GetPrgRomMemoryType());
				if(stats.TotalBytes > 0) {
					statsString = $"Code: {(double)stats.CodeBytes / stats.TotalBytes * 100:0.00}% Data: {(double)stats.DataBytes / stats.TotalBytes * 100:0.00}%";
					if(stats.TotalChrBytes > 0) {
						statsString += $" Drawn (CHR ROM): {(double)stats.DrawnChrBytes / stats.TotalChrBytes * 100:0.00}%";
					}
				}
			}
			CdlStats = statsString;
		}

		public void UpdateActiveAddress(bool scrollToAddress)
		{
			if(scrollToAddress) {
				//Scroll to the active address and highlight it
				int activeAddress = (int)DebugApi.GetProgramCounter(CpuType, true);
				Disassembly.SetActiveAddress(activeAddress);

				if(SourceView != null) {
					bool sourceMappingFound = SourceView.SetActiveAddress(activeAddress);
					if(!sourceMappingFound) {
						//If location is not found in source mappings, swap to disassembly view
						if(IsToolVisible(DockFactory.SourceViewTool) && IsToolActive(DockFactory.SourceViewTool)) {
							_autoSwitchToSourceView = true;
							OpenTool(DockFactory.DisassemblyTool);
						}
					} else if(_autoSwitchToSourceView) {
						//If we previously auto-switched to disassembly view, go back to the
						//source view automatically if the current address is mapped to a source file
						OpenTool(DockFactory.SourceViewTool);
						_autoSwitchToSourceView = false;
					}
				}

				if(!EmuApi.IsPaused()) {
					//Clear the highlight if the emulation is still running
					Disassembly.SetActiveAddress(null);
					SourceView?.SetActiveAddress(null);
				}
			}
		}

		public void UpdateDisassembly(bool scrollToActiveAddress)
		{
			UpdateActiveAddress(scrollToActiveAddress);
			Disassembly.Refresh();
			SourceView?.Refresh();
		}

		public void ProcessResumeEvent(Window wnd)
		{
			if(ConsoleStatus != null) {
				//Disable status fields in 50ms (if the debugger isn't paused by then)
				//This improves performance when stepping through code, etc.
				Task.Run(() => {
					System.Threading.Thread.Sleep(50);
					Dispatcher.UIThread.Post(() => {
						BaseConsoleStatusViewModel status = ConsoleStatus;
						if(status != null && status.EditAllowed && !EmuApi.IsPaused()) {
							status.EditAllowed = false;

							if(DockFactory.StatusTool.Owner is IDock parent && parent.IsActive && parent.ActiveDockable == DockFactory.StatusTool) {
								//If status tool is active when it gets disabled, give window focus to avoid focus issues in Avalonia
								//Disabling focused controls by setting IsEnabled=false on a parent does not appear to behave properly
								//The controls keep their focus and can be modified
								wnd.Focus();
							}
						}
					});
				});
			}

			Disassembly.SetActiveAddress(null);
			Disassembly.InvalidateVisual();
			SourceView?.SetActiveAddress(null);
			SourceView?.InvalidateVisual();
		}

		private ToolDock? FindToolDock(IDock dock)
		{
			if(dock is ToolDock) {
				return (ToolDock)dock;
			}

			if(dock.VisibleDockables == null) {
				return null;
			}

			foreach(IDockable dockable in dock.VisibleDockables) {
				if(dockable is IDock) {
					ToolDock? result = FindToolDock((IDock)dockable);
					if(result != null) {
						return result;
					}
				}
			}

			return null;
		}

		private bool IsToolActive(Tool tool)
		{
			return (tool.Owner as IDock)?.ActiveDockable == tool;
		}

		private bool IsDockVisible(IDock? dock)
		{
			return dock is RootDock || (dock != null && (dock.Owner as IDock)?.VisibleDockables?.Contains(dock) == true && (dock.Owner == null || !(dock.Owner is IDock) || IsDockVisible(dock.Owner as IDock)));
		}

		private bool IsToolVisible(Tool tool)
		{
			return (tool.Owner as IDock)?.VisibleDockables?.Contains(tool) == true && IsDockVisible(tool.Owner as IDock);
		}

		private void ToggleTool(Tool tool)
		{
			if(IsToolVisible(tool)) {
				DockFactory.CloseDockable(tool);
			} else {
				OpenTool(tool);
			}
		}

		public void OpenTool(Tool tool)
		{
			if(!IsToolVisible(tool)) {
				IDockable? visibleTool = DockFactory.FindDockable(DockLayout, x => x is BaseToolContainerViewModel && x != DockFactory.DisassemblyTool && x.Owner is IDock owner && owner.VisibleDockables?.Contains(x) == true);
				if(visibleTool?.Owner is IDock dock) {
					DockFactory.AddDockable(dock, tool);
				} else {
					//Couldn't find any where else to open tool, create a new section
					if(DockLayout.VisibleDockables?.Count > 0 && DockLayout.VisibleDockables[0] is IDock mainDock) {
						DockFactory.SplitToDock(mainDock, new ToolDock {
							Proportion = 0.33,
							VisibleDockables = DockFactory.CreateList<IDockable>(tool)
						}, DockOperation.Bottom);
					}
				}
			}

			DockFactory.SetActiveDockable(tool);
			DockFactory.SetFocusedDockable(DockLayout, tool);
		}

		public void InitializeMenu(Window wnd)
		{
			DebuggerConfig cfg = ConfigManager.Config.Debug.Debugger;

			_gotoSubActions = new() {
				new ContextMenuAction() {
					ActionType = ActionType.GoToAddress,
					Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.GoToAddress),
					OnClick = async () => {
						MemoryType memType = CpuType.ToMemoryType();
						int? address = await new GoToWindow(CpuType, memType, DebugApi.GetMemorySize(memType) - 1).ShowCenteredDialog<int?>(wnd);
						if(address != null) {
							ScrollToAddress(address.Value);
						}
					}
				},
				new ContextMenuAction() {
					ActionType = ActionType.GoToProgramCounter,
					Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.GoToProgramCounter),
					OnClick = () => {
						if(Disassembly.ActiveAddress != null) {
							ScrollToAddress(Disassembly.ActiveAddress.Value);
						}
					}
				}
			};

			InitGoToCpuVectorActions();

			ToolbarItems = AddDisposables(GetDebugMenu(wnd, true));
			DebugMenuItems = AddDisposables(GetDebugMenu(wnd, false));

			ToolbarItems.Add(new ContextMenuAction() {
				ActionType = ActionType.GoTo,
				SubActions = _gotoSubActions
			});

			FileMenuItems = AddDisposables(new List<ContextMenuAction>() {
				SaveRomActionHelper.GetSaveRomAction(wnd),
				SaveRomActionHelper.GetSaveRomAsAction(wnd),
				SaveRomActionHelper.GetSaveEditsAsIpsAction(wnd),
				new ContextMenuSeparator(),
				GetCdlActionMenu(wnd),
				GetWorkspaceActionMenu(wnd),
				new ContextMenuSeparator(),
				new ContextMenuAction() {
					ActionType = ActionType.ResetLayout,
					OnClick = () => {
						DockLayout = DockFactory.GetDefaultLayout();
						InitDock();
					}
				},
				new ContextMenuSeparator(),
				new ContextMenuAction() {
					ActionType = ActionType.Exit,
					OnClick = () => wnd.Close()
				}
			});

			OptionMenuItems = AddDisposables(new List<ContextMenuAction>() {
				new ContextMenuAction() {
					ActionType = ActionType.ShowSettingsPanel,
					IsSelected = () => cfg.ShowSettingsPanel,
					Shortcut =  () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.ToggleSettingsPanel),
					OnClick = () => cfg.ShowSettingsPanel = !cfg.ShowSettingsPanel
				},
				new ContextMenuAction() {
					ActionType = ActionType.ShowMemoryMappings,
					IsVisible = () => MemoryMappings != null,
					IsSelected = () => cfg.ShowMemoryMappings,
					OnClick = () => cfg.ShowMemoryMappings = !cfg.ShowMemoryMappings
				},
				new ContextMenuSeparator(),
				new ContextMenuAction() {
					ActionType = ActionType.ShowBreakpointList,
					IsSelected = () => IsToolVisible(DockFactory.BreakpointListTool),
					OnClick = () => ToggleTool(DockFactory.BreakpointListTool)
				},
				new ContextMenuAction() {
					ActionType = ActionType.ShowCallStack,
					IsSelected = () => IsToolVisible(DockFactory.CallStackTool),
					OnClick = () => ToggleTool(DockFactory.CallStackTool)
				},
				new ContextMenuAction() {
					ActionType = ActionType.ShowConsoleStatus,
					IsSelected = () => IsToolVisible(DockFactory.StatusTool),
					OnClick = () => ToggleTool(DockFactory.StatusTool)
				},
				new ContextMenuAction() {
					ActionType = ActionType.ShowControllers,
					IsVisible = () => FunctionList != null,
					IsSelected = () => IsToolVisible(DockFactory.ControllerListTool),
					OnClick = () => ToggleTool(DockFactory.ControllerListTool)
				},
				new ContextMenuAction() {
					ActionType = ActionType.ShowFunctionList,
					IsVisible = () => FunctionList != null,
					IsSelected = () => IsToolVisible(DockFactory.FunctionListTool),
					OnClick = () => ToggleTool(DockFactory.FunctionListTool)
				},
				new ContextMenuAction() {
					ActionType = ActionType.ShowCallerCallee,
					IsVisible = () => CallerCallee != null,
					IsSelected = () => IsToolVisible(DockFactory.CallerCalleeTool),
					OnClick = () => ToggleTool(DockFactory.CallerCalleeTool)
				},
				new ContextMenuAction() {
					ActionType = ActionType.ShowLabelList,
					IsSelected = () => IsToolVisible(DockFactory.LabelListTool),
					OnClick = () => ToggleTool(DockFactory.LabelListTool)
				},
				new ContextMenuAction() {
					ActionType = ActionType.ShowWatchList,
					IsSelected = () => IsToolVisible(DockFactory.WatchListTool),
					OnClick = () => ToggleTool(DockFactory.WatchListTool)
				},
				new ContextMenuSeparator(),
				new ContextMenuAction() {
					ActionType = ActionType.OpenDebugSettings,
					Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.OpenDebugSettings),
					OnClick = () => DebuggerConfigWindow.Open(DebugConfigWindowTab.Debugger, wnd)
				},
			});

			SearchMenuItems = AddDisposables(new List<ContextMenuAction>() {
				new ContextMenuAction() {
					ActionType = ActionType.GoTo,
					SubActions = _gotoSubActions
				},
				new ContextMenuAction() {
					ActionType = ActionType.GoToAll,
					Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.GoToAll),
					OnClick = async () => {
						GoToDestination? dest = await GoToAllWindow.Open(wnd, CpuType, GoToAllOptions.ShowFilesAndConstants, DebugWorkspaceManager.SymbolProvider);
						if(dest != null) {
							if(GetActiveCodeTool() != DockFactory.SourceViewTool && dest.RelativeAddress?.Type == CpuType.ToMemoryType()) {
								//Try to stay in disassembly view if it was the last view used
								ScrollToAddress(dest.RelativeAddress.Value.Address);
							} else if(dest.SourceLocation != null) {
								OpenTool(DockFactory.SourceViewTool);
								SourceView?.ScrollToLocation(dest.SourceLocation.Value);
							} else if(dest.RelativeAddress?.Type == CpuType.ToMemoryType()) {
								ScrollToAddress(dest.RelativeAddress.Value.Address);
							}
						}
					}
				},
				new ContextMenuSeparator(),
				new ContextMenuAction() {
					ActionType = ActionType.Find,
					Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.Find),
					OnClick = () => GetActiveQuickSearch().Open()
				},
				new ContextMenuAction() {
					ActionType = ActionType.FindPrev,
					Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.FindPrev),
					OnClick = () => GetActiveQuickSearch().FindPrev()
				},
				new ContextMenuAction() {
					ActionType = ActionType.FindNext,
					Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.FindNext),
					OnClick = () => GetActiveQuickSearch().FindNext()
				},
				new ContextMenuSeparator(),
				new ContextMenuAction() {
					ActionType = ActionType.FindOccurrences,
					Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.FindOccurrences),
					OnClick = async () => {
						FindAllOccurrencesWindow searchWnd = new();
						string? search = await searchWnd.ShowCenteredDialog<string?>(wnd);
						if(search!= null) {
							DisassemblySearchOptions options = new() { MatchWholeWord = searchWnd.MatchWholeWord, MatchCase = searchWnd.MatchCase };
							FindAllOccurrences(search, options);
						}
					}
				},
			});

			DebugShortcutManager.RegisterActions(wnd, FileMenuItems);
			DebugShortcutManager.RegisterActions(wnd, DebugMenuItems);
			DebugShortcutManager.RegisterActions(wnd, SearchMenuItems);
			DebugShortcutManager.RegisterActions(wnd, OptionMenuItems);
		}

		private void InitGoToCpuVectorActions()
		{
			DebuggerFeatures features = DebugApi.GetDebuggerFeatures(CpuType);

			if(features.CpuVectorCount == 0) {
				return;
			}

			int getAddress(CpuVectorDefinition def)
			{
				if(def.Type == VectorType.Indirect) {
					byte[] vector = DebugApi.GetMemoryValues(CpuType.ToMemoryType(), def.Address, def.Address + 1);
					return vector[0] | (vector[1] << 8);
				} else if(def.Type == VectorType.Direct) {
					return (int)def.Address;
				} else {
					byte[] vector = Array.Empty<byte>();
					if(def.Type == VectorType.x86) {
						vector = DebugApi.GetMemoryValues(CpuType.ToMemoryType(), def.Address, def.Address + 3);
					} else if(def.Type == VectorType.x86WithOffset) {
						byte irqVectorOffset = DebugApi.GetDebuggerFeatures(CpuType).IrqVectorOffset;
						uint baseAddr = (irqVectorOffset + def.Address) * 4;
						vector = DebugApi.GetMemoryValues(CpuType.ToMemoryType(), baseAddr, baseAddr + 3);
					}

					UInt16 ip = (UInt16)(vector[0] | (vector[1] << 8));
					UInt16 cs = (UInt16)(vector[2] | (vector[3] << 8));
					return (cs << 4) + ip;
				}
			}

			Func<DbgShortKeys>? getShortcut(int index)
			{
				switch(index) {
					case 0: return () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.GoToCpuVector1);
					case 1: return () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.GoToCpuVector2);
					case 2: return () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.GoToCpuVector3);
					case 3: return () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.GoToCpuVector4);
					case 4: return () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.GoToCpuVector5);
				}
				return null;
			}

			_gotoSubActions.Add(new ContextMenuSeparator());

			for(int i = 0; i < features.CpuVectorCount; i++) {
				CpuVectorDefinition def = features.CpuVectors[i];
				string name = Utf8Utilities.GetStringFromArray(features.CpuVectors[i].Name);
				_gotoSubActions.Add(new ContextMenuAction() {
					ActionType = ActionType.Custom,
					Shortcut = getShortcut(i),
					HintText = () => $"${getAddress(def):X4}",
					OnClick = () => ScrollToAddress(getAddress(def)),
					CustomText = name
				});
			}
		}

		private List<ContextMenuAction> GetDebugMenu(Control wnd, bool forToolbar)
		{
			List<ContextMenuAction> debugMenu = new();
			debugMenu.AddRange(DebugSharedActions.GetStepActions(wnd, () => CpuType));

			if(!forToolbar) {
				debugMenu.AddRange(new List<ContextMenuAction> {
					new ContextMenuSeparator(),

					new ContextMenuAction() {
						ActionType = ActionType.Reset,
						Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.Reset),
						OnClick = () => ShortcutHandler.Reset()
					},
					new ContextMenuAction() {
						ActionType = ActionType.PowerCycle,
						Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.PowerCycle),
						OnClick = () => ShortcutHandler.PowerCycle()
					},
					new ContextMenuAction() {
						ActionType = ActionType.ReloadRom,
						Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.ReloadRom),
						OnClick = () => ShortcutHandler.ReloadRom()
					}
				});
			}

			return debugMenu;
		}

		private ContextMenuAction GetCdlActionMenu(Window wnd)
		{
			return new ContextMenuAction() {
				ActionType = ActionType.CodeDataLogger,
				IsEnabled = () => CpuType.ToMemoryType().SupportsCdl(),
				SubActions = new() {
					new ContextMenuAction() {
						ActionType = ActionType.ResetCdl,
						Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.ResetCdl),
						OnClick = () => {
							DebugApi.ResetCdl(CpuType.GetPrgRomMemoryType());
							Disassembly.Refresh();
							UpdateCdlStats();
						}
					},
					new ContextMenuSeparator(),
					new ContextMenuAction() {
						ActionType = ActionType.LoadCdl,
						Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.LoadCdl),
						OnClick = async () => {
							string? filename = await FileDialogHelper.OpenFile(ConfigManager.DebuggerFolder, wnd, FileDialogHelper.CdlExt);
							if(filename != null) {
								DebugApi.LoadCdlFile(CpuType.GetPrgRomMemoryType(), filename);
								Disassembly.Refresh();
								UpdateCdlStats();
							}
						}
					},
					new ContextMenuAction() {
						ActionType = ActionType.SaveCdl,
						Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.SaveCdl),
						OnClick = async () => {
							string? filename = await FileDialogHelper.SaveFile(ConfigManager.DebuggerFolder, EmuApi.GetRomInfo().GetRomName() + ".cdl", wnd, FileDialogHelper.CdlExt);
							if(filename != null) {
								DebugApi.SaveCdlFile(CpuType.GetPrgRomMemoryType(), filename);
							}
						}
					},
					new ContextMenuSeparator(),
					new ContextMenuAction() {
						ActionType = ActionType.GenerateRom,
						SubActions = new() {
							new ContextMenuAction() {
								ActionType = ActionType.CdlRomStripUnused,
								OnClick = () => SaveRomActionHelper.SaveRomAs(wnd, false, CdlStripOption.StripUnused)
							},
							new ContextMenuAction() {
								ActionType = ActionType.CdlRomStripUsed,
								OnClick = () => SaveRomActionHelper.SaveRomAs(wnd, false, CdlStripOption.StripUsed)
							}
						}
					},
				}
			};
		}

		private ContextMenuAction GetWorkspaceActionMenu(Window wnd)
		{
			return new ContextMenuAction() {
				ActionType = ActionType.Workspace,
				SubActions = new() {
					new ContextMenuAction() {
						ActionType = ActionType.ImportLabels,
						Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.ImportLabels),
						OnClick = async () => {
							string? filename = await FileDialogHelper.OpenFile(null, wnd, FileDialogHelper.LabelFileExt);
							if(filename != null) {
								DebugWorkspaceManager.LoadSupportedFile(filename, true);
							}
						}
					},
					new ContextMenuAction() {
						ActionType = ActionType.ExportLabels,
						Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.ExportLabels),
						OnClick = async () => {
							string initFilename = EmuApi.GetRomInfo().GetRomName() + "." + FileDialogHelper.MesenLabelExt;
							string? filename = await FileDialogHelper.SaveFile(null, initFilename, wnd, FileDialogHelper.MesenLabelExt);
							if(filename != null) {
								MesenLabelFile.Export(filename);
							}
						}
					},

					new ContextMenuSeparator(),

					new ContextMenuAction() {
						ActionType = ActionType.ImportWatchEntries,
						Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.ImportWatchEntries),
						OnClick = async () => {
							string? filename = await FileDialogHelper.OpenFile(null, wnd, FileDialogHelper.WatchFileExt);
							if(filename != null) {
								WatchManager.GetWatchManager(CpuType).Import(filename);
							}
						}
					},
					new ContextMenuAction() {
						ActionType = ActionType.ExportWatchEntries,
						Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.ExportWatchEntries),
						OnClick = async () => {
							string? filename = await FileDialogHelper.SaveFile(null, null, wnd, FileDialogHelper.WatchFileExt);
							if(filename != null) {
								WatchManager.GetWatchManager(CpuType).Export(filename);
							}
						}
					},

					new ContextMenuSeparator(),
					new ContextMenuAction() {
						ActionType = ActionType.ResetWorkspace,
						Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.ResetWorkspace),
						OnClick = async () => {
							if(await MesenMsgBox.Show(wnd, "ResetWorkspaceConfirmation", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK) {
								DebugWorkspaceManager.Workspace.Reset();
							}
						}
					},
				}
			};
		}

		private BaseToolContainerViewModel GetActiveCodeTool()
		{
			if(DockLayout.ActiveDockable == DockFactory.SourceViewTool) {
				return DockFactory.SourceViewTool;
			} else if(DockLayout.ActiveDockable == DockFactory.DisassemblyTool) {
				return DockFactory.DisassemblyTool;
			} else if(IsToolActive(DockFactory.DisassemblyTool)) {
				return DockFactory.DisassemblyTool;
			} else if(IsToolActive(DockFactory.SourceViewTool)) {
				return DockFactory.SourceViewTool;
			}
			return DockFactory.DisassemblyTool;
		}

		private QuickSearchViewModel GetActiveQuickSearch()
		{
			if(DockLayout.ActiveDockable == DockFactory.SourceViewTool) {
				return SourceView?.QuickSearch ?? Disassembly.QuickSearch;
			} else if(DockLayout.ActiveDockable == DockFactory.DisassemblyTool) {
				return Disassembly.QuickSearch;
			} else if(IsToolActive(DockFactory.DisassemblyTool)) {
				return Disassembly.QuickSearch;
			} else if(IsToolActive(DockFactory.SourceViewTool)) {
				return SourceView?.QuickSearch ?? Disassembly.QuickSearch;
			}
			return Disassembly.QuickSearch;
		}

		public void Step(StepType type, int instructionCount = 1)
		{
			switch(type) {
				case StepType.PpuStep:
				case StepType.PpuScanline:
				case StepType.PpuFrame:
					DebugApi.Step(CpuType.GetConsoleType().GetMainCpuType(), instructionCount, type);
					break;

				default:
					DebugApi.Step(CpuType, instructionCount, type);
					break;
			}
		}

		public void FindAllOccurrences(string search, DisassemblySearchOptions options)
		{
			if(!options.MatchCase) {
				search = search.ToLower();
			}

			if(SourceView != null && GetActiveCodeTool() == DockFactory.SourceViewTool) {
				FindResultList.SetResults(SourceView.FindAllOccurrences(search, options));
			} else {
				CodeLineData[] results = DebugApi.FindOccurrences(CpuType, search.Trim(), options);
				FindResultList.SetResults(results.Select(x => new FindResultViewModel(x)));
			}
		}

		public void RunToLocation(LocationInfo actionLocation)
		{
			AddressInfo? addr = actionLocation.AbsAddress ?? actionLocation.RelAddress;
			if(addr == null) {
				return;
			}

			BreakpointManager.AddTemporaryBreakpoint(new Breakpoint() {
				CpuType = CpuType,
				MemoryType = addr.Value.Type,
				BreakOnExec = true,
				Enabled = true,
				StartAddress = (uint)addr.Value.Address,
				EndAddress = (uint)addr.Value.Address
			});

			if(EmuApi.IsPaused()) {
				DebugApi.ResumeExecution();
			}
		}
	}
}
