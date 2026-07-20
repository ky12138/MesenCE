using Mesen.Debugger.ViewModels;
using Mesen.Interop;
using Mesen.Localization;
using System;
using System.Collections.Generic;
using static Mesen.Debugger.ViewModels.RegEntry;

namespace Mesen.Debugger.RegisterViewer;

public class PceRegisterViewer
{
	public static List<RegisterViewerTab> GetTabs(ref PceState pceState)
	{
		List<RegisterViewerTab> tabs = new List<RegisterViewerTab>() {
			GetPceCpuTab(ref pceState)
		};

		if(pceState.IsSuperGrafx) {
			tabs.Add(GetPceVdcTab(ref pceState.Video.Vdc, "1"));
			tabs.Add(GetPceVdcTab(ref pceState.Video.Vdc2, "2"));
			tabs.Add(GetPceVpcTab(ref pceState));
		} else {
			tabs.Add(GetPceVdcTab(ref pceState.Video.Vdc));
		}
		tabs.Add(GetPceVceTab(ref pceState));
		tabs.Add(GetPcePsgTab(ref pceState));

		if(pceState.HasCdRom) {
			tabs.Add(GetPceCdRomTab(ref pceState));
		}

		if(pceState.HasArcadeCard) {
			tabs.Add(GetPceArcadeCardTab(ref pceState));
		}

		return tabs;
	}

	private static RegisterViewerTab GetPceVceTab(ref PceState state)
	{
		ref PceVceState vce = ref state.Video.Vce;

		List<RegEntry> entries = new List<RegEntry>() {
			new RegEntry("$00.0-1", ResourceHelper.GetMessage("RegView_Pce_ClockSpeed"), vce.ClockDivider == 4 ? ResourceHelper.GetMessage("RegView_Pce_5_37MHz") : vce.ClockDivider == 3 ? ResourceHelper.GetMessage("RegView_Pce_7_16MHz") : ResourceHelper.GetMessage("RegView_Pce_10_74MHz"), vce.ClockDivider),
			new RegEntry("$00.2", ResourceHelper.GetMessage("RegView_Pce_CR_NumberOfScanlines"), vce.ScanlineCount),
			new RegEntry("$00.7", ResourceHelper.GetMessage("RegView_Pce_CR_Grayscale"), vce.Grayscale),
			new RegEntry("$01.0-8", ResourceHelper.GetMessage("RegView_Pce_ColorTableAddress"), vce.PalAddr, Format.X16),
		};

		return new RegisterViewerTab(ResourceHelper.GetMessage("RegView_Pce_VCE"), entries, CpuType.Pce, MemoryType.PceMemory);
	}

	private static RegisterViewerTab GetPceVpcTab(ref PceState state)
	{
		ref PceVpcState vpc = ref state.Video.Vpc;

		List<RegEntry> entries = new List<RegEntry>() {
			new RegEntry("$08.0-3", ResourceHelper.GetMessage("RegView_Pce_PriorityConfigBothWindows")),
			new RegEntry("$08.0", ResourceHelper.GetMessage("RegView_Pce_VDC1Enabled"), vpc.WindowCfg[3].Vdc1Enabled),
			new RegEntry("$08.1", ResourceHelper.GetMessage("RegView_Pce_VDC2Enabled"), vpc.WindowCfg[3].Vdc2Enabled),
			new RegEntry("$08.2-3", ResourceHelper.GetMessage("RegView_Pce_PriorityMode"), (vpc.Priority1 >> 2) & 0x03),

			new RegEntry("$08.4-7", ResourceHelper.GetMessage("RegView_Pce_PriorityConfigWindow2Only")),
			new RegEntry("$08.4", ResourceHelper.GetMessage("RegView_Pce_VDC1Enabled"), vpc.WindowCfg[2].Vdc1Enabled),
			new RegEntry("$08.5", ResourceHelper.GetMessage("RegView_Pce_VDC2Enabled"), vpc.WindowCfg[2].Vdc2Enabled),
			new RegEntry("$08.6-7", ResourceHelper.GetMessage("RegView_Pce_PriorityMode"), (vpc.Priority1 >> 6) & 0x03),

			new RegEntry("$09.0-3", ResourceHelper.GetMessage("RegView_Pce_PriorityConfigWindow1Only")),
			new RegEntry("$09.0", ResourceHelper.GetMessage("RegView_Pce_VDC1Enabled"), vpc.WindowCfg[1].Vdc1Enabled),
			new RegEntry("$09.1", ResourceHelper.GetMessage("RegView_Pce_VDC2Enabled"), vpc.WindowCfg[1].Vdc2Enabled),
			new RegEntry("$09.2-3", ResourceHelper.GetMessage("RegView_Pce_PriorityMode"), (vpc.Priority2 >> 2) & 0x03),

			new RegEntry("$09.4-7", ResourceHelper.GetMessage("RegView_Pce_PriorityConfigNoWindow")),
			new RegEntry("$09.4", ResourceHelper.GetMessage("RegView_Pce_VDC1Enabled"), vpc.WindowCfg[0].Vdc1Enabled),
			new RegEntry("$09.5", ResourceHelper.GetMessage("RegView_Pce_VDC2Enabled"), vpc.WindowCfg[0].Vdc2Enabled),
			new RegEntry("$09.6-7", ResourceHelper.GetMessage("RegView_Pce_PriorityMode"), (vpc.Priority2 >> 6) & 0x03),

			new RegEntry("$0A-0D", ResourceHelper.GetMessage("RegView_Pce_Windows")),
			new RegEntry("$0A-0B", ResourceHelper.GetMessage("RegView_Pce_Window1"), vpc.Window1, Format.X16),
			new RegEntry("$0C-0D", ResourceHelper.GetMessage("RegView_Pce_Window2"), vpc.Window2, Format.X16),

			new RegEntry("$0E", ""),
			new RegEntry("$0E", ResourceHelper.GetMessage("RegView_Pce_STnWritesToVDC2"), vpc.StToVdc2Mode),
		};

		return new RegisterViewerTab(ResourceHelper.GetMessage("RegView_Pce_VPC"), entries, CpuType.Pce, MemoryType.PceMemory);
	}

