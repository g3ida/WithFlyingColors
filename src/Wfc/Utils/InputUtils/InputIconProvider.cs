namespace Wfc.Utils;

using Wfc.Core.Input.Controllers;

// Entry point to the input icon providers: hands out the one that draws
// bindings the way the given device does.
public static class InputIconProvider {
  private static readonly KeyboardIconProvider _keyboard = new();
  private static readonly GamepadIconProvider _gamepad = new();

  public static IInputIconProvider For(ControllerType type) =>
      type == ControllerType.Gamepad ? _gamepad : _keyboard;
}
