using Mesen.Config;
using Mesen.Interop;
using Mesen.Localization;
using System;
using System.Collections.Generic;

namespace Mesen.Debugger.Labels
{
	public class DefaultLabelHelper
	{
		public static void SetDefaultLabels()
		{
			if(ConfigManager.Config.Debug.Debugger.DisableDefaultLabels) {
				return;
			}

			HashSet<CpuType> cpuTypes = EmuApi.GetRomInfo().CpuTypes;
			if(cpuTypes.Contains(CpuType.Gameboy)) {
				SetGameboyDefaultLabels();
			}

			if(cpuTypes.Contains(CpuType.Snes)) {
				SetSnesDefaultLabels();
			} else if(cpuTypes.Contains(CpuType.Nes)) {
				SetDefaultNesLabels();
			} else if(cpuTypes.Contains(CpuType.Pce)) {
				SetPceDefaultLabels();
			} else if(cpuTypes.Contains(CpuType.Sms)) {
				SetSmsDefaultLabels();
			} else if(cpuTypes.Contains(CpuType.Gba)) {
				SetGbaDefaultLabels();
			} else if(cpuTypes.Contains(CpuType.Ws)) {
				SetWsDefaultLabels();
			}
		}

		private static void SetSnesDefaultLabels()
		{
			//B-Bus registers
			LabelManager.SetLabel(0x2100, MemoryType.SnesRegister, "INIDISP", ResourceHelper.GetMessage("SnesReg_INIDISP"));
			LabelManager.SetLabel(0x2101, MemoryType.SnesRegister, "OBSEL", ResourceHelper.GetMessage("SnesReg_OBSEL"));
			LabelManager.SetLabel(0x2102, MemoryType.SnesRegister, "OAMADDL", ResourceHelper.GetMessage("SnesReg_OAMADDL"));
			LabelManager.SetLabel(0x2103, MemoryType.SnesRegister, "OAMADDH", ResourceHelper.GetMessage("SnesReg_OAMADDH"));
			LabelManager.SetLabel(0x2104, MemoryType.SnesRegister, "OAMDATA", ResourceHelper.GetMessage("SnesReg_OAMDATA"));
			LabelManager.SetLabel(0x2105, MemoryType.SnesRegister, "BGMODE", ResourceHelper.GetMessage("SnesReg_BGMODE"));
			LabelManager.SetLabel(0x2106, MemoryType.SnesRegister, "MOSAIC", ResourceHelper.GetMessage("SnesReg_MOSAIC"));
			LabelManager.SetLabel(0x2107, MemoryType.SnesRegister, "BG1SC", ResourceHelper.GetMessage("SnesReg_BG1SC"));
			LabelManager.SetLabel(0x2108, MemoryType.SnesRegister, "BG2SC", ResourceHelper.GetMessage("SnesReg_BG2SC"));
			LabelManager.SetLabel(0x2109, MemoryType.SnesRegister, "BG3SC", ResourceHelper.GetMessage("SnesReg_BG3SC"));
			LabelManager.SetLabel(0x210A, MemoryType.SnesRegister, "BG4SC", ResourceHelper.GetMessage("SnesReg_BG4SC"));
			LabelManager.SetLabel(0x210B, MemoryType.SnesRegister, "BG12NBA", ResourceHelper.GetMessage("SnesReg_BG12NBA"));
			LabelManager.SetLabel(0x210C, MemoryType.SnesRegister, "BG34NBA", ResourceHelper.GetMessage("SnesReg_BG34NBA"));
			LabelManager.SetLabel(0x210D, MemoryType.SnesRegister, "BG1HOFS", ResourceHelper.GetMessage("SnesReg_BGScrollBG1"));
			LabelManager.SetLabel(0x210E, MemoryType.SnesRegister, "BG1VOFS", ResourceHelper.GetMessage("SnesReg_BGScrollBG1"));
			LabelManager.SetLabel(0x210F, MemoryType.SnesRegister, "BG2HOFS", ResourceHelper.GetMessage("SnesReg_BGScrollBG2"));
			LabelManager.SetLabel(0x2110, MemoryType.SnesRegister, "BG2VOFS", ResourceHelper.GetMessage("SnesReg_BGScrollBG2"));
			LabelManager.SetLabel(0x2111, MemoryType.SnesRegister, "BG3HOFS", ResourceHelper.GetMessage("SnesReg_BGScrollBG3"));
			LabelManager.SetLabel(0x2112, MemoryType.SnesRegister, "BG3VOFS", ResourceHelper.GetMessage("SnesReg_BGScrollBG3"));
			LabelManager.SetLabel(0x2113, MemoryType.SnesRegister, "BG4HOFS", ResourceHelper.GetMessage("SnesReg_BGScrollBG4"));
			LabelManager.SetLabel(0x2114, MemoryType.SnesRegister, "BG4VOFS", ResourceHelper.GetMessage("SnesReg_BGScrollBG4"));
			LabelManager.SetLabel(0x2115, MemoryType.SnesRegister, "VMAIN", ResourceHelper.GetMessage("SnesReg_VMAIN"));
			LabelManager.SetLabel(0x2116, MemoryType.SnesRegister, "VMADDL", ResourceHelper.GetMessage("SnesReg_VMADDL"));
			LabelManager.SetLabel(0x2117, MemoryType.SnesRegister, "VMADDH", ResourceHelper.GetMessage("SnesReg_VMADDH"));
			LabelManager.SetLabel(0x2118, MemoryType.SnesRegister, "VMDATAL", ResourceHelper.GetMessage("SnesReg_VMDATAL"));
			LabelManager.SetLabel(0x2119, MemoryType.SnesRegister, "VMDATAH", ResourceHelper.GetMessage("SnesReg_VMDATAH"));
			LabelManager.SetLabel(0x211A, MemoryType.SnesRegister, "M7SEL", ResourceHelper.GetMessage("SnesReg_M7SEL"));
			LabelManager.SetLabel(0x211B, MemoryType.SnesRegister, "M7A", ResourceHelper.GetMessage("SnesReg_Mode7Matrix"));
			LabelManager.SetLabel(0x211C, MemoryType.SnesRegister, "M7B", ResourceHelper.GetMessage("SnesReg_Mode7Matrix"));
			LabelManager.SetLabel(0x211D, MemoryType.SnesRegister, "M7C", ResourceHelper.GetMessage("SnesReg_Mode7Matrix"));
			LabelManager.SetLabel(0x211E, MemoryType.SnesRegister, "M7D", ResourceHelper.GetMessage("SnesReg_Mode7Matrix"));
			LabelManager.SetLabel(0x211F, MemoryType.SnesRegister, "M7X", ResourceHelper.GetMessage("SnesReg_Mode7Matrix"));
			LabelManager.SetLabel(0x2120, MemoryType.SnesRegister, "M7Y", ResourceHelper.GetMessage("SnesReg_Mode7Matrix"));
			LabelManager.SetLabel(0x2121, MemoryType.SnesRegister, "CGADD", ResourceHelper.GetMessage("SnesReg_CGADD"));
			LabelManager.SetLabel(0x2122, MemoryType.SnesRegister, "CGDATA", ResourceHelper.GetMessage("SnesReg_CGDATA"));
			LabelManager.SetLabel(0x2123, MemoryType.SnesRegister, "W12SEL", ResourceHelper.GetMessage("SnesReg_WindowMaskSettings"));
			LabelManager.SetLabel(0x2124, MemoryType.SnesRegister, "W34SEL", ResourceHelper.GetMessage("SnesReg_WindowMaskSettings"));
			LabelManager.SetLabel(0x2125, MemoryType.SnesRegister, "WOBJSEL", ResourceHelper.GetMessage("SnesReg_WindowMaskSettings"));
			LabelManager.SetLabel(0x2126, MemoryType.SnesRegister, "WH0", ResourceHelper.GetMessage("SnesReg_WH0"));
			LabelManager.SetLabel(0x2127, MemoryType.SnesRegister, "WH1", ResourceHelper.GetMessage("SnesReg_WH1"));
			LabelManager.SetLabel(0x2128, MemoryType.SnesRegister, "WH2", ResourceHelper.GetMessage("SnesReg_WH2"));
			LabelManager.SetLabel(0x2129, MemoryType.SnesRegister, "WH3", ResourceHelper.GetMessage("SnesReg_WH3"));
			LabelManager.SetLabel(0x212A, MemoryType.SnesRegister, "WBGLOG", ResourceHelper.GetMessage("SnesReg_WBGLOG"));
			LabelManager.SetLabel(0x212B, MemoryType.SnesRegister, "WOBJLOG", ResourceHelper.GetMessage("SnesReg_WOBJLOG"));
			LabelManager.SetLabel(0x212C, MemoryType.SnesRegister, "TM", ResourceHelper.GetMessage("SnesReg_ScreenDestination"));
			LabelManager.SetLabel(0x212D, MemoryType.SnesRegister, "TS", ResourceHelper.GetMessage("SnesReg_ScreenDestination"));
			LabelManager.SetLabel(0x212E, MemoryType.SnesRegister, "TMW", ResourceHelper.GetMessage("SnesReg_WindowMaskDestination"));
			LabelManager.SetLabel(0x212F, MemoryType.SnesRegister, "TSW", ResourceHelper.GetMessage("SnesReg_WindowMaskDestination"));
			LabelManager.SetLabel(0x2130, MemoryType.SnesRegister, "CGWSEL", ResourceHelper.GetMessage("SnesReg_ColorMath"));
			LabelManager.SetLabel(0x2131, MemoryType.SnesRegister, "CGADSUB", ResourceHelper.GetMessage("SnesReg_ColorMath"));
			LabelManager.SetLabel(0x2132, MemoryType.SnesRegister, "COLDATA", ResourceHelper.GetMessage("SnesReg_ColorMath"));
			LabelManager.SetLabel(0x2133, MemoryType.SnesRegister, "SETINI", ResourceHelper.GetMessage("SnesReg_SETINI"));
			LabelManager.SetLabel(0x2134, MemoryType.SnesRegister, "MPYL", ResourceHelper.GetMessage("SnesReg_MultiplicationResult"));
			LabelManager.SetLabel(0x2135, MemoryType.SnesRegister, "MPYM", ResourceHelper.GetMessage("SnesReg_MultiplicationResult"));
			LabelManager.SetLabel(0x2136, MemoryType.SnesRegister, "MPYH", ResourceHelper.GetMessage("SnesReg_MultiplicationResult"));
			LabelManager.SetLabel(0x2137, MemoryType.SnesRegister, "SLHV", ResourceHelper.GetMessage("SnesReg_SLHV"));
			LabelManager.SetLabel(0x2138, MemoryType.SnesRegister, "OAMDATAREAD", ResourceHelper.GetMessage("SnesReg_OAMDATAREAD"));
			LabelManager.SetLabel(0x2139, MemoryType.SnesRegister, "VMDATALREAD", ResourceHelper.GetMessage("SnesReg_VMDATALREAD"));
			LabelManager.SetLabel(0x213A, MemoryType.SnesRegister, "VMDATAHREAD", ResourceHelper.GetMessage("SnesReg_VMDATAHREAD"));
			LabelManager.SetLabel(0x213B, MemoryType.SnesRegister, "CGDATAREAD", ResourceHelper.GetMessage("SnesReg_CGDATAREAD"));
			LabelManager.SetLabel(0x213C, MemoryType.SnesRegister, "OPHCT", ResourceHelper.GetMessage("SnesReg_OPHCT"));
			LabelManager.SetLabel(0x213D, MemoryType.SnesRegister, "OPVCT", ResourceHelper.GetMessage("SnesReg_OPVCT"));
			LabelManager.SetLabel(0x213E, MemoryType.SnesRegister, "STAT77", ResourceHelper.GetMessage("SnesReg_PPUStatus"));
			LabelManager.SetLabel(0x213F, MemoryType.SnesRegister, "STAT78", ResourceHelper.GetMessage("SnesReg_PPUStatus"));
			LabelManager.SetLabel(0x2140, MemoryType.SnesRegister, "APUIO0", ResourceHelper.GetMessage("SnesReg_APUIO"));
			LabelManager.SetLabel(0x2141, MemoryType.SnesRegister, "APUIO1", ResourceHelper.GetMessage("SnesReg_APUIO"));
			LabelManager.SetLabel(0x2142, MemoryType.SnesRegister, "APUIO2", ResourceHelper.GetMessage("SnesReg_APUIO"));
			LabelManager.SetLabel(0x2143, MemoryType.SnesRegister, "APUIO3", ResourceHelper.GetMessage("SnesReg_APUIO"));
			LabelManager.SetLabel(0x2180, MemoryType.SnesRegister, "WMDATA", ResourceHelper.GetMessage("SnesReg_WMDATA"));
			LabelManager.SetLabel(0x2181, MemoryType.SnesRegister, "WMADDL", ResourceHelper.GetMessage("SnesReg_WMADD"));
			LabelManager.SetLabel(0x2182, MemoryType.SnesRegister, "WMADDM", ResourceHelper.GetMessage("SnesReg_WMADD"));
			LabelManager.SetLabel(0x2183, MemoryType.SnesRegister, "WMADDH", ResourceHelper.GetMessage("SnesReg_WMADD"));

			//A-Bus registers (CPU registers)
			LabelManager.SetLabel(0x4016, MemoryType.SnesRegister, "JOYSER0", ResourceHelper.GetMessage("SnesReg_OldJoypad"));
			LabelManager.SetLabel(0x4017, MemoryType.SnesRegister, "JOYSER1", ResourceHelper.GetMessage("SnesReg_OldJoypad"));

			LabelManager.SetLabel(0x4200, MemoryType.SnesRegister, "NMITIMEN", ResourceHelper.GetMessage("SnesReg_NMITIMEN"));
			LabelManager.SetLabel(0x4201, MemoryType.SnesRegister, "WRIO", ResourceHelper.GetMessage("SnesReg_WRIO"));
			LabelManager.SetLabel(0x4202, MemoryType.SnesRegister, "WRMPYA", ResourceHelper.GetMessage("SnesReg_Multiplicand"));
			LabelManager.SetLabel(0x4203, MemoryType.SnesRegister, "WRMPYB", ResourceHelper.GetMessage("SnesReg_Multiplicand"));
			LabelManager.SetLabel(0x4204, MemoryType.SnesRegister, "WRDIVL", ResourceHelper.GetMessage("SnesReg_DivisorDividend"));
			LabelManager.SetLabel(0x4205, MemoryType.SnesRegister, "WRDIVH", ResourceHelper.GetMessage("SnesReg_DivisorDividend"));
			LabelManager.SetLabel(0x4206, MemoryType.SnesRegister, "WRDIVB", ResourceHelper.GetMessage("SnesReg_DivisorDividend"));
			LabelManager.SetLabel(0x4207, MemoryType.SnesRegister, "HTIMEL", ResourceHelper.GetMessage("SnesReg_HTIMEL"));
			LabelManager.SetLabel(0x4208, MemoryType.SnesRegister, "HTIMEH", ResourceHelper.GetMessage("SnesReg_HTIMEH"));
			LabelManager.SetLabel(0x4209, MemoryType.SnesRegister, "VTIMEL", ResourceHelper.GetMessage("SnesReg_VTIMEL"));
			LabelManager.SetLabel(0x420A, MemoryType.SnesRegister, "VTIMEH", ResourceHelper.GetMessage("SnesReg_VTIMEH"));
			LabelManager.SetLabel(0x420B, MemoryType.SnesRegister, "MDMAEN", ResourceHelper.GetMessage("SnesReg_MDMAEN"));
			LabelManager.SetLabel(0x420C, MemoryType.SnesRegister, "HDMAEN", ResourceHelper.GetMessage("SnesReg_HDMAEN"));
			LabelManager.SetLabel(0x420D, MemoryType.SnesRegister, "MEMSEL", ResourceHelper.GetMessage("SnesReg_MEMSEL"));
			LabelManager.SetLabel(0x4210, MemoryType.SnesRegister, "RDNMI", ResourceHelper.GetMessage("SnesReg_InterruptFlag"));
			LabelManager.SetLabel(0x4211, MemoryType.SnesRegister, "TIMEUP", ResourceHelper.GetMessage("SnesReg_InterruptFlag"));
			LabelManager.SetLabel(0x4212, MemoryType.SnesRegister, "HVBJOY", ResourceHelper.GetMessage("SnesReg_PPUStatus"));
			LabelManager.SetLabel(0x4213, MemoryType.SnesRegister, "RDIO", ResourceHelper.GetMessage("SnesReg_RDIO"));
			LabelManager.SetLabel(0x4214, MemoryType.SnesRegister, "RDDIVL", ResourceHelper.GetMessage("SnesReg_MulDivResultL"));
			LabelManager.SetLabel(0x4215, MemoryType.SnesRegister, "RDDIVH", ResourceHelper.GetMessage("SnesReg_MulDivResultH"));
			LabelManager.SetLabel(0x4216, MemoryType.SnesRegister, "RDMPYL", ResourceHelper.GetMessage("SnesReg_MulDivResultL"));
			LabelManager.SetLabel(0x4217, MemoryType.SnesRegister, "RDMPYH", ResourceHelper.GetMessage("SnesReg_MulDivResultH"));
			LabelManager.SetLabel(0x4218, MemoryType.SnesRegister, "JOY1L", ResourceHelper.GetMessage("SnesReg_JOY1L"));
			LabelManager.SetLabel(0x4219, MemoryType.SnesRegister, "JOY1H", ResourceHelper.GetMessage("SnesReg_JOY1H"));
			LabelManager.SetLabel(0x421A, MemoryType.SnesRegister, "JOY2L", ResourceHelper.GetMessage("SnesReg_JOY2L"));
			LabelManager.SetLabel(0x421B, MemoryType.SnesRegister, "JOY2H", ResourceHelper.GetMessage("SnesReg_JOY2H"));
			LabelManager.SetLabel(0x421C, MemoryType.SnesRegister, "JOY3L", ResourceHelper.GetMessage("SnesReg_JOY3L"));
			LabelManager.SetLabel(0x421D, MemoryType.SnesRegister, "JOY3H", ResourceHelper.GetMessage("SnesReg_JOY3H"));
			LabelManager.SetLabel(0x421E, MemoryType.SnesRegister, "JOY4L", ResourceHelper.GetMessage("SnesReg_JOY4L"));
			LabelManager.SetLabel(0x421F, MemoryType.SnesRegister, "JOY4H", ResourceHelper.GetMessage("SnesReg_JOY4H"));

			//DMA registers
			for(uint i = 0; i < 8; i++) {
				LabelManager.SetLabel(0x4300 + i * 0x10, MemoryType.SnesRegister, "DMAP" + i.ToString(), ResourceHelper.GetMessage("SnesReg_DMAP"));
				LabelManager.SetLabel(0x4301 + i * 0x10, MemoryType.SnesRegister, "BBAD" + i.ToString(), ResourceHelper.GetMessage("SnesReg_BBAD"));
				LabelManager.SetLabel(0x4302 + i * 0x10, MemoryType.SnesRegister, "A1T" + i.ToString() + "L", ResourceHelper.GetMessage("SnesReg_A1TL"));
				LabelManager.SetLabel(0x4303 + i * 0x10, MemoryType.SnesRegister, "A1T" + i.ToString() + "H", ResourceHelper.GetMessage("SnesReg_A1TH"));
				LabelManager.SetLabel(0x4304 + i * 0x10, MemoryType.SnesRegister, "A1B" + i.ToString(), ResourceHelper.GetMessage("SnesReg_A1B"));
				LabelManager.SetLabel(0x4305 + i * 0x10, MemoryType.SnesRegister, "DAS" + i.ToString() + "L", ResourceHelper.GetMessage("SnesReg_DASL"));
				LabelManager.SetLabel(0x4306 + i * 0x10, MemoryType.SnesRegister, "DAS" + i.ToString() + "H", ResourceHelper.GetMessage("SnesReg_DASH"));
				LabelManager.SetLabel(0x4307 + i * 0x10, MemoryType.SnesRegister, "DAS" + i.ToString() + "B", ResourceHelper.GetMessage("SnesReg_DASB"));
				LabelManager.SetLabel(0x4308 + i * 0x10, MemoryType.SnesRegister, "A2A" + i.ToString() + "L", ResourceHelper.GetMessage("SnesReg_A2AL"));
				LabelManager.SetLabel(0x4309 + i * 0x10, MemoryType.SnesRegister, "A2A" + i.ToString() + "H", ResourceHelper.GetMessage("SnesReg_A2AH"));
				LabelManager.SetLabel(0x430A + i * 0x10, MemoryType.SnesRegister, "NTLR" + i.ToString(), ResourceHelper.GetMessage("SnesReg_NTLR"));
			}

			//SPC registers
			LabelManager.SetLabel(0xF0, MemoryType.SpcRam, "TEST", ResourceHelper.GetMessage("SnesReg_TEST"));
			LabelManager.SetLabel(0xF1, MemoryType.SpcRam, "CONTROL", ResourceHelper.GetMessage("SnesReg_CONTROL"));
			LabelManager.SetLabel(0xF2, MemoryType.SpcRam, "DSPADDR", ResourceHelper.GetMessage("SnesReg_DSPADDR"));
			LabelManager.SetLabel(0xF3, MemoryType.SpcRam, "DSPDATA", ResourceHelper.GetMessage("SnesReg_DSPDATA"));
			LabelManager.SetLabel(0xF4, MemoryType.SpcRam, "CPUIO0", ResourceHelper.GetMessage("SnesReg_CPUIO0"));
			LabelManager.SetLabel(0xF5, MemoryType.SpcRam, "CPUIO1", ResourceHelper.GetMessage("SnesReg_CPUIO1"));
			LabelManager.SetLabel(0xF6, MemoryType.SpcRam, "CPUIO2", ResourceHelper.GetMessage("SnesReg_CPUIO2"));
			LabelManager.SetLabel(0xF7, MemoryType.SpcRam, "CPUIO3", ResourceHelper.GetMessage("SnesReg_CPUIO3"));
			LabelManager.SetLabel(0xF8, MemoryType.SpcRam, "RAMREG1", ResourceHelper.GetMessage("SnesReg_RAMREG1"));
			LabelManager.SetLabel(0xF9, MemoryType.SpcRam, "RAMREG2", ResourceHelper.GetMessage("SnesReg_RAMREG2"));
			LabelManager.SetLabel(0xFA, MemoryType.SpcRam, "T0TARGET", ResourceHelper.GetMessage("SnesReg_T0TARGET"));
			LabelManager.SetLabel(0xFB, MemoryType.SpcRam, "T1TARGET", ResourceHelper.GetMessage("SnesReg_T1TARGET"));
			LabelManager.SetLabel(0xFC, MemoryType.SpcRam, "T2TARGET", ResourceHelper.GetMessage("SnesReg_T2TARGET"));
			LabelManager.SetLabel(0xFD, MemoryType.SpcRam, "T0OUT", ResourceHelper.GetMessage("SnesReg_T0OUT"));
			LabelManager.SetLabel(0xFE, MemoryType.SpcRam, "T1OUT", ResourceHelper.GetMessage("SnesReg_T1OUT"));
			LabelManager.SetLabel(0xFF, MemoryType.SpcRam, "T2OUT", ResourceHelper.GetMessage("SnesReg_T2OUT"));
		}

