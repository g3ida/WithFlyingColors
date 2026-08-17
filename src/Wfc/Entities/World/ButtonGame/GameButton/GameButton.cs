namespace Wfc.Entities.World.ButtonGame;

using System;
using Godot;
using Wfc.Screens.Levels;
using Wfc.Skin;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using Wfc.Utils.Colors;

[ScenePath]
public partial class GameButton : Node2D {
  #region Constants
  private const float PRESS_SPEED = 2.5f * Constants.WORLD_TO_SCREEN;
  private const float RELEASE_DELAY = 0.06f;
  private const float PROBE_Y_OFFSET = 2.5f;
  private const float PROBE_LENGTH = 20.0f;
  // Snapping on and easing off: a note being played reads as a strike rather than a swell, and
  // the tail is what tells the player which button it was after the next one has already lit.
  private const float LIT_FADE_IN = 0.04f;
  private const float LIT_FADE_OUT = 0.14f;
  // How far towards white the lit cap goes. The palette's own lightest shade is nowhere near
  // enough on its own - nothing in this game blooms, so a lit button has to out-brighten the
  // unlit ones by more than one step of a colour ramp to read across a room.
  private const float LIT_WHITENESS = 0.45f;
  #endregion Constants

  #region Signals
  [Signal]
  public delegate void ButtonPressedEventHandler(int buttonIndex);
  #endregion Signals

  // What the button is doing in the melody being played to the player. The whole row drops to
  // Dim while it plays, so the one that is Lit is the only thing on screen with any light in it.
  public enum Highlight {
    Rest,
    Dim,
    Lit
  }

  #region Exports
  // Which button this is within its room: what a round's sequence is written in.
  [Export]
  public int Index { get; set; } = 0;

  // Which of the piano's samples the button sounds.
  [Export]
  public int NoteIndex { get; set; } = 0;

  // How far the cap travels into its base when stood on.
  [Export]
  public float PressDepth { get; set; } = 26.0f;

  [Export]
  public string ColorGroup {
    get => _colorGroup;
    set {
      _colorGroup = value;
      if (_isWired) {
        _applyColor();
      }
    }
  }
  #endregion Exports

  private enum PressState {
    Released,
    Pressing,
    Pressed,
    Releasing
  }

  #region Fields
  private string _colorGroup = ColorUtils.BLUE;
  private PressState _state = PressState.Released;
  private Color _capRest;
  private Color _capDim;
  private Color _capLit;
  private Color _shadeRest;
  private Color _shadeDim;
  private Color _shadeLit;
  private Color _haloColor;
  private float _restY;
  private bool _isPlayerAbove;
  // The exported setter fires while the scene is still loading, before there are any nodes to
  // push the colour into.
  private bool _isWired;
  private Tween? _pressTween;
  private Tween? _glowTween;
  private PhysicsRayQueryParameters2D? _playerProbe;
  private static readonly StringName _colliderKey = "collider";
  #endregion Fields

  #region Nodes
  [NodePath("Cap")]
  private AnimatableBody2D _capNode = default!;
  [NodePath("Cap/Halo")]
  private Sprite2D _haloNode = default!;
  [NodePath("Cap/CapSpr")]
  private Sprite2D _capSpriteNode = default!;
  [NodePath("Cap/ShadeSpr")]
  private Sprite2D _shadeSpriteNode = default!;
  // Sits just under the cap's top rather than straddling it. The cube's side faces stop a
  // couple of pixels short of its own underside, so an area reaching any higher than that is
  // touched by a face that is not the one standing on the button - and the wrong colour there
  // kills the player on a button they stepped on correctly.
  [NodePath("Cap/ColorArea")]
  private Area2D _colorAreaNode = default!;
  [NodePath("Cap/DetectionArea/CollisionShape2D")]
  private CollisionShape2D _detectionShapeNode = default!;
  [NodePath("ReleaseTimer")]
  private Timer _releaseTimerNode = default!;
  #endregion Nodes

  public override void _EnterTree() {
    base._EnterTree();
    this.WireNodes();
    _isWired = true;
    _restY = _capNode.Position.Y;
    _applyColor();
    _releaseTimerNode.Autostart = false;
    _releaseTimerNode.WaitTime = RELEASE_DELAY;
    _releaseTimerNode.Timeout += _onReleaseTimerTimeout;
  }

  public override void _ExitTree() {
    _releaseTimerNode.Timeout -= _onReleaseTimerTimeout;
    base._ExitTree();
  }

  public override void _PhysicsProcess(double delta) {
    base._PhysicsProcess(delta);
    _isPlayerAbove = _isDown() && _probeForPlayer();
    _startReleaseTimerIfRelevant();
  }

  public Highlight CurrentHighlight { get; private set; } = Highlight.Rest;

  public void SetHighlight(Highlight highlight) {
    CurrentHighlight = highlight;
    var (cap, shade, halo, duration) = highlight switch {
      Highlight.Lit => (_capLit, _shadeLit, _haloColor, LIT_FADE_IN),
      Highlight.Dim => (_capDim, _shadeDim, _transparent(_haloColor), LIT_FADE_OUT),
      _ => (_capRest, _shadeRest, _transparent(_haloColor), LIT_FADE_OUT),
    };
    _glowTween?.Kill();
    _glowTween = CreateTween().SetParallel(true);
    _glowTween.TweenProperty(_capSpriteNode, "modulate", cap, duration);
    _glowTween.TweenProperty(_shadeSpriteNode, "modulate", shade, duration);
    _glowTween.TweenProperty(_haloNode, "modulate", halo, duration);
  }

