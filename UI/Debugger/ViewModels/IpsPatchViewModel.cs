using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Selection;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Input.Platform;
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
using System.IO;
using System.Linq;
using System.Text;

namespace Mesen.Debugger.ViewModels
{
	public partial class IpsPatchViewModel : DisposableViewModel
	{
		[ObservableProperty] public partial string PatchFilePath { get; set; } = "";
		[ObservableProperty] public partial string RomFilePath { get; set; } = "";
		[ObservableProperty] public partial int RecordCount { get; set; }
		[ObservableProperty] public partial int TotalChangedBytes { get; set; }
		[ObservableProperty] public partial int TruncateOffset { get; set; } = -1;
		[ObservableProperty] public partial bool HasTruncateOffset { get; set; }
		[ObservableProperty] public partial MesenList<IpsRecordViewModel> Records { get; set; } = new();
		[ObservableProperty] public partial SelectionModel<IpsRecordViewModel?> Selection { get; set; } = new() { SingleSelect = false };
		[ObservableProperty] public partial SortState SortState { get; set; } = new();
		[ObservableProperty] public partial string StatusText { get; set; } = "";

		[ObservableProperty] public partial List<ContextMenuAction> FileMenuItems { get; set; } = new();
		[ObservableProperty] public partial List<ContextMenuAction> ViewMenuItems { get; set; } = new();

		// Highlight ranges: (start address, length, color)
		public List<(int Start, int Length, Color Color)> HighlightRanges { get; } = new();

		// Parsed records for passing to MemoryTools
		public List<ParsedIpsRecord> ParsedRecords { get; } = new();

		private Window? _ownerWindow;

		[Obsolete("For designer only")]
		public IpsPatchViewModel() { }

		public void Sort(object? param)
		{
			UpdateRecords();
		}

		private Dictionary<string, Func<IpsRecordViewModel, IpsRecordViewModel, int>> _comparers = new() {
			{ "Index",       (a, b) => a.Index.CompareTo(b.Index) },
			{ "Memory",      (a, b) => string.Compare(a.MemoryDisplay, b.MemoryDisplay, StringComparison.OrdinalIgnoreCase) },
			{ "Address",     (a, b) => a.Address.CompareTo(b.Address) },
			{ "TargetOffset",(a, b) => a.TargetOffset.CompareTo(b.TargetOffset) },
			{ "Label",       (a, b) => string.Compare(a.LabelDisplay, b.LabelDisplay, StringComparison.OrdinalIgnoreCase) },
			{ "Length",      (a, b) => a.EffectiveLength.CompareTo(b.EffectiveLength) },
			{ "Type",        (a, b) => string.Compare(a.TypeDisplay, b.TypeDisplay, StringComparison.OrdinalIgnoreCase) },
			{ "Preview",     (a, b) => string.Compare(a.DataPreview, b.DataPreview, StringComparison.OrdinalIgnoreCase) },
		};

		private void UpdateRecords()
		{
			List<int> selectedIndexes = Selection.SelectedIndexes.ToList();

			List<IpsRecordViewModel> sorted = Records.ToList();
			if(SortState.SortOrder.Count > 0) {
				SortHelper.SortList(sorted, SortState.SortOrder, _comparers, "Index");
			}

			Records.Replace(sorted);
			Selection.SelectIndexes(selectedIndexes, Records.Count);
		}

		public IpsPatchViewModel(Window ownerWindow)
		{
			_ownerWindow = ownerWindow;

			RomInfo romInfo = EmuApi.GetRomInfo();
			RomFilePath = romInfo.RomPath;

			InitMenus();

			// Auto-load same-name .ips file from ROM directory
			string romPath = RomFilePath;
			if(!string.IsNullOrEmpty(romPath)) {
				string ipsPath = Path.ChangeExtension(romPath, ".ips");
				if(File.Exists(ipsPath)) {
					LoadPatchFile(ipsPath);
				}
			}
		}

