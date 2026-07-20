using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DataBoxControl;
using Mesen.Debugger.Utilities;
using Mesen.Debugger.ViewModels;
using Mesen.Interop;
using System;
using System.Linq;

namespace Mesen.Debugger.Windows
{
	public class IpsPatchWindow : MesenWindow
	{
		private IpsPatchViewModel _model;

		public IpsPatchWindow()
		{
			InitializeComponent();

			_model = new IpsPatchViewModel(this);
			DataContext = _model;
			GotFocus += (_, _) => _model.RefreshLabels();
		}

		public static IpsPatchWindow Open()
		{
			return DebugWindowManager.OpenDebugWindow(() => new IpsPatchWindow());
		}

		protected override void OnOpened(EventArgs e)
		{
			base.OnOpened(e);
		}

		protected override void OnDataContextChanged(EventArgs e)
		{
			if(DataContext is IpsPatchViewModel vm) {
				vm.InitContextMenu(this);
			}
			base.OnDataContextChanged(e);
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
		}

		private void OnCellDoubleClick(DataBoxCell cell)
		{
			if(cell.DataContext is IpsRecordViewModel record) {
				string? colName = cell.Column?.ColumnName;
				if(colName == "Address" || colName == "TargetOffset" || colName == "Index" || colName == "Memory") {
					var sameMemoryRecords = _model.ParsedRecords.Where(r => r.TargetMemory == record.TargetMemory).ToList();
					MemoryToolsWindow wnd = DebugWindowManager.GetOrOpenDebugWindow(() => new MemoryToolsWindow());
					wnd.SetCursorPositionWithIpsHighlight(record.TargetMemory, record.TargetOffset, sameMemoryRecords);
				} else if(colName == "Preview" || colName == "Type") {
					_model.EditRecordWithAssembler(record);
				} else if(colName == "Label") {
					_model.EditLabelForRecord();
				}
			}
		}
	}
}
