namespace Wfc.Screens;

using Godot;
using Wfc.Core.Input;
using Wfc.Core.Settings;
using Wfc.Entities.Ui.InputHint;
using Wfc.Entities.Ui.SettingsUI.UISelect;
using Wfc.Screens.MenuManager;
using Wfc.Utils;
using Wfc.Utils.Attributes;

// The screen the game opens on the very first time it is run, and only then: it has
// no language yet beyond the one the system was guessed to be in, and every screen
// after this one is drawn in whatever is picked here. Confirming writes the choice to
// the settings file, which is what keeps the screen from coming back; from then on it
// is the settings menu that owns the language.
[ScenePath]
public partial class LanguageSelectMenu : GameMenu {
  #region Nodes
  [NodePath("Picker/UISelectButton")]
  private UISelectButton _selectButtonNode = default!;
  [NodePath("InputHintBar")]
  private InputHintBar _hintBarNode = default!;
  #endregion Nodes

  public override void _Ready() {
    base._Ready();
    this.WireNodes();

    // Nothing came before this screen, so there is nowhere for back to go.
    HandleBackEvent = false;
    _hintBarNode.RemoveCard("BackCard");

    _selectButtonNode.OnDarkBackground = true;
    foreach (var label in _selectButtonNode.FindDescendants<Label>()) {
      label.AddThemeColorOverride("font_color", Colors.White);
    }
    _selectButtonNode.Pressed += _onLanguageConfirmed;
    _selectButtonNode.GrabFocus();
  }

  public override void _ExitTree() {
    base._ExitTree();
    _selectButtonNode.Pressed -= _onLanguageConfirmed;
  }

  // The picker reads left and right itself, off polled input. Left to the engine as
  // well, the same press is a request to move the focus, and the only thing to move
  // it to is one of the arrows the picker is drawn from - after which the picker
  // stops answering.
  public override void _Input(InputEvent @event) {
    base._Input(@event);
    if (InputManager.IsEventActionJustPressed(IInputManager.Action.UILeft, @event)
        || InputManager.IsEventActionJustPressed(IInputManager.Action.UIRight, @event)) {
      GetViewport().SetInputAsHandled();
    }
  }

  // The picker has already put the choice into GameSettings; writing the file is what
  // makes it survive the launch, and a settings file that names a language is what
  // tells the next launch not to ask again.
  private void _onLanguageConfirmed() {
    GameSettings.Save();
    NavigateToScreen(GameMenus.MAIN_MENU);
  }
}
