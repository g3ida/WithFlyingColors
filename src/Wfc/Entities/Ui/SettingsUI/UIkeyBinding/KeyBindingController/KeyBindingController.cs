namespace Wfc.Screens.SettingsMenu;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Settings;
using Wfc.Entities.Ui;
using Wfc.Entities.Ui.SettingsUI.Grid;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using EventHandler = Wfc.Core.Event.EventHandler;


[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class KeyBindingController : PanelContainer {

  #region Dependencies
  public override void _Notification(int what) => this.Notify(what);

  #endregion Dependencies

  [Signal]
  public delegate void onActionBoundSignalEventHandler(string action, int key);

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
	  EmitSignal(nameof(onActionBoundSignal), action, key);
	  EventHandler.Instance.EmitOnActionBound(action, key);
	}
  }
}
