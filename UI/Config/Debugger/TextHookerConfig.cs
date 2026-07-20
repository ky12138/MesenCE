using Mesen.Interop;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;

namespace Mesen.Config
{
	public class CharMappingEntry
	{
		public string Key { get; set; } = "";
		public string Value { get; set; } = "";
	}

	public partial class TextHookerConfig : BaseWindowConfig<TextHookerConfig>
	{
		[ObservableProperty] public partial RefreshTimingConfig RefreshTiming { get; set; } = new();

		[ObservableProperty] public partial bool IgnoreMirroredNametables { get; set; } = true;
		[ObservableProperty] public partial bool AdjustViewportScrolling { get; set; } = true;
		[ObservableProperty] public partial bool AutoCopyToClipboard { get; set; } = false;
		[ObservableProperty] public partial bool AutoRefresh { get; set; } = true;
		[ObservableProperty] public partial bool RefreshOnBreak { get; set; } = true;

		public List<CharMappingEntry> SavedCharMappings { get; set; } = new();

		public TextHookerConfig()
		{
		}
	}
}
