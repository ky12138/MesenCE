using Mesen.Config;
using Mesen.Debugger.ViewModels;
using Mesen.Interop;
using Mesen.Localization;
using System;
using System.Collections.Generic;
using static Mesen.Debugger.ViewModels.RegEntry;

namespace Mesen.Debugger.RegisterViewer;

public class WsRegisterViewer
{
	public static List<RegisterViewerTab> GetTabs(ref WsState wsState)
	{
		List<RegisterViewerTab> tabs = new() {
			GetPpuTab(ref wsState),
			GetApuTab(ref wsState),
			GetCartTab(ref wsState),
			GetDmaTab(ref wsState),
			GetIrqTab(ref wsState),
			GetTimerTab(ref wsState),
			GetMiscTab(ref wsState),
		};

		return tabs;
	}

	private static RegisterViewerTab GetPpuTab(ref WsState ws)
	{
		List<RegEntry> entries = new List<RegEntry>();
		WsPpuState ppu = ws.Ppu;

		byte volumeLevel = 0;
		if(ws.Model == WsModel.Monochrome || ws.Model == WsModel.PocketChallenge) {
			switch(ws.Apu.InternalMasterVolume) {
				default: case 0: volumeLevel = 0; break;
				case 1: volumeLevel = 2; break;
				case 2: volumeLevel = 3; break;
			}
		} else {
			switch(ws.Apu.InternalMasterVolume) {
				default: case 0: volumeLevel = 0; break;
				case 1: volumeLevel = 2; break;
				case 2: volumeLevel = 1; break;
				case 3: volumeLevel = 3; break;
			}
		}

		bool headphoneIconVisible = false;
		bool volumeIconVisible = false;
		if(ppu.ShowVolumeIconFrame <= ppu.FrameCount && ppu.FrameCount - ppu.ShowVolumeIconFrame < 128) {
			//Show speaker/headphone icons if sound button was pressed within the last 128 frames
			if(ConfigManager.Config.Ws.AudioMode == WsAudioMode.Headphones) {
				headphoneIconVisible = true;
			} else {
				volumeIconVisible = true;
			}
		}

		entries.AddRange(new List<RegEntry>() {
			new RegEntry("", ResourceHelper.GetMessage("RegView_Common_State")),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Common_CycleH"), ppu.Cycle),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Common_ScanlineV"), ppu.Scanline),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Common_FrameNumber"), ppu.FrameCount),

			new RegEntry("", ResourceHelper.GetMessage("RegView_Common_Ports")),
			new RegEntry("$00.0", ResourceHelper.GetMessage("RegView_Ws_Screen1Enabled"), ppu.BgLayers[0].Enabled),
			new RegEntry("$00.1", ResourceHelper.GetMessage("RegView_Ws_Screen2Enabled"), ppu.BgLayers[1].Enabled),
			new RegEntry("$00.2", ResourceHelper.GetMessage("RegView_Common_SpritesEnabled"), ppu.SpritesEnabled),
			new RegEntry("$00.3", ResourceHelper.GetMessage("RegView_Ws_SpriteWindowEnabled"), ppu.SpriteWindow.Enabled),
			new RegEntry("$00.4", ResourceHelper.GetMessage("RegView_Ws_Screen2WindowDrawOutside"), ppu.DrawOutsideBgWindow),
			new RegEntry("$00.5", ResourceHelper.GetMessage("RegView_Ws_Screen2WindowEnabled"), ppu.BgWindow.Enabled),

			new RegEntry("$01", ResourceHelper.GetMessage("RegView_Ws_BackgroundColorMono"), ppu.BgColor & 0x07),
			new RegEntry("$01", ResourceHelper.GetMessage("RegView_Ws_BackgroundColorWSC"), ppu.BgColor & 0x0F),
			new RegEntry("$01", ResourceHelper.GetMessage("RegView_Ws_BackgroundColorPaletteWSC"), (ppu.BgColor >> 4) & 0x0F),
			new RegEntry("$02", ResourceHelper.GetMessage("RegView_Ws_Scanline"), ppu.Scanline, Format.X8),
			new RegEntry("$03", ResourceHelper.GetMessage("RegView_Ws_IRQScanline"), ppu.IrqScanline, Format.X8),
			new RegEntry("$04", ResourceHelper.GetMessage("RegView_Ws_SpriteTableAddress"), "$" + ppu.SpriteTableAddress.ToString("X4"), ppu.SpriteTableAddress >> 9),
			new RegEntry("$05.0-6", ResourceHelper.GetMessage("RegView_Ws_FirstSpriteIndex"), ppu.FirstSpriteIndex, Format.X8),
			new RegEntry("$06", ResourceHelper.GetMessage("RegView_Ws_SpriteCount"), ppu.SpriteCount, Format.X8),
			new RegEntry("$07.0-3", ResourceHelper.GetMessage("RegView_Ws_Screen1Address"), "$" + ppu.BgLayers[0].MapAddress.ToString("X4"), ppu.BgLayers[0].MapAddress >> 11),
			new RegEntry("$07.4-7", ResourceHelper.GetMessage("RegView_Ws_Screen2Address"), "$" + ppu.BgLayers[1].MapAddress.ToString("X4"), ppu.BgLayers[1].MapAddress >> 11),
			new RegEntry("$08", ResourceHelper.GetMessage("RegView_Ws_Screen2WindowLeft"), ppu.BgWindow.Left, Format.X8),
			new RegEntry("$09", ResourceHelper.GetMessage("RegView_Ws_Screen2WindowTop"), ppu.BgWindow.Top, Format.X8),
			new RegEntry("$0A", ResourceHelper.GetMessage("RegView_Ws_Screen2WindowRight"), ppu.BgWindow.Right, Format.X8),
			new RegEntry("$0B", ResourceHelper.GetMessage("RegView_Ws_Screen2WindowBottom"), ppu.BgWindow.Bottom, Format.X8),
			new RegEntry("$0C", ResourceHelper.GetMessage("RegView_Ws_SpriteWindowLeft"), ppu.SpriteWindow.Left, Format.X8),
			new RegEntry("$0D", ResourceHelper.GetMessage("RegView_Ws_SpriteWindowTop"), ppu.SpriteWindow.Top, Format.X8),
			new RegEntry("$0E", ResourceHelper.GetMessage("RegView_Ws_SpriteWindowRight"), ppu.SpriteWindow.Right, Format.X8),
			new RegEntry("$0F", ResourceHelper.GetMessage("RegView_Ws_SpriteWindowBottom"), ppu.SpriteWindow.Bottom, Format.X8),

			new RegEntry("$10", ResourceHelper.GetMessage("RegView_Ws_Screen1ScrollX"), ppu.BgLayers[0].ScrollX, Format.X8),
			new RegEntry("$11", ResourceHelper.GetMessage("RegView_Ws_Screen1ScrollY"), ppu.BgLayers[0].ScrollY, Format.X8),
			new RegEntry("$12", ResourceHelper.GetMessage("RegView_Ws_Screen2ScrollX"), ppu.BgLayers[1].ScrollX, Format.X8),
			new RegEntry("$13", ResourceHelper.GetMessage("RegView_Ws_Screen2ScrollY"), ppu.BgLayers[1].ScrollY, Format.X8),

			new RegEntry("$14.0", ResourceHelper.GetMessage("RegView_Common_LCDEnabled"), ppu.LcdEnabled),
			new RegEntry("$14.1", ResourceHelper.GetMessage("RegView_Ws_LCDHighContrast"), ppu.HighContrast),

			new RegEntry("$16", ResourceHelper.GetMessage("RegView_Ws_LastScanline"), ppu.LastScanline, Format.X8),
			new RegEntry("$17", ResourceHelper.GetMessage("RegView_Ws_BackPorchScanline"), ppu.BackPorchScanline, Format.X8),

			new RegEntry("$1A.0", ResourceHelper.GetMessage("RegView_Ws_LCDSleepMode"), ppu.SleepEnabled),
			new RegEntry("$1A.1", ResourceHelper.GetMessage("RegView_Ws_HeadphoneIconVisible"), headphoneIconVisible),
			new RegEntry("$1A.2-3", ResourceHelper.GetMessage("RegView_Ws_VolumeLevel"), volumeLevel),
			new RegEntry("$1A.4", ResourceHelper.GetMessage("RegView_Ws_VolumeIconVisible"), volumeIconVisible),

			new RegEntry("$15", ResourceHelper.GetMessage("RegView_Ws_Icons")),
			new RegEntry("$15.0", ResourceHelper.GetMessage("RegView_Ws_Sleep"), ppu.Icons.Sleep),
			new RegEntry("$15.1", ResourceHelper.GetMessage("RegView_Ws_Vertical"), ppu.Icons.Vertical),
			new RegEntry("$15.2", ResourceHelper.GetMessage("RegView_Ws_Horizontal"), ppu.Icons.Horizontal),
			new RegEntry("$15.3", ResourceHelper.GetMessage("RegView_Ws_Aux1"), ppu.Icons.Aux1),
			new RegEntry("$15.4", ResourceHelper.GetMessage("RegView_Ws_Aux2"), ppu.Icons.Aux2),
			new RegEntry("$15.5", ResourceHelper.GetMessage("RegView_Ws_Aux3"), ppu.Icons.Aux3),

			new RegEntry("$70-77", ResourceHelper.GetMessage("RegView_Ws_TFTLCDConfig")),
			new RegEntry("$70", "", ppu.LcdTftConfig[0], Format.X8),
			new RegEntry("$71", "", ppu.LcdTftConfig[1], Format.X8),
			new RegEntry("$72", "", ppu.LcdTftConfig[2], Format.X8),
			new RegEntry("$73", "", ppu.LcdTftConfig[3], Format.X8),
			new RegEntry("$74", "", ppu.LcdTftConfig[4], Format.X8),
			new RegEntry("$75", "", ppu.LcdTftConfig[5], Format.X8),
			new RegEntry("$76", "", ppu.LcdTftConfig[6], Format.X8),
			new RegEntry("$77", "", ppu.LcdTftConfig[7], Format.X8),
		});

		return new RegisterViewerTab(ResourceHelper.GetMessage("RegView_Ws_PPU"), entries, CpuType.Ws, MemoryType.WsPort);
	}

	private static RegisterViewerTab GetApuTab(ref WsState ws)
	{
		List<RegEntry> entries = new List<RegEntry>();

		WsApuState apu = ws.Apu;

		int rightOutput = (
			apu.Ch1.RightOutput +
			apu.Ch2.RightOutput +
			apu.Ch3.RightOutput +
			apu.Ch4.RightOutput
		);

		int leftOutput = (
			apu.Ch1.LeftOutput +
			apu.Ch2.LeftOutput +
			apu.Ch3.LeftOutput +
			apu.Ch4.LeftOutput
		);

		entries.AddRange(new List<RegEntry>() {
			new RegEntry("$8F", ResourceHelper.GetMessage("RegView_Ws_WaveTableAddress"), apu.WaveTableAddress, Format.X16),
			new RegEntry("$91", ResourceHelper.GetMessage("RegView_Ws_SoundOutputControl")),
			new RegEntry("$91.0", ResourceHelper.GetMessage("RegView_Ws_SpeakerEnabled"), apu.SpeakerEnabled),
			new RegEntry("$91.1-2", ResourceHelper.GetMessage("RegView_Ws_SpeakerVolume"), apu.SpeakerVolume switch {
				0 => ResourceHelper.GetMessage("RegView_Ws_100Percent"), 1 => ResourceHelper.GetMessage("RegView_Ws_50Percent"), 2 => ResourceHelper.GetMessage("RegView_Ws_25Percent"), 3 or _ => ResourceHelper.GetMessage("RegView_Ws_12_5Percent")
			}, apu.SpeakerVolume),
			new RegEntry("$91.3", ResourceHelper.GetMessage("RegView_Ws_HeadphonesEnabled"), apu.HeadphoneEnabled),
			new RegEntry("$91.7", ResourceHelper.GetMessage("RegView_Ws_HeadphonesConnected"), ConfigManager.Config.Ws.AudioMode == WsAudioMode.Headphones),
			new RegEntry("$9E.0-1", ResourceHelper.GetMessage("RegView_Ws_MasterVolume"), apu.MasterVolume),

			new RegEntry("", ResourceHelper.GetMessage("RegView_Ws_Channel1")),
			new RegEntry("$90.0", ResourceHelper.GetMessage("RegView_Ws_Enabled"), apu.Ch1.Enabled),
			new RegEntry("$80/1", ResourceHelper.GetMessage("RegView_Ws_FrequencyDivisor"), apu.Ch1.Frequency, Format.X16),
			new RegEntry("$88.0-3", ResourceHelper.GetMessage("RegView_Common_RightVolume"), apu.Ch1.RightVolume, Format.X8),
			new RegEntry("$88.4-7", ResourceHelper.GetMessage("RegView_Common_LeftVolume"), apu.Ch1.LeftVolume, Format.X8),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Ws_LeftOutput"), apu.Ch1.LeftOutput, Format.X8),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Ws_RightOutput"), apu.Ch1.RightOutput, Format.X8),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Common_Timer"), apu.Ch1.Timer, Format.X16),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Ws_SamplePosition"), apu.Ch1.SamplePosition, Format.X8),

			new RegEntry("", ResourceHelper.GetMessage("RegView_Ws_Channel2VoicePCM")),
			new RegEntry("$90.1", ResourceHelper.GetMessage("RegView_Ws_Enabled"), apu.Ch2.Enabled),
			new RegEntry("$82/3", ResourceHelper.GetMessage("RegView_Ws_FrequencyDivisor"), apu.Ch2.Frequency, Format.X16),
			new RegEntry("$89.0-3", ResourceHelper.GetMessage("RegView_Common_RightVolume"), apu.Ch2.RightVolume, Format.X8),
			new RegEntry("$89.4-7", ResourceHelper.GetMessage("RegView_Common_LeftVolume"), apu.Ch2.LeftVolume, Format.X8),
			new RegEntry("$90.5", ResourceHelper.GetMessage("RegView_Ws_PCMEnabled"), apu.Ch2.PcmEnabled),
			new RegEntry("$89", ResourceHelper.GetMessage("RegView_Ws_PCMValue"), (apu.Ch2.LeftVolume << 4) | apu.Ch2.RightVolume, Format.X8),
			new RegEntry("$94.0-1", ResourceHelper.GetMessage("RegView_Ws_PCMRightVolume"), apu.Ch2.MaxPcmVolumeRight ? ResourceHelper.GetMessage("RegView_Ws_100Percent") : (apu.Ch2.HalfPcmVolumeRight ? ResourceHelper.GetMessage("RegView_Ws_50Percent") : ResourceHelper.GetMessage("RegView_Ws_0Percent")), (apu.Ch2.MaxPcmVolumeRight ? 1 : 0) | (apu.Ch2.HalfPcmVolumeRight ? 2 : 0)),
			new RegEntry("$94.2-3", ResourceHelper.GetMessage("RegView_Ws_PCMLeftVolume"), apu.Ch2.MaxPcmVolumeLeft ? ResourceHelper.GetMessage("RegView_Ws_100Percent") : (apu.Ch2.HalfPcmVolumeLeft ? ResourceHelper.GetMessage("RegView_Ws_50Percent") : ResourceHelper.GetMessage("RegView_Ws_0Percent")), (apu.Ch2.MaxPcmVolumeLeft ? 1 : 0) | (apu.Ch2.HalfPcmVolumeLeft ? 2 : 0)),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Ws_LeftOutput"), apu.Ch2.LeftOutput, Format.X8),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Ws_RightOutput"), apu.Ch2.RightOutput, Format.X8),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Common_Timer"), apu.Ch2.Timer, Format.X16),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Ws_SamplePosition"), apu.Ch2.SamplePosition, Format.X8),

			new RegEntry("", ResourceHelper.GetMessage("RegView_Ws_Channel3Sweep")),
			new RegEntry("$90.2", ResourceHelper.GetMessage("RegView_Ws_Enabled"), apu.Ch3.Enabled),
			new RegEntry("$84/5", ResourceHelper.GetMessage("RegView_Ws_FrequencyDivisor"), apu.Ch3.Frequency, Format.X16),
			new RegEntry("$8A.0-3", ResourceHelper.GetMessage("RegView_Common_RightVolume"), apu.Ch3.RightVolume, Format.X8),
			new RegEntry("$8A.4-7", ResourceHelper.GetMessage("RegView_Common_LeftVolume"), apu.Ch3.LeftVolume, Format.X8),
			new RegEntry("$90.6", ResourceHelper.GetMessage("RegView_Ws_SweepEnabled"), apu.Ch3.SweepEnabled),
			new RegEntry("$8C", ResourceHelper.GetMessage("RegView_Ws_SweepValue"), apu.Ch3.SweepValue, Format.X8),
			new RegEntry("$8D.0-4", ResourceHelper.GetMessage("RegView_Ws_SweepPeriod"), apu.Ch3.SweepPeriod, Format.X8),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Ws_SweepTimer"), apu.Ch3.SweepTimer, Format.X8),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Ws_LeftOutput"), apu.Ch3.LeftOutput, Format.X8),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Ws_RightOutput"), apu.Ch3.RightOutput, Format.X8),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Common_Timer"), apu.Ch3.Timer, Format.X16),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Ws_SamplePosition"), apu.Ch3.SamplePosition, Format.X8),

			new RegEntry("", ResourceHelper.GetMessage("RegView_Ws_Channel4Noise")),
			new RegEntry("$90.3", ResourceHelper.GetMessage("RegView_Ws_Enabled"), apu.Ch4.Enabled),
			new RegEntry("$86/7", ResourceHelper.GetMessage("RegView_Ws_FrequencyDivisor"), apu.Ch4.Frequency, Format.X16),
			new RegEntry("$8B.0-3", ResourceHelper.GetMessage("RegView_Common_RightVolume"), apu.Ch4.RightVolume, Format.X8),
			new RegEntry("$8B.4-7", ResourceHelper.GetMessage("RegView_Common_LeftVolume"), apu.Ch4.LeftVolume, Format.X8),
			new RegEntry("$90.7", ResourceHelper.GetMessage("RegView_Ws_NoiseEnabled"), apu.Ch4.NoiseEnabled),
			new RegEntry("$8E.0-2", ResourceHelper.GetMessage("RegView_Ws_NoiseTapMode"), apu.Ch4.TapMode, Format.X8),
			new RegEntry("$8E.4", ResourceHelper.GetMessage("RegView_Ws_LFSREnabled"), apu.Ch4.LfsrEnabled),
			new RegEntry("$92/93.0-14", ResourceHelper.GetMessage("RegView_Ws_LFSRValue"), apu.Ch4.Lfsr),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Ws_LeftOutput"), apu.Ch4.LeftOutput, Format.X8),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Ws_RightOutput"), apu.Ch4.RightOutput, Format.X8),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Common_Timer"), apu.Ch4.Timer, Format.X16),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Ws_SamplePosition"), apu.Ch4.SamplePosition, Format.X8),

			new RegEntry("", ResourceHelper.GetMessage("RegView_Ws_HyperVoice")),
			new RegEntry("$64/65", ResourceHelper.GetMessage("RegView_Ws_LeftOutput"), apu.Voice.LeftOutput, Format.X16),
			new RegEntry("$66/67", ResourceHelper.GetMessage("RegView_Ws_RightOutput"), apu.Voice.RightOutput, Format.X16),
			new RegEntry("$69", ResourceHelper.GetMessage("RegView_Ws_LeftSample"), apu.Voice.LeftSample, Format.X8),
			new RegEntry("$69", ResourceHelper.GetMessage("RegView_Ws_RightSample"), apu.Voice.RightSample, Format.X8),
			new RegEntry("$6A.0-1", ResourceHelper.GetMessage("RegView_Common_Volume"), apu.Voice.Shift switch {
				0 => ResourceHelper.GetMessage("RegView_Ws_100Percent"), 1 => ResourceHelper.GetMessage("RegView_Ws_50Percent"), 2 => ResourceHelper.GetMessage("RegView_Ws_25Percent"), 3 or _ => ResourceHelper.GetMessage("RegView_Ws_12_5Percent")
			}, apu.Voice.Shift),
			new RegEntry("$6A.2-3", ResourceHelper.GetMessage("RegView_Ws_SampleScalingMode"), apu.Voice.ScalingMode),
			new RegEntry("$6A.4-6", ResourceHelper.GetMessage("RegView_Ws_UpdateSampleRate"), ((apu.Voice.ControlLow >> 4) & 0x07) switch {
				0 => ResourceHelper.GetMessage("RegView_Ws_24kHz"), 1 => ResourceHelper.GetMessage("RegView_Ws_12kHz"), 2 => ResourceHelper.GetMessage("RegView_Ws_8kHz"), 3 => ResourceHelper.GetMessage("RegView_Ws_6kHz"), 4 => ResourceHelper.GetMessage("RegView_Ws_4_8kHz"), 5 => ResourceHelper.GetMessage("RegView_Ws_4kHz"), 6 => ResourceHelper.GetMessage("RegView_Ws_3kHz"), 7 or _ => ResourceHelper.GetMessage("RegView_Ws_2kHz")
			}, (apu.Voice.ControlLow >> 4) & 0x07),
			new RegEntry("$6A.7", ResourceHelper.GetMessage("RegView_Ws_Enabled"), apu.Voice.Enabled),
			new RegEntry("$6B.13-14", ResourceHelper.GetMessage("RegView_Ws_ChannelMode"), apu.Voice.ChannelMode),

			new RegEntry("$95", ResourceHelper.GetMessage("RegView_Ws_SoundTest")),
			new RegEntry("$95.0", ResourceHelper.GetMessage("RegView_Ws_HoldChannels1_4"), apu.HoldChannels),
			new RegEntry("$95.1", ResourceHelper.GetMessage("RegView_Ws_UseCpuClockForSweep"), apu.Ch3.UseSweepCpuClock),
			new RegEntry("$95.2-3", ResourceHelper.GetMessage("RegView_Ws_HoldNoiseLFSR"), apu.Ch4.HoldLfsr),
			new RegEntry("$95.5", ResourceHelper.GetMessage("RegView_Ws_ForceOutputToCh2VoiceX5"), apu.ForceOutputCh2Voice),
			new RegEntry("$95.6", ResourceHelper.GetMessage("RegView_Ws_ForceChannels1_4OutputTo2"), apu.ForceOutput2),
			new RegEntry("$95.7", ResourceHelper.GetMessage("RegView_Ws_ForceChannels1_4OutputTo4"), apu.ForceOutput4),

			new RegEntry("", ResourceHelper.GetMessage("RegView_Ws_SoundOutput")),
			new RegEntry("$96/7", ResourceHelper.GetMessage("RegView_Ws_RightOutputSum"), rightOutput, Format.X16),
			new RegEntry("$98/9", ResourceHelper.GetMessage("RegView_Ws_LeftOutputSum"), leftOutput, Format.X16),
			new RegEntry("$99/A", ResourceHelper.GetMessage("RegView_Ws_OutputSum"), leftOutput+rightOutput, Format.X16),
		});

		return new RegisterViewerTab(ResourceHelper.GetMessage("RegView_Ws_APU"), entries, CpuType.Ws, MemoryType.WsPort);
	}

	private static RegisterViewerTab GetDmaTab(ref WsState ws)
	{
		List<RegEntry> entries = new List<RegEntry>();

		WsDmaControllerState dma = ws.DmaController;

		entries.AddRange(new List<RegEntry>() {
			new RegEntry("", ResourceHelper.GetMessage("RegView_Ws_GeneralDMA")),
			new RegEntry("$40/41/42", ResourceHelper.GetMessage("RegView_Common_Source"), dma.GdmaSrc),
			new RegEntry("$44/45", ResourceHelper.GetMessage("RegView_Common_Destination"), dma.GdmaDest),
			new RegEntry("$46/47", ResourceHelper.GetMessage("RegView_Common_Length"), dma.GdmaLength),

			new RegEntry("$48.6", ResourceHelper.GetMessage("RegView_Ws_Decrement"), (dma.GdmaControl & 0x40) != 0),
			new RegEntry("$48.7", ResourceHelper.GetMessage("RegView_Ws_Enabled"), (dma.GdmaControl & 0x80) != 0),

			new RegEntry("", ResourceHelper.GetMessage("RegView_Ws_SoundDMA")),
			new RegEntry("$4A/4B/4C", ResourceHelper.GetMessage("RegView_Common_Source"), dma.SdmaSrc),
			new RegEntry("$4E/4F/50", ResourceHelper.GetMessage("RegView_Common_Length"), dma.SdmaLength),
			new RegEntry("$52.0-1", ResourceHelper.GetMessage("RegView_Ws_Frequency"), (dma.SdmaControl & 0x03) switch {
				0 => ResourceHelper.GetMessage("RegView_Ws_4kHz"), 1 => ResourceHelper.GetMessage("RegView_Ws_6kHz"), 2 => ResourceHelper.GetMessage("RegView_Ws_12kHz"), 3 or _ => ResourceHelper.GetMessage("RegView_Ws_24kHz")
			}, dma.SdmaControl & 0x03),
			new RegEntry("$52.2", ResourceHelper.GetMessage("RegView_Ws_Hold"), dma.SdmaHold),
			new RegEntry("$52.3", ResourceHelper.GetMessage("RegView_Ws_AutoRepeat"), dma.SdmaHyperVoice),
			new RegEntry("$52.4", ResourceHelper.GetMessage("RegView_Ws_Target"), dma.SdmaHyperVoice ? ResourceHelper.GetMessage("RegView_Ws_HyperVoice") : ResourceHelper.GetMessage("RegView_Ws_Ch2PCM"), dma.SdmaHyperVoice ? 1 : 0),
			new RegEntry("$52.6", ResourceHelper.GetMessage("RegView_Ws_Decrement"), dma.SdmaDecrement),
			new RegEntry("$52.7", ResourceHelper.GetMessage("RegView_Ws_Enabled"), dma.SdmaEnabled),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Ws_SourceReloadValue"), dma.SdmaSrcReloadValue),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Ws_LengthReloadValue"), dma.SdmaLengthReloadValue)
		});

		return new RegisterViewerTab(ResourceHelper.GetMessage("RegView_Ws_DMA"), entries, CpuType.Ws, MemoryType.WsPort);
	}

	private static RegisterViewerTab GetTimerTab(ref WsState ws)
	{
		List<RegEntry> entries = new List<RegEntry>();

		WsTimerState timer = ws.Timer;
		entries.AddRange(new List<RegEntry>() {
			new RegEntry("$A2.0", ResourceHelper.GetMessage("RegView_Ws_HorizontalTimerEnabled"), timer.HBlankEnabled),
			new RegEntry("$A2.1", ResourceHelper.GetMessage("RegView_Ws_HorizontalTimerAutoReload"), timer.HBlankAutoReload),
			new RegEntry("$A2.2", ResourceHelper.GetMessage("RegView_Ws_VerticalTimerEnabled"), timer.VBlankEnabled),
			new RegEntry("$A2.3", ResourceHelper.GetMessage("RegView_Ws_VertitalTimerAutoReload"), timer.VBlankAutoReload),
			new RegEntry("$A4/5", ResourceHelper.GetMessage("RegView_Ws_HorizontalReloadValue"), timer.HReloadValue, Format.X16),
			new RegEntry("$A6/7", ResourceHelper.GetMessage("RegView_Ws_VerticalReloadValue"), timer.VReloadValue, Format.X16),
			new RegEntry("$A8/9", ResourceHelper.GetMessage("RegView_Ws_HorizontalTimer"), timer.HTimer, Format.X16),
			new RegEntry("$AA/B", ResourceHelper.GetMessage("RegView_Ws_VerticalTimer"), timer.VTimer, Format.X16),
		});

		return new RegisterViewerTab(ResourceHelper.GetMessage("RegView_Ws_Timer"), entries, CpuType.Ws, MemoryType.WsPort);
	}

	private static RegisterViewerTab GetIrqTab(ref WsState ws)
	{
		List<RegEntry> entries = new List<RegEntry>();

		WsMemoryManagerState mm = ws.MemoryManager;
		WsSerialState serial = ws.Serial;

		byte irqVector = mm.IrqVectorOffset;

		//TODOWS cleanup
		byte activeIrqs = mm.ActiveIrqs;
		if(serial.Enabled && (mm.EnabledIrqs & (int)WsIrqSource.UartSendReady) != 0) {
			bool hasSendData = serial.HasSendData;
			if(hasSendData) {
				int cyclesPerByte = serial.HighSpeed ? 800 : 3200;
				int cyclesElapsed = (int)(ws.Cpu.CycleCount - serial.SendClock);
				if(cyclesElapsed > cyclesPerByte) {
					hasSendData = false;
				}
			}

			if(!hasSendData) {
				activeIrqs |= (int)WsIrqSource.UartSendReady;
			}
		}

		for(int i = 7; i >= 0; i--) {
			if((activeIrqs & mm.EnabledIrqs & (1 << i)) != 0) {
				irqVector += (byte)i;
				break;
			}
		}

		entries.AddRange(new List<RegEntry>() {
			new RegEntry("", ResourceHelper.GetMessage("RegView_Ws_Interrupts")),
			new RegEntry("$B0.3-7", ResourceHelper.GetMessage("RegView_Ws_IRQVectorOffsetW"), mm.IrqVectorOffset),
			new RegEntry("$B0", ResourceHelper.GetMessage("RegView_Ws_ActiveIRQVectorR"), irqVector, Format.X8),
			new RegEntry("$B2", ResourceHelper.GetMessage("RegView_Ws_EnabledIRQs")),
			new RegEntry("$B2.0", ResourceHelper.GetMessage("RegView_Ws_UARTSendReady"), (mm.EnabledIrqs & (int)WsIrqSource.UartSendReady) != 0),
			new RegEntry("$B2.1", ResourceHelper.GetMessage("RegView_Ws_KeyPressed"), (mm.EnabledIrqs & (int)WsIrqSource.KeyPressed) != 0),
			new RegEntry("$B2.2", ResourceHelper.GetMessage("RegView_Ws_Cartridge"), (mm.EnabledIrqs & (int)WsIrqSource.Cart) != 0),
			new RegEntry("$B2.3", ResourceHelper.GetMessage("RegView_Ws_UARTReceiveReady"), (mm.EnabledIrqs & (int)WsIrqSource.UartRecvReady) != 0),
			new RegEntry("$B2.4", ResourceHelper.GetMessage("RegView_Ws_ScanlineIRQ"), (mm.EnabledIrqs & (int)WsIrqSource.Scanline) != 0),
			new RegEntry("$B2.5", ResourceHelper.GetMessage("RegView_Ws_VerticalTimerIRQ"), (mm.EnabledIrqs & (int)WsIrqSource.VerticalBlankTimer) != 0),
			new RegEntry("$B2.6", ResourceHelper.GetMessage("RegView_Ws_VerticalBlankIRQ"), (mm.EnabledIrqs & (int)WsIrqSource.VerticalBlank) != 0),
			new RegEntry("$B2.7", ResourceHelper.GetMessage("RegView_Ws_HorizontalTimerIRQ"), (mm.EnabledIrqs & (int)WsIrqSource.HorizontalBlankTimer) != 0),

			new RegEntry("$B4", ResourceHelper.GetMessage("RegView_Ws_ActiveIRQs")),
			new RegEntry("$B4.0", ResourceHelper.GetMessage("RegView_Ws_UARTSendReady"), (activeIrqs & (int)WsIrqSource.UartSendReady) != 0),
			new RegEntry("$B4.1", ResourceHelper.GetMessage("RegView_Ws_KeyPressed"), (activeIrqs & (int)WsIrqSource.KeyPressed) != 0),
			new RegEntry("$B4.2", ResourceHelper.GetMessage("RegView_Ws_Cartridge"), (activeIrqs & (int)WsIrqSource.Cart) != 0),
			new RegEntry("$B4.3", ResourceHelper.GetMessage("RegView_Ws_UARTReceiveReady"), (activeIrqs & (int)WsIrqSource.UartRecvReady) != 0),
			new RegEntry("$B4.4", ResourceHelper.GetMessage("RegView_Ws_ScanlineIRQ"), (activeIrqs & (int)WsIrqSource.Scanline) != 0),
			new RegEntry("$B4.5", ResourceHelper.GetMessage("RegView_Ws_VerticalTimerIRQ"), (activeIrqs & (int)WsIrqSource.VerticalBlankTimer) != 0),
			new RegEntry("$B4.6", ResourceHelper.GetMessage("RegView_Ws_VerticalBlankIRQ"), (activeIrqs & (int)WsIrqSource.VerticalBlank) != 0),
			new RegEntry("$B4.7", ResourceHelper.GetMessage("RegView_Ws_HorizontalTimerIRQ"), (activeIrqs & (int)WsIrqSource.HorizontalBlankTimer) != 0),

			new RegEntry("", ResourceHelper.GetMessage("RegView_Common_Misc")),
			new RegEntry("$B7.4", ResourceHelper.GetMessage("RegView_Ws_NMIOnLowBattery"), mm.EnableLowBatteryNmi),
		});

		return new RegisterViewerTab(ResourceHelper.GetMessage("RegView_Ws_IRQ"), entries, CpuType.Ws, MemoryType.WsPort);
	}

	private static RegisterViewerTab GetCartTab(ref WsState ws)
	{
		List<RegEntry> entries = new List<RegEntry>();

		WsCartState cart = ws.Cart;

		entries.AddRange(new List<RegEntry>() {
			new RegEntry("$C0", ResourceHelper.GetMessage("RegView_Ws_ROMLinearBank"), cart.SelectedBanks[0], Format.X8),
			new RegEntry("$C1", ResourceHelper.GetMessage("RegView_Ws_RAMBank"), cart.SelectedBanks[1], Format.X8),
			new RegEntry("$C2", ResourceHelper.GetMessage("RegView_Ws_ROM0Bank"), cart.SelectedBanks[2], Format.X8),
			new RegEntry("$C3", ResourceHelper.GetMessage("RegView_Ws_ROM1Bank"), cart.SelectedBanks[3], Format.X8),
		});

		if(cart.CartType == WsCartType.Bandai2003 || cart.CartType == WsCartType.WonderWitch) {
			entries.AddRange(new List<RegEntry>() {
				new RegEntry("$CE.0", "ROM in RAM Bank", cart.RomInRamBank),
				new RegEntry("$D0", "Extended RAM Bank", cart.ExtSelectedBanks[0], Format.X16),
				new RegEntry("$D2", "Extended ROM0 Bank", cart.ExtSelectedBanks[1], Format.X16),
				new RegEntry("$D4", "Extended ROM1 Bank", cart.ExtSelectedBanks[2], Format.X16)
			});
		}

		if(ws.CartEeprom.Size != WsEepromSize.Size0) {
			entries.AddRange(new List<RegEntry>() {
				new RegEntry("", ResourceHelper.GetMessage("RegView_Ws_CartEEPROM")),
				new RegEntry("$C4/C5", ResourceHelper.GetMessage("RegView_Ws_WriteData"), ws.CartEeprom.WriteBuffer, Format.X16),
				new RegEntry("$C4/C5", ResourceHelper.GetMessage("RegView_Ws_ReadData"), ws.CartEeprom.ReadBuffer, Format.X16),
				new RegEntry("$C6/C7", ResourceHelper.GetMessage("RegView_Ws_Command"), ws.CartEeprom.Command, Format.X16),
				new RegEntry("$C8.0", ResourceHelper.GetMessage("RegView_Ws_ReadDone"), ws.CartEeprom.ReadDone),
				new RegEntry("$C8.1", ResourceHelper.GetMessage("RegView_Ws_Idle"), ws.CartEeprom.Idle),
				new RegEntry("", ResourceHelper.GetMessage("RegView_Ws_WriteDisabled"), ws.CartEeprom.WriteDisabled)
			});
		}

		if(cart.CartType == WsCartType.Bandai2003 || cart.CartType == WsCartType.WonderWitch) {
			entries.AddRange(new List<RegEntry>() {
				new RegEntry("", ResourceHelper.GetMessage("RegView_Ws_CartRTC")),
				new RegEntry("$CA.0-3", ResourceHelper.GetMessage("RegView_Ws_Command"), ws.CartRtc.Command, Format.X8),
				new RegEntry("$CA.4", ResourceHelper.GetMessage("RegView_Ws_Busy"), ws.CartRtc.Busy),
				new RegEntry("$CA.7", ResourceHelper.GetMessage("RegView_Ws_Ready"), ws.CartRtc.Ready),
				new RegEntry("$CB", ResourceHelper.GetMessage("RegView_Ws_Data"), ws.CartRtc.Data)
			});
		}

		return new RegisterViewerTab(ResourceHelper.GetMessage("RegView_Ws_Cart"), entries, CpuType.Ws, MemoryType.WsPort);
	}

	private static RegisterViewerTab GetMiscTab(ref WsState ws)
	{
		List<RegEntry> entries = new List<RegEntry>();

		WsMemoryManagerState mm = ws.MemoryManager;
		WsSerialState serial = ws.Serial;

		entries.AddRange(new List<RegEntry>() {
			new RegEntry("$60", ResourceHelper.GetMessage("RegView_Ws_SystemControl2")),
			new RegEntry("$60.1", ResourceHelper.GetMessage("RegView_Ws_SRAMWaitState"), mm.SlowSram),
			new RegEntry("$60.3", ResourceHelper.GetMessage("RegView_Ws_CartIOWaitState"), mm.SlowPort),
			new RegEntry("$60.5", ResourceHelper.GetMessage("RegView_Ws_4BPPPackFormat"), mm.Enable4bppPacked),
			new RegEntry("$60.6", ResourceHelper.GetMessage("RegView_Ws_4BPPEnabled"), mm.Enable4bpp),
			new RegEntry("$60.7", ResourceHelper.GetMessage("RegView_Ws_ColorEnabled"), mm.ColorEnabled),

			new RegEntry("$62", ResourceHelper.GetMessage("RegView_Ws_SystemControl3")),
			new RegEntry("$62.7", ResourceHelper.GetMessage("RegView_Ws_SwanCrystalSystem"), ws.Model == WsModel.SwanCrystal),

			new RegEntry("$A0", ResourceHelper.GetMessage("RegView_Ws_SystemControl")),
			new RegEntry("$A0.0", ResourceHelper.GetMessage("RegView_Ws_BootROMDisabled"), mm.BootRomDisabled),
			new RegEntry("$A0.1", ResourceHelper.GetMessage("RegView_Ws_ColorSystem"), ws.Model != WsModel.Monochrome && ws.Model != WsModel.PocketChallenge),
			new RegEntry("$A0.2", ResourceHelper.GetMessage("RegView_Ws_16BitROMBus"), mm.CartWordBus),
			new RegEntry("$A0.3", ResourceHelper.GetMessage("RegView_Ws_ROMMWaitState"), mm.SlowRom),

			new RegEntry("", ResourceHelper.GetMessage("RegView_Ws_Serial")),
			new RegEntry("$B1", ResourceHelper.GetMessage("RegView_Ws_ReceiveBufferR"), serial.ReceiveBuffer, Format.X8),
			new RegEntry("$B1", ResourceHelper.GetMessage("RegView_Ws_SendBufferW"), serial.SendBuffer, Format.X8),
			new RegEntry("$B3.0", ResourceHelper.GetMessage("RegView_Ws_ReceiveBufferFilled"), serial.HasReceiveData),
			new RegEntry("$B3.1", ResourceHelper.GetMessage("RegView_Ws_ReceiveBufferOverflow"), serial.ReceiveOverflow),
			new RegEntry("$B3.2", ResourceHelper.GetMessage("RegView_Ws_SendBufferEmpty"), !serial.HasSendData),
			new RegEntry("$B3.6", ResourceHelper.GetMessage("RegView_Ws_HighSpeed"), serial.HighSpeed),
			new RegEntry("$B3.7", ResourceHelper.GetMessage("RegView_Ws_Enabled"), serial.Enabled),

			new RegEntry("$B5", ResourceHelper.GetMessage("RegView_Ws_Keypad")),
			new RegEntry("$B5.0-3", ResourceHelper.GetMessage("RegView_Ws_OutputColumn"), ws.ControlManager.InputSelect & 0x0F, Format.X8),
			new RegEntry("$B5.4-6", ResourceHelper.GetMessage("RegView_Ws_InputRow"), (ws.ControlManager.InputSelect & 0x70) >> 4, Format.X8),

			new RegEntry("", ResourceHelper.GetMessage("RegView_Ws_InternalEEPROM")),
			new RegEntry("$BA/BB", ResourceHelper.GetMessage("RegView_Ws_WriteData"), ws.InternalEeprom.WriteBuffer, Format.X16),
			new RegEntry("$BA/BB", ResourceHelper.GetMessage("RegView_Ws_ReadData"), ws.InternalEeprom.ReadBuffer, Format.X16),
			new RegEntry("$BC/BD", ResourceHelper.GetMessage("RegView_Ws_Command"), ws.InternalEeprom.Command, Format.X16),
			new RegEntry("$BE.0", ResourceHelper.GetMessage("RegView_Ws_ReadDone"), ws.InternalEeprom.ReadDone),
			new RegEntry("$BE.1", ResourceHelper.GetMessage("RegView_Ws_Idle"), ws.InternalEeprom.Idle),
			new RegEntry("$BE.7", ResourceHelper.GetMessage("RegView_Ws_WriteProtection"), ws.InternalEeprom.InternalEepromWriteProtected),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Ws_WriteDisabled"), ws.InternalEeprom.WriteDisabled)
		});

		return new RegisterViewerTab(ResourceHelper.GetMessage("RegView_Common_Misc"), entries, CpuType.Ws, MemoryType.WsPort);
	}
}
