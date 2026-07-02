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
using Mesen.Utilities;
using Mesen.ViewModels;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Mesen.Debugger.ViewModels
{
	public class CallerCalleeViewModel : DisposableViewModel
	{
		public CpuType CpuType { get; }
		public DebuggerWindowViewModel Debugger { get; }

		[Reactive] public MesenList<CallerCalleeEntryModel> Callers { get; private set; } = new();
		[Reactive] public MesenList<CallerCalleeEntryModel> Callees { get; private set; } = new();
		[Reactive] public SelectionModel<CallerCalleeEntryModel?> CallerSelection { get; set; } = new() { SingleSelect = true };
		[Reactive] public SelectionModel<CallerCalleeEntryModel?> CalleeSelection { get; set; } = new() { SingleSelect = true };
		[Reactive] public string SelectedFunctionName { get; private set; } = "";

		public List<int> CallerColumnWidths { get; } = new() { 100, 80, 80 };
		public List<int> CalleeColumnWidths { get; } = new() { 100, 80, 80 };

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
				Callers.Replace(new List<CallerCalleeEntryModel>());
				Callees.Replace(new List<CallerCalleeEntryModel>());
				return;
			}

			CodeLabel? label = LabelManager.GetLabel(funcAddr);
			string format = "X" + CpuType.GetAddressSize();
			SelectedFunctionName = label != null
				? label.Label + " ($" + funcAddr.Address.ToString(format) + ")"
				: "$" + funcAddr.Address.ToString(format);

			CallerCalleeRecord record = DebugApi.GetCallerCallee(CpuType, funcAddr);

			List<CallerCalleeEntryModel> callers = new();
			for(int i = 0; i < record.CallerCount && i < 64; i++) {
				CallerCalleeEntry entry = record.Callers[i];
				if(entry.Address.Address >= 0) {
					callers.Add(new CallerCalleeEntryModel {
						FuncAddr = entry.Address,
						FunctionName = GetFunctionName(entry.Address),
						AddressDisplay = "$" + entry.Address.Address.ToString(_format),
						CallCount = entry.CallCount.ToString(),
						CallCountValue = entry.CallCount
					});
				}
			}

			List<CallerCalleeEntryModel> callees = new();
			for(int i = 0; i < record.CalleeCount && i < 64; i++) {
				CallerCalleeEntry entry = record.Callees[i];
				if(entry.Address.Address >= 0) {
					callees.Add(new CallerCalleeEntryModel {
						FuncAddr = entry.Address,
						FunctionName = GetFunctionName(entry.Address),
						AddressDisplay = "$" + entry.Address.Address.ToString(_format),
						CallCount = entry.CallCount.ToString(),
						CallCountValue = entry.CallCount
					});
				}
			}

			Callers.Replace(callers);
			Callees.Replace(callees);
		}

		private string GetFunctionName(AddressInfo addr)
		{
			CodeLabel? label = LabelManager.GetLabel(addr);
			if(label != null) {
				return label.Label;
			}
			return "$" + addr.Address.ToString(_format);
		}

		public void InitContextMenu(Control parent)
		{
			AddDisposables(DebugShortcutManager.CreateContextMenu(parent, new object[] {
				new ContextMenuAction() {
					ActionType = ActionType.GoToLocation,
					IsEnabled = () => CallerSelection.SelectedItem != null || CalleeSelection.SelectedItem != null,
					OnClick = () => {
						CallerCalleeEntryModel? selected = CallerSelection.SelectedItem ?? CalleeSelection.SelectedItem;
						if(selected != null && selected.FuncAddr.Address >= 0) {
							int relAddr = DebugApi.GetRelativeAddress(selected.FuncAddr, CpuType).Address;
							if(relAddr >= 0) {
								Debugger.ScrollToAddress(relAddr);
							}
						}
					}
				},
			}));
		}
	}

	public class CallerCalleeEntryModel
	{
		public AddressInfo FuncAddr { get; set; }
		public string FunctionName { get; set; } = "";
		public string AddressDisplay { get; set; } = "";
		public string CallCount { get; set; } = "";
		public UInt64 CallCountValue { get; set; }
	}
}
