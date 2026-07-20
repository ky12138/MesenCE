using Mesen.Interop;
using Mesen.Debugger.Labels;
using Mesen.Localization;
using System;

namespace Mesen.Debugger.Utilities
{
	public static class MemoryHelper
	{
		public static string GetAddressStr(AddressInfo addr, bool withMemType = true, bool withPage = false) {
			if(addr.Address < 0) {
				return "";
			}
			string format = addr.Type.GetFormatString();
			string page = withPage ? GetPageText(addr) : "";
			string memType = withMemType ? addr.Type.GetShortName() + " " : "";
			return page + memType + $"${addr.Address.ToString(format)}";
		}
		public static string GetAddressStr(int addr, MemoryType mem, bool withMemType = true, bool withPage = false) {
			if(addr < 0) {
				return "";
			}
			string format = mem.GetFormatString();
			string page = withPage ? GetPageText(addr,mem) : "";
			string memType = withMemType ? mem.GetShortName() + " " : "";
			return page + memType + $"$({addr.ToString(format)})";
		}

		public static string GetAddressStr(AddressInfo addr, uint range, bool isAddrHigh = false, bool withMemType = true, bool withPage = false) {
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
		public static string GetAddressStr(int addr, MemoryType mem, uint range, bool isAddrHigh = false, bool withMemType = true, bool withPage = false) {
			if(addr < 0) {
				return "";
			}
			string format = mem.GetFormatString();
			string page = withPage ? GetPageText(addr,mem) : "";
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
					: GetAddressStr(addr,withMemType,withPage) 
				);
		}
		public static string GetFunctionName(int addr, MemoryType mem, bool isLabel = false, bool withMemType = false, bool withPage = false)
		{
			CodeLabel? label = LabelManager.GetLabel((uint)addr,mem);
			return label?.Label 
				?? (isLabel 
					? ResourceHelper.GetMessage("lblNoLabel")
					: GetAddressStr(addr,mem,withMemType,withPage) 
				);
		}

		public static string GetPageText(int addr, MemoryType mem) {
			int page = GetPage(addr,mem);
			return page != -1 
				? page.ToString("X2") + ":" 
				: "";
		}
		public static string GetPageText(AddressInfo addr) {
			return GetPageText(addr.Address,addr.Type);
		}
		public static int GetPage(int addr, MemoryType mem)
		{
			try {
				return mem switch {
					MemoryType.NesMemory => GetNesCpuPage(addr),
					MemoryType.NesPpuMemory => GetNesPpuPage(addr),
					MemoryType.GameboyMemory => GetGameboyPage(addr),
					MemoryType.PceMemory => GetPcePage(addr),
					MemoryType.SmsMemory => GetSmsPage(addr),
					MemoryType.WsMemory => GetWsPage(addr),
					_ => -1
				};
			} catch {
				return -1;
			}
		}

		private static int GetNesCpuPage(int addr)
		{
			NesCartridgeState state = DebugApi.GetConsoleState<NesState>(ConsoleType.Nes).Cartridge;
			int index = addr >> 8;
			if(index < 0x40 || index >= 0x100) {
				return -1;
			}
			if(state.PrgMemoryAccess[index] == NesMemoryAccessType.NoAccess) {
				return -1;
			}
			return state.PrgMemoryType[index] switch {
				NesPrgMemoryType.WorkRam => (int)(state.PrgMemoryOffset[index] / state.WorkRamPageSize),
				NesPrgMemoryType.SaveRam => (int)(state.PrgMemoryOffset[index] / state.SaveRamPageSize),
				NesPrgMemoryType.PrgRom => (int)(state.PrgMemoryOffset[index] / state.PrgPageSize),
				_ => -1
			};
		}

		private static int GetNesPpuPage(int addr)
		{
			NesCartridgeState state = DebugApi.GetConsoleState<NesState>(ConsoleType.Nes).Cartridge;
			int index = addr >> 8;
			if(index < 0x00 || index >= 0x40) {
				return -1;
			}
			if(state.ChrMemoryAccess[index] == NesMemoryAccessType.NoAccess) {
				return -1;
			}
			int page = state.ChrMemoryType[index] switch {
				NesChrMemoryType.NametableRam => (int)(state.ChrMemoryOffset[index] / 0x400),
				NesChrMemoryType.ChrRom => (int)(state.ChrMemoryOffset[index] / state.ChrPageSize),
				NesChrMemoryType.ChrRam => (int)(state.ChrMemoryOffset[index] / state.ChrRamPageSize),
				_ => -1
			};
			if(state.ChrMemoryType[index] == NesChrMemoryType.NametableRam || state.ChrMemoryType[index] == NesChrMemoryType.MapperRam) {
				page = -1;
			}
			return page;
		}

		private static int GetGameboyPage(int addr)
		{
			GbState gbState = DebugApi.GetConsoleState<GbState>(ConsoleType.Gameboy);
			GbMemoryManagerState state = gbState.MemoryManager;
			int index = addr >> 8;
			if(index < 0 || index >= 0xFE) {
				return -1;
			}
			if(index >= 0x80 && index < 0xA0) {
				return -1;
			}
			if(state.MemoryAccessType[index] == GbRegisterAccess.None) {
				return -1;
			}
			GbMemoryType memType = state.MemoryType[index];
			if(memType == GbMemoryType.BootRom) {
				return -1;
			}
			int bankSize = memType switch {
				GbMemoryType.PrgRom => 0x4000,
				GbMemoryType.CartRam => 0x2000,
				GbMemoryType.WorkRam => gbState.Ppu.CgbEnabled ? 0x1000 : 0x2000,
				_ => -1
			};
			if(bankSize < 0) {
				return -1;
			}
			return (int)(state.MemoryOffset[index] / (uint)bankSize);
		}

		private static int GetPcePage(int addr)
		{
			PceState state = DebugApi.GetConsoleState<PceState>(ConsoleType.PcEngine);
			int bankIndex = addr >> 13;
			if(bankIndex < 0 || bankIndex >= 8) {
				return -1;
			}
			return state.MemoryManager.Mpr[bankIndex];
		}

		private static int GetSmsPage(int addr)
		{
			AddressInfo absAddr = DebugApi.GetAbsoluteAddress(new AddressInfo() { Address = addr, Type = MemoryType.SmsMemory });
			if(absAddr.Address < 0) {
				return -1;
			}
			return absAddr.Address / 0x400;
		}

		private static int GetWsPage(int addr)
		{
			AddressInfo absAddr = DebugApi.GetAbsoluteAddress(new AddressInfo() { Address = addr, Type = MemoryType.WsMemory });
			if(absAddr.Address < 0) {
				return -1;
			}
			return absAddr.Address / 0x10000;
		}

	}
}
