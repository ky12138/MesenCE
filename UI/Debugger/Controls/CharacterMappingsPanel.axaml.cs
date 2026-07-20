using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Mesen.Config;
using Mesen.Debugger.ViewModels;
using Mesen.Interop;
using Mesen.Utilities;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Mesen.Debugger.Controls
{
	public class CharacterMappingsPanel : UserControl
	{
		private ComboBox? _chrSelectionComboBox;

		public CharacterMappingsPanel()
		{
			InitializeComponent();
			_chrSelectionComboBox = this.GetControl<ComboBox>("ChrSelectionComboBox");
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
		}

		protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
		{
			if(_chrSelectionComboBox != null && _chrSelectionComboBox.IsPointerOver) {
				//Allow the mouse wheel to change the selected CHR bank even before the ComboBox has been focused
				int count = _chrSelectionComboBox.ItemCount;
				if(count > 0) {
					int delta = e.Delta.Y > 0 ? -1 : 1;
					int newIndex = _chrSelectionComboBox.SelectedIndex + delta;
					if(newIndex >= 0 && newIndex < count) {
						_chrSelectionComboBox.SelectedIndex = newIndex;
						e.Handled = true;
					}
				}
			}
			base.OnPointerWheelChanged(e);
		}

		private async void ExportTbl_Click(object? sender, RoutedEventArgs e)
		{
			TextHookerViewModel? vm = DataContext as TextHookerViewModel;
			if(vm == null) {
				return;
			}

			string romName = EmuApi.GetRomInfo().GetRomName();
			string? filename = await FileDialogHelper.SaveFile(ConfigManager.DebuggerFolder, romName + ".tbl", this.GetWindow(), FileDialogHelper.TblExt);
			if(filename == null) {
				return;
			}

			string tblContent = vm.GenerateTblContent();
			await File.WriteAllTextAsync(filename, tblContent, Encoding.UTF8);
		}
	}
}
