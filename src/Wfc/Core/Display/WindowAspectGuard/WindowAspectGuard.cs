namespace Wfc.Core.Display;

using Godot;

// A window the player drags keeps the shape the game is drawn at.
//
// The stretch mode is keep_height, so a window let go of at another shape does not
// scale the view - it widens or narrows it, and the menus are laid out for one
// width with only so much bleed to give. Whichever edge was pulled furthest is the
// one taken as meant; the other follows from it.
//
// Only windowed drags are answered. Every size the game sets itself is already the
// right shape, so those pass through untouched.
public partial class WindowAspectGuard : Node {
  // Below this the settings panel no longer fits what it holds. It is the smallest
  // size the resolution row offers, so the two agree on how small is too small.
  private static readonly Vector2I MINIMUM_SIZE = new(800, 450);

  private Vector2I _lastSize;
  private bool _isCorrecting;
  private bool _isSubscribed;

  public override void _EnterTree() {
    base._EnterTree();
    if (!_isSubscribed) {
      GetWindow().SizeChanged += _onWindowSizeChanged;
      _isSubscribed = true;
    }
    _lastSize = DisplayServer.WindowGetSize();
    DisplayServer.WindowSetMinSize(MINIMUM_SIZE);
  }

  public override void _ExitTree() {
    base._ExitTree();
    if (_isSubscribed) {
      GetWindow().SizeChanged -= _onWindowSizeChanged;
      _isSubscribed = false;
    }
  }

  private void _onWindowSizeChanged() {
    var size = DisplayServer.WindowGetSize();
    if (_isCorrecting || DisplayServer.WindowGetMode() != DisplayServer.WindowMode.Windowed) {
      _lastSize = size;
      return;
    }

    var corrected = ToAspect(size, _lastSize, _gameAspect(),
        DisplayServer.ScreenGetSize(DisplayServer.WindowGetCurrentScreen()));
    _lastSize = corrected;
    if (corrected == size) {
      return;
    }
    // Writing the size raises this again. The second pass finds a size already the
    // right shape and stops there, so the flag is a guard rather than the thing
    // that ends it.
    _isCorrecting = true;
    DisplayServer.WindowSetSize(corrected);
    _isCorrecting = false;
  }

  /// <summary>
  /// The size a window dragged from <paramref name="lastSize"/> to
  /// <paramref name="size"/> should end at to keep the given aspect, brought inside
  /// the screen. Whichever edge moved furthest is taken as the one the player meant;
  /// the other follows from it. A zero screen means no limit.
  /// </summary>
  public static Vector2I ToAspect(Vector2I size, Vector2I lastSize, float aspect, Vector2I screen) {
    var pulledVertically = Mathf.Abs(size.Y - lastSize.Y) > Mathf.Abs(size.X - lastSize.X);
    var width = pulledVertically ? size.Y * aspect : size.X;

    if (screen != Vector2I.Zero) {
      width = Mathf.Min(width, Mathf.Min(screen.X, screen.Y * aspect));
    }
    return new Vector2I(Mathf.RoundToInt(width), Mathf.RoundToInt(width / aspect));
  }

  private static float _gameAspect() {
    var width = (int)ProjectSettings.GetSetting("display/window/size/viewport_width");
    var height = (int)ProjectSettings.GetSetting("display/window/size/viewport_height");
    return (float)width / height;
  }
}
