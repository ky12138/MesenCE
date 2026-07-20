using Mesen.Interop;
using Avalonia.Media;
using Mesen.Debugger.Utilities;
using System;
using System.Collections.Generic;

namespace Mesen.Debugger.ViewModels
{
	public class AccessRangeViewModel
	{
		internal readonly AccessRange _range;
		private readonly CpuType _cpuType;
		private readonly DebuggerWindowViewModel _debugger;
		private readonly AddressInfo _absAddr;

		public AddressInfo FuncAddr { get; }
		public bool IsDetail { get; }
		public bool IsExpanded { get; set; }
		public List<AccessRangeViewModel> Children { get; } = new();
		public bool IsExpandable => !IsDetail && _range.Length > 1;

		public uint End => _range.End;
		public uint SpanLength => _range.SpanLength;
		public uint Interval => _range.Interval;

		public AccessRangeViewModel(AccessRange range, CpuType cpuType, DebuggerWindowViewModel debugger, AddressInfo funcAddr = default, bool isDetail = false)
		{
			_range = range;
			_cpuType = cpuType;
			_debugger = debugger;
			FuncAddr = funcAddr;
			IsDetail = isDetail;
			_absAddr = new AddressInfo { Address = (int)_range.Start, Type = _range.MemType };
		}

		public AddressInfo RelAddr
		{
			get
			{
				if(!_range.MemType.IsRomMemory()) return default;
				_debugger.GetOrUpdateRelAddressDisplay(_absAddr, out AddressInfo rel);
				return rel;
			}
		}

		public string RangeDisplay
		{
			get
			{
				string core = MemoryHelper.GetRangeIntervalStr(_range.MemType, _range.Start, _range.Length, _range.Interval);
				string rel = GetRelAddrDisplay();
				if(!string.IsNullOrEmpty(rel)) core = rel + "/" + core;
				string prefix = IsDetail ? "      " : (IsExpandable ? (IsExpanded ? "\u25BE " : "\u25B8 ") : "");
				return prefix + core;
			}
		}

		private string GetRelAddrDisplay()
		{
			if(!_range.MemType.IsRomMemory()) return "";
			if(_range.RelAddress.HasValue)
				return MemoryHelper.FormatRelDisplay(_range.RelPage, _range.RelAddress, _cpuType);

			_debugger.GetOrUpdateRelAddressDisplay(_absAddr, out AddressInfo rel);
			if(rel.Address >= 0 && _debugger.RelAddressCache.TryGetValue(_absAddr, out var cached)) {
				_range.RelPage = cached.RelPage ?? -1;
				_range.RelAddress = cached.RelAddress;
				return MemoryHelper.FormatRelDisplay(_range.RelPage, _range.RelAddress, _cpuType);
			}
			return "";
		}

		public string RwDisplay => _range.Flags switch {
			RwFlags.Read => "R",
			RwFlags.Write => "W",
			RwFlags.ReadWrite => "RW",
			_ => "-"
		};
		public string MemTypeDisplay => _range.MemType.GetShortName();
		public string ReadCountDisplay => _range.ReadCount.ToString();
		public string WriteCountDisplay => _range.WriteCount.ToString();
		public string AccessCountDisplay => _range.AccessCount.ToString();
		public bool IsFromCache => _range.AccessCount == 0;
		public FontStyle RowStyle => IsFromCache ? FontStyle.Italic : FontStyle.Normal;
		public uint Start => _range.Start;
		public uint Length => _range.Length;
		public RwFlags Flags => _range.Flags;
		public MemoryType MemType => _range.MemType;
		public uint ReadCount => _range.ReadCount;
		public uint WriteCount => _range.WriteCount;
		public uint AccessCount => _range.AccessCount;

		public void UpdateCounts(uint read, uint write, uint access)
		{
			_range.ReadCount = read;
			_range.WriteCount = write;
			_range.AccessCount = access;
		}
	}
}
