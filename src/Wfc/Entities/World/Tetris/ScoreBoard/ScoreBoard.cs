namespace Wfc.Entities.Tetris;

using Godot;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[ScenePath]
public partial class ScoreBoard : Node2D {
  #region Nodes
  [NodePath("Score")]
  private Label _scoreNode = default!;
  [NodePath("Level")]
  private Label _levelNode = default!;
  [NodePath("HiScore2")]
  private Label _highScoreNode = default!;
  #endregion Nodes

  private int _score;
  private int _level;
  private int _highScore;

  public override void _Ready() {
    base._Ready();
    this.WireNodes();

    SetScore(0);
    SetLevel(1);
  }

  public void SetHighScore(int _score) {
    _highScore = _score;
    _highScoreNode.Text = string.Format("SCORE: {0:0000}", _highScore);
  }

  public void SetScore(int _score) {
    this._score = _score;
    _scoreNode.Text = string.Format("SCORE:  {0:0000}", this._score);
  }

  public void SetLevel(int _level) {
    this._level = _level;
    _levelNode.Text = string.Format("LEVEL:  {0:0000}", this._level);
  }
}
