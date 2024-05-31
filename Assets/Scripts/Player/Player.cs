using Godot;
using System;
using System.Collections.Generic;

public class Player : KinematicBody2D
{
    public const float SQUEEZE_ANIM_DURATION = 0.17f;
    public const float SCALE_ANIM_DURATION = 0.17f;

    public const float SPEED = 3.5f * Constants.WORLD_TO_SCREEN;
    public const float SPEED_UNIT = 0.7f * Constants.WORLD_TO_SCREEN;
    public float speed_limit = SPEED;
    public float speed_unit = SPEED_UNIT;

    public Vector2 velocity = Vector2.Zero;
    public PlayerRotationAction playerRotationAction;

    public TransformAnimation scale_animation;
    private TransformAnimation idle_animation;
    public TransformAnimation current_animation;

    private int sprite_size;
    private bool was_on_floor = true;

    public PlayerStatesStore states_store;
    public PlayerBaseState player_state;
    public PlayerBaseState player_rotation_state;

    public bool can_dash = true;
    public bool handle_input_is_disabled = false;

    // Touch input
    public InputTouchMove touch_move_input = null;
    public InputTouchJump touch_jump_input = null;
    public InputTouchDash touch_dash_input = null;
    public InputTouchRotate touch_rotation_input = null;


    public CPUParticles2D jumpParticlesNode;
    public Timer fallTimerNode;

    private BoxCorner faceSparatorBR_node;
    private BoxCorner faceSparatorBL_node;
    private BoxCorner faceSparatorTL_node;
    private BoxCorner faceSparatorTR_node;

    private BoxFace bottomFaceNode;
    private BoxFace topFaceNode;
    private BoxFace leftFaceNode;
    private BoxFace rightFaceNode;

    private CollisionShape2D FaceCollisionShapeL_node;
    private CollisionShape2D FaceCollisionShapeR_node;
    private CollisionShape2D FaceCollisionShapeT_node;
    private CollisionShape2D FaceCollisionShapeB_node;

    public CollisionShape2D FaceCollisionShapeTL_node;
    public CollisionShape2D FaceCollisionShapeTR_node;
    public CollisionShape2D FaceCollisionShapeBL_node;
    public CollisionShape2D FaceCollisionShapeBR_node;

    private CollisionShape2D collisionShapeNode;
    public AnimatedSprite animatedSpriteNode;
    public Timer dashGhostTimerNode;

    private List<BoxCorner> faceSeparatorNodes;
    private List<BoxFace> faceNodes;
    private List<CollisionShape2D> faceCollisionNodes;
    private List<CollisionShape2D> faceCornerCollisionNodes;


    private Dictionary<string, object> save_data;

    // Used to backup collision layer and collision mask of the player areas
    private List<Dictionary<string, int>> _face_separators_mask_backup = new List<Dictionary<string, int>>();
    private List<Dictionary<string, int>> _face_nodes_mask_backup = new List<Dictionary<string, int>>();

    public float CurrentDefaultCornerScaleFactor = 1.0f;
    private float current_scale_factor = 1.0f; // Do not edit by yourself this is used by scale_corners_by