	private static RegisterViewerTab GetPceVdcTab(ref PceVdcState vdc, string suffix = "")
	{
		List<RegEntry> entries = new List<RegEntry>() {
			new RegEntry("", ResourceHelper.GetMessage("RegView_Common_State")),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Pce_HClockH"), vdc.HClock, Format.X16),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Common_ScanlineV"), vdc.Scanline, Format.X16),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Common_FrameNumber"), vdc.FrameCount),

			new RegEntry("", ResourceHelper.GetMessage("RegView_Pce_SelectedRegister"), vdc.CurrentReg, Format.X8),

			new RegEntry("", ResourceHelper.GetMessage("RegView_Pce_VDCRegisters")),

			new RegEntry("", ResourceHelper.GetMessage("RegView_Common_Status")),
			new RegEntry("$00.0", ResourceHelper.GetMessage("RegView_Pce_Sprite0Hit"), vdc.Sprite0Hit),
			new RegEntry("$00.1", ResourceHelper.GetMessage("RegView_Pce_SpriteOverflow"), vdc.SpriteOverflow),
			new RegEntry("$00.2", ResourceHelper.GetMessage("RegView_Pce_RCRScanlineDetected"), vdc.ScanlineDetected),
			new RegEntry("$00.3", ResourceHelper.GetMessage("RegView_Pce_SATBTransferCompleted"), vdc.SatbTransferDone),
			new RegEntry("$00.4", ResourceHelper.GetMessage("RegView_Pce_VRAMTransferCompleted"), vdc.VramTransferDone),
			new RegEntry("$00.5", ResourceHelper.GetMessage("RegView_Pce_VerticalBlank"), vdc.VerticalBlank),
			//TODOv2
			//new RegEntry("$00.6", "Busy", ...),

			new RegEntry("", ResourceHelper.GetMessage("RegView_Pce_VRAMAddressData")),
			new RegEntry("$00", ResourceHelper.GetMessage("RegView_Pce_MAWR_MemoryWriteAddress"), vdc.MemAddrWrite, Format.X16),
			new RegEntry("$01", ResourceHelper.GetMessage("RegView_Pce_MARR_MemoryReadAddress"), vdc.MemAddrRead, Format.X16),
			new RegEntry("$02", ResourceHelper.GetMessage("RegView_Pce_VWR_VRAMWriteData"), vdc.VramData, Format.X16),

