namespace Wfc.Entities.World.Temple;

using System.Collections.Generic;
using Godot;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using Wfc.Utils.Colors;
using EventHandler = Wfc.Core.Event.EventHandler;

[ScenePath]
public partial class GemsTemple : Node2D {
  #region Constants
  private const float WAIT_DELAY = 0.1f;
  private const float MAX_GEM_TEMPLE_ANGULAR_VELOCITY = 10.0f * Mathf.Pi;
  private const float DURATION_TO_FULL_GEM_ROTATION_SPEED = 2.7f;
  private const float ROTATION_DURATION = 3.6f;
  private const float BLOOM_SPRITE_MAX_SCALE = 25.0f;
  private const float BLOOM_SPRITE_SCALE_DURATION = 1.2f;
  private readonly int[] GEMS_EASE_TYPE = { 1, 1, 0, 0 };
  #endregion Constants

  private enum States {
    NotTriggered,
    WalkPhase,
    CollectPhase,
    RotationPhase,
    BloomPhase,
    EndPhase
  }

  private readonly List<Node2D> _templeGems = [];
  private float _gemsAngularVelocity = 0;
  private int _numActiveGems = 0;
  private float _bloomSpriteScale = 1.0f;
  private States _currentState = States.NotTriggered;

  #region Nodes
  [NodePath("GemsContainer")]
  private Node2D GemSlotsContainerNode = default!;
  private Godot.Collections.Array<Node> GemsSlotsNodes = default!;
  [NodePath("StartGemsArea")]
  private Area2D StartGemsAreaNode = default!;
  [NodePath("RotationTimer")]
  private Timer RotationTimerNode = default!;
  [NodePath("BloomSprite")]
  private Sprite2D BloomSpriteNode = default!;
  #endregion Nodes

