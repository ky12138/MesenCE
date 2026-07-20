using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Selection;
using Avalonia.Media;
using Mesen.Config;
using Mesen.Debugger.Labels;
using Mesen.Debugger.Utilities;
using Mesen.Debugger.Windows;
using Mesen.Interop;
using Mesen.Utilities;
using Mesen.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;

namespace Mesen.Debugger.ViewModels
{
	public partial class CallStackViewModel : DisposableViewModel
	{
		public CpuType CpuType { get; }
		public DebuggerWindowViewModel Debugger { get; }

		[ObservableProperty] public partial MesenList<StackInfo> CallStackContent { get; private set; } = new();
		[ObservableProperty] public partial SelectionModel<StackInfo?> Selection { get; set; } = new();
		public List<int> ColumnWidths { get; } = ConfigManager.Config.Debug.Debugger.CallStackColumnWidths;

		private StackFrameInfo[] _stackFrames = Array.Empty<StackFrameInfo>();

		[Obsolete("For designer only")]
		public CallStackViewModel() : this(CpuType.Snes, new()) { }

		public CallStackViewModel(CpuType cpuType, DebuggerWindowViewModel debugger)
		{
			Debugger = debugger;
			CpuType = cpuType;
		}

		public void UpdateCallStack()
		{
			_stackFrames = DebugApi.GetCallstack(CpuType);
			RefreshCallStack();
		}

		public void RefreshCallStack()
		{
			CallStackContent.Replace(GetStackInfo());
		}

		private List<StackInfo> GetStackInfo()
		{
			StackFrameInfo[] stackFrames = _stackFrames;

			List<StackInfo> stack = new List<StackInfo>();
			for(int i = 0; i < stackFrames.Length; i++) {

				AddressInfo pcAbsAddr = stackFrames[i].AbsSource;
				AddressInfo pcRelAddr = DebugApi.GetRelativeAddress(pcAbsAddr, CpuType);
				bool isMapped = pcRelAddr.Address >= 0;
				stack.Insert(0, new StackInfo() {
					EntryPointStr = GetEntryPointStr(i == 0 ? null : stackFrames[i - 1]),
					EntryPointAbsAddr = i == 0 ? null : stackFrames[i - 1].AbsTarget,
					PcAbsAddr = pcAbsAddr,
					PcRelAddr = pcRelAddr,
					RowBrush = isMapped ? AvaloniaProperty.UnsetValue : Brushes.Gray,
					RowStyle = isMapped ? FontStyle.Normal : FontStyle.Italic
				});
			}

			//Add current location
			AddressInfo curPcRelAddr = new AddressInfo() { Address = (int)DebugApi.GetProgramCounter(CpuType, true), Type = CpuType.ToMemoryType() };
			stack.Insert(0, new StackInfo() {
				EntryPointStr = GetEntryPointStr(stackFrames.Length > 0 ? stackFrames[^1] : null),
				EntryPointAbsAddr = stackFrames.Length > 0 ? stackFrames[^1].AbsTarget : null,
				PcAbsAddr = DebugApi.GetAbsoluteAddress(curPcRelAddr),
				PcRelAddr = curPcRelAddr
			});

			return stack;
		}

		private string GetEntryPointStr(StackFrameInfo? stackFrame)
		{
			if(stackFrame == null) {
				return "[bottom of stack]";
			}

			StackFrameInfo entry = stackFrame.Value;
			CodeLabel? label = entry.AbsTarget.Address >= 0 ? LabelManager.GetLabel(entry.AbsTarget) : null;
			string entryRelStr = MemoryHelper.GetAddressStr((int)entry.Target, CpuType.ToMemoryType());
			if(label != null) {
				return label.Label + " (" + entryRelStr + ")";
			} else if(entry.Flags == StackFrameFlags.Nmi) {
				return "[nmi] " + entryRelStr;
			} else if(entry.Flags == StackFrameFlags.Irq) {
				return "[irq] " + entryRelStr;
			}
			return entryRelStr;
		}

