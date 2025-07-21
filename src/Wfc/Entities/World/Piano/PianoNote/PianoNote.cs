namespace Wfc.Entities.World.Piano;

using System;
using System.Collections.Generic;
using Godot;
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
  private bool _isPlayerAboveTheNote = false;

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
    Position = _calculatedPosition;
    _isPlayerAboveTheNote = false;
    if (IsPressingOrPressedState()) {
      _isPlayerAboveTheNote = RaycastPlayer();
    }
    StartReleasingNoteTimerIfRelevant();
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
    if (body == Global.Instance().Player) {
      PressNoteIfRelevant();
    }
  }

  public void _onArea2DBodyExited(Node body) {
    if (body == Global.Instance().Player) {
      StartReleasingNoteTimerIfRelevant();
    }
  }

  private void PressNoteIfRelevant() {
    if (IsReleasingOrReleasedState()) {
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

  private List<Dictionary<string, Vector2>> GetRayLinesInGlobalPosition() {
    var rays = new List<Dictionary<string, Vector2>>();
    Vector2 note_half_size = GetDetectionAreaShapeSize() * 0.5f * Scale;

    var from_offset_x = new float[]
    {
            -note_half_size.X,
            -note_half_size.X * 0.5f,
            0.0f,
            note_half_size.X * 0.5f,
            note_half_size.X
    };
    var spriteHeight = _spriteNode.Texture.GetHeight();
    foreach (float offset in from_offset_x) {
      var from = GlobalPosition + new Vector2(offset, -spriteHeight * 0.5f - RAYCAST_Y_OFFSET);
      var to = from + new Vector2(0.0f, -RAYCAST_LENGTH);
      rays.Add(new Dictionary<string, Vector2> { { "from", from }, { "to", to } });
    }
    return rays;
  }

  private bool RaycastPlayer() {
    var spaceState = GetWorld2D().DirectSpaceState;
    var rays = GetRayLinesInGlobalPosition();
    foreach (var ray in rays) {
      var from = ray["from"];
      var to = ray["to"];
      var physicsRayQueryParameters = PhysicsRayQueryParameters2D.Create(from, to, exclude: new Godot.Collections.Array<Rid> { GetRid() });
      var result = spaceState.IntersectRay(physicsRayQueryParameters);
      if (result.ContainsKey("collider") && result["collider"].As<Node>().Name == Global.Instance().Player.Name) {
        return true;
      }
    }
    return false;
  }

  private static bool _isPlayerStandingOrFalling() {
    var player = Global.Instance().Player;
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
