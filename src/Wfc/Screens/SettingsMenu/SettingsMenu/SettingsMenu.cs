namespace Wfc.Screens.SettingsMenu;

using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Event;
using Wfc.Core.Input;
using Wfc.Core.Settings;
using Wfc.Entities.Ui;
using Wfc.Entities.Ui.Dialogs;
using Wfc.Entities.Ui.SettingsUI;
using Wfc.Entities.Ui.SettingsUI.Grid;
using Wfc.Screens.MenuManager;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[ScenePath]
public partial class SettingsMenu : GameMenu {

  #region Nodes
  [NodePath("DialogContainer")]
  private DialogContainer _dialogContainerNode = default!;
  [NodePath("UiTabContainer")]
  private SettingsTabManager _settingsTabManager = default!;
  #endregion Nodes

  private SettingsFocusManager _focusManager = default!;

  public override void _Ready() {
    base._Ready();
    this.WireNodes();

    // Create and add focus manager
    _focusManager = new SettingsFocusManager();
    AddChild(_focusManager);

    // Connect focus manager signals
    _focusManager.TabNavigationRequested += OnTabNavigationRequested;

    // Connect tab manager signals
    _settingsTabManager.PanelChanged += OnPanelChanged;
    // Walking away from a broken mapping is refused the same way whether it goes
    // through another tab or out of the screen.
    _settingsTabManager.CanLeavePanel = OnCanLeavePanel;

    // Initialize focus after a frame to ensure all nodes are ready
    CallDeferred(nameof(InitializeFocus));
  }

  private void InitializeFocus() {
    // Trigger initial panel change to set up focus
    _settingsTabManager.SwitchToPanel(0);
  }

  public override void _ExitTree() {
    base._ExitTree();
    _focusManager.TabNavigationRequested -= OnTabNavigationRequested;
    _settingsTabManager.PanelChanged -= OnPanelChanged;
    _focusManager.ClearFocus();
  }

  // Up/Down/Left/Right navigation is handled by SettingsFocusManager.
  private void OnTabNavigationRequested(int direction) => _settingsTabManager.NavigateTab(direction);

  private void OnPanelChanged(Button currentPanelButton, Godot.Collections.Array<UIGridRow> rows) {
    // Convert Godot array to C# list for the focus manager
    var rowList = new List<UIGridRow>();
    foreach (var row in rows) {
      rowList.Add(row);
    }
    _focusManager.SetFocusableRows(currentPanelButton, rowList);
  }

  public override bool OnMenuButtonPressed(MenuAction menuAction) {
    base.OnMenuButtonPressed(menuAction);
    switch (menuAction) {
      case MenuAction.ShowDialog:
        _dialogContainerNode.ShowDialog();
        return true;
      case MenuAction.GoBack:
        if (IsValidState()) {
          GameSettings.Save();
          return false; // We don't return true here because we want the default behavior to be called
        }
        else {
          EventHandler.EmitMenuActionPressed(MenuAction.ShowDialog);
          return true;
        }
      default:
        return false;
    }
  }

  private bool OnCanLeavePanel(int panelIndex) {
    if (panelIndex != SettingsTabManager.CONTROLLER_PANEL_INDEX || IsValidState()) {
      return true;
    }
    EventHandler.EmitMenuActionPressed(MenuAction.ShowDialog);
    return false;
  }

  private static bool IsValidState() => HasAllRequiredBindings() && HasNoDuplicateBindings();

  private static bool HasAllRequiredBindings() {
    // Check keyboard bindings if keyboard is selected
    if (GameSettings.LastUsedController == Core.Input.Controllers.ControllerType.Keyboard) {
      return GameSettings.AreActionKeysValid();
    }
    // Check gamepad bindings if gamepad is selected and connected
    if (GameSettings.LastUsedController == Core.Input.Controllers.ControllerType.Gamepad && InputUtils.IsGamepadConnected()) {
      return GameSettings.AreGamepadBindingsValid();
    }
    // Default to checking keyboard bindings
    return GameSettings.AreActionKeysValid();
  }

  // A key on two actions is broken on any device, but a pad can only be remapped
  // while one is plugged in, so its duplicates only hold the player here then.
  private static bool HasNoDuplicateBindings() {
    if (GameSettings.HasDuplicateKeyboardBindings()) {
      return false;
    }
    return !InputUtils.IsGamepadConnected() || !GameSettings.HasDuplicateGamepadBindings();
  }

  // Only reports the intent. Whether the bindings are valid is decided in one place,
  // the GoBack case above, so the button and UICancel can't disagree.
  private void OnBackButtonPressed() => EventHandler.EmitMenuActionPressed(MenuAction.GoBack);
}