		private static void SetGameboyDefaultLabels()
		{
			//LCD
			LabelManager.SetLabel(0xFF40, MemoryType.GameboyMemory, "LCDC_FF40", ResourceHelper.GetMessage("GbReg_LCDC"));
			LabelManager.SetLabel(0xFF41, MemoryType.GameboyMemory, "STAT_FF41", ResourceHelper.GetMessage("GbReg_STAT"));
			LabelManager.SetLabel(0xFF42, MemoryType.GameboyMemory, "SCY_FF42", ResourceHelper.GetMessage("GbReg_SCY"));
			LabelManager.SetLabel(0xFF43, MemoryType.GameboyMemory, "SCX_FF43", ResourceHelper.GetMessage("GbReg_SCX"));
			LabelManager.SetLabel(0xFF44, MemoryType.GameboyMemory, "LY_FF44", ResourceHelper.GetMessage("GbReg_LY"));
			LabelManager.SetLabel(0xFF45, MemoryType.GameboyMemory, "LYC_FF45", ResourceHelper.GetMessage("GbReg_LYC"));

			LabelManager.SetLabel(0xFF47, MemoryType.GameboyMemory, "BGP_FF47", ResourceHelper.GetMessage("GbReg_BGP"));
			LabelManager.SetLabel(0xFF48, MemoryType.GameboyMemory, "OBP0_FF48", ResourceHelper.GetMessage("GbReg_OBP0"));
			LabelManager.SetLabel(0xFF49, MemoryType.GameboyMemory, "OBP1_FF49", ResourceHelper.GetMessage("GbReg_OBP1"));

			LabelManager.SetLabel(0xFF4A, MemoryType.GameboyMemory, "WY_FF4A", ResourceHelper.GetMessage("GbReg_WY"));
			LabelManager.SetLabel(0xFF4B, MemoryType.GameboyMemory, "WX_FF4B", ResourceHelper.GetMessage("GbReg_WX"));

			//APU
			LabelManager.SetLabel(0xFF10, MemoryType.GameboyMemory, "NR10_FF10", ResourceHelper.GetMessage("GbReg_NR10"));
			LabelManager.SetLabel(0xFF11, MemoryType.GameboyMemory, "NR11_FF11", ResourceHelper.GetMessage("GbReg_NR11"));
			LabelManager.SetLabel(0xFF12, MemoryType.GameboyMemory, "NR12_FF12", ResourceHelper.GetMessage("GbReg_NR12"));
			LabelManager.SetLabel(0xFF13, MemoryType.GameboyMemory, "NR13_FF13", ResourceHelper.GetMessage("GbReg_NR13"));
			LabelManager.SetLabel(0xFF14, MemoryType.GameboyMemory, "NR14_FF14", ResourceHelper.GetMessage("GbReg_NR14"));

			LabelManager.SetLabel(0xFF16, MemoryType.GameboyMemory, "NR21_FF16", ResourceHelper.GetMessage("GbReg_NR21"));
			LabelManager.SetLabel(0xFF17, MemoryType.GameboyMemory, "NR22_FF17", ResourceHelper.GetMessage("GbReg_NR22"));
			LabelManager.SetLabel(0xFF18, MemoryType.GameboyMemory, "NR23_FF18", ResourceHelper.GetMessage("GbReg_NR23"));
			LabelManager.SetLabel(0xFF19, MemoryType.GameboyMemory, "NR24_FF19", ResourceHelper.GetMessage("GbReg_NR24"));

			LabelManager.SetLabel(0xFF1A, MemoryType.GameboyMemory, "NR30_FF1A", ResourceHelper.GetMessage("GbReg_NR30"));
			LabelManager.SetLabel(0xFF1B, MemoryType.GameboyMemory, "NR31_FF1B", ResourceHelper.GetMessage("GbReg_NR31"));
			LabelManager.SetLabel(0xFF1C, MemoryType.GameboyMemory, "NR32_FF1C", ResourceHelper.GetMessage("GbReg_NR32"));
			LabelManager.SetLabel(0xFF1D, MemoryType.GameboyMemory, "NR33_FF1D", ResourceHelper.GetMessage("GbReg_NR33"));
			LabelManager.SetLabel(0xFF1E, MemoryType.GameboyMemory, "NR34_FF1E", ResourceHelper.GetMessage("GbReg_NR34"));

			LabelManager.SetLabel(0xFF20, MemoryType.GameboyMemory, "NR41_FF20", ResourceHelper.GetMessage("GbReg_NR41"));
			LabelManager.SetLabel(0xFF21, MemoryType.GameboyMemory, "NR42_FF21", ResourceHelper.GetMessage("GbReg_NR42"));
			LabelManager.SetLabel(0xFF22, MemoryType.GameboyMemory, "NR43_FF22", ResourceHelper.GetMessage("GbReg_NR43"));
			LabelManager.SetLabel(0xFF23, MemoryType.GameboyMemory, "NR44_FF23", ResourceHelper.GetMessage("GbReg_NR44"));

			LabelManager.SetLabel(0xFF24, MemoryType.GameboyMemory, "NR50_FF24", ResourceHelper.GetMessage("GbReg_NR50"));
			LabelManager.SetLabel(0xFF25, MemoryType.GameboyMemory, "NR51_FF25", ResourceHelper.GetMessage("GbReg_NR51"));
			LabelManager.SetLabel(0xFF26, MemoryType.GameboyMemory, "NR52_FF26", ResourceHelper.GetMessage("GbReg_NR52"));

			//Others
			LabelManager.SetLabel(0xFF00, MemoryType.GameboyMemory, "JOYP_FF00", ResourceHelper.GetMessage("GbReg_JOYP"));
			LabelManager.SetLabel(0xFF01, MemoryType.GameboyMemory, "SB_FF01", ResourceHelper.GetMessage("GbReg_SB"));
			LabelManager.SetLabel(0xFF02, MemoryType.GameboyMemory, "SC_FF02", ResourceHelper.GetMessage("GbReg_SC"));

			LabelManager.SetLabel(0xFF04, MemoryType.GameboyMemory, "DIV_FF04", ResourceHelper.GetMessage("GbReg_DIV"));
			LabelManager.SetLabel(0xFF05, MemoryType.GameboyMemory, "TIMA_FF05", ResourceHelper.GetMessage("GbReg_TIMA"));
			LabelManager.SetLabel(0xFF06, MemoryType.GameboyMemory, "TMA_FF06", ResourceHelper.GetMessage("GbReg_TMA"));
			LabelManager.SetLabel(0xFF07, MemoryType.GameboyMemory, "TAC_FF07", ResourceHelper.GetMessage("GbReg_TAC"));

			LabelManager.SetLabel(0xFF0F, MemoryType.GameboyMemory, "IF_FF0F", ResourceHelper.GetMessage("GbReg_IF"));
			LabelManager.SetLabel(0xFFFF, MemoryType.GameboyMemory, "IE_FFFF", ResourceHelper.GetMessage("GbReg_IE"));

			LabelManager.SetLabel(0xFF46, MemoryType.GameboyMemory, "DMA_FF46", ResourceHelper.GetMessage("GbReg_DMA"));
		}