		private void InitMenus()
		{
			FileMenuItems = AddDisposables(new List<ContextMenuAction>() {
				new ContextMenuAction() {
					ActionType = ActionType.Custom,
					CustomText = ResourceHelper.GetViewLabel(nameof(IpsPatchWindow), "mnuOpenPatch"),
					DynamicIcon = () => "Folder",
					OnClick = () => OpenPatchFile()
				},
				new ContextMenuSeparator(),
				new ContextMenuAction() {
					ActionType = ActionType.Custom,
					CustomText = ResourceHelper.GetViewLabel(nameof(IpsPatchWindow), "mnuSaveIps"),
					DynamicIcon = () => "SaveFloppy",
					IsEnabled = () => Records.Count > 0,
					OnClick = () => SaveIpsFile()
				},
				new ContextMenuSeparator(),
				new ContextMenuAction() {
					ActionType = ActionType.Exit,
					OnClick = () => _ownerWindow?.Close()
				}
			});

			ViewMenuItems = AddDisposables(new List<ContextMenuAction>() {
				new ContextMenuAction() {
					ActionType = ActionType.SelectAll,
					OnClick = () => Selection.SelectAll()
				}
			});
		}

		public async void OpenPatchFile()
		{
			string? filename = await FileDialogHelper.OpenFile(null, _ownerWindow, FileDialogHelper.IpsExt);
			if(filename != null) {
				LoadPatchFile(filename);
			}
		}

		public void LoadPatchFile(string path)
		{
			try {
				byte[] ipsData = File.ReadAllBytes(path);
				PatchFilePath = path;
				ParseIps(ipsData);
			} catch(Exception ex) {
				StatusText = $"Error: {ex.Message}";
			}
		}

		private void ParseIps(byte[] ipsData)
		{
			Records.Clear();
			HighlightRanges.Clear();
			ParsedRecords.Clear();
			TruncateOffset = -1;
			HasTruncateOffset = false;

			if(ipsData.Length < 5) {
				StatusText = "Invalid IPS file: too short";
				return;
			}

			// Verify header "PATCH"
			if(ipsData[0] != 'P' || ipsData[1] != 'A' || ipsData[2] != 'T' || ipsData[3] != 'C' || ipsData[4] != 'H') {
				StatusText = "Invalid IPS file: bad header";
				return;
			}

			// Determine NES ROM boundaries from current ROM
			int headerSize = 16;
			int trainerSize = 0;
			int prgSize = DebugApi.GetMemorySize(MemoryType.NesPrgRom);
			int chrSize = DebugApi.GetMemorySize(MemoryType.NesChrRom);

			if(prgSize > 0) {
				// NES ROM: check for trainer flag in header byte 6 bit 2
				byte[] header = DebugApi.GetRomHeader();
				if(header.Length >= 16) {
					trainerSize = (header[6] & 0x04) != 0 ? 512 : 0;
				}
			}

			int prgStart = headerSize + trainerSize;
			int chrStart = prgStart + prgSize;
			bool isNes = prgSize > 0;

			int offset = 5;
			int recordIndex = 1;
			int totalBytes = 0;

			while(offset < ipsData.Length) {
				// Check for EOF marker
				if(offset + 3 <= ipsData.Length &&
					ipsData[offset] == 'E' && ipsData[offset + 1] == 'O' && ipsData[offset + 2] == 'F') {
					offset += 3;
					// Read optional truncate offset (3 bytes big-endian)
					if(offset + 3 <= ipsData.Length) {
						TruncateOffset = (ipsData[offset] << 16) | (ipsData[offset + 1] << 8) | ipsData[offset + 2];
						HasTruncateOffset = true;
					}
					break;
				}

				if(offset + 5 > ipsData.Length) {
					StatusText = "Invalid IPS file: truncated record";
					break;
				}

				// Read address (3 bytes big-endian)
				uint address = (uint)((ipsData[offset] << 16) | (ipsData[offset + 1] << 8) | ipsData[offset + 2]);
				offset += 3;

				// Read length (2 bytes big-endian)
				ushort length = (ushort)((ipsData[offset] << 8) | ipsData[offset + 1]);
				offset += 2;

				// Map IPS file offset to correct MemoryType
				MemoryType targetMemory;
				int targetOffset;
				int effectiveLen;

				if(length == 0) {
					// RLE record
					if(offset + 3 > ipsData.Length) {
						StatusText = "Invalid IPS file: truncated RLE record";
						break;
					}
					ushort rleCount = (ushort)((ipsData[offset] << 8) | ipsData[offset + 1]);
					byte rleValue = ipsData[offset + 2];
					offset += 3;
					effectiveLen = rleCount;

					MapIpsAddress(isNes, address, effectiveLen, prgStart, prgSize, chrStart, chrSize,
						out targetMemory, out targetOffset);

					var vm = new IpsRecordViewModel(recordIndex, address, 0, true, rleCount, rleValue,
						Array.Empty<byte>(), targetMemory, targetOffset);
					Records.Add(vm);
					HighlightRanges.Add(((int)address, rleCount, vm.HighlightColor));
					ParsedRecords.Add(new ParsedIpsRecord(address, 0, true, rleCount, rleValue,
						Array.Empty<byte>(), targetMemory, targetOffset));
					totalBytes += rleCount;
				} else {
					// Standard record
					if(offset + length > ipsData.Length) {
						StatusText = "Invalid IPS file: truncated data record";
						break;
					}
					byte[] data = new byte[length];
					Array.Copy(ipsData, offset, data, 0, length);
					offset += length;
					effectiveLen = length;

					MapIpsAddress(isNes, address, effectiveLen, prgStart, prgSize, chrStart, chrSize,
						out targetMemory, out targetOffset);

					var vm = new IpsRecordViewModel(recordIndex, address, length, false, 0, 0,
						data, targetMemory, targetOffset);
					Records.Add(vm);
					HighlightRanges.Add(((int)address, length, vm.HighlightColor));
					ParsedRecords.Add(new ParsedIpsRecord(address, length, false, 0, 0,
						data, targetMemory, targetOffset));
					totalBytes += length;
				}

				recordIndex++;
			}

			RecordCount = Records.Count;
			TotalChangedBytes = totalBytes;
			StatusText = $"Loaded {RecordCount} records, {TotalChangedBytes} total changed bytes";
		}