			new RegEntry("$05", ResourceHelper.GetMessage("RegView_Pce_CR_Control")),
			new RegEntry("$05.0", ResourceHelper.GetMessage("RegView_Pce_Sprite0HitIRQEnabled"), vdc.EnableCollisionIrq),
			new RegEntry("$05.1", ResourceHelper.GetMessage("RegView_Pce_OverflowIRQEnabled"), vdc.EnableOverflowIrq),
			new RegEntry("$05.2", ResourceHelper.GetMessage("RegView_Pce_ScanlineDetectRCRIRQEnabled"), vdc.EnableScanlineIrq),
			new RegEntry("$05.3", ResourceHelper.GetMessage("RegView_Pce_VerticalBlankIRQEnabled"), vdc.EnableVerticalBlankIrq),
			new RegEntry("$05.4-5", ResourceHelper.GetMessage("RegView_Pce_ExternalSync"),
				(vdc.OutputHorizontalSync ? ResourceHelper.GetMessage("RegView_Pce_HSYNCOut") : ResourceHelper.GetMessage("RegView_Pce_HSYNCIn")) + ", " +
				(vdc.OutputVerticalSync ? ResourceHelper.GetMessage("RegView_Pce_VSYNCOut") : ResourceHelper.GetMessage("RegView_Pce_VSYNCIn"))
			, null),
			new RegEntry("$05.6", ResourceHelper.GetMessage("RegView_Common_SpritesEnabled"), vdc.NextSpritesEnabled),
			new RegEntry("$05.7", ResourceHelper.GetMessage("RegView_Common_BackgroundEnabled"), vdc.NextBackgroundEnabled),
			new RegEntry("$05.11-12", ResourceHelper.GetMessage("RegView_Pce_VRAMAddressIncrement"), vdc.VramAddrIncrement),

			new RegEntry("$06", ResourceHelper.GetMessage("RegView_Pce_RCR_RasterCompareRegister"), vdc.RasterCompareRegister, Format.X16),
			new RegEntry("$07", ResourceHelper.GetMessage("RegView_Pce_BXR_BGScrollX"), vdc.HvReg.BgScrollX, Format.X16),
			new RegEntry("$08", ResourceHelper.GetMessage("RegView_Pce_BYR_BGScrollY"), vdc.HvReg.BgScrollY, Format.X16),

			new RegEntry("$09", ResourceHelper.GetMessage("RegView_Pce_MWR_MemoryWidth")),
			new RegEntry("$09.0-1", ResourceHelper.GetMessage("RegView_Pce_VRAMAccessMode"), vdc.HvReg.VramAccessMode),
			new RegEntry("$09.2-3", ResourceHelper.GetMessage("RegView_Pce_SpriteAccessMode"), vdc.HvReg.SpriteAccessMode),
			new RegEntry("$09.4-5", ResourceHelper.GetMessage("RegView_Pce_ColumnCount"), vdc.HvReg.ColumnCount),
			new RegEntry("$09.6", ResourceHelper.GetMessage("RegView_Pce_RowCount"), vdc.HvReg.RowCount),
			new RegEntry("$09.7", ResourceHelper.GetMessage("RegView_Pce_CGMode"), vdc.HvReg.CgMode),

			new RegEntry("$0A", ResourceHelper.GetMessage("RegView_Pce_HSR_HorizontalSync")),
			new RegEntry("$0A.0-4", ResourceHelper.GetMessage("RegView_Pce_HSW_HorizontalSyncWidth"), vdc.HvReg.HorizSyncWidth, Format.X8),
			new RegEntry("$0A.8-14", ResourceHelper.GetMessage("RegView_Pce_HDS_HorizontalDisplayStartPosition"), vdc.HvReg.HorizDisplayStart, Format.X8),

			new RegEntry("$0B", ResourceHelper.GetMessage("RegView_Pce_HDR_HorizontalDisplay")),
			new RegEntry("$0B.0-6", ResourceHelper.GetMessage("RegView_Pce_HDW_HorizontalDisplayWidth"), vdc.HvReg.HorizDisplayWidth, Format.X8),
			new RegEntry("$0B.8-14", ResourceHelper.GetMessage("RegView_Pce_HDE_HorizontalDisplayEndPosition"), vdc.HvReg.HorizDisplayEnd, Format.X8),

			new RegEntry("$0C", ResourceHelper.GetMessage("RegView_Pce_VPR_VerticalSync")),
			new RegEntry("$0C.0-4", ResourceHelper.GetMessage("RegView_Pce_VSW_VerticalSyncWidth"), vdc.HvReg.VertSyncWidth, Format.X8),
			new RegEntry("$0C.8-15", ResourceHelper.GetMessage("RegView_Pce_VDS_VerticalDisplayStartPosition"), vdc.HvReg.VertDisplayStart, Format.X8),

