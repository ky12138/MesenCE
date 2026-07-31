using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using DataBoxControl;
using Mesen.Debugger.ViewModels;
using System;

namespace Mesen.Debugger.Views
{
	public class CpuRegisterAccessView : UserControl
	{
		public CpuRegisterAccessView()
		{
			InitializeComponent();
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
		}

		protected override void OnDataContextChanged(EventArgs e)
		{
			if(DataContext is CpuRegisterAccessViewModel vm) {
				vm.InitContextMenu(this);
			}
			base.OnDataContextChanged(e);
		}

		private void OnCellDoubleClick(DataBoxCell cell)
		{
			if(DataContext is CpuRegisterAccessViewModel listModel && cell.DataContext is RegWriteInfo entry) {
				listModel.GoToEntry(entry);
			}
		}
	}
}
