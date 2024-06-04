using Godot;

public partial class NodeOscillator : Node
{
    private float timer = 0;
    private Vector2 initialPosition;
    private Node2D node;
    private float amplitude = 1.0f;
    private float duration = 1.0f;

    public float Timer { get { return timer; } }
    public NodeOscillator()
    {
        // FIXME remove this constructor after c# migration
    }

    public void Set(Node2D _node, float _amplitude, float _duration)
    {
        node = _node;
        amplitude = _amplitude;
        duration = _duration;
        initialPosition = _node.Position;
    }

    public void Update(float delta)
    {
        timer += delta;
        if (timer > duration)
        {
            timer = 0;
        }
        node.Position = new Vector2(node.Position.X, initialPosition.Y + amplitude * Mathf.Sin(2 * Mathf.Pi * timer / duration));
    }
}
