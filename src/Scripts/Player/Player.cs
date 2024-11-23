using Godot;
using System;
using System.Collections.Generic;
using Wfc.Core.Event;
using EventHandler = Wfc.Core.Event.EventHandler;

public partial class Player : CharacterBody2D, IPersistent {
  public const float SQUEEZE_ANIM_DURATION = 0.17f;
  public const float SCALE_ANIM_DURATION = 0.17f;

  public const float SPEED = 3.5f * Constants.WORLD_TO_SCREEN;
  public const float SPEED_UNIT = 0.7f * Constants.WORLD_TO_SCREEN;
  public float speed_limit = SPEED;
  public float speed_unit = SPEED_UNIT;

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


  public CpuParticles2D jumpParticlesNode;
  public Timer fallTimerNode;

  private BoxCorner faceSeparatorBR_node;
  private BoxCorner faceSeparatorBL_node;
  private BoxCorner faceSeparatorTL_node;
  private BoxCorner faceSeparatorTR_node;

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
  public AnimatedSprite2D animatedSpriteNode;
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


  private void PrepareChildrenNodes() {
    lightOccluder = GetNode<LightOccluder2D>("AnimatedSprite2D/LightOccluder2D");
    jumpParticlesNode = GetNode<CpuParticles2D>("JumpParticles");
    fallTimerNode = GetNode<Timer>("FallTimer");

    faceSeparatorBR_node = GetNode<BoxCorner>("FaceSeparatorBR");
    faceSeparatorBL_node = GetNode<BoxCorner>("FaceSeparatorBL");
    faceSeparatorTL_node = GetNode<BoxCorner>("FaceSeparatorTL");
    faceSeparatorTR_node = GetNode<BoxCorner>("FaceSeparatorTR");

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
    animatedSpriteNode = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
    dashGhostTimerNode = GetNode<Timer>("DashGhostTimer");

    faceSeparatorNodes = new List<BoxCorner>
        {
            faceSeparatorBR_node,
            faceSeparatorBL_node,
            faceSeparatorTL_node,
            faceSeparatorTR_node
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


  public override void _Ready() {
    base._Ready();
    PrepareChildrenNodes();
    playerRotationAction = new PlayerRotationAction();
    playerRotationAction.Set(this);
    animatedSpriteNode.SpriteFrames.SetFrame("idle", 0, Global.Instance().GetPlayerSprite());
    sprite_size = animatedSpriteNode.SpriteFrames.GetFrameTexture("idle", 0).GetWidth();
    InitSpriteAnimation();
    was_on_floor = IsOnFloor();
    UpDirection = Vector2.Up;
    InitFacesAreas();
    InitState();

    save_data = new Dictionary<string, object>
        {
            { "position_x", GlobalPosition.X },
            { "position_y", GlobalPosition.Y },
            { "angle", 0.0f },
            { "default_corner_scale_factor", 1.0f }
        };
  }

  private void InitSpriteAnimation() {
    idle_animation = new TransformAnimation(0, new ElasticOut(1, 1, 1, 0.1f), 0);
    scale_animation = new TransformAnimation(SCALE_ANIM_DURATION, new ElasticOut(1, 1, 1, 0.1f), sprite_size * 0.5f);
    current_animation = idle_animation;
  }

  private void InitState() {
    states_store = new PlayerStatesStore(this);
    player_state = states_store.fallingState;
    player_state.Enter(this);
    player_rotation_state = states_store.idleState;
    player_rotation_state.Enter(this);
  }

  private void InitFacesAreas() {
    for (int i = 0; i < faceCollisionNodes.Count; i++) {
      foreach (string grp in faceNodes[i].GetGroups()) {
        faceCollisionNodes[i].AddToGroup(grp);
      }
    }

    for (int i = 0; i < faceCornerCollisionNodes.Count; i++) {
      foreach (string grp in faceSeparatorNodes[i].GetGroups()) {
        faceCornerCollisionNodes[i].AddToGroup(grp);
      }
    }

    FillFaceNodesBackup();
    FillFaceSeparatorsBackup();
  }

  public override void _Input(InputEvent ev) {
    player_state._Input(ev);
    player_rotation_state._Input(ev);
  }

  public override void _PhysicsProcess(double delta) {
    var next_state = (PlayerBaseState)player_rotation_state.PhysicsUpdate(this, (float)delta);
    SwitchRotationState(next_state);

    var next_player_state = (PlayerBaseState)player_state.PhysicsUpdate(this, (float)delta);
    SwitchState(next_player_state);

    if (IsJustHitTheFloor()) {
      OnLand();
    }

    was_on_floor = IsOnFloor();
  }

  public Dictionary<string, object> save() {
    return save_data;
  }

  public void reset() {
    animatedSpriteNode.Play("idle");
    animatedSpriteNode.Stop();
    GlobalPosition = new Vector2(Convert.ToSingle(save_data["position_x"]), Convert.ToSingle(save_data["position_y"]));
    Velocity = Vector2.Zero;
    var angle_rot = Convert.ToSingle(save_data["angle"]);
    Rotate(angle_rot - Rotation);
    CurrentDefaultCornerScaleFactor = Convert.ToSingle(save_data["default_corner_scale_factor"]);
    ShowColorAreas();
    SwitchRotationState((PlayerBaseState)states_store.GetState(PlayerStatesEnum.IDLE));
    SwitchState((PlayerBaseState)states_store.GetState(PlayerStatesEnum.FALLING));
    handle_input_is_disabled = false;
  }

  private void OnCheckpointHit(CheckpointArea checkpoint_object) {
    if (bottomFaceNode.GetGroups().Contains(checkpoint_object.color_group)) {
      save_data["angle"] = 0f;
    }
    else if (leftFaceNode.GetGroups().Contains(checkpoint_object.color_group)) {
      save_data["angle"] = -Mathf.Pi / 2f;
    }
    else if (rightFaceNode.GetGroups().Contains(checkpoint_object.color_group)) {
      save_data["angle"] = Mathf.Pi / 2f;
    }
    else if (topFaceNode.GetGroups().Contains(checkpoint_object.color_group)) {
      save_data["angle"] = Mathf.Pi;
    }

    if (checkpoint_object.IsInsideTree()) {
      save_data["position_x"] = checkpoint_object.GlobalPosition.X;
      save_data["position_y"] = checkpoint_object.GlobalPosition.Y;
    }

    save_data["default_corner_scale_factor"] = CurrentDefaultCornerScaleFactor;
  }

  private void ConnectSignals() {
    EventHandler.Instance.Connect(EventType.PlayerDying, new Callable(this, nameof(OnPlayerDying)));
    EventHandler.Instance.Connect(EventType.CheckpointReached, new Callable(this, nameof(OnCheckpointHit)));
    EventHandler.Instance.Connect(EventType.CheckpointLoaded, new Callable(this, nameof(reset)));
  }

  private void DisconnectSignals() {
    EventHandler.Instance.Disconnect(EventType.PlayerDying, new Callable(this, nameof(OnPlayerDying)));
    EventHandler.Instance.Disconnect(EventType.CheckpointReached, new Callable(this, nameof(OnCheckpointHit)));
    EventHandler.Instance.Disconnect(EventType.CheckpointLoaded, new Callable(this, nameof(reset)));
  }

  public override void _EnterTree() {
    Global.Instance().Player = this;
    ConnectSignals();
  }

  public override void _ExitTree() {
    DisconnectSignals();
  }

  private void OnPlayerDying(Node area, Vector2 position, int entity_type) {
    var next_state = (player_state).OnPlayerDying(this, area, position, (Constants.EntityType)entity_type);
    SwitchState(next_state);
  }

  private bool IsJustHitTheFloor() {
    return !was_on_floor && IsOnFloor();
  }

  private void OnLand() {
    var next_player_state = player_state.OnLand(this);
    SwitchState(next_player_state);
  }

  private void SwitchState(PlayerBaseState new_state) {
    if (new_state != null) {
      player_state.Exit(this);
      player_state = new_state;
      player_state.Enter(this);
    }
  }

  private void SwitchRotationState(PlayerBaseState new_state) {
    if (new_state != null) {
      player_rotation_state.Exit(this);
      player_rotation_state = new_state;
      player_rotation_state.Enter(this);
    }
  }

  private void ScaleFaceSeparatorsBy(float factor) {
    foreach (var face_sep in faceSeparatorNodes) {
      face_sep.ScaleBy(factor);
    }
  }

  private void ScaleFacesBy(float factor) {
    foreach (var face_sep in faceNodes) {
      face_sep.ScaleBy(factor);
    }
  }

  public void ScaleCornersBy(float factor) {
    if (current_scale_factor == factor)
      return;
    current_scale_factor = factor;
    var edge = faceSeparatorNodes[0].edgeLength;
    var face = faceNodes[0].edgeLength;
    var total_length = 2 * edge + face;
    var reverse_factor = (total_length - 2f * edge * factor) / face;
    ScaleFaceSeparatorsBy(factor);
    ScaleFacesBy(reverse_factor);
  }

  public Vector2 GetCollisionShapeSize() {
    var extra_w = (FaceCollisionShapeL_node.Shape as RectangleShape2D).Size.X;
    return ((collisionShapeNode.Shape as RectangleShape2D).Size * 0.5f + 2.0f * new Vector2(extra_w, extra_w)) * 2.0f;
  }

  public bool ContainsNode(Node node) {
    return GetChildren().Contains(node);
  }

  // This function is a hack for bullets and fast moving objects because of this Godot issue:
  // https://github.com/godotengine/godot/issues/43743
  public void OnFastAreaCollidingWithPlayerShape(uint body_shape_index, Area2D color_area, Constants.EntityType entity_type) {
    var collision_shape = (CollisionShape2D)ShapeOwnerGetOwner(body_shape_index);
    var shape_groups = collision_shape.GetGroups();
    var group_found = false;
    foreach (string group in shape_groups) {
      if (color_area.IsInGroup(group)) {
        group_found = true;
        break;
      }
    }
    if (!group_found) {
      EventHandler.Instance.EmitPlayerDying(color_area, GlobalPosition, entity_type);
    }
  }

  // Face areas backup
  private void FillFaceNodesBackup() {
    _face_nodes_mask_backup.Clear();
    foreach (var face in faceNodes) {
      _face_nodes_mask_backup.Add(new Dictionary<string, int>
            {
                { "layer", (int)face.CollisionLayer },
                { "mask", (int)face.CollisionMask }
            });
    }
  }

  private void FillFaceSeparatorsBackup() {
    _face_separators_mask_backup.Clear();
    foreach (var face in faceSeparatorNodes) {
      _face_separators_mask_backup.Add(new Dictionary<string, int>
            {
                { "layer", (int)face.CollisionLayer },
                { "mask", (int)face.CollisionMask }
            });
    }
  }

  public void HideColorAreas() {
    FillFaceSeparatorsBackup();
    foreach (var face in faceSeparatorNodes) {
      face.CollisionLayer = 0;
      face.CollisionMask = 0;
    }
    FillFaceNodesBackup();
    foreach (var face in faceNodes) {
      face.CollisionLayer = 0;
      face.CollisionMask = 0;
    }
  }

  public void SetCollisionShapesDisabledFlagDeferred(bool disable) {
    CallDeferred(nameof(SetCollisionShapesDisabledFlag), disable);
  }

  private void SetCollisionShapesDisabledFlag(bool disable) {
    foreach (var face in faceCollisionNodes) {
      face.Disabled = disable;
    }
    foreach (var face in faceCornerCollisionNodes) {
      face.Disabled = disable;
    }
    collisionShapeNode.Disabled = disable;
  }

  public void ShowColorAreas() {
    for (int i = 0; i < faceSeparatorNodes.Count; i++) {
      faceSeparatorNodes[i].CollisionLayer = (uint)_face_separators_mask_backup[i]["layer"];
      faceSeparatorNodes[i].CollisionMask = (uint)_face_separators_mask_backup[i]["mask"];
    }
    for (int i = 0; i < faceNodes.Count; i++) {
      faceNodes[i].CollisionLayer = (uint)_face_nodes_mask_backup[i]["layer"];
      faceNodes[i].CollisionMask = (uint)_face_nodes_mask_backup[i]["mask"];
    }
  }

  // Methods added to convert PianoNote to C#
  public bool IsJumpingState() {
    return player_state.baseState == PlayerStatesEnum.JUMPING;
  }

  public bool IsFalling() {
    return Velocity.Y >= -Constants.EPSILON;
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
    Velocity = new Vector2(SPEED, Velocity.Y);
  }

  public void load(Dictionary<string, object> save_data) {
    this.save_data = save_data;
    reset();
  }
}
