using Mesen.Interop;
using Mesen.Debugger.Labels;
using Mesen.Localization;
using System;
using System.Collections.Generic;

namespace Mesen.Debugger.Utilities
{
	public static class MemoryHelper
	{
		public static string GetAddressStr(AddressInfo addr, bool withMemType = true, bool withPage = false)
		{
			if(addr.Address < 0) {
				return "";
			}
			string format = addr.Type.GetFormatString();
			string page = withPage ? GetPageText(addr) : "";
			string memType = withMemType ? addr.Type.GetShortName() + " " : "";
			return page + memType + $"${addr.Address.ToString(format)}";
		}
		public static string GetAddressStr(int addr, MemoryType mem, bool withMemType = true, bool withPage = false)
		{
			if(addr < 0) {
				return "";
			}
			string format = mem.GetFormatString();
			string page = withPage ? GetPageText(addr, mem) : "";
			string memType = withMemType ? mem.GetShortName() + " " : "";
			return page + memType + $"$({addr.ToString(format)})";
		}

		public static string GetAddressStr(AddressInfo addr, uint range, bool isAddrHigh = false, bool withMemType = true, bool withPage = false)
		{
			if(addr.Address < 0) {
				return "";
			}
			string format = addr.Type.GetFormatString();
			string page = withPage ? GetPageText(addr) : "";
			string memType = withMemType ? addr.Type.GetShortName() + " " : "";
			string addrStart = isAddrHigh
				? (addr.Address - (int)range).ToString(format)
				: addr.Address.ToString(format);
			return page + memType + $"${addrStart}-${(addr.Address + range).ToString(format)}";
		}
		public static string GetAddressStr(int addr, MemoryType mem, uint range, bool isAddrHigh = false, bool withMemType = true, bool withPage = false)
		{
			if(addr < 0) {
				return "";
			}
			string format = mem.GetFormatString();
			string page = withPage ? GetPageText(addr, mem) : "";
			string memType = withMemType ? mem.GetShortName() + " " : "";
			string addrStart = isAddrHigh
				? (addr - (int)range).ToString(format)
				: addr.ToString(format);
			return page + memType + $"${addrStart}-${(addr + range).ToString(format)}";
		}

		public static string GetFunctionName(AddressInfo addr, bool isLabel = false, bool withMemType = false, bool withPage = false)
		{
			CodeLabel? label = LabelManager.GetLabel(addr);
			return label?.Label
				?? (isLabel
					? ResourceHelper.GetMessage("lblNoLabel")
					: GetAddressStr(addr, withMemType, withPage)
				);
		}
		public static string GetFunctionName(int addr, MemoryType mem, bool isLabel = false, bool withMemType = false, bool withPage = false)
		{
			CodeLabel? label = LabelManager.GetLabel((uint)addr, mem);
			return label?.Label
				?? (isLabel
					? ResourceHelper.GetMessage("lblNoLabel")
					: GetAddressStr(addr, mem, withMemType, withPage)
				);
		}

	public static string GetPageText(int addr, MemoryType mem)
	{
		int page = GetPage(addr, mem);
		return page != -1 ? page.ToString("X2") + ":" : "";
	}
	public static string GetPageText(AddressInfo addr)
	{
		return GetPageText(addr.Address, addr.Type);
	}

	// Page sizes are a stable per-ROM property (mapper/banking layout), so we
	// cache the value returned by GetPageSize and only re-query on ROM load.
	// This keeps GetPage a pure integer division with zero P/Invoke in the hot
	// path, replacing the old GetConsoleState/GetPpuState serialization.
	private static readonly Dictionary<MemoryType, int> _pageSizeCache = new();
	private static readonly NotificationListener _romLoadListener = new NotificationListener();
	static MemoryHelper()
	{
		_romLoadListener.OnNotification += (e) => {
			if(e.NotificationType == ConsoleNotificationType.GameLoaded) {
				_pageSizeCache.Clear();
			}
		};
	}

	private static int GetCachedPageSize(MemoryType memType)
	{
		if(_pageSizeCache.TryGetValue(memType, out int size)) {
			return size;
		}
		int s = DebugApi.GetPageSize(memType);
		_pageSizeCache[memType] = s;
		return s;
	}

	// abs 直算版：给定绝对地址 + CPU 类型，直接算出 page。
	// NES/GB 走缓存的页大小（零 P/Invoke）；SNES/WS/SMS 为纯整数除法；
	// PCE 的 page 是运行时 MPR 窗口寄存器值，无法缓存，回落到 C++ 查询。
	public static int GetPage(AddressInfo absAddr, CpuType cpuType)
	{
		if(absAddr.Address < 0) {
			return -1;
		}
		switch(cpuType) {
			case CpuType.Nes:
			case CpuType.Gameboy: {
				int size = GetCachedPageSize(absAddr.Type);
				return size > 0 ? absAddr.Address / size : -1;
			}
			case CpuType.Snes:    return absAddr.Address / 0x400;
			case CpuType.Ws:      return absAddr.Address / 0x10000;
			case CpuType.Sms:     return absAddr.Address / 0x400;
			case CpuType.Pce:     return DebugApi.GetAbsoluteAddressPage(absAddr, cpuType);
			default: return -1;
		}
	}

	// 兼容旧调用点：int + MemoryType 形式（相对或绝对类型均可）。
	// 相对类型先转回绝对地址再走 abs 直算；这同时修掉了旧实现把绝对类型
	// 直接喂给 C++ rel 版返回 -1 的 bug。
	public static int GetPage(int addr, MemoryType mem)
	{
		if(addr < 0) {
			return -1;
		}
		CpuType cpuType = mem.ToCpuType();
		AddressInfo absAddr = mem.IsRelativeMemory()
			? DebugApi.GetAbsoluteAddress(new AddressInfo { Address = addr, Type = mem })
			: new AddressInfo { Address = addr, Type = mem };
		return absAddr.Address >= 0 ? GetPage(absAddr, cpuType) : -1;
	}

	public static string GetPageText(AddressInfo absAddr, CpuType cpuType)
	{
		int page = GetPage(absAddr, cpuType);
		return page != -1 ? page.ToString("X2") + ":" : "";
	}

}
}
