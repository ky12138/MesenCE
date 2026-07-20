using Mesen.Debugger.ViewModels;
using Mesen.Interop;
using Mesen.Localization;
using System.Collections.Generic;
using static Mesen.Debugger.ViewModels.RegEntry;

namespace Mesen.Debugger.RegisterViewer;

public class SmsRegisterViewer
{
	public static List<RegisterViewerTab> GetTabs(ref SmsState smsState, RomFormat romFormat)
	{
		List<RegisterViewerTab> tabs = new() {
			GetSmsVdpTab(ref smsState),
			GetSmsPsgTab(ref smsState, romFormat == RomFormat.GameGear),
			GetSmsMiscTab(ref smsState),
		};
		return tabs;
	}

	private static RegisterViewerTab GetSmsVdpTab(ref SmsState sms)
	{
		List<RegEntry> entries = new List<RegEntry>();

		SmsVdpState vdp = sms.Vdp;
		entries.AddRange(new List<RegEntry>() {
			new RegEntry("", ResourceHelper.GetMessage("RegView_Common_State")),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Common_CycleH"), vdp.Cycle),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Common_ScanlineV"), vdp.Scanline),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Common_FrameNumber"), vdp.FrameCount),

			new RegEntry("", ResourceHelper.GetMessage("RegView_Common_Ports")),
			new RegEntry("$7E", ResourceHelper.GetMessage("RegView_Sms_VerticalCounter"), vdp.Scanline),
			new RegEntry("$7F", ResourceHelper.GetMessage("RegView_Sms_HorizontalCounterLatch"), vdp.HCounterLatch),
			new RegEntry("$BF.7", ResourceHelper.GetMessage("RegView_Sms_VerticalBlankIRQPending"), vdp.VerticalBlankIrqPending),
			new RegEntry("$BF.6", ResourceHelper.GetMessage("RegView_Sms_SpriteOverflow"), vdp.SpriteOverflow),
			new RegEntry("$BF.5", ResourceHelper.GetMessage("RegView_Sms_SpriteCollision"), vdp.SpriteCollision),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Sms_DataPortBuffer"), vdp.VramBuffer),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Sms_AddressRegister"), vdp.AddressReg, Format.X16),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Sms_CodeRegister"), vdp.CodeReg),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Sms_ControlPortMSBToggle"), vdp.ControlPortMsbToggle),

			new RegEntry("", ResourceHelper.GetMessage("RegView_Common_Registers")),
			new RegEntry("$00.0", ResourceHelper.GetMessage("RegView_Sms_SyncDisabled"), vdp.SyncDisabled),
			new RegEntry("$00.1", ResourceHelper.GetMessage("RegView_Sms_M2_Allow224_240LineMode"), vdp.M2_AllowHeightChange),
			new RegEntry("$00.2", ResourceHelper.GetMessage("RegView_Sms_M4_UseMode4"), vdp.UseMode4),
			new RegEntry("$00.3", ResourceHelper.GetMessage("RegView_Sms_ShiftSpritesLeft"), vdp.ShiftSpritesLeft),
			new RegEntry("$00.4", ResourceHelper.GetMessage("RegView_Sms_ScanlineIRQEnabled"), vdp.EnableScanlineIrq),
			new RegEntry("$00.5", ResourceHelper.GetMessage("RegView_Sms_MaskFirstColumn"), vdp.MaskFirstColumn),
			new RegEntry("$00.6", ResourceHelper.GetMessage("RegView_Sms_HorizontalScrollLock"), vdp.HorizontalScrollLock),
			new RegEntry("$00.7", ResourceHelper.GetMessage("RegView_Sms_VerticalScrollLock"), vdp.VerticalScrollLock),

			new RegEntry("$01.0", ResourceHelper.GetMessage("RegView_Sms_ZoomSprites2xSize"), vdp.EnableDoubleSpriteSize),
			new RegEntry("$01.1", ResourceHelper.GetMessage("RegView_Sms_LargeSprites8x16Or16x16"), vdp.UseLargeSprites),
			new RegEntry("$01.3", ResourceHelper.GetMessage("RegView_Sms_M3_240LineOutput"), vdp.M3_Use240LineMode),
			new RegEntry("$01.4", ResourceHelper.GetMessage("RegView_Sms_M1_224LineOutput"), vdp.M1_Use224LineMode),
			new RegEntry("$01.5", ResourceHelper.GetMessage("RegView_Sms_VerticalBlankIRQEnabled"), vdp.EnableVerticalBlankIrq),
			new RegEntry("$01.6", ResourceHelper.GetMessage("RegView_Sms_RenderingEnabled"), vdp.RenderingEnabled),
			new RegEntry("$01.7", ResourceHelper.GetMessage("RegView_Sms_SG1000_16KVRAMMode"), vdp.Sg16KVramMode),

			new RegEntry("$02.0-3", ResourceHelper.GetMessage("RegView_Sms_NametableAddress"), vdp.NametableAddress),
			new RegEntry("$03", ResourceHelper.GetMessage("RegView_Sms_ColorTableAddress"), vdp.ColorTableAddress),
			new RegEntry("$04.0-2", ResourceHelper.GetMessage("RegView_Sms_PatternTableAddress"), vdp.BgPatternTableAddress),
			new RegEntry("$05.0-6", ResourceHelper.GetMessage("RegView_Sms_SpriteTableAddress"), vdp.SpriteTableAddress),
			new RegEntry("$06.0-2", ResourceHelper.GetMessage("RegView_Sms_SpriteTilesetAddress"), vdp.SpritePatternSelector),
			new RegEntry("$07.0-3", ResourceHelper.GetMessage("RegView_Sms_BackgroundColorIndex"), vdp.BackgroundColorIndex),
			new RegEntry("$07.4-7", ResourceHelper.GetMessage("RegView_Sms_TextColorIndex"), vdp.TextColorIndex),
			new RegEntry("$08", ResourceHelper.GetMessage("RegView_Sms_HorizontalScroll"), vdp.HorizontalScroll),
			new RegEntry("$09", ResourceHelper.GetMessage("RegView_Sms_VerticalScroll"), vdp.VerticalScroll),
			new RegEntry("$0A", ResourceHelper.GetMessage("RegView_Sms_ScanlineIRQReloadValue"), vdp.ScanlineCounter),
			new RegEntry("--", ResourceHelper.GetMessage("RegView_Sms_ScanlineIRQCounter"), vdp.ScanlineCounterLatch),
		});

		return new RegisterViewerTab(ResourceHelper.GetMessage("RegView_Sms_VDP"), entries, CpuType.Sms);
	}

	private static RegisterViewerTab GetSmsPsgTab(ref SmsState sms, bool isGameGear)
	{
		List<RegEntry> entries = new List<RegEntry>();

		SmsPsgState psg = sms.Psg;

		entries.Add(new RegEntry("", ResourceHelper.GetMessage("RegView_Sms_LatchedRegister"), psg.SelectedReg));

		for(int i = 0; i < 3; i++) {
			entries.AddRange(new List<RegEntry>() {
				new RegEntry("", ResourceHelper.GetMessage("RegView_Sms_Tone" + (i + 1))),
				new RegEntry("", ResourceHelper.GetMessage("RegView_Common_Volume"), psg.Tone[i].Volume, Format.X8),
				new RegEntry("", ResourceHelper.GetMessage("RegView_Sms_ReloadValue"), psg.Tone[i].ReloadValue, Format.X16),
				new RegEntry("", ResourceHelper.GetMessage("RegView_Common_Timer"), psg.Tone[i].Timer, Format.X16),
				new RegEntry("", ResourceHelper.GetMessage("RegView_Common_Output"), psg.Tone[i].Output, Format.X8),
			});
		}

		entries.AddRange(new List<RegEntry>() {
			new RegEntry("", ResourceHelper.GetMessage("RegView_Common_Noise")),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Common_Volume"), psg.Noise.Volume, Format.X8),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Sms_WhiteNoiseMode"), (psg.Noise.Control & 0x04) != 0),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Common_Divider"), psg.Noise.Control & 0x03),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Common_Timer"), psg.Noise.Timer, Format.X16),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Common_Output"), psg.Noise.Output),
		});

		if(isGameGear) {
			entries.AddRange(new List<RegEntry>() {
				new RegEntry("", ResourceHelper.GetMessage("RegView_Sms_Panning")),
				new RegEntry("$06.0", ResourceHelper.GetMessage("RegView_Sms_RightTone1Enabled"), (psg.GameGearPanningReg & 0x01) != 0),
				new RegEntry("$06.1", ResourceHelper.GetMessage("RegView_Sms_RightTone2Enabled"), (psg.GameGearPanningReg & 0x02) != 0),
				new RegEntry("$06.2", ResourceHelper.GetMessage("RegView_Sms_RightTone3Enabled"), (psg.GameGearPanningReg & 0x04) != 0),
				new RegEntry("$06.3", ResourceHelper.GetMessage("RegView_Sms_RightNoiseEnabled"), (psg.GameGearPanningReg & 0x08) != 0),
				new RegEntry("$06.4", ResourceHelper.GetMessage("RegView_Sms_LeftTone1Enabled"), (psg.GameGearPanningReg & 0x10) != 0),
				new RegEntry("$06.5", ResourceHelper.GetMessage("RegView_Sms_LeftTone2Enabled"), (psg.GameGearPanningReg & 0x20) != 0),
				new RegEntry("$06.6", ResourceHelper.GetMessage("RegView_Sms_LeftTone3Enabled"), (psg.GameGearPanningReg & 0x40) != 0),
				new RegEntry("$06.7", ResourceHelper.GetMessage("RegView_Sms_LeftNoiseEnabled"), (psg.GameGearPanningReg & 0x80) != 0),
			});
		}

		return new RegisterViewerTab(ResourceHelper.GetMessage("RegView_Sms_PSG"), entries, CpuType.Sms);
	}

	private static RegisterViewerTab GetSmsMiscTab(ref SmsState sms)
	{
		List<RegEntry> entries = new List<RegEntry>();

		SmsControlManagerState ctrl = sms.ControlManager;
		SmsMemoryManagerState mem = sms.MemoryManager;

		entries.AddRange(new List<RegEntry>() {
			new RegEntry("", ResourceHelper.GetMessage("RegView_Sms_Port_3E")),
			new RegEntry("$3E.2", ResourceHelper.GetMessage("RegView_Sms_IO_Disabled"), !mem.IoEnabled),
			new RegEntry("$3E.3", ResourceHelper.GetMessage("RegView_Sms_BIOSDisabled"), !mem.BiosEnabled),
			new RegEntry("$3E.4", ResourceHelper.GetMessage("RegView_Sms_SystemRAMDisabled"), !mem.WorkRamEnabled),
			new RegEntry("$3E.5", ResourceHelper.GetMessage("RegView_Sms_CardSlotDisabled"), !mem.CardEnabled),
			new RegEntry("$3E.6", ResourceHelper.GetMessage("RegView_Sms_CartridgeDisabled"), !mem.CartridgeEnabled),
			new RegEntry("$3E.7", ResourceHelper.GetMessage("RegView_Sms_ExpansionSlotDisabled"), !mem.ExpEnabled),
			new RegEntry("", ResourceHelper.GetMessage("RegView_Sms_Port_3F")),
			new RegEntry("$3F.0", ResourceHelper.GetMessage("RegView_Sms_PortATRDirection"), (ctrl.ControlPort & 0x01) != 0 ? ResourceHelper.GetMessage("RegView_Common_Input") : ResourceHelper.GetMessage("RegView_Common_Output"), (ctrl.ControlPort & 0x01) != 0),
			new RegEntry("$3F.1", ResourceHelper.GetMessage("RegView_Sms_PortATHDirection"), (ctrl.ControlPort & 0x02) != 0 ? ResourceHelper.GetMessage("RegView_Common_Input") : ResourceHelper.GetMessage("RegView_Common_Output"), (ctrl.ControlPort & 0x02) != 0),
			new RegEntry("$3F.2", ResourceHelper.GetMessage("RegView_Sms_PortBTRDirection"), (ctrl.ControlPort & 0x04) != 0 ? ResourceHelper.GetMessage("RegView_Common_Input") : ResourceHelper.GetMessage("RegView_Common_Output"), (ctrl.ControlPort & 0x04) != 0),
			new RegEntry("$3F.3", ResourceHelper.GetMessage("RegView_Sms_PortBTHDirection"), (ctrl.ControlPort & 0x08) != 0 ? ResourceHelper.GetMessage("RegView_Common_Input") : ResourceHelper.GetMessage("RegView_Common_Output"), (ctrl.ControlPort & 0x08) != 0),
			new RegEntry("$3F.4", ResourceHelper.GetMessage("RegView_Sms_PortATROutputLevel"), (ctrl.ControlPort & 0x10) != 0 ? ResourceHelper.GetMessage("RegView_Sms_High") : ResourceHelper.GetMessage("RegView_Sms_Low"), (ctrl.ControlPort & 0x10) != 0),
			new RegEntry("$3F.5", ResourceHelper.GetMessage("RegView_Sms_PortATHOutputLevel"), (ctrl.ControlPort & 0x20) != 0 ? ResourceHelper.GetMessage("RegView_Sms_High") : ResourceHelper.GetMessage("RegView_Sms_Low"), (ctrl.ControlPort & 0x20) != 0),
			new RegEntry("$3F.6", ResourceHelper.GetMessage("RegView_Sms_PortBTROutputLevel"), (ctrl.ControlPort & 0x40) != 0 ? ResourceHelper.GetMessage("RegView_Sms_High") : ResourceHelper.GetMessage("RegView_Sms_Low"), (ctrl.ControlPort & 0x40) != 0),
			new RegEntry("$3F.7", ResourceHelper.GetMessage("RegView_Sms_PortBTHOutputLevel"), (ctrl.ControlPort & 0x80) != 0 ? ResourceHelper.GetMessage("RegView_Sms_High") : ResourceHelper.GetMessage("RegView_Sms_Low"), (ctrl.ControlPort & 0x80) != 0)
		});

		return new RegisterViewerTab(ResourceHelper.GetMessage("RegView_Sms_Ports"), entries, CpuType.Sms);
	}
}
