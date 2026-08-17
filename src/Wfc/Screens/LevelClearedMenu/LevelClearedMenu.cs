namespace Wfc.Screens;

using Godot;
using Wfc.Core.Event;
using Wfc.Core.Input;
using Wfc.Core.Localization;
using Wfc.Entities.Ui;
using Wfc.Screens.MenuManager;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[ScenePath]
public partial class LevelClearedMenu : GameMenu {
  [NodePath("LevelClearedLabel")]
  private TitleLabel _titleNode = default!;

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _refreshTitle();
    // The screen owns cancel itself: the default back navigation would return to the
    // previous menu, which is the level the player just finished.
    HandleBackEvent = false;
  }

  // The title holds a string that was already translated when the screen was built,
  // so the engine's own auto-translation has nothing left to redo once the player
  // picks another language.
  public override void _Notification(int what) {
    base._Notification(what);
    if (what == NotificationTranslationChanged && IsNodeReady()) {
      _refreshTitle();
    }
  }

  private void _refreshTitle() =>
    _titleNode.SetContent(LocalizationService.GetLocalizedString(TranslationKey.menu_header_levelCleared));

  public override void _Input(InputEvent @event) {
    base._Input(@event);
    // Either action dismisses the screen. Going through the input manager rather than
    // testing for InputEventKey covers the gamepad, and drops key releases and the
    // auto-repeat echoes that used to fire this twice per press.
    if (InputManager.IsEventActionJustPressed(IInputManager.Action.UIConfirm, @event) ||
        InputManager.IsEventActionJustPressed(IInputManager.Action.UICancel, @event)) {
      GameEvents.Instance.OnMenuActionPressed(MenuAction.ExitClearedLevel);
    }
  }

  public override bool OnMenuButtonPressed(MenuAction menuAction) {
    if (menuAction == MenuAction.ExitClearedLevel) {
      NavigateToScreen(GameMenus.MAIN_MENU);
      return true;
    }
    return false;
  }
}
