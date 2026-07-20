using Mesen.Debugger.ViewModels;
using Mesen.Interop;
using Mesen.Localization;
using System.Collections.Generic;
using static Mesen.Debugger.ViewModels.RegEntry;

namespace Mesen.Debugger.RegisterViewer;

public class GbRegisterViewer
{
	public static List<RegisterViewerTab> GetTabs(ref GbState gbState)
	{
		List<RegisterViewerTab> tabs = new() {
			GetGbLcdTab(ref gbState),
			GetGbApuTab(ref gbState),
			GetGbMiscTab(ref gbState),
		};

		if(gbState.Type == GbType.Cgb) {
			tabs.Add(GetGbCgbTab(ref gbState));
		}
		return tabs;
	}

	public static RegisterViewerTab GetGbLcdTab(ref GbState gb, string tabPrefix = "")
	{
		List<RegEntry> entries = new List<RegEntry>();

		GbPpuState ppu = gb.Ppu;
		entries.AddRange(new List<RegEntry>() {
			new RegEntry("", ResourceHelper.GetMessage("RegView_Common_State")),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Common_CycleH"), ppu.Cycle),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Common_ScanlineV"), ppu.Scanline),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Common_FrameNumber"), ppu.FrameCount),

			new RegEntry("$FF40", ResourceHelper.GetMessage("RegView_Gb_LCDControlLCDC")),
			new RegEntry("$FF40.0", ResourceHelper.GetMessage("RegView_Gb_BGEnabled"), ppu.BgEnabled),
			new RegEntry("$FF40.1", ResourceHelper.GetMessage("RegView_Common_SpritesEnabled"), ppu.SpritesEnabled),
			new RegEntry("$FF40.2", ResourceHelper.GetMessage("RegView_Gb_SpriteSize"), ppu.LargeSprites ? ResourceHelper.GetMessage("RegView_Common_8x16") : ResourceHelper.GetMessage("RegView_Common_8x8"), ppu.LargeSprites),
			new RegEntry("$FF40.3", ResourceHelper.GetMessage("RegView_Gb_BGTilemapSelect"), ppu.BgTilemapSelect ? 0x9C00 : 0x9800, Format.X16),
			new RegEntry("$FF40.4", ResourceHelper.GetMessage("RegView_Gb_BGTileSelect"), ppu.BgTileSelect ? "$8000-$8FFF" : "$8800-$97FF", ppu.BgTileSelect),
			new RegEntry("$FF40.5", ResourceHelper.GetMessage("RegView_Gb_WindowEnabled"), ppu.WindowEnabled),
			new RegEntry("$FF40.6", ResourceHelper.GetMessage("RegView_Gb_WindowTilemapSelect"), ppu.WindowTilemapSelect ? 0x9C00 : 0x9800, Format.X16),
			new RegEntry("$FF40.7", ResourceHelper.GetMessage("RegView_Common_LCDEnabled"), ppu.LcdEnabled),

			new RegEntry("$FF41", ResourceHelper.GetMessage("RegView_Gb_LCDStatusSTAT")),
			new RegEntry("$FF41.0-1", ResourceHelper.GetMessage("RegView_Gb_Mode"), (int)ppu.Mode),
			new RegEntry("$FF41.2", ResourceHelper.GetMessage("RegView_Gb_CoincidenceFlag"), ppu.LyCoincidenceFlag),
			new RegEntry("$FF41.3", ResourceHelper.GetMessage("RegView_Gb_Mode0HBlankIRQ"), (ppu.Status & 0x08) != 0),
			new RegEntry("$FF41.4", ResourceHelper.GetMessage("RegView_Gb_Mode1VBlankIRQ"), (ppu.Status & 0x10) != 0),
			new RegEntry("$FF41.5", ResourceHelper.GetMessage("RegView_Gb_Mode2OAMIRQ"), (ppu.Status & 0x20) != 0),
			new RegEntry("$FF41.6", ResourceHelper.GetMessage("RegView_Gb_LYCCoincidenceIRQ"), (ppu.Status & 0x40) != 0),

			new RegEntry("", ResourceHelper.GetMessage("RegView_Gb_LCDRegisters")),
			new RegEntry("$FF42", ResourceHelper.GetMessage("RegView_Gb_ScrollYSCY"), ppu.ScrollY, Format.X8),
			new RegEntry("$FF43", ResourceHelper.GetMessage("RegView_Gb_ScrollXSCX"), ppu.ScrollX, Format.X8),
			new RegEntry("$FF44", ResourceHelper.GetMessage("RegView_Gb_YCoordinateLY"), ppu.Ly, Format.X8),
			new RegEntry("$FF45", ResourceHelper.GetMessage("RegView_Gb_LYCCompareLYC"), ppu.LyCompare, Format.X8),
			new RegEntry("$FF47", ResourceHelper.GetMessage("RegView_Gb_BGPaletteBGP"), ppu.BgPalette, Format.X8),
			new RegEntry("$FF48", ResourceHelper.GetMessage("RegView_Gb_OBJPalette0OBP0"), ppu.ObjPalette0, Format.X8),
			new RegEntry("$FF49", ResourceHelper.GetMessage("RegView_Gb_OBJPalette1OBP1"), ppu.ObjPalette1, Format.X8),
			new RegEntry("$FF4A", ResourceHelper.GetMessage("RegView_Gb_WindowYWY"), ppu.WindowY, Format.X8),
			new RegEntry("$FF4B", ResourceHelper.GetMessage("RegView_Gb_WindowXWX"), ppu.WindowX, Format.X8),
		});

		return new RegisterViewerTab(tabPrefix + ResourceHelper.GetMessage("RegView_Gb_LCD"), entries, CpuType.Gameboy, MemoryType.GameboyMemory);
	}

	private static RegisterViewerTab GetGbCgbTab(ref GbState gb)
	{
		List<RegEntry> entries = new List<RegEntry>();

		GbPpuState ppu = gb.Ppu;
		GbDmaControllerState dma = gb.Dma;
		entries.AddRange(new List<RegEntry>() {
			new RegEntry("$FF4D.0", ResourceHelper.GetMessage("RegView_Gb_CPUSwitchSpeedRequest"), gb.MemoryManager.CgbSwitchSpeedRequest),
			new RegEntry("$FF4D.7", ResourceHelper.GetMessage("RegView_Gb_CPUSpeed"), gb.MemoryManager.CgbHighSpeed ? ResourceHelper.GetMessage("RegView_Gb_8_39MHz") : ResourceHelper.GetMessage("RegView_Gb_4_19MHz"), gb.MemoryManager.CgbHighSpeed),

			new RegEntry("$FF4F.0", ResourceHelper.GetMessage("RegView_Gb_VideoRAMBank"), ppu.CgbVramBank),

			new RegEntry("", ResourceHelper.GetMessage("RegView_Gb_DMARegisters")),
			new RegEntry("$FF51-52", ResourceHelper.GetMessage("RegView_Gb_DMASource"), dma.CgbDmaSource, Format.X16),
			new RegEntry("$FF53-54", ResourceHelper.GetMessage("RegView_Gb_DMADestination"), dma.CgbDmaDest, Format.X16),
			new RegEntry("$FF55.0-6", ResourceHelper.GetMessage("RegView_Gb_DMALength"), dma.CgbDmaLength, Format.X8),
			new RegEntry("$FF55.7", ResourceHelper.GetMessage("RegView_Gb_HDMAInactive"), !dma.CgbHdmaRunning),

			new RegEntry("", ResourceHelper.GetMessage("RegView_Gb_PaletteRegisters")),
			new RegEntry("$FF68", ResourceHelper.GetMessage("RegView_Gb_BGPI_BG_PaletteIndex")),
			new RegEntry("$FF68.0-5", ResourceHelper.GetMessage("RegView_Gb_BGPaletteAddress"), ppu.CgbBgPalPosition, Format.X8),
			new RegEntry("$FF68.7", ResourceHelper.GetMessage("RegView_Gb_BGPaletteAutoIncrement"), ppu.CgbBgPalAutoInc),
			new RegEntry("$FF6A", ResourceHelper.GetMessage("RegView_Gb_OBPI_OBJPaletteIndex")),
			new RegEntry("$FF6A.0-5", ResourceHelper.GetMessage("RegView_Gb_OBJPaletteAddress"), ppu.CgbObjPalPosition, Format.X8),
			new RegEntry("$FF6A.7", ResourceHelper.GetMessage("RegView_Gb_OBJPaletteAutoIncrement"), ppu.CgbObjPalAutoInc),

			new RegEntry("", ResourceHelper.GetMessage("RegView_Common_Misc")),
			new RegEntry("$FF70.0-2", ResourceHelper.GetMessage("RegView_Gb_WorkRAMBank"), gb.MemoryManager.CgbWorkRamBank, Format.X8),
			new RegEntry("$FF72", ResourceHelper.GetMessage("RegView_Gb_Undocumented"), gb.MemoryManager.CgbRegFF72, Format.X8),
			new RegEntry("$FF73", ResourceHelper.GetMessage("RegView_Gb_Undocumented"), gb.MemoryManager.CgbRegFF73, Format.X8),
			new RegEntry("$FF74", ResourceHelper.GetMessage("RegView_Gb_Undocumented"), gb.MemoryManager.CgbRegFF74, Format.X8),
			new RegEntry("$FF75", ResourceHelper.GetMessage("RegView_Gb_Undocumented"), gb.MemoryManager.CgbRegFF75, Format.X8),
		});

		return new RegisterViewerTab(ResourceHelper.GetMessage("RegView_Gb_CGB"), entries, CpuType.Gameboy, MemoryType.GameboyMemory);
	}

	public static RegisterViewerTab GetGbMiscTab(ref GbState gb, string tabPrefix = "")
	{
		List<RegEntry> entries = new List<RegEntry>();

		GbTimerState timer = gb.Timer;
		entries.AddRange(new List<RegEntry>() {
			new RegEntry("$FF04-7", ResourceHelper.GetMessage("RegView_Gb_Timer")),
			new RegEntry("$FF04", ResourceHelper.GetMessage("RegView_Gb_DIV_Divider"), timer.Divider, Format.X16),
			new RegEntry("$FF05", ResourceHelper.GetMessage("RegView_Gb_TIMA_Counter"), timer.Counter, Format.X8),
			new RegEntry("$FF06", ResourceHelper.GetMessage("RegView_Gb_TMA_Modulo"), timer.Modulo, Format.X8),
			new RegEntry("$FF07", ResourceHelper.GetMessage("RegView_Gb_TAC_Control"), timer.Control, Format.X8)
		});

		GbDmaControllerState dma = gb.Dma;
		entries.AddRange(new List<RegEntry>() {
			new RegEntry("", ResourceHelper.GetMessage("RegView_Gb_DMA")),
			new RegEntry("$FF46", ResourceHelper.GetMessage("RegView_Gb_OAMDMA_Source"), dma.OamDmaSource << 8, Format.X16),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Gb_OAMDMA_Running"), dma.OamDmaRunning)
		});

		GbMemoryManagerState memManager = gb.MemoryManager;
		entries.AddRange(new List<RegEntry>() {
			new RegEntry("", ResourceHelper.GetMessage("RegView_Common_IRQ")),
			new RegEntry("$FF0F", ResourceHelper.GetMessage("RegView_Gb_IF_IRQFlags"), memManager.IrqRequests, Format.X8),
			new RegEntry("$FF0F.0", ResourceHelper.GetMessage("RegView_Gb_IF_VerticalBlankIRQ"), (memManager.IrqRequests & 0x01) != 0),
			new RegEntry("$FF0F.1", ResourceHelper.GetMessage("RegView_Gb_IF_STATIRQ"), (memManager.IrqRequests & 0x02) != 0),
			new RegEntry("$FF0F.2", ResourceHelper.GetMessage("RegView_Gb_IF_TimerIRQ"), (memManager.IrqRequests & 0x04) != 0),
			new RegEntry("$FF0F.3", ResourceHelper.GetMessage("RegView_Gb_IF_SerialIRQ"), (memManager.IrqRequests & 0x08) != 0),
			new RegEntry("$FF0F.4", ResourceHelper.GetMessage("RegView_Gb_IF_JoypadIRQ"), (memManager.IrqRequests & 0x10) != 0),

			new RegEntry("$FFFF", ResourceHelper.GetMessage("RegView_Gb_IE_IRQEnabled"), memManager.IrqEnabled, Format.X8),
			new RegEntry("$FFFF.0", ResourceHelper.GetMessage("RegView_Gb_IE_VerticalBlankIRQEnabled"), (memManager.IrqEnabled & 0x01) != 0),
			new RegEntry("$FFFF.1", ResourceHelper.GetMessage("RegView_Gb_IE_STATIRQEnabled"), (memManager.IrqEnabled & 0x02) != 0),
			new RegEntry("$FFFF.2", ResourceHelper.GetMessage("RegView_Gb_IE_TimerIRQEnabled"), (memManager.IrqEnabled & 0x04) != 0),
			new RegEntry("$FFFF.3", ResourceHelper.GetMessage("RegView_Gb_IE_SerialIRQEnabled"), (memManager.IrqEnabled & 0x08) != 0),
			new RegEntry("$FFFF.4", ResourceHelper.GetMessage("RegView_Gb_IE_JoypadIRQEnabled"), (memManager.IrqEnabled & 0x10) != 0),

			new RegEntry("", ResourceHelper.GetMessage("RegView_Common_Misc")),
			new RegEntry("$FF00", ResourceHelper.GetMessage("RegView_Gb_InputSelect"), gb.ControlManager.InputSelect, Format.X8),
			new RegEntry("$FF01", ResourceHelper.GetMessage("RegView_Gb_SerialData"), memManager.SerialData, Format.X8),
			new RegEntry("$FF02", ResourceHelper.GetMessage("RegView_Gb_SerialControl"), memManager.SerialControl, Format.X8),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Gb_SerialBitCount"), memManager.SerialBitCount),
		});

		return new RegisterViewerTab(tabPrefix + ResourceHelper.GetMessage("RegView_Gb_TimerDMAIRQ"), entries, CpuType.Gameboy, MemoryType.GameboyMemory);
	}

	public static RegisterViewerTab GetGbApuTab(ref GbState gb, string tabPrefix = "")
	{
		List<RegEntry> entries = new List<RegEntry>();

		GbApuState apu = gb.Apu.Common;
		entries.AddRange(new List<RegEntry>() {
			new RegEntry("", ResourceHelper.GetMessage("RegView_Gb_APU")),
			new RegEntry("$FF24.0-2", ResourceHelper.GetMessage("RegView_Common_VolumeRight"), apu.RightVolume),
			new RegEntry("$FF24.3", ResourceHelper.GetMessage("RegView_Gb_ExternalAudioRightEnabled"), apu.ExtAudioRightEnabled),
			new RegEntry("$FF24.4-6", ResourceHelper.GetMessage("RegView_Common_VolumeLeft"), apu.LeftVolume),
			new RegEntry("$FF24.7", ResourceHelper.GetMessage("RegView_Gb_ExternalAudioLeftEnabled"), apu.ExtAudioRightEnabled),
			new RegEntry("$FF25.0", ResourceHelper.GetMessage("RegView_Common_RightSquare1Enabled"), apu.EnableRightSq1 != 0),
			new RegEntry("$FF25.1", ResourceHelper.GetMessage("RegView_Common_RightSquare2Enabled"), apu.EnableRightSq2 != 0),
			new RegEntry("$FF25.2", ResourceHelper.GetMessage("RegView_Common_RightWaveEnabled"), apu.EnableRightWave != 0),
			new RegEntry("$FF25.3", ResourceHelper.GetMessage("RegView_Common_RightNoiseEnabled"), apu.EnableRightNoise != 0),
			new RegEntry("$FF25.4", ResourceHelper.GetMessage("RegView_Common_LeftSquare1Enabled"), apu.EnableLeftSq1 != 0),
			new RegEntry("$FF25.5", ResourceHelper.GetMessage("RegView_Common_LeftSquare2Enabled"), apu.EnableLeftSq2 != 0),
			new RegEntry("$FF25.6", ResourceHelper.GetMessage("RegView_Common_LeftWaveEnabled"), apu.EnableLeftWave != 0),
			new RegEntry("$FF25.7", ResourceHelper.GetMessage("RegView_Common_LeftNoiseEnabled"), apu.EnableLeftNoise != 0),
			new RegEntry("$FF26.7", ResourceHelper.GetMessage("RegView_Common_APUEnabled"), apu.ApuEnabled),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Common_FrameSequencer"), apu.FrameSequenceStep),
		});

		GbSquareState sq1 = gb.Apu.Square1;
		entries.AddRange(new List<RegEntry>() {
			new RegEntry("$FF10-$FF14", ResourceHelper.GetMessage("RegView_Common_Square1")),
			new RegEntry("$FF10.0-2", ResourceHelper.GetMessage("RegView_Common_SweepShift"), sq1.SweepShift),
			new RegEntry("$FF10.3", ResourceHelper.GetMessage("RegView_Common_SweepNegate"), sq1.SweepNegate),
			new RegEntry("$FF10.4-7", ResourceHelper.GetMessage("RegView_Common_SweepPeriod"), sq1.SweepPeriod),

			new RegEntry("$FF11.0-5", ResourceHelper.GetMessage("RegView_Common_Length"), sq1.Length),
			new RegEntry("$FF11.6-7", ResourceHelper.GetMessage("RegView_Common_Duty"), sq1.Duty),

			new RegEntry("$FF12.0-2", ResourceHelper.GetMessage("RegView_Common_EnvelopePeriod"), sq1.EnvPeriod),
			new RegEntry("$FF12.3", ResourceHelper.GetMessage("RegView_Common_EnvelopeIncreaseVolume"), sq1.EnvRaiseVolume),
			new RegEntry("$FF12.4-7", ResourceHelper.GetMessage("RegView_Common_EnvelopeVolume"), sq1.EnvVolume),

			new RegEntry("$FF13-$FF14.0-2", ResourceHelper.GetMessage("RegView_Common_Frequency"), sq1.Frequency),
			new RegEntry("$FF14.6", ResourceHelper.GetMessage("RegView_Common_LengthCounterEnabled"), sq1.LengthEnabled),
			new RegEntry("$FF14.7", ResourceHelper.GetMessage("RegView_Common_ChannelEnabled"), sq1.Enabled),

			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_Timer"), sq1.Timer),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_DutyPosition"), sq1.DutyPos),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_SweepEnabled"), sq1.SweepEnabled),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_SweepFrequency"), sq1.SweepFreq),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_SweepTimer"), sq1.SweepTimer),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_EnvelopeTimer"), sq1.EnvTimer),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_Output"), sq1.Output)
		});

		GbSquareState sq2 = gb.Apu.Square2;
		entries.AddRange(new List<RegEntry>() {
			new RegEntry("$FF16-$FF19", ResourceHelper.GetMessage("RegView_Common_Square2")),
			new RegEntry("$FF16.0-5", ResourceHelper.GetMessage("RegView_Common_Length"), sq2.Length),
			new RegEntry("$FF16.6-7", ResourceHelper.GetMessage("RegView_Common_Duty"), sq2.Duty),

			new RegEntry("$FF17.0-2", ResourceHelper.GetMessage("RegView_Common_EnvelopePeriod"), sq2.EnvPeriod),
			new RegEntry("$FF17.3", ResourceHelper.GetMessage("RegView_Common_EnvelopeIncreaseVolume"), sq2.EnvRaiseVolume),
			new RegEntry("$FF17.4-7", ResourceHelper.GetMessage("RegView_Common_EnvelopeVolume"), sq2.EnvVolume),

			new RegEntry("$FF18-$FF19.0-2", ResourceHelper.GetMessage("RegView_Common_Frequency"), sq2.Frequency),
			new RegEntry("$FF19.6", ResourceHelper.GetMessage("RegView_Common_LengthCounterEnabled"), sq2.LengthEnabled),
			new RegEntry("$FF19.7", ResourceHelper.GetMessage("RegView_Common_ChannelEnabled"), sq2.Enabled),

			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_Timer"), sq2.Timer),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_DutyPosition"), sq2.DutyPos),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_EnvelopeTimer"), sq2.EnvTimer),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_Output"), sq2.Output)
		});

		GbWaveState wave = gb.Apu.Wave;
		entries.AddRange(new List<RegEntry>() {
			new RegEntry("$FF1A-$FF1E", ResourceHelper.GetMessage("RegView_Common_Wave")),
			new RegEntry("$FF1A.7", ResourceHelper.GetMessage("RegView_Gb_SoundEnabled"), wave.DacEnabled),

			new RegEntry("$FF1B", ResourceHelper.GetMessage("RegView_Common_Length"), wave.Length),

			new RegEntry("$FF1C.5-6", ResourceHelper.GetMessage("RegView_Common_Volume"), wave.Volume),

			new RegEntry("$FF1D-$FF1E.0-2", ResourceHelper.GetMessage("RegView_Common_Frequency"), wave.Frequency),

			new RegEntry("$FF1E.6", ResourceHelper.GetMessage("RegView_Common_LengthCounterEnabled"), wave.LengthEnabled),
			new RegEntry("$FF1E.7", ResourceHelper.GetMessage("RegView_Common_ChannelEnabled"), wave.Enabled),

			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_Timer"), wave.Timer),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Gb_SampleBuffer"), wave.SampleBuffer),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Gb_Position"), wave.Position),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_Output"), wave.Output),
		});

		GbNoiseState noise = gb.Apu.Noise;
		entries.AddRange(new List<RegEntry>() {
			new RegEntry("$FF20-$FF23", ResourceHelper.GetMessage("RegView_Common_Noise")),
			new RegEntry("$FF20.0-5", ResourceHelper.GetMessage("RegView_Common_Length"), noise.Length),

			new RegEntry("$FF21.0-2", ResourceHelper.GetMessage("RegView_Common_EnvelopePeriod"), noise.EnvPeriod),
			new RegEntry("$FF21.3", ResourceHelper.GetMessage("RegView_Common_EnvelopeIncreaseVolume"), noise.EnvRaiseVolume),
			new RegEntry("$FF21.4-7", ResourceHelper.GetMessage("RegView_Common_EnvelopeVolume"), noise.EnvVolume),

			new RegEntry("$FF23.0-2", ResourceHelper.GetMessage("RegView_Common_Divisor"), noise.Divisor),
			new RegEntry("$FF23.3", ResourceHelper.GetMessage("RegView_Common_ShortMode"), noise.ShortWidthMode),
			new RegEntry("$FF23.4-7", ResourceHelper.GetMessage("RegView_Common_PeriodShift"), noise.PeriodShift),

			new RegEntry("$FF24.6", ResourceHelper.GetMessage("RegView_Common_LengthCounterEnabled"), noise.LengthEnabled),
			new RegEntry("$FF24.7", ResourceHelper.GetMessage("RegView_Common_ChannelEnabled"), noise.Enabled),

			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_Timer"), noise.Timer),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_EnvelopeTimer"), noise.EnvTimer),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_ShiftRegister"), noise.ShiftRegister, Format.X16),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_Output"), noise.Output)
		});

		return new RegisterViewerTab(tabPrefix + ResourceHelper.GetMessage("RegView_Gb_APU"), entries, CpuType.Gameboy, MemoryType.GameboyMemory);
	}
}
