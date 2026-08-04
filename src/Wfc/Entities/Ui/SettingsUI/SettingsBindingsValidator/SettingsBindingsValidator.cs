namespace Wfc.Entities.Ui.SettingsUI;

using Wfc.Core.Input.Controllers;
using Wfc.Core.Settings;
using Wfc.Utils;

// Whether the current bindings may be walked away from. Both hosts of the
// settings panel - the standalone screen and the pause overlay - refuse to
// close or change tab over a broken mapping, and they have to refuse for the
// same reasons or the two would disagree about what counts as broken.
public static class SettingsBindingsValidator {

  public static bool IsValidState() => HasAllRequiredBindings() && HasNoDuplicateBindings();

  private static bool HasAllRequiredBindings() {
    // Check keyboard bindings if keyboard is selected
    if (GameSettings.LastUsedController == ControllerType.Keyboard) {
      return GameSettings.AreActionKeysValid();
    }
    // Check gamepad bindings if gamepad is selected and connected
    if (GameSettings.LastUsedController == ControllerType.Gamepad && InputUtils.IsGamepadConnected()) {
      return GameSettings.AreGamepadBindingsValid();
    }
    // Default to checking keyboard bindings
    return GameSettings.AreActionKeysValid();
  }

  // A key on two actions is broken on any device, but a pad can only be remapped
  // while one is plugged in, so its duplicates only hold the player here then.
  private static bool HasNoDuplicateBindings() {
    if (GameSettings.HasDuplicateKeyboardBindings()) {
      return false;
    }
    return !InputUtils.IsGamepadConnected() || !GameSettings.HasDuplicateGamepadBindings();
  }
}
