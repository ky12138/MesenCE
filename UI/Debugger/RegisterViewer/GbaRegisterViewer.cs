using Mesen.Debugger.ViewModels;
using Mesen.Interop;
using Mesen.Localization;
using System.Collections.Generic;
using static Mesen.Debugger.ViewModels.RegEntry;

namespace Mesen.Debugger.RegisterViewer;

public class GbaRegisterViewer
{
	public static List<RegisterViewerTab> GetTabs(ref GbaState gbaState)
	{
		List<RegisterViewerTab> tabs = new List<RegisterViewerTab>() {
			GetGbaPpuTab(ref gbaState),
			GetGbaApuTab(ref gbaState),
			GetGbaDmaTab(ref gbaState),
			GetGbaTimerTab(ref gbaState),
			GetGbaMiscTab(ref gbaState),
		};
		return tabs;
	}

	private static RegisterViewerTab GetGbaMiscTab(ref GbaState gbaState)
	{
		List<RegEntry> entries = new List<RegEntry>();

		GbaMemoryManagerState memManager = gbaState.MemoryManager;
		GbaGpioState gpio = gbaState.Cart.Gpio;

		entries.AddRange(new List<RegEntry>() {
			/*new RegEntry("", "Prefetch"),
			new RegEntry("", "Read Address", gbaState.Prefetch.ReadAddr, Format.X32),
			new RegEntry("", "Prefetch Address", gbaState.Prefetch.PrefetchAddr, Format.X32),
			new RegEntry("", "Length", (gbaState.Prefetch.PrefetchAddr - gbaState.Prefetch.ReadAddr) / 2, Format.X8),
			new RegEntry("", "Clock Counter", gbaState.Prefetch.ClockCounter),
			new RegEntry("", "Filled", (gbaState.Prefetch.PrefetchAddr - gbaState.Prefetch.ReadAddr) >= 16),
			new RegEntry("", "Was Filled", gbaState.Prefetch.WasFilled),*/

			new RegEntry("", ResourceHelper.GetMessage("RegView_Gba_InputIRQControl")),
			new RegEntry("$4000132-3", ResourceHelper.GetMessage("RegView_Gba_RegisterValue"), gbaState.ControlManager.KeyControl, Format.X16),
			new RegEntry("$4000132.0", ResourceHelper.GetMessage("RegView_Gba_A"), (gbaState.ControlManager.KeyControl & 0x01) != 0),
			new RegEntry("$4000132.1", ResourceHelper.GetMessage("RegView_Gba_B"), (gbaState.ControlManager.KeyControl & 0x02) != 0),
			new RegEntry("$4000132.2", ResourceHelper.GetMessage("RegView_Gba_Select"), (gbaState.ControlManager.KeyControl & 0x04) != 0),
			new RegEntry("$4000132.3", ResourceHelper.GetMessage("RegView_Gba_Start"), (gbaState.ControlManager.KeyControl & 0x08) != 0),
			new RegEntry("$4000132.4", ResourceHelper.GetMessage("RegView_Gba_Right"), (gbaState.ControlManager.KeyControl & 0x10) != 0),
			new RegEntry("$4000132.5", ResourceHelper.GetMessage("RegView_Gba_Left"), (gbaState.ControlManager.KeyControl & 0x20) != 0),
			new RegEntry("$4000132.6", ResourceHelper.GetMessage("RegView_Gba_Up"), (gbaState.ControlManager.KeyControl & 0x40) != 0),
			new RegEntry("$4000132.7", ResourceHelper.GetMessage("RegView_Gba_Down"), (gbaState.ControlManager.KeyControl & 0x80) != 0),
			new RegEntry("$4000133.0", ResourceHelper.GetMessage("RegView_Gba_R"), (gbaState.ControlManager.KeyControl & 0x100) != 0),
			new RegEntry("$4000133.1", ResourceHelper.GetMessage("RegView_Gba_L"), (gbaState.ControlManager.KeyControl & 0x200) != 0),
			new RegEntry("$4000133.6", ResourceHelper.GetMessage("RegView_Common_IRQEnabled"), (gbaState.ControlManager.KeyControl & 0x4000) != 0),
			new RegEntry("$4000133.7", ResourceHelper.GetMessage("RegView_Gba_IRQCondition"), (gbaState.ControlManager.KeyControl & 0x8000) == 0 ? ResourceHelper.GetMessage("RegView_Gba_OR") : ResourceHelper.GetMessage("RegView_Gba_AND"), gbaState.ControlManager.KeyControl & 0x8000),

			new RegEntry("", ResourceHelper.GetMessage("RegView_Common_IRQ")),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Gba_IE_IRQEnabled")),
			new RegEntry("$4000200-1", ResourceHelper.GetMessage("RegView_Gba_IE_RegisterValue"), memManager.IE, Format.X16),
			new RegEntry("$4000200.0", ResourceHelper.GetMessage("RegView_Gba_VerticalBlankIRQEnabled"), ((memManager.IE >> 0) & 0x01) != 0),
			new RegEntry("$4000200.1", ResourceHelper.GetMessage("RegView_Gba_HorizontalBlankIRQEnabled"), ((memManager.IE >> 1) & 0x01) != 0),
			new RegEntry("$4000200.2", ResourceHelper.GetMessage("RegView_Gba_LYCMatchIRQEnabled"), ((memManager.IE >> 2) & 0x01) != 0),
			new RegEntry("$4000200.3", ResourceHelper.GetMessage("RegView_Gba_Timer0IRQEnabled"), ((memManager.IE >> 3) & 0x01) != 0),
			new RegEntry("$4000200.4", ResourceHelper.GetMessage("RegView_Gba_Timer1IRQEnabled"), ((memManager.IE >> 4) & 0x01) != 0),
			new RegEntry("$4000200.5", ResourceHelper.GetMessage("RegView_Gba_Timer2IRQEnabled"), ((memManager.IE >> 5) & 0x01) != 0),
			new RegEntry("$4000200.6", ResourceHelper.GetMessage("RegView_Gba_Timer3IRQEnabled"), ((memManager.IE >> 6) & 0x01) != 0),
			new RegEntry("$4000200.7", ResourceHelper.GetMessage("RegView_Gba_SerialIRQEnabled"), ((memManager.IE >> 7) & 0x01) != 0),
			new RegEntry("$4000201.0", ResourceHelper.GetMessage("RegView_Gba_DMAChannel0IRQEnabled"), ((memManager.IE >> 8) & 0x01) != 0),
			new RegEntry("$4000201.1", ResourceHelper.GetMessage("RegView_Gba_DMAChannel1IRQEnabled"), ((memManager.IE >> 9) & 0x01) != 0),
			new RegEntry("$4000201.2", ResourceHelper.GetMessage("RegView_Gba_DMAChannel2IRQEnabled"), ((memManager.IE >> 10) & 0x01) != 0),
			new RegEntry("$4000201.3", ResourceHelper.GetMessage("RegView_Gba_DMAChannel3IRQEnabled"), ((memManager.IE >> 11) & 0x01) != 0),
			new RegEntry("$4000201.4", ResourceHelper.GetMessage("RegView_Gba_InputIRQEnabled"), ((memManager.IE >> 12) & 0x01) != 0),
			new RegEntry("$4000201.5", ResourceHelper.GetMessage("RegView_Gba_CartridgeIRQEnabled"), ((memManager.IE >> 13) & 0x01) != 0),

			new RegEntry("", ResourceHelper.GetMessage("RegView_Gba_IE_IRQFlags")),
			new RegEntry("$4000202-3", ResourceHelper.GetMessage("RegView_Gba_IF_RegisterValue"), memManager.IF, Format.X16),
			new RegEntry("$4000202.0", ResourceHelper.GetMessage("RegView_Gba_VerticalBlankIRQActive"), ((memManager.IF >> 0) & 0x01) != 0),
			new RegEntry("$4000202.1", ResourceHelper.GetMessage("RegView_Gba_HorizontalBlankIRQActive"), ((memManager.IF >> 1) & 0x01) != 0),
			new RegEntry("$4000202.2", ResourceHelper.GetMessage("RegView_Gba_LYCMatchIRQActive"), ((memManager.IF >> 2) & 0x01) != 0),
			new RegEntry("$4000202.3", ResourceHelper.GetMessage("RegView_Gba_Timer0IRQActive"), ((memManager.IF >> 3) & 0x01) != 0),
			new RegEntry("$4000202.4", ResourceHelper.GetMessage("RegView_Gba_Timer1IRQActive"), ((memManager.IF >> 4) & 0x01) != 0),
			new RegEntry("$4000202.5", ResourceHelper.GetMessage("RegView_Gba_Timer2IRQActive"), ((memManager.IF >> 5) & 0x01) != 0),
			new RegEntry("$4000202.6", ResourceHelper.GetMessage("RegView_Gba_Timer3IRQActive"), ((memManager.IF >> 6) & 0x01) != 0),
			new RegEntry("$4000202.7", ResourceHelper.GetMessage("RegView_Gba_SerialIRQActive"), ((memManager.IF >> 7) & 0x01) != 0),
			new RegEntry("$4000203.0", ResourceHelper.GetMessage("RegView_Gba_DMAChannel0IRQActive"), ((memManager.IF >> 8) & 0x01) != 0),
			new RegEntry("$4000203.1", ResourceHelper.GetMessage("RegView_Gba_DMAChannel1IRQActive"), ((memManager.IF >> 9) & 0x01) != 0),
			new RegEntry("$4000203.2", ResourceHelper.GetMessage("RegView_Gba_DMAChannel2IRQActive"), ((memManager.IF >> 10) & 0x01) != 0),
			new RegEntry("$4000203.3", ResourceHelper.GetMessage("RegView_Gba_DMAChannel3IRQActive"), ((memManager.IF >> 11) & 0x01) != 0),
			new RegEntry("$4000203.4", ResourceHelper.GetMessage("RegView_Gba_InputIRQActive"), ((memManager.IF >> 12) & 0x01) != 0),
			new RegEntry("$4000203.5", ResourceHelper.GetMessage("RegView_Gba_CartridgeIRQActive"), ((memManager.IF >> 13) & 0x01) != 0),

			new RegEntry("$4000208.0", ResourceHelper.GetMessage("RegView_Gba_IME"), (memManager.IME & 0x01) != 0),

			new RegEntry("", ResourceHelper.GetMessage("RegView_Gba_Misc")),
			new RegEntry("$4000204-5", ResourceHelper.GetMessage("RegView_Gba_WaitControl"), memManager.WaitControl),
			new RegEntry("$4000204.0-1", ResourceHelper.GetMessage("RegView_Gba_SRAM_BankE"), memManager.SramWaitStates + " clocks", null),
			new RegEntry("$4000204.2-3", ResourceHelper.GetMessage("RegView_Gba_Bank_8_9"), memManager.PrgWaitStates0[0] + " clocks", null),
			new RegEntry("$4000204.4", ResourceHelper.GetMessage("RegView_Gba_Bank_8_9_Sequential"), memManager.PrgWaitStates0[1] + " clocks", null),
			new RegEntry("$4000204.5-6", ResourceHelper.GetMessage("RegView_Gba_Bank_AB"), memManager.PrgWaitStates1[0] + " clocks", null),
			new RegEntry("$4000204.7", ResourceHelper.GetMessage("RegView_Gba_Bank_AB_Sequential"), memManager.PrgWaitStates1[1] + " clocks", null),
			new RegEntry("$4000205.0-1", ResourceHelper.GetMessage("RegView_Gba_Bank_CD"), memManager.PrgWaitStates2[0] + " clocks", null),
			new RegEntry("$4000205.2", ResourceHelper.GetMessage("RegView_Gba_Bank_CD_Sequential"), memManager.PrgWaitStates2[1] + " clocks", null),
			new RegEntry("$4000205.6", ResourceHelper.GetMessage("RegView_Gba_PrefetchEnabled"), memManager.PrefetchEnabled),
		});

		if(gbaState.Cart.HasGpio) {
			entries.AddRange(new List<RegEntry>() {
				new RegEntry("", ResourceHelper.GetMessage("RegView_Gba_CartGPIO")),
				new RegEntry("$80000C4.0", ResourceHelper.GetMessage("RegView_Gba_SCK_RTC"), (gpio.Data & 0x01) != 0),
				new RegEntry("$80000C4.1", ResourceHelper.GetMessage("RegView_Gba_SIO_RTC"), (gpio.Data & 0x02) != 0),
				new RegEntry("$80000C4.2", ResourceHelper.GetMessage("RegView_Gba_CS_RTC"), (gpio.Data & 0x04) != 0),
				new RegEntry("$80000C4.3", ResourceHelper.GetMessage("RegView_Gba_Unused_RTC"), (gpio.Data & 0x04) != 0),
				new RegEntry("$80000C6.0", ResourceHelper.GetMessage("RegView_Gba_Pin0Direction"), (gpio.WritablePins & 0x01) != 0 ? ResourceHelper.GetMessage("RegView_Common_Out") : ResourceHelper.GetMessage("RegView_Common_In"), (gpio.WritablePins & 0x01) != 0),
				new RegEntry("$80000C6.1", ResourceHelper.GetMessage("RegView_Gba_Pin1Direction"), (gpio.WritablePins & 0x02) != 0 ? ResourceHelper.GetMessage("RegView_Common_Out") : ResourceHelper.GetMessage("RegView_Common_In"), (gpio.WritablePins & 0x02) != 0),
				new RegEntry("$80000C6.2", ResourceHelper.GetMessage("RegView_Gba_Pin2Direction"), (gpio.WritablePins & 0x04) != 0 ? ResourceHelper.GetMessage("RegView_Common_Out") : ResourceHelper.GetMessage("RegView_Common_In"), (gpio.WritablePins & 0x04) != 0),
				new RegEntry("$80000C6.3", ResourceHelper.GetMessage("RegView_Gba_Pin3Direction"), (gpio.WritablePins & 0x08) != 0 ? ResourceHelper.GetMessage("RegView_Common_Out") : ResourceHelper.GetMessage("RegView_Common_In"), (gpio.WritablePins & 0x08) != 0),
				new RegEntry("$80000C8.0", ResourceHelper.GetMessage("RegView_Gba_AllowGPIORead"), gpio.ReadWrite),
			});
		}

		return new RegisterViewerTab(ResourceHelper.GetMessage("RegView_Gba_Misc"), entries, CpuType.Gba, MemoryType.GbaMemory);
	}

