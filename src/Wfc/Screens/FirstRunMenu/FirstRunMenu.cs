namespace Wfc.Screens;

using System.Linq;
using Godot;
using Wfc.Core.Input;
using Wfc.Core.Settings;
using Wfc.Entities.Ui.InputHint;
using Wfc.Entities.Ui.SettingsUI.UISelect;
using Wfc.Screens.MenuManager;
using Wfc.Utils;

// The shape shared by the screens the game shows once, on its very first launch,
// before the main menu. Each asks for one thing the game cannot sensibly guess and
// cannot draw itself without: the language its words are in, and the colours its
// shapes are told apart by. One picker, nothing else to reach, and no way back.
//
// Confirming writes the answer to the settings file, which is what stops the screen
// coming back, and hands over to whatever is still unanswered.
//
// The picker and the hint bar are found rather than declared by path: the screens lay
// themselves out differently and there is exactly one of each on any of them.
public abstract partial class FirstRunMenu : GameMenu {
  private UISelectButton _pickerNode = default!;

  // Where this screen hands over once the player has answered it.
  protected abstract GameMenus NextScreen { get; }

  public override void _Ready() {
    base._Ready();

    // Nothing came before this screen, so there is nowhere for back to go.
    HandleBackEvent = false;
    this.FindDescendants<InputHintBar>().First().RemoveCard("BackCard");

    _pickerNode = this.FindDescendants<UISelectButton>().First();
    _pickerNode.OnDarkBackground = true;
    foreach (var label in _pickerNode.FindDescendants<Label>()) {
      label.AddThemeColorOverride("font_color", Colors.White);
    }
    _pickerNode.Pressed += _onConfirmed;
    _pickerNode.GrabFocus();
    OnFirstRunReady();
  }

  // Anything the screen wants done once its picker is standing up.
  protected virtual void OnFirstRunReady() { }

  public override void _ExitTree() {
    base._ExitTree();
    _pickerNode.Pressed -= _onConfirmed;
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

  // The picker has already put the choice into the settings; writing the file is what
  // makes it survive the launch, and a settings file that carries an answer is what
  // tells the next launch not to ask again.
  private void _onConfirmed() {
    GameSettings.Save();
    NavigateToScreen(NextScreen);
  }
}
