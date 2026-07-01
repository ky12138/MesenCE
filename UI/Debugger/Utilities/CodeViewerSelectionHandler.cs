using Avalonia.Controls;
using Avalonia.Input;
using Mesen.Debugger.Controls;
using Mesen.Debugger.Disassembly;
using Mesen.Debugger.Utilities;
using Mesen.Interop;
using Mesen.Utilities;
using System;

namespace Mesen.Debugger.Utilities
{
	public class CodeViewerSelectionHandler : IDisposable
	{
		private DisassemblyViewer _viewer;
		private ISelectableModel _model;
		private LocationInfo? _mouseOverCodeLocation;
		private LocationInfo? _contextMenuLocation;

		private bool _marginClicked = false;
		private bool _allowMarginClick = false;
		private Func<int, int, int> _getRowAddress;
		private double _scrollAccumulator = 0.0;

		private bool _isMarginDragging = false;
		private bool _marginDragAddBreakpoint = true;
		private bool _marginDragIsMiddleButton = false;
		private int _marginDragLastRow = -1;

		public CodeSegmentInfo? MouseOverSegment { get; private set; }
		public bool IsMarginClick => _marginClicked;

		public CodeViewerSelectionHandler(DisassemblyViewer viewer, ISelectableModel model, Func<int, int, int> getRowAddress, bool allowMarginClick = false)
		{
			_allowMarginClick = allowMarginClick;
			_viewer = viewer;
			_model = model;
			_getRowAddress = getRowAddress;

			_viewer.PointerExited += Viewer_PointerExited;
			_viewer.RowClicked += Viewer_RowClicked;
			_viewer.CodePointerMoved += Viewer_CodePointerMoved;
			_viewer.PointerWheelChanged += Viewer_PointerWheelChanged;
			_viewer.KeyDown += Viewer_KeyDown;
		}

		public void Dispose()
		{
			_viewer.PointerExited -= Viewer_PointerExited;
			_viewer.RowClicked -= Viewer_RowClicked;
			_viewer.CodePointerMoved -= Viewer_CodePointerMoved;
			_viewer.PointerWheelChanged -= Viewer_PointerWheelChanged;
			_viewer.KeyDown -= Viewer_KeyDown;
		}

		private void Viewer_PointerExited(object? sender, PointerEventArgs e)
		{
			_mouseOverCodeLocation = null;
			_isMarginDragging = false;
		}

		public int GetAddress(RowClickedEventArgs e)
		{
			return _getRowAddress(e.RowNumber, e.CodeLineData.Address);
		}

		public int GetAddress(CodePointerMovedEventArgs e)
		{
			return _getRowAddress(e.RowNumber, (e.Data?.Address ?? -1));
		}

		private AddressInfo? GetCodeLineAddress(CodeLineData lineData)
		{
			CpuType cpuType = lineData.CpuType;
			if(lineData.AbsoluteAddress.Address >= 0) {
				return lineData.AbsoluteAddress;
			} else if(lineData.Address >= 0) {
				AddressInfo relAddress = new AddressInfo() {
					Address = lineData.Address,
					Type = cpuType.ToMemoryType()
				};
				AddressInfo absAddress = DebugApi.GetAbsoluteAddress(relAddress);
				return absAddress.Address >= 0 ? absAddress : relAddress;
			}
			return null;
		}

		public void Viewer_RowClicked(DisassemblyViewer sender, RowClickedEventArgs e)
		{
			_marginClicked = e.MarginClicked;

			if(e.Properties.IsLeftButtonPressed || e.Properties.IsMiddleButtonPressed) {
				if(_marginClicked && _allowMarginClick) {
					AddressInfo? address = GetCodeLineAddress(e.CodeLineData);
					if(address.HasValue && address.Value.Address >= 0) {
						CpuType cpuType = e.CodeLineData.CpuType;
						bool isMiddleButton = e.Properties.IsMiddleButtonPressed;

						Breakpoint? existingBp = isMiddleButton
							? BreakpointManager.GetMatchingForbidBreakpoint(address.Value, cpuType)
							: (BreakpointManager.GetMatchingForbidBreakpoint(address.Value, cpuType) ?? BreakpointManager.GetMatchingBreakpoint(address.Value, cpuType, true));

						_isMarginDragging = true;
						_marginDragAddBreakpoint = existingBp == null;
						_marginDragIsMiddleButton = isMiddleButton;
						_marginDragLastRow = e.RowNumber;

						if(isMiddleButton) {
							BreakpointManager.ToggleForbidBreakpoint(address.Value, cpuType);
						} else {
							BreakpointManager.ToggleBreakpoint(address.Value, cpuType);
						}
					}
				} else if(e.Properties.IsLeftButtonPressed) {
					if(e.PointerEvent.KeyModifiers.HasFlag(KeyModifiers.Shift)) {
						_model.ResizeSelectionTo(GetAddress(e));
					} else {
						_model.SetSelectedRow(GetAddress(e));
					}
				}
			} else if(e.Properties.IsRightButtonPressed) {
				_contextMenuLocation = _mouseOverCodeLocation;

				if(!_model.IsSelected(GetAddress(e))) {
					_model.SetSelectedRow(GetAddress(e));
				}
			}
		}