	private static RegisterViewerTab GetGbaPpuTab(ref GbaState gbaState)
	{
		List<RegEntry> entries = new List<RegEntry>();

		GbaPpuState ppu = gbaState.Ppu;
		entries.AddRange(new List<RegEntry>() {
			new RegEntry($"$4000000-1", ResourceHelper.GetMessage("RegView_Common_Control"), ppu.Control | (ppu.Control2 << 8), Format.X16),
			new RegEntry($"$4000000.0-2", ResourceHelper.GetMessage("RegView_Gba_BGMode"), ppu.BgMode),
			new RegEntry($"$4000000.4", ResourceHelper.GetMessage("RegView_Gba_Show2ndFrame"), ppu.DisplayFrameSelect),
			new RegEntry($"$4000000.5", ResourceHelper.GetMessage("RegView_Gba_HBlankOAMAccess"), ppu.AllowHblankOamAccess),
			new RegEntry($"$4000000.6", ResourceHelper.GetMessage("RegView_Gba_SequentialOBJMapping"), ppu.ObjVramMappingOneDimension),
			new RegEntry($"$4000000.7", ResourceHelper.GetMessage("RegView_Gba_ForcedBlank"), ppu.ForcedBlank),

			new RegEntry($"$4000001.0", ResourceHelper.GetMessage("RegView_Gba_BG0Enabled"), ppu.BgLayers[0].Enabled),
			new RegEntry($"$4000001.1", ResourceHelper.GetMessage("RegView_Gba_BG1Enabled"), ppu.BgLayers[1].Enabled),
			new RegEntry($"$4000001.2", ResourceHelper.GetMessage("RegView_Gba_BG2Enabled"), ppu.BgLayers[2].Enabled),
			new RegEntry($"$4000001.3", ResourceHelper.GetMessage("RegView_Gba_BG3Enabled"), ppu.BgLayers[3].Enabled),
			new RegEntry($"$4000001.4", ResourceHelper.GetMessage("RegView_Gba_OBJEnabled"), ppu.ObjLayerEnabled),
			new RegEntry($"$4000001.5", ResourceHelper.GetMessage("RegView_Gba_Window0Enabled"), ppu.Window0Enabled),
			new RegEntry($"$4000001.6", ResourceHelper.GetMessage("RegView_Gba_Window1Enabled"), ppu.Window1Enabled),
			new RegEntry($"$4000001.7", ResourceHelper.GetMessage("RegView_Gba_OBJWindowEnabled"), ppu.ObjWindowEnabled),

			new RegEntry($"$4000002.0", ResourceHelper.GetMessage("RegView_Gba_StereoscopicGreenswap"), ppu.StereoscopicEnabled),

			new RegEntry($"$4000004", ResourceHelper.GetMessage("RegView_Common_Status"), ppu.DispStat, Format.X8),

			//TODOGBA fix this to always match real value
			new RegEntry($"$4000004.0", ResourceHelper.GetMessage("RegView_Gba_VerticalBlank"), ppu.Scanline >= 160 && ppu.Scanline != 227),
			new RegEntry($"$4000004.1", ResourceHelper.GetMessage("RegView_Gba_HorizontalBlank"), ppu.Cycle > 1007),
			new RegEntry($"$4000004.2", ResourceHelper.GetMessage("RegView_Gba_LYCMatch"), ppu.Scanline == ppu.Lyc),

			new RegEntry($"$4000004.3", ResourceHelper.GetMessage("RegView_Gba_VBlankIRQEnabled"), ppu.VblankIrqEnabled),
			new RegEntry($"$4000004.4", ResourceHelper.GetMessage("RegView_Gba_HBlankIRQEnabled"), ppu.HblankIrqEnabled),
			new RegEntry($"$4000004.5", ResourceHelper.GetMessage("RegView_Gba_LYCIRQEnabled"), ppu.ScanlineIrqEnabled),
			new RegEntry($"$4000005", ResourceHelper.GetMessage("RegView_Gba_LYC"), ppu.Lyc, Format.X8),
			new RegEntry($"$4000006", ResourceHelper.GetMessage("RegView_Gba_Scanline"), ppu.Scanline, Format.X8),
			new RegEntry($"", ResourceHelper.GetMessage("RegView_Gba_Cycle"), ppu.Cycle),
			new RegEntry($"", ResourceHelper.GetMessage("RegView_Common_FrameNumber"), ppu.FrameCount),
		});

		for(int i = 0; i < 4; i++) {
			GbaBgConfig layer = ppu.BgLayers[i];
			int baseAddr = 0x4000008 + i * 2;
			entries.AddRange(new List<RegEntry>() {
				new RegEntry("", ResourceHelper.GetMessage("RegView_Gba_BGLayer" + i)),
				new RegEntry($"$4000001." + i, ResourceHelper.GetMessage("RegView_Gba_BG" + i + "Enabled"), ppu.BgLayers[i].Enabled),

				new RegEntry($"${baseAddr:X}.0-1", ResourceHelper.GetMessage("RegView_Gba_Priority"), layer.Priority),
				new RegEntry($"${baseAddr:X}.2-3", ResourceHelper.GetMessage("RegView_Gba_TilesetAddress"), layer.TilesetAddr),
				new RegEntry($"${baseAddr:X}.4-5", ResourceHelper.GetMessage("RegView_Gba_StereoscopicMode"), layer.StereoMode),
				new RegEntry($"${baseAddr:X}.6", ResourceHelper.GetMessage("RegView_Gba_MosaicEnabled"), layer.Mosaic),
				new RegEntry($"${baseAddr:X}.7", ResourceHelper.GetMessage("RegView_Gba_BPPSelect"), layer.Bpp8Mode ? ResourceHelper.GetMessage("RegView_Common_8Bpp") : ResourceHelper.GetMessage("RegView_Gba_4BPP"), layer.Bpp8Mode),

				new RegEntry($"${baseAddr+1:X}.0-4", ResourceHelper.GetMessage("RegView_Gba_TilemapAddress"), layer.TilemapAddr),
			});

			if(i >= 2) {
				entries.Add(new RegEntry($"${baseAddr + 1:X}.5", ResourceHelper.GetMessage("RegView_Gba_Wraparound"), layer.WrapAround));
			}

			if(ppu.BgMode == 0 || i < 2) {
				entries.Add(new RegEntry($"${baseAddr + 1:X}.6-7", ResourceHelper.GetMessage("RegView_Gba_Size"),
					(layer.DoubleWidth ? "512" : "256") + "x" +
					(layer.DoubleHeight ? "512" : "256")
				, layer.ScreenSize));
			} else {
				int size = 128 << layer.ScreenSize;
				entries.Add(new RegEntry($"${baseAddr + 1:X}.6-7", ResourceHelper.GetMessage("RegView_Gba_Size"), size + "x" + size, layer.ScreenSize));
			}

			baseAddr = 0x4000010 + i * 4;
			entries.Add(new RegEntry($"${baseAddr:X}.0-15", ResourceHelper.GetMessage("RegView_Gba_ScrollX"), layer.ScrollX));
			entries.Add(new RegEntry($"${baseAddr + 2:X}.0-15", ResourceHelper.GetMessage("RegView_Gba_ScrollY"), layer.ScrollY));

			if(i >= 2) {
				GbaTransformConfig cfg = ppu.Transform[i - 2];
				entries.Add(new RegEntry($"${0x4000020 + (i - 2) * 0x10:X}-1", ResourceHelper.GetMessage("RegView_Gba_ParamA"), cfg.Matrix[0]));
				entries.Add(new RegEntry($"${0x4000022 + (i - 2) * 0x10:X}-3", ResourceHelper.GetMessage("RegView_Gba_ParamB"), cfg.Matrix[1]));
				entries.Add(new RegEntry($"${0x4000024 + (i - 2) * 0x10:X}-5", ResourceHelper.GetMessage("RegView_Gba_ParamC"), cfg.Matrix[2]));
				entries.Add(new RegEntry($"${0x4000026 + (i - 2) * 0x10:X}-7", ResourceHelper.GetMessage("RegView_Gba_ParamD"), cfg.Matrix[3]));
				entries.Add(new RegEntry($"${0x4000028 + (i - 2) * 0x10:X}-B.0-27", ResourceHelper.GetMessage("RegView_Gba_OriginX"), ((int)cfg.OriginX << 4) >> 4, Format.X28));
				entries.Add(new RegEntry($"${0x400002C + (i - 2) * 0x10:X}-F.0-27", ResourceHelper.GetMessage("RegView_Gba_OriginY"), ((int)cfg.OriginY << 4) >> 4, Format.X28));
			}
		}

		entries.Add(new RegEntry($"", ResourceHelper.GetMessage("RegView_Gba_Windows")));
		entries.Add(new RegEntry($"", ResourceHelper.GetMessage("RegView_Gba_Window0")));
		entries.Add(new RegEntry($"$4000040", ResourceHelper.GetMessage("RegView_Gba_RightX"), ppu.Window[0].RightX));
		entries.Add(new RegEntry($"$4000041", ResourceHelper.GetMessage("RegView_Gba_LeftX"), ppu.Window[0].LeftX));
		entries.Add(new RegEntry($"$4000044", ResourceHelper.GetMessage("RegView_Gba_BottomY"), ppu.Window[0].BottomY));
		entries.Add(new RegEntry($"$4000045", ResourceHelper.GetMessage("RegView_Gba_TopY"), ppu.Window[0].TopY));

		entries.Add(new RegEntry($"$4000048.0", ResourceHelper.GetMessage("RegView_Gba_BG0Enabled"), ppu.WindowActiveLayers[0] != 0));
		entries.Add(new RegEntry($"$4000048.1", ResourceHelper.GetMessage("RegView_Gba_BG1Enabled"), ppu.WindowActiveLayers[1] != 0));
		entries.Add(new RegEntry($"$4000048.2", ResourceHelper.GetMessage("RegView_Gba_BG2Enabled"), ppu.WindowActiveLayers[2] != 0));
		entries.Add(new RegEntry($"$4000048.3", ResourceHelper.GetMessage("RegView_Gba_BG3Enabled"), ppu.WindowActiveLayers[3] != 0));
		entries.Add(new RegEntry($"$4000048.4", ResourceHelper.GetMessage("RegView_Gba_OBJEnabled"), ppu.WindowActiveLayers[4] != 0));
		entries.Add(new RegEntry($"$4000048.5", ResourceHelper.GetMessage("RegView_Gba_ColorEffectsEnabled"), ppu.WindowActiveLayers[5] != 0));

		entries.Add(new RegEntry($"", ResourceHelper.GetMessage("RegView_Gba_Window1")));
		entries.Add(new RegEntry($"$4000042", ResourceHelper.GetMessage("RegView_Gba_RightX"), ppu.Window[1].RightX));
		entries.Add(new RegEntry($"$4000043", ResourceHelper.GetMessage("RegView_Gba_LeftX"), ppu.Window[1].LeftX));
		entries.Add(new RegEntry($"$4000046", ResourceHelper.GetMessage("RegView_Gba_BottomY"), ppu.Window[1].BottomY));
		entries.Add(new RegEntry($"$4000047", ResourceHelper.GetMessage("RegView_Gba_TopY"), ppu.Window[1].TopY));

		entries.Add(new RegEntry($"$4000049.0", ResourceHelper.GetMessage("RegView_Gba_BG0Enabled"), ppu.WindowActiveLayers[6] != 0));
		entries.Add(new RegEntry($"$4000049.1", ResourceHelper.GetMessage("RegView_Gba_BG1Enabled"), ppu.WindowActiveLayers[7] != 0));
		entries.Add(new RegEntry($"$4000049.2", ResourceHelper.GetMessage("RegView_Gba_BG2Enabled"), ppu.WindowActiveLayers[8] != 0));
		entries.Add(new RegEntry($"$4000049.3", ResourceHelper.GetMessage("RegView_Gba_BG3Enabled"), ppu.WindowActiveLayers[9] != 0));
		entries.Add(new RegEntry($"$4000049.4", ResourceHelper.GetMessage("RegView_Gba_OBJEnabled"), ppu.WindowActiveLayers[10] != 0));
		entries.Add(new RegEntry($"$4000049.5", ResourceHelper.GetMessage("RegView_Gba_ColorEffectsEnabled"), ppu.WindowActiveLayers[11] != 0));

		entries.Add(new RegEntry($"", ResourceHelper.GetMessage("RegView_Gba_OutsideWindow")));
		entries.Add(new RegEntry($"$400004A.0", ResourceHelper.GetMessage("RegView_Gba_BG0Enabled"), ppu.WindowActiveLayers[18] != 0));
		entries.Add(new RegEntry($"$400004A.1", ResourceHelper.GetMessage("RegView_Gba_BG1Enabled"), ppu.WindowActiveLayers[19] != 0));
		entries.Add(new RegEntry($"$400004A.2", ResourceHelper.GetMessage("RegView_Gba_BG2Enabled"), ppu.WindowActiveLayers[20] != 0));
		entries.Add(new RegEntry($"$400004A.3", ResourceHelper.GetMessage("RegView_Gba_BG3Enabled"), ppu.WindowActiveLayers[21] != 0));
		entries.Add(new RegEntry($"$400004A.4", ResourceHelper.GetMessage("RegView_Gba_OBJEnabled"), ppu.WindowActiveLayers[22] != 0));
		entries.Add(new RegEntry($"$400004A.5", ResourceHelper.GetMessage("RegView_Gba_ColorEffectsEnabled"), ppu.WindowActiveLayers[23] != 0));

		entries.Add(new RegEntry($"", ResourceHelper.GetMessage("RegView_Gba_ObjectWindow")));
		entries.Add(new RegEntry($"$400004B.0", ResourceHelper.GetMessage("RegView_Gba_BG0Enabled"), ppu.WindowActiveLayers[12] != 0));
		entries.Add(new RegEntry($"$400004B.1", ResourceHelper.GetMessage("RegView_Gba_BG1Enabled"), ppu.WindowActiveLayers[13] != 0));
		entries.Add(new RegEntry($"$400004B.2", ResourceHelper.GetMessage("RegView_Gba_BG2Enabled"), ppu.WindowActiveLayers[14] != 0));
		entries.Add(new RegEntry($"$400004B.3", ResourceHelper.GetMessage("RegView_Gba_BG3Enabled"), ppu.WindowActiveLayers[15] != 0));
		entries.Add(new RegEntry($"$400004B.4", ResourceHelper.GetMessage("RegView_Gba_OBJEnabled"), ppu.WindowActiveLayers[16] != 0));
		entries.Add(new RegEntry($"$400004B.5", ResourceHelper.GetMessage("RegView_Gba_ColorEffectsEnabled"), ppu.WindowActiveLayers[17] != 0));

		entries.Add(new RegEntry($"", ResourceHelper.GetMessage("RegView_Gba_Mosaic")));
		entries.Add(new RegEntry($"$400004C.0-3", ResourceHelper.GetMessage("RegView_Gba_BGMosaicXSize"), ppu.BgMosaicSizeX));
		entries.Add(new RegEntry($"$400004C.4-7", ResourceHelper.GetMessage("RegView_Gba_BGMosaicYSize"), ppu.BgMosaicSizeY));
		entries.Add(new RegEntry($"$400004D.0-3", ResourceHelper.GetMessage("RegView_Gba_OBJMosaicXSize"), ppu.ObjMosaicSizeX));
		entries.Add(new RegEntry($"$400004D.4-7", ResourceHelper.GetMessage("RegView_Gba_OBJMosaicYSize"), ppu.ObjMosaicSizeY));

		entries.Add(new RegEntry($"", ResourceHelper.GetMessage("RegView_Gba_ColorEffects")));
		entries.Add(new RegEntry($"$4000050.0", ResourceHelper.GetMessage("RegView_Gba_BG0MainTarget"), ppu.BlendMain[0] != 0));
		entries.Add(new RegEntry($"$4000050.1", ResourceHelper.GetMessage("RegView_Gba_BG1MainTarget"), ppu.BlendMain[1] != 0));
		entries.Add(new RegEntry($"$4000050.2", ResourceHelper.GetMessage("RegView_Gba_BG2MainTarget"), ppu.BlendMain[2] != 0));
		entries.Add(new RegEntry($"$4000050.3", ResourceHelper.GetMessage("RegView_Gba_BG3MainTarget"), ppu.BlendMain[3] != 0));
		entries.Add(new RegEntry($"$4000050.4", ResourceHelper.GetMessage("RegView_Gba_OBJMainTarget"), ppu.BlendMain[4] != 0));
		entries.Add(new RegEntry($"$4000050.5", ResourceHelper.GetMessage("RegView_Gba_BackdropMainTarget"), ppu.BlendMain[5] != 0));
		entries.Add(new RegEntry($"$4000050.6-7", ResourceHelper.GetMessage("RegView_Gba_EffectType"), ppu.BlendEffect));
		entries.Add(new RegEntry($"$4000051.0", ResourceHelper.GetMessage("RegView_Gba_BG0SubTarget"), ppu.BlendSub[0] != 0));
		entries.Add(new RegEntry($"$4000051.1", ResourceHelper.GetMessage("RegView_Gba_BG1SubTarget"), ppu.BlendSub[1] != 0));
		entries.Add(new RegEntry($"$4000051.2", ResourceHelper.GetMessage("RegView_Gba_BG2SubTarget"), ppu.BlendSub[2] != 0));
		entries.Add(new RegEntry($"$4000051.3", ResourceHelper.GetMessage("RegView_Gba_BG3SubTarget"), ppu.BlendSub[3] != 0));
		entries.Add(new RegEntry($"$4000051.4", ResourceHelper.GetMessage("RegView_Gba_OBJSubTarget"), ppu.BlendSub[4] != 0));
		entries.Add(new RegEntry($"$4000051.5", ResourceHelper.GetMessage("RegView_Gba_BackdropSubTarget"), ppu.BlendSub[5] != 0));
		entries.Add(new RegEntry($"$4000052.0-4", ResourceHelper.GetMessage("RegView_Gba_BlendMainCoefficient"), ppu.BlendMainCoefficient));
		entries.Add(new RegEntry($"$4000053.0-4", ResourceHelper.GetMessage("RegView_Gba_BlendSubCoefficient"), ppu.BlendSubCoefficient));
		entries.Add(new RegEntry($"$4000054.0-4", ResourceHelper.GetMessage("RegView_Gba_BrightnessCoefficient"), ppu.Brightness));

		return new RegisterViewerTab(ResourceHelper.GetMessage("RegView_Gba_PPU"), entries, CpuType.Gba, MemoryType.GbaMemory);
	}

