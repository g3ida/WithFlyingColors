namespace Wfc.Entities.Ui.SettingsUI.UISelect;

using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Input.Controllers;
using Wfc.Core.Localization;
using Wfc.Core.Settings;
using Wfc.Entities.Ui;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using EventHandler = Wfc.Core.Event.EventHandler;

[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class ControllerSelectDriver : UISelectDriver {

  public override void _Notification(int what) => this.Notify(what);

  [Dependency]
  public ILocalizationService LocalizationService => this.DependOn<ILocalizationService>();

  private List<ControllerType> _availableControllers = new();

  private bool _isJoyConnectionSubscribed;

  // The currently selected controller type.
  public ControllerType SelectedControllerType { get; private set; } = ControllerType.Keyboard;

  // Emitted when the gamepad connection status changes.
  [Signal]
  public delegate void GamepadConnectionChangedEventHandler(bool isConnected);

  // Emitted when the selected controller type changes.
  [Signal]
  public delegate void ControllerTypeChangedEventHandler(int controllerType);

  public ControllerSelectDriver() {

  }

  public override void onItemSelected(Variant? item) {
    if (item != null) {
      var controllerTypeInt = int.Parse(item.Value.As<string>());
      SelectedControllerType = (ControllerType)controllerTypeInt;
      GameSettings.LastUsedController = SelectedControllerType;
      EmitSignal(SignalName.ControllerTypeChanged, controllerTypeInt);
    }
  }

  public override int GetDefaultSelectedIndex() {
    // Return index based on last used controller
    var lastUsed = GameSettings.LastUsedController;

    // If last used was gamepad but no gamepad is connected, default to keyboard
    if (lastUsed == ControllerType.Gamepad && !InputUtils.IsGamepadConnected()) {
      return 0;
    }

    var index = _availableControllers.FindIndex(x => x == lastUsed);
    return index == -1 ? 0 : index;
  }

  public override void _Ready() {
    base._Ready();
    if (!_isJoyConnectionSubscribed) {
      Input.JoyConnectionChanged += OnJoyConnectionChanged;
      _isJoyConnectionSubscribed = true;
    }
  }

  public override void _ExitTree() {
    base._ExitTree();
    if (_isJoyConnectionSubscribed) {
      Input.JoyConnectionChanged -= OnJoyConnectionChanged;
      _isJoyConnectionSubscribed = false;
    }
  }

  private void OnJoyConnectionChanged(long deviceId, bool connected) {
    RefreshControllerList();
    if (connected) {
      var deviceName = Input.GetJoyName((int)deviceId);
      EventHandler.Instance.EmitGamepadConnected((int)deviceId, deviceName);
    }
    else {
      EventHandler.Instance.EmitGamepadDisconnected((int)deviceId);
    }
    EmitSignal(SignalName.GamepadConnectionChanged, InputUtils.IsGamepadConnected());
  }

  private void RefreshControllerList() {
    Items.Clear();
    ItemValues.Clear();
    _availableControllers.Clear();

    // Always add keyboard
    _availableControllers.Add(ControllerType.Keyboard);
    Items.Add(ControllerType.Keyboard.GetLocalizedName(LocalizationService));
    ItemValues.Add(((int)ControllerType.Keyboard).ToString());

    // Only add gamepad if one is connected
    if (InputUtils.IsGamepadConnected()) {
      _availableControllers.Add(ControllerType.Gamepad);
      var gamepadName = InputUtils.GetConnectedGamepadName() ?? "Gamepad";
      Items.Add($"{ControllerType.Gamepad.GetLocalizedName(LocalizationService)} ({gamepadName})");
      ItemValues.Add(((int)ControllerType.Gamepad).ToString());
    }

    // Notify parent to refresh UI if needed
    if (GetParent() is UISelectButton selectButton) {
      selectButton.RefreshItems();
    }
  }

  public void OnResolved() {
    RefreshControllerList();
    // Set initial selected controller type based on last used
    SelectedControllerType = GameSettings.LastUsedController;
    if (SelectedControllerType == ControllerType.Gamepad && !InputUtils.IsGamepadConnected()) {
      SelectedControllerType = ControllerType.Keyboard;
    }
  }

  // Returns true if a gamepad is currently connected.
  public bool IsGamepadConnected() => InputUtils.IsGamepadConnected();
}
