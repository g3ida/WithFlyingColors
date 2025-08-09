namespace Wfc.Screens.SettingsMenu;

using Godot;
using Wfc.Core.Settings;
using Wfc.Entities.Ui;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using EventHandler = Wfc.Core.Event.EventHandler;

public partial class KeyBindingController : Control, IUITab {
  [Signal]
  public delegate void on_action_bound_signalEventHandler(string action, int key);

  [NodePath("GridContainer/JumpBtn")]
  private Button jumpBtn = default!;

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
  }

  private void _onKeyboardInputActionBound(string action, int key) {
    if (key < 0) {
      GameSettings.UnbindActionKey(action);
    }
    else {
      GameSettings.BindActionToKeyboardKey(action, key);
      EmitSignal(nameof(on_action_bound_signal), action, key);
      EventHandler.Instance.EmitOnActionBound(action, key);
    }
  }

  public void OnGainFocus() {
    jumpBtn.GrabFocus();
  }
}

