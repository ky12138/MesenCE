// Stub type definitions used ONLY when building with NesOnly=true.
//
// Several shared Avalonia XAML files (DebuggerOptionsView.axaml, DebuggerWindow.axaml,
// EventViewerWindow.axaml, ConfigWindow.axaml) reference platform-specific types that are
// excluded from the NES-only build via <Compile Remove>/<AvaloniaXaml Remove>. Because XAML
// cannot use C# preprocessor directives, the simplest way to keep those XAML files compiling
// is to provide minimal stub types so the type/property references resolve. These stubs are
// never actually used at runtime in a NES-only build (the corresponding UI branches are hidden
// via IsVisible bindings or are simply never instantiated).
//
// Under a normal (non-NES_ONLY) build this entire file emits nothing, so it cannot clash with
// the real type definitions.
#if NES_ONLY
using Avalonia.Controls;

namespace Mesen.Config
{
	// Break-option sub-configs referenced by DebuggerOptionsView.axaml bindings.
	public class SnesDebuggerConfig
	{
		public bool UseAltSpcOpNames { get; set; }
		public bool BreakOnBrk { get; set; }
		public bool BreakOnCop { get; set; }
		public bool BreakOnWdm { get; set; }
		public bool BreakOnStp { get; set; }
		public bool BreakOnInvalidPpuAccess { get; set; }
		public bool BreakOnReadDuringAutoJoy { get; set; }
		public bool SpcBreakOnBrk { get; set; }
		public bool SpcBreakOnStpSleep { get; set; }
		public bool IgnoreDspReadWrites { get; set; }
	}

	public class GbDebuggerConfig
	{
		public bool GbBreakOnInvalidOamAccess { get; set; }
		public bool GbBreakOnInvalidVramAccess { get; set; }
		public bool GbBreakOnDisableLcdOutsideVblank { get; set; }
		public bool GbBreakOnInvalidOpCode { get; set; }
		public bool GbBreakOnNopLoad { get; set; }
		public bool GbBreakOnOamCorruption { get; set; }
	}

	public class GbaDebuggerConfig
	{
		public GbaDisassemblyMode DisassemblyMode { get; set; }
		public bool BreakOnInvalidOpCode { get; set; }
		public bool BreakOnNopLoad { get; set; }
		public bool BreakOnUnalignedMemAccess { get; set; }
	}

	public class PceDebuggerConfig
	{
		public bool BreakOnBrk { get; set; }
		public bool BreakOnUnofficialOpCode { get; set; }
		public bool BreakOnInvalidVramAddress { get; set; }
	}

	public class SmsDebuggerConfig
	{
		public bool BreakOnNopLoad { get; set; }
	}

	public class WsDebuggerConfig
	{
		public bool BreakOnUndefinedOpCode { get; set; }
	}

	public enum GbaDisassemblyMode : byte
	{
		Default,
		Arm,
		Thumb
	}

	// Event viewer config data types used as DataTemplate keys in EventViewerWindow.axaml.
	public class SnesEventViewerConfig { }
	public class GbEventViewerConfig { }
	public class GbaEventViewerConfig { }
	public class PceEventViewerConfig { }
	public class SmsEventViewerConfig { }
	public class WsEventViewerConfig { }

	// Enum referenced by InteropPceEventViewerConfig (DebugApi.cs), which is always compiled.
	// Must match the ordering in Core/PCE/Debugger/PceEventManager.h.
	public enum PceEventViewerSgxFilter : byte
	{
		Both,
		Vdc1,
		Vdc2
	}
}

namespace Mesen.ViewModels
{
	public class SnesConfigViewModel { }
	public class GameboyConfigViewModel { }
	public class GbaConfigViewModel { }
	public class PceConfigViewModel { }
	public class SmsConfigViewModel { }
	public class WsConfigViewModel { }
	public class OtherConsolesConfigViewModel { }
}

namespace Mesen.Views
{
	public class SnesConfigView : UserControl { }
	public class GameboyConfigView : UserControl { }
	public class GbaConfigView : UserControl { }
	public class PceConfigView : UserControl { }
	public class SmsConfigView : UserControl { }
	public class WsConfigView : UserControl { }
	public class OtherConsolesConfigView : UserControl { }

	// Platform controller views referenced by ControllerConfigViewLocator.axaml.cs
	// (kept compiling the locator's switch; NES-only never instantiates them).
	public class SnesControllerView : UserControl { }
	public class SnesNttDataKeypadControllerView : UserControl { }
	public class GbaControllerView : UserControl { }
	public class PceControllerView : UserControl { }
	public class PceAvenuePad6View : UserControl { }
	public class SmsControllerView : UserControl { }
	public class WsControllerView : UserControl { }
	public class WsControllerVerticalView : UserControl { }
	public class WsPcv2ControllerView : UserControl { }
}

namespace Mesen.Debugger.Views
{
	public class SnesEventViewerConfigView : UserControl { }
	public class GbEventViewerConfigView : UserControl { }
	public class GbaEventViewerConfigView : UserControl { }
	public class PceEventViewerConfigView : UserControl { }
	public class SmsEventViewerConfigView : UserControl { }
	public class WsEventViewerConfigView : UserControl { }
}

namespace Mesen.Debugger.StatusViews
{
	public class Cx4StatusViewModel { }
	public class Cx4StatusView : UserControl { }
	public class GbaStatusViewModel { }
	public class GbaStatusView : UserControl { }
	public class GbStatusViewModel { }
	public class GbStatusView : UserControl { }
	public class GsuStatusViewModel { }
	public class GsuStatusView : UserControl { }
	public class NecDspStatusViewModel { }
	public class NecDspStatusView : UserControl { }
	public class St018StatusViewModel { }
	public class St018StatusView : UserControl { }
	public class PceStatusViewModel { }
	public class PceStatusView : UserControl { }
	public class SmsStatusViewModel { }
	public class SmsStatusView : UserControl { }
	public class WsStatusViewModel { }
	public class WsStatusView : UserControl { }
	public class SnesStatusViewModel { }
	public class SnesStatusView : UserControl { }
	public class SpcStatusViewModel { }
	public class SpcStatusView : UserControl { }
}
#endif
