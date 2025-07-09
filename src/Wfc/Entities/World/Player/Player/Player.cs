namespace Wfc.Entities.World.Player;

using System;
using System.Collections.Generic;
using Godot;
using Wfc.Core.Event;
using Wfc.Core.Persistence;
using Wfc.Core.Serialization;
using Wfc.Utils;
using Wfc.Utils.Animation;
using Wfc.Utils.Attributes;
using Wfc.Utils.Interpolation;
using EventHandler = Wfc.Core.Event.EventHandler;

public partial class Player : CharacterBody2D, IPersistent {

  private sealed record SaveData(
    float PositionX = 0f,
    float PositionY = 0f,
    float Angle = 0f,
    float DefaultCornerScaleFactor = 1f
);

  public const float SQUEEZE_ANIM_DURATION = 0.17f;
  public const float SCALE_ANIM_DURATION = 0.17f;

  public const float SPEED = 3.5f * Constants.WORLD_TO_SCREEN;
  public const float SPEED_UNIT = 0.7f * Constants.WORLD_TO_SCREEN;
  public float SpeedLimit { get; set; } = SPEED;
  public float SpeedUnit { get; set; } = SPEED_UNIT;

  public PlayerRotationAction PlayerRotationAction { get; private set; } = new();

  public TransformAnimation ScaleAnimation { get; private set; } = null!;
  public TransformAnimation IdleAnimation { get; set; } = null!;
  public TransformAnimation CurrentAnimation { get; set; } = null!;

  private int _spriteSize;
  public bool WasOnFloor { get; private set; } = true;

  public PlayerStatesStore StatesStore { get; private set; } = null!;
  public PlayerBaseState PlayerState { get; set; } = null!;
  public PlayerBaseState PlayerRotationState { get; private set; } = null!;

  public bool CanDash = true;
  public bool HandleInputIsDisabled = false;


  [NodePath("JumpParticles")]
  public CpuParticles2D JumpParticlesNode = null!;
  [NodePath("FallTimer")]
  public Timer FallTimerNode = null!;
  [NodePath("FaceSeparatorBR")]
  private BoxCorner _faceSeparatorBR_node = null!;
  [NodePath("FaceSeparatorBL")]
  private BoxCorner _faceSeparatorBL_node = null!;
  [NodePath("FaceSeparatorTL")]
  private BoxCorner _faceSeparatorTL_node = null!;
  [NodePath("FaceSeparatorTR")]
  private BoxCorner _faceSeparatorTR_node = null!;
  [NodePath("BottomFace")]
  private BoxFace _bottomFaceNode = null!;
  [NodePath("TopFace")]
  private BoxFace _topFaceNode = null!;
  [NodePath("LeftFace")]
  private BoxFace _leftFaceNode = null!;
  [NodePath("RightFace")]
  private BoxFace _rightFaceNode = null!;
  [NodePath("FaceCollisionShapeL")]
  private CollisionShape2D FaceCollisionShapeL_node = null!;
  [NodePath("FaceCollisionShapeR")]
  private CollisionShape2D FaceCollisionShapeR_node = null!;
  [NodePath("FaceCollisionShapeT")]
  private CollisionShape2D FaceCollisionShapeT_node = null!;
  [NodePath("FaceCollisionShapeB")]
  private CollisionShape2D FaceCollisionShapeB_node = null!;
  [NodePath("FaceCollisionShapeTL")]
  public CollisionShape2D FaceCollisionShapeTL_node = null!;
  [NodePath("FaceCollisionShapeTR")]
  public CollisionShape2D FaceCollisionShapeTR_node = null!;
  [NodePath("FaceCollisionShapeBL")]
  public CollisionShape2D FaceCollisionShapeBL_node = null!;
  [NodePath("FaceCollisionShapeBR")]
  public CollisionShape2D FaceCollisionShapeBR_node = null!;

  [NodePath("CollisionShape2D")]
  private CollisionShape2D _collisionShapeNode = null!;
  [NodePath("AnimatedSprite2D")]
  public AnimatedSprite2D AnimatedSpriteNode = null!;
  [NodePath("DashGhostTimer")]
  public Timer DashGhostTimerNode = null!;

  private List<BoxCorner> faceSeparatorNodes = new List<BoxCorner>();
  private List<BoxFace> faceNodes = new List<BoxFace>();
  private List<CollisionShape2D> faceCollisionNodes = new List<CollisionShape2D>();
  private List<CollisionShape2D> faceCornerCollisionNodes = new List<CollisionShape2D>();


