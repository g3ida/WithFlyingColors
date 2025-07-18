namespace Wfc.Screens;

using Godot;
using Wfc.Core.Event;
using Wfc.Core.Settings;
using Wfc.Utils.Attributes;

[ScenePath]
public partial class SettingsMenu : GameMenu {
  private TabContainer tabContainer;
  private int tabsCount;
  private DialogContainer DialogContainerNode;

  public override void _Ready() {
    base._Ready();
    tabContainer = GetNode<TabContainer>("TabContainer");
    tabsCount = tabContainer.GetChildCount();
    DialogContainerNode = GetNode<DialogContainer>("DialogContainer");
  }

  public override void _Input(InputEvent @event) {
    base._Input(@event);
    if (Input.IsActionJustPressed("ui_left")) {
      tabContainer.CurrentTab = Mathf.Clamp(tabContainer.CurrentTab - 1, 0, tabsCount - 1);
    }
    else if (Input.IsActionJustPressed("ui_right")) {
      tabContainer.CurrentTab = Mathf.Clamp(tabContainer.CurrentTab + 1, 0, tabsCount - 1);
    }
  }

  public override bool OnMenuButtonPressed(MenuButtons menuButton) {
    base.OnMenuButtonPressed(menuButton);
    switch (menuButton) {
      case MenuButtons.SHOW_DIALOG:
        DialogContainerNode.ShowDialog();
        return true;
      case MenuButtons.BACK:
        if (IsValidState()) {
          GameSettings.Save();
          return false; // We don't return true here because we want the default behavior to be called
        }
        else {
          EventHandler.EmitMenuButtonPressed(MenuButtons.SHOW_DIALOG);
          return true;
        }
      default:
        return false;
    }
  }

  private static bool IsValidState() {
    return GameSettings.AreActionKeysValid();
  }

  private void OnBackButtonPressed() {
    if (!IsInTransitionState()) {
      if (IsValidState()) {
        EventHandler.EmitMenuButtonPressed(MenuButtons.BACK);
      }
      else {
        EventHandler.EmitMenuButtonPressed(MenuButtons.SHOW_DIALOG);
      }
    }
  }
}