  private static Color _transparent(Color color) => new Color(color, 0.0f);

  #region Colour
  private void _applyColor() {
    var skinColor = GameSkin.ColorGroupToSkinColor(_colorGroup);
    var skin = SkinManager.Instance.CurrentSkin;
    _capRest = skin.GetColor(skinColor, SkinColorIntensity.Basic);
    _capDim = skin.GetColor(skinColor, SkinColorIntensity.SuperDark);
    _capLit = skin.GetColor(skinColor, SkinColorIntensity.Background).Lerp(Colors.White, LIT_WHITENESS);
    _shadeRest = skin.GetColor(skinColor, SkinColorIntensity.Dark);
    _shadeDim = skin.GetColor(skinColor, SkinColorIntensity.ExtremelyDark);
    _shadeLit = skin.GetColor(skinColor, SkinColorIntensity.Light);
    _haloColor = skin.GetColor(skinColor, SkinColorIntensity.Basic);
    _capSpriteNode.Modulate = _capRest;
    _shadeSpriteNode.Modulate = _shadeRest;
    _haloNode.Modulate = _transparent(_haloColor);

    foreach (var group in ColorUtils.COLOR_GROUPS) {
      _colorAreaNode.RemoveFromGroup(group);
    }
    _colorAreaNode.AddToGroup(_colorGroup);
  }
  #endregion Colour

  #region Press
  public void _onDetectionAreaBodyEntered(Node body) {
    if (body is Player.Player && !_isDown()) {
      _stopReleaseTimer();
      _state = PressState.Pressing;
      _moveCapTo(PressDepth);
    }
  }

  public void _onDetectionAreaBodyExited(Node body) {
    if (body is Player.Player) {
      _startReleaseTimerIfRelevant();
    }
  }

  private bool _isDown() => _state is PressState.Pressing or PressState.Pressed;

  // The cap carries the player down with it, so it has to travel on the physics clock: a tween
  // stepped on the idle clock moves the body between ticks, where nothing is there to notice it.
  private void _moveCapTo(float depth) {
    var target = _restY + depth;
    var duration = Math.Abs(_capNode.Position.Y - target) / PRESS_SPEED;
    _pressTween?.Kill();
    _pressTween = CreateTween();
    _pressTween.SetProcessMode(Tween.TweenProcessMode.Physics);
    _pressTween.Connect(
      Tween.SignalName.Finished,
      new Callable(this, nameof(_onPressTweenFinished)),
      flags: (uint)ConnectFlags.OneShot
    );
    _pressTween.TweenProperty(_capNode, "position:y", target, duration)
        .SetTrans(Tween.TransitionType.Linear)
        .SetEase(Tween.EaseType.InOut);
  }

  private void _onPressTweenFinished() {
    if (_state == PressState.Pressing) {
      _state = PressState.Pressed;
      EmitSignal(SignalName.ButtonPressed, Index);
    }
    else if (_state == PressState.Releasing) {
      _state = PressState.Released;
    }
  }

  private void _startReleaseTimerIfRelevant() {
    if (_isDown() && !_isPlayerHoldingItDown() && _releaseTimerNode.IsStopped()) {
      _releaseTimerNode.Start();
    }
  }

  private void _stopReleaseTimer() {
    if (!_releaseTimerNode.IsStopped()) {
      _releaseTimerNode.Stop();
    }
  }

  private void _onReleaseTimerTimeout() {
    if (_isDown() && !_isPlayerHoldingItDown()) {
      _state = PressState.Releasing;
      _moveCapTo(0.0f);
    }
    _stopReleaseTimer();
  }

  // A player who is climbing away is no longer standing on the cap even while the detection band
  // still contains them.
  private bool _isPlayerHoldingItDown() {
    if (!_isPlayerAbove) {
      return false;
    }
    var player = GameRepo.Instance.Player.Value;
    return player is not null && !player.IsJumping() && player.IsFalling();
  }

  // Five probes spread across the cap, reused query and all: this runs every physics tick the
  // button spends down, and building the machinery fresh allocates a dozen engine objects a tick.
  private bool _probeForPlayer() {
    var spaceState = GetWorld2D().DirectSpaceState;
    var halfWidth = ((_detectionShapeNode.Shape as RectangleShape2D)?.Size.X ?? 0.0f) * 0.5f * Scale.X;

    _playerProbe ??= new PhysicsRayQueryParameters2D {
      Exclude = new Godot.Collections.Array<Rid> { _capNode.GetRid() },
      // The probe leaves from just above the cap, which is inside the cube whenever the cube is
      // the thing being looked for. Without this the ray is skipped for containing its own
      // origin, and the button reads as unoccupied and rises out from under the player.
      HitFromInside = true,
    };

    Span<float> offsets = stackalloc float[] {
      -halfWidth, -halfWidth * 0.5f, 0.0f, halfWidth * 0.5f, halfWidth
    };
    foreach (var offset in offsets) {
      var from = _capNode.GlobalPosition + new Vector2(offset, -PROBE_Y_OFFSET);
      _playerProbe.From = from;
      _playerProbe.To = from + new Vector2(0.0f, -PROBE_LENGTH);
      using var result = spaceState.IntersectRay(_playerProbe);
      // Read as a GodotObject and type-tested here rather than converted straight to a Player:
      // As<T> is a hard cast, and anything else the probe can hit - death debris, a bucket -
      // throws out of the physics tick instead of just not being the player.
      if (result.Count > 0 && result[_colliderKey].As<GodotObject>() is Player.Player) {
        return true;
      }
    }
    return false;
  }
  #endregion Press
}
