namespace Wfc.Entities.World.Exit;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Chickensoft.Sync.Primitives;
using Godot;
using Wfc.Core.Event;
using Wfc.Core.Localization;
using Wfc.Screens.Levels;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using Wfc.Utils.Layers;

// The end of a level. Crossing it takes the player's input away and walks them off the right
// of the frame while the camera holds where it was, so what ends the level is them leaving
// the view rather than anything they had to collect on the way.
[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class LevelExit : Area2D {
  private AutoChannel.Binding? _checkpointBinding;

  public override void _Notification(int what) => this.Notify(what);

  #region Constants
  // Start and end have to agree on this id or the cutscene never hands input back.
  private const string CUTSCENE_ID = "LevelExit";
  private const float CAMERA_HOLD_SPEED = 3.2f;
  // How far past the edge the player goes before the level is called cleared, so the clear
  // lands once they are out of sight rather than as their leading edge touches it.
  private const float OFF_SCREEN_MARGIN = 96.0f;
  // A walk that cannot finish - something in the way, a player pinned against it - would hold
  // the input lock for as long as the level lives, so the clear comes anyway.
  private const float WALK_TIMEOUT = 12.0f;
  #endregion Constants

  #region Exports
  // How far past the threshold still counts as having crossed it. The exit is a line, not a
  // doorway: a strip only as wide as the arch can be cleared in one jump, and the player who
  // comes down on the far side of it is left walking into the end wall with nothing to trigger
  // and no way back up. Wider than the ground any level leaves beyond its exit.
  [Export]
  public float CrossingWidth { get; set; } = 4096.0f;
  #endregion Exports

  #region Dependencies
  [Dependency]
  public IGameLevel GameLevel => this.DependOn<IGameLevel>();
  #endregion Dependencies

  #region Nodes
  [NodePath("CollisionShape2D")]
  private CollisionShape2D _collisionShapeNode = default!;
  [NodePath("CameraAnchor")]
  private Marker2D _cameraAnchorNode = default!;
  #endregion Nodes

  private bool _isSubscribed;
  private bool _isWalkingOut;
  private float _walkTimeLeft;

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    CollisionMask = PhysicsLayers.Player.Mask;
    _extendPastTheThreshold();
    SetProcess(false);
  }

  // Grown to the right of the line the scene drew, never to the left of it: where the level ends
  // is authored, while everything beyond it is run-off the player cannot come back from.
  private void _extendPastTheThreshold() {
    if (_collisionShapeNode.Shape is not RectangleShape2D box) {
      return;
    }
    var leftEdge = _collisionShapeNode.Position.X - (box.Size.X * 0.5f);
    box.Size = new Vector2(CrossingWidth, box.Size.Y);
    _collisionShapeNode.Position = new Vector2(leftEdge + (CrossingWidth * 0.5f), _collisionShapeNode.Position.Y);
  }

  public override void _EnterTree() {
    base._EnterTree();
    if (!_isSubscribed) {
    _checkpointBinding ??= GameEvents.Instance.Channel.Bind()
      .On((in IGameEvents.CheckpointLoaded _) => _onCheckpointLoaded());
      _isSubscribed = true;
    }
  }

  public override void _ExitTree() {
    base._ExitTree();
    if (_isSubscribed) {
    _checkpointBinding?.Dispose();
    _checkpointBinding = null;
      _isSubscribed = false;
    }
  }

  private void _onBodyEntered(Node body) {
    if (!_isWalkingOut && body == GameLevel.PlayerNode) {
      _beginWalkOut();
    }
  }

  private void _beginWalkOut() {
    _isWalkingOut = true;
    _walkTimeLeft = WALK_TIMEOUT;
    var cameraNode = GameLevel.CameraNode;
    // Anchored on what the camera is showing rather than on the camera node, which its drag
    // margins leave standing beside that: the view holds still instead of easing over first.
    _cameraAnchorNode.GlobalPosition = cameraNode.GetScreenCenterPosition();
    // Never handed back: the frame the player walks out of is the last one this level shows,
    // and aiming the camera at them again would chase them off-screen as the level clears. A
    // respawn revokes the borrow on its own, so nothing is left holding it.
    cameraNode.BeginFocusOverride(_cameraAnchorNode, CAMERA_HOLD_SPEED);
    GameEvents.Instance.OnCutsceneRequestStart(CUTSCENE_ID);
    GameEvents.Instance.OnNotificationRaised(TranslationKey.game_notification_levelCleared);
    SetProcess(true);
  }

  public override void _Process(double delta) {
    base._Process(delta);
    if (!_isWalkingOut) {
      SetProcess(false);
      return;
    }
    _walkTimeLeft -= (float)delta;
    var playerNode = GameLevel.PlayerNode;
    if (!IsInstanceValid(playerNode)) {
      return;
    }
    // The input lock belongs to the cutscene, so someone has to push.
    playerNode.SetMaxSpeed();
    if (_walkTimeLeft <= 0.0f || _hasLeftTheView(playerNode)) {
      _finishWalkOut();
    }
  }

  private bool _hasLeftTheView(Node2D playerNode) {
    var cameraNode = GameLevel.CameraNode;
    var halfViewWidth = GetViewport().GetVisibleRect().Size.X * 0.5f / cameraNode.Zoom.X;
    return playerNode.GlobalPosition.X > cameraNode.GetScreenCenterPosition().X + halfViewWidth + OFF_SCREEN_MARGIN;
  }

  private void _finishWalkOut() {
    _isWalkingOut = false;
    SetProcess(false);
    GameEvents.Instance.OnCutsceneRequestEnd(CUTSCENE_ID);
    GameEvents.Instance.OnLevelCleared();
  }

  // A respawn puts the player back before the exit, so the walk goes back with them and the
  // trigger is armed again. The camera and the cutscene undo themselves on the same signal.
  private void _onCheckpointLoaded() {
    if (_isWalkingOut) {
      _isWalkingOut = false;
      SetProcess(false);
    }
  }
}
