namespace Wfc.test.instrumented;

using System;
using System.Collections.Generic;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Utils;

// The pad art ships twice: once for the light panels the menus draw it on, once
// inverted for hints painted onto the level itself. A missing file in either set
// resolves to null, and the hint quietly degrades to a key cap carrying the button's
// name - which is exactly the sort of thing nobody notices until a playtest.
public class GamepadIconHelperTests(Node testScene) : TestClass(testScene) {
  private static readonly GamepadIconHelper.ControllerIconType[] ICON_TYPES = [
    GamepadIconHelper.ControllerIconType.Xbox360,
    GamepadIconHelper.ControllerIconType.PlayStation
  ];

  [Test]
  public void EveryButtonDrawnOnLightIsAlsoDrawnOnDark() {
    foreach (var iconType in ICON_TYPES) {
      foreach (var button in Enum.GetValues<JoyButton>()) {
        if (GamepadIconHelper.GetButtonIcon(button, iconType) == null) {
          continue;
        }
        GamepadIconHelper.GetButtonIcon(button, iconType, onDarkBackground: true)
          .ShouldNotBeNull($"{iconType} {button} has no inverted art");
      }
    }
  }

  [Test]
  public void EveryAxisDrawnOnLightIsAlsoDrawnOnDark() {
    foreach (var iconType in ICON_TYPES) {
      foreach (var axis in Enum.GetValues<JoyAxis>()) {
        foreach (var direction in new[] { -1f, 1f }) {
          if (GamepadIconHelper.GetAxisIcon(axis, direction, iconType) == null) {
            continue;
          }
          GamepadIconHelper.GetAxisIcon(axis, direction, iconType, onDarkBackground: true)
            .ShouldNotBeNull($"{iconType} {axis} {direction} has no inverted art");
        }
      }
    }
  }

  [Test]
  public void TheDirectionalPadIsDrawnOnBothSurfaces() {
    foreach (var iconType in ICON_TYPES) {
      GamepadIconHelper.GetDirectionalPadIcon(iconType).ShouldNotBeNull();
      GamepadIconHelper.GetDirectionalPadIcon(iconType, onDarkBackground: true)
        .ShouldNotBeNull($"{iconType} has no inverted d-pad art");
    }
  }

  // The two sets are different pictures of the same button, so resolving one must
  // never hand back the other's file.
  [Test]
  public void TheInvertedArtIsNotTheSameFileAsTheDefault() {
    var seen = new HashSet<string>();
    foreach (var iconType in ICON_TYPES) {
      var light = GamepadIconHelper.GetButtonIcon(JoyButton.A, iconType);
      var dark = GamepadIconHelper.GetButtonIcon(JoyButton.A, iconType, onDarkBackground: true);
      light.ShouldNotBeNull();
      dark.ShouldNotBeNull();
      light!.ResourcePath.ShouldNotBe(dark!.ResourcePath);
      seen.Add(dark.ResourcePath).ShouldBeTrue($"{iconType} reuses another pad's inverted art");
    }
  }
}
