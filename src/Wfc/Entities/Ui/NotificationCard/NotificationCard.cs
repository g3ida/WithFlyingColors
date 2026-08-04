namespace Wfc.Entities.Ui;

using Godot;
using Wfc.Utils;
using Wfc.Utils.Attributes;

// One line in the corner of the screen: a dark bar with a coloured edge that slides in from the
// side it will leave by, holds long enough to be read without being looked at, and frees itself on
// the way out. Nothing outside has to remember it exists once it has been handed its message.
[ScenePath]
public partial class NotificationCard : Control {
  #region Constants
  private const float SLIDE_IN = 0.34f;
  private const float HOLD = 2.6f;
  private const float SLIDE_OUT = 0.26f;
  // How far off its resting place the bar starts and ends, against its own width, so the travel is
  // the same whatever width the stack gives it.
  private const float TRAVEL = 1.1f;
  #endregion Constants

  #region Nodes
  [NodePath("Body")]
  private Control _bodyNode = default!;
  [NodePath("Body/Band")]
  private ColorRect _bandNode = default!;
  [NodePath("Body/Message")]
  private Label _messageNode = default!;
  #endregion Nodes

  private string _message = string.Empty;
  private Color _bandColor = Colors.White;
  private Tween? _run;
  private bool _isLeaving;

  // Called before the card is in the tree, the way the stack builds it - both are applied once
  // _Ready has the nodes to apply them to.
  public void Configure(string message, Color band) {
    _message = message;
    _bandColor = band;
  }

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _messageNode.Text = _message;
    _bandNode.Color = _bandColor;
    _centreOnTheCapitals();
    _show();
  }

  // Cut short, because something newer wants the room. A card already leaving is left to leave.
  public void Dismiss() {
    if (_isLeaving) {
      return;
    }
    _run?.Kill();
    _run = _leave();
  }

  // A Label centres the line box the font asks for, and that box keeps room under the baseline for
  // letters that hang below it. This bar only ever draws capitals, in every language it is written
  // in, so that room is empty and the words sit high in the bar. Giving half of it back puts the
  // capitals themselves in the middle.
  private void _centreOnTheCapitals() {
    var fontSize = _messageNode.GetThemeFontSize("font_size");
    var nudge = _messageNode.GetThemeFont("font").GetDescent(fontSize) * 0.5f;
    _messageNode.OffsetTop = nudge;
    _messageNode.OffsetBottom = nudge;
  }

  private void _show() {
    var travel = _travel();
    _bodyNode.Position = new Vector2(travel, 0.0f);
    _bodyNode.Modulate = new Color(1.0f, 1.0f, 1.0f, 0.0f);

    _run = CreateTween();
    _run.TweenProperty(_bodyNode, "position:x", 0.0f, SLIDE_IN)
      .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
    _run.Parallel().TweenProperty(_bodyNode, "modulate:a", 1.0f, SLIDE_IN * 0.6f);
    _run.TweenInterval(HOLD);
    _run.TweenCallback(Callable.From(() => _run = _leave()));
  }

  private Tween _leave() {
    _isLeaving = true;
    var tween = CreateTween();
    tween.TweenProperty(_bodyNode, "position:x", _travel(), SLIDE_OUT)
      .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.In);
    tween.Parallel().TweenProperty(_bodyNode, "modulate:a", 0.0f, SLIDE_OUT);
    tween.TweenCallback(Callable.From(QueueFree));
    return tween;
  }

  private float _travel() => Mathf.Max(Size.X, CustomMinimumSize.X) * TRAVEL;
}
