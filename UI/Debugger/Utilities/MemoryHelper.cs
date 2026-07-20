using Mesen.Interop;
using Mesen.Debugger.Labels;
using Mesen.Localization;
using System;

namespace Mesen.Debugger.Utilities
{
	public static class MemoryHelper
	{
		public static string GetAddressStr(AddressInfo addr, bool withMemType = true, bool withPage = false) {
			string page = withPage ? GetPageText(addr) : "";
			string memType = withMemType ? addr.Type.GetShortName() + " " : "";
			return addr.Address >= 0
				? page + memType + $"${addr.Address.ToString(addr.Type.GetFormatString())}"
				: "";
		}
		public static string GetAddressStr(AddressInfo addr, CpuType cpu, bool withMemType = true, bool withPage = false) {
			string page = $" [{GetPageText(addr.Address,cpu)}]";
			string memType = withMemType ? cpu.ToMemoryType().GetShortName() + " " : "";
			return addr.Address >= 0
				? page + memType + $"$({addr.Address.ToString(GetStrFormat(cpu))})"
				: "";
		}

		public static string GetAddressStr(AddressInfo addr, int range, bool withMemType = true, bool withPage = false) {
			if(range <= 0) {
				return "";
			}
			string page = withPage ? GetPageText(addr) : "";
			string memType = withMemType ? addr.Type.GetShortName() + " " : "";
			string format = GetStrFormat(addr.Type.ToCpuType());
			return addr.Address >= 0
				? page + memType + $"$({addr.Address.ToString(format)}-{(addr.Address + range).ToString(format)})"
				: "";
		}
		public static string GetAddressStr(AddressInfo addr, CpuType cpu, int range, bool withMemType = true, bool withPage = false) {
			if(range <= 0) {
				return "";
			}
			string page = withPage ? GetPageText(addr.Address,cpu) : "";
			string memType = withMemType ? cpu.ToMemoryType().GetShortName() + " " : "";
			string format = GetStrFormat(cpu);
			return addr.Address >= 0
				? page + memType + $"$({addr.Address.ToString(format)}-{(addr.Address + range).ToString(format)})"
				: "";
		}

		public static string GetStrFormat(CpuType cpu) {
			return $"X{cpu.GetAddressSize()}";
		}

		public static string GetFunctionName(AddressInfo addr, bool isLabel = false)
		{
			CodeLabel? label = LabelManager.GetLabel(addr);
			return label?.Label 
				?? (isLabel 
					? ResourceHelper.GetMessage("lblNoLabel")
					: GetAddressStr(addr,false,false) 
				);
		}
		public static string GetFunctionName(AddressInfo addr, CpuType cpu, bool isLabel = false)
		{
			CodeLabel? label = LabelManager.GetLabel(addr);
			return label?.Label 
				?? (isLabel 
					? ResourceHelper.GetMessage("lblNoLabel")
					: GetAddressStr(addr,cpu,false,false) 
				);
		}


		public static string GetPageText(int address, CpuType cpu) {
			return GetPageText(address,cpu.ToMemoryType());
		}
		public static string GetPageText(int address, MemoryType memType) {
			int page = GetPage(address,memType);
			return page != -1 
				? page.ToString("X2") + ":" 
				: "";
		}
		public static string GetPageText(AddressInfo addr) {
			int page = GetPage(addr.Address,addr.Type);
			return page != -1 
				? page.ToString("X2") + ":" 
				: "";
		}
		public static int GetPage(int address, CpuType cpu) {
			return GetPage(address,cpu.ToMemoryType());
		}
		public static int GetPage(int address, MemoryType memType)
		{
			try {
				return memType switch {
					MemoryType.NesMemory => GetNesCpuPage(address),
					MemoryType.NesPpuMemory => GetNesPpuPage(address),
					MemoryType.GameboyMemory => GetGameboyPage(address),
					MemoryType.PceMemory => GetPcePage(address),
					MemoryType.SmsMemory => GetSmsPage(address),
					MemoryType.WsMemory => GetWsPage(address),
					_ => -1
				};
			} catch {
				return -1;
			}
		}

		private static int GetNesCpuPage(int address)
		{
			NesCartridgeState state = DebugApi.GetConsoleState<NesState>(ConsoleType.Nes).Cartridge;
			int index = address >> 8;
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

		private static int GetNesPpuPage(int address)
		{
			NesCartridgeState state = DebugApi.GetConsoleState<NesState>(ConsoleType.Nes).Cartridge;
			int index = address >> 8;
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

		private static int GetGameboyPage(int address)
		{
			GbState gbState = DebugApi.GetConsoleState<GbState>(ConsoleType.Gameboy);
			GbMemoryManagerState state = gbState.MemoryManager;
			int index = address >> 8;
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

		private static int GetPcePage(int address)
		{
			PceState state = DebugApi.GetConsoleState<PceState>(ConsoleType.PcEngine);
			int bankIndex = address >> 13;
			if(bankIndex < 0 || bankIndex >= 8) {
				return -1;
			}
			return state.MemoryManager.Mpr[bankIndex];
		}

		private static int GetSmsPage(int address)
		{
			AddressInfo absAddr = DebugApi.GetAbsoluteAddress(new AddressInfo() { Address = address, Type = MemoryType.SmsMemory });
			if(absAddr.Address < 0) {
				return -1;
			}
			return absAddr.Address / 0x400;
		}

		private static int GetWsPage(int address)
		{
			AddressInfo absAddr = DebugApi.GetAbsoluteAddress(new AddressInfo() { Address = address, Type = MemoryType.WsMemory });
			if(absAddr.Address < 0) {
				return -1;
			}
			return absAddr.Address / 0x10000;
		}

	}
}