			new RegEntry("$0D", ResourceHelper.GetMessage("RegView_Pce_VDR_VerticalDisplay")),
			new RegEntry("$0D.0-8", ResourceHelper.GetMessage("RegView_Pce_VDW_VerticalDisplayWidth"), vdc.HvReg.VertDisplayWidth, Format.X16),

			new RegEntry("$0E", ResourceHelper.GetMessage("RegView_Pce_VCR_VerticalDisplayEnd")),
			new RegEntry("$0E.0-7", ResourceHelper.GetMessage("RegView_Pce_VCR_VerticalDisplayEndPosition"), vdc.HvReg.VertEndPosVcr, Format.X8),

			new RegEntry("$0F", ResourceHelper.GetMessage("RegView_Pce_DCR_BlockTransferControl")),
			new RegEntry("$0F.0", ResourceHelper.GetMessage("RegView_Pce_VRAM_SATBTransferCompleteIRQEnabled"), vdc.VramSatbIrqEnabled),
			new RegEntry("$0F.1", ResourceHelper.GetMessage("RegView_Pce_VRAM_VRAMTransferCompleteIRQEnabled"), vdc.VramVramIrqEnabled),
			new RegEntry("$0F.2", ResourceHelper.GetMessage("RegView_Pce_DecrementSourceAddress"), vdc.DecrementSrc),
			new RegEntry("$0F.3", ResourceHelper.GetMessage("RegView_Pce_DecrementDestinationAddress"), vdc.DecrementDst),
			new RegEntry("$0F.4", ResourceHelper.GetMessage("RegView_Pce_VRAM_SATBTransferAutoRepeat"), vdc.RepeatSatbTransfer),

