namespace Wfc.Entities.World.Player;

using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Event;
using Wfc.Core.Input;
using Wfc.Core.Persistence;
using Wfc.Core.Serialization;
using Wfc.Entities.World.Checkpoints;
using Wfc.Screens.Levels;
using Wfc.State;
using Wfc.Utils;
using Wfc.Utils.Animation;
using Wfc.Utils.Attributes;
using Wfc.Utils.Colors;
using Wfc.Utils.Interpolation;
using EventHandler = Wfc.Core.Event.EventHandler;

[Meta(typeof(IAutoNode))]
public partial class Player : CharacterBody2D, IPersistent {
  public override void _Notification(int what) => this.Notify(what);

  private sealed record SaveData(
    float PositionX = 0f,
    float PositionY = 0f,
    float Angle = 0f,
    float DefaultCornerScaleFactor = 1f
);

  #region Dependencies
  [Dependency]
  public IInputManager InputManager => this.DependOn<IInputManager>();
  [Dependency]
  public IGameLevel GameLevel => this.DependOn<IGameLevel>();
  #endregion Dependencies

  #region Constants
  public const float SQUEEZE_ANIM_DURATION = 0.17f;
  public const float SCALE_ANIM_DURATION = 0.17f;

  public const float SPEED = 3.5f * Constants.WORLD_TO_SCREEN;
  public const float SPEED_UNIT = 0.7f * Constants.WORLD_TO_SCREEN;
  #endregion Constants
  public float SpeedLimit { get; set; } = SPEED;
  public float SpeedUnit { get; set; } = SPEED_UNIT;
  public PlayerRotationAction PlayerRotationAction { get; private set; } = new();
  public TransformAnimation ScaleAnimation { get; private set; } = null!;
  public TransformAnimation IdleAnimation { get; set; } = null!;
  public TransformAnimation CurrentAnimation { get; set; } = null!;
  public bool WasOnFloor { get; private set; } = true;

  private PlayerStatesStore? _statesStore = null;
  public PlayerBaseState? PlayerState { get; set; } = null;
  public IState<Player>? PlayerRotationState { get; private set; } = null!;

  public bool CanDash = true;
  public bool HandleInputIsDisabled = false;
  private int _spriteSize;
  private Texture2D? _playerSprite = null;

  #region Nodes
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
  #endregion Nodes

  private SaveData _saveData = new SaveData();

  // Used to backup collision layer and collision mask of the player areas
  private List<Dictionary<string, int>> _faceSeparatorsMaskBackup = new List<Dictionary<string, int>>();
  private List<Dictionary<string, int>> _faceNodesMaskBackup = new List<Dictionary<string, int>>();
  private bool _colorAreasAreHidden;



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

  public void OnResolved() {
    // The OnResolved method will be called after _Ready/OnReady, but before the first frame
    // if (and only if) all the providers it depends on call this.Provide() before the first frame.
    InitState();
  }

  public override void _Ready() {
    base._Ready();
    PrepareChildrenNodes();
    PlayerRotationAction.SetBody(this);
    AnimatedSpriteNode.SpriteFrames.SetFrame("idle", 0, GetSprite());
    _spriteSize = AnimatedSpriteNode.SpriteFrames.GetFrameTexture("idle", 0).GetWidth();
    InitSpriteAnimation();
    WasOnFloor = IsOnFloor();
    UpDirection = Vector2.Up;
    InitFacesAreas();

    _saveData = new SaveData(GlobalPosition.X, GlobalPosition.Y, 0f, 1f);
  }

  private void InitSpriteAnimation() {
    IdleAnimation = new TransformAnimation(0, new ElasticOut(1, 1, 1, 0.1f), 0);
    ScaleAnimation = new TransformAnimation(SCALE_ANIM_DURATION, new ElasticOut(1, 1, 1, 0.1f), _spriteSize * 0.5f);
    CurrentAnimation = IdleAnimation;
  }

  private void InitState() {
    _statesStore = new PlayerStatesStore(InputManager);
    PlayerState = _statesStore.GetState<PlayerFallingState>();
    PlayerState?.Enter(this);
    PlayerRotationState = _statesStore.GetState<PlayerRotatingIdleState>();
    PlayerRotationState?.Enter(this);
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

    _fillFaceNodesBackup();
    _fillFaceSeparatorsBackup();
  }

