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

  // The item names are translated device names, built once, so the list has to be
  // made again for it to follow a language change.
  public override void _Notification(int what) {
    this.Notify(what);
    if (what == NotificationTranslationChanged && IsNodeReady()) {
      RefreshControllerList();
    }
  }

  [Dependency]
  public ILocalizationService LocalizationService => this.DependOn<ILocalizationService>();

  private List<ControllerType> _availableControllers = new();

  private bool _isSubscribed;

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
    // Keyboard is always first in the list, so an unplugged gamepad falling back to
    // keyboard and an unknown device both land on the same entry.
    var index = _availableControllers.FindIndex(x => x == InputUtils.GetEffectiveControllerType());
    return index == -1 ? 0 : index;
  }

  // Subscribed from _EnterTree rather than _Ready so it survives being moved:
  // UIGridRow reparents the select button (and this driver with it) into the row
  // as the settings screen builds itself, which fires _ExitTree on a node whose
  // _Ready will never run a second time. Paired the other way round, everything
  // here would come unsubscribed the moment the screen finished loading.
  public override void _EnterTree() {
    base._EnterTree();
    if (!_isSubscribed) {
      Input.JoyConnectionChanged += OnJoyConnectionChanged;
      EventHandler.Instance.Events.LastUsedControllerChanged += OnLastUsedControllerChanged;
      _isSubscribed = true;
    }
  }

  public override void _ExitTree() {
    base._ExitTree();
    if (_isSubscribed) {
      Input.JoyConnectionChanged -= OnJoyConnectionChanged;
      EventHandler.Instance.Events.LastUsedControllerChanged -= OnLastUsedControllerChanged;
      _isSubscribed = false;
    }
  }

  // Keyboard and gamepad are live at the same time, so this select isn't really
  // a choice the player makes once: it shows whichever device they last touched.
  // Moving it re-runs onItemSelected, which is what carries the change on to the
  // key binding rows below it.
  private void OnLastUsedControllerChanged(int controllerType) {
    var type = (ControllerType)controllerType;
    if (type == SelectedControllerType) {
      // Same kind of device, so this is one pad swapped for another: the item
      // names the pad, so it has to be built again to name the new one.
      if (type == ControllerType.Gamepad) {
        RefreshControllerList();
      }
      return;
    }

    // Nothing to show for a gamepad that isn't in the list (none connected).
    if (!_availableControllers.Contains(type)) {
      return;
    }
    (GetParent() as UISelectButton)?.SyncSelectionToDefault();
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
    SelectedControllerType = InputUtils.GetEffectiveControllerType();
  }

  // Returns true if a gamepad is currently connected.
  public bool IsGamepadConnected() => InputUtils.IsGamepadConnected();
}
