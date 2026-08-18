namespace Wfc.Entities.World.Piano;

using System;
using System.Collections.Generic;
using Godot;
using Wfc.Core.Event;
using Wfc.Entities.World.Explosion;
using Wfc.Screens.Levels;
using Wfc.Skin;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using Wfc.Utils.Colors;

[ScenePath]
public partial class PianoNote : AnimatableBody2D {

  [Export]
  public int Index = 0;
  [Export]
  public string ColorGroup {
    get { return _colorGroup; }
    set { _setColorGroup(value); }
  }
  [Export]
  public int NoteEdgeIndex = 0;

  private enum NoteStates {
    Released,
    Pressing,
    Pressed,
    Releasing
  }

  [Signal]
  public delegate void OnNotePressedEventHandler(int noteIndex);
  [Signal]
  public delegate void OnNoteReleasedEventHandler(int noteIndex);

  private static readonly Texture2D PairTexture = GD.Load<Texture2D>("res://Assets/Sprites/Piano/note_1.png");
  private static readonly Texture2D OddTexture = GD.Load<Texture2D>("res://Assets/Sprites/Piano/note_2.png");

  private static readonly Texture2D[] NoteEdgeTextures = {
        GD.Load<Texture2D>("res://Assets/Sprites/Piano/note_edge.png"),
        GD.Load<Texture2D>("res://Assets/Sprites/Piano/note_edge2.png"),
        GD.Load<Texture2D>("res://Assets/Sprites/Piano/note_edge3.png"),
    };

  private static readonly Vector2 PRESS_OFFSET = new Vector2(0, 25);
  private const float PRESS_SPEED = 2.5f * Constants.WORLD_TO_SCREEN;
  private const float RAYCAST_Y_OFFSET = 2.5f;
  private const float RAYCAST_LENGTH = 20.0f;
  private const float RESPONSIVENESS = 0.06f;

  private static readonly Vector2 STRIKE_OFFSET = new Vector2(0, 10);
  private const float STRIKE_DOWN_DURATION = 0.035f;
  private const float STRIKE_UP_DURATION = 0.11f;
  private const float STRIKE_COOLDOWN = 0.15f;
  private const float MIN_STRIKE_SPEED = 1.0f * Constants.WORLD_TO_SCREEN;

  // How long the cube has to be out of a key's reach before it counts as having left it. A key
  // sinking under a cube standing still out-runs it, dropping the cube clear of the key's own
  // detection area and catching it again a frame or two later; answering that as a fresh arrival
  // sounds the note again, and again, under a cube that never moved. Well over the frame or two
  // that costs, and well under the only way off a key the cube has, which is a full jump.
  private const float DEPARTURE = 0.2f;

  private string _colorGroup = ColorUtils.BLUE;

  private NoteStates _currentState = NoteStates.Released;

  [NodePath("NoteSpr")]
  private Sprite2D _spriteNode = null!;
  [NodePath("Area2D/CollisionShape2D")]
  private CollisionShape2D _areaCollisionShapeNode = null!;
  [NodePath("CollisionShape2D")]
  private CollisionShape2D _collisionShapeNode = null!;
  [NodePath("ResponsivenessTimer")]
  private Timer _responsivenessTimerNode = null!;
  [NodePath("NoteEdge")]
  private Sprite2D _noteEdge = null!;

  private Vector2 _releasedPosition;
  private Vector2 _calculatedPosition;
  private Tween? _tweener = null;
  private Vector2 _strikeOffset = Vector2.Zero;
  private Tween? _strikeTween = null;
  private float _strikeCooldown = 0.0f;
  private Player.Player? _playerOnTheNote = null;
  private float? _timeOffTheNote = null;
  private bool _wasOnlyReachingOver = false;
  private bool _isPlayerAboveTheNote = false;
  private PhysicsRayQueryParameters2D? _playerProbe;
  private static readonly StringName _colliderKey = "collider";