  private SaveData _saveData = new SaveData();

  // Used to backup collision layer and collision mask of the player areas
  private List<Dictionary<string, int>> _faceSeparatorsMaskBackup = new List<Dictionary<string, int>>();
  private List<Dictionary<string, int>> _faceNodesMaskBackup = new List<Dictionary<string, int>>();

  public float CurrentDefaultCornerScaleFactor { get; set; } = 1.0f;
  private float _currentScaleFactor = 1.0f; // Do not edit by yourself this is used by scale_corners_by

  [NodePath("AnimatedSprite2D/LightOccluder2D")]
  public LightOccluder2D LightOccluder = null!;

  private void PrepareChildrenNodes() {
    faceSeparatorNodes = new List<BoxCorner>
        {
            _faceSeparatorBR_node,
            _faceSeparatorBL_node,
            _faceSeparatorTL_node,
            _faceSeparatorTR_node
        };

    faceNodes = new List<BoxFace>
        {
            _bottomFaceNode,
            _topFaceNode,
            _leftFaceNode,
            _rightFaceNode
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
    PlayerRotationAction.SetBody(this);
    AnimatedSpriteNode.SpriteFrames.SetFrame("idle", 0, Global.Instance().GetPlayerSprite());
    _spriteSize = AnimatedSpriteNode.SpriteFrames.GetFrameTexture("idle", 0).GetWidth();
    InitSpriteAnimation();
    WasOnFloor = IsOnFloor();
    UpDirection = Vector2.Up;
    InitFacesAreas();
    InitState();

    _saveData = new SaveData(GlobalPosition.X, GlobalPosition.Y, 0f, 1f);
  }

  private void InitSpriteAnimation() {
    IdleAnimation = new TransformAnimation(0, new ElasticOut(1, 1, 1, 0.1f), 0);
    ScaleAnimation = new TransformAnimation(SCALE_ANIM_DURATION, new ElasticOut(1, 1, 1, 0.1f), _spriteSize * 0.5f);
    CurrentAnimation = IdleAnimation;
  }

  private void InitState() {
    StatesStore = new PlayerStatesStore(this);
    PlayerState = StatesStore.fallingState;
    PlayerState.Enter(this);
    PlayerRotationState = StatesStore.idleState;
    PlayerRotationState.Enter(this);
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

  public override void _Input(InputEvent @event) {
    PlayerState._Input(@event);
    PlayerRotationState._Input(@event);
  }

  public override void _PhysicsProcess(double delta) {
    var nextState = PlayerRotationState.PhysicsUpdate(this, (float)delta) as PlayerBaseState;
    SwitchRotationState(nextState);

    var nextPlayerState = PlayerState.PhysicsUpdate(this, (float)delta) as PlayerBaseState;
    SwitchState(nextPlayerState);

    if (IsJustHitTheFloor()) {
      OnLand();
    }

    WasOnFloor = IsOnFloor();
  }

  public void reset() {
    AnimatedSpriteNode.Play("idle");
    AnimatedSpriteNode.Stop();
    GlobalPosition = new Vector2(_saveData.PositionX, _saveData.PositionY);
    Velocity = Vector2.Zero;
    Rotate(_saveData.Angle - Rotation);
    CurrentDefaultCornerScaleFactor = _saveData.DefaultCornerScaleFactor;
    ShowColorAreas();
    SwitchRotationState(StatesStore.GetState(PlayerStatesEnum.IDLE) as PlayerBaseState);
    SwitchState(StatesStore.GetState(PlayerStatesEnum.FALLING) as PlayerBaseState);
    HandleInputIsDisabled = false;
  }

  private void OnCheckpointHit(CheckpointArea checkpoint_object) {
    var angle = 0f;

    if (_bottomFaceNode.GetGroups().Contains(checkpoint_object.color_group)) {
      angle = 0f;
    }

    else if (_leftFaceNode.GetGroups().Contains(checkpoint_object.color_group)) {
      angle = -Mathf.Pi / 2f;
    }
    else if (_rightFaceNode.GetGroups().Contains(checkpoint_object.color_group)) {
      angle = Mathf.Pi / 2f;
    }
    else if (_topFaceNode.GetGroups().Contains(checkpoint_object.color_group)) {
      angle = Mathf.Pi;
    }

    Vector2 position = checkpoint_object.IsInsideTree()
        ? new Vector2(checkpoint_object.GlobalPosition.X, checkpoint_object.GlobalPosition.Y)
        : new Vector2(0f, 0f);

    _saveData = new SaveData(position.X, position.Y, angle, CurrentDefaultCornerScaleFactor);
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
    this.WireNodes();
    Global.Instance().Player = this;
    ConnectSignals();
  }

  public override void _ExitTree() {
    DisconnectSignals();
  }

  private void OnPlayerDying(Node? area, Vector2 position, int entity_type) {
    var next_state = (PlayerState).OnPlayerDying(this, area, position, (Constants.EntityType)entity_type);
    SwitchState(next_state);
  }

  private bool IsJustHitTheFloor() {
    return !WasOnFloor && IsOnFloor();
  }

  private void OnLand() {
    var next_player_state = PlayerState.OnLand(this);
    SwitchState(next_player_state);
  }

  private void SwitchState(PlayerBaseState? new_state) {
    if (new_state != null) {
      PlayerState.Exit(this);
      PlayerState = new_state;
      PlayerState.Enter(this);
    }
  }

  private void SwitchRotationState(PlayerBaseState? new_state) {
    if (new_state != null) {
      PlayerRotationState.Exit(this);
      PlayerRotationState = new_state;
      PlayerRotationState.Enter(this);
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
    if (_currentScaleFactor == factor)
      return;
    _currentScaleFactor = factor;
    var edge = faceSeparatorNodes[0].EdgeLength;
    var face = faceNodes[0].EdgeLength;
    var total_length = 2 * edge + face;
    var reverse_factor = (total_length - 2f * edge * factor) / face;
    ScaleFaceSeparatorsBy(factor);
    ScaleFacesBy(reverse_factor);
  }

  public Vector2 GetCollisionShapeSize() {
    var extra_w = (FaceCollisionShapeL_node.Shape as RectangleShape2D)?.Size.X ?? 0f;
    return (((_collisionShapeNode.Shape as RectangleShape2D)?.Size ?? Vector2.Zero) * 0.5f + 2.0f * new Vector2(extra_w, extra_w)) * 2.0f;
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
    _faceNodesMaskBackup.Clear();
    foreach (var face in faceNodes) {
      _faceNodesMaskBackup.Add(new Dictionary<string, int>
            {
                { "layer", (int)face.CollisionLayer },
                { "mask", (int)face.CollisionMask }
            });
    }
  }

  private void FillFaceSeparatorsBackup() {
    _faceSeparatorsMaskBackup.Clear();
    foreach (var face in faceSeparatorNodes) {
      _faceSeparatorsMaskBackup.Add(new Dictionary<string, int>
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
    _collisionShapeNode.Disabled = disable;
  }

  public void ShowColorAreas() {
    for (int i = 0; i < faceSeparatorNodes.Count; i++) {
      faceSeparatorNodes[i].CollisionLayer = (uint)_faceSeparatorsMaskBackup[i]["layer"];
      faceSeparatorNodes[i].CollisionMask = (uint)_faceSeparatorsMaskBackup[i]["mask"];
    }
    for (int i = 0; i < faceNodes.Count; i++) {
      faceNodes[i].CollisionLayer = (uint)_faceNodesMaskBackup[i]["layer"];
      faceNodes[i].CollisionMask = (uint)_faceNodesMaskBackup[i]["mask"];
    }
  }

  // Methods added to convert PianoNote to C#
  public bool IsJumpingState() {
    return PlayerState.baseState == PlayerStatesEnum.JUMPING;
  }

  public bool IsFalling() {
    return Velocity.Y >= -Constants.EPSILON;
  }

  public bool IsRotationIdle() {
    return PlayerRotationState.baseState == PlayerStatesEnum.IDLE;
  }


  public bool IsStanding() {
    return PlayerState.baseState == PlayerStatesEnum.STANDING;
  }

  public bool IsDying() {
    return PlayerState.baseState == PlayerStatesEnum.DYING;
  }

  public void SetMaxSpeed() {
    Velocity = new Vector2(SPEED, Velocity.Y);
  }

  public string GetSaveId() => GetPath();
  public string Save(ISerializer serializer) {
    return serializer.Serialize(this._saveData);
  }
  public void Load(ISerializer serializer, string data) {
    this._saveData = serializer.Deserialize<SaveData>(data) ?? new SaveData();
    reset();
  }
}
