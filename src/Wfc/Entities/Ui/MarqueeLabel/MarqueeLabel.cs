namespace Wfc.Entities.Ui;

using System.Collections.Generic;
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

  // The size the text is drawn at. Zero leaves the label whatever the theme gives it.
  [Export]
  public int FontSize {
    get => _fontSize;
    set {
      _fontSize = value;
      _applyFontSize();
    }
  }
  #endregion Exports

  #region Nodes
  private readonly Label _labelNode = new() {
    MouseFilter = MouseFilterEnum.Ignore,
    VerticalAlignment = VerticalAlignment.Center,
  };
  #endregion Nodes

  private static readonly StringName FONT = "font";
  private static readonly StringName FONT_SIZE = "font_size";

  private Tween? _scroller;
  private bool _isSubscribed;
  private int _fontSize;
  private IReadOnlyList<string> _reservedTexts = [];
  private float _reservedWidth;
  private bool _alignLeft;

  // The inner label is only there from _Ready onwards, so this guards every read of
  // it that a resize could reach first.
  private bool _isWired;

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
    MouseFilter = MouseFilterEnum.Ignore;
    AddChild(_labelNode);
    _isWired = true;
    // Both arrive before the label they describe exists.
    _applyFontSize();
    _applyReservation();
    _scheduleLayout();
  }

  private void _applyFontSize() {
    if (!_isWired) {
      return;
    }
    if (_fontSize > 0) {
      _labelNode.AddThemeFontSizeOverride(FONT_SIZE, _fontSize);
    }
    else {
      _labelNode.RemoveThemeFontSizeOverride(FONT_SIZE);
    }
    // The reservation is a width in pixels, so it means something different now.
    _applyReservation();
    UpdateMinimumSize();
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

  /// <summary>
  /// Whether the text sits at the left of the room reserved for it rather than in the
  /// middle of it. A value kept between a pair of arrows reads better centred; one
  /// that opens a list underneath has to stay on the column the list writes its
  /// options down, whatever the value happens to be.
  /// </summary>
  public bool AlignLeft {
    get => _alignLeft;
    set {
      _alignLeft = value;
      _scheduleLayout();
    }
  }

  /// <summary>
  /// Keeps room for the longest of these however short the text currently is, so a
  /// control that cycles through them keeps one width throughout. Whatever is beside
  /// it - a picker's arrows - then stays where it is instead of walking in and out as
  /// the value changes under it.
  /// </summary>
  public void ReserveWidthFor(IReadOnlyList<string> texts) {
    _reservedTexts = texts;
    _applyReservation();
  }

  // Measured against the label's own font, so it has to be measured again whenever the
  // size that font is drawn at changes.
  private void _applyReservation() {
    if (!_isWired) {
      return;
    }
    var font = _labelNode.GetThemeFont(FONT);
    var fontSize = _labelNode.GetThemeFontSize(FONT_SIZE);
    _reservedWidth = 0f;
    foreach (var text in _reservedTexts) {
      _reservedWidth = Mathf.Max(_reservedWidth, font.GetStringSize(text, fontSize: fontSize).X);
    }
    UpdateMinimumSize();
    _scheduleLayout();
  }

  // As wide as its text or its reservation, whichever is wider, up to the cap. Past
  // the cap the text moves instead of the row.
  public override Vector2 _GetMinimumSize() {
    var text = _labelNode.GetMinimumSize();
    return new Vector2(Mathf.Min(Mathf.Max(text.X, _reservedWidth), MaxWidth), text.Y);
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
    // Only text that has to be held in is clipped. Clipping is to the box on both
    // axes, and the box is exactly a line tall, so a clip kept on for text that fits
    // buys nothing and shaves the accents that reach above the ascent - the tilde
    // came off the N of "Español".
    ClipContents = overflow > OVERFLOW_EPSILON;
    if (overflow <= OVERFLOW_EPSILON) {
      // Sits where a plain centred Label would, so rows that fit look untouched.
      _labelNode.Position = new Vector2(
          _alignLeft ? 0f : Mathf.Round((Size.X - text.X) * 0.5f), 0f);
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
