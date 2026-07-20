using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Selection;
using Avalonia.Threading;
using Avalonia.VisualTree;
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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace Mesen.Debugger.ViewModels
{
	public partial class ProfilerWindowViewModel : DisposableViewModel
	{
		[ObservableProperty] public partial List<ProfilerTab> ProfilerTabs { get; set; } = new List<ProfilerTab>();
		[ObservableProperty] public partial ProfilerTab? SelectedTab { get; set; } = null;

		[ObservableProperty] public partial List<ContextMenuAction> ToolbarItems { get; private set; } = new();
		[ObservableProperty] public partial List<ContextMenuAction> DebugMenuActions { get; private set; } = new();

		public List<object> FileMenuActions { get; } = new();
		public List<object> ViewMenuActions { get; } = new();

		public ProfilerConfig Config { get; }

		public ProfilerWindowViewModel(Window? wnd)
		{
			Config = ConfigManager.Config.Debug.Profiler;

			if(Design.IsDesignMode) {
				return;
			}

			UpdateAvailableTabs();

			AddDisposable(this.ObserveProp(nameof(SelectedTab), () => {
				if(SelectedTab != null && EmuApi.IsPaused()) {
					RefreshData();
				}
			}));

			FileMenuActions = AddDisposables(new List<object>() {
				new ContextMenuAction() {
					ActionType = ActionType.ResetProfilerData,
					OnClick = () => SelectedTab?.ResetData()
				},
				new ContextMenuAction() {
					ActionType = ActionType.CopyToClipboard,
					Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.Copy),
					OnClick = () => wnd?.GetVisualDescendants().Where(a => a is DataBox).Cast<DataBox>().FirstOrDefault()?.CopyToClipboard()
				},
				new ContextMenuSeparator(),
				new ContextMenuAction() {
					ActionType = ActionType.Exit,
					OnClick = () => wnd?.Close()
				}
			});

			ViewMenuActions = AddDisposables(new List<object>() {
				new ContextMenuAction() {
					ActionType = ActionType.Refresh,
					Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.Refresh),
					OnClick = () => RefreshData()
				},
				new ContextMenuSeparator(),
				new ContextMenuAction() {
					ActionType = ActionType.EnableAutoRefresh,
					IsSelected = () => Config.AutoRefresh,
					OnClick = () => Config.AutoRefresh = !Config.AutoRefresh
				},
				new ContextMenuAction() {
					ActionType = ActionType.RefreshOnBreakPause,
					IsSelected = () => Config.RefreshOnBreakPause,
					OnClick = () => Config.RefreshOnBreakPause = !Config.RefreshOnBreakPause
				}
			});

			if(Design.IsDesignMode || wnd == null) {
				return;
			}

			DebugShortcutManager.RegisterActions(wnd, FileMenuActions);
			DebugShortcutManager.RegisterActions(wnd, ViewMenuActions);

			DebugMenuActions = AddDisposables(DebugSharedActions.GetStepActions(wnd, () => SelectedTab?.CpuType ?? CpuType.Snes));
			ToolbarItems = AddDisposables(DebugSharedActions.GetStepActions(wnd, () => SelectedTab?.CpuType ?? CpuType.Snes));

			DebugShortcutManager.RegisterActions(wnd, DebugMenuActions);

			InitContextMenu(wnd);

			LabelManager.OnLabelUpdated += LabelManager_OnLabelUpdated;
		}

		protected override void DisposeView()
		{
			LabelManager.OnLabelUpdated -= LabelManager_OnLabelUpdated;
		}

		private void LabelManager_OnLabelUpdated(object? sender, EventArgs e)
		{
			ProfilerTab tab = (SelectedTab ?? ProfilerTabs[0]);
			Dispatcher.UIThread.Post(() => {
				tab?.RefreshGrid();
			});
		}

		private void InitContextMenu(Window wnd)
		{
			AddDisposables(DebugShortcutManager.CreateContextMenu(wnd, new object[] {
				new ContextMenuAction() {
					ActionType = ActionType.EditLabel,
					IsEnabled = () => GetSelectedFuncAddr() != null,
					OnClick = () => {
						AddressInfo? addr = GetSelectedFuncAddr();
						if(addr != null) {
							CpuType cpuType = SelectedTab?.CpuType ?? CpuType.Snes;
							CodeLabel? label = LabelManager.GetLabel(addr.Value);
							LabelEditWindow.EditLabel(cpuType, wnd, label ?? new CodeLabel(addr.Value));
						}
					}
				},

				new ContextMenuSeparator(),

				new ContextMenuAction() {
					ActionType = ActionType.ToggleBreakpoint,
					IsEnabled = () => GetSelectedFuncAddr() != null,
					OnClick = () => {
						AddressInfo? addr = GetSelectedFuncAddr();
						if(addr != null) {
							BreakpointManager.EditBreakpointAtAddress(addr.Value, SelectedTab?.CpuType ?? CpuType.Snes, wnd);
						}
					}
				},

				new ContextMenuSeparator(),

				new ContextMenuAction() {
					ActionType = ActionType.GoToLocation,
					IsEnabled = () => GetSelectedRelAddr() >= 0,
					OnClick = () => {
						int relAddr = GetSelectedRelAddr();
						if(relAddr >= 0) {
							DebuggerWindow.OpenWindowAtAddress(SelectedTab?.CpuType ?? CpuType.Snes, relAddr);
						}
					}
				},

				new ContextMenuAction() {
					ActionType = ActionType.ViewInMemoryViewer,
					IsEnabled = () => GetSelectedFuncAddr() != null,
					OnClick = () => {
						AddressInfo? addr = GetSelectedFuncAddr();
						if(addr != null) {
							CpuType cpuType = SelectedTab?.CpuType ?? CpuType.Snes;
							int relAddr = DebugApi.GetRelativeAddress(addr.Value, cpuType).Address;
							AddressInfo memAddr = new AddressInfo() { Address = relAddr, Type = cpuType.ToMemoryType() };
							if(memAddr.Address < 0) {
								memAddr = addr.Value;
							}
							MemoryToolsWindow.ShowInMemoryTools(memAddr.Type, memAddr.Address);
						}
					}
				},
			}));
		}

		private AddressInfo? GetSelectedFuncAddr()
		{
			if(SelectedTab?.Selection.SelectedItem is ProfiledFunctionViewModel vm) {
				return vm.FuncAddr;
			}
			return null;
		}

		private int GetSelectedRelAddr()
		{
			AddressInfo? addr = GetSelectedFuncAddr();
			if(addr != null) {
				return DebugApi.GetRelativeAddress(addr.Value, SelectedTab?.CpuType ?? CpuType.Snes).Address;
			}
			return -1;
		}

		public void UpdateAvailableTabs()
		{
			List<ProfilerTab> tabs = new();
			foreach(CpuType type in EmuApi.GetRomInfo().CpuTypes) {
				if(type.SupportsCallStack()) {
					tabs.Add(new ProfilerTab() {
						TabName = ResourceHelper.GetEnumText(type),
						CpuType = type
					});
				}
			}

			ProfilerTabs = tabs;
			SelectedTab = tabs[0];
		}

		public void RefreshData()
		{
			ProfilerTab tab = (SelectedTab ?? ProfilerTabs[0]);
			tab.RefreshData();
			Dispatcher.UIThread.Post(() => {
				tab.RefreshGrid();
			});
		}
	}

	public partial class ProfilerTab : ObservableObject
	{
		[ObservableProperty] public partial string TabName { get; set; } = "";
		[ObservableProperty] public partial CpuType CpuType { get; set; } = CpuType.Snes;
		[ObservableProperty] public partial MesenList<ProfiledFunctionViewModel> GridData { get; private set; } = new();
		[ObservableProperty] public partial SelectionModel<ProfiledFunctionViewModel> Selection { get; set; } = new();
		[ObservableProperty] public partial SortState SortState { get; set; } = new();
		public ProfilerConfig Config => ConfigManager.Config.Debug.Profiler;
		public List<int> ColumnWidths { get; } = ConfigManager.Config.Debug.Profiler.ColumnWidths;

		private object _updateLock = new();
		private int _dataSize = 0;
		private ProfiledFunction[] _coreProfilerData = new ProfiledFunction[100000];
		private ProfiledFunction[] _profilerData = Array.Empty<ProfiledFunction>();

		private UInt64 _totalCycles;

		public ProfilerTab()
		{
			SortState.SetColumnSort("InclusiveTime", ListSortDirection.Descending, false);
		}

		public ProfiledFunction? GetRawData(int index)
		{
			ProfiledFunction[] data = _profilerData;
			if(index < data.Length) {
				return data[index];
			}
			return null;
		}

		public void ResetData()
		{
			DebugApi.ResetProfiler(CpuType);
			GridData.Clear();
			RefreshData();
			RefreshGrid();
		}

		public void RefreshData()
		{
			lock(_updateLock) {
				_dataSize = DebugApi.GetProfilerData(CpuType, ref _coreProfilerData);
			}
		}

		public void RefreshGrid()
		{
			lock(_updateLock) {
				Array.Resize(ref _profilerData, _dataSize);
				Array.Copy(_coreProfilerData, _profilerData, _dataSize);
			}

			Sort();

			UInt64 totalCycles = 0;
			ProfiledFunction[] profilerData = _profilerData;
			foreach(ProfiledFunction f in profilerData) {
				totalCycles += f.ExclusiveCycles;
			}
			_totalCycles = totalCycles;

			while(GridData.Count < profilerData.Length) {
				GridData.Add(new ProfiledFunctionViewModel());
			}

			for(int i = 0; i < profilerData.Length; i++) {
				GridData[i].Update(profilerData[i], CpuType, _totalCycles);
			}
		}

		public void SortCommand(object? param)
		{
			RefreshGrid();
		}

		public void Sort()
		{
			CpuType cpuType = CpuType;

			Dictionary<string, Func<ProfiledFunction, ProfiledFunction, int>> comparers = new() {
				{ "FunctionName", (a, b) => string.Compare(a.GetFunctionName(cpuType), b.GetFunctionName(cpuType), StringComparison.OrdinalIgnoreCase) },
				{ "CallCount", (a, b) => a.CallCount.CompareTo(b.CallCount) },
				{ "InclusiveTime", (a, b) => a.InclusiveCycles.CompareTo(b.InclusiveCycles) },
				{ "InclusiveTimePercent", (a, b) => a.InclusiveCycles.CompareTo(b.InclusiveCycles) },
				{ "ExclusiveTime", (a, b) => a.ExclusiveCycles.CompareTo(b.ExclusiveCycles) },
				{ "ExclusiveTimePercent", (a, b) => a.ExclusiveCycles.CompareTo(b.ExclusiveCycles) },
				{ "AvgCycles", (a, b) => a.GetAvgCycles().CompareTo(b.GetAvgCycles()) },
				{ "MinCycles", (a, b) => a.MinCycles.CompareTo(b.MinCycles) },
				{ "MaxCycles", (a, b) => a.MaxCycles.CompareTo(b.MaxCycles) },
			};

			SortHelper.SortArray(_profilerData, SortState.SortOrder, comparers, "InclusiveTime");
		}
	}

	public static class ProfiledFunctionExtensions
	{
		public static string GetFunctionName(this ProfiledFunction func, CpuType cpuType)
		{
			string functionName;

			if(func.Address.Address == -1) {
				functionName = "[Reset]";
			} else {
				CodeLabel? label = LabelManager.GetLabel((UInt32)func.Address.Address, func.Address.Type);

				int hexCount = cpuType.GetAddressSize();
				functionName = func.Address.Type.GetShortName() + ": $" + func.Address.Address.ToString("X" + hexCount.ToString());
				if(label != null) {
					functionName = label.Label + " (" + functionName + ")";
				}
			}

			if(func.Flags.HasFlag(StackFrameFlags.Irq)) {
				functionName = "[irq] " + functionName;
			} else if(func.Flags.HasFlag(StackFrameFlags.Nmi)) {
				functionName = "[nmi] " + functionName;
			}

			return functionName;
		}
	}

	public class ProfiledFunctionViewModel : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler? PropertyChanged;

		private string _functionName = "";
		public string FunctionName
		{
			get
			{
				UpdateFields();
				return _functionName;
			}
		}

		public string ExclusiveCycles { get; set; } = "";
		public string InclusiveCycles { get; set; } = "";
		public string CallCount { get; set; } = "";
		public string MinCycles { get; set; } = "";
		public string MaxCycles { get; set; } = "";

		public string ExclusivePercent { get; set; } = "";
		public string InclusivePercent { get; set; } = "";
		public string AvgCycles { get; set; } = "";

		private ProfiledFunction _funcData;
		private CpuType _cpuType;
		private UInt64 _totalCycles;

		public AddressInfo FuncAddr => _funcData.Address;
		public CpuType CurrentCpuType => _cpuType;

		public void Update(ProfiledFunction func, CpuType cpuType, UInt64 totalCycles)
		{
			_funcData = func;
			_cpuType = cpuType;
			_totalCycles = totalCycles;

			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ProfiledFunctionViewModel.FunctionName)));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ProfiledFunctionViewModel.ExclusiveCycles)));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ProfiledFunctionViewModel.InclusiveCycles)));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ProfiledFunctionViewModel.CallCount)));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ProfiledFunctionViewModel.MinCycles)));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ProfiledFunctionViewModel.MaxCycles)));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ProfiledFunctionViewModel.ExclusivePercent)));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ProfiledFunctionViewModel.InclusivePercent)));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ProfiledFunctionViewModel.AvgCycles)));
		}

		private void UpdateFields()
		{
			_functionName = _funcData.GetFunctionName(_cpuType);
			ExclusiveCycles = _funcData.ExclusiveCycles.ToString();
			InclusiveCycles = _funcData.InclusiveCycles.ToString();
			CallCount = _funcData.CallCount.ToString();
			MinCycles = _funcData.MinCycles == UInt64.MaxValue ? "n/a" : _funcData.MinCycles.ToString();
			MaxCycles = _funcData.MaxCycles == 0 ? "n/a" : _funcData.MaxCycles.ToString();

			AvgCycles = (_funcData.CallCount == 0 ? 0 : (_funcData.InclusiveCycles / _funcData.CallCount)).ToString();
			ExclusivePercent = ((double)_funcData.ExclusiveCycles / _totalCycles * 100).ToString("0.00");
			InclusivePercent = ((double)_funcData.InclusiveCycles / _totalCycles * 100).ToString("0.00");
		}
	}
}
