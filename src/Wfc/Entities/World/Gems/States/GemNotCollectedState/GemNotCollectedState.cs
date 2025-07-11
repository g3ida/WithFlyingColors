namespace Wfc.Entities.World.Gems;

using System;
using Godot;
using Wfc.State;
using Wfc.Utils.Animation;
using static Godot.Area2D;

public partial class GemNotCollectedState : GemBaseState {
  private const float AMPLITUDE = 4.0f;
  private const float ANIMATION_DURATION = 4.0f;
  private const float SHINE_VARIANCE = 0.08f;
  private const float ROTATION_SPEED = 0.002f;

  private bool _isActive = false;
  private NodeOscillator? _oscillator;
  private AreaEnteredEventHandler? _areaEnteredEventHandler;
  private IState<Gem>? _requestedState = null;


  public GemNotCollectedState() : base() { }

  public override void Init(Gem o) {
    _oscillator = new NodeOscillator(o, AMPLITUDE, ANIMATION_DURATION);
  }

  public override void Enter(Gem o) {
    _isActive = true;
    o.AnimationPlayerNode.Play("RESET");
    o.AnimatedSpriteNode.Play("default");
    o.ShineSfxNode.Play();

    _areaEnteredEventHandler = (Area2D area) => {
      _requestedState = _handleAreaEntered(o, area);
    };
    o.AreaEntered += _areaEnteredEventHandler;
  }

  public override void Exit(Gem o) {
    _isActive = false;
    o.ShineSfxNode.Stop();
    if (_areaEnteredEventHandler != null) {
      o.AreaEntered -= _areaEnteredEventHandler;
      _areaEnteredEventHandler = null;
    }
  }

  public override IState<Gem>? PhysicsUpdate(Gem gem, float delta) {
    gem.LightNode.Position = gem.AnimatedSpriteNode.Position;
    _oscillator?.Update(delta);
    var timer = _oscillator?.Timer ?? 0f;
    gem.LightNode.Energy = 1 + SHINE_VARIANCE * (float)Math.Sin(2 * Mathf.Pi * timer / ANIMATION_DURATION);
    gem.LightNode.Rotate(ROTATION_SPEED);
    return _requestedState;
  }

  public IState<Gem>? _handleAreaEntered(Gem gem, Area2D area) {
    if (!_isActive)
      return null;
    // FIXME: We should remove the player area or make it inactive instead of doing
    // the check here
    if (Global.Instance().Player.IsDying())
      return null;
    if (area.IsInGroup(gem.group_name)) {
      _isActive = false;
      return gem.StatesStore.Collecting;
    }
    return null;
  }
}