  public override void _PhysicsProcess(double delta) {
    base._PhysicsProcess(delta);
    var nextState = PlayerRotationState?.PhysicsUpdate(this, (float)delta);
    _switchRotationState(nextState);

    var nextPlayerState = PlayerState?.PhysicsUpdate(this, (float)delta) as PlayerBaseState;
    _switchState(nextPlayerState);

    if (_isJustHitTheFloor()) {
      _onLand();
    }

    WasOnFloor = IsOnFloor();
  }

  public void Reset() {
    AnimatedSpriteNode.Play("idle");
    AnimatedSpriteNode.Stop();
    GlobalPosition = new Vector2(_saveData.PositionX, _saveData.PositionY);
    Velocity = Vector2.Zero;
    Rotate(_saveData.Angle - Rotation);
    PlayerRotationAction.Reset(_saveData.Angle);
    CurrentDefaultCornerScaleFactor = _saveData.DefaultCornerScaleFactor;
    ShowColorAreas();
    _switchRotationState(_statesStore?.GetState<PlayerRotatingIdleState>());
    _switchState(_statesStore?.GetState<PlayerFallingState>());
    HandleInputIsDisabled = false;
  }

  private void OnCheckpointHit(Vector2 position, string colorGroup) {
    var angle = _respawnAngleForColor(colorGroup);
    if (angle == null) {
      // No face wears this color, so there is no orientation to respawn in. This used to fall
      // through with angle 0 - the bottom face - and overwrite a perfectly good save with an
      // orientation nobody asked for.
      GD.PushError($"Checkpoint color group '{colorGroup}' matches no face of the player; keeping the previous respawn point.");
      return;
    }

    _saveData = new SaveData(position.X, position.Y, angle.Value, CurrentDefaultCornerScaleFactor);
  }

  // A checkpoint names the color it wants facing the floor; this is the rotation that puts it
  // there.
  private float? _respawnAngleForColor(string colorGroup) {
    if (_bottomFaceNode.GetGroups().Contains(colorGroup)) {
      return 0f;
    }
    if (_leftFaceNode.GetGroups().Contains(colorGroup)) {
      return -Mathf.Pi / 2f;
    }
    if (_rightFaceNode.GetGroups().Contains(colorGroup)) {
      return Mathf.Pi / 2f;
    }
    if (_topFaceNode.GetGroups().Contains(colorGroup)) {
      return Mathf.Pi;
    }
    return null;
  }

  // The color currently facing the floor - what a room that wants to record a checkpoint of its
  // own, with no colored post to read an orientation off, should ask for.
  public string GroundColorGroup {
    get {
      var lowest = faceNodes[0];
      foreach (var face in faceNodes) {
        if (face.GlobalPosition.Y > lowest.GlobalPosition.Y) {
          lowest = face;
        }
      }
      var groups = lowest.GetGroups();
      return groups.Count > 0 ? groups[0].ToString() : ColorUtils.BLUE;
    }
  }

  private void ConnectSignals() {
    EventHandler.Instance.Events.CheckpointReached += OnCheckpointHit;
    EventHandler.Instance.Events.CheckpointLoaded += Reset;
  }

  private void DisconnectSignals() {
    EventHandler.Instance.Events.CheckpointReached -= OnCheckpointHit;
    EventHandler.Instance.Events.CheckpointLoaded -= Reset;
  }

  public override void _EnterTree() {
    base._EnterTree();
    this.WireNodes();
    Global.Instance().Player = this;
    ConnectSignals();
    // Leaving the tree exits the active states, which is what drops their subscriptions;
    // coming back has to put them back or the player would be inert. On the first entry
    // there is no FSM yet - InitState builds and enters it once dependencies resolve.
    PlayerState?.Enter(this);
    PlayerRotationState?.Enter(this);
  }

  public override void _ExitTree() {
    base._ExitTree();
    DisconnectSignals();

    // Every state subscribes to the process-lifetime EventHandler in Enter and unsubscribes
    // in Exit, so a state left active when the level unloads keeps an autoload holding a
    // strong handle to it. That one handle roots the states store and all of its sibling
    // states, and a stale PlayerDying subscription then fires on the next level's player -
    // quit and replay a few times and the events pile up along with the objects.
    PlayerState?.Exit(this);
    PlayerRotationState?.Exit(this);
  }

