using Godot;
using System.Collections.Generic;

public partial class GemsTemple : Node2D
{
    // Constants
    private PackedScene TempleGemScene = GD.Load<PackedScene>("res://Assets/Scenes/Temple/TempleGem.tscn");
    private const float WAIT_DELAY = 0.1f;
    private const float MAX_GEM_TEMPLE_ANGULAR_VELOCITY = 10.0f * Mathf.Pi;
    private const float DURATION_TO_FULL_GEM_ROTATION_SPEED = 2.7f;
    private const float ROTATION_DURATION = 3.6f;
    private const float BLOOM_SPRITE_MAX_SCALE = 25.0f;
    private const float BLOOM_SPRITE_SCALE_DURATION = 1.2f;

    private readonly int[] GEMS_EASE_TYPE = {1, 1, 0, 0};

    private enum States
    {
      NOT_TRIGGERED,
      WALK_PHASE,
      COLLECT_PHASE,
      ROTATION_PHASE,
      BLOOM_PHASE,
      END_PHASE
    }

    private List<Node2D> templeGems = new List<Node2D>();
    private float gemsAngularVelocity = 0;
    private int numActiveGems = 0;
    private float bloomSpriteScale = 1.0f;
    private States currentState = States.NOT_TRIGGERED;

    // Nodes
    private Node2D GemSlotsContainerNode;
    private Godot.Collections.Array<Node> GemsSlotsNodes;
    private Area2D StartGemsAreaNode;
    private Timer RotationTimerNode;
    private Sprite2D BloomSpriteNode;

    private Tween tweener;

    public override void _Ready()
    {
        GemSlotsContainerNode = GetNode<Node2D>("GemsContainer");
        GemsSlotsNodes = GemSlotsContainerNode.GetChildren();
        StartGemsAreaNode = GetNode<Area2D>("StartGemsArea");
        RotationTimerNode = GetNode<Timer>("RotationTimer");
        BloomSpriteNode = GetNode<Sprite2D>("BloomSprite");

        BloomSpriteNode.Visible = false;
    }

    private void _on_TriggerArea_body_entered(Node body)
    {
        if (currentState == States.NOT_TRIGGERED && body == Global.Instance().Player)
        {
            GoToWalkPhase();
        }
    }

    private bool CreateTempleGems()
    {
        int i = 0;
        foreach (string colorGrp in Constants.COLOR_GROUPS)
        {
            if (Global.Instance().GemHUD.IsGemCollected(colorGrp))
            {
                var templeGem = CreateTempleGem(
                    colorGrp,
                    WAIT_DELAY * (i + 1),
                    Global.Instance().Player.GlobalPosition,
                    (GemsSlotsNodes[i] as Node2D).GlobalPosition,
                    GEMS_EASE_TYPE[i]);
                templeGems.Add(templeGem);
                numActiveGems += 1;
                i += 1;
            }
        }
        return i > 0;
    }

    private Node2D CreateTempleGem(string colorGroup, float delay, Vector2 position, Vector2 destination, int easeType)
    {
        var templeGem = TempleGemScene.Instantiate<TempleGem>();
        GemSlotsContainerNode.AddChild(templeGem);
        templeGem.Owner = GemSlotsContainerNode;
        templeGem.GlobalPosition = position;
        templeGem.SetColorGroup(colorGroup);
        var waitTime = delay;
        templeGem.MoveToPosition(destination, waitTime, easeType);
        templeGem.Connect(nameof(TempleGem.MoveCompletedEventHandler), new Callable(this, nameof(OnGemCollected)), (uint)ConnectFlags.OneShot);
        return templeGem;
    }

    public override void _Process(double delta)
    {
        switch (currentState)
        {
            case States.WALK_PHASE:
                Global.Instance().Player.SetMaxSpeed();
                break;
            case States.ROTATION_PHASE:
                ProcessRotateGems((float)delta);
                break;
            case States.BLOOM_PHASE:
                BloomSpriteNode.Scale = new Vector2(bloomSpriteScale, bloomSpriteScale);
                break;
        }
    }

