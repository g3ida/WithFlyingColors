namespace Wfc.Screens.SettingsMenu;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Event;
using Wfc.Core.Input;
using Wfc.Core.Settings;
using Wfc.Entities.Ui;
using Wfc.Screens.MenuManager;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class SettingsMenu : GameMenu {

  #region Nodes
  [NodePath("TabContainer")]
  private TabContainer _tabContainer = default!;
  [NodePath("DialogContainer")]
  private DialogContainer _dialogContainerNode = default!;
  #endregion Nodes

  [Dependency]
  public IInputManager InputManager => this.DependOn<IInputManager>();
  private int tabsCount;
  public override void _Notification(int what) => this.Notify(what);

  public void OnResolved() { }

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    tabsCount = _tabContainer.GetChildCount();
  }

  public override void _Input(InputEvent @event) {
    base._Input(@event);
    if (InputManager.IsJustPressed(IInputManager.Action.UILeft)) {
      _tabContainer.CurrentTab = Mathf.Clamp(_tabContainer.CurrentTab - 1, 0, tabsCount - 1);
    }
    else if (InputManager.IsJustPressed(IInputManager.Action.UIRight)) {
      _tabContainer.CurrentTab = Mathf.Clamp(_tabContainer.CurrentTab + 1, 0, tabsCount - 1);
    }
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

  private static bool IsValidState() {
    return GameSettings.AreActionKeysValid();
  }

  private void OnBackButtonPressed() {
    if (!IsInTransitionState()) {
      if (IsValidState()) {
        EventHandler.EmitMenuActionPressed(MenuAction.GoBack);
      }
      else {
        EventHandler.EmitMenuActionPressed(MenuAction.ShowDialog);
      }
    }
  }
}
