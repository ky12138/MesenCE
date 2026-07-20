using Mesen.Interop;
using System.Collections.Generic;

namespace Mesen.Debugger.ViewModels
{
	public class RelAddressCacheData
	{
		public Dictionary<CpuType, List<RelAddressCacheEntry>> CacheByCpu { get; set; } = new();
	}

	public class RelAddressCacheEntry
	{
		public int Address { get; set; }
		public MemoryType Type { get; set; }
		public string Display { get; set; } = "";
	}
}