  public override void _EnterTree() {
    base._EnterTree();
    this.WireNodes();
    _setColorGroup(_colorGroup);
    SetNoteEdgeIndex(NoteEdgeIndex);
    _responsivenessTimerNode.Timeout += _onResponsivenessTimerTimeout;
  }

  public override void _ExitTree() {
    base._ExitTree();
    _responsivenessTimerNode.Timeout -= _onResponsivenessTimerTimeout;
  }

  public override void _Ready() {
    base._Ready();
    _releasedPosition = Position;
    _calculatedPosition = Position;

    SetupResponsivenessTimer();
    SetTexture();
  }

  public override void _PhysicsProcess(double delta) {
    base._PhysicsProcess(delta);
    Position = _calculatedPosition + _strikeOffset;
    if (_strikeCooldown > 0.0f) {
      _strikeCooldown -= (float)delta;
    }
    _isPlayerAboveTheNote = false;
    if (IsPressingOrPressedState()) {
      _isPlayerAboveTheNote = RaycastPlayer();
    }
    StartReleasingNoteTimerIfRelevant();
    _pressIfTheCubeHasJustArrived(delta);
  }

  private void SetTexture() {
    _spriteNode.Texture = Index % 2 == 0 ? OddTexture : PairTexture;
  }

  private void SetupResponsivenessTimer() {
    _responsivenessTimerNode.Autostart = false;
    _responsivenessTimerNode.WaitTime = RESPONSIVENESS;
  }

  private void MoveToPosition(Vector2 dest_position) {
    float duration = Math.Abs(_calculatedPosition.Y - dest_position.Y) / PRESS_SPEED;
    _tweener?.Kill();
    _tweener = CreateTween();
    _tweener.Connect(
      Tween.SignalName.Finished,
      new Callable(this, nameof(OnTweenCompleted)),
      flags: (uint)ConnectFlags.OneShot
    );
    _tweener.TweenProperty(this, nameof(_calculatedPosition), dest_position, duration)
        .From(_calculatedPosition)
        .SetTrans(Tween.TransitionType.Linear)
        .SetEase(Tween.EaseType.InOut);
  }

  private bool IsReleasingOrReleasedState() {
    return _currentState == NoteStates.Released || _currentState == NoteStates.Releasing;
  }

  private bool IsPressingOrPressedState() {
    return _currentState == NoteStates.Pressed || _currentState == NoteStates.Pressing;
  }

  public void _onArea2DBodyEntered(Node body) {
    if (body is Player.Player player) {
      bool hasJustArrived = _playerOnTheNote is null;
      _playerOnTheNote = player;
      _timeOffTheNote = null;
      if (hasJustArrived) {
        _wasOnlyReachingOver = _isPlayerOnlyReachingOver();
        PressNoteIfRelevant();
      }
    }
    else if (body is ExplosionElement debris) {
      _strikeNote(debris);
    }
  }

  // Whether the cube has so little of itself over this key that it is reaching across it from the
  // one beside it. Walking a key to its end reaches over its neighbour, and a key that answered
  // that would sound a note the player never played and spell it onto the sheet as their answer.
  private bool _isPlayerOnlyReachingOver() {
    if (_playerOnTheNote is not { } player || !IsInstanceValid(player)) {
      return true;
    }
    var half = GetDetectionAreaShapeSize().X * 0.5f * Mathf.Abs(GlobalScale.X);
    var center = _areaCollisionShapeNode.GlobalPosition.X;
    var reach = player.GetCollisionHalfExtents().X;
    var overlap = Mathf.Min(player.GlobalPosition.X + reach, center + half)
      - Mathf.Max(player.GlobalPosition.X - reach, center - half);
    return overlap < player.GrazeWidth;
  }

