namespace Wfc.Utils;

using Wfc.Core.Input.Controllers;

// Entry point to the input icon providers: hands out the one that draws
// bindings the way the given device does.
//
// Only the gamepad art comes in two shades. A key cap is its own light surface
// whatever it is drawn on, so the keyboard provider is shared by both.
public static class InputIconProvider {
  private static readonly KeyboardIconProvider _keyboard = new();
  private static readonly GamepadIconProvider _gamepad = new();
  private static readonly GamepadIconProvider _gamepadOnDark = new(onDarkBackground: true);

  public static IInputIconProvider For(ControllerType type, bool onDarkBackground = false) =>
      type == ControllerType.Gamepad
        ? onDarkBackground ? _gamepadOnDark : _gamepad
        : _keyboard;
}