		private static void MapIpsAddress(bool isNes, uint ipsAddr, int length,
			int prgStart, int prgSize, int chrStart, int chrSize,
			out MemoryType targetMemory, out int targetOffset)
		{
			if(isNes) {
				if(ipsAddr >= prgStart && ipsAddr < prgStart + prgSize) {
					targetMemory = MemoryType.NesPrgRom;
					targetOffset = (int)ipsAddr - prgStart;
					return;
				} else if(chrSize > 0 && ipsAddr >= chrStart && ipsAddr < chrStart + chrSize) {
					targetMemory = MemoryType.NesChrRom;
					targetOffset = (int)ipsAddr - chrStart;
					return;
				}
			}
			// Fallback: assume PRG with header offset
			targetMemory = isNes ? MemoryType.NesPrgRom : MemoryType.None;
			targetOffset = isNes ? (int)ipsAddr - prgStart : (int)ipsAddr;
		}

		public void InitContextMenu(Control parent)
		{
			AddDisposables(DebugShortcutManager.CreateContextMenu(parent, new object[] {
				new ContextMenuAction() {
					ActionType = ActionType.ViewInMemoryViewer,
					HintText = () => GetHintText(),
					IsEnabled = () => Selection.SelectedItem != null,
					OnClick = () => ViewSelectedInMemoryTools()
				},
				new ContextMenuSeparator(),
				new ContextMenuAction() {
					ActionType = ActionType.EditSelectedCode,
					HintText = () => GetHintText(),
					IsEnabled = () => Selection.SelectedItem is IpsRecordViewModel r && !r.IsRle && r.Data.Length > 0,
					OnClick = () => EditWithAssembler()
				},
				new ContextMenuAction() {
					ActionType = ActionType.ToggleBreakpoint,
					HintText = () => GetHintText(),
					IsEnabled = () => Selection.SelectedItem != null,
					OnClick = () => EditBreakpointForRecord()
				},
				new ContextMenuAction() {
					ActionType = ActionType.EditLabel,
					HintText = () => GetHintText(),
					IsEnabled = () => Selection.SelectedItem != null,
					OnClick = () => EditLabelForRecord()
				},
				new ContextMenuSeparator(),
				new ContextMenuAction() {
					ActionType = ActionType.Copy,
					IsEnabled = () => Selection.SelectedItem != null,
					OnClick = () => CopyAddress()
				},
			}));
		}

