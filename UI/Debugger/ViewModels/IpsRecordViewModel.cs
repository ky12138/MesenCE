using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Mesen.Interop;
using Mesen.ViewModels;
using Mesen.Debugger.Labels;
using Mesen.Debugger.Utilities;
using System;
using System.Linq;

namespace Mesen.Debugger.ViewModels
{
	public partial class IpsRecordViewModel : ViewModelBase
	{
		public int Index { get; }
		public uint Address { get; }
		public ushort Length { get; }
		public bool IsRle { get; }
		public ushort RleRepeatCount { get; }
		public byte RleValue { get; }
		[ObservableProperty] public partial byte[] Data { get; set; }
		[ObservableProperty] public partial int EffectiveLength { get; set; }

		public MemoryType TargetMemory { get; }
		public int TargetOffset { get; }
		public AddressInfo TargetAddress => new AddressInfo() { Address = TargetOffset, Type = TargetMemory };

		public string AddressDisplay => $"${Address.ToString(TargetMemory.GetFormatString())}";
		public string TargetAddressDisplay => MemoryHelper.GetAddrStr(TargetAddress, false, true);
		public string LengthDisplay => IsRle ? $"RLE x{RleRepeatCount}" : EffectiveLength.ToString();
		public string TypeDisplay => IsRle ? "RLE" : "Data";
		public string MemoryDisplay => TargetMemory switch {
			MemoryType.NesPrgRom => "PRG",
			MemoryType.NesChrRom => "CHR",
			_ => TargetMemory.ToString()
		};
		[ObservableProperty] public partial string DataPreview { get; set; }
		[ObservableProperty] public partial string DisassemblyText { get; set; } = "";
		[ObservableProperty] public partial string LabelDisplay { get; set; }

		// Size-based highlight color
		[ObservableProperty] public partial Color HighlightColor { get; set; }
		public IBrush RowBackgroundBrush => new SolidColorBrush(HighlightColor);

		// Size thresholds for color assignment (standard records)
		private static readonly (int MinBytes, Color Color)[] SizeThresholds = new (int, Color)[]
		{
			(16,    Color.FromArgb(100, 0, 180, 0)),     // Green - tiny changes (<=16 bytes)
			(64,    Color.FromArgb(100, 0, 120, 255)),    // Blue - small changes (17-64 bytes)
			(256,   Color.FromArgb(100, 200, 130, 0)),    // Orange - medium changes (65-256 bytes)
			(1024,  Color.FromArgb(100, 220, 50, 50)),    // Red - large changes (257-1024 bytes)
			(int.MaxValue, Color.FromArgb(100, 180, 0, 220)), // Purple - huge changes (>1024 bytes)
		};

		// Dedicated RLE color
		private static readonly Color RleColor = Color.FromArgb(100, 200, 200, 200);

		public IpsRecordViewModel(int index, uint address, ushort length, bool isRle,
			ushort rleRepeatCount, byte rleValue, byte[] data,
			MemoryType targetMemory, int targetOffset,
			string? disassemblyText = null)
		{
			Index = index;
			Address = address;
			Length = length;
			IsRle = isRle;
			RleRepeatCount = rleRepeatCount;
			RleValue = rleValue;
			Data = data ?? Array.Empty<byte>();
			TargetMemory = targetMemory;
			TargetOffset = targetOffset;
			DisassemblyText = disassemblyText ?? "";

			EffectiveLength = isRle ? rleRepeatCount : length;
			HighlightColor = isRle ? RleColor : GetColorForSize(EffectiveLength);
			DataPreview = BuildPreview();
			LabelDisplay = LookupLabel();
		}

		public void UpdateData(byte[] newData)
		{
			Data = newData ?? Array.Empty<byte>();
			EffectiveLength = IsRle ? RleRepeatCount : newData?.Length ?? 0;
			HighlightColor = IsRle ? RleColor : GetColorForSize(EffectiveLength);
			DataPreview = BuildPreview();
		}

		public void RefreshLabel()
		{
			LabelDisplay = LookupLabel();
		}

		private string LookupLabel()
		{
			if(!TargetMemory.SupportsLabels()) return "";
			CodeLabel? label = LabelManager.GetLabel((uint)TargetOffset, TargetMemory);
			return label?.Label ?? "";
		}

		private static Color GetColorForSize(int byteCount)
		{
			foreach(var (minBytes, color) in SizeThresholds) {
				if(byteCount <= minBytes) {
					return color;
				}
			}
			return SizeThresholds[^1].Color;
		}

		private string BuildPreview()
		{
			if(IsRle) {
				return $"[{RleValue:X2}] x{RleRepeatCount}";
			}

			if(Data.Length == 0) {
				return "";
			}

			// If we have cached disassembly text, show the first few lines as preview
			if(!string.IsNullOrEmpty(DisassemblyText)) {
				var cleanText = DisassemblyText.Replace("\r", "");
				var lines = cleanText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
				int show = Math.Min(lines.Length, 3);
				var preview = string.Join(" | ", lines.Take(show));
				if(lines.Length > 3) preview += " ...";
				return preview;
			}

			// Fallback: raw hex preview
			int previewLen = Math.Min(Data.Length, 16);
			var sb = new System.Text.StringBuilder(previewLen * 3);
			for(int i = 0; i < previewLen; i++) {
				if(i > 0) sb.Append(' ');
				sb.Append(Data[i].ToString("X2"));
			}
			if(Data.Length > 16) {
				sb.Append("...");
			}
			return sb.ToString();
		}
	}
}
