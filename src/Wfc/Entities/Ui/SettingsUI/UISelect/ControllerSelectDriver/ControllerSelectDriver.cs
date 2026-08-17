namespace Wfc.Entities.Ui.SettingsUI.UISelect;

using Chickensoft.Sync.Primitives;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Event;
using Wfc.Core.Input.Controllers;
using Wfc.Core.Localization;
using Wfc.Core.Settings;
using Wfc.Entities.Ui;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class ControllerSelectDriver : UISelectDriver {
  private AutoChannel.Binding? _controllerBinding;


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

  // Set once the player has moved this row themselves, and left set for as long
  // as the settings screen is open (this driver is freed with it).
  //
  // The row reads as a device setting but isn't one: both devices keep working
  // whichever it shows, it only picks which set of key bindings the rows below
  // it display. So a player who moves it is asking to read the other device's
  // bindings, and the automatic follow has to stop overruling them.
  //
  // Without this the row cannot be moved back to the gamepad at all. The pad
  // press asking for it reaches InputDeviceDetector first, which sees the device
  // kind change back and snaps the selection to the gamepad; the left/right that
  // follows later in the same frame then steps straight off it again, so every
  // press lands back on the keyboard.
  private bool _isManuallySelected;

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

  // onItemSelected can't announce this: it also carries the row being drawn for
  // the first time and following the active device, neither of which is a menu
  // action the player took, and the sfx is wired to this event.
  public override void OnUserSelectionChanged() {
    _isManuallySelected = true;
    GameEvents.Instance.OnControllerSelectionChanged(SelectedControllerType);
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
    _controllerBinding ??= GameEvents.Instance.Channel.Bind()
      .On((in IGameEvents.LastUsedControllerChanged m) => OnLastUsedControllerChanged(m.Controller));
      _isSubscribed = true;
    }
  }

  public override void _ExitTree() {
    base._ExitTree();
    if (_isSubscribed) {
      Input.JoyConnectionChanged -= OnJoyConnectionChanged;
    _controllerBinding?.Dispose();
    _controllerBinding = null;
      _isSubscribed = false;
    }
  }

  // Keyboard and gamepad are live at the same time, so until the player says
  // otherwise this select shows whichever device they last touched. Moving it
  // re-runs onItemSelected, which is what carries the change on to the key
  // binding rows below it.
  private void OnLastUsedControllerChanged(ControllerType controllerType) {
    var type = (ControllerType)controllerType;
    if (type == SelectedControllerType) {
      // Same kind of device, so this is one pad swapped for another: the item
      // names the pad, so it has to be built again to name the new one.
      if (type == ControllerType.Gamepad) {
        RefreshControllerList();
      }
      return;
    }

    // The player has pointed the row at a device themselves, so it stays there
    // even while they go on playing with the other one.
    if (_isManuallySelected) {
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
      GameEvents.Instance.OnGamepadConnected((int)deviceId, deviceName);
    }
    else {
      GameEvents.Instance.OnGamepadDisconnected((int)deviceId);
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