    public LightOccluder2D lightOccluder;
    

private void PrepareChildrenNodes()
    {
        lightOccluder = GetNode<LightOccluder2D>("AnimatedSprite/LightOccluder2D");
        jumpParticlesNode = GetNode<CPUParticles2D>("JumpParticles");
        fallTimerNode = GetNode<Timer>("FallTimer");

        faceSparatorBR_node = GetNode<BoxCorner>("FaceSeparatorBR");
        faceSparatorBL_node = GetNode<BoxCorner>("FaceSeparatorBL");
        faceSparatorTL_node = GetNode<BoxCorner>("FaceSeparatorTL");
        faceSparatorTR_node = GetNode<BoxCorner>("FaceSeparatorTR");

        bottomFaceNode = GetNode<BoxFace>("BottomFace");
        topFaceNode = GetNode<BoxFace>("TopFace");
        leftFaceNode = GetNode<BoxFace>("LeftFace");
        rightFaceNode = GetNode<BoxFace>("RightFace");

        FaceCollisionShapeL_node = GetNode<CollisionShape2D>("FaceCollisionShapeL");
        FaceCollisionShapeR_node = GetNode<CollisionShape2D>("FaceCollisionShapeR");
        FaceCollisionShapeT_node = GetNode<CollisionShape2D>("FaceCollisionShapeT");
        FaceCollisionShapeB_node = GetNode<CollisionShape2D>("FaceCollisionShapeB");

        FaceCollisionShapeTL_node = GetNode<CollisionShape2D>("FaceCollisionShapeTL");
        FaceCollisionShapeTR_node = GetNode<CollisionShape2D>("FaceCollisionShapeTR");
        FaceCollisionShapeBL_node = GetNode<CollisionShape2D>("FaceCollisionShapeBL");
        FaceCollisionShapeBR_node = GetNode<CollisionShape2D>("FaceCollisionShapeBR");

        collisionShapeNode = GetNode<CollisionShape2D>("CollisionShape2D");
        animatedSpriteNode = GetNode<AnimatedSprite>("AnimatedSprite");
        dashGhostTimerNode = GetNode<Timer>("DashGhostTimer");

        faceSeparatorNodes = new List<BoxCorner>
        {
            faceSparatorBR_node,
            faceSparatorBL_node,
            faceSparatorTL_node,
            faceSparatorTR_node
        };

        faceNodes = new List<BoxFace>
        {
            bottomFaceNode,
            topFaceNode,
            leftFaceNode,
            rightFaceNode
        };

        faceCollisionNodes = new List<CollisionShape2D>
        {
            FaceCollisionShapeB_node,
            FaceCollisionShapeT_node,
            FaceCollisionShapeL_node,
            FaceCollisionShapeR_node
        };

        faceCornerCollisionNodes = new List<CollisionShape2D>
        {
            FaceCollisionShapeBR_node,
            FaceCollisionShapeBL_node,
            FaceCollisionShapeTL_node,
            FaceCollisionShapeTR_node
        };
    }


    public override void _Ready()
    {
        PrepareChildrenNodes();
        playerRotationAction = new PlayerRotationAction();
        playerRotationAction.Set(this);
        animatedSpriteNode.Frames.SetFrame("idle", 0, Global.Instance().GetPlayerSprite());
        sprite_size = animatedSpriteNode.Frames.GetFrame("idle", 0).GetWidth();
        InitSpriteAnimation();
        was_on_floor = IsOnFloor();
        InitFacesAreas();
        InitState();

        save_data = new Dictionary<string, object>
        {
            { "position_x", GlobalPosition.x },
            { "position_y", GlobalPosition.y },
            { "angle", 0.0f },
            { "default_corner_scale_factor", 1.0f }
        };
    }

    private void InitSpriteAnimation()
    {
        idle_animation = new TransformAnimation(0, new ElasticOut(1, 1, 1, 0.1f), 0);
        scale_animation = new TransformAnimation(SCALE_ANIM_DURATION, new ElasticOut(1, 1, 1, 0.1f), sprite_size * 0.5f);
        current_animation = idle_animation;
    }

    private void InitState()
    {
        states_store = new PlayerStatesStore(this);
        player_state = states_store.fallingState;
        player_state.Enter(this);
        player_rotation_state = states_store.idleState;
        player_rotation_state.Enter(this);
    }

    private void InitFacesAreas()
    {
        for (int i = 0; i < faceCollisionNodes.Count; i++)
        {
            foreach (string grp in faceNodes[i].GetGroups())
            {
                faceCollisionNodes[i].AddToGroup(grp);
            }
        }

        for (int i = 0; i < faceCornerCollisionNodes.Count; i++)
        {
            foreach (string grp in faceSeparatorNodes[i].GetGroups())
            {
                faceCornerCollisionNodes[i].AddToGroup(grp);
            }
        }

        FillFaceNodesBackup();
        FillFaceSeparatorsBackup();
    }

    public override void _Input(InputEvent @event)
    {
        player_state._Input(@event);
        player_rotation_state._Input(@event);
    }

    public override void _PhysicsProcess(float delta)
    {
        var next_state = (PlayerBaseState)player_rotation_state.PhysicsUpdate(this, delta);
        SwitchRotationState(next_state);

        var next_player_state = (PlayerBaseState)player_state.PhysicsUpdate(this, delta);
        SwitchState(next_player_state);

        if (IsJustHitTheFloor())
        {
            OnLand();
        }

        was_on_floor = IsOnFloor();
    }

