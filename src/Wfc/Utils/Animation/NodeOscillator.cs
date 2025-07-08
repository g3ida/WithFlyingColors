namespace Wfc.Utils.Animation;

using Godot;

public partial class NodeOscillator : Node {
  private Vector2 _initialPosition;
  private Node2D _node;
  private float _amplitude = 1.0f;
  private float _duration = 1.0f;

  public float Timer { get; private set; }

  public NodeOscillator(Node2D node, float amplitude, float duration) {
    this._node = node;
    this._amplitude = amplitude;
    this._duration = duration;
    _initialPosition = node.Position;
  }

  public void Update(float delta) {
    Timer += delta;
    if (Timer > _duration) {
      Timer = 0;
    }
    _node.Position = new Vector2(
      _node.Position.X,
      _initialPosition.Y + _amplitude * Mathf.Sin(2 * Mathf.Pi * Timer / _duration)
    );
  }
}
