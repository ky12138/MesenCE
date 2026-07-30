using CommunityToolkit.Mvvm.ComponentModel;
using Mesen.ViewModels;

namespace Mesen.Config
{
	public partial class NesDebuggerConfig : ViewModelBase
	{
		[ObservableProperty] public partial bool BreakOnBrk { get; set; } = false;
		[ObservableProperty] public partial bool BreakOnUnofficialOpCode { get; set; } = false;
		[ObservableProperty] public partial bool BreakOnUnstableOpCode { get; set; } = true;
		[ObservableProperty] public partial bool BreakOnCpuCrash { get; set; } = true;

		[ObservableProperty] public partial bool BreakOnBusConflict { get; set; } = false;
		[ObservableProperty] public partial bool BreakOnDecayedOamRead { get; set; } = false;
		[ObservableProperty] public partial bool BreakOnPpuScrollGlitch { get; set; } = false;
		[ObservableProperty] public partial bool BreakOnExtOutputMode { get; set; } = true;
		[ObservableProperty] public partial bool BreakOnInvalidVramAccess { get; set; } = false;
		[ObservableProperty] public partial bool BreakOnInvalidOamWrite { get; set; } = false;
		[ObservableProperty] public partial bool BreakOnDmaInputRead { get; set; } = false;
		[ObservableProperty] public partial bool BreakOnPrgBankSwitchBefore { get; set; } = false;
		[ObservableProperty] public partial bool BreakOnChrBankSwitchBefore { get; set; } = false;
		[ObservableProperty] public partial string PrgBankSwitchPages { get; set; } = "";
		[ObservableProperty] public partial string ChrBankSwitchPages { get; set; } = "";

		[ObservableProperty] public partial bool IndirectTrackerRead { get; set; } = false;
		[ObservableProperty] public partial bool IndirectTrackerWrite { get; set; } = false;
		[ObservableProperty] public partial bool IndirectTrackerJump { get; set; } = true;
	}
}