		private static void SetDefaultNesLabels()
		{
			LabelManager.SetLabel(0x2000, MemoryType.NesMemory, "PpuControl_2000", ResourceHelper.GetMessage("NesReg_PpuControl"));
			LabelManager.SetLabel(0x2001, MemoryType.NesMemory, "PpuMask_2001", ResourceHelper.GetMessage("NesReg_PpuMask"));
			LabelManager.SetLabel(0x2002, MemoryType.NesMemory, "PpuStatus_2002", ResourceHelper.GetMessage("NesReg_PpuStatus"));
			LabelManager.SetLabel(0x2003, MemoryType.NesMemory, "OamAddr_2003", ResourceHelper.GetMessage("NesReg_OamAddr"));
			LabelManager.SetLabel(0x2004, MemoryType.NesMemory, "OamData_2004", ResourceHelper.GetMessage("NesReg_OamData"));
			LabelManager.SetLabel(0x2005, MemoryType.NesMemory, "PpuScroll_2005", ResourceHelper.GetMessage("NesReg_PpuScroll"));
			LabelManager.SetLabel(0x2006, MemoryType.NesMemory, "PpuAddr_2006", ResourceHelper.GetMessage("NesReg_PpuAddr"));
			LabelManager.SetLabel(0x2007, MemoryType.NesMemory, "PpuData_2007", ResourceHelper.GetMessage("NesReg_PpuData"));

			LabelManager.SetLabel(0x4000, MemoryType.NesMemory, "Sq0Duty_4000", ResourceHelper.GetMessage("NesReg_SqDuty"));
			LabelManager.SetLabel(0x4001, MemoryType.NesMemory, "Sq0Sweep_4001", ResourceHelper.GetMessage("NesReg_SqSweep"));
			LabelManager.SetLabel(0x4002, MemoryType.NesMemory, "Sq0Timer_4002", ResourceHelper.GetMessage("NesReg_SqTimer"));
			LabelManager.SetLabel(0x4003, MemoryType.NesMemory, "Sq0Length_4003", ResourceHelper.GetMessage("NesReg_SqLength"));

			LabelManager.SetLabel(0x4004, MemoryType.NesMemory, "Sq1Duty_4004", ResourceHelper.GetMessage("NesReg_SqDuty"));
			LabelManager.SetLabel(0x4005, MemoryType.NesMemory, "Sq1Sweep_4005", ResourceHelper.GetMessage("NesReg_SqSweep"));
			LabelManager.SetLabel(0x4006, MemoryType.NesMemory, "Sq1Timer_4006", ResourceHelper.GetMessage("NesReg_SqTimer"));
			LabelManager.SetLabel(0x4007, MemoryType.NesMemory, "Sq1Length_4007", ResourceHelper.GetMessage("NesReg_SqLength"));

			LabelManager.SetLabel(0x4008, MemoryType.NesMemory, "TrgLinear_4008", ResourceHelper.GetMessage("NesReg_TrgLinear"));
			LabelManager.SetLabel(0x400A, MemoryType.NesMemory, "TrgTimer_400A", ResourceHelper.GetMessage("NesReg_SqTimer"));
			LabelManager.SetLabel(0x400B, MemoryType.NesMemory, "TrgLength_400B", ResourceHelper.GetMessage("NesReg_SqLength"));

			LabelManager.SetLabel(0x400C, MemoryType.NesMemory, "NoiseVolume_400C", ResourceHelper.GetMessage("NesReg_NoiseVolume"));
			LabelManager.SetLabel(0x400E, MemoryType.NesMemory, "NoisePeriod_400E", ResourceHelper.GetMessage("NesReg_NoisePeriod"));
			LabelManager.SetLabel(0x400F, MemoryType.NesMemory, "NoiseLength_400F", ResourceHelper.GetMessage("NesReg_NoiseLength"));

			LabelManager.SetLabel(0x4010, MemoryType.NesMemory, "DmcFreq_4010", ResourceHelper.GetMessage("NesReg_DmcFreq"));
			LabelManager.SetLabel(0x4011, MemoryType.NesMemory, "DmcCounter_4011", ResourceHelper.GetMessage("NesReg_DmcCounter"));
			LabelManager.SetLabel(0x4012, MemoryType.NesMemory, "DmcAddress_4012", ResourceHelper.GetMessage("NesReg_DmcAddress"));
			LabelManager.SetLabel(0x4013, MemoryType.NesMemory, "DmcLength_4013", ResourceHelper.GetMessage("NesReg_DmcLength"));

			LabelManager.SetLabel(0x4014, MemoryType.NesMemory, "SpriteDma_4014", ResourceHelper.GetMessage("NesReg_SpriteDma"));

			LabelManager.SetLabel(0x4015, MemoryType.NesMemory, "ApuStatus_4015", ResourceHelper.GetMessage("NesReg_ApuStatus"));

			LabelManager.SetLabel(0x4016, MemoryType.NesMemory, "Ctrl1_4016", ResourceHelper.GetMessage("NesReg_Ctrl1"));
			LabelManager.SetLabel(0x4017, MemoryType.NesMemory, "Ctrl2_FrameCtr_4017", ResourceHelper.GetMessage("NesReg_Ctrl2"));

			if(EmuApi.GetRomInfo().Format == RomFormat.Fds) {
				LabelManager.SetLabel(0x01F8, MemoryType.NesPrgRom, "LoadFiles", ResourceHelper.GetMessage("NesFds_LoadFiles"));
				LabelManager.SetLabel(0x0237, MemoryType.NesPrgRom, "AppendFile", ResourceHelper.GetMessage("NesFds_AppendFile"));
				LabelManager.SetLabel(0x0239, MemoryType.NesPrgRom, "WriteFile", ResourceHelper.GetMessage("NesFds_WriteFile"));
				LabelManager.SetLabel(0x02B7, MemoryType.NesPrgRom, "CheckFileCount", ResourceHelper.GetMessage("NesFds_CheckFileCount"));
				LabelManager.SetLabel(0x02BB, MemoryType.NesPrgRom, "AdjustFileCount", ResourceHelper.GetMessage("NesFds_AdjustFileCount"));
				LabelManager.SetLabel(0x0301, MemoryType.NesPrgRom, "SetFileCount1", ResourceHelper.GetMessage("NesFds_SetFileCount1"));
				LabelManager.SetLabel(0x0305, MemoryType.NesPrgRom, "SetFileCount", ResourceHelper.GetMessage("NesFds_SetFileCount"));
				LabelManager.SetLabel(0x032A, MemoryType.NesPrgRom, "GetDiskInfo", ResourceHelper.GetMessage("NesFds_GetDiskInfo"));

				LabelManager.SetLabel(0x0445, MemoryType.NesPrgRom, "CheckDiskHeader", ResourceHelper.GetMessage("NesFds_CheckDiskHeader"));
				LabelManager.SetLabel(0x0484, MemoryType.NesPrgRom, "GetNumFiles", ResourceHelper.GetMessage("NesFds_GetNumFiles"));
				LabelManager.SetLabel(0x0492, MemoryType.NesPrgRom, "SetNumFiles", ResourceHelper.GetMessage("NesFds_SetNumFiles"));
				LabelManager.SetLabel(0x04A0, MemoryType.NesPrgRom, "FileMatchTest", ResourceHelper.GetMessage("NesFds_FileMatchTest"));
				LabelManager.SetLabel(0x04DA, MemoryType.NesPrgRom, "SkipFiles", ResourceHelper.GetMessage("NesFds_SkipFiles"));

				LabelManager.SetLabel(0x0149, MemoryType.NesPrgRom, "Delay131", ResourceHelper.GetMessage("NesFds_Delay131"));
				LabelManager.SetLabel(0x0153, MemoryType.NesPrgRom, "Delayms", ResourceHelper.GetMessage("NesFds_Delayms"));
				LabelManager.SetLabel(0x0161, MemoryType.NesPrgRom, "DisPFObj", ResourceHelper.GetMessage("NesFds_DisPFObj"));
				LabelManager.SetLabel(0x016B, MemoryType.NesPrgRom, "EnPFObj", ResourceHelper.GetMessage("NesFds_EnPFObj"));
				LabelManager.SetLabel(0x0171, MemoryType.NesPrgRom, "DisObj", ResourceHelper.GetMessage("NesFds_DisObj"));
				LabelManager.SetLabel(0x0178, MemoryType.NesPrgRom, "EnObj", ResourceHelper.GetMessage("NesFds_EnObj"));
				LabelManager.SetLabel(0x017E, MemoryType.NesPrgRom, "DisPF", ResourceHelper.GetMessage("NesFds_DisPF"));
				LabelManager.SetLabel(0x0185, MemoryType.NesPrgRom, "EnPF", ResourceHelper.GetMessage("NesFds_EnPF"));
				LabelManager.SetLabel(0x01B2, MemoryType.NesPrgRom, "VINTWait", ResourceHelper.GetMessage("NesFds_VINTWait"));
				LabelManager.SetLabel(0x07BB, MemoryType.NesPrgRom, "VRAMStructWrite", ResourceHelper.GetMessage("NesFds_VRAMStructWrite"));
				LabelManager.SetLabel(0x0844, MemoryType.NesPrgRom, "FetchDirectPtr", ResourceHelper.GetMessage("NesFds_FetchDirectPtr"));
				LabelManager.SetLabel(0x086A, MemoryType.NesPrgRom, "WriteVRAMBuffer", ResourceHelper.GetMessage("NesFds_WriteVRAMBuffer"));
				LabelManager.SetLabel(0x08B3, MemoryType.NesPrgRom, "ReadVRAMBuffer", ResourceHelper.GetMessage("NesFds_ReadVRAMBuffer"));
				LabelManager.SetLabel(0x08D2, MemoryType.NesPrgRom, "PrepareVRAMString", ResourceHelper.GetMessage("NesFds_PrepareVRAMString"));
				LabelManager.SetLabel(0x08E1, MemoryType.NesPrgRom, "PrepareVRAMStrings", ResourceHelper.GetMessage("NesFds_PrepareVRAMStrings"));
				LabelManager.SetLabel(0x094F, MemoryType.NesPrgRom, "GetVRAMBufferByte", ResourceHelper.GetMessage("NesFds_GetVRAMBufferByte"));
				LabelManager.SetLabel(0x097D, MemoryType.NesPrgRom, "Pixel2NamConv", ResourceHelper.GetMessage("NesFds_Pixel2NamConv"));
				LabelManager.SetLabel(0x0997, MemoryType.NesPrgRom, "Nam2PixelConv", ResourceHelper.GetMessage("NesFds_Nam2PixelConv"));
				LabelManager.SetLabel(0x09B1, MemoryType.NesPrgRom, "Random", ResourceHelper.GetMessage("NesFds_Random"));
				LabelManager.SetLabel(0x09C8, MemoryType.NesPrgRom, "SpriteDMA", ResourceHelper.GetMessage("NesFds_SpriteDMA"));
				LabelManager.SetLabel(0x09D3, MemoryType.NesPrgRom, "CounterLogic", ResourceHelper.GetMessage("NesFds_CounterLogic"));
				LabelManager.SetLabel(0x09EB, MemoryType.NesPrgRom, "ReadPads", ResourceHelper.GetMessage("NesFds_ReadPads"));
				LabelManager.SetLabel(0x0A0D, MemoryType.NesPrgRom, "OrPads", ResourceHelper.GetMessage("NesFds_OrPads"));
				LabelManager.SetLabel(0x0A1A, MemoryType.NesPrgRom, "ReadDownPads", ResourceHelper.GetMessage("NesFds_ReadDownPads"));
				LabelManager.SetLabel(0x0A1F, MemoryType.NesPrgRom, "ReadOrDownPads", ResourceHelper.GetMessage("NesFds_ReadOrDownPads"));
				LabelManager.SetLabel(0x0A36, MemoryType.NesPrgRom, "ReadDownVerifyPads", ResourceHelper.GetMessage("NesFds_ReadDownVerifyPads"));
				LabelManager.SetLabel(0x0A4C, MemoryType.NesPrgRom, "ReadOrDownVerifyPads", ResourceHelper.GetMessage("NesFds_ReadOrDownVerifyPads"));
				LabelManager.SetLabel(0x0A68, MemoryType.NesPrgRom, "ReadDownExpPads", ResourceHelper.GetMessage("NesFds_ReadDownExpPads"));
				LabelManager.SetLabel(0x0A84, MemoryType.NesPrgRom, "VRAMFill", ResourceHelper.GetMessage("NesFds_VRAMFill"));
				LabelManager.SetLabel(0x0Ad2, MemoryType.NesPrgRom, "MemFill", ResourceHelper.GetMessage("NesFds_MemFill"));
				LabelManager.SetLabel(0x0AEA, MemoryType.NesPrgRom, "SetScroll", ResourceHelper.GetMessage("NesFds_SetScroll"));
				LabelManager.SetLabel(0x0AFD, MemoryType.NesPrgRom, "JumpEngine", ResourceHelper.GetMessage("NesFds_JumpEngine"));
				LabelManager.SetLabel(0x0B13, MemoryType.NesPrgRom, "ReadKeyboard", ResourceHelper.GetMessage("NesFds_ReadKeyboard"));
				LabelManager.SetLabel(0x0B66, MemoryType.NesPrgRom, "LoadTileset", ResourceHelper.GetMessage("NesFds_LoadTileset"));
				LabelManager.SetLabel(0x0C22, MemoryType.NesPrgRom, "UploadObject", ResourceHelper.GetMessage("NesFds_UploadObject"));
			}
		}

