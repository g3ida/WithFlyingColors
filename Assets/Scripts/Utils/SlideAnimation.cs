using Godot;

public partial class SlideAnimation : Node2D
{
    private Node2D thisNode;
    private Vector2 destination;
    private bool notified = false;
    private string animationName;

    private float duration;
    private float timer;
    private Vector2 oldPosition;

    public SlideAnimation()
    {
        // FIXME remove this constructor after c# migration
    }

    public void Set(string _animationName, Node2D _this, Vector2 _destination, float _duration)
    {
        thisNode = _this;
        destination = _destination;
        animationName = _animationName;
        oldPosition = thisNode.Position;
        duration = _duration;
        timer = 0;
    }

    public void Update(float delta)
    {
        timer += delta;
        if (timer > duration)
        {
            timer = duration;
        }

        float weight = timer / duration;
        thisNode.Position = thisNode.Position.Lerp(destination, weight);

        if (!notified && thisNode.Position == destination)
        {
            notified = true;
            Notify();
        }
    }

    private void Notify()
    {
        Event.Instance().EmitSlideAnimationEnded(animationName);
    }
}
