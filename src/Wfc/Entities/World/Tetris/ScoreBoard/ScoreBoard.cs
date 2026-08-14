namespace Wfc.Entities.Tetris;

using Godot;
using Wfc.Skin;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using Wfc.Utils.Colors;

[ScenePath]
public partial class ScoreBoard : Node2D {
  #region Constants
  // One pip per line still owed to the next level, so the row empties and refills as the level
  // turns over. Read off the same arithmetic the pool levels up by.
  public const int LINES_PER_LEVEL = 10;

  private const float PIP_WIDTH = 38.0f;
  private const float PIP_HEIGHT = 14.0f;
  private const float PIP_GAP = 6.0f;
  private const float PIP_LIT_ALPHA = 1.0f;
  private const float PIP_UNLIT_ALPHA = 0.16f;
  private const float PIP_FADE = 0.18f;
  #endregion Constants

  #region Nodes
  [NodePath("Score")]
  private ScoreBlinkingLabel _scoreNode = default!;
  [NodePath("Level")]
  private ScoreBlinkingLabel _levelNode = default!;
  [NodePath("HiScore2")]
  private Label _highScoreNode = default!;
  [NodePath("Pips")]
  private Node2D _pipsNode = default!;
  #endregion Nodes

  private readonly ColorRect[] _pips = new ColorRect[LINES_PER_LEVEL];
  // Where each pip is heading. A pip that is part way through a fade still reports the colour it
  // is passing through, so the live one cannot be used to decide whether anything needs doing.
  private readonly Color[] _pipTargets = new Color[LINES_PER_LEVEL];
  private readonly Tween?[] _pipFades = new Tween?[LINES_PER_LEVEL];
  private int _score;
  // Levels are counted from one. Left at zero it would spend the first refresh a level below the
  // bottom, and the colours wrap, so the board opened wearing the last level's colour instead.
  private int _level = 1;
  private int _highScore;

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _buildPips();

    SetScore(0);
    SetLevel(1);
  }

  private void _buildPips() {
    for (var i = 0; i < _pips.Length; i++) {
      var unlit = _levelColor(_level) with { A = PIP_UNLIT_ALPHA };
      var pip = new ColorRect {
        Position = new Vector2(i * (PIP_WIDTH + PIP_GAP), 0.0f),
        Size = new Vector2(PIP_WIDTH, PIP_HEIGHT),
        Color = unlit,
      };
      _pipsNode.AddChild(pip);
      _pips[i] = pip;
      _pipTargets[i] = unlit;
    }
  }

  // Each level wears the next colour in the cube's own set, so the board turns over with the
  // pool rather than staying one fixed shade all the way up.
  private static Color _levelColor(int level) {
    var group = ColorUtils.COLOR_GROUPS[Mathf.PosMod(level - 1, ColorUtils.COLOR_GROUPS.Length)];
    return SkinManager.Instance.CurrentSkin.GetColor(
      GameSkin.ColorGroupToSkinColor(group),
      SkinColorIntensity.Basic
    );
  }

  private void _refreshPips() {
    var lit = Mathf.PosMod(_score, LINES_PER_LEVEL);
    var color = _levelColor(_level);
    for (var i = 0; i < _pips.Length; i++) {
      var wanted = color with { A = i < lit ? PIP_LIT_ALPHA : PIP_UNLIT_ALPHA };
      if (_pipTargets[i] == wanted) {
        continue;
      }
      _pipTargets[i] = wanted;
      // Killed rather than left running: two fades on the one colour fight for it frame by frame,
      // and which of them lands is down to the order the tweens happen to be stepped in.
      _pipFades[i]?.Kill();
      // Tweened rather than set, so the pip that has just been earned lights up instead of
      // appearing between two frames along with the score it belongs to.
      _pipFades[i] = _pips[i].CreateTween();
      _pipFades[i]!.TweenProperty(_pips[i], "color", wanted, PIP_FADE);
    }
  }

  public void SetHighScore(int highScore) {
    _highScore = highScore;
    _highScoreNode.Text = string.Format("SCORE: {0:0000}", _highScore);
  }

  public void SetScore(int score) {
    _score = score;
    _scoreNode.SetValue(string.Format("SCORE:  {0:0000}", _score));
    _refreshPips();
  }

  public void SetLevel(int level) {
    _level = level;
    _levelNode.SetValue(string.Format("LEVEL:  {0:0000}", _level));
    // The font colour rather than the modulate: the blink the label plays on every change owns
    // its modulate, and would wash this straight back out to white.
    _levelNode.AddThemeColorOverride("font_color", _levelColor(_level));
    _refreshPips();
  }
}