    public Dictionary<string, object> save()
    {
        return save_data;
    }

    public void reset()
    {
        animatedSpriteNode.Play("idle");
        animatedSpriteNode.Playing = false;
        GlobalPosition = new Vector2((float)save_data["position_x"], (float)save_data["position_y"]);
        velocity = Vector2.Zero;
        var angle_rot = (float)save_data["angle"];
        Rotate(angle_rot - Rotation);
        CurrentDefaultCornerScaleFactor = (float)save_data["default_corner_scale_factor"];
        ShowColorAreas();
        SwitchRotationState((PlayerBaseState)states_store.GetState(PlayerStatesEnum.IDLE));
        SwitchState((PlayerBaseState)states_store.GetState(PlayerStatesEnum.FALLING));
        handle_input_is_disabled = false;
    }

    private void OnCheckpointHit(Node2D checkpoint_object)
    {
        if (checkpoint_object.GetGroups().Contains(bottomFaceNode.GetGroups()[0]))
        {
            save_data["angle"] = 0f;
        }
        else if (checkpoint_object.GetGroups().Contains(leftFaceNode.GetGroups()[0]))
        {
            save_data["angle"] = -Mathf.Pi / 2f;
        }
        else if (checkpoint_object.GetGroups().Contains(rightFaceNode.GetGroups()[0]))
        {
            save_data["angle"] = Mathf.Pi / 2f;
        }
        else if (checkpoint_object.GetGroups().Contains(topFaceNode.GetGroups()[0]))
        {
            save_data["angle"] = Mathf.Pi;
        }

        if (checkpoint_object.IsInsideTree())
        {
            save_data["position_x"] = checkpoint_object.GlobalPosition.x;
            save_data["position_y"] = checkpoint_object.GlobalPosition.y;
        }

        save_data["default_corner_scale_factor"] = CurrentDefaultCornerScaleFactor;
    }

    private void ConnectSignals()
    {
        Event.GdInstance().Connect("player_diying", this, nameof(OnPlayerDiying));
        Event.GdInstance().Connect("checkpoint_reached", this, nameof(OnCheckpointHit));
        Event.GdInstance().Connect("checkpoint_loaded", this, nameof(reset));
    }

    private void DisconnectSignals()
    {
        Event.GdInstance().Disconnect("player_diying", this, nameof(OnPlayerDiying));
        Event.GdInstance().Disconnect("checkpoint_reached", this, nameof(OnCheckpointHit));
        Event.GdInstance().Disconnect("checkpoint_loaded", this, nameof(reset));
    }

    public override void _EnterTree()
    {
        Global.Instance().Player = this;
        ConnectSignals();
    }

    public override void _ExitTree()
    {
        DisconnectSignals();
    }

    private void OnPlayerDiying(Node area, Vector2 position, Constants.EntityType entity_type)
    {
        var next_state = (player_state).OnPlayerDying(this, area, position, entity_type);
        SwitchState(next_state);
    }

    private bool IsJustHitTheFloor()
    {
        return !was_on_floor && IsOnFloor();
    }

    private void OnLand()
    {
        var next_player_state = player_state.OnLand(this);
        SwitchState(next_player_state);
    }

    private void SwitchState(PlayerBaseState new_state)
    {
        if (new_state != null)
        {
            player_state.Exit(this);
            player_state = new_state;
            player_state.Enter(this);
        }
    }

    private void SwitchRotationState(PlayerBaseState new_state)
    {
        if (new_state != null)
        {
            player_rotation_state.Exit(this);
            player_rotation_state = new_state;
            player_rotation_state.Enter(this);
        }
    }

    private void ScaleFaceSeparatorsBy(float factor)
    {
        foreach (var face_sep in faceSeparatorNodes)
        {
            face_sep.ScaleBy(factor);
        }
    }

    private void ScaleFacesBy(float factor)
    {
        foreach (var face_sep in faceNodes)
        {
            face_sep.ScaleBy(factor);
        }
    }

    public void ScaleCornersBy(float factor)
    {
        if (current_scale_factor == factor) return;
        current_scale_factor = factor;
        var edge = faceSeparatorNodes[0].edgeLength;
        var face = faceNodes[0].edgeLength;
        var total_length = 2 * edge + face;
        var reverse_factor = (total_length - 2f * edge * factor) / face;
        ScaleFaceSeparatorsBy(factor);
        ScaleFacesBy(reverse_factor);
    }

