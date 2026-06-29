using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Mesen.Config;
using Mesen.Debugger.Windows;
using Mesen.Interop;
using Mesen.Localization;
using Mesen.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mesen.Debugger.Controls
{
	public record class MemoryMappingBlock
	{
		public string Name = "";
		public int Length;
		public Color Color;
		public string Note = "";
		public int Page = -1;
	}

	public partial class MemoryMappingViewer : Control
	{
		public static readonly StyledProperty<List<MemoryMappingBlock>> MappingsProperty = AvaloniaProperty.Register<MemoryMappingViewer, List<MemoryMappingBlock>>(nameof(Mappings));
		public static readonly StyledProperty<MemoryType> MemTypeProperty = AvaloniaProperty.Register<MemoryMappingViewer, MemoryType>(nameof(MemType));
		private MemoryMappingBlock? _prevTooltipMapping;
		private const int BlockHeight = 32;

		public List<MemoryMappingBlock> Mappings
		{
			get { return GetValue(MappingsProperty); }
			set { SetValue(MappingsProperty, value); }
		}

		public MemoryType MemType
		{
			get { return GetValue(MemTypeProperty); }
			set { SetValue(MemTypeProperty, value); }
		}

		static MemoryMappingViewer()
		{
			AffectsRender<MemoryMappingViewer>(MappingsProperty);
			AffectsMeasure<MemoryMappingViewer>(MappingsProperty);
		}

		public MemoryMappingViewer()
		{
			ColorHelper.InvalidateControlOnThemeChange(this);
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			if(Mappings == null) {
				return new Size();
			}
			return new Size(availableSize.Width, BlockHeight);
		}

		protected override void OnPointerMoved(PointerEventArgs e)
		{
			base.OnPointerMoved(e);

			var (hoveredMapping, start) = GetBlockAtPosition(e.GetCurrentPoint(this).Position.X);

			if(_prevTooltipMapping == hoveredMapping) {
				return;
			}

			_prevTooltipMapping = hoveredMapping;

			if(hoveredMapping != null) {
				TooltipEntries entries = new TooltipEntries();
				DynamicTooltip dynTooltip = new DynamicTooltip();
				entries.AddEntry(ResourceHelper.GetMessage("MemMap_Entry"), GetBlockText(hoveredMapping));
				int end = start + hoveredMapping.Length - 1;
				entries.AddEntry(ResourceHelper.GetMessage("MemMap_Range", MemType.GetShortName()), "$" + start.ToString("X4") + " - $" + end.ToString("X4"));
				entries.AddEntry(ResourceHelper.GetMessage("MemMap_Size", MemType.GetShortName()), "$" + hoveredMapping.Length.ToString("X4") + " (" + FormatSize(hoveredMapping.Length) + ")");

				AddressInfo absStart = DebugApi.GetAbsoluteAddress(new AddressInfo() { Address = start, Type = MemType });
				AddressInfo absEnd = DebugApi.GetAbsoluteAddress(new AddressInfo() { Address = end, Type = MemType });
				if(absStart.Address >= 0 && absEnd.Address >= 0 && absStart.Type == absEnd.Type) {
					int absTotalSize = DebugApi.GetMemorySize(absStart.Type);
					entries.AddEntry(ResourceHelper.GetMessage("MemMap_Range", absStart.Type.GetShortName()), "$" + absStart.Address.ToString("X4") + " - $" + absEnd.Address.ToString("X4"));
					entries.AddEntry(ResourceHelper.GetMessage("MemMap_TotalSize", absStart.Type.GetShortName()),"$" + absTotalSize.ToString("X4") + " (" + FormatSize(absTotalSize) + ")");
				}

				if(hoveredMapping.Note.StartsWith("RW")) {
					entries.AddEntry(ResourceHelper.GetMessage("MemMap_Access"), ResourceHelper.GetMessage("MemMap_AccessReadWrite"));
				} else if(hoveredMapping.Note.StartsWith("R")) {
					entries.AddEntry(ResourceHelper.GetMessage("MemMap_Access"), ResourceHelper.GetMessage("MemMap_AccessReadOnly"));
				} else if(hoveredMapping.Note.StartsWith("W")) {
					entries.AddEntry(ResourceHelper.GetMessage("MemMap_Access"), ResourceHelper.GetMessage("MemMap_AccessWriteOnly"));
				} else if(hoveredMapping.Note.StartsWith("OB")) {
					entries.AddEntry(ResourceHelper.GetMessage("MemMap_Access"), ResourceHelper.GetMessage("MemMap_AccessOpenBus"));
				}
				dynTooltip.Items = entries;

				TooltipHelper.ShowTooltip(this, dynTooltip, 1);
			} else {
				TooltipHelper.HideTooltip(this);
			}
		}

		protected override void OnPointerPressed(PointerPressedEventArgs e)
		{
			base.OnPointerPressed(e);

			if(e.ClickCount == 2 && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) {
				var (block, startAddress) = GetBlockAtPosition(e.GetCurrentPoint(this).Position.X);
				if(block == null) {
					return;
				}

				CpuType cpuType = MemType.ToCpuType();
				if(MemType == cpuType.ToMemoryType()) {
					DebuggerWindow.OpenWindowAtAddress(cpuType, startAddress);
				} else {
					MemoryToolsWindow.ShowInMemoryTools(MemType, startAddress);
				}
			}
		}

		protected override void OnPointerExited(PointerEventArgs e)
		{
			base.OnPointerExited(e);
			_prevTooltipMapping = null;
			TooltipHelper.HideTooltip(this);
		}

		private (MemoryMappingBlock? block, int startAddress) GetBlockAtPosition(double x)
		{
			List<MemoryMappingBlock> mappings = new(Mappings);
			int totalSize = mappings.Sum(m => m.Length);
			double pixelsPerByte = Bounds.Size.Width / totalSize;

			double pos = 0;
			int start = 0;
			foreach(MemoryMappingBlock mapping in mappings) {
				pos += mapping.Length * pixelsPerByte;
				if(pos >= x) {
					return (mapping, start);
				}
				start += mapping.Length;
			}
			return (null, 0);
		}

		private FormattedText GetFormattedText(string text, Typeface typeface, double size)
		{
			return new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, size, ColorHelper.GetBrush(Colors.Black));
		}

		public override void Render(DrawingContext context)
		{
			if(Mappings == null) {
				return;
			}

			List<MemoryMappingBlock> mappings = new(Mappings);

			int totalSize = mappings.Sum(m => m.Length);

			Size size = Bounds.Size;

			double pixelsPerByte = size.Width / totalSize;

			int start = 0;
			double x = 0;
			Typeface typeface = new Typeface(ConfigManager.Config.Preferences.MesenFont.FontFamily);
			Pen borderPen = ColorHelper.GetPen(Color.FromRgb(0x60, 0x60, 0x60));
			for(int i = 0; i < mappings.Count; i++) {
				MemoryMappingBlock block = mappings[i];

				double blockWidth = Math.Round(block.Length * pixelsPerByte);
				if(i == mappings.Count - 1) {
					blockWidth = Bounds.Width - x - 1;
				}

				context.DrawRectangle(ColorHelper.GetBrush(block.Color), borderPen, new Rect(x - 0.5, 0.5, blockWidth + 1, BlockHeight));
				string blockText = GetBlockText(block);
				var text = GetFormattedText(blockText, typeface, 12);
				FormattedText? addressText = GetFormattedText(start.ToString("X4"), typeface, 11);
				double margin = addressText.Height;

				if(text.Width >= blockWidth - margin) {
					//Hide name if there's no space
					text = GetFormattedText(block.Page >= 0 ? $"${block.Page:X2}" : blockText, typeface, 12);
				}

				if(text.Width >= blockWidth - margin) {
					//Hide address text if there's no space
					margin = 0;
					addressText = null;
				}

				if(text.Width < blockWidth - margin) {
					context.DrawText(text, new Point(x + (blockWidth + margin - text.Width) / 2, (BlockHeight - text.Height) / 2));
				}

				if(addressText != null && addressText.Height < blockWidth - 4) {
					using var rotate = context.PushTransform(Matrix.CreateRotation(-Math.PI / 2));
					context.DrawText(addressText, new Point(-BlockHeight + (BlockHeight - addressText.Width) / 2, x));
				}

				if(!string.IsNullOrEmpty(block.Note)) {
					var noteText = GetFormattedText(block.Note, typeface, 9);
					if(noteText.Width < blockWidth - 15) {
						context.DrawText(noteText, new Point(x + blockWidth - noteText.Width - 3, BlockHeight - noteText.Height));
					}
				}

				start += block.Length;
				x += blockWidth;
			}
		}

		private static string GetBlockText(MemoryMappingBlock block)
		{
			if(string.IsNullOrEmpty(block.Name)) {
				return block.Page >= 0 ? $"${block.Page:X2}" : "";
			} else if(block.Page >= 0) {
				return $"{block.Name} (${block.Page:X2})";
			} else {
				return block.Name;
			}
		}

		private static string FormatSize(int bytes)
		{
			if(bytes >= 1024 * 1024) {
				return (bytes / (1024.0 * 1024.0)).ToString("0.##") + " MB";
			} else if(bytes >= 1024) {
				return (bytes / 1024.0).ToString("0.##") + " KB";
			}
			return bytes.ToString() + " bytes";
		}

	}
}