		private static void SetPceDefaultLabels()
		{
			bool isSuperGrafx = DebugApi.GetConsoleState<PceState>(ConsoleType.PcEngine).IsSuperGrafx;

			LabelManager.SetLabel(0x000, MemoryType.PceMemory, "VDC_AR_0000", ResourceHelper.GetMessage("PceReg_VDC_AR"));
			LabelManager.SetLabel(0x002, MemoryType.PceMemory, "VDC_DATA_LO_0002", ResourceHelper.GetMessage("PceReg_VDC_DATA_LO"));
			LabelManager.SetLabel(0x003, MemoryType.PceMemory, "VDC_DATA_HI_0003", ResourceHelper.GetMessage("PceReg_VDC_DATA_HI"));

			if(isSuperGrafx) {
				LabelManager.SetLabel(0x008, MemoryType.PceMemory, "VPC_PRIO_LO_0008", "Priority Control (LSB)");
				LabelManager.SetLabel(0x009, MemoryType.PceMemory, "VPC_PRIO_HI_0009", "Priority Control (MSB)");
				LabelManager.SetLabel(0x00A, MemoryType.PceMemory, "VPC_WND1_LO_000A", "Window 1 (LSB)");
				LabelManager.SetLabel(0x00B, MemoryType.PceMemory, "VPC_WND1_HI_000B", "Window 1 (MSB)");
				LabelManager.SetLabel(0x00C, MemoryType.PceMemory, "VPC_WND2_LO_000C", "Window 2 (LSB)");
				LabelManager.SetLabel(0x00D, MemoryType.PceMemory, "VPC_WND2_HI_000D", "Window 2 (MSB)");
				LabelManager.SetLabel(0x00E, MemoryType.PceMemory, "VPC_STCTRL_000E", "Store Immediate Control");
				LabelManager.SetLabel(0x010, MemoryType.PceMemory, "VDC2_AR_0010", "Address Register (W) / Status Register (R)");
				LabelManager.SetLabel(0x012, MemoryType.PceMemory, "VDC2_DATA_LO_0012", "Data (low byte)");
				LabelManager.SetLabel(0x013, MemoryType.PceMemory, "VDC2_DATA_HI_0013", "Data (high byte) + Latch");
			}

			LabelManager.SetLabel(0x400, MemoryType.PceMemory, "VCE_CONTROL_0400", ResourceHelper.GetMessage("PceReg_VCE_CONTROL"));
			LabelManager.SetLabel(0x402, MemoryType.PceMemory, "VCE_ADDR_LO_0402", ResourceHelper.GetMessage("PceReg_VCE_ADDR_LO"));
			LabelManager.SetLabel(0x403, MemoryType.PceMemory, "VCE_ADDR_HI_0403", ResourceHelper.GetMessage("PceReg_VCE_ADDR_HI"));
			LabelManager.SetLabel(0x404, MemoryType.PceMemory, "VCE_DATA_LO_0404", ResourceHelper.GetMessage("PceReg_VCE_DATA_LO"));
			LabelManager.SetLabel(0x405, MemoryType.PceMemory, "VCE_DATA_HI_0405", ResourceHelper.GetMessage("PceReg_VCE_DATA_HI"));

			LabelManager.SetLabel(0x800, MemoryType.PceMemory, "PSG_CHANSELECT_0800", ResourceHelper.GetMessage("PceReg_PSG_CHANSELECT"));
			LabelManager.SetLabel(0x801, MemoryType.PceMemory, "PSG_GLOBALVOL_0801", ResourceHelper.GetMessage("PceReg_PSG_GLOBALVOL"));
			LabelManager.SetLabel(0x802, MemoryType.PceMemory, "PSG_FREQLO_0802", ResourceHelper.GetMessage("PceReg_PSG_FREQLO"));
			LabelManager.SetLabel(0x803, MemoryType.PceMemory, "PSG_FREQHI_0803", ResourceHelper.GetMessage("PceReg_PSG_FREQHI"));
			LabelManager.SetLabel(0x804, MemoryType.PceMemory, "PSG_CHANCTRL_0804", ResourceHelper.GetMessage("PceReg_PSG_CHANCTRL"));
			LabelManager.SetLabel(0x805, MemoryType.PceMemory, "PSG_CHANPAN_0805", ResourceHelper.GetMessage("PceReg_PSG_CHANPAN"));
			LabelManager.SetLabel(0x806, MemoryType.PceMemory, "PSG_CHANDATA_0806", ResourceHelper.GetMessage("PceReg_PSG_CHANDATA"));
			LabelManager.SetLabel(0x807, MemoryType.PceMemory, "PSG_NOISE_0807", ResourceHelper.GetMessage("PceReg_PSG_NOISE"));
			LabelManager.SetLabel(0x808, MemoryType.PceMemory, "PSG_LFOFREQ_0808", ResourceHelper.GetMessage("PceReg_PSG_LFOFREQ"));
			LabelManager.SetLabel(0x809, MemoryType.PceMemory, "PSG_LFOCONTROL_0809", ResourceHelper.GetMessage("PceReg_PSG_LFOCONTROL"));

			LabelManager.SetLabel(0xC00, MemoryType.PceMemory, "TIMER_COUNTER_0C00", ResourceHelper.GetMessage("PceReg_TIMER_COUNTER"));
			LabelManager.SetLabel(0xC01, MemoryType.PceMemory, "TIMER_CONTROL_0C01", ResourceHelper.GetMessage("PceReg_TIMER_CONTROL"));

			LabelManager.SetLabel(0x1000, MemoryType.PceMemory, "JOYPAD_1000", ResourceHelper.GetMessage("PceReg_JOYPAD"));

			LabelManager.SetLabel(0x1402, MemoryType.PceMemory, "IRQ_DISABLE_1402", ResourceHelper.GetMessage("PceReg_IRQ_DISABLE"));
			LabelManager.SetLabel(0x1403, MemoryType.PceMemory, "IRQ_STATUS_1403", ResourceHelper.GetMessage("PceReg_IRQ_STATUS"));
		}

