namespace Wfc.Screens;

using System.Collections.Generic;
using Godot;
using Wfc.Core.Settings;
using Wfc.Entities.Ui.Dialogs;
using Wfc.Entities.Ui.InputHint;
using Wfc.Entities.Ui.SettingsUI;
using Wfc.Entities.Ui.SettingsUI.Grid;
using Wfc.Utils;
using Wfc.Utils.Attributes;

// The settings screen's tabbed panel hosted inside the pause overlay, wearing
// its dark clothing over the paused level. Opening and closing it is the pause
// menu's affair; this node owns the focus wiring, the binding validation and
// the save on the way out - the same contract the standalone screen keeps.
[ScenePath]
public partial class PauseSettingsMenu : Control {

  #region Nodes
  [NodePath("UiTabContainer")]
  private SettingsTabManager _settingsTabManager = default!;
  [NodePath("DialogContainer")]
  private DialogContainer _dialogContainerNode = default!;
  [NodePath("InputHintBar")]
  private InputHintBar _inputHintBar = default!;
  #endregion Nodes

  private SettingsFocusManager _focusManager = default!;

  public bool IsOpen { get; private set; }

  public override void _Ready() {
    base._Ready();
    this.WireNodes();

    _focusManager = new SettingsFocusManager();
    AddChild(_focusManager);
    // The focus manager processes Always, and this overlay sits in every level
    // for its whole run: left listening while closed it would keep answering
    // navigation input meant for the pause buttons or the level itself.
    _focusManager.SetProcessInput(false);
    _focusManager.TabNavigationRequested += _onTabNavigationRequested;
    _settingsTabManager.PanelChanged += _onPanelChanged;
    // Walking away from a broken mapping is refused the same way whether it
    // goes through another tab or back out to the pause buttons.
    _settingsTabManager.CanLeavePanel = _onCanLeavePanel;
  }

  public override void _ExitTree() {
    base._ExitTree();
    _focusManager.TabNavigationRequested -= _onTabNavigationRequested;
    _settingsTabManager.PanelChanged -= _onPanelChanged;
    _focusManager.ClearFocus();
  }

  public void Open() {
    IsOpen = true;
    Show();
    _focusManager.SetProcessInput(true);
    // Always lands on the first tab: reopening the pause menu's settings on
    // whatever tab it was left on would also reopen it on stale focus rows.
    _settingsTabManager.SwitchToPanel(0);
    _inputHintBar.Enter();
  }

  // Declines while the bindings would leave the player unable to play, exactly
  // as the settings screen declines to navigate away, and says so in a dialog.
  public bool TryClose() {
    if (!SettingsBindingsValidator.IsValidState()) {
      _dialogContainerNode.ShowDialog();
      return false;
    }
    GameSettings.Save();
    IsOpen = false;
    _focusManager.SetProcessInput(false);
    _focusManager.ClearFocus();
    _inputHintBar.Exit();
    Hide();
    return true;
  }

  private void _onTabNavigationRequested(int direction) => _settingsTabManager.NavigateTab(direction);

  private void _onPanelChanged(Button currentPanelButton, Godot.Collections.Array<UIGridRow> rows) {
    var rowList = new List<UIGridRow>();
    foreach (var row in rows) {
      rowList.Add(row);
    }
    _focusManager.SetFocusableRows(currentPanelButton, rowList);
  }

  private bool _onCanLeavePanel(int panelIndex) {
    if (panelIndex != SettingsTabManager.CONTROLLER_PANEL_INDEX || SettingsBindingsValidator.IsValidState()) {
      return true;
    }
    _dialogContainerNode.ShowDialog();
    return false;
  }
}