		private string GetHintText()
		{
			return Selection.SelectedItem is IpsRecordViewModel record
				? MemoryHelper.GetAddressStr(record.TargetAddress)
				: "";
		}
		private void ViewSelectedInMemoryTools()
		{
			if(Selection.SelectedItem is IpsRecordViewModel record) {
				// Filter ParsedRecords to same memory type as the selected record
				var sameMemoryRecords = ParsedRecords.Where(r => r.TargetMemory == record.TargetMemory).ToList();
				MemoryToolsWindow wnd = DebugWindowManager.GetOrOpenDebugWindow(() => new MemoryToolsWindow());
				wnd.SetCursorPositionWithIpsHighlight(record.TargetMemory, record.TargetOffset, sameMemoryRecords);
			}
		}

		private void EditBreakpointForRecord()
		{
			if(Selection.SelectedItem is IpsRecordViewModel record && _ownerWindow != null) {
				CpuType cpuType = record.TargetMemory.ToCpuType();
				BreakpointManager.EditBreakpointAtRange(record.TargetAddress, (uint)record.EffectiveLength, cpuType, _ownerWindow);
			}
		}

		public void EditLabelForRecord()
		{
			if(Selection.SelectedItem is IpsRecordViewModel record && _ownerWindow != null) {
				CpuType cpuType = record.TargetMemory.ToCpuType();
				var label = new CodeLabel(new AddressInfo() { Address = record.TargetOffset, Type = record.TargetMemory }) {
					Length = (uint)record.EffectiveLength
				};
				LabelEditWindow.EditLabel(cpuType, _ownerWindow, label);
			}
		}

		public void RefreshLabels()
		{
			foreach(var record in Records) {
				record.RefreshLabel();
			}
		}

		private void CopyAddress()
		{
			if(Selection.SelectedItem is IpsRecordViewModel record) {
				var clipboard = _ownerWindow?.Clipboard;
				clipboard?.SetTextAsync(record.AddressDisplay);
			}
		}

		public void EditRecordWithAssembler(IpsRecordViewModel record)
		{
			if(record.IsRle || record.Data.Length == 0) {
				return;
			}
			EditWithAssemblerCore(record);
		}

		private void EditWithAssembler()
		{
			if(Selection.SelectedItem is IpsRecordViewModel record && !record.IsRle && record.Data.Length > 0) {
				EditWithAssemblerCore(record);
			}
		}

		private void EditWithAssemblerCore(IpsRecordViewModel record)
		{
			if(_ownerWindow == null) return;

			// Write current record data to ROM memory so assembler can read it
			DebugApi.SetMemoryValues(record.TargetMemory, (uint)record.TargetOffset,
				record.Data, record.Data.Length);

			// Get CPU bus address from the physical ROM offset
			CpuType cpuType = record.TargetMemory.ToCpuType();
			AddressInfo cpuAddr = DebugApi.GetRelativeAddress(record.TargetAddress, cpuType);
			int address = cpuAddr.Address >= 0 ? cpuAddr.Address : record.TargetOffset;

			// Build assembly preview from the bytes
			string code = ".db " + string.Join(" ", record.Data.Select(b => "$" + b.ToString("X2")));

			// Open assembler targeting NesMemory (CPU bus address space)
			AssemblerWindow.EditCode(cpuType, address, code, record.Data.Length);

			// Poll for assembler window closure, then read back changes
			IpsRecordViewModel trackedRecord = record;
			DispatcherTimer timer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(200) };
			timer.Tick += (s, e) => {
				AssemblerWindow? asmWnd = DebugWindowManager.GetDebugWindow<AssemblerWindow>(w => true);
				if(asmWnd == null) {
					timer.Stop();
					// Read back the modified bytes from ROM memory
					byte[] newBytes = DebugApi.GetMemoryValues(trackedRecord.TargetMemory,
						(uint)trackedRecord.TargetOffset,
						(uint)(trackedRecord.TargetOffset + trackedRecord.EffectiveLength - 1));
					if(newBytes != null && newBytes.Length > 0) {
						trackedRecord.UpdateData(newBytes);
						SyncParsedRecord(trackedRecord);
						UpdateHighlights(trackedRecord);
						StatusText = $"Record #{trackedRecord.Index} updated via assembler";
					}
				}
			};
			timer.Start();
		}

