using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Mesen.Debugger.ViewModels;

namespace Mesen.Debugger.Controls
{
	public class TextHookerPanel : UserControl
	{
		public TextHookerPanel()
		{
			InitializeComponent();
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
		}
	}
}
