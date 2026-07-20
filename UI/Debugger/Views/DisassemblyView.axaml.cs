using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Dock.Avalonia.Controls;
using Mesen.Config;
using Mesen.Debugger.Controls;
using Mesen.Debugger.Disassembly;
using Mesen.Debugger.Labels;
using Mesen.Debugger.Utilities;
using Mesen.Debugger.ViewModels;
using Mesen.Debugger.ViewModels.DebuggerDock;
using Mesen.Debugger.Windows;
using Mesen.Interop;
using Mesen.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mesen.Debugger.Views
{
	public class DisassemblyView : MesenUserControl
	{
		private DisassemblyViewModel Model => _model!;
		private CpuType CpuType => Model.CpuType;
		private LocationInfo ActionLoc => _selectionHandler?.ActionLocation ?? new LocationInfo();
		private bool IsMarginClick => _selectionHandler?.IsMarginClick ?? false;

		private DisassemblyViewModel? _model;
		private CodeViewerSelectionHandler? _selectionHandler;
		private DisassemblyViewer _viewer;
		private BaseToolContainerViewModel? _parentModel;

		public DisassemblyView()
		{
			InitializeComponent();

			_viewer = this.GetControl<DisassemblyViewer>("disViewer");

			AddDisposable(_viewer.ObserveProp(DisassemblyViewer.VisibleRowCountProperty, x => {
				int rowCount = _viewer.VisibleRowCount;
				int prevCount = Model.VisibleRowCount;
				if(prevCount != rowCount) {
					Model.VisibleRowCount = rowCount;
					if(prevCount < rowCount) {
						Model.Refresh();
					}
				}
			}));

			InitContextMenu();
		}

		protected override void OnDataContextChanged(EventArgs e)
		{
			if(DataContext is DisassemblyViewModel model && _model != model) {
				_model = model;
				_model.SetViewer(_viewer);
				_selectionHandler = new CodeViewerSelectionHandler(_viewer, _model, (rowIndex, rowAddress) => rowAddress, true);
			}
			base.OnDataContextChanged(e);
		}

		private void InitContextMenu()
		{
			List<ContextMenuAction> actions = new List<ContextMenuAction> {
				MarkSelectionHelper.GetAction(
					() => CpuType.ToMemoryType(),
					() => Model.SelectionStart,
					() => Model.SelectionEnd,
					() => Model.Refresh(),
					() => !IsMarginClick
				),
				new ContextMenuAction() {
					ActionType = ActionType.EditSelectedCode,
					Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.CodeWindow_EditSelectedCode),
					IsVisible = () => !IsMarginClick,
					IsEnabled = () => CpuType.SupportsAssembler() && EmuApi.IsPaused(),
					OnClick = () => {
						string code = Model.GetSelection(false, false, true, false, out int byteCount, true);
						AssemblerWindow.EditCode(CpuType, Model.SelectionStart, code, byteCount);
					}
				},
				new ContextMenuAction() {
					ActionType = ActionType.Undo,
					IsEnabled = () => DebugApi.HasUndoHistory(),
					IsVisible = () => !IsMarginClick,
					Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.Undo),
					OnClick = () => {
						if(DebugApi.HasUndoHistory()) {
							DebugApi.PerformUndo();
							Model.Debugger.UpdateDisassembly(false);
						}
					}
				},
				new ContextMenuSeparator() { IsVisible = () => !IsMarginClick },
				new ContextMenuAction() {
					ActionType = ActionType.Copy,
					Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.Copy),
					IsVisible = () => !IsMarginClick,
					OnClick = () => Model.CopySelection()
				},
				new ContextMenuSeparator() { IsVisible = () => !IsMarginClick },
				new ContextMenuAction() {
					ActionType = ActionType.ToggleBreakpoint,
					Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.CodeWindow_ToggleBreakpoint),
					HintText = () => GetRangeHintText(),
					IsVisible = () => !IsMarginClick && (Model.SelectionStart != Model.SelectionEnd || ActionLoc.RelAddress != null),
					IsEnabled = () => Model.SelectionStart != Model.SelectionEnd || ActionLoc.RelAddress != null,
					OnClick = () => {
						if(ActionLoc.RelAddress != null && Model.SelectionStart != Model.SelectionEnd) {
							uint range = (uint)Math.Abs(Model.SelectionStart - Model.SelectionEnd);
							if(IsRelAddrHigh()) {
								BreakpointManager.EditBreakpointAtRange(ActionLoc.RelAddress.Value,range,CpuType,this,true);
							} else {
								BreakpointManager.EditBreakpointAtRange(ActionLoc.RelAddress.Value,range,CpuType,this);
							}
						} else if(ActionLoc.RelAddress != null) {
							BreakpointManager.EditBreakpointAtAddress(ActionLoc.RelAddress.Value, CpuType, this);
						}
					}
				},
				new ContextMenuAction() {
					ActionType = ActionType.ToggleBreakpoint,
					Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.CodeWindow_ToggleBreakpointAbsAddr),
					HintText = () => GetRangeHintText(true),
					IsVisible = () => !IsMarginClick && (Model.SelectionStart != Model.SelectionEnd || ActionLoc.AbsAddress != null) &&
							ActionLoc.AbsAddress != null && ActionLoc.RelAddress != null &&
							ActionLoc.AbsAddress.Value.Type != ActionLoc.RelAddress.Value.Type,
					IsEnabled = () => Model.SelectionStart != Model.SelectionEnd || ActionLoc.AbsAddress != null,
					OnClick = () => {
						if(ActionLoc.AbsAddress != null) {
							if(Model.SelectionStart != Model.SelectionEnd) {
								uint range = (uint)Math.Abs(Model.SelectionStart - Model.SelectionEnd);
								if(IsRelAddrHigh()) {
									BreakpointManager.EditBreakpointAtRange(ActionLoc.AbsAddress.Value, range, CpuType, this, true);
								} else {
									BreakpointManager.EditBreakpointAtRange(ActionLoc.AbsAddress.Value, range, CpuType, this);
								}
							} else {
								BreakpointManager.EditBreakpointAtAddress(ActionLoc.AbsAddress.Value, CpuType, this);
							}
						}
					}
				},
				new ContextMenuSeparator() { IsVisible = () => !IsMarginClick },
				new ContextMenuAction() {
					ActionType = ActionType.AddWatch,
					Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.CodeWindow_AddToWatch),
					HintText = () => GetHintText(),
					IsVisible = () => !IsMarginClick,
					IsEnabled = () => ActionLoc.Label != null || ActionLoc.RelAddress != null,
					OnClick = () => {
						if(ActionLoc.Label != null) {
							if(ActionLoc.LabelAddressOffset != null) {
								WatchManager.GetWatchManager(CpuType).AddWatch($"[{ActionLoc.Label.Label}+{ActionLoc.LabelAddressOffset}]");
							} else {
								WatchManager.GetWatchManager(CpuType).AddWatch("[" + ActionLoc.Label.Label + "]");
							}
						} else if(ActionLoc.RelAddress != null) {
							WatchManager.GetWatchManager(CpuType).AddWatch("[$" + ActionLoc.RelAddress.Value.Address.ToString(GetFormatString()) + "]");
						}
					}
				},
				new ContextMenuAction() {
					ActionType = ActionType.EditLabel,
					Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.CodeWindow_EditLabel),
					HintText = () => GetHintText(),
					IsVisible = () => !IsMarginClick,
					IsEnabled = () => ActionLoc.Label != null || ActionLoc.AbsAddress != null || (ActionLoc.RelAddress != null && ActionLoc.RelAddress.Value.Type.SupportsLabels()),
					OnClick = () => {
						CodeLabel? label = ActionLoc.Label ?? (ActionLoc.AbsAddress.HasValue ? LabelManager.GetLabel(ActionLoc.AbsAddress.Value) : null);
						if(label != null) {
							LabelEditWindow.EditLabel(CpuType, this, label);
						} else if(ActionLoc.AbsAddress != null) {
							LabelEditWindow.EditLabel(CpuType, this, new CodeLabel(ActionLoc.AbsAddress.Value));
						} else if(ActionLoc.RelAddress != null) {
							LabelEditWindow.EditLabel(CpuType, this, new CodeLabel(ActionLoc.RelAddress.Value));
						}
					}
				},
				new ContextMenuAction() {
					ActionType = ActionType.EditComment,
					Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.CodeWindow_EditComment),
					HintText = () => GetHintText(),
					IsVisible = () => !IsMarginClick,
					AllowedWhenHidden = true,
					IsEnabled = () => ActionLoc.Label != null || ActionLoc.AbsAddress != null || (ActionLoc.RelAddress != null && ActionLoc.RelAddress.Value.Type.SupportsLabels()),
					OnClick = () => {
						CodeLabel? label = ActionLoc.Label ?? (ActionLoc.AbsAddress.HasValue ? LabelManager.GetLabel(ActionLoc.AbsAddress.Value) : null);
						if(label != null) {
							CommentEditWindow.EditComment(this, label);
						} else if(ActionLoc.AbsAddress != null) {
							CommentEditWindow.EditComment(this, new CodeLabel(ActionLoc.AbsAddress.Value));
						}else if(ActionLoc.RelAddress != null) {
							CommentEditWindow.EditComment(this, new CodeLabel(ActionLoc.RelAddress.Value));
						}
					}
				},
				new ContextMenuSeparator() { IsVisible = () => !IsMarginClick },
				new ContextMenuAction() {
					ActionType = ActionType.ViewInMemoryViewer,
					Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.CodeWindow_ViewInMemoryViewer),
					HintText = () => GetHintText(),
					IsVisible = () => !IsMarginClick && ActionLoc.RelAddress != null,
					IsEnabled = () => ActionLoc.RelAddress != null,
					OnClick = () => {
						if(ActionLoc.RelAddress != null) {
							MemoryToolsWindow.ShowInMemoryTools(ActionLoc.RelAddress.Value.Type, ActionLoc.RelAddress.Value.Address);
						}
					}
				},
				new ContextMenuAction() {
					ActionType = ActionType.ViewInMemoryViewer,
					Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.CodeWindow_ViewInMemoryViewerAbsAddr),
					HintText = () => GetHintText(true),
					IsVisible = () => !IsMarginClick && ActionLoc.AbsAddress != null && ActionLoc.RelAddress != null && ActionLoc.AbsAddress.Value.Type != ActionLoc.RelAddress.Value.Type,
					IsEnabled = () => ActionLoc.AbsAddress != null,
					OnClick = () => {
						if(ActionLoc.AbsAddress != null) {
							MemoryToolsWindow.ShowInMemoryTools(ActionLoc.AbsAddress.Value.Type, ActionLoc.AbsAddress.Value.Address);
						}
					}
				},
				new ContextMenuSeparator() { IsVisible = () => !IsMarginClick },
				new ContextMenuAction() {
					ActionType = ActionType.FindOccurrences,
					HintText = () => GetSearchString() ?? "",
					IsVisible = () => !IsMarginClick,
					IsEnabled = () => GetSearchString() != null,
					OnClick = () => {
						if(_model != null) {
							string? searchString = GetSearchString();
							if(searchString != null) {
								DisassemblySearchOptions options = new() { MatchWholeWord = true, MatchCase = true };
								_model.Debugger.FindAllOccurrences(searchString, options);
							}
						}
					}
				},
				new ContextMenuSeparator() { IsVisible = () => !IsMarginClick },
				new ContextMenuAction() {
					ActionType = ActionType.MoveProgramCounter,
					Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.CodeWindow_MoveProgramCounter),
					HintText = () => GetHintText(),
					IsVisible = () => !IsMarginClick,
					IsEnabled = () => ActionLoc.RelAddress != null && DebugApi.GetDebuggerFeatures(CpuType).ChangeProgramCounter,
					OnClick = () => {
						if(ActionLoc.RelAddress != null) {
							Model.Debugger.UpdateConsoleState();
							DebugApi.SetProgramCounter(CpuType, (uint)ActionLoc.RelAddress.Value.Address);
							Model.Debugger.ConsoleStatus?.UpdateUiState();
							Model.Debugger.UpdateDisassembly(true);
						}
					}
				},
				new ContextMenuAction() {
					ActionType = ActionType.RunToLocation,
					Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.CodeWindow_RunToLocation),
					HintText = () => GetHintText(),
					IsVisible = () => !IsMarginClick,
					IsEnabled = () => ActionLoc.RelAddress != null || ActionLoc.AbsAddress != null,
					OnClick = () => {
						Model.Debugger.RunToLocation(ActionLoc);
					}
				},
				new ContextMenuSeparator() { IsVisible = () => !IsMarginClick },
				new ContextMenuAction() {
					ActionType = ActionType.GoToLocation,
					Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.CodeWindow_GoToLocation),
					HintText = () => GetHintText(),
					IsVisible = () => !IsMarginClick,
					IsEnabled = () => ActionLoc.RelAddress != null,
					OnClick = () => {
						if(ActionLoc.RelAddress != null) {
							Model.SetSelectedRow(ActionLoc.RelAddress.Value.Address, true, true);
						}
					}
				},
				new ContextMenuAction() {
					ActionType = ActionType.LocateInFunctionList,
					Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.CodeWindow_GoToLocation),
					HintText = () => GetHintText(),
					IsVisible = () => !IsMarginClick,
					IsEnabled = () => ActionLoc.RelAddress != null && Model.Debugger.FunctionList != null && FindFunctionForAddress() != null,
				OnClick = () => {
					FunctionNode? func = FindFunctionForAddress();
						if(func != null && Model.Debugger.FunctionList != null) {
							Model.Debugger.FunctionList.Selection.SelectedItem = func;
						}
					}
				},
				new ContextMenuSeparator() { IsVisible = () => !IsMarginClick },
				new ContextMenuAction() {
					ActionType = ActionType.NavigateBack,
					Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.CodeWindow_NavigateBack),
					IsVisible = () => !IsMarginClick,
					IsEnabled = () => Model.History.CanGoBack(),
					OnClick = () => {
						Model.GoBack();
					}
				},
				new ContextMenuAction() {
					ActionType = ActionType.NavigateForward,
					Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.CodeWindow_NavigateForward),
					IsVisible = () => !IsMarginClick,
					IsEnabled = () => Model.History.CanGoForward(),
					OnClick = () => {
						Model.GoForward();
					}
				},
			};

			actions.AddRange(GetBreakpointContextMenu());
			AddDisposables(DebugShortcutManager.CreateContextMenu(_viewer, actions));
		}

		private string? GetSearchString()
		{
			CodeSegmentInfo? segment = _selectionHandler?.MouseOverSegment;
			if(segment == null || !AllowSearch(segment.Type)) {
				if(ActionLoc.RelAddress?.Address >= 0) {
					CodeLabel? label = LabelManager.GetLabel(ActionLoc.RelAddress.Value);
					return label?.Label ?? ("$" + ActionLoc.RelAddress.Value.Address.ToString("X" + CpuType.GetAddressSize()));
				}
				return null;
			}
			return segment.Text.Trim(' ', '[', ']', '=', ':', '.', '+');
		}

		private bool AllowSearch(CodeSegmentType? type)
		{
			if(type == null) {
				return false;
			}

			switch(type) {
				case CodeSegmentType.OpCode:
				case CodeSegmentType.Token:
				case CodeSegmentType.Address:
				case CodeSegmentType.Label:
				case CodeSegmentType.ImmediateValue:
				case CodeSegmentType.LabelDefinition:
				case CodeSegmentType.EffectiveAddress:
					return true;

				default:
					return false;
			}
		}

		private List<ContextMenuAction> GetBreakpointContextMenu()
		{
			Breakpoint? GetBreakpoint()
			{
				return ActionLoc.AbsAddress != null ? BreakpointManager.GetMatchingBreakpoint(ActionLoc.AbsAddress.Value, CpuType, true) : null;
			}

			return new List<ContextMenuAction>() {
				new ContextMenuAction() {
					ActionType = ActionType.SetBreakpoint,
					HintText = () => GetHintText(),
					IsVisible = () => GetBreakpoint() == null && IsMarginClick,
					OnClick = () => {
						if(ActionLoc.AbsAddress != null) {
							BreakpointManager.ToggleBreakpoint(ActionLoc.AbsAddress.Value, CpuType);
						}
					}
				},
				new ContextMenuSeparator() { IsVisible = () => GetBreakpoint() == null && IsMarginClick },
				new ContextMenuAction() {
					ActionType = ActionType.CodeWindowEditBreakpoint,
					HintText = () => GetHintText(),
					IsVisible =() => IsMarginClick,
					IsEnabled = () => GetBreakpoint() != null,
					OnClick = () => {
						if(GetBreakpoint() is Breakpoint bp) {
							BreakpointEditWindow.EditBreakpoint(bp, this);
						}
					}
				},
				new ContextMenuAction() {
					ActionType = ActionType.EnableBreakpoint,
					HintText = () => GetHintText(),
					IsVisible = () => GetBreakpoint()?.Enabled == false && IsMarginClick,
					IsEnabled = () => GetBreakpoint()?.Enabled == false,
					OnClick = () => {
						if(ActionLoc.AbsAddress != null) {
							BreakpointManager.EnableDisableBreakpoint(ActionLoc.AbsAddress.Value, CpuType);
						}
					}
				},
				new ContextMenuAction() {
					ActionType = ActionType.DisableBreakpoint,
					HintText = () => GetHintText(),
					IsVisible = () => GetBreakpoint()?.Enabled != false && IsMarginClick,
					IsEnabled = () => GetBreakpoint()?.Enabled == true,
					OnClick = () => {
						if(ActionLoc.AbsAddress != null) {
							BreakpointManager.EnableDisableBreakpoint(ActionLoc.AbsAddress.Value, CpuType);
						}
					}
				},
				new ContextMenuSeparator() { IsVisible = () => GetBreakpoint() != null && IsMarginClick },
				new ContextMenuAction() {
					ActionType = ActionType.RemoveBreakpoint,
					HintText = () => GetHintText(),
					IsVisible = () => GetBreakpoint() != null && IsMarginClick,
					OnClick = () => {
						if(ActionLoc.AbsAddress != null) {
							BreakpointManager.ToggleBreakpoint(ActionLoc.AbsAddress.Value, CpuType);
						}
					}
				},
				new ContextMenuSeparator() { IsVisible = () => IsMarginClick },
				new ContextMenuAction() {
					ActionType = ActionType.ToggleForbidBreakpoint,
					HintText = () => GetHintText(),
					IsVisible = () => IsMarginClick,
					OnClick = () => {
						if(ActionLoc.AbsAddress != null) {
							BreakpointManager.ToggleForbidBreakpoint(ActionLoc.AbsAddress.Value, CpuType);
						}
					}
				},
			};
		}

		private FunctionNode? FindFunctionForAddress()
		{
			if(Model.Debugger.FunctionList == null || ActionLoc.RelAddress == null) {
				return null;
			}

			int address = ActionLoc.RelAddress.Value.Address;
			var functions = Model.Debugger.FunctionList.Functions;

			FunctionNode? bestMatch = null;
			foreach(var func in functions) {
				if(func.FuncRelAddr.Address >= 0 && func.FuncRelAddr.Address <= address) {
					if(bestMatch == null || func.FuncRelAddr.Address > bestMatch.FuncRelAddr.Address) {
						bestMatch = func;
					}
				}
			}

			return bestMatch;
		}

		private string GetFormatString()
		{
			return CpuType.ToMemoryType().GetFormatString();
		}

		private string GetHintText(bool isAbs = false)
		{
			if(ActionLoc?.Label != null && ActionLoc.AbsAddress != null) {
				return MemoryHelper.GetFunctionName(ActionLoc.AbsAddress.Value, true);
			}
			if(isAbs && ActionLoc?.AbsAddress != null) {
				return MemoryHelper.GetAddrStr(ActionLoc.AbsAddress.Value);
			} else if(ActionLoc?.RelAddress != null && ActionLoc.RelAddress.Value.Address >= 0) {
				return MemoryHelper.GetAddrStr(ActionLoc.RelAddress.Value);
			}
			return "";
		}

		private string GetRangeHintText(bool isAbs = false)
		{
			if(Model.SelectionStart == Model.SelectionEnd) {
				return GetHintText(isAbs);
			}
			if(ActionLoc == null || ActionLoc.RelAddress == null) {
				return "";
			}
			uint range = (uint)Math.Abs(Model.SelectionStart - Model.SelectionEnd);

			if(isAbs && ActionLoc.AbsAddress != null) {
				return IsRelAddrHigh()
					? MemoryHelper.GetAddrRangeStr(ActionLoc.AbsAddress.Value, range, true)
					: MemoryHelper.GetAddrRangeStr(ActionLoc.AbsAddress.Value, range);
			} else if(ActionLoc.RelAddress.Value.Address >= 0) {
				return IsRelAddrHigh()
					? MemoryHelper.GetAddrRangeStr(ActionLoc.RelAddress.Value, range, true)
					: MemoryHelper.GetAddrRangeStr(ActionLoc.RelAddress.Value, range);
			}
			return "";
		}

		private bool IsRelAddrHigh()
		{
			if(ActionLoc == null) {
				return false;
			}
			return ActionLoc.RelAddress != null
				&& ActionLoc.RelAddress.Value.Address == Math.Max(Model.SelectionStart, Model.SelectionEnd);
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
		}

		protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
		{
			if(this.FindLogicalAncestorOfType<DockableControl>()?.DataContext is BaseToolContainerViewModel parentModel) {
				_parentModel = parentModel;
				_parentModel.Selected += Parent_Selected;
			}

			_model?.SetViewer(_viewer);
			FocusViewer();
		}

		protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
		{
			if(_parentModel != null) {
				_parentModel.Selected -= Parent_Selected;
				_parentModel = null;
			}

			_model?.SetViewer(null);
		}

		protected override void OnPointerPressed(PointerPressedEventArgs e)
		{
			base.OnPointerPressed(e);

			//Navigate on double-click left click
			if(e.Source is DisassemblyViewer) {
				PointerPointProperties props = e.GetCurrentPoint(this).Properties;
				if(_selectionHandler?.IsMarginClick == false && ActionLoc.RelAddress != null && props.IsLeftButtonPressed && e.ClickCount == 2) {
					Model.SetSelectedRow(ActionLoc.RelAddress.Value.Address, true, true);

					FunctionNode? func = FindFunctionForAddress();
					if(func != null && Model.Debugger.FunctionList != null) {
						Model.Debugger.FunctionList.Selection.SelectedItem = func;
					}
				} else if(props.IsXButton1Pressed) {
					Model.GoBack();
				} else if(props.IsXButton2Pressed) {
					Model.GoForward();
				}
			}
		}

		private void FocusViewer()
		{
			Dispatcher.UIThread.Post(() => {
				//Focus disassembly view when selected by code
				if(_viewer.IsParentWindowFocused()) {
					_viewer.Focus();
				}
			});
		}

		private void Parent_Selected(object? sender, EventArgs e)
		{
			this.FocusViewer();
		}
	}
}
