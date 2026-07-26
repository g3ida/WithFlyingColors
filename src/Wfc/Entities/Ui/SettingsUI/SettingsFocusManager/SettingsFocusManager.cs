namespace Wfc.Entities.Ui.SettingsUI;

using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Event;
using Wfc.Core.Input;
using Wfc.Core.Ui;
using Wfc.Entities.Ui.SettingsUI.Grid;
using Wfc.Screens.MenuManager;

using KeyBindingButton = Wfc.Entities.Ui.KeyBindingButton;

// Manages focus navigation for the settings menu.
// Handles:
// - Up/Down arrow keys to navigate between rows
// - Left/Right arrow keys to navigate between tabs (only when panel tab is focused)
// - Mouse hover to update focus
// - Edit mode for KeyBindingButton and GamepadBindingButton
[Meta(typeof(IAutoNode))]
public partial class SettingsFocusManager : Node {
    #region Dependencies
    public override void _Notification(int what) => this.Notify(what);

    [Dependency]
    public IInputManager InputManager => this.DependOn<IInputManager>();
    [Dependency]
    public IEventHandler EventHandler => this.DependOn<IEventHandler>();
    [Dependency]
    public IModalStack ModalStack => this.DependOn<IModalStack>();
    #endregion Dependencies

    #region Signals
    [Signal]
    public delegate void TabNavigationRequestedEventHandler(int direction);
    #endregion Signals

    private List<Control> _currentRows = new();
    private int _currentRowIndex = 0;
    private bool _shouldFocusOnPanelTab = false;
    private KeyBindingButton? _activeKeyBinding = null;

    private readonly Dictionary<Control, Callable> _mouseEnteredCallables = new();
    private Control? _panelTabControl = null;
    private Callable _panelTabMouseEnteredCallable;
    private bool _hasPanelTabMouseEnteredCallable = false;

    public int CurrentRowIndex => _currentRowIndex;
    public int RowCount => _currentRows.Count;
    private bool IsBindingActive => (_activeKeyBinding?.IsInEditMode() ?? false);

    // Sets the list of focusable rows for the current panel. Called when switching tabs/panels.
    public void SetFocusableRows(Button currentPanelButton, List<UIGridRow> rows) {
        _disconnectFromRows();
        _currentRows = rows.ConvertAll(row => (Control)row);
        _currentRows = _currentRows.FindAll(row => row.Visible);
        _currentRows.Insert(0, currentPanelButton);
        _connectToRows();
        int focusIndex = _shouldFocusOnPanelTab || _currentRows.Count <= 1 ? 0 : 1;
        _focusRow(focusIndex);
    }

    public override void _Ready() {
        base._Ready();
        ProcessMode = ProcessModeEnum.Always;
    }

    public override void _Input(InputEvent @event) {
        // This node processes Always so it survives the pause a key capture causes,
        // which also means it keeps receiving input under a dialog. Standing down for
        // whatever holds the screen is what stops it moving focus behind one.
        if (ModalStack.IsAnyOpen || IsBindingActive) {
            return;
        }

        // Tab navigation (always available)
        if (InputManager.IsEventActionJustPressed(IInputManager.Action.UITabNext, @event)) {
            _shouldFocusOnPanelTab = false;
            EmitSignal(SignalName.TabNavigationRequested, 1);
            GetViewport().SetInputAsHandled();
            return;
        }
        if (InputManager.IsEventActionJustPressed(IInputManager.Action.UITabPrevious, @event)) {
            _shouldFocusOnPanelTab = false;
            EmitSignal(SignalName.TabNavigationRequested, -1);
            GetViewport().SetInputAsHandled();
            return;
        }

        // Up/Down navigation
        if (InputManager.IsEventActionJustPressed(IInputManager.Action.UIUp, @event)) {
            _navigateUp();
            GetViewport().SetInputAsHandled();
            return;
        }
        if (InputManager.IsEventActionJustPressed(IInputManager.Action.UIDown, @event)) {
            _navigateDown();
            GetViewport().SetInputAsHandled();
            return;
        }

        // Prevent left/right from changing focus between controls while on a row.
        // Left/Right should only change tabs when the panel tab is focused (index 0).
        if (_currentRowIndex != 0) {
            if (InputManager.IsEventActionJustPressed(IInputManager.Action.UILeft, @event)
             || InputManager.IsEventActionJustPressed(IInputManager.Action.UIRight, @event)) {
                GetViewport().SetInputAsHandled();
                return;
            }
        }

        // Left/Right for tab navigation (only when panel tab is focused at index 0)
        if (_currentRowIndex == 0) {
            if (InputManager.IsEventActionJustPressed(IInputManager.Action.UILeft, @event)) {
                _shouldFocusOnPanelTab = true;
                EmitSignal(SignalName.TabNavigationRequested, -1);
                GetViewport().SetInputAsHandled();
                return;
            }
            if (InputManager.IsEventActionJustPressed(IInputManager.Action.UIRight, @event)) {
                _shouldFocusOnPanelTab = true;
                EmitSignal(SignalName.TabNavigationRequested, 1);
                GetViewport().SetInputAsHandled();
                return;
            }
        }
        // Note: Left/Right for UISelectButton and UiSlider is handled by those controls themselves
        // Note: UICancel is the screen's to answer. This node used to consume it, and
        // being ahead of everything else in the tree that meant a dialog could never
        // see the key that was supposed to close it.
    }

    private void _navigateUp() {
        if (_currentRows.Count == 0)
            return;

        int newIndex = _currentRowIndex - 1;
        if (newIndex < 0) {
            newIndex = _currentRows.Count - 1;
        }
        _focusRow(newIndex);
    }

