namespace Wfc.Entities.Ui.SettingsUI;

using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Event;
using Wfc.Core.Input;
using Wfc.Entities.Ui.SettingsUI.Grid;
using Wfc.Screens.MenuManager;

// Manages focus navigation for the settings menu.
// Handles:
// - Up/Down arrow keys to navigate between rows
// - Left/Right arrow keys to navigate between tabs (when not in edit mode)
// - Edit mode blocking for UISelect/UISlider controls
[Meta(typeof(IAutoNode))]
public partial class SettingsFocusManager : Node {
    #region Dependencies
    public override void _Notification(int what) => this.Notify(what);

    [Dependency]
    public IInputManager InputManager => this.DependOn<IInputManager>();
    [Dependency]
    public IEventHandler EventHandler => this.DependOn<IEventHandler>();
    #endregion Dependencies

    #region Signals
    // withFocusOnPanelTab indicates whether to focus the tab button instead of the first row
    // this is useful when switching tabs via left/right navigation
    [Signal]
    public delegate void TabNavigationRequestedEventHandler(int direction);
    #endregion Signals

    private List<Control> _currentRows = new();
    private int _currentRowIndex = 0;
    private bool _isInEditMode = false;
    private bool _shouldFocusOnPanelTab = false;
    private Control? _currentFocusedItem = null;
    private IEditableControl? _currentEditableControl = null;

    // Sets the list of focusable rows for the current panel. Called when switching tabs/panels.
    public void SetFocusableRows(Button currentPanelButton, List<UIGridRow> rows) {
        DisconnectFromRows();
        _currentRows = rows.ConvertAll(row => (Control)row);
        _currentRows.Insert(0, currentPanelButton);
        ConnectToRows();

        // Determine focus index: 0 for panel tab (if requested or no rows), 1 for first row
        int focusIndex = _shouldFocusOnPanelTab || _currentRows.Count <= 1 ? 0 : 1;
        FocusRow(focusIndex);
    }

    public int CurrentRowIndex => _currentRowIndex;
    public int RowCount => _currentRows.Count;

    public override void _Ready() {
        base._Ready();
        ProcessMode = ProcessModeEnum.Always;
    }

    public override void _Input(InputEvent @event) {


        if (_isInEditMode) {
            GD.Print("[SettingsFocusManager] In edit mode, input handling deferred to editable control");
            return;
        }

        // handle menu tabs navigation
        if (InputManager.IsEventActionJustPressed(IInputManager.Action.UITabNext, @event)) {
            if (_isInEditMode) {
                _currentEditableControl?.setEditing(false);
                GetViewport().SetInputAsHandled();
                return;
            }
            else {
                _shouldFocusOnPanelTab = false;
                EmitSignal(SignalName.TabNavigationRequested, 1);
                GetViewport().SetInputAsHandled();
                return;
            }
        }
        else if (InputManager.IsEventActionJustPressed(IInputManager.Action.UITabPrevious, @event)) {
            if (_isInEditMode) {
                _currentEditableControl?.setEditing(false);
            }
            else {
                _shouldFocusOnPanelTab = false;
                EmitSignal(SignalName.TabNavigationRequested, -1);
                GetViewport().SetInputAsHandled();
                return;
            }
        }

        if (!_isInEditMode) {
            if (InputManager.IsEventActionJustPressed(IInputManager.Action.UIUp, @event)) {
                NavigateUp();
                GetViewport().SetInputAsHandled();
                return;
            }
            else if (InputManager.IsEventActionJustPressed(IInputManager.Action.UIDown, @event)) {
                NavigateDown();
                GetViewport().SetInputAsHandled();
                return;
            }
        }
        else {
            _currentEditableControl?.setEditing(false);
            GetViewport().SetInputAsHandled();
            return;
        }

        // Handle left/right for tab navigation (only when on tab level)
        if (_currentRowIndex == 0) {
            if (InputManager.IsEventActionJustPressed(IInputManager.Action.UILeft, @event)) {
                _shouldFocusOnPanelTab = true;
                EmitSignal(SignalName.TabNavigationRequested, -1);
                GetViewport().SetInputAsHandled();
                return;
            }
            else if (InputManager.IsEventActionJustPressed(IInputManager.Action.UIRight, @event)) {
                _shouldFocusOnPanelTab = true;
                EmitSignal(SignalName.TabNavigationRequested, 1);
                GetViewport().SetInputAsHandled();
                return;
            }
        }

        if (InputManager.IsEventActionJustPressed(IInputManager.Action.UICancel, @event)) {
            GD.Print("[SettingsFocusManager] Back navigation requested");
            EventHandler.EmitMenuActionPressed(MenuAction.GoBack);
            GetViewport().SetInputAsHandled();
            return;
        }

        // if (_isInEditMode
        // || (InputManager.IsEventActionJustPressed(IInputManager.Action.UIConfirm, @event) && _currentFocusedItem is IEditableControl)) {
        //     // In edit mode or confirm pressed - let the current editable control handle input
        //     return;
        // }

        // GetViewport().SetInputAsHandled();
    }