  // Debris raining onto the keyboard sounds the key it lands on, but deliberately stays out of
  // the press state machine: the board would otherwise read a death as a run of wrong answers.
  private void _strikeNote(ExplosionElement debris) {
    if (_strikeCooldown > 0.0f || !debris.TryStrike(MIN_STRIKE_SPEED)) {
      return;
    }
    _strikeCooldown = STRIKE_COOLDOWN;
    GameEvents.Instance.OnPianoNoteStruck(Index);
    _strikeTween?.Kill();
    _strikeTween = CreateTween();
    _strikeTween.TweenProperty(this, nameof(_strikeOffset), STRIKE_OFFSET, STRIKE_DOWN_DURATION)
        .From(_strikeOffset);
    _strikeTween.TweenProperty(this, nameof(_strikeOffset), Vector2.Zero, STRIKE_UP_DURATION);
  }

  public void _onArea2DBodyExited(Node body) {
    if (body is Player.Player) {
      _timeOffTheNote = 0.0f;
      StartReleasingNoteTimerIfRelevant();
    }
  }

  // The cube can be over a key without being on it yet, and walks the rest of the way from there.
  // Arriving is the only moment the area reports, so the crossing is watched for here.
  //
  // The crossing, not the standing: a key that answered for as long as the cube was on it would
  // re-press itself the moment its own release let go, and sound the same note over and over
  // under a cube that never moved.
  private void _pressIfTheCubeHasJustArrived(double delta) {
    if (_playerOnTheNote is null) {
      return;
    }
    if (!IsInstanceValid(_playerOnTheNote)) {
      _forgetTheCube();
      return;
    }
    if (_timeOffTheNote is { } away) {
      _timeOffTheNote = away + (float)delta;
      if (_timeOffTheNote > DEPARTURE) {
        _forgetTheCube();
        return;
      }
    }
    bool isOnlyReachingOver = _isPlayerOnlyReachingOver();
    if (_wasOnlyReachingOver && !isOnlyReachingOver) {
      PressNoteIfRelevant();
    }
    _wasOnlyReachingOver = isOnlyReachingOver;
  }

  private void _forgetTheCube() {
    _playerOnTheNote = null;
    _timeOffTheNote = null;
  }

  private void PressNoteIfRelevant() {
    if (IsReleasingOrReleasedState() && !_isPlayerOnlyReachingOver()) {
      StopTimerIfRelevant();
      PressNote();
    }
  }

  private void PressNote() {
    _currentState = NoteStates.Pressing;
    MoveToPosition(_releasedPosition + PRESS_OFFSET);
  }

  private void StartReleasingNoteTimerIfRelevant() {
    if (IsPressingOrPressedState() && !CheckIfPlayerIsAboveTheNote()) {
      StartTimerIfStopped();
    }
  }

  private void ReleaseNoteIfRelevant() {
    if (IsPressingOrPressedState() && !CheckIfPlayerIsAboveTheNote()) {
      ReleaseNote();
    }
  }

  private void ReleaseNote() {
    _currentState = NoteStates.Releasing;
    MoveToPosition(_releasedPosition);
  }

  private void OnTweenCompleted() {
    if (_currentState == NoteStates.Pressing) {
      _currentState = NoteStates.Pressed;
      EmitSignal(nameof(OnNotePressed), Index);
    }
    else if (_currentState == NoteStates.Releasing) {
      _currentState = NoteStates.Released;
      EmitSignal(nameof(OnNoteReleased), Index);
    }
  }

  private Vector2 GetDetectionAreaShapeSize() {
    return (_areaCollisionShapeNode.Shape as RectangleShape2D)?.Size ?? Vector2.Zero;
  }

  private Vector2 GetCollisionShapeSize() {
    return (_collisionShapeNode.Shape as RectangleShape2D)?.Size ?? Vector2.Zero;
  }

