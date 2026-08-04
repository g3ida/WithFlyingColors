namespace Wfc.Entities.Ui;

using Godot;

// A one-line label that never asks its parent for more than MaxWidth. Text that
// doesn't fit is clipped to the box and slid back and forth inside it, so an
// over-long value - the full name a driver gives a gamepad, say - stays readable
// without pushing the row it sits in past the edge of the panel.
//
// The inner label is built here rather than in a scene: it carries no settings a
// designer would want to reach, and this way the control can be dropped straight
// into an existing layout in place of a Label.
public partial class MarqueeLabel : Control {
  #region Constants
  // Pixels a second the text travels. Slow enough to read on the way past.
  private const float SCROLL_SPEED = 70f;
  // How long it rests at each end before setting off again.
  private const float END_PAUSE = 1.5f;
  // Under a pixel of overflow is rounding, not text that needs scrolling.
  private const float OVERFLOW_EPSILON = 1f;
  #endregion Constants

  #region Exports
  // The widest this control will ever ask for. Anything longer scrolls instead.
  [Export]
  public float MaxWidth { get; set; } = 380f;
  #endregion Exports

  #region Nodes
  private readonly Label _labelNode = new() {
    MouseFilter = MouseFilterEnum.Ignore,
    VerticalAlignment = VerticalAlignment.Center,
  };
  #endregion Nodes

  private Tween? _scroller;
  private bool _isSubscribed;

  // The inner label is only there from _Ready onwards, so this guards every read of
  // it that a resize could reach first.
  private bool _isWired;

  // The inner label normally reads its colour off the theme of the screen this
  // control sits in. A control placed on a surface that theme wasn't made for
  // (the pause overlay's dark panel) hands the colour in directly instead.
  public void SetFontColor(Color color) =>
      _labelNode.AddThemeColorOverride("font_color", color);

  public string Text {
    get => _labelNode.Text;
    set {
      if (_labelNode.Text == value) {
        return;
      }
      _labelNode.Text = value;
      UpdateMinimumSize();
      _scheduleLayout();
    }
  }

  // Subscribed from _EnterTree rather than _Ready so it survives a reparent:
  // UIGridRow moves the select button this sits in into the row while the settings
  // screen builds itself, which fires _ExitTree on a node whose _Ready has yet to
  // run - and only ever runs once. Paired the other way round, the first move both
  // tried to drop a connection that was never made and left the control deaf to
  // every resize after it.
  public override void _EnterTree() {
    base._EnterTree();
    if (!_isSubscribed) {
      Resized += _scheduleLayout;
      _isSubscribed = true;
    }
    _scheduleLayout();
  }

  public override void _Ready() {
    base._Ready();
    ClipContents = true;
    MouseFilter = MouseFilterEnum.Ignore;
    AddChild(_labelNode);
    _isWired = true;
    _scheduleLayout();
  }

  public override void _ExitTree() {
    base._ExitTree();
    if (_isSubscribed) {
      Resized -= _scheduleLayout;
      _isSubscribed = false;
    }
    _stopScrolling();
  }

  // As wide as its text, up to the cap. Past that the text moves instead of the row.
  public override Vector2 _GetMinimumSize() {
    var text = _labelNode.GetMinimumSize();
    return new Vector2(Mathf.Min(text.X, MaxWidth), text.Y);
  }

  // Deferred because both triggers - a new string, and the parent handing this
  // control its width - are read back from a layout the engine has yet to settle.
  private void _scheduleLayout() => Callable.From(_applyLayout).CallDeferred();

  private void _applyLayout() {
    if (!_isWired || !IsInsideTree()) {
      return;
    }

    _stopScrolling();
    var text = _labelNode.GetMinimumSize();
    _labelNode.Size = new Vector2(text.X, Size.Y);

    var overflow = text.X - Size.X;
    if (overflow <= OVERFLOW_EPSILON) {
      // Sits where a plain centred Label would, so rows that fit look untouched.
      _labelNode.Position = new Vector2(Mathf.Round((Size.X - text.X) * 0.5f), 0f);
      return;
    }

    _labelNode.Position = Vector2.Zero;
    var travelTime = overflow / SCROLL_SPEED;
    _scroller = CreateTween().SetLoops();
    _scroller.TweenInterval(END_PAUSE);
    _scroller.TweenProperty(_labelNode, "position:x", -overflow, travelTime)
        .SetTrans(Tween.TransitionType.Sine)
        .SetEase(Tween.EaseType.InOut);
    _scroller.TweenInterval(END_PAUSE);
    _scroller.TweenProperty(_labelNode, "position:x", 0f, travelTime)
        .SetTrans(Tween.TransitionType.Sine)
        .SetEase(Tween.EaseType.InOut);
  }

  // A tween is bound to the node that made it, so leaving the tree can free this one
  // out from under the reference kept here.
  private void _stopScrolling() {
    if (_scroller != null && IsInstanceValid(_scroller)) {
      _scroller.Kill();
    }
    _scroller = null;
  }
}