  private bool _isJustHitTheFloor() {
    return !WasOnFloor && IsOnFloor();
  }

  private void _onLand() {
    var next_player_state = PlayerState?.OnLand(this);
    _switchState(next_player_state);
  }

  private void _switchState(PlayerBaseState? new_state) {
    if (new_state != null) {
      PlayerState?.Exit(this);
      PlayerState = new_state;
      PlayerState.Enter(this);
    }
  }

  private void _switchRotationState(IState<Player>? new_state) {
    if (new_state != null) {
      PlayerRotationState?.Exit(this);
      PlayerRotationState = new_state;
      PlayerRotationState.Enter(this);
    }
  }

  private void _scaleFaceSeparatorsBy(float factor) {
    foreach (var face_sep in faceSeparatorNodes) {
      face_sep.ScaleBy(factor);
    }
  }

  private void _scaleFacesBy(float factor) {
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
    _scaleFaceSeparatorsBy(factor);
    _scaleFacesBy(reverse_factor);
  }

  public Vector2 GetCollisionShapeSize() {
    var extra_w = (FaceCollisionShapeL_node.Shape as RectangleShape2D)?.Size.X ?? 0f;
    return (((_collisionShapeNode.Shape as RectangleShape2D)?.Size ?? Vector2.Zero) * 0.5f + 2.0f * new Vector2(extra_w, extra_w)) * 2.0f;
  }

  // The cube's outer surface in world units - the hull plus the colored plates standing proud
  // of it, which is what anything bouncing off the cube actually meets. GetCollisionShapeSize
  // adds the plates twice over and so reports a cube noticeably larger than the one on screen.
  public Vector2 GetCollisionHalfExtents() {
    var plate = ((FaceCollisionShapeR_node.Shape as RectangleShape2D)?.Size.X ?? 0f) * 0.5f;
    return new Vector2(
      Mathf.Abs(FaceCollisionShapeR_node.Position.X) + plate,
      Mathf.Abs(FaceCollisionShapeB_node.Position.Y) + plate
    ) * GlobalScale.Abs();
  }

  // Whether the cube survives touching `colorGroup` at a point on its surface. A point out near
  // a corner lies on the seam two faces share and either of their colors is safe there, which
  // is what the corner separators are for - and why widening them, as the brick breaker does,
  // makes the cube more forgiving to play.
  public bool AcceptsColorAt(Vector2 globalPoint, string colorGroup) {
    var half = GetCollisionHalfExtents();
    var seam = _cornerSeamReach();
    var local = (globalPoint - GlobalPosition).Rotated(-GlobalRotation);

    var onSide = Mathf.Abs(local.X) >= half.X - seam;
    var onEnd = Mathf.Abs(local.Y) >= half.Y - seam;

    if (onSide && (local.X >= 0.0f ? _rightFaceNode : _leftFaceNode).AcceptsColor(colorGroup)) {
      return true;
    }
    if (onEnd && (local.Y >= 0.0f ? _bottomFaceNode : _topFaceNode).AcceptsColor(colorGroup)) {
      return true;
    }
    // A point that belongs to no face at all is not one this cube has an opinion about.
    return !onSide && !onEnd;
  }

  // How far a corner separator reaches along each of the two faces it joins.
  private float _cornerSeamReach() =>
    _faceSeparatorBR_node.EdgeLength * _faceSeparatorBR_node.Scale.X * GlobalScale.X * 0.5f;

  // This function is a hack for bullets and fast moving objects because of this Godot issue:
  // https://github.com/godotengine/godot/issues/43743
  //
  public void OnFastAreaCollidingWithPlayerShape(uint bodyShapeIndex, Area2D colorArea, EntityType entityType) {
    var collisionShape = (CollisionShape2D)ShapeOwnerGetOwner(bodyShapeIndex);
    var shapeGroups = collisionShape.GetGroups();
    if (shapeGroups.Count == 0) {
      return;
    }
    var groupFound = false;
    foreach (string group in shapeGroups) {
      if (colorArea.IsInGroup(group)) {
        groupFound = true;
        break;
      }
    }
    if (!groupFound) {
      EventHandler.Instance.EmitPlayerDying(colorArea, GlobalPosition, entityType);
    }
  }