			new RegEntry("$10", ResourceHelper.GetMessage("RegView_Pce_BlockTransferSourceAddress"), vdc.BlockSrc, Format.X16),
			new RegEntry("$11", ResourceHelper.GetMessage("RegView_Pce_BlockTransferSourceAddressDESR"), vdc.BlockDst, Format.X16),
			new RegEntry("$12", ResourceHelper.GetMessage("RegView_Pce_BlockTransferSourceAddressLENR"), vdc.BlockLen, Format.X16),
			new RegEntry("$13", ResourceHelper.GetMessage("RegView_Pce_DVSSR_VRAM_SATBTransferSourceAddress"), vdc.SatbBlockSrc, Format.X16)
		};

		return new RegisterViewerTab(ResourceHelper.GetMessage("RegView_Pce_VDC") + suffix, entries);
	}

	private static RegisterViewerTab GetPceCpuTab(ref PceState state)
	{
		ref PceMemoryManagerState mem = ref state.MemoryManager;
		ref PceTimerState timer = ref state.Timer;

		List<RegEntry> entries = new List<RegEntry>() {
			new RegEntry("", ResourceHelper.GetMessage("RegView_Pce_CPUSpeed"), mem.FastCpuSpeed ? ResourceHelper.GetMessage("RegView_Pce_7_16MHz") : ResourceHelper.GetMessage("RegView_Pce_1_79MHz"), mem.FastCpuSpeed),

			new RegEntry("", ResourceHelper.GetMessage("RegView_Common_Timer")),
			new RegEntry("$C00.0-6", ResourceHelper.GetMessage("RegView_Pce_ReloadValue"), timer.ReloadValue, Format.X8),
			new RegEntry("$C01.0", ResourceHelper.GetMessage("RegView_Common_Enabled"), timer.Enabled),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Pce_Counter"), timer.Counter, Format.X8),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Pce_Scaler"), timer.Scaler, Format.X16),

			new RegEntry("", ResourceHelper.GetMessage("RegView_Common_IRQ")),
			new RegEntry("$1402", ResourceHelper.GetMessage("RegView_Pce_DisabledIRQs")),
			new RegEntry("$1402.0", ResourceHelper.GetMessage("RegView_Pce_IRQ2_CDROMDisabled"), (mem.DisabledIrqs & 0x01) != 0),
			new RegEntry("$1402.1", ResourceHelper.GetMessage("RegView_Pce_IRQ1_VDCDisabled"), (mem.DisabledIrqs & 0x02) != 0),
			new RegEntry("$1402.2", ResourceHelper.GetMessage("RegView_Pce_TimerIRQDisabled"), (mem.DisabledIrqs & 0x04) != 0),
			new RegEntry("$1403", ResourceHelper.GetMessage("RegView_Pce_ActiveIRQs")),
			new RegEntry("$1403.0", ResourceHelper.GetMessage("RegView_Pce_IRQ2_CDROM"), (mem.ActiveIrqs & 0x01) != 0),
			new RegEntry("$1403.1", ResourceHelper.GetMessage("RegView_Pce_IRQ1_VDC"), (mem.ActiveIrqs & 0x02) != 0),
			new RegEntry("$1403.2", ResourceHelper.GetMessage("RegView_Pce_TimerIRQ"), (mem.ActiveIrqs & 0x04) != 0),

			new RegEntry("", ResourceHelper.GetMessage("RegView_Pce_MPR")),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Pce_MPR0"), mem.Mpr[0], Format.X8),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Pce_MPR1"), mem.Mpr[1], Format.X8),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Pce_MPR2"), mem.Mpr[2], Format.X8),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Pce_MPR3"), mem.Mpr[3], Format.X8),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Pce_MPR4"), mem.Mpr[4], Format.X8),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Pce_MPR5"), mem.Mpr[5], Format.X8),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Pce_MPR6"), mem.Mpr[6], Format.X8),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Pce_MPR7"), mem.Mpr[7], Format.X8),
		};

		return new RegisterViewerTab(ResourceHelper.GetMessage("RegView_Pce_CPU"), entries, CpuType.Pce, MemoryType.PceMemory);
	}

	private static RegisterViewerTab GetPcePsgTab(ref PceState pceState)
	{
		List<RegEntry> entries = new List<RegEntry>() {
			new RegEntry("$800.0-2", ResourceHelper.GetMessage("RegView_Pce_ChannelSelect"), pceState.Psg.ChannelSelect, Format.X8),
			new RegEntry("$801.0-3", ResourceHelper.GetMessage("RegView_Pce_RightAmplitude"), pceState.Psg.RightVolume, Format.X8),
			new RegEntry("$801.4-7", ResourceHelper.GetMessage("RegView_Pce_LeftAmplitude"), pceState.Psg.LeftVolume, Format.X8),
			new RegEntry("$808.4-7", ResourceHelper.GetMessage("RegView_Pce_LFOFrequency"), pceState.Psg.LfoFrequency, Format.X8),
			new RegEntry("$809", ResourceHelper.GetMessage("RegView_Pce_LFOControl"), pceState.Psg.LfoControl, Format.X8),
		};

		for(int i = 0; i < 6; i++) {
			ref PcePsgChannelState ch = ref pceState.PsgChannels[i];

			entries.AddRange(new List<RegEntry>() {
				new RegEntry("", ResourceHelper.GetMessage("RegView_Pce_Channel", i + 1)),
				new RegEntry("$802-$803", ResourceHelper.GetMessage("RegView_Common_Frequency"), ch.Frequency, Format.X16),
				new RegEntry("$804.0-4", ResourceHelper.GetMessage("RegView_Pce_Amplitude"), ch.Amplitude, Format.X8),
				new RegEntry("$804.6", ResourceHelper.GetMessage("RegView_Pce_DDAEnabled"), ch.DdaEnabled),
				new RegEntry("$804.7", ResourceHelper.GetMessage("RegView_Common_ChannelEnabled"), ch.Enabled),
				new RegEntry("$805.0-3", ResourceHelper.GetMessage("RegView_Pce_RightAmplitude"), ch.RightVolume, Format.X8),
				new RegEntry("$805.4-7", ResourceHelper.GetMessage("RegView_Pce_LeftAmplitude"), ch.LeftVolume, Format.X8),
				new RegEntry("$806.0-4", ResourceHelper.GetMessage("RegView_Pce_DDAOutputValue"), ch.DdaOutputValue, Format.X8),
				new RegEntry("", ResourceHelper.GetMessage("RegView_Common_Timer"), ch.Timer),
			});

			if(i >= 4) {
				entries.Add(new RegEntry("$807.7", ResourceHelper.GetMessage("RegView_Pce_NoiseEnabled"), ch.NoiseEnabled));
				entries.Add(new RegEntry("$807.0-4", ResourceHelper.GetMessage("RegView_Pce_NoiseFrequency"), ch.NoiseFrequency, Format.X8));
				entries.Add(new RegEntry("", ResourceHelper.GetMessage("RegView_Pce_NoiseTimer"), ch.NoiseTimer));
				entries.Add(new RegEntry("", ResourceHelper.GetMessage("RegView_Pce_NoiseOutput"), ch.NoiseOutput == 0x0F ? 1 : 0));
				entries.Add(new RegEntry("", ResourceHelper.GetMessage("RegView_Pce_NoiseLSFR"), ch.NoiseLfsr, Format.X24));
			}
		}

		return new RegisterViewerTab(ResourceHelper.GetMessage("RegView_Pce_PSG"), entries, CpuType.Pce, MemoryType.PceMemory);
	}

	private static RegisterViewerTab GetPceCdRomTab(ref PceState pceState)
	{
		ref PceCdRomState cdrom = ref pceState.CdRom;
		ref PceCdAudioPlayerState player = ref pceState.CdPlayer;
		ref PceAudioFaderState fader = ref pceState.AudioFader;
		ref PceAdpcmState adpcm = ref pceState.Adpcm;
		ref PceScsiBusState scsi = ref pceState.ScsiDrive;

		List<RegEntry> entries = new List<RegEntry>() {
			new RegEntry("$1807.7", ResourceHelper.GetMessage("RegView_Pce_BRAMEnabled"), !cdrom.BramLocked),

			new RegEntry("", ResourceHelper.GetMessage("RegView_Pce_ADPCM")),
			new RegEntry("$1808-$1809", ResourceHelper.GetMessage("RegView_Pce_Address"), adpcm.AddressPort, Format.X16),
			new RegEntry("$180A", ResourceHelper.GetMessage("RegView_Pce_WriteBuffer"), adpcm.WriteBuffer, Format.X8),
			new RegEntry("$180A", ResourceHelper.GetMessage("RegView_Pce_ReadBuffer"), adpcm.ReadBuffer, Format.X8),
			new RegEntry("$180B", ResourceHelper.GetMessage("RegView_Pce_DMAControl"), adpcm.DmaControl, Format.X8),
			new RegEntry("$180B.0", ResourceHelper.GetMessage("RegView_Pce_DMARequested"), (adpcm.DmaControl & 0x01) != 0),
			new RegEntry("$180B.1", ResourceHelper.GetMessage("RegView_Pce_DMAEnabled"), (adpcm.DmaControl & 0x02) != 0),
			new RegEntry("$180C.0", ResourceHelper.GetMessage("RegView_Pce_EndReached"), adpcm.EndReached),
			new RegEntry("$180C.2", ResourceHelper.GetMessage("RegView_Pce_WritePending"), adpcm.WriteClockCounter > 0),
			new RegEntry("$180C.3", ResourceHelper.GetMessage("RegView_Pce_ADPCMPlayingBusy"), adpcm.Playing),
			new RegEntry("$180C.7", ResourceHelper.GetMessage("RegView_Pce_ReadPending"), adpcm.ReadClockCounter > 0),
			new RegEntry("$180D", ResourceHelper.GetMessage("RegView_Common_Control"), adpcm.Control),
			/*new RegEntry("$180D.0", "Clock Write Address (?)", (adpcm.Control & 0x01) != 0),
			new RegEntry("$180D.1", "Latch Write Address", (adpcm.Control & 0x02) != 0),
			new RegEntry("$180D.2", "Clock Read Address (?)", (adpcm.Control & 0x04) != 0),
			new RegEntry("$180D.3", "Latch Read Address", (adpcm.Control & 0x08) != 0),*/
			new RegEntry("$180D.4", ResourceHelper.GetMessage("RegView_Pce_LatchLength"), (adpcm.Control & 0x10) != 0),
			new RegEntry("$180D.5", ResourceHelper.GetMessage("RegView_Pce_StartPlayback"), (adpcm.Control & 0x20) != 0),
			new RegEntry("$180D.6", ResourceHelper.GetMessage("RegView_Pce_AutoStopOnLength0"), (adpcm.Control & 0x40) != 0),
			new RegEntry("$180D.7", ResourceHelper.GetMessage("RegView_Pce_Reset"), (adpcm.Control & 0x80) != 0),
			new RegEntry("$180E", ResourceHelper.GetMessage("RegView_Pce_Value"), adpcm.PlaybackRate),
			new RegEntry("$180E.0-3", ResourceHelper.GetMessage("RegView_Pce_PlaybackRate"), Math.Round(32000.0 / (16 - adpcm.PlaybackRate)) + ResourceHelper.GetMessage("RegView_Common_HzSuffix"), (adpcm.PlaybackRate & 0xF)),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Pce_HalfReached"), adpcm.HalfReached),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Pce_ADPCMLength"), adpcm.AdpcmLength, Format.X16),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Pce_ReadAddress"), adpcm.ReadAddress, Format.X16),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Pce_WriteAddress"), adpcm.WriteAddress, Format.X16),

			new RegEntry("$1802", ResourceHelper.GetMessage("RegView_Common_EnabledIRQs"), cdrom.EnabledIrqs),
			new RegEntry("$1802.2", ResourceHelper.GetMessage("RegView_Pce_ADPCM_HalfReachedIRQEnabled"), (cdrom.EnabledIrqs & (int)PceCdRomIrqSource.Adpcm) != 0),
			new RegEntry("$1802.3", ResourceHelper.GetMessage("RegView_Pce_ADPCM_EndReachedIRQEnabled"), (cdrom.EnabledIrqs & (int)PceCdRomIrqSource.Stop) != 0),
			new RegEntry("$1802.4", ResourceHelper.GetMessage("RegView_Pce_SubcodeIRQEnabled"), (cdrom.EnabledIrqs & (int)PceCdRomIrqSource.SubCode) != 0),
			new RegEntry("$1802.5", ResourceHelper.GetMessage("RegView_Pce_StatusMessageInIRQEnabled"), (cdrom.EnabledIrqs & (int)PceCdRomIrqSource.StatusMsgIn) != 0),
			new RegEntry("$1802.6", ResourceHelper.GetMessage("RegView_Pce_DataInIRQEnabled"), (cdrom.EnabledIrqs & (int)PceCdRomIrqSource.DataIn) != 0),

			new RegEntry("$1803", ResourceHelper.GetMessage("RegView_Pce_ActiveIRQs")),
			new RegEntry("$1803.2", ResourceHelper.GetMessage("RegView_Pce_ADPCM_HalfReachedIRQ"), (cdrom.ActiveIrqs & (int)PceCdRomIrqSource.Adpcm) != 0),
			new RegEntry("$1803.3", ResourceHelper.GetMessage("RegView_Pce_ADPCM_EndReachedIRQ"), (cdrom.ActiveIrqs & (int)PceCdRomIrqSource.Stop) != 0),
			new RegEntry("$1803.4", ResourceHelper.GetMessage("RegView_Pce_SubcodeIRQ"), (cdrom.ActiveIrqs & (int)PceCdRomIrqSource.SubCode) != 0),
			new RegEntry("$1803.5", ResourceHelper.GetMessage("RegView_Pce_StatusMessageInIRQ"), (cdrom.ActiveIrqs & (int)PceCdRomIrqSource.StatusMsgIn) != 0),
			new RegEntry("$1803.6", ResourceHelper.GetMessage("RegView_Pce_DataInIRQ"), (cdrom.ActiveIrqs & (int)PceCdRomIrqSource.DataIn) != 0),

			new RegEntry("", ResourceHelper.GetMessage("RegView_Pce_SCSIDrive")),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Pce_CurrentSector"), scsi.Sector),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Pce_ReadUntilSector"), scsi.Sector + scsi.SectorsToRead),
			new RegEntry("$1801", ResourceHelper.GetMessage("RegView_Pce_DataPortWrite"), scsi.DataPort),
			new RegEntry("$1801", ResourceHelper.GetMessage("RegView_Pce_DataPortRead"), scsi.ReadDataPort),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Pce_SCSIPhase"), scsi.Phase),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Pce_SCSISignals")),
			new RegEntry("$1800.3-7", ResourceHelper.GetMessage("RegView_Pce_Status"), (
				(scsi.Signals[4] != 0 ? 0x08 : 0) |
				(scsi.Signals[3] != 0 ? 0x10 : 0) |
				(scsi.Signals[5] != 0 ? 0x20 : 0) |
				(scsi.Signals[6] != 0 ? 0x40 : 0) |
				(scsi.Signals[2] != 0 ? 0x80 : 0)
			)),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Pce_ACK"), scsi.Signals[0] != 0),
			//new RegEntry("", "ATN", scsi.Signals[1] != 0), //unused
			new RegEntry("$1800.7", ResourceHelper.GetMessage("RegView_Pce_BSY"), scsi.Signals[2] != 0),
			new RegEntry("$1800.4", ResourceHelper.GetMessage("RegView_Pce_CD"), scsi.Signals[3] != 0),
			new RegEntry("$1800.3", ResourceHelper.GetMessage("RegView_Pce_IO"), scsi.Signals[4] != 0),
			new RegEntry("$1800.5", ResourceHelper.GetMessage("RegView_Pce_MSG"), scsi.Signals[5] != 0),
			new RegEntry("$1800.6", ResourceHelper.GetMessage("RegView_Pce_REQ"), scsi.Signals[6] != 0),
			new RegEntry("$1804.1", ResourceHelper.GetMessage("RegView_Pce_RST"), scsi.Signals[7] != 0),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Pce_SEL"), scsi.Signals[8] != 0),

			new RegEntry("", ResourceHelper.GetMessage("RegView_Pce_CDAudioPlayer")),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Pce_CDAudioStatus"), player.Status),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Pce_CurrentSector"), player.CurrentSector, Format.X16),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Pce_CurrentSample"), player.CurrentSample, Format.X16),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Pce_StartSector"), player.StartSector, Format.X16),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Pce_EndSector"), player.EndSector, Format.X16),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Pce_EndBehavior"), player.EndBehavior),

			new RegEntry("$180F", ResourceHelper.GetMessage("RegView_Pce_AudioFader"), fader.RegValue),
			new RegEntry("$180F.1", ResourceHelper.GetMessage("RegView_Pce_Target"), fader.Target),
			new RegEntry("$180F.2", ResourceHelper.GetMessage("RegView_Pce_FadeSpeed"), fader.FastFade ? ResourceHelper.GetMessage("RegView_Pce_2_5Secs") : ResourceHelper.GetMessage("RegView_Pce_6Secs"), fader.FastFade),
			new RegEntry("$180F.3", ResourceHelper.GetMessage("RegView_Common_Enabled"), fader.Enabled),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Pce_EffectiveVolume"), fader.Enabled ? (int)Math.Max(0.0, 100 - ((pceState.MemoryManager.CycleCount - fader.StartClock) / ((fader.FastFade ? 0.025 : 0.06) * 21477270))) : 100)
		};

		return new RegisterViewerTab(ResourceHelper.GetMessage("RegView_Pce_CDROM"), entries, CpuType.Pce, MemoryType.PceMemory);
	}

	private static RegisterViewerTab GetPceArcadeCardTab(ref PceState pceState)
	{
		ref PceArcadeCardState state = ref pceState.ArcadeCard;

		List<RegEntry> entries = new List<RegEntry>() {
			new RegEntry("$1AE0-3", ResourceHelper.GetMessage("RegView_Pce_ShiftRegister"), state.ValueReg, Format.X32),
			new RegEntry("$1AE4", ResourceHelper.GetMessage("RegView_Pce_ShiftValue"), state.ShiftReg, Format.X8),
			new RegEntry("$1AE5", ResourceHelper.GetMessage("RegView_Pce_RotateValue"), state.RotateReg, Format.X8),
		};

		for(int i = 0; i < 4; i++) {
			ref PceArcadeCardPortConfig port = ref state.Port[i];

			entries.AddRange(new List<RegEntry>() {
				new RegEntry("", ResourceHelper.GetMessage("RegView_Pce_Port", i + 1)),
				new RegEntry("$1A" + i + "2-4", ResourceHelper.GetMessage("RegView_Pce_BaseAddress"), port.BaseAddress, Format.X24),
				new RegEntry("$1A" + i + "5-6", ResourceHelper.GetMessage("RegView_Pce_Offset"), port.Offset, Format.X16),
				new RegEntry("$1A" + i + "7-8", ResourceHelper.GetMessage("RegView_Pce_IncrementValue"), port.IncValue, Format.X16),
				new RegEntry("$1A" + i + "9", ResourceHelper.GetMessage("RegView_Pce_Control"), port.Control, Format.X8),
				new RegEntry("$1A" + i + "9.0", ResourceHelper.GetMessage("RegView_Pce_AutoIncrement"), port.AutoIncrement),
				new RegEntry("$1A" + i + "9.1", ResourceHelper.GetMessage("RegView_Pce_AddOffset"), port.AddOffset),
				new RegEntry("$1A" + i + "9.3", ResourceHelper.GetMessage("RegView_Pce_NegativeOffset"), port.SignedOffset),
				new RegEntry("$1A" + i + "9.4", ResourceHelper.GetMessage("RegView_Pce_AddIncrementToBase"), port.AddIncrementToBase),
				new RegEntry("$1A" + i + "9.5-6", ResourceHelper.GetMessage("RegView_Pce_AddOffsetTrigger"), port.AddOffsetTrigger)
			});
		}

		return new RegisterViewerTab(ResourceHelper.GetMessage("RegView_Pce_ArcadeCard"), entries, CpuType.Pce, MemoryType.PceMemory);
	}
}