    private void NavigateUp() {
        if (_currentRows.Count == 0)
            return;

        int newIndex = _currentRowIndex - 1;
        if (newIndex < 0) {
            newIndex = _currentRows.Count - 1; // Wrap to bottom
        }
        FocusRow(newIndex);
    }

    private void NavigateDown() {
        if (_currentRows.Count == 0)
            return;

        int newIndex = (_currentRowIndex + 1) % _currentRows.Count;
        if (newIndex == 0) {
            newIndex = 1; // Skip panel tab when navigating down
        }
        FocusRow(newIndex);
    }

    private void FocusRow(int index) {
        if (index < 0 || index >= _currentRows.Count)
            return;

        _currentRowIndex = index;
        var row = _currentRows[index];

        // Special case: focusing the panel tab (index 0)
        if (index == 0) {
            row.GrabFocus();
            GD.Print("[SettingsFocusManager] Focusing panel tab");
        }
        else {
            var focusableControl = (row as UIGridRow)?.GetFocusableControl();
            if (focusableControl != null) {
                _currentFocusedItem = focusableControl;
                focusableControl.GrabFocus();
                GD.Print($"[SettingsFocusManager] Focused row {index}: {focusableControl.Name}");
            }
        }
    }

    // Refocuses the current row. Called when regaining focus on the settings panel.
    public void RefocusCurrentRow() {
        if (_currentRows.Count > 0) {
            FocusRow(_currentRowIndex);
        }
    }

    private void ConnectToRows() {
        foreach (var row in _currentRows) {
            var control = (row as UIGridRow)?.GetFocusableControl();
            if (control != null) {
                ConnectToSelectionChanged(control);
            }
        }
    }

    private void DisconnectFromRows() {
        foreach (var row in _currentRows) {
            var control = (row as UIGridRow)?.GetFocusableControl();
            if (control != null) {
                DisconnectFromSelectionChanged(control);
            }
        }
    }

    private void ConnectToSelectionChanged(Control control) {
        // Check if control has SelectionChanged signal (UISelectButton, UiSlider, etc.)
        if (control is IEditableControl selectableControl && control.HasSignal("SelectionChanged")) {
            if (!control.IsConnected("SelectionChanged", new Callable(this, nameof(OnControlSelectionChanged)))) {
                control.Connect("SelectionChanged", new Callable(this, nameof(OnControlSelectionChanged)));
            }
        }
    }

    private void DisconnectFromSelectionChanged(Control control) {
        if (control is IEditableControl selectableControl && control.HasSignal("SelectionChanged")) {
            if (control.IsConnected("SelectionChanged", new Callable(this, nameof(OnControlSelectionChanged)))) {
                control.Disconnect("SelectionChanged", new Callable(this, nameof(OnControlSelectionChanged)));
            }
        }
    }

    private void OnControlSelectionChanged(bool isSelected) {
        GD.Print($"[SettingsFocusManager] Control selection changed. IsSelected: {isSelected}");
        var currentControl = _getEditModeItem();
        _currentEditableControl = currentControl;
        _isInEditMode = isSelected;
        // make sure the all the other controls are not in edit mode
        foreach (var row in _currentRows) {
            if (row is UIGridRow gridRow) {
                var control = gridRow.GetFocusableControl();
                if (control is IEditableControl selectableControl && selectableControl != currentControl && selectableControl.IsInEditMode()) {
                    selectableControl.setEditing(false);
                }
            }
        }
    }

    private IEditableControl? _getEditModeItem() {
        foreach (var row in _currentRows) {
            if (row is UIGridRow gridRow) {
                // Only UIGridRow can have editable controls
                var control = gridRow.GetFocusableControl();
                if (control is IEditableControl selectableControl && selectableControl.IsInEditMode()) {
                    return selectableControl;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Clears focus state. Call when leaving the settings menu.
    /// </summary>
    public void ClearFocus() {
        DisconnectFromRows();
        _currentRows.Clear();
        _currentRowIndex = 0;
        _isInEditMode = false;
        _currentFocusedItem = null;
        _currentEditableControl?.setEditing(false);
        _currentEditableControl = null;
    }
}
