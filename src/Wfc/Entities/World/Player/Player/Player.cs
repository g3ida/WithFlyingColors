namespace Wfc.Entities.World.Player;

using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Autoload;
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

  // A hazard reports a contact; whether that contact kills is the cube's own business. Held here
  // rather than in the active state so there is one answer to it, and one place that knows a cube
  // already dying does not die again - hazards report per contact, and some report every frame
  // they still cover the corpse.
  private EntityType _pendingDeath = EntityType.None;

  #region Nodes
  [NodePath("JumpParticles")]
  public CpuParticles2D JumpParticlesNode = null!;
  [NodePath("DashLaunchParticles")]
  public CpuParticles2D DashLaunchParticlesNode = null!;
  [NodePath("DashImpactParticles")]
  public CpuParticles2D DashImpactParticlesNode = null!;
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
  [NodePath("CollisionShape2D")]
  private CollisionShape2D _collisionShapeNode = null!;
  [NodePath("AnimatedSprite2D")]
  public AnimatedSprite2D AnimatedSpriteNode = null!;
  [NodePath("Hitstop")]
  public Hitstop HitstopNode = null!;
  private List<BoxCorner> faceSeparatorNodes = new List<BoxCorner>();
  private List<BoxFace> faceNodes = new List<BoxFace>();
  #endregion Nodes

  private SaveData _saveData = new SaveData();

  // Used to backup collision layer and collision mask of the player areas
  private List<Dictionary<string, int>> _faceSeparatorsMaskBackup = new List<Dictionary<string, int>>();
  private List<Dictionary<string, int>> _faceNodesMaskBackup = new List<Dictionary<string, int>>();
  private bool _colorAreasAreHidden;

  // How forgiving the corners are whenever no state is asking for anything else. Applied on the
  // spot rather than left for the next state to carry in: the brick breaker widens them with the
  // player already standing in the arena, and a paddle that only ever slides never changes state.
  public float CurrentDefaultCornerScaleFactor {
    get => _defaultScaleFactor;
    set {
      var stateIsHoldingItOpen = _currentScaleFactor != _defaultScaleFactor;
      _defaultScaleFactor = value;
      if (!stateIsHoldingItOpen) {
        ScaleCornersBy(value);
      }
    }
  }

  private float _defaultScaleFactor = 1.0f;
  private float _currentScaleFactor = 1.0f; // Do not edit by yourself this is used by scale_corners_by
  private PlayerBox.Ring _colorRing;

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
    InitColorAreas();

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

  private void InitColorAreas() {
    var corner = faceSeparatorNodes[0];
    _colorRing = new PlayerBox.Ring(
      CornerOuterEdge: corner.OuterReach,
      RestingCornerSide: corner.EdgeLength,
      Overlap: (faceNodes[0].EdgeLength * 0.5f) - (corner.OuterReach - corner.EdgeLength)
    );
    // Lays the areas out where they were authored, and seeds the seam every color query reads.
    // Not through ScaleCornersBy, which short-circuits on the factor it already holds.
    _layOutColorAreas(_currentScaleFactor);

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
    // The states are torn down first. Leaving one is its last chance to act on the cube - the
    // slippering state hands back a rotation on the way out - and a respawn does not always
    // interrupt a corpse: a late death report arrives on a cube that is up and playing. Whatever
    // the outgoing state asks for, the checkpoint's own description of the cube comes after it.
    CurrentDefaultCornerScaleFactor = _saveData.DefaultCornerScaleFactor;
    _switchRotationState(_statesStore?.GetState<PlayerRotatingIdleState>());
    _switchState(_statesStore?.GetState<PlayerFallingState>());
    // A contact reported in the same breath as the respawn belongs to the run that ended.
    _pendingDeath = EntityType.None;

    AnimatedSpriteNode.Play("idle");
    AnimatedSpriteNode.Stop();
    GlobalPosition = new Vector2(_saveData.PositionX, _saveData.PositionY);
    Velocity = Vector2.Zero;
    // The brick breaker's power-ups are the only thing that resizes the cube, and none of them
    // outlives a respawn.
    Scale = Vector2.One;
    Rotate(_saveData.Angle - Rotation);
    PlayerRotationAction.Reset(_saveData.Angle);
    ShowColorAreas();
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

  private void _onPlayerDying(Node? area, Vector2 position, int entityType) {
    if (IsDying()) {
      return;
    }
    _pendingDeath = (EntityType)entityType;
  }

  // Taken, not read: one contact is one death, and the state that takes it is the one that turns
  // it into a way of dying.
  public EntityType TakePendingDeath() {
    var death = _pendingDeath;
    _pendingDeath = EntityType.None;
    return death;
  }

  private void ConnectSignals() {
    EventHandler.Instance.Events.CheckpointReached += OnCheckpointHit;
    EventHandler.Instance.Events.CheckpointLoaded += Reset;
    EventHandler.Instance.Events.PlayerDying += _onPlayerDying;
  }

  private void DisconnectSignals() {
    EventHandler.Instance.Events.CheckpointReached -= OnCheckpointHit;
    EventHandler.Instance.Events.CheckpointLoaded -= Reset;
    EventHandler.Instance.Events.PlayerDying -= _onPlayerDying;
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

  // How forgiving the corners are, in the cube's own units: the seam reaches this far back along
  // each of the two faces it joins. The color areas are laid out from it and every color query
  // reads it, so a contact is judged the same wherever it came from.
  public float CornerSeam { get; private set; }

  public void ScaleCornersBy(float factor) {
    if (_currentScaleFactor == factor)
      return;
    _currentScaleFactor = factor;
    _layOutColorAreas(factor);
  }

  // The corner squares grow about their pinned outer corners and the faces give way to meet
  // them. Only the extent along each edge moves: face thickness is what decides how deep a
  // contact must be before it registers at all, and dragging it along with the corner tolerance
  // made flat-face contacts harder to detect exactly when corners were made easier.
  private void _layOutColorAreas(float factor) {
    var half = CollisionHalfExtentsLocal;
    CornerSeam = PlayerBox.ClampSeam(_colorRing.SeamFor(factor, half.X), half);

    var faceHalfLength = _colorRing.FaceHalfLengthFor(CornerSeam, half.X);
    foreach (var corner in faceSeparatorNodes) {
      corner.SetSeamSide(_colorRing.CornerSideFor(faceHalfLength));
    }
    foreach (var face in faceNodes) {
      face.SetEdgeLength(faceHalfLength * 2.0f);
    }
  }

  // The cube's outer surface, in its own units and in world units. One rectangle, which is what
  // anything bouncing off the cube, probing the ground under it or framing it in the camera
  // measures itself against.
  public Vector2 CollisionHalfExtentsLocal =>
    ((_collisionShapeNode.Shape as RectangleShape2D)?.Size ?? Vector2.Zero) * 0.5f;

  public Vector2 GetCollisionHalfExtents() => CollisionHalfExtentsLocal * GlobalScale.Abs();

  // Whether the cube survives touching `colorGroup` at a point on its surface. A point out near
  // a corner lies on the seam two faces share and either of their colors is safe there, which
  // is what the corner separators are for - and why widening them, as the brick breaker does,
  // makes the cube more forgiving to play.
  public bool AcceptsColorAt(Vector2 globalPoint, string colorGroup) =>
    _acceptsAt(globalPoint, face => face.AcceptsColor(colorGroup));

  // The same question asked of an area that carries its color as a group, which is how every
  // object the cube can touch is tagged.
  public bool AcceptsColorOfAt(Vector2 globalPoint, Area2D area) =>
    _acceptsAt(globalPoint, face => face.AcceptsColorOf(area));

  private bool _acceptsAt(Vector2 globalPoint, Func<BoxFace, bool> accepts) {
    var box = new PlayerBox(GetCollisionHalfExtents(), CornerSeam * Mathf.Abs(GlobalScale.X));
    var faces = box.FacesAt((globalPoint - GlobalPosition).Rotated(-GlobalRotation));

    // A point that belongs to no face at all is not one this cube has an opinion about.
    if (faces == PlayerBox.Faces.None) {
      return true;
    }
    return (faces.HasFlag(PlayerBox.Faces.Right) && accepts(_rightFaceNode))
      || (faces.HasFlag(PlayerBox.Faces.Left) && accepts(_leftFaceNode))
      || (faces.HasFlag(PlayerBox.Faces.Bottom) && accepts(_bottomFaceNode))
      || (faces.HasFlag(PlayerBox.Faces.Top) && accepts(_topFaceNode));
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