    private void _navigateDown() {
        if (_currentRows.Count == 0)
            return;

        int newIndex = (_currentRowIndex + 1) % _currentRows.Count;
        if (newIndex == 0) {
            newIndex = 1; // Skip panel tab when wrapping
        }
        _focusRow(newIndex);
    }

    private void _focusRow(int index) {
        if (index < 0 || index >= _currentRows.Count)
            return;

        _currentRowIndex = index;
        var row = _currentRows[index];

        if (index == 0) {
            row.GrabFocus();
        }
        else {
            var focusableControl = (row as UIGridRow)?.GetFocusableControl();
            focusableControl?.GrabFocus();
        }
    }

    public void _refocusCurrentRow() {
        if (_currentRows.Count > 0) {
            _focusRow(_currentRowIndex);
        }
    }

    private void _connectToRows() {
        if (_currentRows.Count > 0 && _currentRows[0] is Control panelTab) {
            _connectToPanelTabHover(panelTab);
        }

        foreach (var row in _currentRows) {
            if (row is UIGridRow gridRow) {
                var control = gridRow.GetFocusableControl();
                if (control != null) {
                    _connectToMouseHover(control);
                    _connectToKeyBindingSignals(control);
                }
            }
        }
    }
    private void _disconnectFromRows() {
        _disconnectFromPanelTabHover();

        foreach (var row in _currentRows) {
            if (row is UIGridRow gridRow) {
                var control = gridRow.GetFocusableControl();
                if (control != null) {
                    _disconnectFromMouseHover(control);
                    _disconnectFromKeyBindingSignals(control);
                }
            }
        }
        _activeKeyBinding = null;
    }

    private void _connectToMouseHover(Control control) {
        if (_mouseEnteredCallables.ContainsKey(control))
            return;

        var callable = Callable.From(() => OnControlMouseEntered(control));
        _mouseEnteredCallables[control] = callable;

        if (!control.IsConnected(Control.SignalName.MouseEntered, callable)) {
            control.Connect(Control.SignalName.MouseEntered, callable);
        }
    }

    private void _connectToPanelTabHover(Control panelTab) {
        if (_panelTabControl != null && _panelTabControl != panelTab) {
            _disconnectFromPanelTabHover();
        }

        _panelTabControl = panelTab;
        var callable = _getPanelTabMouseEnteredCallable();
        if (!panelTab.IsConnected(Control.SignalName.MouseEntered, callable)) {
            panelTab.Connect(Control.SignalName.MouseEntered, callable);
        }
    }

    private void _disconnectFromMouseHover(Control control) {
        if (_mouseEnteredCallables.TryGetValue(control, out var callable)) {
            if (control.IsConnected(Control.SignalName.MouseEntered, callable)) {
                control.Disconnect(Control.SignalName.MouseEntered, callable);
            }
            _mouseEnteredCallables.Remove(control);
        }
    }

    private Callable _getPanelTabMouseEnteredCallable() {
        if (!_hasPanelTabMouseEnteredCallable) {
            _panelTabMouseEnteredCallable = Callable.From(OnPanelTabMouseEntered);
            _hasPanelTabMouseEnteredCallable = true;
        }
        return _panelTabMouseEnteredCallable;
    }

    private void _disconnectFromPanelTabHover() {
        if (_panelTabControl == null)
            return;

        if (_hasPanelTabMouseEnteredCallable) {
            var callable = _panelTabMouseEnteredCallable;
            if (_panelTabControl.IsConnected(Control.SignalName.MouseEntered, callable)) {
                _panelTabControl.Disconnect(Control.SignalName.MouseEntered, callable);
            }
        }

        _panelTabControl = null;
    }

    private void _connectToKeyBindingSignals(Control control) {
        if (control is KeyBindingButton keyBinding) {
            if (!keyBinding.IsConnected("SelectionChanged", new Callable(this, nameof(OnKeyBindingSelectionChanged)))) {
                keyBinding.Connect("SelectionChanged", new Callable(this, nameof(OnKeyBindingSelectionChanged)));
            }
        }
    }

    private void _disconnectFromKeyBindingSignals(Control control) {
        if (control is KeyBindingButton keyBinding) {
            if (keyBinding.IsConnected("SelectionChanged", new Callable(this, nameof(OnKeyBindingSelectionChanged)))) {
                keyBinding.Disconnect("SelectionChanged", new Callable(this, nameof(OnKeyBindingSelectionChanged)));
            }
        }
    }

    private void OnControlMouseEntered(Control control) {
        // Find the index of the row containing this control and update focus
        for (int i = 1; i < _currentRows.Count; i++) {
            if (_currentRows[i] is UIGridRow gridRow && gridRow.GetFocusableControl() == control) {
                _focusRow(i);
                return;
            }
        }
    }

    private void OnPanelTabMouseEntered() {
        _focusRow(0);
    }

    private void OnKeyBindingSelectionChanged(bool isEditing) {
        if (isEditing) {
            _activeKeyBinding = _findActiveKeyBinding();
        }
        else {
            _activeKeyBinding = null;
        }
    }

    private KeyBindingButton? _findActiveKeyBinding() {
        foreach (var row in _currentRows) {
            if (row is UIGridRow gridRow) {
                var control = gridRow.GetFocusableControl();
                if (control is KeyBindingButton keyBinding && keyBinding.IsInEditMode()) {
                    return keyBinding;
                }
            }
        }
        return null;
    }

    public void ClearFocus() {
        _disconnectFromRows();
        _currentRows.Clear();
        _currentRowIndex = 0;
        _activeKeyBinding?.setEditing(false);
        _activeKeyBinding = null;
    }
}