    private void ProcessRotateGems(float delta)
    {
        var amount = gemsAngularVelocity * delta;
        GemSlotsContainerNode.Rotate(amount);
        foreach (Node node in GemSlotsContainerNode.GetChildren())
        {
            ((Node2D)node).Rotate(-amount);
        }
        var intensity = 0.5f + (gemsAngularVelocity / MAX_GEM_TEMPLE_ANGULAR_VELOCITY);
        foreach (Node2D node in templeGems)
        {
            node.Set("light_intensity", intensity);
        }
    }

    private void OnGemCollected(Node2D templeGem)
    {
        if (currentState == States.COLLECT_PHASE)
        {
            numActiveGems -= 1;
            Event.Instance().EmitGemPutInTemple();
            if (numActiveGems <= 0)
            {
                GoToRotationPhase();
            }
        }
    }

    private void StartRotationTimer()
    {
        RotationTimerNode.WaitTime = ROTATION_DURATION;
        RotationTimerNode.Start();
    }

    private void _on_StartGemsArea_body_entered(Node body)
    {
        if (currentState == States.WALK_PHASE && body == Global.Instance().Player)
        {
            GoToCollectPhase();
        }
    }

    private void _on_RotationTimer_timeout()
    {
        if (currentState == States.ROTATION_PHASE)
        {
            GoToBloomPhase();
        }
    }

    // State transitions
    private void GoToNotTriggeredPhase()
    {
        currentState = States.NOT_TRIGGERED;
        foreach (Node2D el in templeGems)
        {
            el.QueueFree();
        }
        templeGems.Clear();
        gemsAngularVelocity = 0;
        numActiveGems = 0;
        bloomSpriteScale = 1.0f;
        BloomSpriteNode.Visible = false;
    }

    private void GoToWalkPhase()
    {
        currentState = States.WALK_PHASE;
        Global.Instance().Player.velocity = new Vector2(0, Global.Instance().Player.velocity.Y);
        Event.Instance().EmitGemTempleTriggered();
        Global.Instance().Cutscene.ShowSomeNode(Global.Instance().Player, 10.0f, 3.2f);
    }

    private void GoToRotationPhase()
    {
        currentState = States.ROTATION_PHASE;
        RotateGemsTween();
        StartRotationTimer();
        Event.Instance().EmitGemEngineStarted();
    }

    private void GoToCollectPhase()
    {
        currentState = States.COLLECT_PHASE;
        Global.Instance().Player.velocity = new Vector2(0, Global.Instance().Player.velocity.Y);
        if (!CreateTempleGems())
        {
            OnGemCollected(null);
        }
    }

    private void GoToBloomPhase()
    {
        currentState = States.BLOOM_PHASE;
        BloomSpriteNode.Visible = true;
        BloomSpriteResizeTween();
    }

    private void GoToEndPhase()
    {
        Event.Instance().EmitLevelCleared();
    }

    // Tween animations
    private void RotateGemsTween()
    {
        if (tweener != null)
        {
            tweener.Kill();
        }
        tweener = GetTree().CreateTween();
        tweener.TweenProperty(this, "gemsAngularVelocity", MAX_GEM_TEMPLE_ANGULAR_VELOCITY, DURATION_TO_FULL_GEM_ROTATION_SPEED)
            .From(gemsAngularVelocity)
            .SetTrans(Tween.TransitionType.Linear)
            .SetEase(Tween.EaseType.Out);
    }

    private void BloomSpriteResizeTween()
    {
        if (tweener != null)
        {
            tweener.Kill();
        }
        tweener = GetTree().CreateTween();
        tweener.TweenProperty(this, "bloomSpriteScale", BLOOM_SPRITE_MAX_SCALE, BLOOM_SPRITE_SCALE_DURATION)
            .From(bloomSpriteScale)
            .SetTrans(Tween.TransitionType.Linear)
            .SetEase(Tween.EaseType.Out);
        tweener.Connect("finished", new Callable(this, nameof(OnBloomSpriteResizeEnd)), (uint)ConnectFlags.OneShot);
    }

    private void OnBloomSpriteResizeEnd()
    {
        GoToEndPhase();
    }
}