    public Vector2 GetCollisionShapeSize()
    {
        var extra_w = (FaceCollisionShapeL_node.Shape as RectangleShape2D).Extents.x;
        return ((collisionShapeNode.Shape as RectangleShape2D).Extents + 2.0f * new Vector2(extra_w, extra_w)) * 2.0f;
    }

    public bool ContainsNode(Node node)
    {
        return GetChildren().Contains(node);
    }

    // This function is a hack for bullets and fast moving objects because of this Godot issue:
    // https://github.com/godotengine/godot/issues/43743
    public void OnFastAreaCollidingWithPlayerShape(uint body_shape_index, Area2D color_area, Constants.EntityType entity_type)
    {
        var collision_shape = (CollisionShape2D)ShapeOwnerGetOwner(body_shape_index);
        var shape_groups = collision_shape.GetGroups();
        var group_found = false;
        foreach (string group in shape_groups)
        {
            if (color_area.IsInGroup(group))
            {
                group_found = true;
                break;
            }
        }
        if (!group_found)
        {
            Event.Instance().EmitPlayerDiying(color_area, GlobalPosition, entity_type);
        }
    }

    // Face areas backup
    private void FillFaceNodesBackup()
    {
        _face_nodes_mask_backup.Clear();
        foreach (var face in faceNodes)
        {
            _face_nodes_mask_backup.Add(new Dictionary<string, int>
            {
                { "layer", (int)face.CollisionLayer },
                { "mask", (int)face.CollisionMask }
            });
        }
    }

    private void FillFaceSeparatorsBackup()
    {
        _face_separators_mask_backup.Clear();
        foreach (var face in faceSeparatorNodes)
        {
            _face_separators_mask_backup.Add(new Dictionary<string, int>
            {
                { "layer", (int)face.CollisionLayer },
                { "mask", (int)face.CollisionMask }
            });
        }
    }

    public void HideColorAreas()
    {
        FillFaceSeparatorsBackup();
        foreach (var face in faceSeparatorNodes)
        {
            face.CollisionLayer = 0;
            face.CollisionMask = 0;
        }
        FillFaceNodesBackup();
        foreach (var face in faceNodes)
        {
            face.CollisionLayer = 0;
            face.CollisionMask = 0;
        }
    }

    public void SetCollisionShapesDisabledFlagDeferred(bool disable)
    {
        CallDeferred(nameof(SetCollisionShapesDisabledFlag), disable);
    }

    private void SetCollisionShapesDisabledFlag(bool disable)
    {
        foreach (var face in faceCollisionNodes)
        {
            face.Disabled = disable;
        }
        foreach (var face in faceCornerCollisionNodes)
        {
            face.Disabled = disable;
        }
        collisionShapeNode.Disabled = disable;
    }

    public void ShowColorAreas()
    {
        for (int i = 0; i < faceSeparatorNodes.Count; i++)
        {
            faceSeparatorNodes[i].CollisionLayer = (uint)_face_separators_mask_backup[i]["layer"];
            faceSeparatorNodes[i].CollisionMask = (uint)_face_separators_mask_backup[i]["mask"];
        }
        for (int i = 0; i < faceNodes.Count; i++)
        {
            faceNodes[i].CollisionLayer = (uint)_face_nodes_mask_backup[i]["layer"];
            faceNodes[i].CollisionMask = (uint)_face_nodes_mask_backup[i]["mask"];
        }
    }

    // Methods added to convert PianoNote to C#
    public bool IsJumpingState()
    {
        return player_state.baseState == PlayerStatesEnum.JUMPING;
    }

    public bool IsFalling()
    {
        return velocity.y >= -Constants.EPSILON;
    }

    public bool IsRotationIdle() {
        return player_rotation_state.baseState == PlayerStatesEnum.IDLE;
    }


    public bool IsStanding() {
        return player_state.baseState == PlayerStatesEnum.STANDING;
    }

    public bool IsDying() {
        return player_state.baseState == PlayerStatesEnum.DYING;
    }

    public void SetMaxSpeed() {
        velocity.x = SPEED;
    }
}
