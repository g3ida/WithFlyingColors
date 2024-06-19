using Godot;

public partial class ScoreBoard : Node2D
{
    private Label ScoreNode;
    private Label LevelNode;
    private Label HighScoreNode;

    private int score;
    private int level;
    private int highScore;

    public override void _Ready()
    {
        ScoreNode = GetNode<Label>("Score");
        LevelNode = GetNode<Label>("Level");
        HighScoreNode = GetNode<Label>("HiScore2");

        SetScore(0);
        SetLevel(1);
    }

    public void SetHighScore(int _score)
    {
        highScore = _score;
        HighScoreNode.Text = string.Format("SCORE: {0:0000}", highScore);
    }

    public void SetScore(int _score)
    {
        score = _score;
        ScoreNode.Text = string.Format("SCORE:  {0:0000}", score);
    }

    public void SetLevel(int _level)
    {
        level = _level;
        LevelNode.Text = string.Format("LEVEL:  {0:0000}", level);
    }
}
