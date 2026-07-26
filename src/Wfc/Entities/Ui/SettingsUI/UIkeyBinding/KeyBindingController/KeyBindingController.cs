namespace Wfc.Screens.SettingsMenu;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Input.Controllers;
using Wfc.Core.Settings;
using Wfc.Entities.Ui;
using Wfc.Entities.Ui.SettingsUI.Grid;
using Wfc.Entities.Ui.SettingsUI.UISelect;
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

	[Signal]
	public delegate void OnGamepadActionBoundSignalEventHandler(string action, int buttonOrAxis, bool isAxis, float axisDirection);

	private List<KeyBindingButton> _bindingButtons = new();
	private ControllerType _currentControllerType = ControllerType.Keyboard;

	public override void _Ready() {
		base._Ready();
		this.WireNodes();

		_bindingButtons = [.. this.FindDescendants<KeyBindingButton>()];
		_currentControllerType = InputUtils.GetEffectiveControllerType();
		_updateBindingButtons();
	}

	// Called by the ControllerSelectDriver when the user changes the controller type.
	public void OnControllerTypeChanged(int controllerType) {
		_currentControllerType = (ControllerType)controllerType;
		_updateBindingButtons();
	}

	private void _updateBindingButtons() {
		var bindingType = _currentControllerType switch {
			ControllerType.Keyboard => KeyBindingButton.BindingType.Keyboard,
			ControllerType.Gamepad => KeyBindingButton.BindingType.Gamepad,
			_ => KeyBindingButton.BindingType.Keyboard,
		};


		foreach (var button in _bindingButtons) {
			button.Type = bindingType;
		}
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

	private void _onGamepadInputActionBound(string action, int buttonOrAxis, bool isAxis, float axisDirection) {
		if (buttonOrAxis < 0) {
			GameSettings.UnbindActionGamepad(action);
		}
		else {
			if (isAxis) {
				GameSettings.BindActionToGamepadAxis(action, (JoyAxis)buttonOrAxis, axisDirection);
			}
			else {
				GameSettings.BindActionToGamepadButton(action, (JoyButton)buttonOrAxis);
			}
			EmitSignal(nameof(OnGamepadActionBoundSignal), action, buttonOrAxis, isAxis, axisDirection);
			EventHandler.Instance.EmitOnGamepadActionBound(action, buttonOrAxis, isAxis, axisDirection);
		}
	}
}