		private static void SetSmsDefaultLabels()
		{
			LabelManager.SetLabel(0x3E, MemoryType.SmsPort, "MEMORY_ENABLE_3E", "");
			LabelManager.SetLabel(0x3F, MemoryType.SmsPort, "IO_3F", "");
			LabelManager.SetLabel(0x7E, MemoryType.SmsPort, "VDP_V_COUNTER_7E", "");
			LabelManager.SetLabel(0x7F, MemoryType.SmsPort, "PSG_7F", "");
			LabelManager.SetLabel(0xBE, MemoryType.SmsPort, "VDP_DATA_BE", "");
			LabelManager.SetLabel(0xBF, MemoryType.SmsPort, "VDP_CMD_STATUS_BF", "");
			LabelManager.SetLabel(0xDC, MemoryType.SmsPort, "JOY1_DC", "");
			LabelManager.SetLabel(0xDD, MemoryType.SmsPort, "JOY2_DD", "");
		}

		private static void SetGbaDefaultLabels()
		{
			Action<uint, uint, string, string> addLabel = (addr, length, label, desc) => {
				LabelManager.SetLabel(new CodeLabel() {
					Address = 0x4000000 | addr,
					Length = length,
					MemoryType = MemoryType.GbaMemory,
					Label = label,
					Comment = desc
				}, false);
			};

			addLabel(0x000, 2, "DISPCNT", ResourceHelper.GetMessage("GbaReg_DISPCNT"));
			addLabel(0x002, 1, "GREENSWAP", "");
			addLabel(0x004, 1, "DISPSTAT", ResourceHelper.GetMessage("GbaReg_DISPSTAT"));
			addLabel(0x005, 1, "LYC", "");
			addLabel(0x006, 1, "VCOUNT", "");

			addLabel(0x008, 2, "BG0CNT", ResourceHelper.GetMessage("GbaReg_BG0CNT"));
			addLabel(0x00A, 2, "BG1CNT", ResourceHelper.GetMessage("GbaReg_BG1CNT"));
			addLabel(0x00C, 2, "BG2CNT", ResourceHelper.GetMessage("GbaReg_BG2CNT"));
			addLabel(0x00E, 2, "BG3CNT", ResourceHelper.GetMessage("GbaReg_BG3CNT"));

			addLabel(0x010, 2, "BG0HOFS", ResourceHelper.GetMessage("GbaReg_BG0HOFS"));
			addLabel(0x012, 2, "BG0VOFS", ResourceHelper.GetMessage("GbaReg_BG0VOFS"));
			addLabel(0x014, 2, "BG1HOFS", ResourceHelper.GetMessage("GbaReg_BG1HOFS"));
			addLabel(0x016, 2, "BG1VOFS", ResourceHelper.GetMessage("GbaReg_BG1VOFS"));
			addLabel(0x018, 2, "BG2HOFS", ResourceHelper.GetMessage("GbaReg_BG2HOFS"));
			addLabel(0x01A, 2, "BG2VOFS", ResourceHelper.GetMessage("GbaReg_BG2VOFS"));
			addLabel(0x01C, 2, "BG3HOFS", ResourceHelper.GetMessage("GbaReg_BG3HOFS"));
			addLabel(0x01E, 2, "BG3VOFS", ResourceHelper.GetMessage("GbaReg_BG3VOFS"));

			addLabel(0x020, 2, "BG2PA", ResourceHelper.GetMessage("GbaReg_BG2PA"));
			addLabel(0x022, 2, "BG2PB", ResourceHelper.GetMessage("GbaReg_BG2PB"));
			addLabel(0x024, 2, "BG2PC", ResourceHelper.GetMessage("GbaReg_BG2PC"));
			addLabel(0x026, 2, "BG2PD", ResourceHelper.GetMessage("GbaReg_BG2PD"));
			addLabel(0x028, 4, "BG2X", ResourceHelper.GetMessage("GbaReg_BG2X"));
			addLabel(0x02C, 4, "BG2Y", ResourceHelper.GetMessage("GbaReg_BG2Y"));

			addLabel(0x030, 2, "BG3PA", ResourceHelper.GetMessage("GbaReg_BG3PA"));
			addLabel(0x032, 2, "BG3PB", ResourceHelper.GetMessage("GbaReg_BG3PB"));
			addLabel(0x034, 2, "BG3PC", ResourceHelper.GetMessage("GbaReg_BG3PC"));
			addLabel(0x036, 2, "BG3PD", ResourceHelper.GetMessage("GbaReg_BG3PD"));
			addLabel(0x038, 4, "BG3X", ResourceHelper.GetMessage("GbaReg_BG3X"));
			addLabel(0x03C, 4, "BG3Y", ResourceHelper.GetMessage("GbaReg_BG3Y"));

			addLabel(0x040, 2, "WIN0H", ResourceHelper.GetMessage("GbaReg_WIN0H"));
			addLabel(0x042, 2, "WIN1H", ResourceHelper.GetMessage("GbaReg_WIN1H"));

			addLabel(0x044, 2, "WIN0V", ResourceHelper.GetMessage("GbaReg_WIN0V"));
			addLabel(0x046, 2, "WIN1V", ResourceHelper.GetMessage("GbaReg_WIN1V"));

			addLabel(0x048, 2, "WININ", ResourceHelper.GetMessage("GbaReg_WININ"));
			addLabel(0x04A, 2, "WINOUT", ResourceHelper.GetMessage("GbaReg_WINOUT"));

			addLabel(0x04C, 2, "MOSAIC", ResourceHelper.GetMessage("GbaReg_MOSAIC"));
			addLabel(0x050, 2, "BLDCNT", ResourceHelper.GetMessage("GbaReg_BLDCNT"));
			addLabel(0x052, 2, "BLDALPHA", ResourceHelper.GetMessage("GbaReg_BLDALPHA"));
			addLabel(0x054, 1, "BLDY", ResourceHelper.GetMessage("GbaReg_BLDY"));

			addLabel(0x060, 1, "NR10", ResourceHelper.GetMessage("GbReg_NR10"));
			addLabel(0x062, 1, "NR11", ResourceHelper.GetMessage("GbReg_NR11"));
			addLabel(0x063, 1, "NR12", ResourceHelper.GetMessage("GbReg_NR12"));
			addLabel(0x064, 1, "NR13", ResourceHelper.GetMessage("GbReg_NR13"));
			addLabel(0x065, 1, "NR14", ResourceHelper.GetMessage("GbReg_NR14"));

			addLabel(0x068, 1, "NR21", ResourceHelper.GetMessage("GbReg_NR21"));
			addLabel(0x069, 1, "NR22", ResourceHelper.GetMessage("GbReg_NR22"));
			addLabel(0x06C, 1, "NR23", ResourceHelper.GetMessage("GbReg_NR23"));
			addLabel(0x06D, 1, "NR24", ResourceHelper.GetMessage("GbReg_NR24"));

			addLabel(0x070, 1, "NR30", ResourceHelper.GetMessage("GbReg_NR30"));
			addLabel(0x072, 1, "NR31", ResourceHelper.GetMessage("GbReg_NR31"));
			addLabel(0x073, 1, "NR32", ResourceHelper.GetMessage("GbReg_NR32"));
			addLabel(0x074, 1, "NR33", ResourceHelper.GetMessage("GbReg_NR33"));
			addLabel(0x075, 1, "NR34", ResourceHelper.GetMessage("GbReg_NR34"));

			addLabel(0x078, 1, "NR41", ResourceHelper.GetMessage("GbReg_NR41"));
			addLabel(0x079, 1, "NR42", ResourceHelper.GetMessage("GbReg_NR42"));
			addLabel(0x07C, 1, "NR43", ResourceHelper.GetMessage("GbReg_NR43"));
			addLabel(0x07D, 1, "NR44", ResourceHelper.GetMessage("GbReg_NR44"));

			addLabel(0x080, 1, "NR50", ResourceHelper.GetMessage("GbReg_NR50"));
			addLabel(0x081, 1, "NR51", ResourceHelper.GetMessage("GbReg_NR51"));
			addLabel(0x082, 2, "SOUNDCNT_H", ResourceHelper.GetMessage("GbaReg_SOUNDCNT_H"));
			addLabel(0x084, 1, "NR52", ResourceHelper.GetMessage("GbReg_NR52"));
			addLabel(0x088, 2, "SOUNDBIAS", "");

			addLabel(0x090, 0x10, "WAVERAM", ResourceHelper.GetMessage("GbaReg_WAVERAM"));

			addLabel(0x0A0, 4, "FIFO_A", ResourceHelper.GetMessage("GbaReg_FIFO_A"));
			addLabel(0x0A4, 4, "FIFO_B", ResourceHelper.GetMessage("GbaReg_FIFO_B"));

			addLabel(0x0B0, 4, "DMA0SAD", ResourceHelper.GetMessage("GbaReg_DMA0SAD"));
			addLabel(0x0B4, 4, "DMA0DAD", ResourceHelper.GetMessage("GbaReg_DMA0DAD"));
			addLabel(0x0B8, 2, "DMA0CNT_L", ResourceHelper.GetMessage("GbaReg_DMA0CNT_L"));
			addLabel(0x0BA, 2, "DMA0CNT_H", ResourceHelper.GetMessage("GbaReg_DMA0CNT_H"));
			addLabel(0x0BC, 4, "DMA1SAD", ResourceHelper.GetMessage("GbaReg_DMA1SAD"));
			addLabel(0x0C0, 4, "DMA1DAD", ResourceHelper.GetMessage("GbaReg_DMA1DAD"));
			addLabel(0x0C4, 2, "DMA1CNT_L", ResourceHelper.GetMessage("GbaReg_DMA1CNT_L"));
			addLabel(0x0C6, 2, "DMA1CNT_H", ResourceHelper.GetMessage("GbaReg_DMA1CNT_H"));
			addLabel(0x0C8, 4, "DMA2SAD", ResourceHelper.GetMessage("GbaReg_DMA2SAD"));
			addLabel(0x0CC, 4, "DMA2DAD", ResourceHelper.GetMessage("GbaReg_DMA2DAD"));
			addLabel(0x0D0, 2, "DMA2CNT_L", ResourceHelper.GetMessage("GbaReg_DMA2CNT_L"));
			addLabel(0x0D2, 2, "DMA2CNT_H", ResourceHelper.GetMessage("GbaReg_DMA2CNT_H"));
			addLabel(0x0D4, 4, "DMA3SAD", ResourceHelper.GetMessage("GbaReg_DMA3SAD"));
			addLabel(0x0D8, 4, "DMA3DAD", ResourceHelper.GetMessage("GbaReg_DMA3DAD"));
			addLabel(0x0DC, 2, "DMA3CNT_L", ResourceHelper.GetMessage("GbaReg_DMA3CNT_L"));
			addLabel(0x0DE, 2, "DMA3CNT_H", ResourceHelper.GetMessage("GbaReg_DMA3CNT_H"));

			addLabel(0x100, 2, "TM0CNT_L", ResourceHelper.GetMessage("GbaReg_TM0CNT_L"));
			addLabel(0x102, 1, "TM0CNT_H", ResourceHelper.GetMessage("GbaReg_TM0CNT_H"));
			addLabel(0x104, 2, "TM1CNT_L", ResourceHelper.GetMessage("GbaReg_TM1CNT_L"));
			addLabel(0x106, 1, "TM1CNT_H", ResourceHelper.GetMessage("GbaReg_TM1CNT_H"));
			addLabel(0x108, 2, "TM2CNT_L", ResourceHelper.GetMessage("GbaReg_TM2CNT_L"));
			addLabel(0x10A, 1, "TM2CNT_H", ResourceHelper.GetMessage("GbaReg_TM2CNT_H"));
			addLabel(0x10C, 2, "TM3CNT_L", ResourceHelper.GetMessage("GbaReg_TM3CNT_L"));
			addLabel(0x10E, 1, "TM3CNT_H", ResourceHelper.GetMessage("GbaReg_TM3CNT_H"));

			addLabel(0x120, 4, "SIODATA32", ResourceHelper.GetMessage("GbaReg_SIODATA32"));
			addLabel(0x124, 2, "SIOMULTI2", ResourceHelper.GetMessage("GbaReg_SIOMULTI2"));
			addLabel(0x126, 2, "SIOMULTI3", ResourceHelper.GetMessage("GbaReg_SIOMULTI3"));
			addLabel(0x128, 2, "SIOCNT", ResourceHelper.GetMessage("GbaReg_SIOCNT"));
			addLabel(0x12A, 2, "SIODATA8", ResourceHelper.GetMessage("GbaReg_SIODATA8"));

			addLabel(0x130, 2, "KEYINPUT", ResourceHelper.GetMessage("GbaReg_KEYINPUT"));
			addLabel(0x132, 2, "KEYCNT", ResourceHelper.GetMessage("GbaReg_KEYCNT"));

			addLabel(0x134, 2, "RNT", ResourceHelper.GetMessage("GbaReg_RNT"));
			addLabel(0x140, 2, "JOYCNT", ResourceHelper.GetMessage("GbaReg_JOYCNT"));
			addLabel(0x150, 4, "JOYRECV", ResourceHelper.GetMessage("GbaReg_JOYRECV"));
			addLabel(0x154, 4, "JOYSEND", ResourceHelper.GetMessage("GbaReg_JOYSEND"));
			addLabel(0x158, 2, "JOYSTAT", ResourceHelper.GetMessage("GbaReg_JOYSTAT"));

			addLabel(0x200, 2, "IE", ResourceHelper.GetMessage("GbaReg_IE"));
			addLabel(0x202, 2, "IF", ResourceHelper.GetMessage("GbaReg_IF"));
			addLabel(0x204, 2, "WAITCNT", ResourceHelper.GetMessage("GbaReg_WAITCNT"));
			addLabel(0x208, 2, "IME", ResourceHelper.GetMessage("GbaReg_IME"));
			addLabel(0x300, 1, "POSTFLG", ResourceHelper.GetMessage("GbaReg_POSTFLG"));
			addLabel(0x301, 1, "HALTCNT", ResourceHelper.GetMessage("GbaReg_HALTCNT"));
		}