  // Face areas backup
  private void _fillFaceNodesBackup() {
    _faceNodesMaskBackup.Clear();
    foreach (var face in faceNodes) {
      _faceNodesMaskBackup.Add(new Dictionary<string, int>
            {
                { "layer", (int)face.CollisionLayer },
                { "mask", (int)face.CollisionMask }
            });
    }
  }

  private void _fillFaceSeparatorsBackup() {
    _faceSeparatorsMaskBackup.Clear();
    foreach (var face in faceSeparatorNodes) {
      _faceSeparatorsMaskBackup.Add(new Dictionary<string, int>
            {
                { "layer", (int)face.CollisionLayer },
                { "mask", (int)face.CollisionMask }
            });
    }
  }

  // Hiding is what the dying states use to stop the corpse reporting further collisions.
  // It has to be idempotent: the backup is the only record of what the masks were, so
  // hiding twice would capture the zeroes it just wrote and the restore afterwards would
  // leave the player permanently unable to touch any colored surface again.
  public void HideColorAreas() {
    if (_colorAreasAreHidden) {
      return;
    }
    _colorAreasAreHidden = true;

    _fillFaceSeparatorsBackup();
    foreach (var face in faceSeparatorNodes) {
      face.CollisionLayer = 0;
      face.CollisionMask = 0;
    }
    _fillFaceNodesBackup();
    foreach (var face in faceNodes) {
      face.CollisionLayer = 0;
      face.CollisionMask = 0;
    }
  }

  public void SetCollisionShapesDisabledFlagDeferred(bool disable) {
    CallDeferred(nameof(_setCollisionShapesDisabledFlag), disable);
  }

  private void _setCollisionShapesDisabledFlag(bool disable) {
    foreach (var face in faceCollisionNodes) {
      face.Disabled = disable;
    }
    foreach (var face in faceCornerCollisionNodes) {
      face.Disabled = disable;
    }
    _collisionShapeNode.Disabled = disable;
  }

  // Reset() calls this whether or not the player was dying, and the backups only exist
  // once something has hidden them - restoring from an empty list used to be an index
  // error thrown inside a checkpoint callback.
  public void ShowColorAreas() {
    if (!_colorAreasAreHidden) {
      return;
    }
    _colorAreasAreHidden = false;

    for (int i = 0; i < faceSeparatorNodes.Count; i++) {
      faceSeparatorNodes[i].CollisionLayer = (uint)_faceSeparatorsMaskBackup[i]["layer"];
      faceSeparatorNodes[i].CollisionMask = (uint)_faceSeparatorsMaskBackup[i]["mask"];
    }
    for (int i = 0; i < faceNodes.Count; i++) {
      faceNodes[i].CollisionLayer = (uint)_faceNodesMaskBackup[i]["layer"];
      faceNodes[i].CollisionMask = (uint)_faceNodesMaskBackup[i]["mask"];
    }
  }

  public bool IsJumping() => PlayerState is PlayerJumpingState;
  public bool IsFalling() => Velocity.Y >= -MathUtils.EPSILON;
  public bool IsRotationIdle() => PlayerRotationState?.GetType()?.IsAssignableFrom(typeof(PlayerRotatingIdleState)) ?? false;
  public bool IsStanding() => PlayerState is PlayerStandingState;
  public bool IsDying() => PlayerState is PlayerDyingBaseState;
  public bool IsDashing() => PlayerState is PlayerDashingState;
  public bool IsSlippering() => PlayerState is PlayerSlipperingState;

  public void SetMaxSpeed() {
    Velocity = new Vector2(SPEED, Velocity.Y);
  }

  public string GetSaveId() => GetPath();
  public string Save(ISerializer serializer) {
    return serializer.Serialize(this._saveData);
  }
  public void Load(ISerializer serializer, string data) {
    this._saveData = serializer.Deserialize<SaveData>(data) ?? new SaveData();
    Reset();
  }

  public Texture2D GetSprite() {
    if (_playerSprite == null) {
      _playerSprite = PlayerSpriteGenerator.GetTexture();
    }
    return _playerSprite;
  }
}