  // Five probes spread across the note's top, reused query and all: this runs every physics
  // tick the note spends pressed, and building the machinery fresh allocated a dozen engine
  // objects a tick.
  private bool RaycastPlayer() {
    var spaceState = GetWorld2D().DirectSpaceState;
    // Global, not local: the offsets it sizes are added to a global position, so a scale anywhere
    // above the key has to count.
    var noteHalfWidth = GetDetectionAreaShapeSize().X * 0.5f * Mathf.Abs(GlobalScale.X);
    var spriteHeight = _spriteNode.Texture.GetHeight();

    // Cast from the key's own surface, so a cube resting on it has the ray start inside itself.
    // Without this the probe finds nothing under the very cube it is looking for, the key takes
    // that for an empty key and rises out from under it - and the cube landing back on it plays
    // the note again, over and over, while the player stands perfectly still.
    _playerProbe ??= new PhysicsRayQueryParameters2D {
      Exclude = new Godot.Collections.Array<Rid> { GetRid() },
      HitFromInside = true,
    };

    Span<float> offsets = stackalloc float[] {
      -noteHalfWidth, -noteHalfWidth * 0.5f, 0.0f, noteHalfWidth * 0.5f, noteHalfWidth
    };
    foreach (float offset in offsets) {
      var from = GlobalPosition + new Vector2(offset, (-spriteHeight * 0.5f) - RAYCAST_Y_OFFSET);
      _playerProbe.From = from;
      _playerProbe.To = from + new Vector2(0.0f, -RAYCAST_LENGTH);
      using var result = spaceState.IntersectRay(_playerProbe);
      // As<T> is a hard cast, so converting straight to a Player throws out of the physics tick
      // on anything else the probe can hit - the debris a death leaves above the keys, say.
      if (result.Count > 0 && result[_colliderKey].As<GodotObject>() is Player.Player) {
        return true;
      }
    }
    return false;
  }

  private static bool _isPlayerStandingOrFalling() {
    if (GameRepo.Instance.Player.Value is not { } player) {
      return false;
    }
    bool isJumping = player.IsJumping();
    bool isFalling = player.IsFalling();
    return !isJumping && isFalling;
  }

  private bool CheckIfPlayerIsAboveTheNote() {
    return _isPlayerAboveTheNote && _isPlayerStandingOrFalling();
  }

  // Uncomment this code to debug draw raycast rays
  // public override void _Draw()
  // {
  //     var rays = GetRayLinesInGlobalPosition();
  //     foreach (var ray in rays)
  //     {
  //         var from = ray["from"] - GlobalPosition;
  //         var to = ray["to"] - GlobalPosition;
  //         var color = new Color(GD.Randf(), GD.Randf(), GD.Randf());
  //         DrawLine(from, to, color, 4.0f);
  //     }
  // }

  private void StartTimerIfStopped() {
    if (_responsivenessTimerNode.IsStopped()) {
      _responsivenessTimerNode.Autostart = true;
      _responsivenessTimerNode.Start();
    }
  }

  private void StopTimerIfRelevant() {
    if (!_responsivenessTimerNode.IsStopped()) {
      _responsivenessTimerNode.Autostart = false;
      _responsivenessTimerNode.Stop();
    }
  }

  public void _onResponsivenessTimerTimeout() {
    ReleaseNoteIfRelevant();
    StopTimerIfRelevant();
  }

  private void _setColorGroup(string colorGroup) {
    _colorGroup = colorGroup;
    Color color = SkinManager.Instance.CurrentSkin.GetColor(
      GameSkin.ColorGroupToSkinColor(_colorGroup),
      SkinColorIntensity.Basic
    );
    GetNode<Sprite2D>("NoteEdge").Modulate = color;
    var area = GetNode<Area2D>("ColorArea");
    foreach (string grp in area.GetGroups()) {
      area.RemoveFromGroup(grp);
    }
    area.AddToGroup(colorGroup);
  }

  public void SetNoteEdgeIndex(int noteIndex) {
    int scale = (noteIndex / (NoteEdgeTextures.Length + 1)) % 2 == 0 ? 1 : -1;
    NoteEdgeIndex = noteIndex % NoteEdgeTextures.Length;
    _noteEdge.Texture = NoteEdgeTextures[NoteEdgeIndex];
    _noteEdge.Scale = new Vector2(scale, 1);
  }

  public int GetNoteEdgeIndex() {
    return NoteEdgeIndex;
  }
}
