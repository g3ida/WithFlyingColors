using Godot;
using System;
using System.Collections.Generic;

public class PianoNote : KinematicBody2D
{
    private enum NoteStates
    {
        RELEASED,
        PRESSING,
        PRESSED,
        RELEASING
    }

    [Signal]
    public delegate void on_note_pressed(PianoNote note);
    [Signal]
    public delegate void on_note_released(PianoNote note);

    private static readonly Texture PairTexture = GD.Load<Texture>("res://Assets/Sprites/Piano/note_1.png");
    private static readonly Texture OddTexture = GD.Load<Texture>("res://Assets/Sprites/Piano/note_2.png");

    private static readonly Texture[] NoteEdgeTextures = {
        GD.Load<Texture>("res://Assets/Sprites/Piano/note_edge.png"),
        GD.Load<Texture>("res://Assets/Sprites/Piano/note_edge2.png"),
        GD.Load<Texture>("res://Assets/Sprites/Piano/note_edge3.png"),
    };

    private static readonly Vector2 PRESS_OFFSET = new Vector2(0, 25);
    private const float PRESS_SPEED = 2.5f * Constants.WORLD_TO_SCREEN;
    private const float RAYCAST_Y_OFFSET = 3.0f;
    private const float RAYCAST_LENGTH = 20.0f;
    private const float RESPONSIVENESS = 0.06f;

    [Export]
    public int index = 0;

    private string color_group;
    [Export]
    public string ColorGroup
    {
        get { return color_group; }
        set { SetColorGroup(value); }
    }

    [Export]
    public int note_edge_index = 0;

    private NoteStates current_state = NoteStates.RELEASED;

    private Sprite SpriteNode;
    private CollisionShape2D AreaCollisionShapeNode;
    private CollisionShape2D CollisionShapeNode;
    private Timer ResponsivenessTimerNode;

    private Vector2 released_position;
    private Vector2 calculated_position;

    private SceneTreeTween tweener;

    public override void _Ready()
    {
        SpriteNode = GetNode<Sprite>("NoteSpr");
        AreaCollisionShapeNode = GetNode<CollisionShape2D>("Area2D/CollisionShape2D");
        CollisionShapeNode = GetNode<CollisionShape2D>("CollisionShape2D");
        ResponsivenessTimerNode = GetNode<Timer>("ResponsivenessTimer");

        released_position = Position;
        calculated_position = Position;

        SetupResponsivenessTimer();
        SetTexture();
    }

    public override void _PhysicsProcess(float delta)
    {
        Position = calculated_position;
        StartReleasingNoteTimerIfRelevant();
    }

    private void SetTexture()
    {
        SpriteNode.Texture = index % 2 == 0 ? PairTexture : OddTexture;
    }

    private void SetupResponsivenessTimer()
    {
        ResponsivenessTimerNode.Autostart = false;
        ResponsivenessTimerNode.WaitTime = RESPONSIVENESS;
    }

    private void MoveToPosition(Vector2 dest_position)
    {
        float duration = Math.Abs(calculated_position.y - dest_position.y) / PRESS_SPEED;
        tweener?.Kill();
        tweener = CreateTween();
        tweener.Connect("finished", this, nameof(OnTweenCompleted), flags: (uint)ConnectFlags.Oneshot);
        tweener.TweenProperty(this, "calculated_position", dest_position, duration)
            .From(calculated_position)
            .SetTrans(Tween.TransitionType.Linear)
            .SetEase(Tween.EaseType.InOut);
    }

    private bool IsReleasingOrReleasedState()
    {
        return current_state == NoteStates.RELEASED || current_state == NoteStates.RELEASING;
    }

    private bool IsPressingOrPressedState()
    {
        return current_state == NoteStates.PRESSED || current_state == NoteStates.PRESSING;
    }

    public void _on_Area2D_body_entered(Node body)
    {
        if (body == Global.Instance().Player)
        {
            PressNoteIfRelevant();
        }
    }

    private void PressNoteIfRelevant()
    {
        if (IsReleasingOrReleasedState())
        {
            StopTimerIfRelevant();
            PressNote();
        }
    }

    private void PressNote()
    {
        current_state = NoteStates.PRESSING;
        MoveToPosition(released_position + PRESS_OFFSET);
    }

    public void _on_Area2D_body_exited(Node body)
    {
        if (body == Global.Instance().Player)
        {
            StartReleasingNoteTimerIfRelevant();
        }
    }

    private void StartReleasingNoteTimerIfRelevant()
    {
        if (IsPressingOrPressedState() && !CheckIfPlayerIsAboveTheNote())
        {
            StartTimerIfStopped();
        }
    }

    private void ReleaseNoteIfRelevant()
    {
        if (IsPressingOrPressedState() && !CheckIfPlayerIsAboveTheNote())
        {
            ReleaseNote();
        }
    }

