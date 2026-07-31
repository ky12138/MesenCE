using Avalonia.Controls;
using Avalonia.Controls.Selection;
using CommunityToolkit.Mvvm.ComponentModel;
using Mesen.Config;
using Mesen.Debugger.Utilities;
using Mesen.Debugger.Windows;
using Mesen.Interop;
using Mesen.Utilities;
using Mesen.ViewModels;
using System;
using System.Collections.Generic;

namespace Mesen.Debugger.ViewModels
{
	public partial class CpuRegisterAccessViewModel : DisposableViewModel
	{
		public CpuType CpuType { get; }
		public DebuggerWindowViewModel Debugger { get; }

		[ObservableProperty] public partial MesenList<RegWriteInfo> WriteEntries { get; private set; } = new();
		[ObservableProperty] public partial SelectionModel<RegWriteInfo?> Selection { get; set; } = new();
		[ObservableProperty] public partial int HistorySize { get; set; } = 3;
		public List<int> ColumnWidths { get; } = ConfigManager.Config.Debug.Debugger.RegWriteHistoryColumnWidths;

		[Obsolete("For designer only")]
		public CpuRegisterAccessViewModel() : this(CpuType.Snes, new()) { }

		public CpuRegisterAccessViewModel(CpuType cpuType, DebuggerWindowViewModel debugger)
		{
			Debugger = debugger;
			CpuType = cpuType;

			if(!Design.IsDesignMode) {
				HistorySize = Math.Clamp(ConfigManager.Config.Debug.Debugger.RegWriteHistorySize, 1, 5);
				DebugApi.SetRegisterWriteHistorySize((uint)HistorySize);
			}
		}

		partial void OnHistorySizeChanged(int value)
		{
			if(Design.IsDesignMode) {
				return;
			}

			int size = Math.Clamp(value, 1, 5);
			ConfigManager.Config.Debug.Debugger.RegWriteHistorySize = size;
			DebugApi.SetRegisterWriteHistorySize((uint)size);
			UpdateHistory();
		}

		public void UpdateHistory()
		{
			RegisterWriteEntry[] entries = DebugApi.GetRegisterWriteHistory(CpuType);

			//Group by register, most recent write first within each register
			Array.Sort(entries, (a, b) => a.RegisterId != b.RegisterId ? a.RegisterId.CompareTo(b.RegisterId) : b.Sequence.CompareTo(a.Sequence));

			string addrFormat = "X" + CpuType.GetAddressSize();
			List<RegWriteInfo> rows = new(entries.Length);
			foreach(RegisterWriteEntry entry in entries) {
				rows.Add(new RegWriteInfo(entry, CpuType, addrFormat));
			}
			WriteEntries.Replace(rows);
		}

		public void GoToEntry(RegWriteInfo entry)
		{
			if(entry.RelAddress >= 0) {
				Debugger.ScrollToAddress(entry.RelAddress);
			}
		}

		public void InitContextMenu(Control parent)
		{
			AddDisposables(DebugShortcutManager.CreateContextMenu(parent, new object[] {
				new ContextMenuAction() {
					ActionType = ActionType.GoToLocation,
					IsEnabled = () => Selection.SelectedItem is RegWriteInfo entry && entry.RelAddress >= 0,
					OnClick = () => {
						if(Selection.SelectedItem is RegWriteInfo entry) {
							GoToEntry(entry);
						}
					}
				},
				new ContextMenuAction() {
					ActionType = ActionType.ViewInMemoryViewer,
					IsEnabled = () => Selection.SelectedItem is RegWriteInfo entry && entry.RelAddress >= 0,
					OnClick = () => {
						if(Selection.SelectedItem is RegWriteInfo entry && entry.RelAddress >= 0) {
							MemoryToolsWindow.ShowInMemoryTools(CpuType.ToMemoryType(), entry.RelAddress);
						}
					}
				},
				new ContextMenuSeparator(),
				new ContextMenuAction() {
					ActionType = ActionType.Reset,
					OnClick = () => {
						DebugApi.ResetRegisterWriteHistory();
						UpdateHistory();
					}
				}
			}));
		}
	}

	public class RegWriteInfo
	{
		public string Register { get; }
		public string AddressStr { get; }
		public string Disassembly { get; }
		public string ValueChange { get; }
		public uint HitCount { get; }
		public int RelAddress { get; }

		public RegWriteInfo(RegisterWriteEntry entry, CpuType cpuType, string addrFormat)
		{
			Register = entry.GetRegisterName();
			HitCount = entry.HitCount;

			//Prefer remapping the absolute address, in case bank mappings changed since the write
			int relAddress = (int)entry.RelativeAddress;
			if(entry.AbsAddress >= 0) {
				AddressInfo absAddr = new AddressInfo() { Address = entry.AbsAddress, Type = entry.AbsMemType };
				AddressInfo relAddr = DebugApi.GetRelativeAddress(absAddr, cpuType);
				if(relAddr.Address >= 0) {
					relAddress = relAddr.Address;
				}
			}
			RelAddress = relAddress;
			AddressStr = "$" + relAddress.ToString(addrFormat);

			string disassembly = "";
			if(relAddress >= 0) {
				CodeLineData[] lines = DebugApi.GetDisassemblyOutput(cpuType, (uint)relAddress, 1);
				if(lines.Length > 0) {
					disassembly = lines[0].Text.Trim();
				}
			}
			Disassembly = disassembly;

			string valueFormat = "X" + (entry.ValueSize * 2);
			ValueChange = "$" + entry.OldValue.ToString(valueFormat) + " -> $" + entry.NewValue.ToString(valueFormat);
		}
	}
}