	private static RegisterViewerTab GetGbaApuTab(ref GbaState gbaState)
	{
		List<RegEntry> entries = new List<RegEntry>();

		GbaApuState apu = gbaState.Apu.Common;
		entries.AddRange(new List<RegEntry>() {
			new RegEntry($"", ResourceHelper.GetMessage("RegView_Common_Volume")),
			new RegEntry("$4000080.0-2", ResourceHelper.GetMessage("RegView_Gba_VolumeRightGB"), apu.RightVolume),
			new RegEntry("$4000080.4-6", ResourceHelper.GetMessage("RegView_Gba_VolumeLeftGB"), apu.LeftVolume),
			new RegEntry("$4000081.0", ResourceHelper.GetMessage("RegView_Common_RightSquare1Enabled"), apu.EnableRightSq1 != 0),
			new RegEntry("$4000081.1", ResourceHelper.GetMessage("RegView_Common_RightSquare2Enabled"), apu.EnableRightSq2 != 0),
			new RegEntry("$4000081.2", ResourceHelper.GetMessage("RegView_Common_RightWaveEnabled"), apu.EnableRightWave != 0),
			new RegEntry("$4000081.3", ResourceHelper.GetMessage("RegView_Common_RightNoiseEnabled"), apu.EnableRightNoise != 0),
			new RegEntry("$4000081.4", ResourceHelper.GetMessage("RegView_Common_LeftSquare1Enabled"), apu.EnableLeftSq1 != 0),
			new RegEntry("$4000081.5", ResourceHelper.GetMessage("RegView_Common_LeftSquare2Enabled"), apu.EnableLeftSq2 != 0),
			new RegEntry("$4000081.6", ResourceHelper.GetMessage("RegView_Common_LeftWaveEnabled"), apu.EnableLeftWave != 0),
			new RegEntry("$4000081.7", ResourceHelper.GetMessage("RegView_Common_LeftNoiseEnabled"), apu.EnableLeftNoise != 0),

			new RegEntry($"$4000082.0-1", ResourceHelper.GetMessage("RegView_Gba_GameBoyChannelsVolume"), apu.GbVolume switch {
				0 => ResourceHelper.GetMessage("RegView_Gba_Percent25"),
				1 => ResourceHelper.GetMessage("RegView_Gba_Percent50"),
				2 => ResourceHelper.GetMessage("RegView_Gba_Percent100"),
				3 or _ => ResourceHelper.GetMessage("RegView_Gba_Invalid")
			}, apu.GbVolume),
			new RegEntry($"$4000082.2", ResourceHelper.GetMessage("RegView_Gba_ChannelAVolume"), apu.VolumeA == 0 ? ResourceHelper.GetMessage("RegView_Gba_Percent50") : ResourceHelper.GetMessage("RegView_Gba_Percent100"), apu.VolumeA),
			new RegEntry($"$4000082.3", ResourceHelper.GetMessage("RegView_Gba_ChannelBVolume"), apu.VolumeB == 0 ? ResourceHelper.GetMessage("RegView_Gba_Percent50") : ResourceHelper.GetMessage("RegView_Gba_Percent100"), apu.VolumeB),

			new RegEntry($"", ResourceHelper.GetMessage("RegView_Gba_ChannelA")),
			new RegEntry($"$4000083.0", ResourceHelper.GetMessage("RegView_Gba_ChannelALeftEnabled"), apu.EnableLeftA),
			new RegEntry($"$4000083.1", ResourceHelper.GetMessage("RegView_Gba_ChannelARightEnabled"), apu.EnableRightA),
			new RegEntry($"$4000083.2", ResourceHelper.GetMessage("RegView_Gba_ChannelATimer"), apu.TimerA),
			new RegEntry($"", ResourceHelper.GetMessage("RegView_Gba_CurrentOutput"), apu.DmaSampleA),

			new RegEntry($"", ResourceHelper.GetMessage("RegView_Gba_ChannelB")),
			new RegEntry($"$4000083.4", ResourceHelper.GetMessage("RegView_Gba_ChannelBLeftEnabled"), apu.EnableLeftB),
			new RegEntry($"$4000083.5", ResourceHelper.GetMessage("RegView_Gba_ChannelBRightEnabled"), apu.EnableRightB),
			new RegEntry($"$4000083.6", ResourceHelper.GetMessage("RegView_Gba_ChannelBTimer"), apu.TimerB),
			new RegEntry($"", ResourceHelper.GetMessage("RegView_Gba_CurrentOutput"), apu.DmaSampleB),

			new RegEntry($"", ResourceHelper.GetMessage("RegView_Gba_Misc")),
			new RegEntry($"$4000084.7", ResourceHelper.GetMessage("RegView_Common_APUEnabled"), apu.ApuEnabled),
			new RegEntry($"$4000088-9.1-9", ResourceHelper.GetMessage("RegView_Gba_SoundBias"), apu.Bias),
			new RegEntry($"$4000088-9.14-15", ResourceHelper.GetMessage("RegView_Gba_SamplingRate"), apu.SamplingRate switch {
				0 => ResourceHelper.GetMessage("RegView_Gba_SamplingRate0"),
				1 => ResourceHelper.GetMessage("RegView_Gba_SamplingRate1"),
				2 => ResourceHelper.GetMessage("RegView_Gba_SamplingRate2"),
				3 or _ => ResourceHelper.GetMessage("RegView_Gba_SamplingRate3"),
			}, apu.SamplingRate),

			new RegEntry("", ResourceHelper.GetMessage("RegView_Common_FrameSequencer"), apu.FrameSequenceStep),
		});

		GbaSquareState sq1 = gbaState.Apu.Square1;
		entries.AddRange(new List<RegEntry>() {
			new RegEntry("$4000060-65", ResourceHelper.GetMessage("RegView_Common_Square1")),
			new RegEntry("$4000060.0-2", ResourceHelper.GetMessage("RegView_Common_SweepShift"), sq1.SweepShift),
			new RegEntry("$4000060.3", ResourceHelper.GetMessage("RegView_Common_SweepNegate"), sq1.SweepNegate),
			new RegEntry("$4000060.4-7", ResourceHelper.GetMessage("RegView_Common_SweepPeriod"), sq1.SweepPeriod),

			new RegEntry("$4000062.0-5", ResourceHelper.GetMessage("RegView_Common_Length"), sq1.Length),
			new RegEntry("$4000062.6-7", ResourceHelper.GetMessage("RegView_Common_Duty"), sq1.Duty),

			new RegEntry("$4000063.0-2", ResourceHelper.GetMessage("RegView_Common_EnvelopePeriod"), sq1.EnvPeriod),
			new RegEntry("$4000063.3", ResourceHelper.GetMessage("RegView_Common_EnvelopeIncreaseVolume"), sq1.EnvRaiseVolume),
			new RegEntry("$4000063.4-7", ResourceHelper.GetMessage("RegView_Common_EnvelopeVolume"), sq1.EnvVolume),

			new RegEntry("$4000064-65.0-10", ResourceHelper.GetMessage("RegView_Common_Frequency"), sq1.Frequency),
			new RegEntry("$4000065.6", ResourceHelper.GetMessage("RegView_Common_LengthCounterEnabled"), sq1.LengthEnabled),
			new RegEntry("$4000065.7", ResourceHelper.GetMessage("RegView_Common_ChannelEnabled"), sq1.Enabled),

			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_Timer"), sq1.Timer),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_DutyPosition"), sq1.DutyPos),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_SweepEnabled"), sq1.SweepEnabled),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_SweepFrequency"), sq1.SweepFreq),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_SweepTimer"), sq1.SweepTimer),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_EnvelopeTimer"), sq1.EnvTimer),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_Output"), sq1.Output)
		});

		GbaSquareState sq2 = gbaState.Apu.Square2;
		entries.AddRange(new List<RegEntry>() {
			new RegEntry("$4000068-6D", ResourceHelper.GetMessage("RegView_Common_Square2")),
			new RegEntry("$4000068.0-5", ResourceHelper.GetMessage("RegView_Common_Length"), sq2.Length),
			new RegEntry("$4000068.6-7", ResourceHelper.GetMessage("RegView_Common_Duty"), sq2.Duty),

			new RegEntry("$4000069.0-2", ResourceHelper.GetMessage("RegView_Common_EnvelopePeriod"), sq2.EnvPeriod),
			new RegEntry("$4000069.3", ResourceHelper.GetMessage("RegView_Common_EnvelopeIncreaseVolume"), sq2.EnvRaiseVolume),
			new RegEntry("$4000069.4-7", ResourceHelper.GetMessage("RegView_Common_EnvelopeVolume"), sq2.EnvVolume),

			new RegEntry("$400006C-6D.0-10", ResourceHelper.GetMessage("RegView_Common_Frequency"), sq2.Frequency),
			new RegEntry("$400006D.6", ResourceHelper.GetMessage("RegView_Common_LengthCounterEnabled"), sq2.LengthEnabled),
			new RegEntry("$400006D.7", ResourceHelper.GetMessage("RegView_Common_ChannelEnabled"), sq2.Enabled),

			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_Timer"), sq2.Timer),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_DutyPosition"), sq2.DutyPos),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_EnvelopeTimer"), sq2.EnvTimer),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_Output"), sq2.Output)
		});

		GbaWaveState wave = gbaState.Apu.Wave;
		entries.AddRange(new List<RegEntry>() {
			new RegEntry("$4000070-75", ResourceHelper.GetMessage("RegView_Common_Wave")),
			new RegEntry("$4000070.7", ResourceHelper.GetMessage("RegView_Gba_SoundEnabled"), wave.DacEnabled),
			new RegEntry("$4000070.6", ResourceHelper.GetMessage("RegView_Gba_SelectedBank"), wave.SelectedBank),
			new RegEntry("$4000070.5", ResourceHelper.GetMessage("RegView_Gba_DoubleSize"), wave.DoubleLength),

			new RegEntry("$4000072", ResourceHelper.GetMessage("RegView_Common_Length"), wave.Length),

			new RegEntry("$4000073.5-6", ResourceHelper.GetMessage("RegView_Common_Volume"), wave.Volume),
			new RegEntry("$4000073.7", ResourceHelper.GetMessage("RegView_Gba_Force75PercentVolume"), wave.OverrideVolume),

			new RegEntry("$4000074-75.0-10", ResourceHelper.GetMessage("RegView_Common_Frequency"), wave.Frequency),

			new RegEntry("$4000075.6", ResourceHelper.GetMessage("RegView_Common_LengthCounterEnabled"), wave.LengthEnabled),
			new RegEntry("$4000075.7", ResourceHelper.GetMessage("RegView_Common_ChannelEnabled"), wave.Enabled),

			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_Timer"), wave.Timer),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Gba_SampleBuffer"), wave.SampleBuffer),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Gba_Position"), wave.Position),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_Output"), wave.Output),
		});

		GbaNoiseState noise = gbaState.Apu.Noise;
		entries.AddRange(new List<RegEntry>() {
			new RegEntry("$4000078-$400007D", ResourceHelper.GetMessage("RegView_Common_Noise")),
			new RegEntry("$4000078.0-5", ResourceHelper.GetMessage("RegView_Common_Length"), noise.Length),

			new RegEntry("$4000079.0-2", ResourceHelper.GetMessage("RegView_Common_EnvelopePeriod"), noise.EnvPeriod),
			new RegEntry("$4000079.3", ResourceHelper.GetMessage("RegView_Common_EnvelopeIncreaseVolume"), noise.EnvRaiseVolume),
			new RegEntry("$4000079.4-7", ResourceHelper.GetMessage("RegView_Common_EnvelopeVolume"), noise.EnvVolume),

			new RegEntry("$400007C.0-2", ResourceHelper.GetMessage("RegView_Common_Divisor"), noise.Divisor),
			new RegEntry("$400007C.3", ResourceHelper.GetMessage("RegView_Common_ShortMode"), noise.ShortWidthMode),
			new RegEntry("$400007C.4-7", ResourceHelper.GetMessage("RegView_Common_PeriodShift"), noise.PeriodShift),

			new RegEntry("$400007D.6", ResourceHelper.GetMessage("RegView_Common_LengthCounterEnabled"), noise.LengthEnabled),
			new RegEntry("$400007D.7", ResourceHelper.GetMessage("RegView_Common_ChannelEnabled"), noise.Enabled),

			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_Timer"), noise.Timer),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_EnvelopeTimer"), noise.EnvTimer),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_ShiftRegister"), noise.ShiftRegister, Format.X16),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_Output"), noise.Output)
		});

		return new RegisterViewerTab(ResourceHelper.GetMessage("RegView_Gba_APU"), entries, CpuType.Gba, MemoryType.GbaMemory);
	}

	private static RegisterViewerTab GetGbaTimerTab(ref GbaState gbaState)
	{
		List<RegEntry> entries = new List<RegEntry>();

		GbaTimersState timers = gbaState.Timer;
		for(int i = 0; i < 4; i++) {
			GbaTimerState timer = timers.Timer[i];
			int baseAddr = 0x4000100 + i * 4;

			byte prescaler = timer.PrescaleMask switch {
				0 => 0,
				0x3F => 1,
				0xFF => 2,
				_ or 0x3FF => 3,
			};

			entries.AddRange(new List<RegEntry>() {
				new RegEntry("", ResourceHelper.GetMessage("RegView_Gba_Timer" + i)),
				new RegEntry($"${baseAddr:X}-{(baseAddr+1)&0xF:X}", ResourceHelper.GetMessage("RegView_Gba_ReloadValueW"), timer.ReloadValue),
				new RegEntry($"${baseAddr:X}-{(baseAddr+1)&0xF:X}", ResourceHelper.GetMessage("RegView_Gba_TimerValueR"), timer.Timer),
				new RegEntry($"${baseAddr+2:X}.0-1", ResourceHelper.GetMessage("RegView_Gba_Prescale"), prescaler + " (" + (timer.PrescaleMask + 1) + ")", prescaler),
				new RegEntry($"${baseAddr+2:X}.2", ResourceHelper.GetMessage("RegView_Gba_CountUpMode"), timer.Mode),
				new RegEntry($"${baseAddr+2:X}.6", ResourceHelper.GetMessage("RegView_Common_IRQEnabled"), timer.IrqEnabled),
				new RegEntry($"${baseAddr+2:X}.7", ResourceHelper.GetMessage("RegView_Common_Enabled"), timer.Enabled),
			});
		}

		return new RegisterViewerTab(ResourceHelper.GetMessage("RegView_Gba_Timers"), entries, CpuType.Gba, MemoryType.GbaMemory);
	}

	private static RegisterViewerTab GetGbaDmaTab(ref GbaState gbaState)
	{
		List<RegEntry> entries = new List<RegEntry>();

		GbaDmaControllerState state = gbaState.Dma;
		for(int i = 0; i < 4; i++) {
			GbaDmaChannel ch = state.Ch[i];
			int baseAddr = 0x40000B0 + i * 12;
			entries.AddRange(new List<RegEntry>() {
				new RegEntry("", ResourceHelper.GetMessage("RegView_Gba_DMAChannel" + i)),
				new RegEntry($"${baseAddr:X}-{(baseAddr+3)&0xF:X}", ResourceHelper.GetMessage("RegView_Common_Source"), ch.Source),
				new RegEntry($"${baseAddr+4:X}-{(baseAddr+7)&0xF:X}", ResourceHelper.GetMessage("RegView_Common_Destination"), ch.Destination),
				new RegEntry($"${baseAddr+8:X}-{(baseAddr+9)&0xF:X}", ResourceHelper.GetMessage("RegView_Common_Length"), ch.Length),
				new RegEntry($"${baseAddr+10:X}-{(baseAddr+11)&0xF:X}.5-6", ResourceHelper.GetMessage("RegView_Gba_DstMode"), ch.DestMode),
				new RegEntry($"${baseAddr+10:X}-{(baseAddr+11)&0xF:X}.7-8", ResourceHelper.GetMessage("RegView_Gba_SrcMode"), ch.SrcMode),
				new RegEntry($"${baseAddr+10:X}-{(baseAddr+11)&0xF:X}.9", ResourceHelper.GetMessage("RegView_Gba_Repeat"), ch.Repeat),
				new RegEntry($"${baseAddr+10:X}-{(baseAddr+11)&0xF:X}.10", ResourceHelper.GetMessage("RegView_Gba_32bitTransfer"), ch.WordTransfer),
				new RegEntry($"${baseAddr+10:X}-{(baseAddr+11)&0xF:X}.11", ResourceHelper.GetMessage("RegView_Gba_DRQ"), ch.DrqMode),
				new RegEntry($"${baseAddr+10:X}-{(baseAddr+11)&0xF:X}.12-13", ResourceHelper.GetMessage("RegView_Gba_Trigger"), ch.Trigger),
				new RegEntry($"${baseAddr+10:X}-{(baseAddr+11)&0xF:X}.14", ResourceHelper.GetMessage("RegView_Common_IRQEnabled"), ch.IrqEnabled),
				new RegEntry($"${baseAddr+10:X}-{(baseAddr+11)&0xF:X}.15", ResourceHelper.GetMessage("RegView_Common_Enabled"), ch.Enabled),
				new RegEntry("", ResourceHelper.GetMessage("RegView_Gba_Active"), ch.Active),

				new RegEntry($"", ResourceHelper.GetMessage("RegView_Gba_CurrentSrc"), ch.SrcLatch),
				new RegEntry($"", ResourceHelper.GetMessage("RegView_Gba_CurrentDest"), ch.DestLatch),
			});
		}

		return new RegisterViewerTab(ResourceHelper.GetMessage("RegView_Gba_DMA"), entries, CpuType.Gba, MemoryType.GbaMemory);
	}
}
