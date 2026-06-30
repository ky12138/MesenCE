using Mesen.Config;
using Mesen.Interop;
using Mesen.ViewModels;

namespace Mesen.Debugger.ViewModels
{
	public class DebuggerOptionsViewModel : ViewModelBase
	{
		public DebuggerConfig Config { get; }

		public bool IsSnes { get; }
		public bool IsSpc { get; }
		public bool IsNes { get; }
		public bool IsGameboy { get; }
		public bool IsPce { get; }
		public bool IsSms { get; }
		public bool IsGba { get; }
		public bool IsWs { get; }

		public bool HasSpecificBreakOptions { get; }

		public bool ShowCpuAddress
		{
			get => Config.AddressDisplayType.HasFlag(AddressDisplayType.CpuAddress);
			set {
				if(value) {
					Config.AddressDisplayType |= AddressDisplayType.CpuAddress;
				} else {
					Config.AddressDisplayType &= ~AddressDisplayType.CpuAddress;
				}
			}
		}

		public bool ShowAbsAddress
		{
			get => Config.AddressDisplayType.HasFlag(AddressDisplayType.AbsAddress);
			set {
				if(value) {
					Config.AddressDisplayType |= AddressDisplayType.AbsAddress;
				} else {
					Config.AddressDisplayType &= ~AddressDisplayType.AbsAddress;
				}
			}
		}

		public bool ShowMapping
		{
			get => Config.AddressDisplayType.HasFlag(AddressDisplayType.Mapping);
			set {
				if(value) {
					Config.AddressDisplayType |= AddressDisplayType.Mapping;
				} else {
					Config.AddressDisplayType &= ~AddressDisplayType.Mapping;
				}
			}
		}

		public bool CompactAddress
		{
			get => Config.AddressDisplayType.HasFlag(AddressDisplayType.Compact);
			set {
				if(value) {
					Config.AddressDisplayType |= AddressDisplayType.Compact;
				} else {
					Config.AddressDisplayType &= ~AddressDisplayType.Compact;
				}
			}
		}

		public DebuggerOptionsViewModel() : this(new DebuggerConfig(), CpuType.Snes) { }

		public DebuggerOptionsViewModel(DebuggerConfig config, CpuType cpuType)
		{
			Config = config;
			IsSnes = cpuType == CpuType.Snes;
			IsSpc = cpuType == CpuType.Spc;
			IsNes = cpuType == CpuType.Nes;
			IsGameboy = cpuType == CpuType.Gameboy;
			IsPce = cpuType == CpuType.Pce;
			IsSms = cpuType == CpuType.Sms;
			IsGba = cpuType == CpuType.Gba;
			IsWs = cpuType == CpuType.Ws;

			HasSpecificBreakOptions = IsSnes || IsSpc || IsNes || IsGameboy || IsPce || IsSms || IsGba || IsWs;
		}
	}
}