    private void ReleaseNote()
    {
        current_state = NoteStates.RELEASING;
        MoveToPosition(released_position);
    }

    private void OnTweenCompleted()
    {
        if (current_state == NoteStates.PRESSING)
        {
            current_state = NoteStates.PRESSED;
            EmitSignal(nameof(on_note_pressed), this);
        }
        else if (current_state == NoteStates.RELEASING)
        {
            current_state = NoteStates.RELEASED;
            EmitSignal(nameof(on_note_released), this);
        }
    }

    private Vector2 GetDetectionAreaShapeSize()
    {
        return (AreaCollisionShapeNode.Shape as RectangleShape2D).Extents * 2.0f;
    }

    private Vector2 GetCollisionShapeSize()
    {
        return (CollisionShapeNode.Shape as RectangleShape2D).Extents * 2.0f;
    }

    private List<Dictionary<string, Vector2>> GetRayLinesInGlobalPosition()
    {
        var rays = new List<Dictionary<string, Vector2>>();
        Vector2 note_half_size = GetDetectionAreaShapeSize() * 0.5f * Scale;
        var from_offset_x = new float[]
        {
            -note_half_size.x,
            -note_half_size.x * 0.5f,
            0.0f,
            note_half_size.x * 0.5f,
            note_half_size.x
        };
        foreach (float offset in from_offset_x)
        {
            var from = GlobalPosition + new Vector2(offset, -GetCollisionShapeSize().y * 0.5f + RAYCAST_Y_OFFSET);
            var to = from + new Vector2(0.0f, -RAYCAST_LENGTH);
            rays.Add(new Dictionary<string, Vector2> { { "from", from }, { "to", to } });
        }
        return rays;
    }

    private bool RaycastPlayer()
    {
        var spaceState = GetWorld2d().DirectSpaceState;
        var rays = GetRayLinesInGlobalPosition();
        foreach (var ray in rays)
        {
            var from = ray["from"];
            var to = ray["to"];
            var result = spaceState.IntersectRay(from, to, new Godot.Collections.Array { this });
            if (result.Count > 0 && result["collider"] == Global.Instance().Player)
            {
                return true;
            }
        }
        return false;
    }

    private bool IsPlayerStandingOrFalling()
    {
        var player = Global.Instance().Player;
        bool isJumping  = Convert.ToBoolean(player.IsJumpingState());
        bool isFalling  = Convert.ToBoolean(player.IsFalling());
        return !isJumping && isFalling;
    }

    private bool CheckIfPlayerIsAboveTheNote()
    {
        return RaycastPlayer() && IsPlayerStandingOrFalling();
    }

    // Uncomment this code to debug draw raycast rays
    // public override void _Draw()
    // {
    //     var rays = GetRayLinesInGlobalPosition();
    //     foreach (var ray in rays)
    //     {
    //         var from = ray["from"] - GlobalPosition;
    //         var to = ray["to"] - GlobalPosition;
    //         DrawLine(from, to, Colors.Red, 2.0f);
    //     }
    // }

    private void StartTimerIfStopped()
    {
        if (ResponsivenessTimerNode.IsStopped())
        {
            ResponsivenessTimerNode.Autostart = true;
            ResponsivenessTimerNode.Start();
        }
    }

    private void StopTimerIfRelevant()
    {
        if (!ResponsivenessTimerNode.IsStopped())
        {
            ResponsivenessTimerNode.Autostart = false;
            ResponsivenessTimerNode.Stop();
        }
    }

    public void _on_ResponsivenessTimer_timeout()
    {
        ReleaseNoteIfRelevant();
        StopTimerIfRelevant();
    }

    private void SetColorGroup(string _color_group)
    {
        color_group = _color_group;
        int color_index = ColorUtils.GetGroupColorIndex(color_group);
        Color color = ColorUtils.GetBasicColor(color_index);
        GetNode<Sprite>("NoteEdge").Modulate = color;
        var area = GetNode<Area2D>("ColorArea");
        foreach (string grp in area.GetGroups())
        {
            area.RemoveFromGroup(grp);
        }
        area.AddToGroup(_color_group);
    }

    public void SetNoteEdgeIndex(int note_index)
    {
        int scale = (note_index / (NoteEdgeTextures.Length + 1)) % 2 == 0 ? -1 : 1;
        note_edge_index = note_index % NoteEdgeTextures.Length;
        GetNode<Sprite>("NoteEdge").Texture = NoteEdgeTextures[note_edge_index];
        GetNode<Sprite>("NoteEdge").Scale = new Vector2(scale, 1);
    }

    public int GetNoteEdgeIndex()
    {
        return note_edge_index;
    }
}
