namespace Wfc.Utils;

using System;
using Godot;

public static class FocusHelpers {
  private static readonly StringName BLINK_ANIMATION = "Blink";
  private static readonly StringName RESET_ANIMATION = "RESET";

  // Runs the widget's "Blink" clip while it holds focus and its "RESET" clip when it
  // loses it. Four widgets did this by hand, two of them by calling Play every single
  // frame from _Process rather than waiting to be told focus had moved.
  public static void BlinkWhileFocused(this Control control, AnimationPlayer player) {
    control.FocusEntered += () => _play(player, BLINK_ANIMATION);
    control.FocusExited += () => _play(player, RESET_ANIMATION);
  }

  private static void _play(AnimationPlayer player, StringName animation) {
    player.Stop();
    player.Play(animation);
  }

  // Pointing at a widget focuses it.
  //
  // Nine widgets had grown their own version of this, half wired through a
  // mouse_entered connection in a scene file and half in code, and they disagreed on
  // the details: one guarded against stealing focus mid key-capture, none checked
  // whether the target could take focus at all, and one was connected OneShot so it
  // worked exactly once.
  //
  // Call from _Ready, not _EnterTree: settings widgets are reparented as their screen
  // builds itself, and _EnterTree would subscribe again on every move.
  //
  // The hovered area and the control that ends up focused are often different nodes,
  // a panel whose inner Button is the focusable part. Pass canFocus when the widget
  // has a reason to decline, such as a key capture in progress.
  public static void GrabFocusOnHover(this Control hoverArea, Control? focusTarget = null, Func<bool>? canFocus = null) {
    var target = focusTarget ?? hoverArea;
    hoverArea.MouseEntered += () => {
      // Grabbing focus on a control that cannot take it only logs an error.
      if (target.FocusMode == Control.FocusModeEnum.None || canFocus?.Invoke() == false) {
        return;
      }
      target.GrabFocus();
    };
  }
}
