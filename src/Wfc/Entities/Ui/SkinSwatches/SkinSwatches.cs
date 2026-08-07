namespace Wfc.Entities.Ui;

using Godot;
using Wfc.Skin;
using Wfc.Utils.Attributes;
using EventHandler = Wfc.Core.Event.EventHandler;

// The four colours the game is played in, side by side, in the order the player meets
// them on their own four faces. Shows what a palette looks like rather than only what
// it is called, which is the only thing that can answer "which of these can you tell
// apart" - and the only thing that makes a name like "Clear" mean anything.
//
// Repaints itself whenever the palette changes, so a host can drop one in and forget
// about it. The four blocks are built here rather than in the scene: they carry nothing
// a designer would want to reach except their size, which is exported.
[ScenePath]
public partial class SkinSwatches : HBoxContainer {
  #region Exports
  // A settings row wants them small enough to sit in it; a screen given over to the
  // question wants them big enough to judge.
  [Export]
  public Vector2 SwatchSize { get; set; } = new(150, 150);
  #endregion Exports

  private static readonly SkinColor[] FACES =
    [SkinColor.TopFace, SkinColor.LeftFace, SkinColor.BottomFace, SkinColor.RightFace];

  private readonly ColorRect[] _swatches = new ColorRect[FACES.Length];
  private bool _isSubscribed;
  private bool _isBuilt;

  // Subscribed from _EnterTree rather than _Ready so it survives a reparent: a settings
  // row moves its content into itself while the screen builds, which fires _ExitTree on
  // a node whose _Ready has yet to run.
  public override void _EnterTree() {
    base._EnterTree();
    if (!_isSubscribed) {
      EventHandler.Instance.Events.SkinChanged += _onSkinChanged;
      _isSubscribed = true;
    }
  }

  public override void _ExitTree() {
    base._ExitTree();
    if (_isSubscribed) {
      EventHandler.Instance.Events.SkinChanged -= _onSkinChanged;
      _isSubscribed = false;
    }
  }

  public override void _Ready() {
    base._Ready();
    MouseFilter = MouseFilterEnum.Ignore;
    for (var i = 0; i < _swatches.Length; i++) {
      _swatches[i] = new ColorRect {
        CustomMinimumSize = SwatchSize,
        MouseFilter = MouseFilterEnum.Ignore,
      };
      AddChild(_swatches[i]);
    }
    _isBuilt = true;
    Repaint();
  }

  public void Repaint() {
    // The palette can change before the blocks that show it exist.
    if (!_isBuilt) {
      return;
    }
    var colors = SkinManager.Instance.CurrentSkin.GetColors(SkinColorIntensity.Basic);
    for (var i = 0; i < _swatches.Length; i++) {
      _swatches[i].Color = colors[FACES[i]];
    }
  }

  private void _onSkinChanged(string skin) => Repaint();
}
