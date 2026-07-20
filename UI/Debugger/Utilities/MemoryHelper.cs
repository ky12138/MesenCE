using Mesen.Interop;
using Mesen.Debugger.Labels;
using Mesen.Localization;
using System;
using System.Collections.Generic;

namespace Mesen.Debugger.Utilities
{
	public static class MemoryHelper
	{
		public static string GetAddrStr(AddressInfo addr, bool withMemType = true, bool withPage = false)
		{
			if(addr.Address < 0) {
				return "";
			}
			string format = addr.Type.GetFormatString();
			string page = withPage ? GetPageText(addr) : "";
			string memType = withMemType ? addr.Type.GetShortName() + " " : "";
			return page + memType + $"${addr.Address.ToString(format)}";
		}
		public static string GetAddrStr(int addr, MemoryType mem, bool withMemType = true, bool withPage = false)
		{
			if(addr < 0) {
				return "";
			}
			string format = mem.GetFormatString();
			string page = withPage ? GetPageText(addr, mem) : "";
			string memType = withMemType ? mem.GetShortName() + " " : "";
			return page + memType + $"${addr.ToString(format)}";
		}

		// Build the "page:relAddr" display for a relative (CPU) address from its
		// cached page + address. Shared by function rows (RelAddressCacheEntry) and
		// access-range rows (AccessRange) so both format identically; either can
		// persist just the two ints and rebuild the string here on demand.
		public static string FormatRelDisplay(int? relPage, int? relAddress, CpuType cpuType)
		{
			if(relAddress == null || relAddress.Value < 0) {
				return "";
			}
			MemoryType cpuMem = cpuType.ToMemoryType();
			string pageText = relPage.HasValue && relPage.Value >= 0 ? relPage.Value.ToString("X2") + ":" : "";
			return pageText + GetAddrStr(relAddress.Value, cpuMem, false, false);
		}

		public static string GetAddrRangeStr(AddressInfo addr, uint range, bool isAddrHigh = false, bool withMemType = true, bool withPage = false)
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

		// Unified range formatter for the caller/callee memory/ROM access panel.
		// Three shapes, depending on length/stride:
		//   interval>1 && length>1 : $START×LEN step$INTERVAL   (stride run)
		//   length<=1               : $START                    (single address)
		//   else                    : $START-$END               (END = START+(LEN-1)*INTERVAL)
		public static string GetRangeIntervalStr(MemoryType mem, uint start, uint length, uint interval, bool withMemType = false, bool withPage = false)
		{
			string format = mem.GetFormatString();
			string page = withPage ? GetPageText((int)start, mem) : "";
			string memType = withMemType ? mem.GetShortName() + " " : "";
			string addrStr;
			CodeLabel? label = LabelManager.GetLabel(start, mem);
			string labelStr = label?.Label ?? ("$" + start.ToString(format));
			if(interval > 1 && length > 1) {
				addrStr = $"{labelStr}×{length} step:0x{interval.ToString("X")}";
			} else if(length <= 1) {
				addrStr = labelStr;
			} else {
				string end = label?.Label != null
					? $"~0x{(length - 1).ToString("x")}"
					: "-$" + (start + (length - 1) * (interval > 0 ? interval : 1)).ToString(format);
				addrStr = labelStr + end;
			}
			return page + memType + addrStr;
		}

		public static string GetFunctionName(AddressInfo addr, bool isLabel = false, bool withMemType = false, bool withPage = false)
		{
			CodeLabel? label = LabelManager.GetLabel(addr);
			return label?.Label
				?? (isLabel
					? ResourceHelper.GetMessage("lblNoLabel")
					: GetAddrStr(addr, withMemType, withPage)
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

		public static string GetPageText(AddressInfo absAddr, CpuType cpuType)
		{
			int page = GetPage(absAddr, cpuType);
			return page != -1 ? page.ToString("X2") + ":" : "";
		}

		// Page sizes are a stable per-ROM property (mapper/banking layout), so we
		// cache the value returned by GetPageSize and only re-query on ROM load.
		// This keeps GetPage a pure integer division with zero P/Invoke in the hot
		// path, replacing the old GetConsoleState/GetPpuState serialization.
		private static readonly Dictionary<MemoryType, int> PageSizeCache = new();
		private static readonly NotificationListener RomLoadListener = new NotificationListener();
		static MemoryHelper()
		{
			RomLoadListener.OnNotification += (e) => {
				if(e.NotificationType == ConsoleNotificationType.GameLoaded) {
					PageSizeCache.Clear();
				}
			};
		}

		private static int GetCachedPageSize(MemoryType memType)
		{
			if(PageSizeCache.TryGetValue(memType, out int size)) {
				return size;
			}
			int s = DebugApi.GetPageSize(memType);
			PageSizeCache[memType] = s;
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
				case CpuType.Snes: return absAddr.Address / 0x400;
				case CpuType.Ws: return absAddr.Address / 0x10000;
				case CpuType.Sms: return absAddr.Address / 0x400;
				case CpuType.Pce: return DebugApi.GetAbsoluteAddressPage(absAddr, cpuType);
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
	}
}
