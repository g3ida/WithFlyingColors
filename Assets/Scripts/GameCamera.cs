using Godot;
using System;
using System.Collections.Generic;

public class GameCamera : Camera2D
{
    public const float CAMERA_DRAG_JUMP = 0.45f;

    [Export] public NodePath follow_path { get; set; }

    public Node2D follow;
    public float targetZoom = 1.0f;
    private SceneTreeTween zoomTweener;

    private Godot.Collections.Dictionary<string, object> defaultSaveData;

    private Godot.Collections.Dictionary<string, object> save_data;
    
    // Used for tuning camera
    private float cachedDragMarginTop;
    private float cachedDragMarginBottom;
    private float cachedDragMarginLeft;
    private float cachedDragMarginRight;

    public override void _Ready()
    {
        defaultSaveData = new Godot.Collections.Dictionary<string, object>()
        {
            {"zoom_factor", 1.0f},
            {"bottom_limit", 10000},
            {"top_limit", 0},
            {"left_limit", 0},
            {"right_limit", 10000},
            {"drag_margin_bottom", Constants.DEFAULT_DRAG_MARGIN_TB},
            {"drag_margin_left", Constants.DEFAULT_DRAG_MARGIN_LR},
            {"drag_margin_right", Constants.DEFAULT_DRAG_MARGIN_LR},
            {"drag_margin_top", Constants.DEFAULT_DRAG_MARGIN_TB},
            {"follow_path", follow_path}
        };

        follow = GetNode<Node2D>(follow_path);
        save_data = new Godot.Collections.Dictionary<string, object>(defaultSaveData);
        if (Current)
        {
            Global.Instance().Camera = this;
        }
        CacheDragMargins();
    }

    public override void _Process(float delta)
    {
        if (Current)
        {
            Global.Instance().Camera = this;
        }
    }

    public override void _PhysicsProcess(float delta)
    {
        if (follow != null)
        {
            GlobalPosition = follow.GlobalPosition;
        }
    }

    private void _OnCheckpointHit(object checkpoint)
    {
        // FIXME: better save data as json after migration
        save_data = new Godot.Collections.Dictionary<string, object>();
        save_data["zoom_factor"] = targetZoom;
        save_data["bottom_limit"] = LimitBottom;
        save_data["top_limit"] = LimitTop;
        save_data["left_limit"] = LimitLeft;
        save_data["right_limit"] = LimitRight;
        save_data["drag_margin_bottom"] = cachedDragMarginBottom;
        save_data["drag_margin_left"] = cachedDragMarginLeft;
        save_data["drag_margin_right"] = cachedDragMarginRight;
        save_data["drag_margin_top"] = cachedDragMarginTop;
        save_data["follow_path"] = follow.GetPath();
    }

    public void reset()
    {
        if (save_data != null)
        {
            GD.Print(save_data);
            zoom_by((float)save_data["zoom_factor"]);

            LimitBottom = Helpers.ParseSaveDataInt(save_data, "bottom_limit");
            LimitTop = Helpers.ParseSaveDataInt(save_data, "top_limit");
            LimitLeft = Helpers.ParseSaveDataInt(save_data, "left_limit");
            LimitRight = Helpers.ParseSaveDataInt(save_data, "right_limit");


            DragMarginBottom = (float)save_data["drag_margin_bottom"];
            DragMarginLeft = (float)save_data["drag_margin_left"];
            DragMarginRight = (float)save_data["drag_margin_right"];
            DragMarginTop = (float)save_data["drag_margin_top"];
            follow_path = Helpers.ParseSaveDataNodePath(save_data, "follow_path");
            follow = GetNode<Node2D>(follow_path);
        }
    }

    public Godot.Collections.Dictionary<string, object> save()
    {
        return save_data != null ? save_data : new Godot.Collections.Dictionary<string, object>(defaultSaveData);
    }

    private void _OnPlayerJump()
    {
        CacheDragMargins();
        if (DragMarginBottom < CAMERA_DRAG_JUMP)
        {
            DragMarginBottom = CAMERA_DRAG_JUMP;
        }
        if (DragMarginTop < CAMERA_DRAG_JUMP)
        {
            DragMarginTop = CAMERA_DRAG_JUMP;
        }
    }

    private void _OnPlayerLand()
    {
        RestoreDragMargins();
    }

    private void _OnPlayerDying(object area, Vector2 position, string entityType)
    {
        RestoreDragMargins();
    }

    private void CacheDragMargins()
    {
        cachedDragMarginBottom = DragMarginBottom;
        cachedDragMarginTop = DragMarginTop;
        cachedDragMarginLeft = DragMarginLeft;
        cachedDragMarginRight = DragMarginRight;
    }

    private void RestoreDragMargins()
    {
        DragMarginBottom = cachedDragMarginBottom;
        DragMarginTop = cachedDragMarginTop;
        DragMarginLeft = cachedDragMarginLeft;
        DragMarginRight = cachedDragMarginRight;
    }

    private void zoom_by(float factor)
    {
        targetZoom = factor;
        if (zoomTweener != null)
        {
            zoomTweener.Kill();
        }
        zoomTweener = CreateTween();
        zoomTweener.TweenProperty(this, "zoom", new Vector2(factor, factor), 1.0f);
    }

    private void ConnectSignals()
    {
        Event.GdInstance().Connect("checkpoint_reached", this, nameof(_OnCheckpointHit));
        Event.GdInstance().Connect("checkpoint_loaded", this, nameof(reset));
        Event.GdInstance().Connect("player_jumped", this, nameof(_OnPlayerJump));
        Event.GdInstance().Connect("player_land", this, nameof(_OnPlayerLand));
        Event.GdInstance().Connect("player_diying", this, nameof(_OnPlayerDying));
    }

    private void DisconnectSignals()
    {
        Event.GdInstance().Disconnect("checkpoint_reached", this, nameof(_OnCheckpointHit));
        Event.GdInstance().Disconnect("checkpoint_loaded", this, nameof(reset));
        Event.GdInstance().Disconnect("player_jumped", this, nameof(_OnPlayerJump));
        Event.GdInstance().Disconnect("player_land", this, nameof(_OnPlayerLand));
        Event.GdInstance().Disconnect("player_diying", this, nameof(_OnPlayerDying));
    }

    public override void _EnterTree()
    {
        ConnectSignals();
    }

    public override void _ExitTree()
    {
        DisconnectSignals();
    }

    public async void update_position(Vector2 pos)
    {
        SmoothingEnabled = false;
        GlobalPosition = pos;
        await ToSignal(GetTree(), "idle_frame");
        SetDeferred("smoothing_enabled", true);
    }

    public void set_follow_node(Node2D followNode)
    {
        follow = followNode;
        follow_path = followNode.GetPath();
    }

    public void set_drag_margin_top(float value)
    {
        DragMarginTop = value;
        cachedDragMarginTop = value;
    }

    public void set_drag_margin_bottom(float value)
    {
        DragMarginBottom = value;
        cachedDragMarginBottom = value;
    }

    public void set_drag_margin_left(float value)
    {
        DragMarginLeft = value;
        cachedDragMarginLeft = value;
    }

    public void set_drag_margin_right(float value)
    {
        DragMarginRight = value;
        cachedDragMarginRight = value;
    }
}
