using Mesen.Debugger.ViewModels;
using Mesen.Interop;
using Mesen.Localization;
using System;
using System.Collections.Generic;
using static Mesen.Debugger.ViewModels.RegEntry;

namespace Mesen.Debugger.RegisterViewer;

public class NesRegisterViewer
{
	public static List<RegisterViewerTab> GetTabs(ref NesState nesState)
	{
		List<RegisterViewerTab> tabs = new() {
			GetNesPpuTab(ref nesState),
			GetNesApuTab(ref nesState)
		};

		RegisterViewerTab cartTab = GetNesCartTab(ref nesState);
		if(cartTab.Data.Count > 0) {
			tabs.Add(cartTab);
		}

		return tabs;
	}

	private static RegisterViewerTab GetNesPpuTab(ref NesState state)
	{
		NesPpuState ppu = state.Ppu;

		List<RegEntry> entries = new List<RegEntry>() {
			new RegEntry("", ResourceHelper.GetMessage("RegView_Common_State")),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Common_CycleH"), ppu.Cycle),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Common_ScanlineV"), ppu.Scanline),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Common_FrameNumber"), ppu.FrameCount),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Nes_PPUBusAddress"), ppu.BusAddress, Format.X16),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Nes_PPURegisterBuffer"), ppu.MemoryReadBuffer, Format.X8),

			new RegEntry("$2000", ResourceHelper.GetMessage("RegView_Common_Control")),
			new RegEntry("$2000.2", ResourceHelper.GetMessage("RegView_Nes_IncrementMode"), ppu.Control.VerticalWrite ? ResourceHelper.GetMessage("RegView_Nes_32Bytes") : ResourceHelper.GetMessage("RegView_Nes_1Byte"), ppu.Control.VerticalWrite),
			new RegEntry("$2000.3", ResourceHelper.GetMessage("RegView_Nes_SpriteTableAddress"), ppu.Control.SpritePatternAddr == 0 ? "$0000" : "$1000", ppu.Control.SpritePatternAddr),
			new RegEntry("$2000.4", ResourceHelper.GetMessage("RegView_Nes_BGTableAddress"), ppu.Control.BackgroundPatternAddr == 0 ? "$0000" : "$1000", ppu.Control.BackgroundPatternAddr),
			new RegEntry("$2000.5", ResourceHelper.GetMessage("RegView_Nes_SpritesSize"), ppu.Control.LargeSprites ? ResourceHelper.GetMessage("RegView_Common_8x16") : ResourceHelper.GetMessage("RegView_Common_8x8"), ppu.Control.LargeSprites),
			new RegEntry("$2000.6", ResourceHelper.GetMessage("RegView_Nes_MainSecondaryPPUSelect"), ppu.Control.SecondaryPpu ? ResourceHelper.GetMessage("RegView_Nes_Secondary") : ResourceHelper.GetMessage("RegView_Nes_Main"), ppu.Control.SecondaryPpu),
			new RegEntry("$2000.7", ResourceHelper.GetMessage("RegView_Nes_NMIEnabled"), ppu.Control.NmiOnVerticalBlank),

			new RegEntry("$2001", ResourceHelper.GetMessage("RegView_Nes_Mask")),
			new RegEntry("$2001.0", ResourceHelper.GetMessage("RegView_Nes_Grayscale"), ppu.Mask.Grayscale),
			new RegEntry("$2001.1", ResourceHelper.GetMessage("RegView_Nes_BG_ShowLeftmost8Pixels"), ppu.Mask.BackgroundMask),
			new RegEntry("$2001.2", ResourceHelper.GetMessage("RegView_Nes_Sprites_ShowLeftmost8Pixels"), ppu.Mask.SpriteMask),
			new RegEntry("$2001.3", ResourceHelper.GetMessage("RegView_Nes_BackgroundEnabled"), ppu.Mask.BackgroundEnabled),
			new RegEntry("$2001.4", ResourceHelper.GetMessage("RegView_Nes_SpritesEnabled"), ppu.Mask.SpritesEnabled),
			new RegEntry("$2001.5", ResourceHelper.GetMessage("RegView_Nes_RedEmphasis"), ppu.Mask.IntensifyRed),
			new RegEntry("$2001.6", ResourceHelper.GetMessage("RegView_Nes_GreenEmphasis"), ppu.Mask.IntensifyGreen),
			new RegEntry("$2001.7", ResourceHelper.GetMessage("RegView_Nes_BlueEmphasis"), ppu.Mask.IntensifyBlue),

			new RegEntry("$2002", ResourceHelper.GetMessage("RegView_Common_Status")),
			new RegEntry("$2002.5", ResourceHelper.GetMessage("RegView_Nes_SpriteOverflow"), ppu.StatusFlags.SpriteOverflow),
			new RegEntry("$2002.6", ResourceHelper.GetMessage("RegView_Nes_Sprite0Hit"), ppu.StatusFlags.Sprite0Hit),
			new RegEntry("$2002.7", ResourceHelper.GetMessage("RegView_Nes_VerticalBlank"), ppu.StatusFlags.VerticalBlank),

			new RegEntry("$2003", ResourceHelper.GetMessage("RegView_Nes_OAM1Address"), ppu.SpriteRamAddr, Format.X8),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Nes_OAM2Address"), ppu.SecondaryOamAddr & 0x1F, Format.X8),

			new RegEntry("$2005-2006", ResourceHelper.GetMessage("RegView_Nes_VRAMAddressScrolling")),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Nes_VRAMAddress"), ppu.VideoRamAddr, Format.X16),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Nes_T"), ppu.TmpVideoRamAddr, Format.X16),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Nes_XScroll"), ppu.ScrollX),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Nes_WriteToggle"), ppu.WriteToggle)
		};

		return new RegisterViewerTab(ResourceHelper.GetMessage("RegView_Nes_PPU"), entries, CpuType.Nes, MemoryType.NesMemory);
	}

	private static RegisterViewerTab GetNesApuTab(ref NesState state)
	{
		List<RegEntry> entries = new List<RegEntry>();
		NesApuState apu = state.Apu;

		NesApuSquareState sq1 = apu.Square1;
		entries.AddRange(new List<RegEntry>() {
			new RegEntry("$4000-$4003", ResourceHelper.GetMessage("RegView_Common_Square1")),
			new RegEntry("$4000.0-3", ResourceHelper.GetMessage("RegView_Common_EnvelopeVolume"), sq1.Envelope.Volume, Format.X8),
			new RegEntry("$4000.4", ResourceHelper.GetMessage("RegView_Nes_Envelope_ConstantVolume"), sq1.Envelope.ConstantVolume),
			new RegEntry("$4000.5", ResourceHelper.GetMessage("RegView_Nes_LengthCounter_Halted"), sq1.LengthCounter.Halt),
			new RegEntry("$4000.6-7", ResourceHelper.GetMessage("RegView_Common_Duty"), sq1.Duty),

			new RegEntry("$4001.0-2", ResourceHelper.GetMessage("RegView_Nes_Sweep_Shift"), sq1.SweepShift),
			new RegEntry("$4001.3", ResourceHelper.GetMessage("RegView_Nes_Sweep_Negate"), sq1.SweepNegate),
			new RegEntry("$4001.4-6", ResourceHelper.GetMessage("RegView_Nes_Sweep_Period"), sq1.SweepPeriod),
			new RegEntry("$4001.7", ResourceHelper.GetMessage("RegView_Nes_Sweep_Enabled"), sq1.SweepEnabled),

			new RegEntry("$4002/$4003.0-2", "Period", sq1.Period, Format.X16),
			new RegEntry("$4003.3-7", ResourceHelper.GetMessage("RegView_Nes_LengthCounter_ReloadValue"), sq1.LengthCounter.ReloadValue, Format.X16),

			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_Enabled"), sq1.Enabled),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_Timer"), sq1.Timer, Format.X16),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_Frequency"), Math.Round(sq1.Frequency).ToString("0.") + ResourceHelper.GetMessage("RegView_Common_HzSuffix"), null),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_DutyPosition"), sq1.DutyPosition),

			new RegEntry("--", ResourceHelper.GetMessage("RegView_Nes_LengthCounter_Counter"), sq1.LengthCounter.Counter, Format.X8),

			new RegEntry("--", ResourceHelper.GetMessage("RegView_Nes_Envelope_Counter"), sq1.Envelope.Counter, Format.X8),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Nes_Envelope_Divider"), sq1.Envelope.Divider, Format.X8),

			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_Output"), sq1.OutputVolume, Format.X8),
		});

		NesApuSquareState sq2 = apu.Square2;
		entries.AddRange(new List<RegEntry>() {
			new RegEntry("$4004-$4007", ResourceHelper.GetMessage("RegView_Common_Square2")),
			new RegEntry("$4004.0-3", ResourceHelper.GetMessage("RegView_Common_EnvelopeVolume"), sq2.Envelope.Volume, Format.X8),
			new RegEntry("$4004.4", ResourceHelper.GetMessage("RegView_Nes_Envelope_ConstantVolume"), sq2.Envelope.ConstantVolume),
			new RegEntry("$4004.5", ResourceHelper.GetMessage("RegView_Nes_LengthCounter_Halted"), sq2.LengthCounter.Halt),
			new RegEntry("$4004.6-7", ResourceHelper.GetMessage("RegView_Common_Duty"), sq2.Duty),

			new RegEntry("$4005.0-2", ResourceHelper.GetMessage("RegView_Nes_Sweep_Shift"), sq2.SweepShift),
			new RegEntry("$4005.3", ResourceHelper.GetMessage("RegView_Nes_Sweep_Negate"), sq2.SweepNegate),
			new RegEntry("$4005.4-6", ResourceHelper.GetMessage("RegView_Nes_Sweep_Period"), sq2.SweepPeriod),
			new RegEntry("$4005.7", ResourceHelper.GetMessage("RegView_Nes_Sweep_Enabled"), sq2.SweepEnabled),

			new RegEntry("$4006/$4007.0-2", "Period", sq2.Period, Format.X16),
			new RegEntry("$4007.3-7", ResourceHelper.GetMessage("RegView_Nes_LengthCounter_ReloadValue"), sq2.LengthCounter.ReloadValue, Format.X16),

			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_Enabled"), sq2.Enabled),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_Timer"), sq2.Timer, Format.X16),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_Frequency"), Math.Round(sq2.Frequency).ToString("0.") + ResourceHelper.GetMessage("RegView_Common_HzSuffix"), null),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_DutyPosition"), sq2.DutyPosition),

			new RegEntry("--", ResourceHelper.GetMessage("RegView_Nes_LengthCounter_Counter"), sq2.LengthCounter.Counter, Format.X8),

			new RegEntry("--", ResourceHelper.GetMessage("RegView_Nes_Envelope_Counter"), sq2.Envelope.Counter, Format.X8),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Nes_Envelope_Divider"), sq2.Envelope.Divider, Format.X8),

			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_Output"), sq2.OutputVolume, Format.X8),
		});

		NesApuTriangleState trg = apu.Triangle;
		entries.AddRange(new List<RegEntry>() {
			new RegEntry("$4008-$400B", "Triangle"),
			new RegEntry("$4008.0-6", ResourceHelper.GetMessage("RegView_Nes_LinearCounter_Reload"), trg.LinearCounterReload, Format.X8),
			new RegEntry("$4008.7", ResourceHelper.GetMessage("RegView_Nes_LengthCounter_Halted"), trg.LengthCounter.Halt),

			new RegEntry("$400A/$400B.0-2", "Period", trg.Period, Format.X16),
			new RegEntry("$400B.3-7", ResourceHelper.GetMessage("RegView_Nes_LengthCounter_ReloadValue"), trg.LengthCounter.ReloadValue, Format.X16),

			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_Enabled"), trg.Enabled),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_Timer"), trg.Timer),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_Frequency"), Math.Round(trg.Frequency).ToString("0.") + ResourceHelper.GetMessage("RegView_Common_HzSuffix"), null),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Nes_SequencePosition"), trg.SequencePosition),

			new RegEntry("--", ResourceHelper.GetMessage("RegView_Nes_LengthCounter_Counter"), trg.LengthCounter.Counter),

			new RegEntry("--", ResourceHelper.GetMessage("RegView_Nes_LinearCounter_Counter"), trg.LinearCounter),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Nes_LinearCounter_ReloadFlag"), trg.LinearReloadFlag),

			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_Output"), trg.OutputVolume),
		});

		NesApuNoiseState noise = apu.Noise;
		entries.AddRange(new List<RegEntry>() {
			new RegEntry("$400C-$400F", ResourceHelper.GetMessage("RegView_Common_Noise")),
			new RegEntry("$400C.0-3", ResourceHelper.GetMessage("RegView_Common_EnvelopeVolume"), noise.Envelope.Volume, Format.X8),
			new RegEntry("$400C.4", ResourceHelper.GetMessage("RegView_Nes_Envelope_ConstantVolume"), noise.Envelope.ConstantVolume),
			new RegEntry("$400C.5", ResourceHelper.GetMessage("RegView_Nes_LengthCounter_Halted"), noise.LengthCounter.Halt),

			new RegEntry("$400E.0-3", "Period", noise.Period, Format.X16),
			new RegEntry("$400E.7", ResourceHelper.GetMessage("RegView_Nes_ModeFlag"), noise.ModeFlag),

			new RegEntry("$400F.3-7", ResourceHelper.GetMessage("RegView_Nes_LengthCounter_ReloadValue"), noise.LengthCounter.ReloadValue, Format.X8),

			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_Enabled"), noise.Enabled),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_Timer"), noise.Timer),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_Frequency"), Math.Round(noise.Frequency).ToString("0.") + ResourceHelper.GetMessage("RegView_Common_HzSuffix"), null),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_ShiftRegister"), noise.ShiftRegister),

			new RegEntry("--", ResourceHelper.GetMessage("RegView_Nes_Envelope_Counter"), noise.Envelope.Counter, Format.X8),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Nes_Envelope_Divider"), noise.Envelope.Divider, Format.X8),

			new RegEntry("--", ResourceHelper.GetMessage("RegView_Nes_LengthCounter_Counter"), noise.LengthCounter.Counter),

			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_Output"), noise.OutputVolume),
		});

		NesApuDmcState dmc = apu.Dmc;
		entries.AddRange(new List<RegEntry>() {
			new RegEntry("$4010-4013", ResourceHelper.GetMessage("RegView_Nes_DMC")),
			new RegEntry("$4010.0-3", "Period", dmc.Period, Format.X16),
			new RegEntry("$4010.6", ResourceHelper.GetMessage("RegView_Nes_LoopFlag"), dmc.Loop),
			new RegEntry("$4010.7", ResourceHelper.GetMessage("RegView_Nes_IRQEnabled"), dmc.IrqEnabled),

			new RegEntry("$4011", ResourceHelper.GetMessage("RegView_Nes_OutputLevel"), dmc.OutputVolume),

			new RegEntry("$4012", ResourceHelper.GetMessage("RegView_Nes_SampleAddress"), dmc.SampleAddr, Format.X16),
			new RegEntry("$4013", ResourceHelper.GetMessage("RegView_Nes_SampleLength"), dmc.SampleLength, Format.X16),

			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_Timer"), dmc.Timer),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Common_Frequency"), Math.Round(dmc.SampleRate).ToString("0."), null),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Nes_BytesRemaining"), dmc.BytesRemaining),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Nes_NextSampleAddress"), dmc.NextSampleAddr, Format.X16),
		});

		NesApuFrameCounterState frameCounter = apu.FrameCounter;
		entries.AddRange(new List<RegEntry>() {
			new RegEntry("$4017", ResourceHelper.GetMessage("RegView_Nes_FrameCounter")),
			new RegEntry("$4017.6", ResourceHelper.GetMessage("RegView_Nes_IRQEnabled"), frameCounter.IrqEnabled),
			new RegEntry("$4017.7", ResourceHelper.GetMessage("RegView_Nes_5StepMode"), frameCounter.FiveStepMode),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Nes_SequencePosition"), frameCounter.SequencePosition),
		});

		return new RegisterViewerTab(ResourceHelper.GetMessage("RegView_Nes_APU"), entries, CpuType.Nes, MemoryType.NesMemory);
	}

	private static RegisterViewerTab GetNesCartTab(ref NesState state)
	{
		NesCartridgeState cart = state.Cartridge;

		List<RegEntry> entries = new List<RegEntry>();
		for(int i = 0; i < cart.CustomEntryCount; i++) {
			ref MapperStateEntry entry = ref cart.CustomEntries[i];
			Format format = entry.Type switch {
				MapperStateValueType.Number8 => Format.X8,
				MapperStateValueType.Number16 => Format.X16,
				MapperStateValueType.Number32 => Format.X32,
				_ => Format.None
			};

			object? value = entry.GetValue();
			string addr = entry.GetAddress();
			string name = entry.GetName();

			if(value is ISpanFormattable) {
				entries.Add(new RegEntry(addr, name, (ISpanFormattable)value, format));
			} else if(value is bool) {
				entries.Add(new RegEntry(addr, name, (bool)value));
			} else if(value is string) {
				entries.Add(new RegEntry(addr, name, (string)value, entry.RawValue != Int64.MinValue ? entry.RawValue : null));
			} else {
				entries.Add(new RegEntry(addr, name));
			}
		}

		return new RegisterViewerTab(ResourceHelper.GetMessage("RegView_Nes_Cart"), entries, CpuType.Nes, MemoryType.NesMemory);
	}
}