		private void SyncParsedRecord(IpsRecordViewModel record)
		{
			var parsed = ParsedRecords.FirstOrDefault(r =>
				r.TargetMemory == record.TargetMemory && r.TargetOffset == record.TargetOffset);
			if(parsed != null) {
				parsed.Data = record.Data;
				parsed.Length = (ushort)record.Data.Length;
			}
		}

		private void UpdateHighlights(IpsRecordViewModel record)
		{
			// Update the highlight range for this record
			for(int i = 0; i < HighlightRanges.Count; i++) {
				if(HighlightRanges[i].Start == record.TargetOffset) {
					HighlightRanges[i] = (record.TargetOffset, record.EffectiveLength, record.HighlightColor);
					break;
				}
			}
		}

		private async void SaveIpsFile()
		{
			if(_ownerWindow == null || Records.Count == 0) return;

			string defaultName = !string.IsNullOrEmpty(PatchFilePath) ? Path.GetFileName(PatchFilePath) : "patch.ips";
			string? filename = await FileDialogHelper.SaveFile(ConfigManager.DebuggerFolder, defaultName, _ownerWindow, FileDialogHelper.IpsExt);
			if(filename != null) {
				byte[] ipsBinary = SerializeRecordsToIps();
				File.WriteAllBytes(filename, ipsBinary);
				StatusText = $"Saved {Path.GetFileName(filename)} ({Records.Count} records)";
			}
		}

		private byte[] SerializeRecordsToIps()
		{
			using var ms = new MemoryStream();
			using var bw = new BinaryWriter(ms);

			// "PATCH" header
			bw.Write(new byte[] { (byte)'P', (byte)'A', (byte)'T', (byte)'C', (byte)'H' });

			foreach(var record in Records) {
				// Write address (3 bytes big-endian)
				bw.Write((byte)((record.Address >> 16) & 0xFF));
				bw.Write((byte)((record.Address >> 8) & 0xFF));
				bw.Write((byte)(record.Address & 0xFF));

				if(record.IsRle) {
					// RLE: length=0, then 2-byte count + 1-byte value
					bw.Write((ushort)0);
					bw.Write(record.RleRepeatCount);
					bw.Write(record.RleValue);
				} else {
					// Standard record
					bw.Write(record.Length);
					bw.Write(record.Data);
				}
			}

			// "EOF" marker
			bw.Write(new byte[] { (byte)'E', (byte)'O', (byte)'F' });

			return ms.ToArray();
		}
	}

	// Parsed IPS record for passing to MemoryTools highlighting
	public class ParsedIpsRecord
	{
		public uint Address { get; }
		public ushort Length { get; set; }
		public bool IsRle { get; }
		public ushort RleRepeatCount { get; }
		public byte RleValue { get; }
		public byte[] Data { get; set; }

		public MemoryType TargetMemory { get; }
		public int TargetOffset { get; }

		public int EffectiveLength => IsRle ? RleRepeatCount : Length;

		public ParsedIpsRecord(uint address, ushort length, bool isRle,
			ushort rleRepeatCount, byte rleValue, byte[] data,
			MemoryType targetMemory, int targetOffset)
		{
			Address = address;
			Length = length;
			IsRle = isRle;
			RleRepeatCount = rleRepeatCount;
			RleValue = rleValue;
			Data = data ?? Array.Empty<byte>();
			TargetMemory = targetMemory;
			TargetOffset = targetOffset;
		}
	}
}