		private static void SetWsDefaultLabels()
		{
			Action<uint, uint, string, string> addLabel = (addr, length, label, desc) => {
				LabelManager.SetLabel(new CodeLabel() {
					Address = addr,
					Length = length,
					MemoryType = MemoryType.WsPort,
					Label = label,
					Comment = desc
				}, false);
			};

			/* Begin auto-generated labels from https://codeberg.org/WonderfulToolchain/hardware-definitions */
			addLabel(0xC0, 1, "WS_CART_BANK_ROML_PORT", ResourceHelper.GetMessage("WsReg_CART_BANK_ROML_PORT"));
			addLabel(0xC1, 1, "WS_CART_BANK_RAM_PORT", ResourceHelper.GetMessage("WsReg_CART_BANK_RAM_PORT"));
			addLabel(0xC2, 1, "WS_CART_BANK_ROM0_PORT", ResourceHelper.GetMessage("WsReg_CART_BANK_ROM0_PORT"));
			addLabel(0xC3, 1, "WS_CART_BANK_ROM1_PORT", ResourceHelper.GetMessage("WsReg_CART_BANK_ROM1_PORT"));
			addLabel(0xCE, 1, "WS_CART_BANK_FLASH_PORT", ResourceHelper.GetMessage("WsReg_CART_BANK_FLASH_PORT"));
			addLabel(0xCF, 1, "WS_CART_EXTBANK_ROML_PORT", ResourceHelper.GetMessage("WsReg_CART_EXTBANK_ROML_PORT"));
			addLabel(0xD0, 2, "WS_CART_EXTBANK_RAM_PORT", ResourceHelper.GetMessage("WsReg_CART_EXTBANK_RAM_PORT"));
			addLabel(0xD2, 2, "WS_CART_EXTBANK_ROM0_PORT", ResourceHelper.GetMessage("WsReg_CART_EXTBANK_ROM0_PORT"));
			addLabel(0xD4, 2, "WS_CART_EXTBANK_ROM1_PORT", ResourceHelper.GetMessage("WsReg_CART_EXTBANK_ROM1_PORT"));
			addLabel(0xC4, 2, "WS_CART_EEP_DATA_PORT", ResourceHelper.GetMessage("WsReg_CART_EEP_DATA_PORT"));
			addLabel(0xC6, 2, "WS_CART_EEP_COMMAND_PORT", ResourceHelper.GetMessage("WsReg_CART_EEP_COMMAND_PORT"));
			addLabel(0xC8, 1, "WS_CART_EEP_CTRL_PORT", "");
			addLabel(0xCC, 1, "WS_CART_GPIO_DIR_PORT", "");
			addLabel(0xCD, 1, "WS_CART_GPIO_DATA_PORT", "");
			addLabel(0xD6, 1, "WS_CART_KARNAK_CTRL_PORT", "");
			addLabel(0xD8, 1, "WS_CART_KARNAK_ADPCM_IN_PORT", "");
			addLabel(0xD9, 1, "WS_CART_KARNAK_ADPCM_OUT_PORT", "");
			addLabel(0xCA, 1, "WS_CART_RTC_CTRL_PORT", "");
			addLabel(0xCB, 1, "WS_CART_RTC_DATA_PORT", "");
			addLabel(0x00, 1, "WS_DISPLAY_CTRL_PORT", "");
			addLabel(0x01, 1, "WS_DISPLAY_BACK_PORT", ResourceHelper.GetMessage("WsReg_DISPLAY_BACK_PORT"));
			addLabel(0x02, 1, "WS_DISPLAY_LINE_PORT", ResourceHelper.GetMessage("WsReg_DISPLAY_LINE_PORT"));
			addLabel(0x03, 1, "WS_DISPLAY_LINE_IRQ_PORT", ResourceHelper.GetMessage("WsReg_DISPLAY_LINE_IRQ_PORT"));
			addLabel(0x04, 1, "WS_SPR_BASE_PORT", ResourceHelper.GetMessage("WsReg_SPR_BASE_PORT"));
			addLabel(0x05, 1, "WS_SPR_FIRST_PORT", ResourceHelper.GetMessage("WsReg_SPR_FIRST_PORT"));
			addLabel(0x06, 1, "WS_SPR_COUNT_PORT", ResourceHelper.GetMessage("WsReg_SPR_COUNT_PORT"));
			addLabel(0x07, 1, "WS_SCR_BASE_PORT", ResourceHelper.GetMessage("WsReg_SCR_BASE_PORT"));
			addLabel(0x08, 1, "WS_SCR2_WIN_X1_PORT", ResourceHelper.GetMessage("WsReg_SCR2_WIN_X1_PORT"));
			addLabel(0x09, 1, "WS_SCR2_WIN_Y1_PORT", ResourceHelper.GetMessage("WsReg_SCR2_WIN_Y1_PORT"));
			addLabel(0x0A, 1, "WS_SCR2_WIN_X2_PORT", ResourceHelper.GetMessage("WsReg_SCR2_WIN_X2_PORT"));
			addLabel(0x0B, 1, "WS_SCR2_WIN_Y2_PORT", ResourceHelper.GetMessage("WsReg_SCR2_WIN_Y2_PORT"));
			addLabel(0x0C, 1, "WS_SPR_WIN_X1_PORT", ResourceHelper.GetMessage("WsReg_SPR_WIN_X1_PORT"));
			addLabel(0x0D, 1, "WS_SPR_WIN_Y1_PORT", ResourceHelper.GetMessage("WsReg_SPR_WIN_Y1_PORT"));
			addLabel(0x0E, 1, "WS_SPR_WIN_X2_PORT", ResourceHelper.GetMessage("WsReg_SPR_WIN_X2_PORT"));
			addLabel(0x0F, 1, "WS_SPR_WIN_Y2_PORT", ResourceHelper.GetMessage("WsReg_SPR_WIN_Y2_PORT"));
			addLabel(0x10, 1, "WS_SCR1_SCRL_X_PORT", ResourceHelper.GetMessage("WsReg_SCR1_SCRL_X_PORT"));
			addLabel(0x11, 1, "WS_SCR1_SCRL_Y_PORT", ResourceHelper.GetMessage("WsReg_SCR1_SCRL_Y_PORT"));
			addLabel(0x12, 1, "WS_SCR2_SCRL_X_PORT", ResourceHelper.GetMessage("WsReg_SCR2_SCRL_X_PORT"));
			addLabel(0x13, 1, "WS_SCR2_SCRL_Y_PORT", ResourceHelper.GetMessage("WsReg_SCR2_SCRL_Y_PORT"));
			addLabel(0x14, 1, "WS_LCD_CTRL_PORT", ResourceHelper.GetMessage("WsReg_LCD_CTRL_PORT"));
			addLabel(0x15, 1, "WS_LCD_ICON_PORT", ResourceHelper.GetMessage("WsReg_LCD_ICON_PORT"));
			addLabel(0x16, 1, "WS_LCD_VTOTAL_PORT", ResourceHelper.GetMessage("WsReg_LCD_VTOTAL_PORT"));
			addLabel(0x17, 1, "WS_LCD_STN_VSYNC_PORT", ResourceHelper.GetMessage("WsReg_LCD_STN_VSYNC_PORT"));
			addLabel(0x18, 1, "WS_LCD_NEXT_LINE_PORT", ResourceHelper.GetMessage("WsReg_LCD_NEXT_LINE_PORT"));
			addLabel(0x1A, 1, "WS_LCD_ICON_LATCH_PORT", ResourceHelper.GetMessage("WsReg_LCD_ICON_LATCH_PORT"));
			addLabel(0x1C, 1, "WS_LCD_SHADE_01_PORT", "");
			addLabel(0x1D, 1, "WS_LCD_SHADE_23_PORT", "");
			addLabel(0x1E, 1, "WS_LCD_SHADE_45_PORT", "");
			addLabel(0x1F, 1, "WS_LCD_SHADE_67_PORT", "");

			addLabel(0x40, 2, "WS_GDMA_SOURCE_L_PORT", ResourceHelper.GetMessage("WsReg_GDMA_SOURCE_L_PORT"));
			addLabel(0x42, 1, "WS_GDMA_SOURCE_H_PORT", ResourceHelper.GetMessage("WsReg_GDMA_SOURCE_H_PORT"));
			addLabel(0x44, 2, "WS_GDMA_DEST_PORT", ResourceHelper.GetMessage("WsReg_GDMA_DEST_PORT"));
			addLabel(0x46, 2, "WS_GDMA_LENGTH_PORT", ResourceHelper.GetMessage("WsReg_GDMA_LENGTH_PORT"));
			addLabel(0x48, 1, "WS_GDMA_CTRL_PORT", ResourceHelper.GetMessage("WsReg_GDMA_CTRL_PORT"));
			addLabel(0x4A, 2, "WS_SDMA_SOURCE_L_PORT", ResourceHelper.GetMessage("WsReg_SDMA_SOURCE_L_PORT"));
			addLabel(0x4C, 1, "WS_SDMA_SOURCE_H_PORT", ResourceHelper.GetMessage("WsReg_SDMA_SOURCE_H_PORT"));
			addLabel(0x4E, 2, "WS_SDMA_LENGTH_L_PORT", ResourceHelper.GetMessage("WsReg_SDMA_LENGTH_L_PORT"));
			addLabel(0x50, 1, "WS_SDMA_LENGTH_H_PORT", ResourceHelper.GetMessage("WsReg_SDMA_LENGTH_H_PORT"));
			addLabel(0x52, 1, "WS_SDMA_CTRL_PORT", "");
			addLabel(0xBA, 2, "WS_IEEP_DATA_PORT", ResourceHelper.GetMessage("WsReg_IEEP_DATA_PORT"));
			addLabel(0xBC, 2, "WS_IEEP_COMMAND_PORT", ResourceHelper.GetMessage("WsReg_IEEP_COMMAND_PORT"));
			addLabel(0xBE, 1, "WS_IEEP_CTRL_PORT", "");
			addLabel(0x64, 2, "WS_HYPERV_OUT_L_PORT", "");
			addLabel(0x66, 2, "WS_HYPERV_OUT_R_PORT", "");
			addLabel(0x6A, 2, "WS_HYPERV_CTRL_PORT", "");
			addLabel(0xB0, 1, "WS_INT_VECTOR_PORT", ResourceHelper.GetMessage("WsReg_INT_VECTOR_PORT"));
			addLabel(0xB2, 1, "WS_INT_ENABLE_PORT", "");
			addLabel(0xB4, 1, "WS_INT_STATUS_PORT", "");
			addLabel(0xB6, 1, "WS_INT_ACK_PORT", "");
			addLabel(0xB7, 1, "WS_INT_NMI_CTRL_PORT", ResourceHelper.GetMessage("WsReg_INT_NMI_CTRL_PORT"));
			addLabel(0xB5, 1, "WS_KEY_SCAN_PORT", ResourceHelper.GetMessage("WsReg_KEY_SCAN_PORT"));
			addLabel(0x60, 1, "WS_SYSTEM_CTRL_COLOR_PORT", "");
			addLabel(0x62, 1, "WS_SYSTEM_CTRL_COLOR2_PORT", "");
			addLabel(0xA0, 1, "WS_SYSTEM_CTRL_PORT", "");
			addLabel(0xA3, 1, "WS_SYSTEM_TEST_PORT", "");
			addLabel(0x80, 2, "WS_SOUND_FREQ_CH1_PORT", ResourceHelper.GetMessage("WsReg_SOUND_FREQ_CH1_PORT"));
			addLabel(0x82, 2, "WS_SOUND_FREQ_CH2_PORT", ResourceHelper.GetMessage("WsReg_SOUND_FREQ_CH2_PORT"));
			addLabel(0x84, 2, "WS_SOUND_FREQ_CH3_PORT", ResourceHelper.GetMessage("WsReg_SOUND_FREQ_CH3_PORT"));
			addLabel(0x86, 2, "WS_SOUND_FREQ_CH4_PORT", ResourceHelper.GetMessage("WsReg_SOUND_FREQ_CH4_PORT"));
			addLabel(0x88, 1, "WS_SOUND_VOL_CH1_PORT", ResourceHelper.GetMessage("WsReg_SOUND_VOL_CH1_PORT"));
			// addLabel(0x89, 1, "WS_SOUND_VOL_CH2_PORT", "Sound channel 2 volume.");
			// addLabel(0x89, 1, "WS_SOUND_VOICE_SAMPLE_PORT", "Sound channel 2 unsigned PCM sample; used in voice mode.");
			addLabel(0x8A, 1, "WS_SOUND_VOL_CH3_PORT", ResourceHelper.GetMessage("WsReg_SOUND_VOL_CH3_PORT"));
			addLabel(0x8B, 1, "WS_SOUND_VOL_CH4_PORT", ResourceHelper.GetMessage("WsReg_SOUND_VOL_CH4_PORT"));
			addLabel(0x8C, 1, "WS_SOUND_SWEEP_PORT", ResourceHelper.GetMessage("WsReg_SOUND_SWEEP_PORT"));
			addLabel(0x8D, 1, "WS_SOUND_SWEEP_TIME_PORT", ResourceHelper.GetMessage("WsReg_SOUND_SWEEP_TIME_PORT"));
			addLabel(0x8E, 1, "WS_SOUND_NOISE_CTRL_PORT", "");
			addLabel(0x8F, 1, "WS_SOUND_WAVE_BASE_PORT", ResourceHelper.GetMessage("WsReg_SOUND_WAVE_BASE_PORT"));
			addLabel(0x90, 1, "WS_SOUND_CH_CTRL_PORT", ResourceHelper.GetMessage("WsReg_SOUND_CH_CTRL_PORT"));
			addLabel(0x91, 1, "WS_SOUND_OUT_CTRL_PORT", ResourceHelper.GetMessage("WsReg_SOUND_OUT_CTRL_PORT"));
			addLabel(0x92, 2, "WS_SOUND_NOISE_LFSR_PORT", ResourceHelper.GetMessage("WsReg_SOUND_NOISE_LFSR_PORT"));
			addLabel(0x94, 1, "WS_SOUND_VOICE_VOL_PORT", "");
			addLabel(0x95, 1, "WS_SOUND_TEST_PORT", ResourceHelper.GetMessage("WsReg_SOUND_TEST_PORT"));
			addLabel(0x96, 2, "WS_SOUND_TEST_CHOUT_R_PORT", ResourceHelper.GetMessage("WsReg_SOUND_TEST_CHOUT_R_PORT"));
			addLabel(0x98, 2, "WS_SOUND_TEST_CHOUT_L_PORT", ResourceHelper.GetMessage("WsReg_SOUND_TEST_CHOUT_L_PORT"));
			addLabel(0x9A, 2, "WS_SOUND_TEST_CHOUT_M_PORT", ResourceHelper.GetMessage("WsReg_SOUND_TEST_CHOUT_M_PORT"));
			addLabel(0x9E, 1, "WS_SOUND_SPEAKER_VOL_PORT", ResourceHelper.GetMessage("WsReg_SOUND_SPEAKER_VOL_PORT"));
			addLabel(0xA2, 1, "WS_TIMER_CTRL_PORT", "");
			addLabel(0xA4, 2, "WS_TIMER_HBL_RELOAD_PORT", ResourceHelper.GetMessage("WsReg_TIMER_HBL_RELOAD_PORT"));
			addLabel(0xA6, 2, "WS_TIMER_VBL_RELOAD_PORT", ResourceHelper.GetMessage("WsReg_TIMER_VBL_RELOAD_PORT"));
			addLabel(0xA8, 2, "WS_TIMER_HBL_COUNTER_PORT", ResourceHelper.GetMessage("WsReg_TIMER_HBL_COUNTER_PORT"));
			addLabel(0xAA, 2, "WS_TIMER_VBL_COUNTER_PORT", ResourceHelper.GetMessage("WsReg_TIMER_VBL_COUNTER_PORT"));
			addLabel(0xB1, 1, "WS_UART_DATA_PORT", "");
			addLabel(0xB3, 1, "WS_UART_CTRL_PORT", "");
			/* End auto-generated labels */

			addLabel(0x68, 1, "WS_HYPERV_IN_L_PORT", "");
			addLabel(0x69, 1, "WS_HYPERV_IN_R_PORT", "");
			addLabel(0x89, 1, "WS_SOUND_VOL_CH2_PORT", ResourceHelper.GetMessage("WsReg_SOUND_VOL_CH2_PORT"));

			addLabel(0x20, 2, "WS_SCR_PAL_0_PORT", ResourceHelper.GetMessage("WsReg_SCR_PAL_0_PORT"));
			addLabel(0x22, 2, "WS_SCR_PAL_1_PORT", ResourceHelper.GetMessage("WsReg_SCR_PAL_1_PORT"));
			addLabel(0x24, 2, "WS_SCR_PAL_2_PORT", ResourceHelper.GetMessage("WsReg_SCR_PAL_2_PORT"));
			addLabel(0x26, 2, "WS_SCR_PAL_3_PORT", ResourceHelper.GetMessage("WsReg_SCR_PAL_3_PORT"));
			addLabel(0x28, 2, "WS_SCR_PAL_4_PORT", ResourceHelper.GetMessage("WsReg_SCR_PAL_4_PORT"));
			addLabel(0x2A, 2, "WS_SCR_PAL_5_PORT", ResourceHelper.GetMessage("WsReg_SCR_PAL_5_PORT"));
			addLabel(0x2C, 2, "WS_SCR_PAL_6_PORT", ResourceHelper.GetMessage("WsReg_SCR_PAL_6_PORT"));
			addLabel(0x2E, 2, "WS_SCR_PAL_7_PORT", ResourceHelper.GetMessage("WsReg_SCR_PAL_7_PORT"));
			addLabel(0x30, 2, "WS_SCR_PAL_8_PORT", ResourceHelper.GetMessage("WsReg_SCR_PAL_8_PORT"));
			addLabel(0x32, 2, "WS_SCR_PAL_9_PORT", ResourceHelper.GetMessage("WsReg_SCR_PAL_9_PORT"));
			addLabel(0x34, 2, "WS_SCR_PAL_10_PORT", ResourceHelper.GetMessage("WsReg_SCR_PAL_10_PORT"));
			addLabel(0x36, 2, "WS_SCR_PAL_11_PORT", ResourceHelper.GetMessage("WsReg_SCR_PAL_11_PORT"));
			addLabel(0x38, 2, "WS_SCR_PAL_12_PORT", ResourceHelper.GetMessage("WsReg_SCR_PAL_12_PORT"));
			addLabel(0x3A, 2, "WS_SCR_PAL_13_PORT", ResourceHelper.GetMessage("WsReg_SCR_PAL_13_PORT"));
			addLabel(0x3C, 2, "WS_SCR_PAL_14_PORT", ResourceHelper.GetMessage("WsReg_SCR_PAL_14_PORT"));
			addLabel(0x3E, 2, "WS_SCR_PAL_15_PORT", ResourceHelper.GetMessage("WsReg_SCR_PAL_15_PORT"));
		}
	}
}
