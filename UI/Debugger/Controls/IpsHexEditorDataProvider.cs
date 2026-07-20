using Avalonia.Media;
using Mesen.Debugger.ViewModels;
using System;
using System.Collections.Generic;

namespace Mesen.Debugger.Controls
{
	/// <summary>
	/// Decorator pattern: wraps an IHexEditorDataProvider and applies
	/// custom background colors for IPS patch modified regions.
	/// Colors are assigned based on record size (byte count).
	/// </summary>
	public class IpsHexEditorDataProvider : IHexEditorDataProvider
	{
		private readonly IHexEditorDataProvider _inner;
		private readonly List<IpsHighlightRange> _highlights;

		// Size-based color thresholds (matching IpsRecordViewModel)
		private static readonly (int MinBytes, Color Color)[] SizeThresholds = new (int, Color)[]
		{
			(16,    Color.FromArgb(100, 0, 180, 0)),      // Green - tiny changes (<=16 bytes)
			(64,    Color.FromArgb(100, 0, 120, 255)),     // Blue - small changes (17-64 bytes)
			(256,   Color.FromArgb(100, 200, 130, 0)),     // Orange - medium changes (65-256 bytes)
			(1024,  Color.FromArgb(100, 220, 50, 50)),     // Red - large changes (257-1024 bytes)
			(int.MaxValue, Color.FromArgb(100, 180, 0, 220)), // Purple - huge changes (>1024 bytes)
		};

		public IpsHexEditorDataProvider(IHexEditorDataProvider inner, List<ParsedIpsRecord> records)
		{
			_inner = inner;
			_highlights = new List<IpsHighlightRange>();

			foreach(var record in records) {
				int length = record.IsRle ? record.RleRepeatCount : record.Length;
				Color color = GetColorForSize(length);
				_highlights.Add(new IpsHighlightRange(record.TargetOffset, length, color));
			}
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

		public void Prepare(int firstByteIndex, int lastByteIndex)
		{
			_inner.Prepare(firstByteIndex, lastByteIndex);
		}

		public ByteInfo GetByte(int byteIndex)
		{
			var info = _inner.GetByte(byteIndex);

			// Check if this byte falls in any IPS highlight range
			foreach(var hl in _highlights) {
				if(byteIndex >= hl.Start && byteIndex < hl.Start + hl.Length) {
					info.BackColor = hl.Color;
					break;
				}
			}

			return info;
		}

		public byte GetRawByte(int byteIndex)
		{
			return _inner.GetRawByte(byteIndex);
		}

		public byte[] GetRawBytes(int start, int length)
		{
			return _inner.GetRawBytes(start, length);
		}

		public int Length => _inner.Length;

		public string ConvertValueToString(UInt64 val, out int keyLength)
		{
			return _inner.ConvertValueToString(val, out keyLength);
		}

		public byte ConvertCharToByte(char c)
		{
			return _inner.ConvertCharToByte(c);
		}

		private readonly record struct IpsHighlightRange(int Start, int Length, Color Color);
	}
}