		public void Viewer_CodePointerMoved(DisassemblyViewer sender, CodePointerMovedEventArgs e)
		{
			DynamicTooltip? tooltip = null;

			if(_viewer.ContextMenu?.IsOpen != true) {
				MouseOverSegment = e.CodeSegment;
			}

			if(e.CodeSegment != null && e.Data != null) {
				_mouseOverCodeLocation = CodeTooltipHelper.GetLocation(e.Data.CpuType, e.CodeSegment);
				tooltip = CodeTooltipHelper.GetTooltip(e.Data.CpuType, e.CodeSegment);
				if(tooltip == null && e.Fragment != null && !string.IsNullOrWhiteSpace(e.Fragment.Text)) {
					//No tooltip was found, try again using the word under the mouse cursor
					//This is only useful for source view, since the syntax doesn't always
					//match the format expected by the color highlighting logic, etc.
					LocationInfo? codeLoc = CodeTooltipHelper.GetLocation(e.Data.CpuType, new CodeSegmentInfo(e.Fragment.Text, CodeSegmentType.Label, default, e.Data));
					if(codeLoc != null && (codeLoc.RelAddress?.Address >= 0 || codeLoc.AbsAddress?.Address >= 0 || codeLoc.Label != null || codeLoc.Symbol != null)) {
						tooltip = CodeTooltipHelper.GetCodeAddressTooltip(e.Data.CpuType, codeLoc, !codeLoc.RelAddress.HasValue || codeLoc.RelAddress?.Address < 0);
					}
				}

				if(tooltip != null) {
					TooltipHelper.ShowTooltip(_viewer, tooltip, 15);
				}
			} else {
				_mouseOverCodeLocation = null;
			}

			if(tooltip == null) {
				TooltipHelper.HideTooltip(_viewer);
			}

			if(!_marginClicked && GetAddress(e) >= 0 && e.PointerEvent.GetCurrentPoint(null).Properties.IsLeftButtonPressed) {
				_model.ResizeSelectionTo(GetAddress(e));
			}

			if(_isMarginDragging) {
				bool buttonPressed = _marginDragIsMiddleButton
					? e.PointerEvent.GetCurrentPoint(null).Properties.IsMiddleButtonPressed
					: e.PointerEvent.GetCurrentPoint(null).Properties.IsLeftButtonPressed;

				if(!buttonPressed) {
					_isMarginDragging = false;
				} else if(e.RowNumber >= 0 && e.Data != null && e.RowNumber != _marginDragLastRow) {
					AddressInfo? address = GetCodeLineAddress(e.Data);
					if(address.HasValue && address.Value.Address >= 0) {
						CpuType cpuType = e.Data.CpuType;
						if(_marginDragIsMiddleButton) {
							Breakpoint? existing = BreakpointManager.GetMatchingForbidBreakpoint(address.Value, cpuType);
							if(_marginDragAddBreakpoint && existing == null) {
								BreakpointManager.ToggleForbidBreakpoint(address.Value, cpuType);
							} else if(!_marginDragAddBreakpoint && existing != null) {
								BreakpointManager.ToggleForbidBreakpoint(address.Value, cpuType);
							}
						} else {
							Breakpoint? existingForbid = BreakpointManager.GetMatchingForbidBreakpoint(address.Value, cpuType);
							Breakpoint? existingBp = existingForbid ?? BreakpointManager.GetMatchingBreakpoint(address.Value, cpuType, true);
							if(_marginDragAddBreakpoint && existingBp == null) {
								BreakpointManager.ToggleBreakpoint(address.Value, cpuType);
							} else if(!_marginDragAddBreakpoint && existingBp != null) {
								BreakpointManager.ToggleBreakpoint(address.Value, cpuType);
							}
						}
					}
					_marginDragLastRow = e.RowNumber;
				}
			}
		}

		public void Viewer_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
		{
			_scrollAccumulator += -e.GetDeltaY() * 3;
			_model.Scroll((int)_scrollAccumulator);
			_scrollAccumulator -= (int)_scrollAccumulator;
		}

		private void Viewer_KeyDown(object? sender, KeyEventArgs e)
		{
			bool shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
			switch(e.Key) {
				case Key.PageDown: _model.MoveCursor(_model.VisibleRowCount - 2, shift); e.Handled = true; break;
				case Key.PageUp: _model.MoveCursor(-(_model.VisibleRowCount - 2), shift); e.Handled = true; break;
				case Key.Home: _model.ScrollToTop(shift); e.Handled = true; break;
				case Key.End: _model.ScrollToBottom(shift); e.Handled = true; break;

				case Key.Up: _model.MoveCursor(-1, shift); e.Handled = true; break;
				case Key.Down: _model.MoveCursor(1, shift); e.Handled = true; break;
			}
		}

		public LocationInfo ActionLocation
		{
			get
			{
				if(_marginClicked && _allowMarginClick) {
					return GetSelectedRowLocation();
				} else if(_viewer.ContextMenu?.IsOpen == true && _contextMenuLocation != null) {
					return _contextMenuLocation;
				} else if(_mouseOverCodeLocation != null) {
					return _mouseOverCodeLocation;
				}
				return GetSelectedRowLocation();
			}
		}

		private LocationInfo GetSelectedRowLocation()
		{
			AddressInfo? rowAddress = _model.GetSelectedRowAddress();

			AddressInfo? relAddress = null;
			AddressInfo? absAddress = null;

			if(rowAddress != null && rowAddress?.Address >= 0) {
				if(rowAddress.Value.Type.IsRelativeMemory()) {
					relAddress = rowAddress;
					absAddress = DebugApi.GetAbsoluteAddress(relAddress.Value);
				} else {
					absAddress = rowAddress;
					relAddress = DebugApi.GetRelativeAddress(absAddress.Value, absAddress.Value.Type.ToCpuType());
				}
			}

			return new LocationInfo() {
				RelAddress = relAddress?.Address >= 0 ? relAddress : null,
				AbsAddress = absAddress?.Address >= 0 ? absAddress : null
			};
		}
	}
}