  private Tween? tweener;

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    GemsSlotsNodes = GemSlotsContainerNode.GetChildren();
    BloomSpriteNode.Visible = false;
  }

  private void _onTriggerAreaBodyEntered(Node body) {
    if (_currentState == States.NotTriggered && body == Global.Instance().Player) {
      _goToWalkPhase();
    }
  }

  private bool _createTempleGems() {
    int i = 0;
    foreach (string colorGrp in ColorUtils.COLOR_GROUPS) {
      if (Global.Instance().GemHUD.IsGemCollected(colorGrp)) {
        var templeGem = _createTempleGem(
            colorGrp,
            WAIT_DELAY * (i + 1),
            Global.Instance().Player.GlobalPosition,
            (GemsSlotsNodes[i] as Node2D)?.GlobalPosition ?? Vector2.Zero,
            GEMS_EASE_TYPE[i]);
        _templeGems.Add(templeGem);
        _numActiveGems += 1;
        i += 1;
      }
    }
    return i > 0;
  }

  private TempleGem _createTempleGem(string colorGroup, float delay, Vector2 position, Vector2 destination, int easeType) {
    var templeGem = SceneHelpers.InstantiateNode<TempleGem>();
    GemSlotsContainerNode.AddChild(templeGem);
    templeGem.Owner = GemSlotsContainerNode;
    templeGem.GlobalPosition = position;
    templeGem.SetColorGroup(colorGroup);
    var waitTime = delay;
    templeGem.MoveToPosition(destination, waitTime, easeType);
    templeGem.Connect(nameof(TempleGem.MoveCompleted), new Callable(this, nameof(_onGemCollected)), (uint)ConnectFlags.OneShot);
    return templeGem;
  }

  public override void _Process(double delta) {
    base._Process(delta);
    switch (_currentState) {
      case States.WalkPhase:
        Global.Instance().Player.SetMaxSpeed();
        break;
      case States.RotationPhase:
        _processRotateGems((float)delta);
        break;
      case States.BloomPhase:
        BloomSpriteNode.Scale = new Vector2(_bloomSpriteScale, _bloomSpriteScale);
        break;
    }
  }

  private void _processRotateGems(float delta) {
    var amount = _gemsAngularVelocity * delta;
    GemSlotsContainerNode.Rotate(amount);
    foreach (Node node in GemSlotsContainerNode.GetChildren()) {
      ((Node2D)node).Rotate(-amount);
    }
    var intensity = 0.5f + (_gemsAngularVelocity / MAX_GEM_TEMPLE_ANGULAR_VELOCITY);
    foreach (Node2D node in _templeGems) {
      node.Set("light_intensity", intensity);
    }
  }

  private void _onGemCollected(Node2D? templeGem) {
    if (_currentState == States.CollectPhase) {
      _numActiveGems -= 1;
      EventHandler.Instance.EmitGemPutInTemple();
      if (_numActiveGems <= 0) {
        _goToRotationPhase();
      }
    }
  }

  private void _startRotationTimer() {
    RotationTimerNode.WaitTime = ROTATION_DURATION;
    RotationTimerNode.Start();
  }

  private void _on_StartGemsArea_body_entered(Node body) {
    if (_currentState == States.WalkPhase && body == Global.Instance().Player) {
      _goToCollectPhase();
    }
  }

  private void _on_RotationTimer_timeout() {
    if (_currentState == States.RotationPhase) {
      _goToBloomPhase();
    }
  }

  // State transitions
  private void _goToNotTriggeredPhase() {
    _currentState = States.NotTriggered;
    foreach (Node2D el in _templeGems) {
      el.QueueFree();
    }
    _templeGems.Clear();
    _gemsAngularVelocity = 0;
    _numActiveGems = 0;
    _bloomSpriteScale = 1.0f;
    BloomSpriteNode.Visible = false;
  }

  private void _goToWalkPhase() {
    _currentState = States.WalkPhase;
    Global.Instance().Player.Velocity = new Vector2(0, Global.Instance().Player.Velocity.Y);
    EventHandler.Instance.EmitGemTempleTriggered();
    Global.Instance().Cutscene.ShowSomeNode(Global.Instance().Player, 10.0f, 3.2f);
  }

  private void _goToRotationPhase() {
    _currentState = States.RotationPhase;
    _rotateGemsTween();
    _startRotationTimer();
    EventHandler.Instance.EmitGemEngineStarted();
  }

  private void _goToCollectPhase() {
    _currentState = States.CollectPhase;
    Global.Instance().Player.Velocity = new Vector2(0, Global.Instance().Player.Velocity.Y);
    if (!_createTempleGems()) {
      _onGemCollected(null);
    }
  }

  private void _goToBloomPhase() {
    _currentState = States.BloomPhase;
    BloomSpriteNode.Visible = true;
    _bloomSpriteResizeTween();
  }

  private static void _goToEndPhase() {
    EventHandler.Instance.EmitLevelCleared();
  }

  // Tween animations
  private void _rotateGemsTween() {
    if (tweener != null) {
      tweener.Kill();
    }
    tweener = GetTree().CreateTween();
    tweener.TweenProperty(this, nameof(_gemsAngularVelocity), MAX_GEM_TEMPLE_ANGULAR_VELOCITY, DURATION_TO_FULL_GEM_ROTATION_SPEED)
        .From(_gemsAngularVelocity)
        .SetTrans(Tween.TransitionType.Linear)
        .SetEase(Tween.EaseType.Out);
  }

  private void _bloomSpriteResizeTween() {
    if (tweener != null) {
      tweener.Kill();
    }
    tweener = GetTree().CreateTween();
    tweener.TweenProperty(this, nameof(_bloomSpriteScale), BLOOM_SPRITE_MAX_SCALE, BLOOM_SPRITE_SCALE_DURATION)
        .From(_bloomSpriteScale)
        .SetTrans(Tween.TransitionType.Linear)
        .SetEase(Tween.EaseType.Out);
    tweener.Connect(
      Tween.SignalName.Finished,
      new Callable(this, nameof(_onBloomSpriteResizeEnd)),
      (uint)ConnectFlags.OneShot
    );
  }

  private static void _onBloomSpriteResizeEnd() {
    _goToEndPhase();
  }
}
