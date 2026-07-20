using Mesen.Interop;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;

namespace Mesen.Config
{
	public partial class MemorySearchConfig : BaseWindowConfig<MemorySearchConfig>
	{
		[ObservableProperty] public bool ShowToolbar { get; set; } = true;
		[ObservableProperty] public partial List<int> ColumnWidths { get; set; } = new();
	}
}
