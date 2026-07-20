using Mesen.Debugger.ViewModels;
using Mesen.Interop;
using Mesen.Localization;
using System;
using System.Collections.Generic;
using static Mesen.Debugger.ViewModels.RegEntry;

namespace Mesen.Debugger.RegisterViewer;

public class SnesRegisterViewer
{
	public static List<RegisterViewerTab> GetTabs(ref SnesState snesState, HashSet<CpuType> cpuTypes, byte snesReg4210, byte snesReg4211, byte snesReg4212)
	{
		List<RegisterViewerTab> tabs = new() {
			GetSnesCpuTab(ref snesState, snesReg4210, snesReg4211, snesReg4212),
			GetSnesPpuTab(ref snesState),
			GetSnesDmaTab(ref snesState),
			GetSnesSpcTab(ref snesState),
			GetSnesDspTab(ref snesState)
		};

		if(cpuTypes.Contains(CpuType.Sa1)) {
			tabs.Add(GetSnesSa1Tab(ref snesState));
		} else if(cpuTypes.Contains(CpuType.Gameboy)) {
			GbState gbState = DebugApi.GetConsoleState<GbState>(ConsoleType.Gameboy);
			string tabPrefix = "GB - ";
			tabs.Add(GbRegisterViewer.GetGbLcdTab(ref gbState, tabPrefix));
			tabs.Add(GbRegisterViewer.GetGbApuTab(ref gbState, tabPrefix));
			tabs.Add(GbRegisterViewer.GetGbMiscTab(ref gbState, tabPrefix));
		} else if(cpuTypes.Contains(CpuType.Gsu)) {
			tabs.Add(GetSnesGsuTab(ref snesState.Gsu));
		} else if(cpuTypes.Contains(CpuType.St018)) {
			tabs.Add(GetSnesSt018Tab(ref snesState.St018));
		}

		return tabs;
	}

