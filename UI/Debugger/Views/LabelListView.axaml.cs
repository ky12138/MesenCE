using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DataBoxControl;
using Mesen.Debugger.Labels;
using Mesen.Debugger.ViewModels;
using Mesen.Debugger.Windows;
using Mesen.Interop;
using System;
using static Mesen.Debugger.ViewModels.LabelListViewModel;

namespace Mesen.Debugger.Views
{
	public class LabelListView : UserControl
	{
		public LabelListView()
		{
			InitializeComponent();
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
		}

		protected override void OnDataContextChanged(EventArgs e)
		{
			if(DataContext is LabelListViewModel model) {
				model.InitContextMenu(this);
			}
			base.OnDataContextChanged(e);
		}

		private void OnCellDoubleClick(DataBoxCell cell)
		{
			if(DataContext is not LabelListViewModel listModel || cell.DataContext is not LabelViewModel label) {
				return;
			}

			string? colName = cell.Column?.ColumnName;
			if(colName == "Label") {
				LabelEditWindow.EditLabel(listModel.CpuType, this, label.Label);
			} else if(colName == "Comment") {
				LabelEditWindow.EditLabel(listModel.CpuType, this, label.Label, focusComment: true);
			} else if(colName == "RelAddr") {
				AddressInfo addr = label.Label.GetRelativeAddress(listModel.CpuType);
				if(addr.Address >= 0) {
					listModel.Debugger.ScrollToAddress(addr.Address);
				}
			} else if(colName == "AbsAddr") {
				AddressInfo addr = label.Label.GetAbsoluteAddress();
				if(addr.Address >= 0) {
					MemoryToolsWindow.ShowInMemoryTools(addr.Type, addr.Address);
				}
			}
		}
	}
}
