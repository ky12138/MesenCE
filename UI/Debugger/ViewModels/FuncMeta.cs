using Mesen.Interop;
using System.Collections.Generic;

namespace Mesen.Debugger.ViewModels
{
	/// <summary>函数维度的标记元数据（按 AddressInfo 索引，运行时存在于内存字典）</summary>
	public class FuncMeta
	{
		public string? FunctionColor { get; set; }
		public bool Blocked { get; set; }
		public bool Marked { get; set; }
		public FuncMemoryAccess? MemoryAccess { get; set; }

		// NES PRG page mapping snapshots collected across function calls.
		// null when not applicable or not yet sampled.
		public List<List<int>>? PrgMapSnapshots { get; set; }

		// NES CHR page mapping snapshots collected across function calls.
		// Only populated when the cartridge has CHR-ROM.
		// null when not applicable or not yet sampled.
		public List<List<int>>? ChrMapSnapshots { get; set; }

		public bool HasPrgMapping => PrgMapSnapshots?.Count > 0;
		public bool HasChrMapping => ChrMapSnapshots?.Count > 0;

		public bool HasData => FunctionColor != null || Blocked || Marked
			|| MemoryAccess != null || HasPrgMapping || HasChrMapping;
	}
}