	private static RegisterViewerTab GetSnesGsuTab(ref GsuState gsu)
	{
		List<RegEntry> entries = new List<RegEntry>() {
			//new RegEntry("$3033.0", "Backup RAM Enabled", gsu.BackupRamEnabled),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Common_Registers")),
			new RegEntry("$3037.5", ResourceHelper.GetMessage("RegView_Snes_HighSpeedMode"), gsu.HighSpeedMode),
			new RegEntry("$3037.7", ResourceHelper.GetMessage("RegView_Snes_IRQDisabled"), gsu.IrqDisabled),
			new RegEntry("$3038", ResourceHelper.GetMessage("RegView_Snes_ScreenBaseAddress"), gsu.ScreenBase, Format.X8),
			new RegEntry("$3039.0", ResourceHelper.GetMessage("RegView_Snes_ClockSelect"), gsu.ClockSelect),
			new RegEntry("$303A.0-1", ResourceHelper.GetMessage("RegView_Snes_ColorGradient"), gsu.PlotBpp + ResourceHelper.GetMessage("RegView_Snes_BPP"), gsu.ColorGradient),
			new RegEntry("$303A.2+5", ResourceHelper.GetMessage("RegView_Snes_ScreenHeight"), gsu.ScreenHeight switch {
				0 => ResourceHelper.GetMessage("RegView_Snes_128px"),
				1 => ResourceHelper.GetMessage("RegView_Snes_160px"),
				2 => ResourceHelper.GetMessage("RegView_Snes_192px"),
				3 or _ => ResourceHelper.GetMessage("RegView_Snes_OBJMode"),
			}, gsu.ScreenHeight),
			new RegEntry("$303A.3", ResourceHelper.GetMessage("RegView_Snes_GSU_RAMAccessEnabled"), gsu.GsuRamAccess),
			new RegEntry("$303A.4", ResourceHelper.GetMessage("RegView_Snes_GSU_ROMAccessEnabled"), gsu.GsuRomAccess),

			new RegEntry("", ResourceHelper.GetMessage("RegView_Snes_PlotOptionRegisterCMODE")),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Snes_Transparent"), gsu.PlotTransparent),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Snes_Dither"), gsu.PlotDither),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Snes_ColorHighNibble"), gsu.ColorHighNibble),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Snes_ColorFreezeHigh"), gsu.ColorFreezeHigh),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Snes_ObjectMode"), gsu.ObjMode),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Snes_Transparent"), gsu.PlotTransparent),
		};

		return new RegisterViewerTab(ResourceHelper.GetMessage("RegView_Snes_GSU"), entries);
	}

	private static RegisterViewerTab GetSnesSt018Tab(ref St018State state)
	{
		List<RegEntry> entries = new List<RegEntry>() {
			new RegEntry("", ResourceHelper.GetMessage("RegView_Snes_SNESRegisters")),
			new RegEntry("$3800", ResourceHelper.GetMessage("RegView_Snes_ARM_SNESData"), state.DataSnes),
			new RegEntry("$3804.0", ResourceHelper.GetMessage("RegView_Snes_ARM_SNESDataReady"), state.HasDataForSnes),
			new RegEntry("$3804.2", ResourceHelper.GetMessage("RegView_Common_Ack"), state.Ack),
			new RegEntry("$3804.3", ResourceHelper.GetMessage("RegView_Snes_SNES_ARMData"), state.HasDataForArm),
			new RegEntry("$3804.7", ResourceHelper.GetMessage("RegView_Snes_ARMCPURest"), state.ArmReset),

			new RegEntry("", ResourceHelper.GetMessage("RegView_Snes_ST018Registers")),
			new RegEntry("$40000010", ResourceHelper.GetMessage("RegView_Snes_ARM_SNESData"), state.DataArm),
			new RegEntry("$40000020.0", ResourceHelper.GetMessage("RegView_Snes_ARM_SNESDataReady"), state.HasDataForSnes),
			new RegEntry("$40000020.2", ResourceHelper.GetMessage("RegView_Common_Ack"), state.Ack),
			new RegEntry("$40000020.3", ResourceHelper.GetMessage("RegView_Snes_SNES_ARMData"), state.HasDataForArm),
			new RegEntry("$40000020.7", ResourceHelper.GetMessage("RegView_Snes_ARMCPURest"), state.ArmReset),
		};

		return new RegisterViewerTab(ResourceHelper.GetMessage("RegView_Snes_ST018"), entries);
	}

	private static RegisterViewerTab GetSnesSa1Tab(ref SnesState state)
	{
		Sa1State sa1 = state.Sa1;

		List<RegEntry> entries = new List<RegEntry>() {
			new RegEntry("$2200", ResourceHelper.GetMessage("RegView_Snes_SA1_CPUControl")),
			new RegEntry("$2200.0-3", ResourceHelper.GetMessage("RegView_Snes_Message"), sa1.Sa1MessageReceived, Format.X8),
			new RegEntry("$2200.4", ResourceHelper.GetMessage("RegView_Snes_SA1_NMIRequested"), sa1.Sa1NmiRequested),
			new RegEntry("$2200.5", ResourceHelper.GetMessage("RegView_Snes_Reset"), sa1.Sa1Reset),
			new RegEntry("$2200.6", ResourceHelper.GetMessage("RegView_Snes_Wait"), sa1.Sa1Wait),
			new RegEntry("$2200.7", ResourceHelper.GetMessage("RegView_Snes_SA1_IRQRequested"), sa1.Sa1IrqRequested),

			new RegEntry("$2201", ResourceHelper.GetMessage("RegView_Snes_SCPUInterruptEnable")),
			new RegEntry("$2201.5", ResourceHelper.GetMessage("RegView_Snes_CharacterConversionIRQEnable"), sa1.CharConvIrqEnabled),
			new RegEntry("$2201.7", ResourceHelper.GetMessage("RegView_Common_IRQEnabled"), sa1.CpuIrqEnabled),

			new RegEntry("$2202", ResourceHelper.GetMessage("RegView_Snes_SCPUInterruptClear")),
			new RegEntry("$2202.5", ResourceHelper.GetMessage("RegView_Snes_CharacterIRQFlag"), sa1.CharConvIrqFlag),
			new RegEntry("$2202.7", ResourceHelper.GetMessage("RegView_Snes_IRQFlag"), sa1.CpuIrqRequested),

			new RegEntry("$2203/4", ResourceHelper.GetMessage("RegView_Snes_SA1_ResetVector"), sa1.Sa1ResetVector, Format.X16),
			new RegEntry("$2205/6", ResourceHelper.GetMessage("RegView_Snes_SA1_NMIVector"), sa1.Sa1NmiVector, Format.X16),
			new RegEntry("$2207/8", ResourceHelper.GetMessage("RegView_Snes_SA1_IRQVector"), sa1.Sa1IrqVector, Format.X16),

			new RegEntry("$2209", ResourceHelper.GetMessage("RegView_Snes_SCPUControl")),
			new RegEntry("$2209.0-3", ResourceHelper.GetMessage("RegView_Snes_Message"), sa1.CpuMessageReceived, Format.X8),
			new RegEntry("$2209.4", ResourceHelper.GetMessage("RegView_Snes_UseNMIVector"), sa1.UseCpuNmiVector),
			new RegEntry("$2209.6", ResourceHelper.GetMessage("RegView_Snes_UseIRQVector"), sa1.UseCpuIrqVector),
			new RegEntry("$2209.7", ResourceHelper.GetMessage("RegView_Snes_IRQRequested"), sa1.CpuIrqRequested),

			new RegEntry("$220A", ResourceHelper.GetMessage("RegView_Snes_SA1_CPUInterruptEnable")),
			new RegEntry("$220A.4", ResourceHelper.GetMessage("RegView_Snes_SA1_NMIEnabled"), sa1.Sa1NmiEnabled),
			new RegEntry("$220A.5", ResourceHelper.GetMessage("RegView_Snes_DMAIRQEnabled"), sa1.DmaIrqEnabled),
			new RegEntry("$220A.6", ResourceHelper.GetMessage("RegView_Snes_TimerIRQEnabled"), sa1.TimerIrqEnabled),
			new RegEntry("$220A.7", ResourceHelper.GetMessage("RegView_Snes_SA1_IRQEnabled"), sa1.Sa1IrqEnabled),

			new RegEntry("$220B", ResourceHelper.GetMessage("RegView_Snes_SCPUInterruptClear")),
			new RegEntry("$220B.4", ResourceHelper.GetMessage("RegView_Snes_SA1_NMIRequested"), sa1.Sa1NmiRequested),
			new RegEntry("$220B.5", ResourceHelper.GetMessage("RegView_Snes_DMAIRQFlag"), sa1.DmaIrqFlag),
			new RegEntry("$220B.7", ResourceHelper.GetMessage("RegView_Snes_SA1_IRQRequested"), sa1.Sa1IrqRequested),

			new RegEntry("$220C/D", ResourceHelper.GetMessage("RegView_Snes_SCPUNMIVector"), sa1.CpuNmiVector, Format.X16),
			new RegEntry("$220E/F", ResourceHelper.GetMessage("RegView_Snes_SCPUIRQVector"), sa1.CpuIrqVector, Format.X16),

			new RegEntry("$2210", ResourceHelper.GetMessage("RegView_Snes_H_VTimerControl")),
			new RegEntry("$2210.0", ResourceHelper.GetMessage("RegView_Snes_HorizontalTimerEnabled"), sa1.HorizontalTimerEnabled),
			new RegEntry("$2210.1", ResourceHelper.GetMessage("RegView_Snes_VerticalTimerEnabled"), sa1.VerticalTimerEnabled),
			new RegEntry("$2210.7", ResourceHelper.GetMessage("RegView_Snes_LinearTimer"), sa1.UseLinearTimer),

			new RegEntry("$2212/3", ResourceHelper.GetMessage("RegView_Snes_H_Timer"), sa1.HTimer, Format.X16),
			new RegEntry("$2214/5", ResourceHelper.GetMessage("RegView_Snes_V_Timer"), sa1.VTimer, Format.X16),

			new RegEntry("", ResourceHelper.GetMessage("RegView_Snes_ROM_BWRAM_IRAMMappings")),
			new RegEntry("$2220", ResourceHelper.GetMessage("RegView_Snes_MMCBankC"), sa1.Banks[0], Format.X8),
			new RegEntry("$2221", ResourceHelper.GetMessage("RegView_Snes_MMCBankD"), sa1.Banks[1], Format.X8),
			new RegEntry("$2222", ResourceHelper.GetMessage("RegView_Snes_MMCBankE"), sa1.Banks[2], Format.X8),
			new RegEntry("$2223", ResourceHelper.GetMessage("RegView_Snes_MMCBankF"), sa1.Banks[3], Format.X8),

			new RegEntry("$2224", ResourceHelper.GetMessage("RegView_Snes_SCPUBWRAMBank"), sa1.CpuBwBank, Format.X8),
			new RegEntry("$2225.0-6", ResourceHelper.GetMessage("RegView_Snes_SA1_CPUBWRAMBank"), sa1.Sa1BwBank, Format.X8),
			new RegEntry("$2225.7", ResourceHelper.GetMessage("RegView_Snes_SA1_CPUBWRAMMode"), sa1.Sa1BwMode, Format.X8),
			new RegEntry("$2226.7", ResourceHelper.GetMessage("RegView_Snes_SCPUBWRAMWriteEnabled"), sa1.CpuBwWriteEnabled),
			new RegEntry("$2227.7", ResourceHelper.GetMessage("RegView_Snes_SA1_BWRAMWriteEnabled"), sa1.Sa1BwWriteEnabled),
			new RegEntry("$2228.0-3", ResourceHelper.GetMessage("RegView_Snes_SCPUBWRAMWriteProtectedArea"), sa1.BwWriteProtectedArea, Format.X8),
			new RegEntry("$2229", ResourceHelper.GetMessage("RegView_Snes_SCPUI_RAMWriteProtection"), sa1.CpuIRamWriteProtect, Format.X8),
			new RegEntry("$222A", ResourceHelper.GetMessage("RegView_Snes_SA1_CPUBWRAMWriteProtection"), sa1.Sa1IRamWriteProtect, Format.X8),

			new RegEntry("$2230", ResourceHelper.GetMessage("RegView_Snes_DMAControl")),
			new RegEntry("$2230.0-1", ResourceHelper.GetMessage("RegView_Snes_DMASourceDevice"), sa1.DmaSrcDevice),
			new RegEntry("$2230.2-3", ResourceHelper.GetMessage("RegView_Snes_DMADestinationDevice"), sa1.DmaDestDevice),
			new RegEntry("$2230.4", ResourceHelper.GetMessage("RegView_Snes_AutomaticDMACharacterConversion"), sa1.DmaCharConvAuto),
			new RegEntry("$2230.5", ResourceHelper.GetMessage("RegView_Snes_DMACharacterConversion"), sa1.DmaCharConv),
			new RegEntry("$2230.6", ResourceHelper.GetMessage("RegView_Snes_DMAPriority"), sa1.DmaPriority),
			new RegEntry("$2230.7", ResourceHelper.GetMessage("RegView_Snes_DMAEnabled"), sa1.DmaEnabled),

			new RegEntry("$2231.0-1", ResourceHelper.GetMessage("RegView_Snes_CharacterFormatBPP"), sa1.CharConvBpp),
			new RegEntry("$2231.2-5", ResourceHelper.GetMessage("RegView_Snes_CharacterConversionWidth"), sa1.CharConvWidth, Format.X8),
			new RegEntry("$2231.7", ResourceHelper.GetMessage("RegView_Snes_CharacterDMActive"), sa1.CharConvDmaActive),

			new RegEntry("$2232/3/4", ResourceHelper.GetMessage("RegView_Snes_DMASourceAddress"), sa1.DmaSrcAddr, Format.X24),
			new RegEntry("$2235/6/7", ResourceHelper.GetMessage("RegView_Snes_DMADestinationAddress"), sa1.DmaDestAddr, Format.X24),

			new RegEntry("$2238/9", ResourceHelper.GetMessage("RegView_Snes_DMASize"), sa1.DmaSize, Format.X16),
			new RegEntry("$223F.7", ResourceHelper.GetMessage("RegView_Snes_BW_RAM2BppMode"), sa1.BwRam2BppMode)
		};

		entries.Add(new RegEntry("", ResourceHelper.GetMessage("RegView_Snes_BitmapRegisterFile")));
		for(int i = 0; i < 8; i++) {
			entries.Add(new RegEntry("$224" + i, ResourceHelper.GetMessage("RegView_Snes_BRF") + i, sa1.BitmapRegister1[i]));
		}
		for(int i = 0; i < 8; i++) {
			entries.Add(new RegEntry("$224" + (8 + i).ToString("X"), ResourceHelper.GetMessage("RegView_Snes_BRF") + (i + 8), sa1.BitmapRegister2[i]));
		}

		entries.AddRange(new List<RegEntry>() {
			new RegEntry("", ResourceHelper.GetMessage("RegView_Snes_MathRegisters")),
			new RegEntry("$2250.0-1", ResourceHelper.GetMessage("RegView_Snes_MathOperation"), sa1.MathOp),
			new RegEntry("$2251/2", ResourceHelper.GetMessage("RegView_Snes_MultiplicandDividend"), sa1.MultiplicandDividend, Format.X16),
			new RegEntry("$2253/4", ResourceHelper.GetMessage("RegView_Snes_MultiplierDivisor"), sa1.MultiplierDivisor, Format.X16),

			new RegEntry("", ResourceHelper.GetMessage("RegView_Snes_VariableLengthRegisters")),
			new RegEntry("$2258", ResourceHelper.GetMessage("RegView_Snes_VariableLengthBitProcessing")),
			new RegEntry("$2258.0-3", ResourceHelper.GetMessage("RegView_Snes_VariableLengthBitCount"), sa1.VarLenBitCount, Format.X8),
			new RegEntry("$2258.7", ResourceHelper.GetMessage("RegView_Snes_VariableLengthAutoIncrement"), sa1.VarLenAutoInc),
			new RegEntry("$2259/A/B", ResourceHelper.GetMessage("RegView_Snes_VariableLengthAddress"), sa1.VarLenAddress, Format.X24),

			new RegEntry("$2300", ResourceHelper.GetMessage("RegView_Snes_SCPUStatusFlags")),
			new RegEntry("$2300.0-3", ResourceHelper.GetMessage("RegView_Snes_MessageReceived"), sa1.CpuMessageReceived, Format.X8),
			new RegEntry("$2300.4", ResourceHelper.GetMessage("RegView_Snes_UseNMIVector"), sa1.UseCpuNmiVector),
			new RegEntry("$2300.5", ResourceHelper.GetMessage("RegView_Snes_CharacterConversionIRQFlag"), sa1.CharConvIrqFlag),
			new RegEntry("$2300.6", ResourceHelper.GetMessage("RegView_Snes_UseIRQVector"), sa1.UseCpuIrqVector),
			new RegEntry("$2300.7", ResourceHelper.GetMessage("RegView_Snes_IRQRequested"), sa1.CpuIrqRequested),

			new RegEntry("$2301", ResourceHelper.GetMessage("RegView_Snes_SA1_StatusFlags")),
			new RegEntry("$2301.0-3", ResourceHelper.GetMessage("RegView_Snes_MessageReceived"), sa1.Sa1MessageReceived, Format.X8),
			new RegEntry("$2301.4", ResourceHelper.GetMessage("RegView_Snes_SA1_NMIRequested"), sa1.Sa1NmiRequested),
			new RegEntry("$2301.5", ResourceHelper.GetMessage("RegView_Snes_DMAIRQFlag"), sa1.DmaIrqFlag),
			new RegEntry("$2301.7", ResourceHelper.GetMessage("RegView_Snes_SA1_IRQRequested"), sa1.Sa1IrqRequested),

			new RegEntry("$2302/3", ResourceHelper.GetMessage("RegView_Snes_SA1_HCounter"), 0, Format.X16),
			new RegEntry("$2304/5", ResourceHelper.GetMessage("RegView_Snes_SA1_VCounter"), 0, Format.X16),

			new RegEntry("$2306/7/8/9/A", ResourceHelper.GetMessage("RegView_Snes_MathResult"), sa1.MathOpResult),
			new RegEntry("$230B.7", ResourceHelper.GetMessage("RegView_Snes_MathOverflow"), sa1.MathOverflow)
		});

		return new RegisterViewerTab(ResourceHelper.GetMessage("RegView_Snes_SA1"), entries);
	}

	private static RegisterViewerTab GetSnesPpuTab(ref SnesState state)
	{
		SnesPpuState ppu = state.Ppu;

		string GetLayerSize(LayerConfig layer)
		{
			if(layer.DoubleWidth && layer.DoubleHeight) return ResourceHelper.GetMessage("RegView_Snes_Size64x64");
			if(layer.DoubleWidth) return ResourceHelper.GetMessage("RegView_Snes_Size64x32");
			if(layer.DoubleHeight) return ResourceHelper.GetMessage("RegView_Snes_Size32x64");
			return ResourceHelper.GetMessage("RegView_Snes_Size32x32");
		}

		List<RegEntry> entries = new List<RegEntry>() {
			new RegEntry("", ResourceHelper.GetMessage("RegView_Common_State")),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Common_CycleH"), ppu.Cycle),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Snes_HClock"), ppu.HClock),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Common_ScanlineV"), ppu.Scanline),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Common_FrameNumber"), ppu.FrameCount),

			new RegEntry("$2100", ResourceHelper.GetMessage("RegView_Snes_Brightness")),
			new RegEntry("$2100.0-3", ResourceHelper.GetMessage("RegView_Snes_Brightness"), ppu.ScreenBrightness),
			new RegEntry("$2100.7", ResourceHelper.GetMessage("RegView_Snes_ForcedBlank"), ppu.ForcedBlank),
			new RegEntry("$2101", ResourceHelper.GetMessage("RegView_Snes_OAMSettings")),
			new RegEntry("$2101.0-2", ResourceHelper.GetMessage("RegView_Snes_OAMBaseAddress"), ppu.OamBaseAddress, Format.X16),
			new RegEntry("$2101.3-4", ResourceHelper.GetMessage("RegView_Snes_OAMSecondTableAddress"), (ppu.OamBaseAddress + ppu.OamAddressOffset) & 0x7FFF, Format.X16),
			new RegEntry("$2101.5-7", ResourceHelper.GetMessage("RegView_Snes_OAMSizeMode"), ppu.OamMode),
			new RegEntry("$2102-2103", ResourceHelper.GetMessage("RegView_Snes_OAMBaseAddress"), ppu.OamRamAddress),
			new RegEntry("$2103.7", ResourceHelper.GetMessage("RegView_Snes_OAMPriority"), ppu.EnableOamPriority),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Snes_OAMAddress"), ppu.InternalOamRamAddress),

			new RegEntry("$2105", ResourceHelper.GetMessage("RegView_Snes_BGModeSize")),
			new RegEntry("$2105.0-2", ResourceHelper.GetMessage("RegView_Snes_BGMode"), ppu.BgMode),
			new RegEntry("$2105.3", ResourceHelper.GetMessage("RegView_Snes_Mode1BG3Priority"), ppu.Mode1Bg3Priority),
			new RegEntry("$2105.4", ResourceHelper.GetMessage("RegView_Snes_BG1_16x16Tiles"), ppu.Layers[0].LargeTiles),
			new RegEntry("$2105.5", ResourceHelper.GetMessage("RegView_Snes_BG2_16x16Tiles"), ppu.Layers[1].LargeTiles),
			new RegEntry("$2105.6", ResourceHelper.GetMessage("RegView_Snes_BG3_16x16Tiles"), ppu.Layers[2].LargeTiles),
			new RegEntry("$2105.7", ResourceHelper.GetMessage("RegView_Snes_BG4_16x16Tiles"), ppu.Layers[3].LargeTiles),

			new RegEntry("$2106", ResourceHelper.GetMessage("RegView_Snes_Mosaic")),
			new RegEntry("$2106.0", ResourceHelper.GetMessage("RegView_Snes_BG1MosaicEnabled"), (ppu.MosaicEnabled & 0x01) != 0),
			new RegEntry("$2106.1", ResourceHelper.GetMessage("RegView_Snes_BG2MosaicEnabled"), (ppu.MosaicEnabled & 0x02) != 0),
			new RegEntry("$2106.2", ResourceHelper.GetMessage("RegView_Snes_BG3MosaicEnabled"), (ppu.MosaicEnabled & 0x04) != 0),
			new RegEntry("$2106.3", ResourceHelper.GetMessage("RegView_Snes_BG4MosaicEnabled"), (ppu.MosaicEnabled & 0x08) != 0),
			new RegEntry("$2106.4-7", ResourceHelper.GetMessage("RegView_Snes_MosaicSize"), (ppu.MosaicSize - 1).ToString() + " (" + ppu.MosaicSize.ToString() + "x" + ppu.MosaicSize.ToString() + ")", ppu.MosaicSize - 1),

			new RegEntry("$2107 - $210A", ResourceHelper.GetMessage("RegView_Snes_TilemapAddressesSizes")),
			new RegEntry("$2107.0-1", ResourceHelper.GetMessage("RegView_Snes_BG1Size"), GetLayerSize(ppu.Layers[0]), (ppu.Layers[0].DoubleWidth ? 0x01 : 0) | (ppu.Layers[0].DoubleHeight ? 0x02 : 0)),
			new RegEntry("$2107.2-6", ResourceHelper.GetMessage("RegView_Snes_BG1Address"), ppu.Layers[0].TilemapAddress, Format.X16),
			new RegEntry("$2108.0-1", ResourceHelper.GetMessage("RegView_Snes_BG2Size"), GetLayerSize(ppu.Layers[1]), (ppu.Layers[1].DoubleWidth ? 0x01 : 0) | (ppu.Layers[1].DoubleHeight ? 0x02 : 0)),
			new RegEntry("$2108.2-6", ResourceHelper.GetMessage("RegView_Snes_BG2Address"), ppu.Layers[1].TilemapAddress, Format.X16),
			new RegEntry("$2109.0-1", ResourceHelper.GetMessage("RegView_Snes_BG3Size"), GetLayerSize(ppu.Layers[2]), (ppu.Layers[2].DoubleWidth ? 0x01 : 0) | (ppu.Layers[2].DoubleHeight ? 0x02 : 0)),
			new RegEntry("$2109.2-6", ResourceHelper.GetMessage("RegView_Snes_BG3Address"), ppu.Layers[2].TilemapAddress, Format.X16),
			new RegEntry("$210A.0-1", ResourceHelper.GetMessage("RegView_Snes_BG4Size"), GetLayerSize(ppu.Layers[3]), (ppu.Layers[3].DoubleWidth ? 0x01 : 0) | (ppu.Layers[3].DoubleHeight ? 0x02 : 0)),
			new RegEntry("$210A.2-6", ResourceHelper.GetMessage("RegView_Snes_BG4Address"), ppu.Layers[3].TilemapAddress, Format.X16),

			new RegEntry("$210B - $210C", ResourceHelper.GetMessage("RegView_Snes_TileAddresses")),
			new RegEntry("$210B.0-2", ResourceHelper.GetMessage("RegView_Snes_BG1TileAddress"), ppu.Layers[0].ChrAddress, Format.X16),
			new RegEntry("$210B.4-6", ResourceHelper.GetMessage("RegView_Snes_BG2TileAddress"), ppu.Layers[1].ChrAddress, Format.X16),
			new RegEntry("$210C.0-2", ResourceHelper.GetMessage("RegView_Snes_BG3TileAddress"), ppu.Layers[2].ChrAddress, Format.X16),
			new RegEntry("$210C.4-6", ResourceHelper.GetMessage("RegView_Snes_BG4TileAddress"), ppu.Layers[3].ChrAddress, Format.X16),

			new RegEntry("$210D - $2114", ResourceHelper.GetMessage("RegView_Snes_H_VScrollOffsets")),
			new RegEntry("$210D", ResourceHelper.GetMessage("RegView_Snes_BG1HOffset"), ppu.Layers[0].HScroll, Format.X16),
			new RegEntry("$210D", ResourceHelper.GetMessage("RegView_Snes_Mode7HOffset"), ppu.Mode7.HScroll, Format.X16),
			new RegEntry("$210E", ResourceHelper.GetMessage("RegView_Snes_BG1VOffset"), ppu.Layers[0].VScroll, Format.X16),
			new RegEntry("$210E", ResourceHelper.GetMessage("RegView_Snes_Mode7VOffset"), ppu.Mode7.VScroll, Format.X16),

			new RegEntry("$210F", ResourceHelper.GetMessage("RegView_Snes_BG2HOffset"), ppu.Layers[1].HScroll, Format.X16),
			new RegEntry("$2110", ResourceHelper.GetMessage("RegView_Snes_BG2VOffset"), ppu.Layers[1].VScroll, Format.X16),
			new RegEntry("$2111", ResourceHelper.GetMessage("RegView_Snes_BG3HOffset"), ppu.Layers[2].HScroll, Format.X16),
			new RegEntry("$2112", ResourceHelper.GetMessage("RegView_Snes_BG3VOffset"), ppu.Layers[2].VScroll, Format.X16),
			new RegEntry("$2113", ResourceHelper.GetMessage("RegView_Snes_BG4HOffset"), ppu.Layers[3].HScroll, Format.X16),
			new RegEntry("$2114", ResourceHelper.GetMessage("RegView_Snes_BG4VOffset"), ppu.Layers[3].VScroll, Format.X16),

			new RegEntry("$2115 - $2117", ResourceHelper.GetMessage("RegView_Snes_VRAM")),
			new RegEntry("$2115.0-1", ResourceHelper.GetMessage("RegView_Snes_IncrementValue"), ppu.VramIncrementValue),
			new RegEntry("$2115.2-3", ResourceHelper.GetMessage("RegView_Snes_AddressMapping"), ppu.VramAddressRemapping),
			new RegEntry("$2115.7", ResourceHelper.GetMessage("RegView_Snes_IncrementOn2119"), ppu.VramAddrIncrementOnSecondReg),
			new RegEntry("$2116/7", ResourceHelper.GetMessage("RegView_Snes_VRAMAddress"), ppu.VramAddress, Format.X16),

			new RegEntry("$211A - $2120", ResourceHelper.GetMessage("RegView_Snes_Mode7")),
			new RegEntry("$211A.0", ResourceHelper.GetMessage("RegView_Snes_Mode7_HorMirroring"), ppu.Mode7.HorizontalMirroring),
			new RegEntry("$211A.1", ResourceHelper.GetMessage("RegView_Snes_Mode7_VertMirroring"), ppu.Mode7.VerticalMirroring),
			new RegEntry("$211A.6", ResourceHelper.GetMessage("RegView_Snes_Mode7_FillWTile0"), ppu.Mode7.FillWithTile0),
			new RegEntry("$211A.7", ResourceHelper.GetMessage("RegView_Snes_Mode7_LargeTilemap"), ppu.Mode7.LargeMap),

			new RegEntry("$211B", ResourceHelper.GetMessage("RegView_Snes_Mode7_MatrixA"), ppu.Mode7.Matrix[0], Format.X16),
			new RegEntry("$211C", ResourceHelper.GetMessage("RegView_Snes_Mode7_MatrixB"), ppu.Mode7.Matrix[1], Format.X16),
			new RegEntry("$211D", ResourceHelper.GetMessage("RegView_Snes_Mode7_MatrixC"), ppu.Mode7.Matrix[2], Format.X16),
			new RegEntry("$211E", ResourceHelper.GetMessage("RegView_Snes_Mode7_MatrixD"), ppu.Mode7.Matrix[3], Format.X16),

			new RegEntry("$211F", ResourceHelper.GetMessage("RegView_Snes_Mode7_CenterX"), ppu.Mode7.CenterX, Format.X16),
			new RegEntry("$2120", ResourceHelper.GetMessage("RegView_Snes_Mode7_CenterY"), ppu.Mode7.CenterY, Format.X16),

			new RegEntry("$2121", ResourceHelper.GetMessage("RegView_Snes_CGRAM")),
			new RegEntry("$2121", ResourceHelper.GetMessage("RegView_Snes_CGRAMAddress"), ppu.CgramAddress, Format.X16),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Snes_CGRAMNextWriteToMSB"), ppu.CgramAddressLatch),

			new RegEntry("$2123 - $212B", ResourceHelper.GetMessage("RegView_Snes_Windows")),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Snes_BG1Windows")),
			new RegEntry("$2123.0", ResourceHelper.GetMessage("RegView_Snes_BG1Window1Inverted"), ppu.Window[0].InvertedLayers[0] != 0),
			new RegEntry("$2123.1", ResourceHelper.GetMessage("RegView_Snes_BG1Window1Active"), ppu.Window[0].ActiveLayers[0] != 0),
			new RegEntry("$2123.2", ResourceHelper.GetMessage("RegView_Snes_BG1Window2Inverted"), ppu.Window[1].InvertedLayers[0] != 0),
			new RegEntry("$2123.3", ResourceHelper.GetMessage("RegView_Snes_BG1Window2Active"), ppu.Window[1].ActiveLayers[0] != 0),

			new RegEntry("", ResourceHelper.GetMessage("RegView_Snes_BG2Windows")),
			new RegEntry("$2123.4", ResourceHelper.GetMessage("RegView_Snes_BG2Window1Inverted"), ppu.Window[0].InvertedLayers[1] != 0),
			new RegEntry("$2123.5", ResourceHelper.GetMessage("RegView_Snes_BG2Window1Active"), ppu.Window[0].ActiveLayers[1] != 0),
			new RegEntry("$2123.6", ResourceHelper.GetMessage("RegView_Snes_BG2Window2Inverted"), ppu.Window[1].InvertedLayers[1] != 0),
			new RegEntry("$2123.7", ResourceHelper.GetMessage("RegView_Snes_BG2Window2Active"), ppu.Window[1].ActiveLayers[1] != 0),

			new RegEntry("", ResourceHelper.GetMessage("RegView_Snes_BG3Windows")),
			new RegEntry("$2124.0", ResourceHelper.GetMessage("RegView_Snes_BG3Window1Inverted"), ppu.Window[0].InvertedLayers[2] != 0),
			new RegEntry("$2124.1", ResourceHelper.GetMessage("RegView_Snes_BG3Window1Active"), ppu.Window[0].ActiveLayers[2] != 0),
			new RegEntry("$2124.2", ResourceHelper.GetMessage("RegView_Snes_BG3Window2Inverted"), ppu.Window[1].InvertedLayers[2] != 0),
			new RegEntry("$2124.3", ResourceHelper.GetMessage("RegView_Snes_BG3Window2Active"), ppu.Window[1].ActiveLayers[2] != 0),

			new RegEntry("", ResourceHelper.GetMessage("RegView_Snes_BG4Windows")),
			new RegEntry("$2124.4", ResourceHelper.GetMessage("RegView_Snes_BG4Window1Inverted"), ppu.Window[0].InvertedLayers[3] != 0),
			new RegEntry("$2124.5", ResourceHelper.GetMessage("RegView_Snes_BG4Window1Active"), ppu.Window[0].ActiveLayers[3] != 0),
			new RegEntry("$2124.6", ResourceHelper.GetMessage("RegView_Snes_BG4Window2Inverted"), ppu.Window[1].InvertedLayers[3] != 0),
			new RegEntry("$2124.7", ResourceHelper.GetMessage("RegView_Snes_BG4Window2Active"), ppu.Window[1].ActiveLayers[3] != 0),

			new RegEntry("", ResourceHelper.GetMessage("RegView_Snes_OAMWindows")),
			new RegEntry("$2125.0", ResourceHelper.GetMessage("RegView_Snes_OAMWindow1Inverted"), ppu.Window[0].InvertedLayers[4] != 0),
			new RegEntry("$2125.1", ResourceHelper.GetMessage("RegView_Snes_OAMWindow1Active"), ppu.Window[0].ActiveLayers[4] != 0),
			new RegEntry("$2125.2", ResourceHelper.GetMessage("RegView_Snes_OAMWindow2Inverted"), ppu.Window[1].InvertedLayers[4] != 0),
			new RegEntry("$2125.3", ResourceHelper.GetMessage("RegView_Snes_OAMWindow2Active"), ppu.Window[1].ActiveLayers[4] != 0),

			new RegEntry("", ResourceHelper.GetMessage("RegView_Snes_ColorWindows")),
			new RegEntry("$2125.4", ResourceHelper.GetMessage("RegView_Snes_ColorWindow1Inverted"), ppu.Window[0].InvertedLayers[5] != 0),
			new RegEntry("$2125.5", ResourceHelper.GetMessage("RegView_Snes_ColorWindow1Active"), ppu.Window[0].ActiveLayers[5] != 0),
			new RegEntry("$2125.6", ResourceHelper.GetMessage("RegView_Snes_ColorWindow2Inverted"), ppu.Window[1].InvertedLayers[5] != 0),
			new RegEntry("$2125.7", ResourceHelper.GetMessage("RegView_Snes_ColorWindow2Active"), ppu.Window[1].ActiveLayers[5] != 0),

			new RegEntry("", ResourceHelper.GetMessage("RegView_Snes_WindowPosition")),
			new RegEntry("$2126", ResourceHelper.GetMessage("RegView_Snes_Window1Left"), ppu.Window[0].Left),
			new RegEntry("$2127", ResourceHelper.GetMessage("RegView_Snes_Window1Right"), ppu.Window[0].Right),
			new RegEntry("$2128", ResourceHelper.GetMessage("RegView_Snes_Window2Left"), ppu.Window[1].Left),
			new RegEntry("$2129", ResourceHelper.GetMessage("RegView_Snes_Window2Right"), ppu.Window[1].Right),

			new RegEntry("", ResourceHelper.GetMessage("RegView_Snes_WindowMasks")),
			new RegEntry("$212A.0-1", ResourceHelper.GetMessage("RegView_Snes_BG1WindowMask"), ppu.MaskLogic[0]),
			new RegEntry("$212A.2-3", ResourceHelper.GetMessage("RegView_Snes_BG2WindowMask"), ppu.MaskLogic[1]),
			new RegEntry("$212A.4-5", ResourceHelper.GetMessage("RegView_Snes_BG3WindowMask"), ppu.MaskLogic[2]),
			new RegEntry("$212A.6-7", ResourceHelper.GetMessage("RegView_Snes_BG4WindowMask"), ppu.MaskLogic[3]),
			new RegEntry("$212B.6-7", ResourceHelper.GetMessage("RegView_Snes_OAMWindowMask"), ppu.MaskLogic[4]),
			new RegEntry("$212B.6-7", ResourceHelper.GetMessage("RegView_Snes_ColorWindowMask"), ppu.MaskLogic[5]),

			new RegEntry("$212C", ResourceHelper.GetMessage("RegView_Snes_MainScreenLayers")),
			new RegEntry("$212C.0", ResourceHelper.GetMessage("RegView_Snes_BG1Enabled"), (ppu.MainScreenLayers & 0x01) != 0),
			new RegEntry("$212C.1", ResourceHelper.GetMessage("RegView_Snes_BG2Enabled"), (ppu.MainScreenLayers & 0x02) != 0),
			new RegEntry("$212C.2", ResourceHelper.GetMessage("RegView_Snes_BG3Enabled"), (ppu.MainScreenLayers & 0x04) != 0),
			new RegEntry("$212C.3", ResourceHelper.GetMessage("RegView_Snes_BG4Enabled"), (ppu.MainScreenLayers & 0x08) != 0),
			new RegEntry("$212C.4", ResourceHelper.GetMessage("RegView_Snes_OAMEnabled"), (ppu.MainScreenLayers & 0x10) != 0),

			new RegEntry("$212D", ResourceHelper.GetMessage("RegView_Snes_SubScreenLayers")),
			new RegEntry("$212D.0", ResourceHelper.GetMessage("RegView_Snes_BG1Enabled"), (ppu.SubScreenLayers & 0x01) != 0),
			new RegEntry("$212D.1", ResourceHelper.GetMessage("RegView_Snes_BG2Enabled"), (ppu.SubScreenLayers & 0x02) != 0),
			new RegEntry("$212D.2", ResourceHelper.GetMessage("RegView_Snes_BG3Enabled"), (ppu.SubScreenLayers & 0x04) != 0),
			new RegEntry("$212D.3", ResourceHelper.GetMessage("RegView_Snes_BG4Enabled"), (ppu.SubScreenLayers & 0x08) != 0),
			new RegEntry("$212D.4", ResourceHelper.GetMessage("RegView_Snes_OAMEnabled"), (ppu.SubScreenLayers & 0x10) != 0),

			new RegEntry("$212E", ResourceHelper.GetMessage("RegView_Snes_MainScreenWindows")),
			new RegEntry("$212E.0", ResourceHelper.GetMessage("RegView_Snes_BG1MainscreenWindowEnabled"), ppu.WindowMaskMain[0] != 0),
			new RegEntry("$212E.1", ResourceHelper.GetMessage("RegView_Snes_BG2MainscreenWindowEnabled"), ppu.WindowMaskMain[1] != 0),
			new RegEntry("$212E.2", ResourceHelper.GetMessage("RegView_Snes_BG3MainscreenWindowEnabled"), ppu.WindowMaskMain[2] != 0),
			new RegEntry("$212E.3", ResourceHelper.GetMessage("RegView_Snes_BG4MainscreenWindowEnabled"), ppu.WindowMaskMain[3] != 0),
			new RegEntry("$212E.4", ResourceHelper.GetMessage("RegView_Snes_OAMMainscreenWindowEnabled"), ppu.WindowMaskMain[4] != 0),

			new RegEntry("$212F", ResourceHelper.GetMessage("RegView_Snes_SubScreenWindows")),
			new RegEntry("$212F.0", ResourceHelper.GetMessage("RegView_Snes_BG1SubscreenWindowEnabled"), ppu.WindowMaskSub[0] != 0),
			new RegEntry("$212F.1", ResourceHelper.GetMessage("RegView_Snes_BG2SubscreenWindowEnabled"), ppu.WindowMaskSub[1] != 0),
			new RegEntry("$212F.2", ResourceHelper.GetMessage("RegView_Snes_BG3SubscreenWindowEnabled"), ppu.WindowMaskSub[2] != 0),
			new RegEntry("$212F.3", ResourceHelper.GetMessage("RegView_Snes_BG4SubscreenWindowEnabled"), ppu.WindowMaskSub[3] != 0),
			new RegEntry("$212F.4", ResourceHelper.GetMessage("RegView_Snes_OAMSubscreenWindowEnabled"), ppu.WindowMaskSub[4] != 0),

			new RegEntry("$2130 - $2131", ResourceHelper.GetMessage("RegView_Snes_ColorMath")),
			new RegEntry("$2130.0", ResourceHelper.GetMessage("RegView_Snes_DirectColorMode"), ppu.DirectColorMode),
			new RegEntry("$2130.1", ResourceHelper.GetMessage("RegView_Snes_CM_AddSubscreen"), ppu.ColorMathAddSubscreen),
			new RegEntry("$2130.4-5", ResourceHelper.GetMessage("RegView_Snes_CM_PreventMode"), ppu.ColorMathPreventMode),
			new RegEntry("$2130.6-7", ResourceHelper.GetMessage("RegView_Snes_CM_ClipMode"), ppu.ColorMathClipMode),

			new RegEntry("$2131.0", ResourceHelper.GetMessage("RegView_Snes_CM_BG1Enabled"), (ppu.ColorMathEnabled & 0x01) != 0),
			new RegEntry("$2131.1", ResourceHelper.GetMessage("RegView_Snes_CM_BG2Enabled"), (ppu.ColorMathEnabled & 0x02) != 0),
			new RegEntry("$2131.2", ResourceHelper.GetMessage("RegView_Snes_CM_BG3Enabled"), (ppu.ColorMathEnabled & 0x04) != 0),
			new RegEntry("$2131.3", ResourceHelper.GetMessage("RegView_Snes_CM_BG4Enabled"), (ppu.ColorMathEnabled & 0x08) != 0),
			new RegEntry("$2131.4", ResourceHelper.GetMessage("RegView_Snes_CM_OAMEnabled"), (ppu.ColorMathEnabled & 0x10) != 0),
			new RegEntry("$2131.5", ResourceHelper.GetMessage("RegView_Snes_CM_BackgroundEnabled"), (ppu.ColorMathEnabled & 0x20) != 0),
			new RegEntry("$2131.6", ResourceHelper.GetMessage("RegView_Snes_CM_HalfMode"), ppu.ColorMathHalveResult),
			new RegEntry("$2131.7", ResourceHelper.GetMessage("RegView_Snes_CM_SubtractMode"), ppu.ColorMathSubtractMode),

			new RegEntry("$2132 - $2133", ResourceHelper.GetMessage("RegView_Snes_Misc")),
			new RegEntry("$2132", ResourceHelper.GetMessage("RegView_Snes_FixedColor_BGR"), ppu.FixedColor, Format.X16),

			new RegEntry("$2133.0", ResourceHelper.GetMessage("RegView_Snes_ScreenInterlace"), ppu.ScreenInterlace),
			new RegEntry("$2133.1", ResourceHelper.GetMessage("RegView_Snes_OAMInterlace"), ppu.ObjInterlace),
			new RegEntry("$2133.2", ResourceHelper.GetMessage("RegView_Snes_OverscanMode"), ppu.OverscanMode),
			new RegEntry("$2133.3", ResourceHelper.GetMessage("RegView_Snes_HighResolutionMode"), ppu.HiResMode),
			new RegEntry("$2133.4", ResourceHelper.GetMessage("RegView_Snes_Ext_BGEnabled"), ppu.ExtBgEnabled),
		};

		return new RegisterViewerTab(ResourceHelper.GetMessage("RegView_Snes_PPU"), entries, CpuType.Snes, MemoryType.SnesRegister);
	}

	private static RegisterViewerTab GetSnesDspTab(ref SnesState state)
	{
		DspState dsp = state.Dsp;
		List<RegEntry> entries = new List<RegEntry>();

		void AddReg(int i, string name, bool signed = false)
		{
			entries.Add(new RegEntry("$" + i.ToString("X2"), name, signed ? (sbyte)dsp.Regs[i] : dsp.Regs[i], Format.X8));
		}

		AddReg(0x0C, ResourceHelper.GetMessage("RegView_Snes_MainVolumeMVOL_Left"), true);
		AddReg(0x1C, ResourceHelper.GetMessage("RegView_Snes_MainVolumeMVOL_Right"), true);
		AddReg(0x2C, ResourceHelper.GetMessage("RegView_Snes_EchoVolumeEVOL_Left"), true);
		AddReg(0x3C, ResourceHelper.GetMessage("RegView_Snes_EchoVolumeEVOL_Right"), true);

		AddReg(0x4C, ResourceHelper.GetMessage("RegView_Snes_KeyOnKON"));
		AddReg(0x5C, ResourceHelper.GetMessage("RegView_Snes_KeyOffKOF"));

		AddReg(0x7C, ResourceHelper.GetMessage("RegView_Snes_SourceEndBlockENDX"));
		AddReg(0x0D, ResourceHelper.GetMessage("RegView_Snes_EchoFeedbackEFB"));
		AddReg(0x2D, ResourceHelper.GetMessage("RegView_Snes_PitchModulationPMON"));
		AddReg(0x3D, ResourceHelper.GetMessage("RegView_Snes_NoiseEnableNON"));
		AddReg(0x4D, ResourceHelper.GetMessage("RegView_Snes_EchoEnableEON"));
		AddReg(0x5D, ResourceHelper.GetMessage("RegView_Snes_SourceDirectoryOffsetDIR"));
		AddReg(0x6D, ResourceHelper.GetMessage("RegView_Snes_EchoBufferOffsetESA"));
		AddReg(0x7D, ResourceHelper.GetMessage("RegView_Snes_EchoDelayEDL"));

		entries.Add(new RegEntry("$6C", ResourceHelper.GetMessage("RegView_Snes_FlagsFLG")));
		entries.Add(new RegEntry("$6C.0-4", ResourceHelper.GetMessage("RegView_Snes_NoiseClock"), dsp.Regs[0x6C] & 0x1F, Format.X8));
		entries.Add(new RegEntry("$6C.5", ResourceHelper.GetMessage("RegView_Snes_EchoDisabled"), (dsp.Regs[0x6C] & 0x20) != 0));
		entries.Add(new RegEntry("$6C.6", ResourceHelper.GetMessage("RegView_Snes_Mute"), (dsp.Regs[0x6C] & 0x40) != 0));
		entries.Add(new RegEntry("$6C.7", ResourceHelper.GetMessage("RegView_Snes_Reset"), (dsp.Regs[0x6C] & 0x80) != 0));

		entries.Add(new RegEntry("$xF", ResourceHelper.GetMessage("RegView_Snes_Coefficients")));
		for(int i = 0; i < 8; i++) {
			AddReg((i << 4) | 0x0F, ResourceHelper.GetMessage("RegView_Snes_Coefficient") + " " + i);
		}

		for(int i = 0; i < 8; i++) {
			entries.Add(new RegEntry(ResourceHelper.GetMessage("RegView_Snes_Voice") + " #" + i.ToString(), ""));

			int voice = i << 4;
			AddReg(voice | 0x00, ResourceHelper.GetMessage("RegView_Snes_LeftVolumeVOL"), true);
			AddReg(voice | 0x01, ResourceHelper.GetMessage("RegView_Snes_RightVolumeVOL"), true);
			entries.Add(new RegEntry("$" + i + "2 + $" + i + "3", ResourceHelper.GetMessage("RegView_Snes_PitchP"), dsp.Regs[voice | 0x02] | (dsp.Regs[voice | 0x03] << 8), Format.X16));
			AddReg(voice | 0x04, ResourceHelper.GetMessage("RegView_Snes_SourceSRCN"));
			AddReg(voice | 0x05, ResourceHelper.GetMessage("RegView_Snes_ADSR1"));
			AddReg(voice | 0x06, ResourceHelper.GetMessage("RegView_Snes_ADSR2"));
			AddReg(voice | 0x07, ResourceHelper.GetMessage("RegView_Snes_GAIN"));
			AddReg(voice | 0x08, ResourceHelper.GetMessage("RegView_Snes_ENVX"));
			AddReg(voice | 0x09, ResourceHelper.GetMessage("RegView_Snes_OUTX"));
		}

		return new RegisterViewerTab(ResourceHelper.GetMessage("RegView_Snes_DSP"), entries);
	}

	private static RegisterViewerTab GetSnesSpcTab(ref SnesState state)
	{
		string GetTimerFrequency(double baseFreq, int divider)
		{
			return (divider == 0 ? (baseFreq / 256) : (baseFreq / divider)).ToString(".00") + ResourceHelper.GetMessage("RegView_Common_HzSuffix");
		}

		SpcState spc = state.Spc;
		List<RegEntry> entries = new List<RegEntry>() {
			new RegEntry("$F0", ResourceHelper.GetMessage("RegView_Snes_Test")),
			new RegEntry("$F0.0", ResourceHelper.GetMessage("RegView_Snes_TimersDirEnabled"), spc.TimersDisabled),
			new RegEntry("$F0.1", ResourceHelper.GetMessage("RegView_Snes_RAMWriteEnabled"), spc.WriteEnabled),
			new RegEntry("$F0.3", ResourceHelper.GetMessage("RegView_Snes_TimersEnabled"), spc.TimersEnabled),
			new RegEntry("$F0.4-5", ResourceHelper.GetMessage("RegView_Snes_ExternalSpeed"), spc.ExternalSpeed),
			new RegEntry("$F0.6-7", ResourceHelper.GetMessage("RegView_Snes_InternalSpeed"), spc.InternalSpeed),

			new RegEntry("$F1", ResourceHelper.GetMessage("RegView_Common_Control")),
			new RegEntry("$F1.0", ResourceHelper.GetMessage("RegView_Snes_Timer0Enabled"), spc.Timer0.Enabled),
			new RegEntry("$F1.1", ResourceHelper.GetMessage("RegView_Snes_Timer1Enabled"), spc.Timer1.Enabled),
			new RegEntry("$F1.2", ResourceHelper.GetMessage("RegView_Snes_Timer2Enabled"), spc.Timer2.Enabled),
			new RegEntry("$F1.7", ResourceHelper.GetMessage("RegView_Snes_IPLROMEnabled"), spc.RomEnabled),

			new RegEntry("$F2", ResourceHelper.GetMessage("RegView_Snes_DSP")),
			new RegEntry("$F2", ResourceHelper.GetMessage("RegView_Snes_DSPRegister"), spc.DspReg, Format.X8),

			new RegEntry("$F4 - $F7", ResourceHelper.GetMessage("RegView_Snes_CPUSPC_Ports")),
			new RegEntry("$F4", ResourceHelper.GetMessage("RegView_Snes_Port0CPURead"), spc.OutputReg[0], Format.X8),
			new RegEntry("$F4", ResourceHelper.GetMessage("RegView_Snes_Port0SPCRead"), spc.CpuRegs[0], Format.X8),
			new RegEntry("$F5", ResourceHelper.GetMessage("RegView_Snes_Port1CPURead"), spc.OutputReg[1], Format.X8),
			new RegEntry("$F5", ResourceHelper.GetMessage("RegView_Snes_Port1SPCRead"), spc.CpuRegs[1], Format.X8),
			new RegEntry("$F6", ResourceHelper.GetMessage("RegView_Snes_Port2CPURead"), spc.OutputReg[2], Format.X8),
			new RegEntry("$F6", ResourceHelper.GetMessage("RegView_Snes_Port2SPCRead"), spc.CpuRegs[2], Format.X8),
			new RegEntry("$F7", ResourceHelper.GetMessage("RegView_Snes_Port3CPURead"), spc.OutputReg[3], Format.X8),
			new RegEntry("$F7", ResourceHelper.GetMessage("RegView_Snes_Port3SPCRead"), spc.CpuRegs[3], Format.X8),

			new RegEntry("$F8 - $F9", ResourceHelper.GetMessage("RegView_Snes_RAMRegisters")),
			new RegEntry("$F8", ResourceHelper.GetMessage("RegView_Snes_RAMReg0"), spc.RamReg[0], Format.X8),
			new RegEntry("$F9", ResourceHelper.GetMessage("RegView_Snes_RAMReg1"), spc.RamReg[1], Format.X8),

			new RegEntry("", ResourceHelper.GetMessage("RegView_Snes_Timer0")),
			new RegEntry("$F1.0", ResourceHelper.GetMessage("RegView_Common_Enabled"), spc.Timer0.Enabled),
			new RegEntry("$FA", ResourceHelper.GetMessage("RegView_Common_Divider"), spc.Timer0.Target, Format.X8),
			new RegEntry("$FD", ResourceHelper.GetMessage("RegView_Common_Output"), spc.Timer0.Output, Format.X8),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Common_Timer"), spc.Timer0.Stage2, Format.X8),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Common_Frequency"), GetTimerFrequency(8000, spc.Timer0.Target), spc.Timer0.Target),

			new RegEntry("", ResourceHelper.GetMessage("RegView_Snes_Timer1")),
			new RegEntry("$F1.1", ResourceHelper.GetMessage("RegView_Common_Enabled"), spc.Timer1.Enabled),
			new RegEntry("$FB", ResourceHelper.GetMessage("RegView_Common_Divider"), spc.Timer1.Target, Format.X8),
			new RegEntry("$FE", ResourceHelper.GetMessage("RegView_Common_Output"), spc.Timer1.Output, Format.X8),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Common_Timer"), spc.Timer1.Stage2, Format.X8),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Common_Frequency"), GetTimerFrequency(8000, spc.Timer1.Target), spc.Timer1.Target),

			new RegEntry("", ResourceHelper.GetMessage("RegView_Snes_Timer2")),
			new RegEntry("$F1.2", ResourceHelper.GetMessage("RegView_Common_Enabled"), spc.Timer2.Enabled),
			new RegEntry("$FC", ResourceHelper.GetMessage("RegView_Common_Divider"), spc.Timer2.Target, Format.X8),
			new RegEntry("$FF", ResourceHelper.GetMessage("RegView_Common_Output"), spc.Timer2.Output, Format.X8),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Common_Timer"), spc.Timer2.Stage2, Format.X8),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Common_Frequency"), GetTimerFrequency(64000, spc.Timer2.Target), spc.Timer2.Target),
		};

		return new RegisterViewerTab(ResourceHelper.GetMessage("RegView_Snes_SPC"), entries, CpuType.Spc, MemoryType.SpcMemory);
	}

	private static RegisterViewerTab GetSnesDmaTab(ref SnesState state)
	{
		List<RegEntry> entries = new List<RegEntry>();

		for(int i = 0; i < 8; i++) {
			DmaChannelConfig ch = state.Dma.Channels[i];
			entries.Add(new RegEntry(ResourceHelper.GetMessage("RegView_Snes_DMAChannel") + " " + i.ToString(), ""));
			entries.Add(new RegEntry("$420B." + i.ToString(), ResourceHelper.GetMessage("RegView_Common_ChannelEnabled"), ch.DmaActive));
			entries.Add(new RegEntry("$420C." + i.ToString(), ResourceHelper.GetMessage("RegView_Snes_HDMAEnabled"), (state.Dma.HdmaChannels & (1 << i)) != 0));

			entries.Add(new RegEntry("$43" + i.ToString() + "0.0-2", ResourceHelper.GetMessage("RegView_Snes_TransferMode"), ch.TransferMode));
			entries.Add(new RegEntry("$43" + i.ToString() + "0.3", ResourceHelper.GetMessage("RegView_Snes_Fixed"), ch.FixedTransfer));
			entries.Add(new RegEntry("$43" + i.ToString() + "0.4", ResourceHelper.GetMessage("RegView_Snes_Decrement"), ch.Decrement));
			entries.Add(new RegEntry("$43" + i.ToString() + "0.6", ResourceHelper.GetMessage("RegView_Snes_IndirectHDMA"), ch.HdmaIndirectAddressing));
			entries.Add(new RegEntry("$43" + i.ToString() + "0.7", ResourceHelper.GetMessage("RegView_Snes_Direction"), ch.InvertDirection ? ResourceHelper.GetMessage("RegView_Snes_BBackslashA") : ResourceHelper.GetMessage("RegView_Snes_ABackslashB"), ch.InvertDirection));

			entries.Add(new RegEntry("$43" + i.ToString() + "1", ResourceHelper.GetMessage("RegView_Snes_BBusAddress"), ch.DestAddress, Format.X8));
			entries.Add(new RegEntry("$43" + i.ToString() + "2/3", ResourceHelper.GetMessage("RegView_Snes_ABusAddress"), ch.SrcAddress, Format.X16));
			entries.Add(new RegEntry("$43" + i.ToString() + "4", ResourceHelper.GetMessage("RegView_Snes_ABusBank"), ch.SrcBank, Format.X8));
			entries.Add(new RegEntry("$43" + i.ToString() + "5/6", ResourceHelper.GetMessage("RegView_Snes_Size"), ch.TransferSize, Format.X16));

			entries.Add(new RegEntry("$43" + i.ToString() + "7", ResourceHelper.GetMessage("RegView_Snes_HDMABank"), ch.HdmaBank, Format.X8));
			entries.Add(new RegEntry("$43" + i.ToString() + "8/9", ResourceHelper.GetMessage("RegView_Snes_HDMAAddress"), ch.HdmaTableAddress, Format.X16));
			entries.Add(new RegEntry("$43" + i.ToString() + "A", ResourceHelper.GetMessage("RegView_Snes_HDMALineCounter"), ch.HdmaLineCounterAndRepeat, Format.X8));
			entries.Add(new RegEntry("$43" + i.ToString() + "B", ResourceHelper.GetMessage("RegView_Snes_UnusedRegister"), ch.UnusedRegister, Format.X8));
		}

		return new RegisterViewerTab(ResourceHelper.GetMessage("RegView_Snes_DMA"), entries, CpuType.Snes, MemoryType.SnesRegister);
	}

	private static RegisterViewerTab GetSnesCpuTab(ref SnesState state, byte snesReg4210, byte snesReg4211, byte snesReg4212)
	{
		InternalRegisterState regs = state.InternalRegs;
		AluState alu = state.Alu;

		List<RegEntry> entries = new List<RegEntry>() {
			new RegEntry("$2181 - $2183", ResourceHelper.GetMessage("RegView_Snes_WorkRAMPosition"), state.WramPosition, Format.X24),

			new RegEntry("$4200 - $4201", ResourceHelper.GetMessage("RegView_Snes_IRQ_NMI_AutopollEnabled")),
			new RegEntry("$4200.0", ResourceHelper.GetMessage("RegView_Snes_AutoJoypadPoll"), regs.EnableAutoJoypadRead),
			new RegEntry("$4200.4", ResourceHelper.GetMessage("RegView_Snes_H_IRQEnabled"), regs.EnableHorizontalIrq),
			new RegEntry("$4200.5", ResourceHelper.GetMessage("RegView_Snes_V_IRQEnabled"), regs.EnableVerticalIrq),
			new RegEntry("$4200.7", ResourceHelper.GetMessage("RegView_Snes_NMIEnabled"), regs.EnableNmi),

			new RegEntry("$4201", ResourceHelper.GetMessage("RegView_Snes_IO_Port"), regs.IoPortOutput, Format.X8),

			new RegEntry("$4202 - $4206", ResourceHelper.GetMessage("RegView_Snes_MultDivRegistersInput")),
			new RegEntry("$4202", ResourceHelper.GetMessage("RegView_Snes_Multiplicand"), alu.MultOperand1, Format.X8),
			new RegEntry("$4203", ResourceHelper.GetMessage("RegView_Snes_Multiplier"), alu.MultOperand2, Format.X8),
			new RegEntry("$4204/5", ResourceHelper.GetMessage("RegView_Snes_Dividend"), alu.Dividend, Format.X16),
			new RegEntry("$4206", ResourceHelper.GetMessage("RegView_Snes_Divisor"), alu.Divisor, Format.X8),

			new RegEntry("$4207 - $420A", ResourceHelper.GetMessage("RegView_Snes_H_VIRQTimers")),
			new RegEntry("$4207/8", ResourceHelper.GetMessage("RegView_Snes_H_Timer"), regs.HorizontalTimer, Format.X16),
			new RegEntry("$4209/A", ResourceHelper.GetMessage("RegView_Snes_V_Timer"), regs.VerticalTimer, Format.X16),

			new RegEntry("$420D - $4212", ResourceHelper.GetMessage("RegView_Snes_MiscFlags")),

			new RegEntry("$420D.0", ResourceHelper.GetMessage("RegView_Snes_FastROMEnabled"), regs.EnableFastRom),
			new RegEntry("$4210.7", ResourceHelper.GetMessage("RegView_Snes_NMIFlag"), (snesReg4210 & 0x80) != 0),
			new RegEntry("$4211.7", ResourceHelper.GetMessage("RegView_Snes_IRQFlag"), (snesReg4211 & 0x80) != 0),

			new RegEntry("$4212.0", ResourceHelper.GetMessage("RegView_Snes_AutoJoypadReadActive"), (snesReg4212 & 0x01) != 0),
			new RegEntry("$4212.6", ResourceHelper.GetMessage("RegView_Snes_H_BlankFlag"), (snesReg4212 & 0x40) != 0),
			new RegEntry("$4212.7", ResourceHelper.GetMessage("RegView_Snes_V_BlankFlag"), (snesReg4212 & 0x80) != 0),

			new RegEntry("$4214 - $4217", ResourceHelper.GetMessage("RegView_Snes_MultDivRegistersResult")),
			new RegEntry("$4214/5", ResourceHelper.GetMessage("RegView_Snes_Quotient"), alu.DivResult, Format.X16),
			new RegEntry("$4216/7", ResourceHelper.GetMessage("RegView_Snes_ProductRemainder"), alu.MultOrRemainderResult, Format.X16),

			new RegEntry("$4218 - $421F", ResourceHelper.GetMessage("RegView_Snes_InputData")),
			new RegEntry("$4218/9", ResourceHelper.GetMessage("RegView_Snes_P1Data"), regs.ControllerData[0], Format.X16),
			new RegEntry("$421A/B", ResourceHelper.GetMessage("RegView_Snes_P2Data"), regs.ControllerData[1], Format.X16),
			new RegEntry("$421C/D", ResourceHelper.GetMessage("RegView_Snes_P3Data"), regs.ControllerData[2], Format.X16),
			new RegEntry("$421E/F", ResourceHelper.GetMessage("RegView_Snes_P4Data"), regs.ControllerData[3], Format.X16),
		};

		return new RegisterViewerTab(ResourceHelper.GetMessage("RegView_Snes_CPU"), entries, CpuType.Snes, MemoryType.SnesRegister);
	}
}