		private string GetHintText(bool isAbs = false)
		{
			if(Selection.SelectedItem is StackInfo entry && entry.EntryPointAbsAddr != null) {
				if(entry.EntryPointAbsAddr.Value.Address >= 0 && isAbs) {
					return MemoryHelper.GetAddressStr(entry.PcAbsAddr);
				} else {
					return MemoryHelper.GetAddressStr(entry.PcRelAddr, true, true);
				}
			}
			return "";
		}
		public void InitContextMenu(Control parent)
		{
			AddDisposables(DebugShortcutManager.CreateContextMenu(parent, new object[] {
				new ContextMenuAction() {
					ActionType = ActionType.EditLabel,
					HintText= () => GetHintText(),
					Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.CallStack_EditLabel),
					IsEnabled = () => Selection.SelectedItem is StackInfo entry && entry.EntryPointAbsAddr != null,
					OnClick = () => {
						if(Selection.SelectedItem is StackInfo entry && entry.EntryPointAbsAddr != null) {
							CodeLabel? label = LabelManager.GetLabel(entry.EntryPointAbsAddr.Value);
							if(label != null) {
								LabelEditWindow.EditLabel(CpuType, parent, label);
							}
							label = new CodeLabel(entry.EntryPointAbsAddr.Value);
						}
					}
				},
				new ContextMenuAction() {
					ActionType = ActionType.GoToLocation,
					HintText= () => GetHintText(),
					Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.CallStack_GoToLocation),
					IsEnabled = () => Selection.SelectedItem is StackInfo entry && entry.PcRelAddr.Address >= 0,
					OnClick = () => {
						if(Selection.SelectedItem is StackInfo entry && entry.PcRelAddr.Address >= 0) {
							Debugger.ScrollToAddress(entry.PcRelAddr.Address);
						}
					}
				},
				new ContextMenuAction() {
					ActionType = ActionType.LocateInFunctionList,
					HintText= () => GetHintText(),
					Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.CodeWindow_GoToLocation),
					IsEnabled = () => Selection.SelectedItem is StackInfo entry && entry.PcAbsAddr.Address >= 0 && Debugger.FunctionList != null,
					OnClick = () => {
						if(Selection.SelectedItem is StackInfo entry && entry.PcAbsAddr.Address >= 0) {
							FunctionListViewModel.ShowInFunctionList(entry.PcAbsAddr);
						}
					}
				},
				new ContextMenuSeparator(),
				new ContextMenuAction() {
					ActionType = ActionType.ViewInMemoryViewer,
					HintText= () => GetHintText(),
					Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.CallStack_GoToLocation),
					IsEnabled = () => Selection.SelectedItem is StackInfo entry && entry.PcRelAddr.Address >= 0,
					OnClick = () => {
						if(Selection.SelectedItem is StackInfo entry && entry.PcRelAddr.Address >= 0) {
							MemoryToolsWindow.ShowInMemoryTools(entry.PcRelAddr.Type, entry.PcRelAddr.Address);
						}
					}
				},
				new ContextMenuAction() {
					ActionType = ActionType.ViewInMemoryViewer,
					HintText= () => GetHintText(true),
					Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.CallStack_GoToLocation),
					IsEnabled = () => Selection.SelectedItem is StackInfo entry && entry.PcAbsAddr.Address >= 0,
					OnClick = () => {
						if(Selection.SelectedItem is StackInfo entry && entry.PcAbsAddr.Address >= 0) {
							MemoryToolsWindow.ShowInMemoryTools(entry.PcAbsAddr.Type, entry.PcAbsAddr.Address);
						}
					}
				},
			}));
		}
	}

	public class StackInfo
	{
		public string EntryPointStr { get; set; } = "";

		public string PcRelAddressStr => MemoryHelper.GetAddressStr(PcRelAddr);
		public string PcAbsAddressStr => MemoryHelper.GetAddressStr(PcAbsAddr);

		public AddressInfo? EntryPointAbsAddr { get; set; }

		// public UInt32 PcRelAddress { get; set; }
		public AddressInfo PcAbsAddr { get; set; }
		public AddressInfo PcRelAddr { get; set; }

		public object RowBrush { get; set; } = AvaloniaProperty.UnsetValue;
		public FontStyle RowStyle { get; set; }
	}
}
