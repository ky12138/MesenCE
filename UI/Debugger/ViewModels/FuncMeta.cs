using Mesen.Interop;

namespace Mesen.Debugger.ViewModels
{
	/// <summary>函数维度的标记元数据（按 AddressInfo 索引，运行时存在于内存字典）</summary>
	public class FuncMeta
	{
		public string? FunctionColor { get; set; }
		public bool Blocked { get; set; }
		public bool Marked { get; set; }
		public FuncMemoryAccess? MemoryAccess { get; set; }

		public bool HasData => FunctionColor != null || Blocked || Marked || MemoryAccess != null;
	}
}
