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
using Wfc.Utils;

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
    public IModalStack ModalStack => this.DependOn<IModalStack>();
    #endregion Dependencies

    #region Signals
    [Signal]
    public delegate void TabNavigationRequestedEventHandler(int direction);
    #endregion Signals

    private static readonly IInputManager.Action[] NAVIGATION_ACTIONS = [
        IInputManager.Action.UIUp,
        IInputManager.Action.UIDown,
        IInputManager.Action.UILeft,
        IInputManager.Action.UIRight,
    ];

    private List<Control> _currentRows = new();
    private UINavigationInput? _navigationInput;
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
    // Built on first use: the dependency it reads is not resolved yet in _Ready.
    private UINavigationInput NavigationInput => _navigationInput ??= new UINavigationInput(InputManager);

    // Sets the list of focusable rows for the current panel. Called when switching tabs/panels.
    public void SetFocusableRows(Button currentPanelButton, List<UIGridRow> rows) {
        _disconnectFromRows();
        // Deliberately not filtered by what is visible right now: a row can be taken
        // away and put back while its panel is open - the resizable window row
        // follows the fullscreen box - so what can be moved to is asked at the time
        // instead of being settled when the panel was built.
        _currentRows = rows.ConvertAll(row => (Control)row);
        _currentRows.Insert(0, currentPanelButton);
        _connectToRows();
        _focusRow(_shouldFocusOnPanelTab ? 0 : _firstAvailableRow());
    }

    public override void _Ready() {
        base._Ready();
        ProcessMode = ProcessModeEnum.Always;
    }

    public override void _Input(InputEvent @event) {
        // Ahead of the guard below: a stick let go of while a dialog is up still has to
        // count as let go of, or the push after the dialog closes moves nothing.
        NavigationInput.ObserveMotion(@event);

        // This node processes Always so it survives the pause a key capture causes,
        // which also means it keeps receiving input under a dialog. Standing down for
        // whatever holds the screen is what stops it moving focus behind one.
        if (ModalStack.IsAnyOpen || IsBindingActive) {
            return;
        }

        // Tab navigation (always available)
        if (NavigationInput.IsJustPressed(IInputManager.Action.UITabNext, @event)) {
            _shouldFocusOnPanelTab = false;
            EmitSignal(SignalName.TabNavigationRequested, 1);
            GetViewport().SetInputAsHandled();
            return;
        }
        if (NavigationInput.IsJustPressed(IInputManager.Action.UITabPrevious, @event)) {
            _shouldFocusOnPanelTab = false;
            EmitSignal(SignalName.TabNavigationRequested, -1);
            GetViewport().SetInputAsHandled();
            return;
        }

        // Up/Down navigation
        if (NavigationInput.IsJustPressed(IInputManager.Action.UIUp, @event)) {
            _navigateUp();
            GetViewport().SetInputAsHandled();
            return;
        }
        if (NavigationInput.IsJustPressed(IInputManager.Action.UIDown, @event)) {
            _navigateDown();
            GetViewport().SetInputAsHandled();
            return;
        }

        // Prevent left/right from changing focus between controls while on a row.
        // Left/Right should only change tabs when the panel tab is focused (index 0).
        if (_currentRowIndex != 0) {
            if (NavigationInput.IsJustPressed(IInputManager.Action.UILeft, @event)
             || NavigationInput.IsJustPressed(IInputManager.Action.UIRight, @event)) {
                GetViewport().SetInputAsHandled();
                return;
            }
        }

        // Left/Right for tab navigation (only when panel tab is focused at index 0)
        if (_currentRowIndex == 0) {
            if (NavigationInput.IsJustPressed(IInputManager.Action.UILeft, @event)) {
                _shouldFocusOnPanelTab = true;
                EmitSignal(SignalName.TabNavigationRequested, -1);
                GetViewport().SetInputAsHandled();
                return;
            }
            if (NavigationInput.IsJustPressed(IInputManager.Action.UIRight, @event)) {
                _shouldFocusOnPanelTab = true;
                EmitSignal(SignalName.TabNavigationRequested, 1);
                GetViewport().SetInputAsHandled();
                return;
            }
        }
        // Anything else carrying a direction is swallowed without being acted on. A stick
        // reports its way up to the strength that steps the menu and keeps reporting for
        // as long as it is held, and whatever of that got through reached the engine's own
        // focus navigation, which walks the focus off the row and onto the tabs.
        if (_carriesNavigation(@event)) {
            GetViewport().SetInputAsHandled();
        }
        // Note: Left/Right for UISelectButton and UiSlider is handled by those controls themselves
        // Note: UICancel is the screen's to answer. This node used to consume it, and
        // being ahead of everything else in the tree that meant a dialog could never
        // see the key that was supposed to close it.
    }

    private static bool _carriesNavigation(InputEvent @event) {
        foreach (var action in NAVIGATION_ACTIONS) {
            if (@event.IsAction(Wfc.Core.Input.InputManager.Actions[action])) {
                return true;
            }
        }
        return false;
    }

    private void _navigateUp() => _focusAvailableRow(-1);

    private void _navigateDown() => _focusAvailableRow(1);

    // Walks until it finds a row that is actually there, so a row hidden while its
    // panel is open cannot be landed on and one put back is reachable again.
    private void _focusAvailableRow(int direction) {
        var index = _currentRowIndex;
        for (var step = 0; step < _currentRows.Count; step++) {
            index += direction;
            if (index < 0) {
                index = _currentRows.Count - 1;
            }
            else if (index >= _currentRows.Count) {
                // Wrapping off the end lands on the first setting, not back on the tab.
                index = Mathf.Min(1, _currentRows.Count - 1);
            }
            if (_isAvailable(index)) {
                _focusRow(index);
                return;
            }
        }
    }

    // The panel tab unless a setting can be reached instead: it is always there, so
    // a panel whose every row is hidden still leaves the focus somewhere.
    private int _firstAvailableRow() {
        for (var i = 1; i < _currentRows.Count; i++) {
            if (_isAvailable(i)) {
                return i;
            }
        }
        return 0;
    }

    private bool _isAvailable(int index) => _currentRows[index].Visible;

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
        if (!PointerFocus.IsPlayerPointing) {
            return;
        }
        // Find the index of the row containing this control and update focus
        for (int i = 1; i < _currentRows.Count; i++) {
            if (_currentRows[i] is UIGridRow gridRow && gridRow.GetFocusableControl() == control) {
                _focusRow(i);
                return;
            }
        }
    }

    private void OnPanelTabMouseEntered() {
        if (!PointerFocus.IsPlayerPointing) {
            return;
        }
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
        _navigationInput?.Reset();
        _currentRowIndex = 0;
        _activeKeyBinding?.setEditing(false);
        _activeKeyBinding = null;
    }
}
